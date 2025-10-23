-- ============================================================================
-- Indexes for MigrationStatus Table
-- Optimized for common query patterns in the migration process
-- ============================================================================

USE
Documents;
GO

PRINT 'Creating indexes on MigrationStatus table...';
GO

-- Covering index for success status checks (most common query)
-- Includes all columns needed to avoid key lookups
CREATE
NONCLUSTERED INDEX IX_MigrationStatus_ContentId_Status
ON dbo.MigrationStatus (ContentId, Status)
INCLUDE (BucketName, ObjectKey, FileName, FileSize, MigratedAt)
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

PRINT '  ✓ Created IX_MigrationStatus_ContentId_Status';
GO

-- Filtered index for failed items (only includes failed records)
-- Used when retrying failed migrations
CREATE
NONCLUSTERED INDEX IX_MigrationStatus_Failed
ON dbo.MigrationStatus (Status, AttemptCount, UpdatedAt)
INCLUDE (ContentId, ErrorMessage)
WHERE Status = 'Failed'
WITH (FILLFACTOR = 80, ONLINE = ON);
GO

PRINT '  ✓ Created IX_MigrationStatus_Failed';
GO

-- Filtered index for in-progress items (used for stuck process detection)
CREATE
NONCLUSTERED INDEX IX_MigrationStatus_InProgress
ON dbo.MigrationStatus (UpdatedAt)
INCLUDE (ContentId, Status)
WHERE Status = 'InProgress'
WITH (FILLFACTOR = 80, ONLINE = ON);
GO

PRINT '  ✓ Created IX_MigrationStatus_InProgress';
GO

-- Index for bucket-based queries (statistics and reporting)
CREATE
NONCLUSTERED INDEX IX_MigrationStatus_Bucket
ON dbo.MigrationStatus (BucketName, Status)
INCLUDE (FileSize, MigratedAt)
WHERE Status = 'Success'
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

PRINT '  ✓ Created IX_MigrationStatus_Bucket';
GO

-- Index for date-based queries and reporting
CREATE
NONCLUSTERED INDEX IX_MigrationStatus_Dates
ON dbo.MigrationStatus (MigratedAt, Status)
INCLUDE (ContentId, BucketName, FileSize)
WHERE MigratedAt IS NOT NULL
WITH (FILLFACTOR = 90, ONLINE = ON);
GO

PRINT '  ✓ Created IX_MigrationStatus_Dates';
GO

PRINT 'All indexes created successfully';
GO

-- Display index information
SELECT i.name                              AS IndexName,
       i.type_desc                         AS IndexType,
       i.is_unique                         AS IsUnique,
       i.fill_factor                       AS FillFactor,
       STATS_DATE(i.object_id, i.index_id) AS StatsLastUpdated
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID('dbo.MigrationStatus')
  AND i.name IS NOT NULL
ORDER BY i.name;
GO