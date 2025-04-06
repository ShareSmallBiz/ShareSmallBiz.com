using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ShareSmallBiz.Portal.Data.Entities;

namespace ShareSmallBiz.Portal.Data;

public partial class ShareSmallBizUserContext(DbContextOptions<ShareSmallBizUserContext> options)
    : IdentityDbContext<ShareSmallBizUser>(options)
{
    private void ConfigureEntities(ModelBuilder builder)
    {
        // Configure user slug to be unique
        builder.Entity<ShareSmallBizUser>()
            .HasIndex(u => u.Slug)
            .IsUnique();

        // Configure each concrete entity to map to its own table
        builder.Entity<Keyword>().ToTable("Keywords");
        builder.Entity<MediaEntity>().ToTable("Media");
        builder.Entity<Post>().ToTable("Posts");
        builder.Entity<PostComment>().ToTable("PostComments");
        builder.Entity<PostCommentLike>().ToTable("PostCommentLikes");
        builder.Entity<PostLike>().ToTable("PostLikes");
        builder.Entity<SocialLink>().ToTable("SocialLinks");
        builder.Entity<UserFollow>().ToTable("UserFollows");
    }

    private void ConfigureManyToMany(ModelBuilder builder)
    {
    }

    private void ConfigureRelationships(ModelBuilder builder)
    {
        // ---- USER ROLES
        builder.Entity<ShareSmallBizUser>()
            .HasMany(u => u.UserRoles)
            .WithOne()
            .HasForeignKey(ur => ur.UserId)
            .IsRequired();

        builder.Entity<Keyword>()
            .HasMany(k => k.Posts)
            .WithMany(p => p.PostCategories)
            .UsingEntity(j => j.ToTable("PostKeywords"));  // Many-to-many relationship between Keywords and Posts

        // ---- POSTS, COMMENTS, LIKES ----
        builder.Entity<PostLike>()
            .HasOne(pl => pl.Post)
            .WithMany(p => p.Likes)
            .HasForeignKey(pl => pl.PostId);

        builder.Entity<PostComment>()
            .HasOne(pc => pc.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(pc => pc.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<PostCommentLike>()
            .HasOne(pcl => pcl.PostComment)
            .WithMany(pc => pc.Likes)
            .HasForeignKey(pcl => pcl.PostCommentId);

        // ---- FOLLOWERS ----
        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId);
    }

    // Configure relationships between entities and users
    private void ConfigureUserRelationships(ModelBuilder builder)
    {
        // Important: use direct references to string FK property instead of navigation properties
        // for relationships with ShareSmallBizUser

        builder.Entity<PostLike>()
            .HasOne<ShareSmallBizUser>()
            .WithMany(u => u.LikedPosts)
            .HasForeignKey(pl => pl.CreatedID)
            .IsRequired(false);

        builder.Entity<PostCommentLike>()
            .HasOne<ShareSmallBizUser>()
            .WithMany(u => u.LikedPostComments)
            .HasForeignKey(pcl => pcl.CreatedID)
            .IsRequired(false);

        builder.Entity<UserFollow>()
            .HasOne<ShareSmallBizUser>()
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.CreatedID)
            .IsRequired(false);

        builder.Entity<Post>()
            .HasOne<ShareSmallBizUser>()
            .WithMany(u => u.Posts)
            .HasForeignKey(p => p.CreatedID)
            .IsRequired(false);

        builder.Entity<MediaEntity>()
            .HasOne<ShareSmallBizUser>()
            .WithMany(u => u.Media)
            .HasForeignKey(m => m.CreatedID)
            .IsRequired(false);

        builder.Entity<Keyword>()
            .HasOne<ShareSmallBizUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedID)
            .IsRequired(false);

        builder.Entity<PostComment>()
            .HasOne<ShareSmallBizUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedID)
            .IsRequired(false);

        builder.Entity<SocialLink>()
            .HasOne<ShareSmallBizUser>()
            .WithMany()
            .HasForeignKey(e => e.CreatedID)
            .IsRequired(false);
    }

    private void UpdateDateTrackingFields()
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is BaseEntity && (e.State == EntityState.Added || e.State == EntityState.Modified));

        foreach (var entry in entries)
        {
            if (entry.Entity is BaseEntity entity)
            {
                entity.ModifiedDate = DateTime.UtcNow;
                if (entry.State == EntityState.Added)
                {
                    entity.CreatedDate = DateTime.UtcNow;
                }
            }
        }
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.ConfigureWarnings(warnings =>
        {
            warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning);

            // Ignore the specific foreign key warning
            warnings.Ignore(RelationalEventId.ForeignKeyPropertiesMappedToUnrelatedTables);
        });
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Important: Set the discriminator for BaseEntity class to handle inheritance
        builder.Entity<BaseEntity>().UseTptMappingStrategy();

        ConfigureEntities(builder);
        ConfigureRelationships(builder);
        ConfigureManyToMany(builder);
        ConfigureUserRelationships(builder);
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

    public virtual DbSet<Keyword> Keywords { get; set; }
    public virtual DbSet<PostCommentLike> PostCommentLikes { get; set; }
    public virtual DbSet<PostComment> PostComments { get; set; }
    public virtual DbSet<PostLike> PostLikes { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<SocialLink> SocialLinks { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }
    public virtual DbSet<MediaEntity> Media { get; set; }
}