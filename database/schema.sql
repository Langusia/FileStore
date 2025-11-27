-- FileStore Database Schema
-- SQL Server

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

    CONSTRAINT CK_StoredObjects_Tier CHECK (Tier IN (0, 1)),
    INDEX IX_StoredObjects_Bucket (Bucket),
    INDEX IX_StoredObjects_Bucket_ObjectId (Bucket, ObjectId),
    INDEX IX_StoredObjects_CreatedAt (CreatedAt),
    INDEX IX_StoredObjects_LastAccessedAt (LastAccessedAt) WHERE LastAccessedAt IS NOT NULL,
    INDEX IX_StoredObjects_Tier_LastAccessedAt (Tier, LastAccessedAt) WHERE Tier = 0 -- Hot tier objects for tiering queries
);

-- ObjectLinks table: Business context mapping
CREATE TABLE ObjectLinks (
    ObjectId UNIQUEIDENTIFIER NOT NULL,
    Channel NVARCHAR(100) NOT NULL,
    Operation NVARCHAR(100) NOT NULL,
    BusinessEntityId NVARCHAR(255) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_ObjectLinks_StoredObjects FOREIGN KEY (ObjectId) REFERENCES StoredObjects(ObjectId) ON DELETE CASCADE,
    INDEX IX_ObjectLinks_ObjectId (ObjectId),
    INDEX IX_ObjectLinks_Channel_Operation (Channel, Operation),
    INDEX IX_ObjectLinks_BusinessEntityId (BusinessEntityId) WHERE BusinessEntityId IS NOT NULL
);

-- Sample queries for common operations:

-- Get object with links
-- SELECT o.*, l.Channel, l.Operation, l.BusinessEntityId
-- FROM StoredObjects o
-- LEFT JOIN ObjectLinks l ON o.ObjectId = l.ObjectId
-- WHERE o.Bucket = 'my-bucket' AND o.ObjectId = '...';

-- List objects in bucket with pagination
-- SELECT ObjectId, Bucket, ContentType, Length, Tier, CreatedAt, LastAccessedAt
-- FROM StoredObjects
-- WHERE Bucket = 'my-bucket'
-- ORDER BY ObjectId
-- OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY;

-- Find objects eligible for cold tiering
-- SELECT TOP (@BatchSize) ObjectId, RelativePath
-- FROM StoredObjects
-- WHERE Tier = 0 -- Hot
--   AND LastAccessedAt < DATEADD(DAY, -@ColdAfterDays, GETUTCDATE())
--   AND Bucket NOT IN (SELECT value FROM STRING_SPLIT(@ExcludedBuckets, ','))
-- ORDER BY LastAccessedAt;
