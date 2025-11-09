using FileStore.Storage.Enums;

namespace FileStore.Storage.Models;

/// <summary>
/// Request model for uploading an object to storage.
/// </summary>
public class UploadRequest
{
    public required Stream Content { get; set; }
    public required string FileName { get; set; }
    public required Channel Channel { get; set; }
    public required Operation Operation { get; set; }
    public string? ContentType { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
    public bool TrackSize { get; set; } = true;
}

/// <summary>
/// Response model for upload operations.
/// </summary>
public class UploadResponse
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public required string FullStorageUrl { get; set; }
    public long? SizeInBytes { get; set; }
    public string? ETag { get; set; }
    public DateTime UploadedAt { get; set; }
}
