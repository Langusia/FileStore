using System.Text.RegularExpressions;
using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.Exceptions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Credo.Core.FileStorage.DB.Repositories;

public sealed class ChannelOperationBucketRepository(IDbConnectionFactory dbf) : IChannelOperationBucketRepository
{
    // ---------- Get (no side effects) ----------

    public Task<ChannelOperationBucket> GetByAliasesAsync(string channelAlias, string operationAlias, CancellationToken ct = default)
        => QueryRouteAsync("c.Alias = @c AND o.Alias = @o", new { c = channelAlias, o = operationAlias }, ct);

    public Task<ChannelOperationBucket> GetByExternalAliasesAsync(string channelExternalAlias, string operationExternalAlias, CancellationToken ct = default)
        => QueryRouteAsync("c.ExternalAlias = @c AND o.ExternalAlias = @o", new { c = channelExternalAlias, o = operationExternalAlias }, ct);

    public Task<ChannelOperationBucket> GetByExternalIdsAsync(long channelExternalId, long operationExternalId, CancellationToken ct = default)
        => QueryRouteAsync("c.ExternalId = @c AND o.ExternalId = @o", new { c = channelExternalId, o = operationExternalId }, ct);

    public async Task<ChannelOperationBucket> GetByIdAsync(Guid channelOperationBucketId, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        var sql = BaseSelect + " WHERE cob.Id = @id;";
        var item = await MapSingleAsync(con, sql, new { id = channelOperationBucketId });
        return item ?? throw new NotFoundException("ChannelOperationBucket", $"Id={channelOperationBucketId}");
    }

    // ---------- GetOrCreate (side effects) ----------

    /// <summary>
    /// Ensures Channel('default'), Operation('default'), Bucket(normalized) exist and are bound.
    /// Returns the fully populated ChannelOperationBucket.
    /// </summary>
    public async Task<ChannelOperationBucket> GetOrCreateDefaultForBucketAsync(string bucketName, CancellationToken ct = default)
    {
        var normalized = NormalizeBucket(bucketName);

        await using var con = await dbf.OpenAsync(ct);
        await using var tx = await con.BeginTransactionAsync(ct);

        const string selCh = "SELECT Id FROM doc.Channels  WHERE Alias=@a;";
        const string selOp = "SELECT Id FROM doc.Operations WHERE Alias=@a;";
        const string insCh = "INSERT INTO doc.Channels  (Alias, ExternalAlias) OUTPUT inserted.Id VALUES (@a, @a);";
        const string insOp = "INSERT INTO doc.Operations(Alias, ExternalAlias) OUTPUT inserted.Id VALUES (@a, @a);";

        const string selB = "SELECT Id FROM doc.Buckets WHERE Name=@n;";
        const string insB = "INSERT INTO doc.Buckets (Name) OUTPUT inserted.Id VALUES (@n);";

        const string selRt = "SELECT Id FROM doc.ChannelOperationBuckets WHERE OperationId=@op AND ChannelId=@ch AND BucketId=@b;";
        const string insRt = "INSERT INTO doc.ChannelOperationBuckets (OperationId, ChannelId, BucketId) OUTPUT inserted.Id VALUES (@op, @ch, @b);";

        var alias = "default";

        var chId = await con.ExecuteScalarAsync<Guid?>(selCh, new { a = alias }, tx)
                   ?? await InsertWithRetryAsync(con, insCh, new { a = alias }, () => con.ExecuteScalarAsync<Guid>(selCh, new { a = alias }, tx), tx);

        var opId = await con.ExecuteScalarAsync<Guid?>(selOp, new { a = alias }, tx)
                   ?? await InsertWithRetryAsync(con, insOp, new { a = alias }, () => con.ExecuteScalarAsync<Guid>(selOp, new { a = alias }, tx), tx);

        var bId = await con.ExecuteScalarAsync<Guid?>(selB, new { n = normalized }, tx)
                  ?? await InsertWithRetryAsync(con, insB, new { n = normalized }, () => con.ExecuteScalarAsync<Guid>(selB, new { n = normalized }, tx), tx);

        var routeId = await con.ExecuteScalarAsync<Guid?>(selRt, new { op = opId, ch = chId, b = bId }, tx)
                      ?? await InsertWithRetryAsync(con, insRt, new { op = opId, ch = chId, b = bId },
                          () => con.ExecuteScalarAsync<Guid>(selRt, new { op = opId, ch = chId, b = bId }, tx), tx);

        await tx.CommitAsync(ct);

        // Return fully populated object
        var sql = BaseSelect + " WHERE cob.Id = @id;";
        var item = await MapSingleAsync(con, sql, new { id = routeId });
        return item ?? throw new NotFoundException("ChannelOperationBucket", $"Id={routeId}");
    }

    // ---------- Internals ----------

    private async Task<ChannelOperationBucket> QueryRouteAsync(string filter, object param, CancellationToken ct)
    {
        await using var con = await dbf.OpenAsync(ct);
        var sql = BaseSelect + $" WHERE {filter};";
        var item = await MapSingleAsync(con, sql, param);
        return item ?? throw new NotFoundException("ChannelOperationBucket", "No binding for given parameters.");
    }

    // Multi-mapping select
    // splitOn markers must appear in order in SELECT: ChannelId, OperationId, BucketId
    private const string BaseSelect = $@" SELECT
    -- cob (first object)
    cob.Id            AS Id,
    cob.OperationId   AS OperationId,
    cob.ChannelId     AS ChannelId,
    cob.BucketId      AS BucketId,

    -- channel (second object)
    c.Id              AS ChannelId,   -- split marker
    c.Id              AS Id,
    c.Alias           AS Alias,
    c.ExternalAlias   AS ExternalAlias,
    c.ExternalId      AS ExternalId,

    -- operation (third object)
    o.Id              AS OperationId, -- split marker
    o.Id              AS Id,
    o.Alias           AS Alias,
    o.ExternalAlias   AS ExternalAlias,
    o.ExternalId      AS ExternalId,

    -- bucket (fourth object)
    b.Id              AS BucketId,    -- split marker
    b.Id              AS Id,
    b.Name            AS Name
FROM doc.ChannelOperationBuckets cob
JOIN doc.Channels   c ON c.Id = cob.ChannelId
JOIN doc.Operations o ON o.Id = cob.OperationId
JOIN doc.Buckets    b ON b.Id = cob.BucketId";

    private static async Task<ChannelOperationBucket?> MapSingleAsync(SqlConnection con, string sql, object param)
    {
        var rows = await con.QueryAsync<ChannelOperationBucket, Channel, Operation, Bucket, ChannelOperationBucket>(
            sql,
            (cob, ch, op, b) =>
            {
                cob.Channel = ch;
                cob.Operation = op;
                cob.Bucket = b;
                return cob;
            },
            param,
            splitOn: "ChannelId,OperationId,BucketId");

        return rows.FirstOrDefault();
    }

    private static string NormalizeBucket(string name)
    {
        name = name.Trim().ToLowerInvariant();
        name = Regex.Replace(name, @"[^a-z0-9.-]", "-");
        name = Regex.Replace(name, @"(^-+)|(-+$)", "");
        return name;
    }

    /// Executes INSERT and on unique key violation (2627/2601) reselects the existing row.
    private static async Task<Guid> InsertWithRetryAsync(
        SqlConnection con,
        string insertSql,
        object insertParams,
        Func<Task<Guid>> reselect,
        System.Data.IDbTransaction tx)
    {
        try
        {
            return await con.ExecuteScalarAsync<Guid>(insertSql, insertParams, tx);
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601)
        {
            return await reselect();
        }
    }
}