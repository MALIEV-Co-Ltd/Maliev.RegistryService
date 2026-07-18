namespace Maliev.RegistryService.Tests;

public sealed class WorkflowContractTests
{
    private static readonly string Root = FindRoot();
    private static readonly string Workflows = Path.Combine(Root, ".github", "workflows");

    [Fact]
    public void PullRequests_AlwaysUseReadOnlyReusableValidation()
    {
        var source = Read("pr-validation.yml");
        Assert.Contains("pull_request:", source, StringComparison.Ordinal);
        Assert.Contains("contents: read", source, StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/_validate.yml", source, StringComparison.Ordinal);
        Assert.DoesNotContain("paths:", source, StringComparison.Ordinal);
        AssertSafe(source);
    }

    [Theory]
    [InlineData("ci-main.yml", "main")]
    [InlineData("ci-develop.yml", "develop")]
    [InlineData("ci-staging.yml", "release/v*")]
    public void BranchAndTagWorkflows_AreValidationOnly(string file, string trigger)
    {
        var source = Read(file);
        Assert.Contains(trigger, source, StringComparison.Ordinal);
        Assert.Contains("uses: ./.github/workflows/_validate.yml", source, StringComparison.Ordinal);
        AssertSafe(source);
    }

    [Fact]
    public void ReusableValidation_UsesImmutablePublicSharedSources()
    {
        var source = Read("_validate.yml");
        Assert.Contains("workflow_call:", source, StringComparison.Ordinal);
        Assert.Contains("name: validate", source, StringComparison.Ordinal);
        Assert.Contains("actions/checkout@9c091bb21b7c1c1d1991bb908d89e4e9dddfe3e0", source, StringComparison.Ordinal);
        Assert.Contains("actions/setup-dotnet@a98b56852c35b8e3190ac28c8c2271da59106c68", source, StringComparison.Ordinal);
        Assert.Contains("ref: 6344320b4e8c11e55ebc78107308a80b74f498da", source, StringComparison.Ordinal);
        Assert.Contains("ref: d633b24f9c02e4da2024bd164f1fb61acf6e6862", source, StringComparison.Ordinal);
        Assert.Contains("dotnet-version: 10.0.x", source, StringComparison.Ordinal);
        Assert.Contains("--configfile nuget.validation.config", source, StringComparison.Ordinal);

        var nuget = File.ReadAllText(Path.Combine(Root, "nuget.validation.config"));
        Assert.Contains("<clear />", nuget, StringComparison.Ordinal);
        Assert.Contains("https://api.nuget.org/v3/index.json", nuget, StringComparison.Ordinal);
        Assert.DoesNotContain("nuget.pkg.github.com", nuget, StringComparison.OrdinalIgnoreCase);
        AssertSafe(source);
    }

    [Fact]
    public void EveryWorkflow_ForbidsCredentialsAndDeploymentMutation()
    {
        foreach (var file in Directory.GetFiles(Workflows, "*.yml"))
        {
            AssertSafe(File.ReadAllText(file));
        }
    }

    private static void AssertSafe(string source)
    {
        foreach (var forbidden in new[]
        {
            "secrets.", "GITOPS_PAT", "GCP_SA_KEY", "NUGET_PASSWORD", "id-token: write",
            "credentials_json", "google-github-actions/auth", "gcloud auth", "docker push",
            "maliev-gitops", "kustomize edit", "git push origin", "gh pr create", "pull_request_target",
        })
        {
            Assert.DoesNotContain(forbidden, source, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string Read(string file)
    {
        var path = Path.Combine(Workflows, file);
        Assert.True(File.Exists(path), $"Required workflow is missing: {file}");
        return File.ReadAllText(path);
    }

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Maliev.RegistryService.slnx")))
            {
                return directory.FullName;
            }
        }
        throw new DirectoryNotFoundException("Could not locate RegistryService repository root.");
    }
}
