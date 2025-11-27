using FileStore.Core.Enums;

namespace FileStore.Core.Models;

public class ObjectMetadata
{
    public Guid ObjectId { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string Hash { get; set; } = string.Empty;
    public StorageTier Tier { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}
