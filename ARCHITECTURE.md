# FileStore Architecture

## System Overview

FileStore is a .NET-based object storage service that provides an S3-like API for storing and retrieving files. It separates physical storage concerns from business logic through a metadata-driven architecture.

## Design Principles

### 1. Stable API Contract
The API remains unchanged regardless of backend storage implementation. This allows seamless migration from SMB to SeaweedFS, MinIO, or S3 without affecting consumers.

### 2. Backend Abstraction
```
IFileStorageBackend
├── SmbStorageBackend (current)
├── SeaweedFSBackend (future)
├── MinIOBackend (future)
└── S3Backend (future)
```

### 3. Metadata-Driven Business Logic
Business context lives in the database, not in folder structures:
- **Physical**: `hot/b9/f7/8e/b9f78e7d5c224b739a27file.pdf`
- **Business**: Channel=loans, Operation=agreements, BusinessEntityId=loan-12345

### 4. Immutable Path Strategy
Once computed, the RelativePath is stored in the database and never recomputed. This prevents:
- Breaking existing references when configuration changes
- Expensive recomputation overhead
- Data loss during migrations

## Architecture Layers

```
┌─────────────────────────────────────────────┐
│           API Layer (Controllers)           │
│  - StorageController (S3-like endpoints)    │
│  - Request/Response DTOs                    │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│        Service Layer (Orchestration)        │
│  - StorageService (business logic)          │
│  - TieringBackgroundService                 │
│  - ShardingStrategy                         │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│         Infrastructure Layer                │
│  ┌─────────────────┐  ┌──────────────────┐ │
│  │ Backend Storage │  │ Database Repos   │ │
│  │ - SMB           │  │ - ObjectRepo     │ │
│  │ - SeaweedFS     │  │ - LinkRepo       │ │
│  │ - MinIO/S3      │  └──────────────────┘ │
│  └─────────────────┘                        │
└─────────────────────────────────────────────┘
                    ↓
┌─────────────────────────────────────────────┐
│            Physical Storage                 │
│  ┌──────────────┐       ┌─────────────┐    │
│  │ SMB Share    │       │ SQL Server  │    │
│  │ - Hot Tier   │       │ - Metadata  │    │
│  │ - Cold Tier  │       │ - Links     │    │
│  └──────────────┘       └─────────────┘    │
└─────────────────────────────────────────────┘
```

## Data Flow

### Upload Flow
```
1. Client → POST /buckets/{bucket}/objects
   ↓
2. StorageController receives IFormFile
   ↓
3. StorageService.UploadAsync()
   ├─→ Generate GUID objectId
   ├─→ ShardingStrategy.ComputeRelativePath()
   ├─→ Compute SHA256 hash
   ├─→ SmbStorageBackend.StoreAsync() → Write to filesystem
   ├─→ ObjectRepository.CreateAsync() → Insert metadata
   └─→ LinkRepository.CreateAsync() → Insert business link
   ↓
4. Return UploadResult with objectId
```

### Download Flow
```
1. Client → GET /buckets/{bucket}/objects/{objectId}
   ↓
2. StorageController
   ↓
3. StorageService.DownloadAsync()
   ├─→ ObjectRepository.GetByBucketAndIdAsync()
   ├─→ SmbStorageBackend.RetrieveAsync() → Read from filesystem
   └─→ ObjectRepository.UpdateLastAccessedAsync()
   ↓
4. Return Stream with ContentType
```

### Tiering Flow
```
1. TieringBackgroundService (every N minutes)
   ↓
2. ObjectRepository.GetObjectsForTieringAsync()
   ├─→ SELECT objects WHERE Tier=Hot AND LastAccessedAt < CutoffDate
   ↓
3. For each object:
   ├─→ SmbStorageBackend.MoveTierAsync()
   │   └─→ Move from /hot/... to /cold/...
   └─→ ObjectRepository.UpdateTierAsync()
       └─→ UPDATE Tier=Cold
```

## Sharding Algorithm

Purpose: Distribute files across directories to avoid filesystem limitations.

```csharp
public string ComputeRelativePath(Guid objectId, string extension, ShardingConfig config)
{
    var guidString = objectId.ToString("N"); // Remove hyphens

    // Config: Levels=3, CharsPerShard=2
    // GUID: b9f78e7d5c224b739a27...

    // Extract shards:
    // shard1 = "b9"
    // shard2 = "f7"
    // shard3 = "8e"

    // Result: "b9/f7/8e/b9f78e7d5c224b739a27file.pdf"

    return Path.Combine(shardParts..., fileName);
}
```

Distribution characteristics:
- **Uniform**: GUIDs provide even distribution
- **Deterministic**: Same GUID always produces same path
- **Scalable**: Supports millions of files per bucket

With default config (Levels=3, CharsPerShard=2):
- Possible combinations: 16² × 16² × 16² = 16,777,216 leaf directories
- Max files per directory: Configurable, typically 100-1000

## Database Schema

### StoredObjects Table
Primary metadata storage:
```sql
CREATE TABLE StoredObjects (
    ObjectId UNIQUEIDENTIFIER PRIMARY KEY,
    Bucket NVARCHAR(255) NOT NULL,
    RelativePath NVARCHAR(1000) NOT NULL,  -- Immutable, never recomputed
    Tier TINYINT NOT NULL,                 -- 0=Hot, 1=Cold
    Length BIGINT NOT NULL,
    ContentType NVARCHAR(255) NOT NULL,
    Hash NVARCHAR(64) NOT NULL,            -- SHA256
    CreatedAt DATETIME2 NOT NULL,
    LastAccessedAt DATETIME2 NULL,
    Tags NVARCHAR(MAX) NULL                -- JSON metadata
);
```

Key indexes:
- `(Bucket, ObjectId)`: Fast lookup by bucket and object
- `(Tier, LastAccessedAt)`: Tiering queries
- `LastAccessedAt`: Access pattern analysis

### ObjectLinks Table
Business context mapping:
```sql
CREATE TABLE ObjectLinks (
    ObjectId UNIQUEIDENTIFIER NOT NULL,
    Channel NVARCHAR(100) NOT NULL,        -- loans, cards, onboarding
    Operation NVARCHAR(100) NOT NULL,      -- agreements, statements, kyc
    BusinessEntityId NVARCHAR(255) NULL,   -- loanId, customerId, etc.
    CreatedAt DATETIME2 NOT NULL,

    FOREIGN KEY (ObjectId) REFERENCES StoredObjects(ObjectId) ON DELETE CASCADE
);
```

Key indexes:
- `ObjectId`: Reverse lookup from object to business context
- `(Channel, Operation)`: Query by business category
- `BusinessEntityId`: Find all objects for a business entity

## Configuration Architecture

### Hierarchical Configuration
```
appsettings.json              # Base configuration
├─→ appsettings.Development.json  # Dev overrides
├─→ appsettings.Production.json   # Prod overrides
└─→ Environment Variables          # Runtime overrides
```

### Options Pattern
```csharp
// Strongly-typed configuration
builder.Services.Configure<SmbStorageOptions>(
    builder.Configuration.GetSection("Storage"));

builder.Services.Configure<TieringOptions>(
    builder.Configuration.GetSection("Tiering"));
```

## Security Considerations

### Current Implementation
- No authentication/authorization (internal service)
- HTTPS recommended for production
- SMB handles file-level ACLs
- SQL Server handles data access control

### Future Enhancements
- JWT authentication
- API key management
- Per-bucket access policies
- Audit logging
- Pre-signed URLs for temporary access

## Scalability Patterns

### Horizontal Scaling
- Stateless API servers
- Load balancer in front
- Shared storage backend (SMB/NFS)
- Shared database (SQL Server)

### Vertical Scaling
- Database: Read replicas for queries
- Storage: Tiered storage (Hot SSD, Cold HDD)
- Caching: Redis for metadata

### Performance Optimizations
1. **Database**:
   - Connection pooling
   - Index tuning
   - Partitioning by bucket or date

2. **Storage**:
   - Async I/O throughout
   - Streaming for large files
   - Range read support

3. **API**:
   - Response compression
   - CDN for public files (future)
   - Caching headers

## Disaster Recovery

### Backup Strategy
1. **Database**: Point-in-time recovery
2. **Storage**: Snapshots + replication
3. **Configuration**: Version controlled

### Recovery Procedures
1. Restore database from backup
2. Mount backup storage snapshots
3. Verify file integrity via hash validation
4. Resume operations

### High Availability
- Database: Always On Availability Groups
- Storage: RAID or distributed filesystem
- API: Multiple instances behind load balancer

## Migration Strategy

### Phase 1: Current (SMB)
- Local SMB mount
- Direct file I/O
- Simple and reliable

### Phase 2: Object Storage (SeaweedFS/MinIO)
1. Implement new backend: `SeaweedFSBackend : IFileStorageBackend`
2. Update configuration: `Storage:Backend = "SeaweedFS"`
3. Migrate existing files (background job or manual)
4. Switch traffic to new backend
5. Decommission old storage

No API changes required. Consumers unaffected.

## Testing Strategy

### Unit Tests
- ShardingStrategy: Verify distribution
- Repositories: Mock database
- Backends: Mock filesystem/API

### Integration Tests
- Database operations
- File storage operations
- End-to-end API tests

### Performance Tests
- Upload throughput
- Download latency
- Concurrent operations
- Tiering efficiency

## Monitoring & Observability

### Metrics to Track
- Upload/download rates
- Storage usage (hot vs cold)
- API latency (p50, p95, p99)
- Error rates
- Tiering efficiency

### Logging
- Structured logging (Serilog)
- Log levels: Debug, Info, Warning, Error
- Correlation IDs for request tracing

### Alerting
- Disk space thresholds
- API error rate spikes
- Database connection failures
- Tiering job failures

## Future Enhancements

1. **Multi-region replication**
2. **Object versioning**
3. **Lifecycle policies** (auto-delete after N days)
4. **Multipart uploads** for large files
5. **Pre-signed URLs** for direct client access
6. **Event notifications** (webhooks on upload/delete)
7. **Storage classes** (beyond hot/cold)
8. **Deduplication** (hash-based)
9. **Encryption at rest**
10. **Cross-bucket copy/move**
