using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStore.Storage.Models;

/// <summary>
/// Represents a storage object metadata in the database.
/// Does NOT contain actual object content - only metadata.
/// </summary>
[Table("StorageObjects")]
public class StorageObject
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    [MaxLength(36)]
    public string ObjectId { get; set; } = Guid.NewGuid().ToString();

    [Required]
    public int BucketId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ObjectKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required]
    [MaxLength(1000)]
    public string FullStorageUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? ContentType { get; set; }

    /// <summary>
    /// Size in bytes. Null if size tracking is disabled for performance.
    /// </summary>
    public long? SizeInBytes { get; set; }

    [MaxLength(64)]
    public string? ETag { get; set; }

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastModifiedAt { get; set; }

    public DateTime? LastAccessedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime? DeletedAt { get; set; }

    [MaxLength(1000)]
    public string? Metadata { get; set; }

    /// <summary>
    /// Navigation property to the bucket.
    /// </summary>
    [ForeignKey(nameof(BucketId))]
    public virtual StorageBucket Bucket { get; set; } = null!;

    /// <summary>
    /// Navigation property to tags associated with this object.
    /// </summary>
    public virtual ICollection<ObjectTag> Tags { get; set; } = new List<ObjectTag>();
}
