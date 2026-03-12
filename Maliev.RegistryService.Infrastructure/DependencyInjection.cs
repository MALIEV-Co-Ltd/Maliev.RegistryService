using Maliev.RegistryService.Application.Interfaces;
using Maliev.RegistryService.Infrastructure.Persistence;
using Maliev.RegistryService.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Maliev.RegistryService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<RegistryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IThaiRegistryService, ThaiRegistryService>();
        
        services.AddHttpClient<IDbdProxyService, DbdProxyService>();

        return services;
    }
}
