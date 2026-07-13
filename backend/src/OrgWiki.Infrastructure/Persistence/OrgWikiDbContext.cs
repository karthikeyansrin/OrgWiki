using Microsoft.EntityFrameworkCore;

namespace OrgWiki.Infrastructure.Persistence;

public sealed class OrgWikiDbContext(DbContextOptions<OrgWikiDbContext> options) : DbContext(options)
{
}
