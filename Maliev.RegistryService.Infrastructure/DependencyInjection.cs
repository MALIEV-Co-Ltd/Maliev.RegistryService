using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.RegistryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IThaiRegistryService, ThaiRegistryService>();
        
        services.AddHttpClient<IDbdProxyService, DbdProxyService>();

        return services;
    }
}
