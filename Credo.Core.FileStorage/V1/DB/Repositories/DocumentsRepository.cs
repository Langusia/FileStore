using Credo.Core.FileStorage.V1.DB.Models;
using Credo.Core.FileStorage.V1.Entities;
using Dapper;

namespace Credo.Core.FileStorage.V1.DB.Repositories;

public sealed class DocumentsRepository : IDocumentsRepository
{
    private readonly IDbConnectionFactory _dbf;
    public DocumentsRepository(IDbConnectionFactory dbf) => _dbf = dbf;

    public async Task<Guid> InsertAsync(DocumentCreate create, CancellationToken ct = default)
    {
        await using var con = await _dbf.OpenAsync(ct);
        await using var tx = await con.BeginTransactionAsync(ct);

        const string sql = @"
                    INSERT INTO doc.Documents (ChannelOperationBucketId, Name, Address, Size, Type)
                    OUTPUT inserted.Id
                    VALUES (@ChannelOperationBucketId, @Name, @Address, @Size, @Type);";

        var id = await con.ExecuteScalarAsync<Guid>(sql, new
        {
            create.ChannelOperationBucketId,
            create.Name,
            create.Address,
            create.Size,
            create.Type
        }, tx);

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<Document?> TryGetAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await _dbf.OpenAsync(ct);
        const string sql = @"SELECT Id, ChannelOperationBucketId, Name, Address, Size, Type, CreatedAt
                             FROM doc.Documents WHERE Id = @id;";
        return await con.QueryFirstOrDefaultAsync<Document>(sql, new { id });
    }
}