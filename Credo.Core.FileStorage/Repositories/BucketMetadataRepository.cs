using System.Data;
using Credo.Core.FileStorage.Models;
using Dapper;

namespace Credo.Core.FileStorage.Repositories;

public class BucketMetadataRepository : IBucketMetadataRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public BucketMetadataRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }


    public async Task<BucketMetadata> CreateAsync(BucketMetadata bucket)
    {
        var sql = @"INSERT INTO doc.BucketMetadatas (Id, Name, ChannelId, StorageOperationId)
                    VALUES (@Id, @Name, @ChannelId, @StorageOperationId)";
        await _connection.ExecuteAsync(sql, bucket, _transaction);
        return bucket;
    }

    public async Task<IEnumerable<BucketMetadata>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.BucketMetadatas";
        return await _connection.QueryAsync<BucketMetadata>(sql, transaction: _transaction);
    }

    public async Task<BucketMetadata> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.BucketMetadatas WHERE Id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<BucketMetadata>(sql, new { Id = id }, _transaction);
    }

    public async Task<BucketMetadata?> GetByAliasAsync(string alias)
    {
        var sql = "SELECT * FROM doc.BucketMetadatas WHERE Name = @alias";
        return await _connection.QuerySingleOrDefaultAsync<BucketMetadata>(sql, new { Alias = alias }, _transaction);
    }

    public async Task<BucketMetadata> UpdateAsync(BucketMetadata bucket)
    {
        var sql = @"UPDATE doc.BucketMetadatas SET
                        Name = @Name,
                        ChannelId = @ChannelId,
                        StorageOperationId = @StorageOperationId
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, bucket, _transaction);
        return bucket;
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.BucketMetadatas WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }
}