using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ShareSmallBiz.Portal.Data;

public partial class ShareSmallBizUserContext(DbContextOptions<ShareSmallBizUserContext> options)
    : IdentityDbContext<ShareSmallBizUser>(options)
{
    public virtual DbSet<Keyword> Keywords { get; set; }
    public virtual DbSet<Post> Posts { get; set; }
    public virtual DbSet<PostLike> PostLikes { get; set; }
    public virtual DbSet<PostComment> PostComments { get; set; }
    public virtual DbSet<PostCommentLike> PostCommentLikes { get; set; }
    public virtual DbSet<UserFollow> UserFollows { get; set; }
    public virtual DbSet<UserCollaboration> UserCollaborations { get; set; }
    public virtual DbSet<UserContentContribution> UserContentContributions { get; set; }
    public virtual DbSet<UserService> UserServices { get; set; }
    public virtual DbSet<SocialLink> SocialLinks { get; set; }
    public virtual DbSet<Testimonial> Testimonials { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureRelationships(builder);
        ConfigureEntities(builder);
        ConfigureManyToMany(builder);
    }


    private void ConfigureRelationships(ModelBuilder builder)
    {
        // ---- POSTS, COMMENTS, LIKES ----
        builder.Entity<PostLike>()
            .HasKey(pl => pl.Id);

        builder.Entity<PostLike>()
            .Property(pl => pl.Id)
            .ValueGeneratedOnAdd();  // Ensures auto-increment behavior

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

        builder.Entity<PostCommentLike>()
            .HasOne(pcl => pcl.User)
            .WithMany(u => u.LikedPostComments)
            .HasForeignKey(pcl => pcl.CreatedID);

        // ---- FOLLOWERS ----
        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Follower)
            .WithMany(u => u.Following)
            .HasForeignKey(uf => uf.FollowerId);

        builder.Entity<UserFollow>()
            .HasOne(uf => uf.Following)
            .WithMany(u => u.Followers)
            .HasForeignKey(uf => uf.FollowingId);

        // ---- USER COLLABORATIONS ----
        builder.Entity<UserCollaboration>()
            .HasOne(uc => uc.User1)
            .WithMany()
            .HasForeignKey(uc => uc.UserId1);

        builder.Entity<UserCollaboration>()
            .HasOne(uc => uc.User2)
            .WithMany()
            .HasForeignKey(uc => uc.UserId2);

        // ---- USER CONTENT CONTRIBUTIONS ----
        builder.Entity<UserContentContribution>()
            .HasOne(ucc => ucc.User)
            .WithMany(u => u.ContentContributions)
            .HasForeignKey(ucc => ucc.UserId);
    }


    private void ConfigureEntities(ModelBuilder builder)
    {
        builder.Entity<ShareSmallBizUser>()
            .HasIndex(u => u.Slug)
            .IsUnique();
    }

    private void ConfigureManyToMany(ModelBuilder builder)
    {
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
