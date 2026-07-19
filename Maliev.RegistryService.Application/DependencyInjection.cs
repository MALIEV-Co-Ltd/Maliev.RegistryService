using Microsoft.Extensions.DependencyInjection;

namespace Maliev.RegistryService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Add application layer services if any (currently mostly interfaces implemented in infrastructure)
        return services;
    }
}
