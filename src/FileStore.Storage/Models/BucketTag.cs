using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStore.Storage.Models;

/// <summary>
/// Represents tags for buckets. Used for lifecycle policies, categorization, etc.
/// This is part of Scope 2 for hot/cold storage transitions and TTLs.
/// </summary>
[Table("BucketTags")]
public class BucketTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int BucketId { get; set; }

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the bucket.
    /// </summary>
    [ForeignKey(nameof(BucketId))]
    public virtual StorageBucket Bucket { get; set; } = null!;
}
