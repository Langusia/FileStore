namespace FileStore.Storage.Models;

/// <summary>
/// Response model for retrieving an object from storage.
/// </summary>
public class GetObjectResponse
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public required Stream Content { get; set; }
    public string? ContentType { get; set; }
    public long? SizeInBytes { get; set; }
    public string? ETag { get; set; }
    public DateTime? LastModified { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

/// <summary>
/// Response model for delete operations.
/// </summary>
public class DeleteResponse
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public bool Success { get; set; }
    public DateTime DeletedAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Response model for object metadata queries.
/// </summary>
public class ObjectMetadataResponse
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public required string OriginalFileName { get; set; }
    public required string FullStorageUrl { get; set; }
    public string? ContentType { get; set; }
    public long? SizeInBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastModifiedAt { get; set; }
    public DateTime? LastAccessedAt { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}
