-- Database initialization script for Docker
-- This script creates the database and schema automatically

IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FileStore')
BEGIN
    CREATE DATABASE FileStore;
END
GO

USE FileStore;
GO

-- Check if tables already exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'StoredObjects')
BEGIN
    -- StoredObjects table: Core metadata for all stored files
    CREATE TABLE StoredObjects (
        ObjectId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
        Bucket NVARCHAR(255) NOT NULL,
        RelativePath NVARCHAR(1000) NOT NULL,
        Tier TINYINT NOT NULL, -- 0=Hot, 1=Cold
        Length BIGINT NOT NULL,
        ContentType NVARCHAR(255) NOT NULL,
        Hash NVARCHAR(64) NOT NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        LastAccessedAt DATETIME2 NULL,
        Tags NVARCHAR(MAX) NULL, -- JSON

        CONSTRAINT CK_StoredObjects_Tier CHECK (Tier IN (0, 1))
    );

    CREATE INDEX IX_StoredObjects_Bucket ON StoredObjects(Bucket);
    CREATE INDEX IX_StoredObjects_Bucket_ObjectId ON StoredObjects(Bucket, ObjectId);
    CREATE INDEX IX_StoredObjects_CreatedAt ON StoredObjects(CreatedAt);
    CREATE INDEX IX_StoredObjects_LastAccessedAt ON StoredObjects(LastAccessedAt) WHERE LastAccessedAt IS NOT NULL;
    CREATE INDEX IX_StoredObjects_Tier_LastAccessedAt ON StoredObjects(Tier, LastAccessedAt) WHERE Tier = 0;
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ObjectLinks')
BEGIN
    -- ObjectLinks table: Business context mapping
    CREATE TABLE ObjectLinks (
        ObjectId UNIQUEIDENTIFIER NOT NULL,
        Channel NVARCHAR(100) NOT NULL,
        Operation NVARCHAR(100) NOT NULL,
        BusinessEntityId NVARCHAR(255) NULL,
        CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT FK_ObjectLinks_StoredObjects FOREIGN KEY (ObjectId) REFERENCES StoredObjects(ObjectId) ON DELETE CASCADE
    );

    CREATE INDEX IX_ObjectLinks_ObjectId ON ObjectLinks(ObjectId);
    CREATE INDEX IX_ObjectLinks_Channel_Operation ON ObjectLinks(Channel, Operation);
    CREATE INDEX IX_ObjectLinks_BusinessEntityId ON ObjectLinks(BusinessEntityId) WHERE BusinessEntityId IS NOT NULL;
END
GO

PRINT 'Database initialization completed successfully';
