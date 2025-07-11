using System.Data;
using Dapper;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Repositories;

public class StorageOperationRepository : IStorageOperationRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public StorageOperationRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<StorageOperation> CreateAsync(StorageOperation storageOperation)
    {
        var sql = @"INSERT INTO doc.StorageOperations (Id, Alias, OperationId)
                    VALUES (@Id, @Alias, @OperationId)";
        await _connection.ExecuteAsync(sql, storageOperation, _transaction);
        return storageOperation;
    }

    public async Task<IEnumerable<StorageOperation>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.StorageOperations";
        return await _connection.QueryAsync<StorageOperation>(sql, transaction: _transaction);
    }

    public async Task<StorageOperation> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.StorageOperations WHERE Id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<StorageOperation>(sql, new { Id = id }, _transaction);
    }

    public async Task<StorageOperation> UpdateAsync(StorageOperation storageOperation)
    {
        var sql = @"UPDATE doc.StorageOperations SET
                        Alias = @Alias,
                        OperationId = @OperationId
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, storageOperation, _transaction);
        return storageOperation;
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.StorageOperations WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }

    public async Task<StorageOperation?> GetByAliasAsync(string alias)
    {
        var sql = "SELECT * FROM doc.StorageOperations WHERE Alias = @Alias";
        return await _connection.QuerySingleOrDefaultAsync<StorageOperation>(sql, new { Alias = alias }, _transaction);
    }
} 