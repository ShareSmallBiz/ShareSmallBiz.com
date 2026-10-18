using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ShareSmallBiz.Core.Auth;
using ShareSmallBiz.Core.Users;

namespace ShareSmallBiz.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options)
{
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<AppUser>(user =>
        {
            user.Property(u => u.DisplayName).HasMaxLength(AppUser.DisplayNameMaxLength);
            user.Property(u => u.FirstName).HasMaxLength(AppUser.NameMaxLength);
            user.Property(u => u.LastName).HasMaxLength(AppUser.NameMaxLength);
            user.Property(u => u.Bio).HasMaxLength(AppUser.BioMaxLength);
            user.Property(u => u.WebsiteUrl).HasMaxLength(AppUser.WebsiteUrlMaxLength);
            user.Property(u => u.ProfileVisibility).HasConversion<string>().HasMaxLength(20);
            user.HasIndex(u => u.DisplayName);
        });

        builder.Entity<RefreshToken>(token =>
        {
            token.Property(t => t.TokenHash).HasMaxLength(64);
            token.HasIndex(t => t.TokenHash).IsUnique();
            token.HasIndex(t => t.FamilyId);
            token.HasOne<AppUser>().WithMany().HasForeignKey(t => t.UserId).OnDelete(DeleteBehavior.Cascade);
        });

        // SQLite cannot order or compare DateTimeOffset natively; store as UTC ticks
        foreach (var property in builder.Model.GetEntityTypes()
                     .SelectMany(t => t.GetProperties())
                     .Where(p => p.ClrType == typeof(DateTimeOffset) || p.ClrType == typeof(DateTimeOffset?)))
        {
            property.SetValueConverter(property.ClrType == typeof(DateTimeOffset)
                ? DateTimeOffsetTicksConverter.Instance
                : NullableDateTimeOffsetTicksConverter.Instance);
        }
    }
}
