namespace FileStore.Core.Models;

public class ListObjectsRequest
{
    public string Bucket { get; set; } = string.Empty;
    public string? Prefix { get; set; }
    public string? ContinuationToken { get; set; }
    public int MaxKeys { get; set; } = 1000;
}
