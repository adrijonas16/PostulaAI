using Microsoft.EntityFrameworkCore;

namespace PostulaAI.Api.Shared.Persistence;

/// <summary>
/// Single EF Core context for the modular monolith. Each module contributes its entity
/// configurations (IEntityTypeConfiguration) which are discovered from this assembly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
