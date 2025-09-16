namespace Credo.FileStorage.Worker.LegacyFetcher;

public sealed record LegacyId(long DocumentID);

public sealed record RouteInfo( // channel-operation mapping
    int? ChannelId,
    long? OperationId,
    string Bucket // resolved destination bucket
);

public sealed record LegacyMeta( // minimal meta about the blob
    long FileSize,
    int? DocumentTypeID,
    string DocumentName,
    string? DocumentExt,
    string? ContentType,
    int? TableID,
    long? TableRecordID,
    int? UserID,
    DateTime RecordDate,
    bool DelStatus,
    string? StorageId,
    long? ContentId
);

public interface ILegacyRepository
{
    Task<IReadOnlyList<LegacyId>> FetchIdsPageAsync(long afterId, int take, CancellationToken ct);
    Task<RouteInfo> ResolveRouteAsync(long id, CancellationToken ct); // determine channel/operation/bucket
    Task<LegacyMeta> ReadMetaAsync(long id, CancellationToken ct);
    Task<Stream> ReadBlobAsync(long id, CancellationToken ct); // returns seekable or forward-only stream
}

