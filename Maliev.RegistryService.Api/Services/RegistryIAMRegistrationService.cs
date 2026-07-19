using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.RegistryService.Api;
using Maliev.RegistryService.Api.Authorization;

namespace Maliev.RegistryService.Api.Services;

/// <summary>
/// Background service to register permissions and roles with the IAM service on startup.
/// </summary>
public class RegistryIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegistryIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public RegistryIAMRegistrationService(IConfiguration configuration, ILogger<RegistryIAMRegistrationService> logger)
        : base(configuration, logger, RegistryConstants.ServiceName)
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return RegistryPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc/>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return RegistryPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
