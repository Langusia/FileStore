using Credo.Core.FileStorage.DB.Entities;
using Dapper;

namespace Credo.Core.FileStorage.DB.Repositories;

public sealed class OperationsAdminRepository(IDbConnectionFactory dbf) : IOperationsAdminRepository
{
    public async Task<IEnumerable<Operation>> GetAllAsync(CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Operations ORDER BY Alias";
        return await con.QueryAsync<Operation>(sql);
    }

    public async Task<Operation?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "SELECT Id, Alias, ExternalAlias, ExternalId FROM doc.Operations WHERE Id=@id";
        return await con.QuerySingleOrDefaultAsync<Operation>(sql, new { id });
    }

    public async Task<Operation> CreateAsync(Operation operation, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "INSERT INTO doc.Operations (Id, Alias, ExternalAlias, ExternalId) VALUES (@Id, @Alias, @ExternalAlias, @ExternalId)";
        if (operation.Id == Guid.Empty) operation.Id = Guid.NewGuid();
        await con.ExecuteAsync(sql, operation);
        return operation;
    }

    public async Task<Operation> UpdateAsync(Operation operation, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "UPDATE doc.Operations SET Alias=@Alias, ExternalAlias=@ExternalAlias, ExternalId=@ExternalId WHERE Id=@Id";
        await con.ExecuteAsync(sql, operation);
        return operation;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await dbf.OpenAsync(ct);
        const string sql = "DELETE FROM doc.Operations WHERE Id=@id";
        await con.ExecuteAsync(sql, new { id });
    }
}


