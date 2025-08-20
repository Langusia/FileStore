namespace Credo.Core.FileStorage.Models.Upload;

public sealed record UploadOptions(string? LogicalName = null, string? ObjectKeyPrefix = null);

public sealed record UploadResult(
    Guid DocumentId,
    string Bucket,
    string ObjectKey,
    string Name,
    long Size,
    short Type,
    DateTime UploadedAtUtc
);