-- ============================================================================
-- Migration Status Table
-- Tracks the migration status of each document from SQL to MinIO
-- ============================================================================

USE
Documents;
GO

-- Drop table if exists (for clean reinstall)
IF OBJECT_ID('dbo.MigrationStatus', 'U') IS NOT NULL
DROP TABLE dbo.MigrationStatus;
GO

-- Create migration tracking table
CREATE TABLE dbo.MigrationStatus
(
    Id           BIGINT PRIMARY KEY IDENTITY(1,1),
    ContentId    BIGINT    NOT NULL UNIQUE,
    BucketName   NVARCHAR(255) NOT NULL,
    ObjectKey    NVARCHAR(500) NOT NULL,
    FileName     NVARCHAR(256) NULL,
    FileSize     INT NULL,
    Status       NVARCHAR(50) NOT NULL,
    ErrorMessage NVARCHAR(MAX) NULL,
    AttemptCount INT       NOT NULL DEFAULT 0,
    MigratedAt   DATETIME2 NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_MigrationStatus_Documents
        FOREIGN KEY (ContentId) REFERENCES dbo.Documents (DocumentID),

    CONSTRAINT CK_MigrationStatus_Status
        CHECK (Status IN ('Success', 'Failed', 'InProgress'))
);
GO

-- Add extended properties for documentation
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Tracks migration status of documents from SQL to MinIO storage',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'MigrationStatus';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Reference to Documents.DocumentID',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'MigrationStatus',
    @level2type = N'COLUMN', @level2name = N'ContentId';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'MinIO bucket name where document is stored',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'MigrationStatus',
    @level2type = N'COLUMN', @level2name = N'BucketName';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'MinIO object key (path) where document is stored',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'MigrationStatus',
    @level2type = N'COLUMN', @level2name = N'ObjectKey';
GO

EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Number of migration attempts for this document',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE',  @level1name = N'MigrationStatus',
    @level2type = N'COLUMN', @level2name = N'AttemptCount';
GO

PRINT 'MigrationStatus table created successfully';
GO