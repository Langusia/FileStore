namespace FileStore.Core.Models;

public class ListObjectsResult
{
    public string Bucket { get; set; } = string.Empty;
    public List<ObjectMetadata> Objects { get; set; } = new();
    public string? NextContinuationToken { get; set; }
    public bool IsTruncated { get; set; }
}
