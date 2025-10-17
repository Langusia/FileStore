using Credo.Core.FileStorage.DB;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Credo.FileStorage.Worker.LegacyFetcher;

public sealed class LegacyRepository : ILegacyRepository
{
    private readonly IDbConnectionFactory _dbFactory;
    private readonly ILogger<LegacyRepository> _logger;

    public LegacyRepository(IDbConnectionFactory dbFactory, ILogger<LegacyRepository> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<LegacyId>> FetchIdsPageAsync(long afterId, int take, CancellationToken ct)
    {
        _logger.LogDebug("Fetching legacy IDs after {AfterId}, taking {Take}", afterId, take);

        await using var conn = await _dbFactory.OpenAsync(ct);
        
        const string sql = @"
            SELECT DocumentID 
            FROM Documents 
            WHERE DocumentID > @AfterId 
                AND DelStatus = 0
                AND Documents IS NOT NULL
            ORDER BY DocumentID 
            OFFSET 0 ROWS FETCH NEXT @Take ROWS ONLY";

        try
        {
            var ids = await conn.QueryAsync<long>(sql, new { AfterId = afterId, Take = take });
            var result = ids.Select(id => new LegacyId(id)).ToList();
            
            _logger.LogDebug("Fetched {Count} legacy IDs", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch legacy IDs after {AfterId}", afterId);
            throw;
        }
    }

    public async Task<RouteInfo> ResolveRouteAsync(long id, CancellationToken ct)
    {
        _logger.LogDebug("Resolving route for legacy ID {Id}", id);

        await using var conn = await _dbFactory.OpenAsync(ct);
        
        const string sql = @"
            SELECT 
                ChannelId,
                OperationId
            FROM Documents 
            WHERE DocumentID = @Id";

        try
        {
            var route = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            
            if (route == null)
            {
                throw new InvalidOperationException($"Document with ID {id} not found");
            }

            // Determine bucket name based on channel/operation or use default
            var bucketName = DetermineBucketName(route.ChannelId, route.OperationId);

            var routeInfo = new RouteInfo(
                ChannelId: route.ChannelId,
                OperationId: route.OperationId,
                Bucket: bucketName
            );

            _logger.LogDebug("Resolved route for ID {Id}: Channel={ChannelId}, Operation={OperationId}, Bucket={Bucket}", 
                id, routeInfo.ChannelId, routeInfo.OperationId, routeInfo.Bucket);
            
            return routeInfo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve route for legacy ID {Id}", id);
            throw;
        }
    }

    public async Task<LegacyMeta> ReadMetaAsync(long id, CancellationToken ct)
    {
        _logger.LogDebug("Reading metadata for legacy ID {Id}", id);

        await using var conn = await _dbFactory.OpenAsync(ct);
        
        const string sql = @"
            SELECT 
                FileSize,
                DocumentTypeID,
                DocumentName,
                DocumentExt,
                ContentType,
                TableID,
                TableRecordID,
                UserID,
                RecordDate,
                DelStatus,
                StorageId,
                ContentId
            FROM Documents 
            WHERE DocumentID = @Id";

        try
        {
            var meta = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new { Id = id });
            
            if (meta == null)
            {
                throw new InvalidOperationException($"Document with ID {id} not found");
            }

            var legacyMeta = new LegacyMeta(
                FileSize: meta.FileSize ?? 0,
                DocumentTypeID: meta.DocumentTypeID,
                DocumentName: meta.DocumentName ?? $"document_{id}",
                DocumentExt: meta.DocumentExt,
                ContentType: meta.ContentType,
                TableID: meta.TableID,
                TableRecordID: meta.TableRecordID,
                UserID: meta.UserID,
                RecordDate: meta.RecordDate ?? DateTime.Now,
                DelStatus: meta.DelStatus ?? false,
                StorageId: meta.StorageId,
                ContentId: meta.ContentId
            );

            _logger.LogDebug("Read metadata for ID {Id}: Size={Size}, TypeID={TypeID}, Name={Name}", 
                id, legacyMeta.FileSize, legacyMeta.DocumentTypeID, legacyMeta.DocumentName);
            
            return legacyMeta;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read metadata for legacy ID {Id}", id);
            throw;
        }
    }

    public async Task<Stream> ReadBlobAsync(long id, CancellationToken ct)
    {
        _logger.LogDebug("Reading blob data for legacy ID {Id}", id);

        await using var conn = await _dbFactory.OpenAsync(ct);
        
        // Read from the Documents column (VARBINARY(MAX))
        const string sql = @"
            SELECT Documents 
            FROM Documents 
            WHERE DocumentID = @Id";

        try
        {
            var blobData = await conn.QueryFirstOrDefaultAsync<byte[]>(sql, new { Id = id });
            
            if (blobData == null)
            {
                throw new InvalidOperationException($"Blob data for document with ID {id} not found");
            }

            var stream = new MemoryStream(blobData);
            
            _logger.LogDebug("Read blob data for ID {Id}: {Size} bytes", id, blobData.Length);
            
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read blob data for legacy ID {Id}", id);
            throw;
        }
    }

    private static string DetermineBucketName(int? channelId, long? operationId)
    {
        // You can customize this logic based on your business rules
        // For now, using a simple pattern
        return channelId switch
        {
            1 => "channel-1-documents",
            2 => "channel-2-documents", 
            3 => "channel-3-documents",
            _ => $"default-channel-{channelId ?? 0}-documents"
        };
    }
}
