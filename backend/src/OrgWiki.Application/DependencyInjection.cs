using Microsoft.Extensions.DependencyInjection;

namespace OrgWiki.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}
