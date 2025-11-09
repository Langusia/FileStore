using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FileStore.Storage.Models;

/// <summary>
/// Represents tags for storage objects. Used for lifecycle policies, categorization, etc.
/// This is part of Scope 2 for hot/cold storage transitions and TTLs.
/// </summary>
[Table("ObjectTags")]
public class ObjectTag
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public long Id { get; set; }

    [Required]
    public long ObjectId { get; set; }

    [Required]
    [MaxLength(128)]
    public string Key { get; set; } = string.Empty;

    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the storage object.
    /// </summary>
    [ForeignKey(nameof(ObjectId))]
    public virtual StorageObject Object { get; set; } = null!;
}
