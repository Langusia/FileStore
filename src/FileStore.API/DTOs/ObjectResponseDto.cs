namespace FileStore.API.DTOs;

/// <summary>
/// DTO for object metadata responses.
/// </summary>
public class ObjectMetadataDto
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

/// <summary>
/// DTO for delete operation responses.
/// </summary>
public class DeleteResponseDto
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public bool Success { get; set; }
    public DateTime DeletedAt { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// DTO for list objects request.
/// </summary>
public class ListObjectsRequestDto
{
    public int? Channel { get; set; }
    public int? Operation { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// DTO for paginated list responses.
/// </summary>
public class PaginatedResponseDto<T>
{
    public required List<T> Items { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
}
