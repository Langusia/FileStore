-- ============================================================================
-- Utility Queries for Migration Management
-- Copy/paste these queries as needed during migration
-- ============================================================================

USE
Documents;
GO

PRINT '
============================================================================
MIGRATION UTILITY QUERIES
============================================================================
Copy and run these queries as needed during your migration process
';
GO

-- ============================================================================
-- 1. OVERALL MIGRATION STATUS
-- ============================================================================
PRINT '
-- Overall Migration Status
-- Shows current state of migration with percentages
';
GO

SELECT Status,
       RecordCount,
       CAST(RecordCount * 100.0 / SUM(RecordCount) OVER() AS DECIMAL (5, 2)) as Percentage,
       RetryCount,
       TotalSizeBytes / 1024.0 / 1024.0 / 1024.0                             as TotalSizeGB,
       FirstMigration,
       LastMigration
FROM dbo.vw_MigrationStats
ORDER BY CASE Status
             WHEN 'Success' THEN 1
             WHEN 'InProgress' THEN 2
             WHEN 'Failed' THEN 3
             END;
GO

-- ============================================================================
-- 2. MIGRATION PROGRESS BY BUCKET
-- ============================================================================
PRINT '
-- Migration Progress by Bucket
-- Shows how much data migrated to each bucket
';
GO

SELECT BucketName,
       FileCount,
       TotalSizeMB,
       TotalSizeGB,
       FirstMigration,
       LastMigration,
       MigrationDurationMinutes,
       CAST(FileCount * 1.0 / NULLIF(MigrationDurationMinutes, 0) AS DECIMAL(10, 2)) as FilesPerMinute
FROM dbo.vw_MigrationByBucket
ORDER BY TotalSizeGB DESC;
GO

-- ============================================================================
-- 3. TOP FAILED MIGRATIONS
-- ============================================================================
PRINT '
-- Top 20 Failed Migrations
-- Most recent failures with error details
';
GO

SELECT TOP 20
    ContentId, FileName,
       ErrorMessage,
       AttemptCount,
       LastAttempt,
       RetryPriority
FROM dbo.vw_MigrationFailures
ORDER BY AttemptCount DESC, LastAttempt DESC;
GO

-- ============================================================================
-- 4. MIGRATION RATE ANALYSIS
-- ============================================================================
PRINT '
-- Migration Rate Analysis
-- Shows throughput over time
';
GO

SELECT MigrationHour,
       FilesProcessed,
       SuccessCount,
       FailureCount,
       TotalMBProcessed,
       CAST(SuccessCount * 100.0 / NULLIF(FilesProcessed, 0) AS DECIMAL(5, 2)) as SuccessRate,
       CAST(AvgFileSize / 1024.0 / 1024.0 AS DECIMAL(10, 2))                   as AvgFileSizeMB
FROM dbo.vw_MigrationTimeline
ORDER BY MigrationHour DESC;
GO

-- ============================================================================
-- 5. PENDING DOCUMENTS COUNT
-- ============================================================================
PRINT '
-- Count of Documents Still Pending Migration
';
GO

SELECT COUNT(*) as PendingDocuments
FROM Documents.dbo.Documents d
WHERE d.DelStatus = 0
  AND EXISTS (SELECT 1
              FROM Documents.dbo.DocumentsContent dc
              WHERE dc.Id = d.DocumentID)
  AND NOT EXISTS (SELECT 1
                  FROM Documents.dbo.MigrationStatus ms
                  WHERE ms.ContentId = d.DocumentID
                    AND ms.Status = 'Success');
GO

-- ============================================================================
-- 6. STUCK IN-PROGRESS ITEMS
-- ============================================================================
PRINT '
-- Documents Stuck In Progress (older than 30 minutes)
';
GO

SELECT ContentId,
       BucketName,
       ObjectKey,
       UpdatedAt,
       DATEDIFF(MINUTE, UpdatedAt, GETUTCDATE()) as MinutesStuck
FROM dbo.MigrationStatus
WHERE Status = 'InProgress'
  AND UpdatedAt < DATEADD(MINUTE, -30, GETUTCDATE())
ORDER BY UpdatedAt;
GO

-- ============================================================================
-- 7. LARGEST FILES MIGRATED
-- ============================================================================
PRINT '
-- Top 20 Largest Files Migrated
';
GO

SELECT TOP 20
    ContentId, FileName,
       BucketName,
       ObjectKey,
       FileSize / 1024.0 / 1024.0 as FileSizeMB,
       MigratedAt
FROM dbo.MigrationStatus
WHERE Status = 'Success'
ORDER BY FileSize DESC;
GO

-- ============================================================================
-- 8. ERROR SUMMARY
-- ============================================================================
PRINT '
-- Error Summary - Common Failure Reasons
';
GO

SELECT CASE
           WHEN ErrorMessage LIKE '%timeout%' THEN 'Timeout'
           WHEN ErrorMessage LIKE '%network%' THEN 'Network Error'
           WHEN ErrorMessage LIKE '%null or empty%' THEN 'Empty Content'
           WHEN ErrorMessage LIKE '%access%denied%' THEN 'Permission Error'
           ELSE 'Other'
           END                                                         as ErrorCategory,
       COUNT(*)                                                        as ErrorCount,
       CAST(COUNT(*) * 100.0 / SUM(COUNT(*)) OVER() AS DECIMAL (5, 2)) as Percentage
FROM dbo.MigrationStatus
WHERE Status = 'Failed'
GROUP BY CASE
             WHEN ErrorMessage LIKE '%timeout%' THEN 'Timeout'
             WHEN ErrorMessage LIKE '%network%' THEN 'Network Error'
             WHEN ErrorMessage LIKE '%null or empty%' THEN 'Empty Content'
             WHEN ErrorMessage LIKE '%access%denied%' THEN 'Permission Error'
             ELSE 'Other'
             END
ORDER BY ErrorCount DESC;
GO

-- ============================================================================
-- 9. RESET FAILED MIGRATIONS (for retry)
-- ============================================================================
PRINT '
-- Reset Failed Migrations to Allow Retry
-- CAUTION: This will reset all failed items with less than 3 attempts
-- Uncomment to execute
';
GO

/*
UPDATE dbo.MigrationStatus
SET Status = 'InProgress',
    UpdatedAt = GETUTCDATE()
WHERE Status = 'Failed'
AND AttemptCount < 3;

SELECT @@ROWCOUNT as ResetCount;
*/
GO

-- ============================================================================
-- 10. CLEANUP COMPLETED MIGRATION DATA (after verification)
-- ============================================================================
PRINT '
-- Clean Up Migration Status (AFTER VERIFICATION ONLY!)
-- CAUTION: This will delete all migration tracking data
-- Only run this after verifying all files are correctly migrated
-- Uncomment to execute
';
GO

/*
-- Delete successful migrations older than 30 days
DELETE FROM dbo.MigrationStatus
WHERE Status = ''Success''
AND MigratedAt < DATEADD(DAY, -30, GETUTCDATE());

SELECT @@ROWCOUNT as DeletedRecords;
*/
GO

PRINT '
============================================================================
END OF UTILITY QUERIES
============================================================================
';
GO