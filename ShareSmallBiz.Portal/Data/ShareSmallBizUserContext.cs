using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ShareSmallBiz.Portal.Data;

public partial class ShareSmallBizUserContext(DbContextOptions<ShareSmallBizUserContext> options)
    : IdentityDbContext<ShareSmallBizUser>(options)
{
    public virtual DbSet<WebSite> WebSites { get; set; }
    public virtual DbSet<ContentPart> ContentParts { get; set; }
    public virtual DbSet<Keyword> Keywords { get; set; }
    public virtual DbSet<Menu> Menus { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostLike> PostLikes { get; set; }
    public virtual DbSet<PostComment> PostComments { get; set; }
    public virtual DbSet<PostCommentLike> PostCommentLikes { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureRelationships(builder);
        ConfigureEntities(builder);
        ConfigureManyToMany(builder);
    }

    private void ConfigureRelationships(ModelBuilder builder)
    {
        builder.Entity<PostLike>()
            .HasOne(pl => pl.User)
            .WithMany(u => u.LikedPosts)
            .HasForeignKey(pl => pl.UserId);

        builder.Entity<PostLike>()
            .HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId);

        builder.Entity<PostComment>()
            .HasOne(pc => pc.Post)
            .WithMany(p => p.Comments) 
            .HasForeignKey(pc => pc.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComment>()
            .HasOne(pc => pc.ParentPost)
            .WithMany()  
            .HasForeignKey(pc => pc.ParentPostId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PostCommentLike>()
            .HasOne(pcl => pcl.User)
            .WithMany(u => u.LikedPostComments)
            .HasForeignKey(pcl => pcl.CreatedID);

        builder.Entity<PostCommentLike>()
            .HasOne(pcl => pcl.PostComment)
            .WithMany(pc => pc.Likes)
            .HasForeignKey(pcl => pcl.PostCommentId);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId);
    }

    private void ConfigureEntities(ModelBuilder builder)
    {
        builder.Entity<WebSite>(entity =>
        {
            entity.Property(e => e.Id).HasColumnName("Id");
            entity.Property(e => e.Description).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Property(e => e.Title).IsRequired().HasMaxLength(250);
            entity.HasIndex(e => e.Title).IsUnique();
            entity.Property(e => e.DomainUrl).IsRequired().HasMaxLength(250);
            entity.HasIndex(e => e.DomainUrl).IsUnique();
            entity.Property(e => e.GalleryFolder).IsRequired().HasMaxLength(250);
            entity.Property(e => e.Style).IsRequired().HasMaxLength(100);
        });

        builder.Entity<Menu>(entity =>
        {
            entity.Property(e => e.Action).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Controller).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(100);
            entity.Property(e => e.KeyWords).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(50);
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

        builder.Entity<Post>()
            .HasKey(p => p.Id);

        builder.Entity<Post>()
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Post>()
            .HasMany(p => p.Likes)
            .WithOne(l => l.Post)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostComment>()
            .HasKey(c => c.Id);

        builder.Entity<PostComment>()
            .Property(c => c.Content)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Entity<PostComment>()
            .HasOne(c => c.ParentPost)
            .WithMany()
            .HasForeignKey(c => c.ParentPostId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<PostLike>()
            .HasKey(l => new { l.PostId, l.UserId });

        builder.Entity<PostLike>()
            .HasOne(l => l.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(l => l.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostLike>()
            .Property(l => l.UserId)
            .IsRequired();
    }

    private void ConfigureManyToMany(ModelBuilder builder)
    {
        builder.Entity<Menu>()
            .HasMany(m => m.Keywords)
            .WithMany(k => k.Menus)
            .UsingEntity<Dictionary<string, object>>(
                "MenuKeyword",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                j => j.HasOne<Menu>().WithMany().HasForeignKey("MenuId"));

        builder.Entity<ContentPart>()
            .HasMany(cp => cp.Keywords)
            .WithMany(k => k.ContentParts)
            .UsingEntity<Dictionary<string, object>>(
                "ContentPartKeyword",
                j => j.HasOne<Keyword>().WithMany().HasForeignKey("KeywordId"),
                j => j.HasOne<ContentPart>().WithMany().HasForeignKey("ContentPartId"));
    }

    private void UpdateDateTrackingFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity &&
                        (e.State == EntityState.Added || e.State == EntityState.Modified));

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
        return await base.SaveChangesAsync(cancellationToken);
    }
}
