using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Interfaces;

public interface IMigrationTracker
{
    Task<long?> GetLastProcessedIdAsync(CancellationToken cancellationToken = default);
    
    Task RecordSuccessAsync(
        long contentId,
        string bucketName,
        string objectKey,
        string fileName,
        int fileSize,
        CancellationToken cancellationToken = default);
    
    Task RecordFailureAsync(
        long contentId,
        string error,
        CancellationToken cancellationToken = default);
    
    Task RecordInProgressAsync(
        long contentId,
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);
    
    Task<MigrationStats> GetStatsAsync(CancellationToken cancellationToken = default);
    
    Task<List<FailedMigrationInfo>> GetFailedItemsAsync(
        int maxRetries = 3,
        CancellationToken cancellationToken = default);
    
    Task ResetStuckInProgressAsync(
        TimeSpan timeout,
        CancellationToken cancellationToken = default);
    
    Task<Dictionary<string, BucketStats>> GetBucketStatsAsync(
        CancellationToken cancellationToken = default);
}