namespace Credo.Core.FileStorage.Models.Upload;

public sealed record DocumentCreate(
    Guid ChannelOperationBucketId,
    string Name,
    string Address, // S3 object key,
    string Key,
    long Size,
    short Type // your SMALLINT code
);