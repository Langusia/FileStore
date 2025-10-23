using System.Diagnostics;
using Credo.FileStorage.Worker.Interfaces;
using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Services;

public class MigrationOrchestrator : IMigrationOrchestrator
{
    private readonly IDocumentRepository _repository;
    private readonly IBucketRouter _router;
    private readonly IMinioStorageService _minio;
    private readonly IMigrationTracker _tracker;
    private readonly ILogger<MigrationOrchestrator> _logger;

    public MigrationOrchestrator(
        IDocumentRepository repository,
        IBucketRouter router,
        IMinioStorageService minio,
        IMigrationTracker tracker,
        ILogger<MigrationOrchestrator> logger)
    {
        _repository = repository;
        _router = router;
        _minio = minio;
        _tracker = tracker;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateAsync(
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var result = new MigrationResult();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            _logger.LogInformation("=== Starting Document Migration ===");

            // Reset stuck items from previous failed runs
            _logger.LogInformation("Checking for stuck in-progress items...");
            await _tracker.ResetStuckInProgressAsync(TimeSpan.FromMinutes(30), cancellationToken);

            // Get total count for progress calculation
            _logger.LogInformation("Calculating pending documents...");
            var totalPending = await _repository.GetPendingCountAsync(cancellationToken);
            _logger.LogInformation("Found {Count} documents to migrate", totalPending);

            if (totalPending == 0)
            {
                _logger.LogInformation("No documents to migrate. Exiting.");
                result.Success = true;
                result.Duration = stopwatch.Elapsed;
                return result;
            }

            var batchNumber = 0;
            var processedCount = 0;
            var lastProgressReport = DateTime.UtcNow;

            _logger.LogInformation("Starting migration process with batch size: {BatchSize}", options.BatchSize);

            await foreach (var metadata in _repository.GetPendingMetadataAsync(options.BatchSize, cancellationToken))
            {
                try
                {
                    var bucket = _router.GetBucketName(metadata);
                    var key = _router.GetObjectKey(metadata);

                    // Ensure bucket exists (cached internally in MinioStorageService)
                    await _minio.EnsureBucketExistsAsync(bucket, cancellationToken);

                    // Check if already exists in MinIO (optional optimization)
                    if (options.SkipExisting && await _minio.ObjectExistsAsync(bucket, key, cancellationToken))
                    {
                        _logger.LogDebug("Skipping existing: {Bucket}/{Key}", bucket, key);

                        // Still record as success even though we skipped upload
                        await _tracker.RecordSuccessAsync(
                            metadata.DocumentID, bucket, key,
                            metadata.DocumentName, metadata.FileSize, cancellationToken);

                        result.Succeeded++;
                        result.TotalBytesProcessed += metadata.FileSize;
                        processedCount++;
                        continue;
                    }

                    // Dry run mode - just simulate without actual upload
                    if (options.DryRun)
                    {
                        _logger.LogInformation(
                            "DRY RUN: Would migrate DocumentID {Id} -> {Bucket}/{Key} ({FileName}, {SizeMB:F2} MB)",
                            metadata.DocumentID, bucket, key, metadata.DocumentName,
                            metadata.FileSize / 1024.0 / 1024.0);

                        result.Succeeded++;
                        result.TotalBytesProcessed += metadata.FileSize;
                        processedCount++;
                        continue;
                    }

                    // Record as in-progress (for crash recovery)
                    await _tracker.RecordInProgressAsync(metadata.DocumentID, bucket, key, cancellationToken);

                    // Fetch actual content from DocumentsContent table
                    var content = await _repository.GetDocumentContentAsync(metadata.DocumentID, cancellationToken);

                    if (content == null || content.Length == 0)
                    {
                        var error = "Content is null or empty";
                        _logger.LogWarning("DocumentID {Id}: {Error}", metadata.DocumentID, error);

                        await _tracker.RecordFailureAsync(metadata.DocumentID, error, cancellationToken);
                        result.Failed++;
                        result.Errors.Add($"DocumentID {metadata.DocumentID}: {error}");
                        processedCount++;
                        continue;
                    }

                    // Upload to MinIO
                    await _minio.UploadAsync(bucket, key, content, metadata.ContentType, cancellationToken);

                    // Use actual content length (most accurate, no DATALENGTH overhead)
                    var actualSize = content.Length;

                    // Record success with actual size
                    await _tracker.RecordSuccessAsync(
                        metadata.DocumentID, bucket, key,
                        metadata.DocumentName, actualSize, cancellationToken);

                    result.Succeeded++;
                    result.TotalBytesProcessed += actualSize;

                    _logger.LogDebug(
                        "Migrated DocumentID {Id}: {FileName} ({SizeMB:F2} MB) -> {Bucket}/{Key}",
                        metadata.DocumentID, metadata.DocumentName,
                        actualSize / 1024.0 / 1024.0, bucket, key);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to migrate DocumentID: {DocumentID}", metadata.DocumentID);

                    await _tracker.RecordFailureAsync(
                        metadata.DocumentID,
                        ex.Message,
                        cancellationToken);

                    result.Failed++;
                    result.Errors.Add($"DocumentID {metadata.DocumentID}: {ex.Message}");
                }

                processedCount++;
                result.TotalProcessed = processedCount;

                // Report progress periodically (not every item to avoid spam)
                var timeSinceLastReport = DateTime.UtcNow - lastProgressReport;
                var shouldReport = processedCount % options.BatchSize == 0 ||
                                   timeSinceLastReport > TimeSpan.FromSeconds(5);

                if (shouldReport)
                {
                    batchNumber++;
                    var elapsed = stopwatch.Elapsed;
                    var rate = processedCount / elapsed.TotalSeconds;
                    var remaining = totalPending - processedCount;
                    var estimatedRemaining = remaining > 0 && rate > 0
                        ? TimeSpan.FromSeconds(remaining / rate)
                        : (TimeSpan?)null;

                    var progressReport = new MigrationProgress
                    {
                        CurrentBatch = batchNumber,
                        TotalProcessed = processedCount,
                        Succeeded = result.Succeeded,
                        Failed = result.Failed,
                        PercentComplete = totalPending > 0 ? (double)processedCount / totalPending : 0,
                        Elapsed = elapsed,
                        EstimatedTimeRemaining = estimatedRemaining,
                        BytesProcessed = result.TotalBytesProcessed
                    };

                    progress?.Report(progressReport);

                    _logger.LogInformation(
                        "Progress: {Processed}/{Total} ({Percent:P1}) | ✓ {Success} | ✗ {Failed} | {SizeMB:F2} MB | Rate: {Rate:F2} files/sec | ETA: {ETA}",
                        processedCount, totalPending,
                        totalPending > 0 ? (double)processedCount / totalPending : 0,
                        result.Succeeded, result.Failed, result.TotalMBProcessed,
                        rate,
                        estimatedRemaining?.ToString(@"hh\:mm\:ss") ?? "N/A");

                    lastProgressReport = DateTime.UtcNow;
                }
            }

            result.Success = result.Failed == 0;
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "\n=== Migration Complete ===\n" +
                "Status: {Status}\n" +
                "Total Processed: {Total}\n" +
                "Succeeded: {Success}\n" +
                "Failed: {Failed}\n" +
                "Total Size: {SizeMB:F2} MB\n" +
                "Duration: {Duration}\n" +
                "Rate: {Rate:F2} files/sec\n" +
                "========================",
                result.Success ? "SUCCESS" : "COMPLETED WITH ERRORS",
                result.TotalProcessed, result.Succeeded, result.Failed,
                result.TotalMBProcessed, result.Duration,
                result.TotalProcessed / result.Duration.TotalSeconds);

            if (result.Errors.Any())
            {
                _logger.LogWarning("Migration completed with {ErrorCount} errors. Check logs for details.", result.Errors.Count);

                // Log first few errors
                foreach (var error in result.Errors.Take(10))
                {
                    _logger.LogWarning("  - {Error}", error);
                }

                if (result.Errors.Count > 10)
                {
                    _logger.LogWarning("  ... and {MoreErrors} more errors", result.Errors.Count - 10);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Migration was cancelled by user");
            result.Success = false;
            result.Duration = stopwatch.Elapsed;
            result.Errors.Add("Migration cancelled by user");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed with fatal error");
            result.Success = false;
            result.Duration = stopwatch.Elapsed;
            result.Errors.Add($"Fatal error: {ex.Message}");
        }

        return result;
    }
}