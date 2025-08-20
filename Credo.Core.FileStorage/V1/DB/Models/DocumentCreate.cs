namespace Credo.Core.FileStorage.V1.DB.Models;

public sealed record DocumentCreate(
    Guid ChannelOperationBucketId,
    string Name,
    string Address,  // S3 object key
    long Size,
    short Type       // your SMALLINT code
);