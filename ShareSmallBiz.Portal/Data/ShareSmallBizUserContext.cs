using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Reflection.Emit;

namespace ShareSmallBiz.Portal.Data;
public partial class ShareSmallBizUserContext(DbContextOptions<ShareSmallBizUserContext> options)
    : IdentityDbContext<ShareSmallBizUser>(options)
{
    protected readonly DbContextOptions<ShareSmallBizUserContext> _options = options;
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);

    public virtual DbSet<WebSite> WebSites { get; set; }
    public virtual DbSet<ContentPart> ContentParts { get; set; }
    public virtual DbSet<Keyword> Keywords { get; set; }
    public virtual DbSet<Menu> Menus { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostLike> PostLikes { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        // Configure relationships
        builder.Entity<PostLike>()
            .HasOne(pl => pl.User)
            .WithMany(u => u.LikedPosts)
            .HasForeignKey(pl => pl.UserId);

        builder.Entity<PostLike>()
            .HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId);


        builder.Entity<WebSite>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("Id");

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Name)
                .IsRequired()
                .HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(250);
            entity.HasIndex(e => e.Title).IsUnique();

            entity.Property(e => e.DomainUrl)
                .IsRequired()
                .HasMaxLength(250);
            entity.HasIndex(e => e.DomainUrl).IsUnique();

            entity.Property(e => e.GalleryFolder)
                .IsRequired()
                .HasMaxLength(250);

            entity.Property(e => e.Style)
                .IsRequired()
                .HasMaxLength(100);

        });

        builder.Entity<Menu>(entity =>
        {
            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Controller)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Description)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.KeyWords)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Icon).HasMaxLength(50);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(50);

            entity.Property(e => e.Url).HasMaxLength(100);

            entity.HasOne(d => d.Domain)
                .WithMany(p => p.Menus)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_Menu_Domain");

            entity.HasOne(d => d.Parent)
                .WithMany(p => p.InverseParent)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK_Menu_ParentMenu_ParentId");

        });
        // Many-to-many relationship between Menu and Keyword
        builder.Entity<Menu>()
            .HasMany(m => m.Keywords)
            .WithMany(k => k.Menus)
            .UsingEntity<Dictionary<string, object>>(
                "MenuKeyword",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                j => j.HasOne<Menu>().WithMany().HasForeignKey("MenuId"));


        // Many-to-many relationship between ContentPart and Keyword
        builder.Entity<ContentPart>()
                .HasMany(cp => cp.Keywords)
                .WithMany(k => k.ContentParts)
                .UsingEntity<Dictionary<string, object>>(
                    "ContentPartKeyword",
                    j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                    j => j.HasOne<ContentPart>().WithMany().HasForeignKey("ContentPartId"));


        string sql = "getdate()";
        if (_options.Extensions != null)
        {
            foreach (var ext in _options.Extensions)
            {
                if (ext.GetType().ToString().StartsWith("Microsoft.EntityFrameworkCore.Sqlite"))
                {
                    sql = "DATE('now')";
                    break;
                }
            }
        }

        OnModelCreatingPartial(builder);

    }










    private void UpdateDateTrackingFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (
                    e.State == EntityState.Added
                    || e.State == EntityState.Modified));

        foreach (var entityEntry in entries)
        {
            ((BaseEntity)entityEntry.Entity).ModifiedDate = DateTime.UtcNow;

            if (entityEntry.State == EntityState.Added)
            {
                ((BaseEntity)entityEntry.Entity).CreatedDate = DateTime.UtcNow;
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
            warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning));
    }

    public override int SaveChanges()
    {
        UpdateDateTrackingFields();
        return base.SaveChanges();
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        UpdateDateTrackingFields();
        return await base.SaveChangesAsync();
    }






}
