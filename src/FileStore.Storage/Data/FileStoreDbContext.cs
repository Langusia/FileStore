using FileStore.Storage.Models;
using Microsoft.EntityFrameworkCore;

namespace FileStore.Storage.Data;

/// <summary>
/// Database context for FileStore metadata.
/// Stores metadata about buckets, objects, and tags - NOT the actual file content.
/// </summary>
public class FileStoreDbContext : DbContext
{
    public FileStoreDbContext(DbContextOptions<FileStoreDbContext> options)
        : base(options)
    {
    }

    public DbSet<StorageBucket> Buckets { get; set; } = null!;
    public DbSet<StorageObject> Objects { get; set; } = null!;
    public DbSet<BucketTag> BucketTags { get; set; } = null!;
    public DbSet<ObjectTag> ObjectTags { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure StorageBucket
        modelBuilder.Entity<StorageBucket>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.BucketName).IsUnique();
            entity.HasIndex(e => new { e.ChannelId, e.OperationId });

            entity.HasMany(e => e.Objects)
                .WithOne(e => e.Bucket)
                .HasForeignKey(e => e.BucketId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(e => e.Tags)
                .WithOne(e => e.Bucket)
                .HasForeignKey(e => e.BucketId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure StorageObject
        modelBuilder.Entity<StorageObject>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ObjectId).IsUnique();
            entity.HasIndex(e => e.ObjectKey);
            entity.HasIndex(e => new { e.BucketId, e.ObjectKey });
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasMany(e => e.Tags)
                .WithOne(e => e.Object)
                .HasForeignKey(e => e.ObjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Configure BucketTag
        modelBuilder.Entity<BucketTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BucketId, e.Key }).IsUnique();
        });

        // Configure ObjectTag
        modelBuilder.Entity<ObjectTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ObjectId, e.Key }).IsUnique();
        });
    }
}
