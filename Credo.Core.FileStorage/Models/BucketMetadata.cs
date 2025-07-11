namespace Credo.Core.FileStorage.Models;

public record BucketMetadata(
    Guid Id,
    string Name,
    Guid ChannelId,
    Guid StorageOperationId
); 