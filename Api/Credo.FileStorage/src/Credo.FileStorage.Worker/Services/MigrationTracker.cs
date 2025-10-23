using System.Data;
using Credo.FileStorage.Worker.Interfaces;
using Credo.FileStorage.Worker.Models;
using Microsoft.Data.SqlClient;

namespace Credo.FileStorage.Worker.Services;

public class SqlMigrationTracker : IMigrationTracker
{
    private readonly string _connectionString;
    private readonly ILogger<SqlMigrationTracker> _logger;

    public SqlMigrationTracker(string connectionString, ILogger<SqlMigrationTracker> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<long?> GetLastProcessedIdAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);

            // Simple aggregation - optimizer handles this well
            var query = @"
                SELECT MAX(ContentId) 
                FROM Documents.dbo.MigrationStatus WITH (NOLOCK)
                WHERE Status = 'Success'";

            await using var command = new SqlCommand(query, connection);
            var result = await command.ExecuteScalarAsync(cancellationToken);

            return result == DBNull.Value ? null : (long?)result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get last processed ID");
            return null;
        }
    }

    public async Task RecordSuccessAsync(
        long contentId,
        string bucketName,
        string objectKey,
        string fileName,
        int fileSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Optimistic UPDATE first (most common case after first run)
        // Falls back to INSERT only if needed - saves one index seek
        var query = @"
            UPDATE Documents.dbo.MigrationStatus
            SET Status = 'Success',
                BucketName = @BucketName,
                ObjectKey = @ObjectKey,
                FileName = @FileName,
                FileSize = @FileSize,
                MigratedAt = GETUTCDATE(),
                UpdatedAt = GETUTCDATE(),
                ErrorMessage = NULL,
                AttemptCount = AttemptCount + 1
            WHERE ContentId = @ContentId;
            
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO Documents.dbo.MigrationStatus 
                    (ContentId, BucketName, ObjectKey, FileName, FileSize, Status, MigratedAt, AttemptCount)
                VALUES 
                    (@ContentId, @BucketName, @ObjectKey, @FileName, @FileSize, 'Success', GETUTCDATE(), 1)
            END";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@ContentId", SqlDbType.BigInt).Value = contentId;
        command.Parameters.Add("@BucketName", SqlDbType.NVarChar, 255).Value = bucketName;
        command.Parameters.Add("@ObjectKey", SqlDbType.NVarChar, 500).Value = objectKey;
        command.Parameters.Add("@FileName", SqlDbType.NVarChar, 256).Value = fileName ?? (object)DBNull.Value;
        command.Parameters.Add("@FileSize", SqlDbType.Int).Value = fileSize;

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogDebug(
            "Success: {ContentId} -> {Bucket}/{Key} ({FileName}, {SizeMB:F2} MB)",
            contentId, bucketName, objectKey, fileName, fileSize / 1024.0 / 1024.0);
    }

    public async Task RecordFailureAsync(
        long contentId,
        string error,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Optimistic UPDATE first
        var query = @"
            UPDATE Documents.dbo.MigrationStatus
            SET Status = 'Failed',
                ErrorMessage = @ErrorMessage,
                AttemptCount = AttemptCount + 1,
                UpdatedAt = GETUTCDATE()
            WHERE ContentId = @ContentId;
            
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO Documents.dbo.MigrationStatus 
                    (ContentId, BucketName, ObjectKey, Status, ErrorMessage, AttemptCount)
                VALUES 
                    (@ContentId, 'unknown', 'unknown', 'Failed', @ErrorMessage, 1)
            END";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@ContentId", SqlDbType.BigInt).Value = contentId;
        command.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, -1).Value = error ?? (object)DBNull.Value;

        await command.ExecuteNonQueryAsync(cancellationToken);

        _logger.LogWarning("Failed: {ContentId} - {Error}", contentId, error);
    }

    public async Task RecordInProgressAsync(
        long contentId,
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Simple conditional insert - readable and efficient
        var query = @"
            IF NOT EXISTS (SELECT 1 FROM Documents.dbo.MigrationStatus WHERE ContentId = @ContentId)
            BEGIN
                INSERT INTO Documents.dbo.MigrationStatus 
                    (ContentId, BucketName, ObjectKey, Status, AttemptCount)
                VALUES 
                    (@ContentId, @BucketName, @ObjectKey, 'InProgress', 0)
            END";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@ContentId", SqlDbType.BigInt).Value = contentId;
        command.Parameters.Add("@BucketName", SqlDbType.NVarChar, 255).Value = bucketName;
        command.Parameters.Add("@ObjectKey", SqlDbType.NVarChar, 500).Value = objectKey;

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<MigrationStats> GetStatsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Single query with aggregations - efficient
        var query = @"
            SELECT 
                SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as Succeeded,
                SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as Failed,
                SUM(CASE WHEN Status = 'InProgress' THEN 1 ELSE 0 END) as InProgress,
                COUNT(*) as TotalProcessed,
                MIN(CreatedAt) as StartedAt,
                MAX(UpdatedAt) as LastProcessedAt,
                SUM(ISNULL(FileSize, 0)) as TotalSizeBytes
            FROM Documents.dbo.MigrationStatus WITH (NOLOCK)";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (await reader.ReadAsync(cancellationToken))
        {
            return new MigrationStats
            {
                Succeeded = reader.GetInt32(0),
                Failed = reader.GetInt32(1),
                InProgress = reader.GetInt32(2),
                TotalProcessed = reader.GetInt32(3),
                StartedAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                LastProcessedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                TotalSizeBytes = reader.IsDBNull(6) ? 0 : reader.GetInt64(6)
            };
        }

        return new MigrationStats();
    }

    public async Task<List<FailedMigrationInfo>> GetFailedItemsAsync(
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Simple filtered query with proper parameter
        var query = @"
            SELECT ContentId, ErrorMessage, AttemptCount, UpdatedAt
            FROM Documents.dbo.MigrationStatus WITH (NOLOCK)
            WHERE Status = 'Failed' AND AttemptCount < @MaxRetries
            ORDER BY UpdatedAt";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@MaxRetries", SqlDbType.Int).Value = maxRetries;

        var items = new List<FailedMigrationInfo>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add(new FailedMigrationInfo
            {
                ContentId = reader.GetInt64(0),
                ErrorMessage = reader.IsDBNull(1) ? null : reader.GetString(1),
                AttemptCount = reader.GetInt32(2),
                LastAttempt = reader.GetDateTime(3)
            });
        }

        return items;
    }

    public async Task ResetStuckInProgressAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Straightforward UPDATE with timeout calculation
        var query = @"
            UPDATE Documents.dbo.MigrationStatus
            SET Status = 'Failed',
                ErrorMessage = 'Stuck in progress - timeout exceeded',
                UpdatedAt = GETUTCDATE()
            WHERE Status = 'InProgress' 
            AND UpdatedAt < DATEADD(MINUTE, -@TimeoutMinutes, GETUTCDATE())";

        await using var command = new SqlCommand(query, connection);
        command.Parameters.Add("@TimeoutMinutes", SqlDbType.Int).Value = (int)timeout.TotalMinutes;

        var affected = await command.ExecuteNonQueryAsync(cancellationToken);

        if (affected > 0)
        {
            _logger.LogWarning("Reset {Count} stuck in-progress items", affected);
        }
    }

    public async Task<Dictionary<string, BucketStats>> GetBucketStatsAsync(
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // GROUP BY aggregation - standard and efficient
        var query = @"
            SELECT 
                BucketName,
                COUNT(*) as FileCount,
                SUM(ISNULL(FileSize, 0)) as TotalSizeBytes,
                MIN(MigratedAt) as FirstMigration,
                MAX(MigratedAt) as LastMigration
            FROM Documents.dbo.MigrationStatus WITH (NOLOCK)
            WHERE Status = 'Success'
            GROUP BY BucketName";

        await using var command = new SqlCommand(query, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var stats = new Dictionary<string, BucketStats>();

        while (await reader.ReadAsync(cancellationToken))
        {
            var bucketName = reader.GetString(0);
            stats[bucketName] = new BucketStats
            {
                BucketName = bucketName,
                FileCount = reader.GetInt32(1),
                TotalSizeBytes = reader.GetInt64(2),
                FirstMigration = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                LastMigration = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
            };
        }

        return stats;
    }
}