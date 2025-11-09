using System.ComponentModel.DataAnnotations;

namespace FileStore.API.DTOs;

/// <summary>
/// DTO for file upload via multipart form data.
/// </summary>
public class UploadRequestDto
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required]
    [Range(1, 7)]
    public int Channel { get; set; }

    [Required]
    [Range(1, 9)]
    public int Operation { get; set; }

    public Dictionary<string, string>? Metadata { get; set; }

    public bool TrackSize { get; set; } = true;
}

/// <summary>
/// Response DTO for upload operations.
/// </summary>
public class UploadResponseDto
{
    public required string ObjectId { get; set; }
    public required string ObjectKey { get; set; }
    public required string BucketName { get; set; }
    public required string FullStorageUrl { get; set; }
    public long? SizeInBytes { get; set; }
    public string? ETag { get; set; }
    public DateTime UploadedAt { get; set; }
}
