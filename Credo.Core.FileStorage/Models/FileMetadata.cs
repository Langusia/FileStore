namespace Credo.Core.FileStorage.Models;

public record FileMetadata(
    Guid Id,
    Guid BucketMetadataId,
    string FileName,
    long Size,
    string ContentType,
    DateTime UploadedAt
); 