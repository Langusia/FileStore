using System.Data;
using System.Data.SqlClient;
using Dapper;

namespace Credo.FileStorage.Worker.LegacyFetcher;

public sealed class SqlMigrationRepository : IMigrationRepository
{
    private readonly string _cs;
    public SqlMigrationRepository(string connectionString) => _cs = connectionString;

    public async Task<long?> GetResumeCursorAsync(CancellationToken ct)
    {
        await using var con = new SqlConnection(_cs);
        const string sql = @"SELECT MAX(DocumentId) FROM dbo.MigItems WHERE Status = 2;";
        return await con.ExecuteScalarAsync<long?>(new CommandDefinition(sql, cancellationToken: ct));
    }

    public async Task UpsertPendingAsync(IEnumerable<(long id, RouteInfo route)> items, CancellationToken ct)
    {
        // Use TVP dbo.MigPending (create once). If you don't have it, see comment below for a per-row fallback.
        var tvp = new DataTable();
        tvp.Columns.Add("DocumentId", typeof(long));
        tvp.Columns.Add("ChannelId", typeof(int));
        tvp.Columns.Add("OperationId", typeof(int));
        tvp.Columns.Add("Bucket", typeof(string));
        foreach (var (id, route) in items)
            tvp.Rows.Add(id, (object?)route.ChannelId ?? DBNull.Value, (object?)route.OperationId ?? DBNull.Value, route.Bucket);

        await using var con = new SqlConnection(_cs);
        var cmd = new CommandDefinition(@"
;WITH src AS (
    SELECT DocumentId, ChannelId, OperationId, Bucket
    FROM @Items
)
MERGE dbo.MigItems AS t
USING src AS s
  ON t.DocumentId = s.DocumentId
WHEN NOT MATCHED BY TARGET THEN
  INSERT (DocumentId, Status, ChannelId, OperationId, Bucket)
  VALUES (s.DocumentId, 0, s.ChannelId, s.OperationId, s.Bucket)
;", new { Items = tvp.AsTableValuedParameter("dbo.MigPending") }, cancellationToken: ct);

        await con.ExecuteAsync(cmd);

        /*
        // Fallback (no TVP): per-row IF NOT EXISTS (slower but ok for small batches)
        await using var con = new SqlConnection(_cs);
        foreach (var (id, route) in items)
        {
            await con.ExecuteAsync(@"
IF NOT EXISTS (SELECT 1 FROM dbo.MigItems WHERE DocumentId=@id)
    INSERT INTO dbo.MigItems (DocumentId, Status, ChannelId, OperationId, Bucket)
    VALUES (@id, 0, @ch, @op, @b);",
            new { id, ch = route.ChannelId, op = route.OperationId, b = route.Bucket });
        }
        */
    }

    public async Task MarkProcessingAsync(long id, CancellationToken ct)
    {
        await using var con = new SqlConnection(_cs);
        const string sql = @"
UPDATE dbo.MigItems
   SET Status = 1,
       AttemptCount = AttemptCount + 1,
       LastTriedAtUtc = SYSUTCDATETIME(),
       LastError = NULL
 WHERE DocumentId = @id;";
        await con.ExecuteAsync(new CommandDefinition(sql, new { id }, cancellationToken: ct));
    }

    public async Task MarkDoneAsync(long id, string bucket, string key, long size, string contentType, CancellationToken ct)
    {
        await using var con = new SqlConnection(_cs);
        const string sql = @"
UPDATE dbo.MigItems
   SET Status = 2,
       Bucket = @bucket,
       ObjectKey = @key,
       Size = @size,
       ContentType = @ct,
       LastSucceededAtUtc = SYSUTCDATETIME()
 WHERE DocumentId = @id;";
        await con.ExecuteAsync(new CommandDefinition(sql, new { id, bucket, key, size, ct = contentType }, cancellationToken: ct));
    }

    public async Task MarkFailedAsync(long id, string error, CancellationToken ct)
    {
        await using var con = new SqlConnection(_cs);
        const string sql = @"
UPDATE dbo.MigItems
   SET Status = 3,
       LastError = @err,
       LastTriedAtUtc = SYSUTCDATETIME()
 WHERE DocumentId = @id;";
        await con.ExecuteAsync(new CommandDefinition(sql, new { id, err = error }, cancellationToken: ct));
    }
}