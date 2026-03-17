using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Maliev.RegistryService.Infrastructure;

/// <summary>
/// Extension methods for registering Infrastructure-layer services.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers EF Core repositories and location services.
    /// HTTP clients must be registered separately via <see cref="AddInfrastructureHttpClients"/>.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddScoped<IThaiRegistryService, ThaiRegistryService>();
        return services;
    }

    /// <summary>
    /// Registers the typed <see cref="HttpClient"/> instances for Creden (primary) and
    /// BDEX (fallback), plus the <see cref="IThaiCompanyRegistryService"/> orchestrator.
    /// Must be called after <c>AddStandardResilienceHandler</c> is available on the builder.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">Application configuration (reads <c>Creden:BaseUrl</c>).</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructureHttpClients(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var credenBaseUrl = configuration["Creden:BaseUrl"] ?? "https://data.creden.co";

        // Creden.co — primary provider, open API, no credentials required
        services.AddHttpClient<ICredenProxyService, CredenProxyService>(client =>
        {
            client.BaseAddress = new Uri(credenBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(20);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            // Browser-like User-Agent to avoid bot-blocking
            client.DefaultRequestHeaders.Add(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/122.0.0.0 Safari/537.36");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression =
                System.Net.DecompressionMethods.GZip |
                System.Net.DecompressionMethods.Deflate |
                System.Net.DecompressionMethods.Brotli
        })
        .AddStandardResilienceHandler();

        // BDEX (api.dbd.go.th) — fallback provider, 13-digit tax-ID only, OAuth2
        services.AddHttpClient<IDbdProxyService, DbdProxyService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Add("Accept", "application/json");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
        {
            AutomaticDecompression =
                System.Net.DecompressionMethods.GZip |
                System.Net.DecompressionMethods.Deflate |
                System.Net.DecompressionMethods.Brotli
        })
        .AddStandardResilienceHandler();

        // Orchestrator: tries Creden first, falls back to BDEX
        services.TryAddScoped<IThaiCompanyRegistryService, ThaiCompanyRegistryService>();

        return services;
    }
}
