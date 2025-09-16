namespace Credo.FileStorage.Worker.LegacyFetcher;

public interface IMigrationRepository
{
    Task<long?> GetResumeCursorAsync(CancellationToken ct); // returns MAX(DocumentId) where Status=2 (or last inserted)
    Task UpsertPendingAsync(IEnumerable<(long id, RouteInfo route)> items, CancellationToken ct);
    Task MarkProcessingAsync(long id, CancellationToken ct);
    Task MarkDoneAsync(long id, string bucket, string key, long size, string contentType, CancellationToken ct);
    Task MarkFailedAsync(long id, string error, CancellationToken ct);
}