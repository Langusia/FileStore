using System.Data;
using Credo.Core.FileStorage.Models;
using Dapper;

namespace Credo.Core.FileStorage.Repositories;

public class OperationRepository : IOperationRepository
{
    private readonly IDbConnection _connection;
    private readonly IDbTransaction _transaction;

    public OperationRepository(IDbConnection connection, IDbTransaction transaction)
    {
        _connection = connection;
        _transaction = transaction;
    }

    public async Task<Operation> CreateAsync(Operation operation)
    {
        var sql = @"INSERT INTO doc.Operations (Id, Alias, Name, DictionaryId)
                    VALUES (@Id, @Alias, @Name, @DictionaryId)";
        await _connection.ExecuteAsync(sql, operation, _transaction);
        return operation;
    }

    public async Task<IEnumerable<Operation>> GetAllAsync()
    {
        var sql = "SELECT * FROM doc.Operations";
        return await _connection.QueryAsync<Operation>(sql, transaction: _transaction);
    }

    public async Task<Operation> GetByIdAsync(Guid id)
    {
        var sql = "SELECT * FROM doc.Operations WHERE Id = @Id";
        return await _connection.QuerySingleOrDefaultAsync<Operation>(sql, new { Id = id }, _transaction);
    }

    public async Task<Operation> UpdateAsync(Operation operation)
    {
        var sql = @"UPDATE doc.Operations SET
                        Alias = @Alias,
                        Name = @Name,
                        DictionaryId = @DictionaryId
                    WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, operation, _transaction);
        return operation;
    }

    public async Task DeleteAsync(Guid id)
    {
        var sql = "DELETE FROM doc.Operations WHERE Id = @Id";
        await _connection.ExecuteAsync(sql, new { Id = id }, _transaction);
    }

    public async Task<Operation?> GetByAliasAsync(string alias)
    {
        var sql = "SELECT * FROM doc.Operations WHERE Alias = @Alias";
        return await _connection.QuerySingleOrDefaultAsync<Operation>(sql, new { Alias = alias }, _transaction);
    }
}