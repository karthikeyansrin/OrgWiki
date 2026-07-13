using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrgWiki.Infrastructure.Persistence;

namespace OrgWiki.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration["DATABASE_URL"]
            ?? configuration.GetConnectionString("OrgWiki")
            ?? throw new InvalidOperationException("A PostgreSQL connection string must be configured.");

        services.AddDbContext<OrgWikiDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }
}
