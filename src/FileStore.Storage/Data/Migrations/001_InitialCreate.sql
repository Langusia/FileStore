-- FileStore Initial Database Schema
-- This script creates the necessary tables for FileStore metadata storage

-- Create StorageBuckets table
CREATE TABLE StorageBuckets (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BucketName NVARCHAR(255) NOT NULL,
    ChannelId INT NOT NULL,
    ChannelName NVARCHAR(50) NOT NULL,
    OperationId INT NOT NULL,
    OperationName NVARCHAR(50) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastAccessedAt DATETIME2 NULL,
    Description NVARCHAR(500) NULL,
    CONSTRAINT UQ_StorageBuckets_BucketName UNIQUE (BucketName)
);

CREATE INDEX IX_StorageBuckets_ChannelId_OperationId ON StorageBuckets(ChannelId, OperationId);

-- Create StorageObjects table
CREATE TABLE StorageObjects (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    ObjectId NVARCHAR(36) NOT NULL,
    BucketId INT NOT NULL,
    ObjectKey NVARCHAR(500) NOT NULL,
    OriginalFileName NVARCHAR(255) NOT NULL,
    FullStorageUrl NVARCHAR(1000) NOT NULL,
    ContentType NVARCHAR(100) NULL,
    SizeInBytes BIGINT NULL,
    ETag NVARCHAR(64) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedAt DATETIME2 NULL,
    LastAccessedAt DATETIME2 NULL,
    IsDeleted BIT NOT NULL DEFAULT 0,
    DeletedAt DATETIME2 NULL,
    Metadata NVARCHAR(MAX) NULL,
    CONSTRAINT FK_StorageObjects_Bucket FOREIGN KEY (BucketId)
        REFERENCES StorageBuckets(Id),
    CONSTRAINT UQ_StorageObjects_ObjectId UNIQUE (ObjectId)
);

CREATE INDEX IX_StorageObjects_ObjectKey ON StorageObjects(ObjectKey);
CREATE INDEX IX_StorageObjects_BucketId_ObjectKey ON StorageObjects(BucketId, ObjectKey);
CREATE INDEX IX_StorageObjects_IsDeleted ON StorageObjects(IsDeleted);
CREATE INDEX IX_StorageObjects_CreatedAt ON StorageObjects(CreatedAt);

-- Create BucketTags table (for Scope 2 - lifecycle management)
CREATE TABLE BucketTags (
    Id INT PRIMARY KEY IDENTITY(1,1),
    BucketId INT NOT NULL,
    [Key] NVARCHAR(128) NOT NULL,
    Value NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_BucketTags_Bucket FOREIGN KEY (BucketId)
        REFERENCES StorageBuckets(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_BucketTags_BucketId_Key UNIQUE (BucketId, [Key])
);

-- Create ObjectTags table (for Scope 2 - lifecycle management)
CREATE TABLE ObjectTags (
    Id BIGINT PRIMARY KEY IDENTITY(1,1),
    ObjectId BIGINT NOT NULL,
    [Key] NVARCHAR(128) NOT NULL,
    Value NVARCHAR(256) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT FK_ObjectTags_Object FOREIGN KEY (ObjectId)
        REFERENCES StorageObjects(Id) ON DELETE CASCADE,
    CONSTRAINT UQ_ObjectTags_ObjectId_Key UNIQUE (ObjectId, [Key])
);

PRINT 'FileStore database schema created successfully';
