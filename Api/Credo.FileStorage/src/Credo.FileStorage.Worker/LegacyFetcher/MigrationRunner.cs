using System.Collections.Concurrent;
using System.Globalization;
using Credo.Core.FileStorage.Storage;

namespace Credo.FileStorage.Worker.LegacyFetcher;

public sealed class MigrationRunner
{
    private readonly ILegacyRepository _legacy;
    private readonly IMigrationRepository _repo;
    private readonly IObjectStorage _minio;

    private readonly int _pageSize;
    private readonly int _dop;
    private readonly SemaphoreSlim _readGate; // protect legacy
    private readonly SemaphoreSlim _putGate; // protect MinIO
    private readonly SemaphoreSlim _dbGate; // optional; small cap for mark-done

    public MigrationRunner(
        ILegacyRepository legacy, IMigrationRepository repo, IObjectStorage minio,
        int pageSize = 1000, int dop = 8, int maxConcurrentReads = 3, int maxConcurrentPuts = 6, int maxConcurrentDb = 12)
    {
        _legacy = legacy;
        _repo = repo;
        _minio = minio;
        _pageSize = pageSize;
        _dop = dop;
        _readGate = new SemaphoreSlim(maxConcurrentReads);
        _putGate = new SemaphoreSlim(maxConcurrentPuts);
        _dbGate = new SemaphoreSlim(maxConcurrentDb);
    }

    public async Task RunAsync(CancellationToken ct)
    {
        // resume from latest done; if null, start before the first id
        var resumeFrom = await _repo.GetResumeCursorAsync(ct);
        long cursor = resumeFrom ?? -1;

        var dict = new ConcurrentDictionary<long, RouteInfo>();

        while (!ct.IsCancellationRequested)
        {
            // 1) fetch a page of IDs after cursor
            var page = await _legacy.FetchIdsPageAsync(cursor, _pageSize, ct);
            if (page.Count == 0) break;

            // 2) resolve routes + seed pending
            var pairs = new List<(long id, RouteInfo route)>(page.Count);
            foreach (var item in page)
            {
                var route = await _legacy.ResolveRouteAsync(item.DocumentID, ct);
                pairs.Add((item.DocumentID, route));
                dict.TryAdd(item.DocumentID, route);
            }

            await _repo.UpsertPendingAsync(pairs, ct);

            // 3) drain dict concurrently
            await DrainAsync(dict, ct);

            // 4) advance cursor to last fetched id
            cursor = page[^1].DocumentID;
        }
    }

    private async Task DrainAsync(ConcurrentDictionary<long, RouteInfo> dict, CancellationToken ct)
    {
        // spawn DOP consumer tasks *without* Task.Run ambiguity
        var tasks = Enumerable.Range(0, _dop)
            .Select(_ => ConsumerLoopAsync(dict, ct))
            .ToArray();

        await Task.WhenAll(tasks);
    }

    private async Task ConsumerLoopAsync(ConcurrentDictionary<long, RouteInfo> dict, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            if (!TryTake(dict, out var id, out var route)) break; // dict empty

            try
            {
                await _repo.MarkProcessingAsync(id, ct);

                // 1) read meta + blob (bounded)
                await _readGate.WaitAsync(ct);
                LegacyMeta meta;
                Stream src;
                try
                {
                    meta = await _legacy.ReadMetaAsync(id, ct);
                    src = await _legacy.ReadBlobAsync(id, ct);
                }
                finally
                {
                    _readGate.Release();
                }

                 // 2) prepare destination
                 var (mime, ext) = TypeMap.From(meta.DocumentTypeID, meta.ContentType, meta.DocumentExt);
                 var key = KeyBuilder.Build(id, ext);
                 await _minio.EnsureBucketAsync(route.Bucket, ct);

                 // 3) put object (bounded)
                 await _putGate.WaitAsync(ct);
                 try
                 {
                     await _minio.PutAsync(route.Bucket, key, src, meta.FileSize, mime, ct);
                 }
                 finally
                 {
                     await src.DisposeAsync();
                     _putGate.Release();
                 }

                 // 4) mark done (tiny DB write)
                 await _dbGate.WaitAsync(ct);
                 try
                 {
                     await _repo.MarkDoneAsync(id, route.Bucket, key, meta.FileSize, mime, ct);
                 }
                 finally
                 {
                     _dbGate.Release();
                 }
            }
            catch (Exception ex)
            {
                await _repo.MarkFailedAsync(id, ex.ToString(), ct);
            }
        }
    }

    private static bool TryTake(ConcurrentDictionary<long, RouteInfo> dict, out long id, out RouteInfo route)
    {
        // pop “any” item: try one pass over keys
        foreach (var kv in dict)
        {
            if (dict.TryRemove(kv.Key, out route!))
            {
                id = kv.Key;
                return true;
            }
        }

        id = default;
        route = default!;
        return false;
    }
}