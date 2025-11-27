using FileStore.Core.Enums;

namespace FileStore.Core.Models;

public class StoredObject
{
    public Guid ObjectId { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public StorageTier Tier { get; set; }
    public long Length { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public string? Tags { get; set; }
}
