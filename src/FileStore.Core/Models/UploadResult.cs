namespace FileStore.Core.Models;

public class UploadResult
{
    public Guid ObjectId { get; set; }
    public string Bucket { get; set; } = string.Empty;
    public long Size { get; set; }
    public string Hash { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
