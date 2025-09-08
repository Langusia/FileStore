using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Models.Upload;
using Dapper;

namespace Credo.Core.FileStorage.DB.Repositories;

public sealed class DocumentsRepository : IDocumentsRepository
{
    private readonly IDbConnectionFactory _dbf;
    public DocumentsRepository(IDbConnectionFactory dbf) => _dbf = dbf;

    private const string sql = @"SELECT
                                 -- document (first object)
                                 d.Id,
                                 d.ChannelOperationBucketId,
                                 d.Name,
                                 d.Address,
                                 d.Size,
                                 d.Type,
                                 d.UploadedAt,
                                 d.[Key],
                                 -- channelOperationBucket (second object)
                                 cob.Id,             -- 👈 split here
                                 cob.ChannelId,
                                 cob.OperationId,
                                 cob.BucketId,
                             
                                 -- channel (third object)
                                 c.Id,               -- 👈 split here
                                 c.Alias,
                                 c.ExternalAlias,
                                 c.ExternalId,
                             
                                 -- operation (fourth object)
                                 o.Id,               -- 👈 split here
                                 o.Alias,
                                 o.ExternalAlias,
                                 o.ExternalId,
                             
                                 -- bucket (fifth object)
                                 b.Id,               -- 👈 split here
                                 b.Name
                             FROM doc.Documents d
                             JOIN doc.ChannelOperationBuckets cob ON cob.Id = d.ChannelOperationBucketId
                             JOIN doc.Channels   c ON c.Id = cob.ChannelId
                             JOIN doc.Operations o ON o.Id = cob.OperationId
                             JOIN doc.Buckets    b ON b.Id = cob.BucketId";

    public async Task<Guid> InsertAsync(DocumentCreate create, CancellationToken ct = default)
    {
        await using var con = await _dbf.OpenAsync(ct);
        await using var tx = await con.BeginTransactionAsync(ct);

        const string sql = @"
                    INSERT INTO doc.Documents (ChannelOperationBucketId, Name, Address, Size, Type, [Key])
                    OUTPUT inserted.Id
                    VALUES (@ChannelOperationBucketId, @Name, @Address, @Size, @Type, @Key);";

        var id = await con.ExecuteScalarAsync<Guid>(sql, new
        {
            create.ChannelOperationBucketId,
            create.Name,
            create.Address,
            create.Size,
            create.Type,
            create.Key
        }, tx);

        await tx.CommitAsync(ct);
        return id;
    }

    public async Task<Document?> TryGetAsync(Guid id, CancellationToken ct = default)
    {
        await using var con = await _dbf.OpenAsync(ct);

        var select = sql + " WHERE d.Id = @id;";

        var result = await con.QueryAsync<Document, ChannelOperationBucket, Channel, Operation, Bucket, Document>(
            select,
            (doc, cob, ch, op, b) =>
            {
                cob.Channel = ch;
                cob.Operation = op;
                cob.Bucket = b;
                doc.ChannelOperationBucket = cob;
                return doc;
            },
            new { id });

        return result.FirstOrDefault();
    }

    public async Task<Document?> TryGetAsync(string bucket, string objKey, CancellationToken ct = default)
    {
        await using var con = await _dbf.OpenAsync(ct);
        var address = $"{bucket}/{objKey}";
        var select = sql + " WHERE d.Address = @address;";

        var result = await con.QueryAsync<Document, ChannelOperationBucket, Channel, Operation, Bucket, Document>(
            select,
            (doc, cob, ch, op, b) =>
            {
                cob.Channel = ch;
                cob.Operation = op;
                cob.Bucket = b;
                doc.ChannelOperationBucket = cob;
                return doc;
            },
            new { address });

        return result.FirstOrDefault();
    }
}