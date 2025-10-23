using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.Storage;

/// <summary>
/// Context object that carries all upload-related data through the pipeline
/// </summary>
internal sealed record UploadContext(
    string BucketName,
    string ObjectKey,
    string DocumentName,
    string ContentType,
    short TypeCode,
    long Size,
    Stream Stream,
    bool IsTempStream,
    Guid ChannelOperationBucketId,
    DateTime UploadedAtUtc,
    UploadFile OriginalFile
);