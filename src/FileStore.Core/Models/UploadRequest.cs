namespace FileStore.Core.Models;

public class UploadRequest
{
    public string Bucket { get; set; } = string.Empty;
    public Stream FileStream { get; set; } = Stream.Null;
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/octet-stream";
    public string Channel { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? BusinessEntityId { get; set; }
    public Dictionary<string, string>? Tags { get; set; }
}
