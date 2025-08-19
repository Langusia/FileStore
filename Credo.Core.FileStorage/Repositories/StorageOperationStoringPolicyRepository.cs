using System.Data;
using Dapper;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Repositories;

public class StorageOperationStoringPolicyRepository : IStorageOperationStoringPolicyRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public StorageOperationStoringPolicyRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public Task<StorageOperationStoringPolicy> CreateAsync(StorageOperationStoringPolicy storageOperationStoringPolicy)
    {
        var sql = @"INSERT INTO doc.StorageOperationStoringPolicies (Id, StorageOperationId, Name, TransitionInDays, ExpirationInDays)
                    VALUES (@Id, @StorageOperationId, @Name, @TransitionInDays, @ExpirationInDays)";
        return _connection.ExecuteAsync(sql, storageOperationStoringPolicy, _transaction)
            .ContinueWith(_ => storageOperationStoringPolicy);
    }

    public Task<IEnumerable<StorageOperationStoringPolicy>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.StorageOperationStoringPolicies";
        return _connection.QueryAsync<StorageOperationStoringPolicy>(sql, transaction: _transaction);
    }

    public Task<StorageOperationStoringPolicy> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.StorageOperationStoringPolicies WHERE Id = @Id";
        return _connection.QuerySingleOrDefaultAsync<StorageOperationStoringPolicy>(sql, new { Id = id }, _transaction);
    }

    public Task<StorageOperationStoringPolicy> UpdateAsync(StorageOperationStoringPolicy storageOperationStoringPolicy)
    {
        var sql = @"UPDATE doc.StorageOperationStoringPolicies SET
                        StorageOperationId = @StorageOperationId,
                        Name = @Name,
                        TransitionInDays = @TransitionInDays,
                        ExpirationInDays = @ExpirationInDays
                    WHERE Id = @Id";
        return _connection.ExecuteAsync(sql, storageOperationStoringPolicy, _transaction)
            .ContinueWith(_ => storageOperationStoringPolicy);
    }

    public Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.StorageOperationStoringPolicies WHERE Id = @Id";
        return _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }

    public Task<StorageOperationStoringPolicy?> GetByStorageOperationIdAsync(Guid storageOperationId)
    {
        var sql = "SELECT * FROM doc.StorageOperationStoringPolicies WHERE StorageOperationId = @StorageOperationId";
        return _connection.QuerySingleOrDefaultAsync<StorageOperationStoringPolicy>(sql, new { StorageOperationId = storageOperationId }, _transaction);
    }
}
