using Microsoft.EntityFrameworkCore;
using PostulaAI.Api.Modules.AuditLogs.Domain;
using PostulaAI.Api.Modules.Auth.Domain;
using PostulaAI.Api.Modules.ProfessionalProfiles.Domain;
using PostulaAI.Api.Modules.Users.Domain;

namespace PostulaAI.Api.Shared.Persistence;

/// <summary>
/// Single EF Core context for the modular monolith. Each module contributes its entity
/// configurations (IEntityTypeConfiguration) which are discovered from this assembly.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<ProfessionalProfile> ProfessionalProfiles => Set<ProfessionalProfile>();

    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
