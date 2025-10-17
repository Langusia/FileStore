using Credo.Core.FileStorage.DB.Entities;
using Dapper;

namespace Credo.Core.FileStorage.DB.Repositories;

public sealed class ChannelOperationBindingsRepository(IDbConnectionFactory dbf) : IChannelOperationBindingsRepository
{
    public async Task<IEnumerable<ChannelOperationBucket>> GetAllAsync(CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = @"SELECT cob.Id, cob.ChannelId, cob.OperationId, cob.BucketId FROM doc.ChannelOperationBuckets cob";
        return await con.QueryAsync<ChannelOperationBucket>(sql);
    }

    public async Task<ChannelOperationBucket?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = @"SELECT cob.Id, cob.ChannelId, cob.OperationId, cob.BucketId FROM doc.ChannelOperationBuckets cob WHERE cob.Id=@id";
        return await con.QuerySingleOrDefaultAsync<ChannelOperationBucket>(sql, new { id });
    }

    public async Task<ChannelOperationBucket> CreateAsync(Guid channelId, Guid operationId, Guid bucketId, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = @"INSERT INTO doc.ChannelOperationBuckets (Id, ChannelId, OperationId, BucketId) VALUES (@Id, @ChannelId, @OperationId, @BucketId)";
        var cob = new ChannelOperationBucket { Id = Guid.NewGuid(), ChannelId = channelId, OperationId = operationId, BucketId = bucketId };
        await con.ExecuteAsync(sql, cob);
        return cob;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = @"DELETE FROM doc.ChannelOperationBuckets WHERE Id=@id";
        await con.ExecuteAsync(sql, new { id });
    }
}




