﻿using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ShareSmallBiz.Portal.Data;

public partial class ShareSmallBizUserContext(DbContextOptions<ShareSmallBizUserContext> options)
    : IdentityDbContext<ShareSmallBizUser>(options)
{
    private void ConfigureEntities(ModelBuilder builder)
    {
        builder.Entity<ShareSmallBizUser>()
            .HasIndex(u => u.Slug)
            .IsUnique();
    }

    private void ConfigureManyToMany(ModelBuilder builder)
    {
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
            warnings.Ignore(RelationalEventId.NonTransactionalMigrationOperationWarning));
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        ConfigureRelationships(builder);
        ConfigureEntities(builder);
        ConfigureManyToMany(builder);
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
}
