using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Asp.Versioning;
using Maliev.Aspire.ServiceDefaults.Authorization;
using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.RegistryService.Api.Authorization;
using Maliev.RegistryService.Api.Controllers;
using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Options;

namespace Maliev.RegistryService.Tests.Unit;

/// <summary>
/// Tests RegistryService's outbound workload authentication boundary.
/// </summary>
public sealed class ServiceAuthenticationWiringTests
{
    private const string ExpectedToken = "centrally-issued-registry-token";

    /// <summary>
    /// RegistryService startup should opt into AuthService exchange and the central IAM client only.
    /// </summary>
    [Fact]
    public void Program_RegistersRegistryExchangeWithoutLegacySigner()
    {
        var source = ReadRepositoryFile("Maliev.RegistryService.Api", "Program.cs");

        Assert.Contains("builder.AddAuthServiceTokenExchange(\"RegistryService\");", source, StringComparison.Ordinal);
        Assert.Contains("builder.AddAuthServiceIAMClient();", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddIAMServiceClient", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// The process identity should be exact and no local-signing services should resolve.
    /// </summary>
    [Fact]
    public void AuthServiceIamClient_RegistersExactIdentityWithoutLegacySigningServices()
    {
        var builder = CreateConfiguredBuilder();

        builder.AddAuthServiceTokenExchange("RegistryService");
        builder.AddAuthServiceIAMClient();

        using var provider = builder.Services.BuildServiceProvider();
        var identity = provider.GetRequiredService<ServiceProcessIdentity>();

        Assert.Equal("RegistryService", identity.ServiceName);
        Assert.Single(provider.GetServices<IIamServiceClient>());
        Assert.Null(provider.GetService<IServiceAccountTokenProvider>());
        Assert.Null(provider.GetService<ServiceAccountAuthenticationHandler>());
    }

    /// <summary>
    /// IAM permission checks should use the bearer token supplied by AuthService exchange.
    /// </summary>
    [Fact]
    public async Task IamPermissionCheck_UsesAuthServiceExchangedBearerTokenOnExactRoute()
    {
        var builder = CreateConfiguredBuilder();
        var capture = new AuthorizationCaptureHandler();
        builder.Services.AddSingleton<IHttpMessageHandlerBuilderFilter>(
            new CapturingPrimaryHandlerFilter(capture));

        builder.AddAuthServiceTokenExchange("RegistryService");
        builder.Services.AddSingleton<IAuthServiceTokenProvider>(new StubTokenProvider());
        builder.AddAuthServiceIAMClient();

        await using var provider = builder.Services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var iamClient = scope.ServiceProvider.GetRequiredService<IIamServiceClient>();

        var allowed = await iamClient.CheckPermissionAsync(
            $"registry-test-{Guid.NewGuid():N}",
            RegistryPermissions.LocationsRead,
            cancellationToken: CancellationToken.None);

        Assert.True(allowed);
        Assert.Equal(new AuthenticationHeaderValue("Bearer", ExpectedToken), capture.Authorization);
        Assert.Equal(new Uri("https://iam.test/iam/v1/auth/check-permission"), capture.RequestUri);
    }

    /// <summary>
    /// Missing or malformed workload credentials should fail options validation.
    /// </summary>
    [Theory]
    [InlineData(null, null)]
    [InlineData("service-registry-service", "short")]
    public void AuthServiceExchange_InvalidCredentials_FailsClosed(string? clientId, string? clientSecret)
    {
        var builder = CreateConfiguredBuilder(clientId, clientSecret);
        builder.AddAuthServiceTokenExchange("RegistryService");

        using var provider = builder.Services.BuildServiceProvider();

        Assert.Throws<OptionsValidationException>(
            () => provider.GetRequiredService<IOptions<AuthServiceTokenExchangeOptions>>().Value);
    }

    /// <summary>
    /// CI should consume the exact published ServiceDefaults version containing central exchange support.
    /// </summary>
    [Fact]
    public void ServiceDefaultsDependency_PinsPublishedCentralExchangeVersion()
    {
        var source = ReadRepositoryFile("Directory.Build.props");

        Assert.Contains(
            "<ServiceDefaultsVersion Condition=\"'$(ServiceDefaultsVersion)' == ''\">1.0.89-alpha</ServiceDefaultsVersion>",
            source,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "<ServiceDefaultsVersion Condition=\"'$(ServiceDefaultsVersion)' == ''\">1.0.*",
            source,
            StringComparison.Ordinal);

        foreach (var project in new[]
                 {
                     "Maliev.RegistryService.Api/Maliev.RegistryService.Api.csproj",
                     "Maliev.RegistryService.Application/Maliev.RegistryService.Application.csproj",
                     "Maliev.RegistryService.Infrastructure/Maliev.RegistryService.Infrastructure.csproj",
                     "Maliev.RegistryService.Tests/Maliev.RegistryService.Tests.csproj"
                 })
        {
            var projectSource = ReadRepositoryFile(project.Split('/'));
            Assert.Contains(
                "<PackageReference Include=\"Maliev.Aspire.ServiceDefaults\" Version=\"$(ServiceDefaultsVersion)\" />",
                projectSource,
                StringComparison.Ordinal);
        }
    }

    /// <summary>
    /// The Docker restore layer must include the shared version property before restoring package-mode projects.
    /// </summary>
    [Fact]
    public void Dockerfile_CopiesSharedVersionPropertiesBeforePackageRestore()
    {
        var source = ReadRepositoryFile("Maliev.RegistryService.Api", "Dockerfile");
        var propertiesCopy = source.IndexOf(
            "COPY [\"Directory.Build.props\", \".\"]",
            StringComparison.Ordinal);
        var restore = source.IndexOf(
            "dotnet restore \"./Maliev.RegistryService.Api/Maliev.RegistryService.Api.csproj\"",
            StringComparison.Ordinal);

        Assert.True(propertiesCopy >= 0, "Dockerfile must copy Directory.Build.props into the restore layer.");
        Assert.True(restore > propertiesCopy, "Directory.Build.props must be available before dotnet restore.");
    }

    /// <summary>
    /// Empty service origins must not shadow ServiceDefaults' environment-aware defaults.
    /// </summary>
    [Theory]
    [InlineData("appsettings.json")]
    [InlineData("appsettings.Development.json")]
    public void ApplicationConfiguration_DoesNotDeclareEmptyServiceOrigins(string fileName)
    {
        var source = ReadRepositoryFile("Maliev.RegistryService.Api", fileName);

        Assert.DoesNotContain("\"BaseUrl\": \"\"", source, StringComparison.Ordinal);
    }

    /// <summary>
    /// Registry routes and permission policies are part of the service contract and must remain unchanged.
    /// </summary>
    [Fact]
    public void RegistryEndpoints_RetainVersionedRoutesAndPermissionPolicies()
    {
        AssertControllerRoute<CompaniesController>("registry/v{version:apiVersion}/thai/companies");
        AssertEndpoint<CompaniesController>(nameof(CompaniesController.Search), "search", "GET", RegistryPermissions.CompaniesRead);

        AssertControllerRoute<LocationsController>("registry/v{version:apiVersion}/thai/addresses");
        AssertEndpoint<LocationsController>(nameof(LocationsController.List), null, "GET", RegistryPermissions.LocationsRead);
        AssertEndpoint<LocationsController>(nameof(LocationsController.Autocomplete), "autocomplete", "GET", RegistryPermissions.LocationsRead);
        AssertEndpoint<LocationsController>(nameof(LocationsController.AutocompleteMultiField), "autocomplete-multi", "GET", RegistryPermissions.LocationsRead);
        AssertEndpoint<LocationsController>(nameof(LocationsController.GetById), "{id:guid}", "GET", RegistryPermissions.LocationsRead);
        AssertEndpoint<LocationsController>(nameof(LocationsController.Create), null, "POST", RegistryPermissions.LocationsCreate);
        AssertEndpoint<LocationsController>(nameof(LocationsController.Update), "{id:guid}", "PUT", RegistryPermissions.LocationsUpdate);
        AssertEndpoint<LocationsController>(nameof(LocationsController.Delete), "{id:guid}", "DELETE", RegistryPermissions.LocationsDelete);
    }

    /// <summary>
    /// Creden and BDEX must remain distinct typed HTTP clients outside the IAM exchange pipeline.
    /// </summary>
    [Fact]
    public void InfrastructureHttpClients_KeepCredenAndBdexRegistrationsSeparate()
    {
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Creden:BaseUrl"] = "https://creden.test"
            })
            .Build();

        services.AddLogging();
        services.AddInfrastructureHttpClients(configuration);

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICredenProxyService));
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IDbdProxyService));
        Assert.DoesNotContain(
            services,
            descriptor => descriptor.ServiceType == typeof(AuthServiceTokenExchangeHandler));

        var source = ReadRepositoryFile(
            "Maliev.RegistryService.Infrastructure",
            "DependencyInjection.cs");
        Assert.Contains(
            "AddHttpClient<ICredenProxyService, CredenProxyService>",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "AddHttpClient<IDbdProxyService, DbdProxyService>",
            source,
            StringComparison.Ordinal);
    }

    private static HostApplicationBuilder CreateConfiguredBuilder(
        string? clientId = "service-registry-service",
        string? clientSecret = "registry-test-secret-with-at-least-32-bytes")
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            EnvironmentName = "Testing"
        });

        using var rsa = RSA.Create(2048);
        builder.Configuration["ServiceAuthentication:ClientId"] = clientId;
        builder.Configuration["ServiceAuthentication:ClientSecret"] = clientSecret;
        builder.Configuration["Services:AuthService:BaseUrl"] = "https://auth.test";
        builder.Configuration["Services:IAMService:BaseUrl"] = "https://iam.test";
        builder.Configuration["Jwt:PublicKey"] = Convert.ToBase64String(
            Encoding.UTF8.GetBytes(rsa.ExportSubjectPublicKeyInfoPem()));
        builder.Configuration["Jwt:Issuer"] = "https://api.maliev.com";
        builder.Configuration["Jwt:Audience"] = "https://api.maliev.com";

        return builder;
    }

    private static void AssertControllerRoute<TController>(string expectedTemplate)
    {
        var controller = typeof(TController);
        Assert.NotNull(controller.GetCustomAttribute<ApiVersionAttribute>());
        Assert.Equal(expectedTemplate, controller.GetCustomAttribute<RouteAttribute>()?.Template);
    }

    private static void AssertEndpoint<TController>(
        string methodName,
        string? expectedTemplate,
        string expectedVerb,
        string expectedPermission)
    {
        var method = typeof(TController).GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        var route = method.GetCustomAttributes<HttpMethodAttribute>().Single();
        Assert.Equal(expectedTemplate, route.Template);
        Assert.Contains(expectedVerb, route.HttpMethods);
        Assert.Equal(expectedPermission, method.GetCustomAttribute<RequirePermissionAttribute>()?.Permission);
    }

    private static string ReadRepositoryFile(params string[] segments)
    {
        var path = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            Path.Combine(segments)));

        Assert.True(File.Exists(path), $"Could not find source file: {path}");
        return File.ReadAllText(path);
    }

    private sealed class StubTokenProvider : IAuthServiceTokenProvider
    {
        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(ExpectedToken);
    }

    private sealed class AuthorizationCaptureHandler : HttpMessageHandler
    {
        public AuthenticationHeaderValue? Authorization { get; private set; }

        public Uri? RequestUri { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization;
            RequestUri = request.RequestUri;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"allowed\":true}", Encoding.UTF8, "application/json")
            });
        }
    }

    private sealed class CapturingPrimaryHandlerFilter(HttpMessageHandler primaryHandler)
        : IHttpMessageHandlerBuilderFilter
    {
        public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next) => builder =>
        {
            next(builder);
            for (var index = builder.AdditionalHandlers.Count - 1; index >= 0; index--)
            {
                if (builder.AdditionalHandlers[index].GetType().FullName?.Contains(
                        "ServiceDiscovery",
                        StringComparison.Ordinal) == true ||
                    builder.AdditionalHandlers[index].GetType().FullName?.Contains(
                        "ResolvingHttpDelegatingHandler",
                        StringComparison.Ordinal) == true)
                {
                    builder.AdditionalHandlers.RemoveAt(index);
                }
            }

            builder.PrimaryHandler = primaryHandler;
        };
    }
}
