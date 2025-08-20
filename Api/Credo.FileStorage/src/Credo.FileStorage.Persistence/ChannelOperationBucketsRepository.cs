using System.Data.SqlClient;
using Credo.FileStorage.Domain.Interfaces;
using Credo.FileStorage.Domain.Models;
using Credo.FileStorage.Domain.Settings;
using Dapper;
using Microsoft.Extensions.Options;

namespace Credo.FileStorage.Persistence;

public sealed class ChannelOperationBucketsRepository(IOptions<ConnectionStrings> cs) : IChannelOperationBucketsRepository
{
    private readonly string _connectionString = cs.Value.DocumentDb;

    public async Task<List<ChannelOperationBucket>> GetAll(CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        var res = await con.QueryAsync<ChannelOperationBucket>("SELECT Id, ChannelId, OperationId, BucketId FROM doc.ChannelOperationBuckets ORDER BY Id");
        return res.ToList();
    }

    public async Task<ChannelOperationBucket?> Get(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        return await con.QuerySingleOrDefaultAsync<ChannelOperationBucket>("SELECT Id, ChannelId, OperationId, BucketId FROM doc.ChannelOperationBuckets WHERE Id=@id", new { id });
    }

    public async Task<ChannelOperationBucket> Create(ChannelOperationBucket binding, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        if (binding.Id == Guid.Empty) binding.Id = Guid.NewGuid();
        await con.ExecuteAsync("INSERT INTO doc.ChannelOperationBuckets (Id, ChannelId, OperationId, BucketId) VALUES (@Id, @ChannelId, @OperationId, @BucketId)", binding);
        return binding;
    }

    public async Task Delete(Guid id, CancellationToken cancellationToken)
    {
        await using var con = new SqlConnection(_connectionString);
        await con.OpenAsync(cancellationToken);
        await con.ExecuteAsync("DELETE FROM doc.ChannelOperationBuckets WHERE Id=@id", new { id });
    }
}


