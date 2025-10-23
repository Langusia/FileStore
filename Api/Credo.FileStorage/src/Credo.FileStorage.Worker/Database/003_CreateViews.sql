-- ============================================================================
-- Views for MigrationStatus Reporting
-- Provides easy access to migration statistics and analytics
-- ============================================================================

USE Documents;
GO

-- Drop views if they exist
IF OBJECT_ID('dbo.vw_MigrationStats', 'V') IS NOT NULL
DROP VIEW dbo.vw_MigrationStats;
GO

IF OBJECT_ID('dbo.vw_MigrationByBucket', 'V') IS NOT NULL
DROP VIEW dbo.vw_MigrationByBucket;
GO

IF OBJECT_ID('dbo.vw_MigrationFailures', 'V') IS NOT NULL
DROP VIEW dbo.vw_MigrationFailures;
GO

IF OBJECT_ID('dbo.vw_MigrationTimeline', 'V') IS NOT NULL
DROP VIEW dbo.vw_MigrationTimeline;
GO

PRINT 'Creating migration views...';
GO

-- ============================================================================
-- Overall Migration Statistics
-- ============================================================================
CREATE VIEW dbo.vw_MigrationStats
            WITH SCHEMABINDING
AS
SELECT
    Status,
    COUNT_BIG(*) as RecordCount,
    SUM(CASE WHEN AttemptCount > 1 THEN 1 ELSE 0 END) as RetryCount,
    MIN(MigratedAt) as FirstMigration,
    MAX(MigratedAt) as LastMigration,
    SUM(ISNULL(FileSize, 0)) as TotalSizeBytes,
    AVG(CAST(ISNULL(FileSize, 0) AS BIGINT)) as AvgSizeBytes,
    MAX(FileSize) as MaxSizeBytes,
    MIN(CASE WHEN FileSize > 0 THEN FileSize END) as MinSizeBytes
FROM dbo.MigrationStatus
GROUP BY Status;
GO

-- Create indexed view for better performance (optional, for large datasets)
CREATE UNIQUE CLUSTERED INDEX IX_vw_MigrationStats 
ON dbo.vw_MigrationStats(Status);
GO

PRINT '  ✓ Created vw_MigrationStats (indexed)';
GO

-- ============================================================================
-- Migration Statistics by Bucket
-- ============================================================================
CREATE VIEW dbo.vw_MigrationByBucket
AS
SELECT
    BucketName,
    COUNT(*) as FileCount,
    SUM(ISNULL(FileSize, 0)) as TotalSizeBytes,
    CAST(SUM(ISNULL(FileSize, 0)) / 1024.0 / 1024.0 AS DECIMAL(18,2)) as TotalSizeMB,
    CAST(SUM(ISNULL(FileSize, 0)) / 1024.0 / 1024.0 / 1024.0 AS DECIMAL(18,2)) as TotalSizeGB,
    AVG(CAST(FileSize AS BIGINT)) as AvgSizeBytes,
    MIN(MigratedAt) as FirstMigration,
    MAX(MigratedAt) as LastMigration,
    DATEDIFF(MINUTE, MIN(MigratedAt), MAX(MigratedAt)) as MigrationDurationMinutes
FROM dbo.MigrationStatus
WHERE Status = 'Success'
GROUP BY BucketName;
GO

PRINT '  ✓ Created vw_MigrationByBucket';
GO

-- ============================================================================
-- Failed Migrations with Details
-- ============================================================================
CREATE VIEW dbo.vw_MigrationFailures
AS
SELECT
    ms.ContentId,
    ms.BucketName,
    ms.ObjectKey,
    ms.FileName,
    ms.ErrorMessage,
    ms.AttemptCount,
    ms.UpdatedAt as LastAttempt,
    d.DocumentName as OriginalDocumentName,
    d.DocumentExt as FileExtension,
    d.FileSize as ExpectedFileSize,
    d.RecordDate as DocumentCreatedDate,
    CASE
        WHEN ms.AttemptCount >= 3 THEN 'Needs Manual Intervention'
        WHEN ms.AttemptCount >= 2 THEN 'High Priority Retry'
        ELSE 'Normal Retry'
        END as RetryPriority
FROM dbo.MigrationStatus ms
         INNER JOIN dbo.Documents d ON ms.ContentId = d.DocumentID
WHERE ms.Status = 'Failed';
GO

PRINT '  ✓ Created vw_MigrationFailures';
GO

-- ============================================================================
-- Migration Timeline (hourly aggregation)
-- ============================================================================
CREATE VIEW dbo.vw_MigrationTimeline
AS
SELECT
    DATEADD(HOUR, DATEDIFF(HOUR, 0, MigratedAt), 0) as MigrationHour,
    COUNT(*) as FilesProcessed,
    SUM(CASE WHEN Status = 'Success' THEN 1 ELSE 0 END) as SuccessCount,
    SUM(CASE WHEN Status = 'Failed' THEN 1 ELSE 0 END) as FailureCount,
    SUM(ISNULL(FileSize, 0)) as TotalBytesProcessed,
    CAST(SUM(ISNULL(FileSize, 0)) / 1024.0 / 1024.0 AS DECIMAL(18,2)) as TotalMBProcessed,
    AVG(CAST(FileSize AS BIGINT)) as AvgFileSize
FROM dbo.MigrationStatus
WHERE MigratedAt IS NOT NULL
GROUP BY DATEADD(HOUR, DATEDIFF(HOUR, 0, MigratedAt), 0);
GO

PRINT '  ✓ Created vw_MigrationTimeline';
GO

PRINT 'All views created successfully';
GO