# FileStore - Internal File/Object Storage Service

A .NET-based internal File/Object Storage Service with an S3-like API, designed to replace database-stored binary files with a scalable, backend-agnostic storage solution.

## Overview

FileStore provides a stable S3-like API that remains unchanged regardless of backend storage implementation. It separates physical storage concerns from business logic through metadata-driven architecture.

### Key Features

- **S3-like API**: Stable REST API for object storage operations
- **Pluggable Backends**: Start with SMB filesystem, migrate to SeaweedFS/MinIO/S3 later
- **Deterministic Sharding**: GUID-based file distribution for filesystem scalability
- **Tiering Support**: Automatic hot-to-cold storage migration
- **Metadata-Driven**: Business context stored in database, not folder structure
- **Backend Agnostic**: Physical storage layout independent from business meaning

## Architecture

### Components

```
FileStore/
├── src/
│   ├── FileStore.Core/           # Domain models, interfaces, enums
│   ├── FileStore.Infrastructure/ # Backend implementations, repositories
│   └── FileStore.API/            # REST API controllers
└── database/
    └── schema.sql                # SQL Server database schema
```

### Physical Storage Layout

Files are sharded using GUID-based deterministic distribution:

```
{tier}/{shard1}/{shard2}/{shard3}/{objectId}.{ext}

Example:
hot/b9/f7/8e/b9f78e7d5c224b739a27file.pdf
cold/0a/3c/4d/0a3c4db...bin
```

Configuration (default):
- Levels: 3
- CharsPerShard: 2

**Important**: The computed `RelativePath` is stored in the database and never recomputed. Sharding configuration changes only affect new files.

### Database Schema

#### StoredObjects Table
Core metadata for all stored files:
- ObjectId (GUID, primary key)
- Bucket (string)
- RelativePath (string, persisted)
- Tier (Hot/Cold)
- Length (bytes)
- ContentType
- Hash (SHA256)
- CreatedAt, LastAccessedAt
- Tags (JSON)

#### ObjectLinks Table
Business context mapping:
- ObjectId (foreign key)
- Channel (required: loans, cards, onboarding)
- Operation (required: agreements, statements, kyc)
- BusinessEntityId (optional: loanId, customerId, etc.)
- CreatedAt

## API Endpoints

### Upload Object
```http
POST /buckets/{bucket}/objects
Content-Type: multipart/form-data

file: <binary>
channel: loans
operation: agreements
businessEntityId: loan-12345
tags: {"department": "retail"}
```

Response:
```json
{
  "objectId": "b9f78e7d-5c22-4b73-9a27-...",
  "bucket": "documents",
  "size": 102400,
  "hash": "a3f5d9...",
  "contentType": "application/pdf",
  "createdAt": "2025-11-27T10:30:00Z"
}
```

### Download Object
```http
GET /buckets/{bucket}/objects/{objectId}
```

Returns file stream with appropriate Content-Type header. Supports range requests.

### Get Object Metadata
```http
HEAD /buckets/{bucket}/objects/{objectId}
```

Returns metadata in response headers:
- X-Object-Id
- X-Content-Length
- X-Content-Type
- X-Hash
- X-Tier
- X-Created-At
- X-Last-Accessed-At

### Delete Object
```http
DELETE /buckets/{bucket}/objects/{objectId}
```

Deletes both physical file and database records.

### List Objects
```http
GET /buckets/{bucket}/objects?prefix=&continuationToken=&maxKeys=1000
```

Response:
```json
{
  "bucket": "documents",
  "objects": [...],
  "isTruncated": false,
  "nextContinuationToken": null
}
```

### Change Tier
```http
POST /buckets/{bucket}/objects/{objectId}/tier
Content-Type: application/json

{
  "tier": "Cold"
}
```

Moves object between Hot and Cold storage tiers.

## Configuration

### appsettings.json

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=FileStore;User Id=sa;Password=YourPassword123;TrustServerCertificate=True"
  },
  "Storage": {
    "Backend": "SMB",
    "HotRootPath": "/mnt/storage/hot",
    "ColdRootPath": "/mnt/storage/cold",
    "Shard": {
      "Levels": 3,
      "CharsPerShard": 2
    },
    "MaxFileSizeMb": 100,
    "AllowedContentTypes": [
      "application/pdf",
      "image/jpeg",
      "image/png"
    ]
  },
  "Tiering": {
    "Enabled": true,
    "IntervalMinutes": 60,
    "ColdAfterDays": 365,
    "BatchSize": 100,
    "BucketsExcludedFromCold": ["audit-logs"]
  }
}
```

### Backend Configuration

Current: **SMB**
- SMB share mounted as local disk paths
- Normal file I/O operations
- SMB handles permissions, ACLs, backups, snapshots

Future backends (pluggable):
- SeaweedFS
- MinIO
- AWS S3

Change backend by implementing `IFileStorageBackend` and updating configuration.

## Setup Instructions

### Prerequisites

- .NET 8.0 SDK
- SQL Server
- SMB share (for production) or local filesystem (for development)

### Database Setup

1. Create database:
```sql
CREATE DATABASE FileStore;
```

2. Run schema script:
```bash
sqlcmd -S localhost -U sa -P YourPassword123 -d FileStore -i database/schema.sql
```

### Application Setup

1. Update connection string in `appsettings.json`

2. Configure storage paths:
   - Production: SMB mount points (`/mnt/storage/hot`, `/mnt/storage/cold`)
   - Development: Local directories (`./storage/hot`, `./storage/cold`)

3. Create storage directories:
```bash
# Production (SMB mounted)
sudo mkdir -p /mnt/storage/hot /mnt/storage/cold
sudo chown www-data:www-data /mnt/storage/hot /mnt/storage/cold

# Development
mkdir -p ./storage/hot ./storage/cold
```

4. Build and run:
```bash
dotnet build
dotnet run --project src/FileStore.API
```

5. Access Swagger UI: `https://localhost:5001/swagger`

## Tiering

The background service automatically moves objects from Hot to Cold storage based on configuration:

- **Enabled**: Enable/disable automatic tiering
- **IntervalMinutes**: How often to check for objects to tier
- **ColdAfterDays**: Age threshold (days since last access or creation)
- **BatchSize**: Number of objects to process per batch
- **BucketsExcludedFromCold**: Buckets to exclude from cold tiering

### Manual Tiering

Use the API endpoint to manually change tier:
```bash
curl -X POST https://localhost:5001/buckets/documents/objects/{objectId}/tier \
  -H "Content-Type: application/json" \
  -d '{"tier": "Cold"}'
```

## Usage Examples

### Upload a file
```bash
curl -X POST https://localhost:5001/buckets/loan-documents/objects \
  -F "file=@agreement.pdf" \
  -F "channel=loans" \
  -F "operation=agreements" \
  -F "businessEntityId=loan-12345" \
  -F 'tags={"department":"retail"}'
```

### Download a file
```bash
curl https://localhost:5001/buckets/loan-documents/objects/{objectId} \
  -o downloaded.pdf
```

### Get metadata
```bash
curl -I https://localhost:5001/buckets/loan-documents/objects/{objectId}
```

### List objects
```bash
curl https://localhost:5001/buckets/loan-documents/objects?maxKeys=100
```

### Delete object
```bash
curl -X DELETE https://localhost:5001/buckets/loan-documents/objects/{objectId}
```

## Core Principles

1. **Storage paths exist only for performance**: Physical layout is an implementation detail

2. **Business meaning lives in metadata**: Use Channel, Operation, BusinessEntityId, and Tags

3. **API stability**: The API contract never changes, regardless of backend

4. **No retroactive changes**: Sharding configuration changes only affect new files

5. **Paths are persisted**: RelativePath is stored in database, never recomputed

## Backend Migration

To migrate from SMB to a different backend:

1. Implement `IFileStorageBackend` for the new backend
2. Register the implementation in `Program.cs`
3. Update configuration: `Storage:Backend = "SeaweedFS"`
4. Migrate existing files (manual process or migration tool)

The API remains unchanged throughout the migration.

## Future Enhancements

- [ ] Range read support for large files
- [ ] Object versioning
- [ ] Multipart upload for large files
- [ ] Lifecycle policies
- [ ] Cross-region replication
- [ ] Access control lists (ACLs)
- [ ] Pre-signed URLs
- [ ] Audit logging
- [ ] Metrics and monitoring

## License

Internal use only.
