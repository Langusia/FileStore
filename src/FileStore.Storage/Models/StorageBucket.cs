using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using FileStore.Storage.Enums;

namespace FileStore.Storage.Models;

/// <summary>
/// Represents a storage bucket metadata in the database.
/// Does NOT contain actual object content.
/// </summary>
[Table("StorageBuckets")]
public class StorageBucket
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string BucketName { get; set; } = string.Empty;

    [Required]
    public int ChannelId { get; set; }

    [Required]
    [MaxLength(50)]
    public string ChannelName { get; set; } = string.Empty;

    [Required]
    public int OperationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string OperationName { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastAccessedAt { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Navigation property to objects stored in this bucket.
    /// </summary>
    public virtual ICollection<StorageObject> Objects { get; set; } = new List<StorageObject>();

    /// <summary>
    /// Navigation property to tags associated with this bucket.
    /// </summary>
    public virtual ICollection<BucketTag> Tags { get; set; } = new List<BucketTag>();
}
