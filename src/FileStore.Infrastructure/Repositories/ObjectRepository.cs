using System.Data;
using Dapper;
using FileStore.Core.Enums;
using FileStore.Core.Interfaces;
using FileStore.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;

namespace FileStore.Infrastructure.Repositories;

public class ObjectRepository : IObjectRepository
{
    private readonly string _connectionString;

    public ObjectRepository(IOptions<DatabaseOptions> options)
    {
        _connectionString = options.Value.ConnectionString;
    }

    private IDbConnection CreateConnection() => new SqlConnection(_connectionString);

    public async Task<StoredObject?> GetByIdAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT ObjectId, Bucket, RelativePath, Tier, Length, ContentType, Hash, CreatedAt, LastAccessedAt, Tags
            FROM StoredObjects
            WHERE ObjectId = @ObjectId";

        return await connection.QuerySingleOrDefaultAsync<StoredObject>(
            new CommandDefinition(sql, new { ObjectId = objectId }, cancellationToken: cancellationToken));
    }

    public async Task<StoredObject?> GetByBucketAndIdAsync(string bucket, Guid objectId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT ObjectId, Bucket, RelativePath, Tier, Length, ContentType, Hash, CreatedAt, LastAccessedAt, Tags
            FROM StoredObjects
            WHERE Bucket = @Bucket AND ObjectId = @ObjectId";

        return await connection.QuerySingleOrDefaultAsync<StoredObject>(
            new CommandDefinition(sql, new { Bucket = bucket, ObjectId = objectId }, cancellationToken: cancellationToken));
    }

    public async Task CreateAsync(StoredObject obj, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            INSERT INTO StoredObjects (ObjectId, Bucket, RelativePath, Tier, Length, ContentType, Hash, CreatedAt, LastAccessedAt, Tags)
            VALUES (@ObjectId, @Bucket, @RelativePath, @Tier, @Length, @ContentType, @Hash, @CreatedAt, @LastAccessedAt, @Tags)";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, obj, cancellationToken: cancellationToken));
    }

    public async Task UpdateLastAccessedAsync(Guid objectId, DateTime accessedAt, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE StoredObjects
            SET LastAccessedAt = @AccessedAt
            WHERE ObjectId = @ObjectId";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ObjectId = objectId, AccessedAt = accessedAt }, cancellationToken: cancellationToken));
    }

    public async Task UpdateTierAsync(Guid objectId, StorageTier tier, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            UPDATE StoredObjects
            SET Tier = @Tier
            WHERE ObjectId = @ObjectId";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ObjectId = objectId, Tier = tier }, cancellationToken: cancellationToken));
    }

    public async Task DeleteAsync(Guid objectId, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = "DELETE FROM StoredObjects WHERE ObjectId = @ObjectId";

        await connection.ExecuteAsync(
            new CommandDefinition(sql, new { ObjectId = objectId }, cancellationToken: cancellationToken));
    }

    public async Task<List<StoredObject>> ListByBucketAsync(string bucket, string? prefix, int skip, int take, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var sql = @"
            SELECT ObjectId, Bucket, RelativePath, Tier, Length, ContentType, Hash, CreatedAt, LastAccessedAt, Tags
            FROM StoredObjects
            WHERE Bucket = @Bucket
            ORDER BY ObjectId
            OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY";

        var results = await connection.QueryAsync<StoredObject>(
            new CommandDefinition(sql, new { Bucket = bucket, Skip = skip, Take = take }, cancellationToken: cancellationToken));

        return results.ToList();
    }

    public async Task<List<StoredObject>> GetObjectsForTieringAsync(int coldAfterDays, List<string> excludedBuckets, int batchSize, CancellationToken cancellationToken = default)
    {
        using var connection = CreateConnection();
        var cutoffDate = DateTime.UtcNow.AddDays(-coldAfterDays);

        var sql = @"
            SELECT TOP (@BatchSize) ObjectId, Bucket, RelativePath, Tier, Length, ContentType, Hash, CreatedAt, LastAccessedAt, Tags
            FROM StoredObjects
            WHERE Tier = 0
              AND (LastAccessedAt IS NULL OR LastAccessedAt < @CutoffDate)
              AND (CreatedAt < @CutoffDate)";

        if (excludedBuckets.Any())
        {
            sql += " AND Bucket NOT IN @ExcludedBuckets";
        }

        sql += " ORDER BY COALESCE(LastAccessedAt, CreatedAt)";

        var results = await connection.QueryAsync<StoredObject>(
            new CommandDefinition(sql, new { BatchSize = batchSize, CutoffDate = cutoffDate, ExcludedBuckets = excludedBuckets }, cancellationToken: cancellationToken));

        return results.ToList();
    }
}

public class DatabaseOptions
{
    public string ConnectionString { get; set; } = string.Empty;
}
