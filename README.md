# FileStore - Object Storage System

A comprehensive object storage solution built with ASP.NET Core and AWS S3 (or S3-compatible services), featuring transactional metadata persistence in SQL Server.

## Features

- **Generic S3 Integration**: Works with AWS S3, MinIO, DigitalOcean Spaces, and other S3-compatible services
- **Interchangeable Storage Brokers**: Easy to swap storage providers via the broker pattern
- **Transactional Operations**: Ensures consistency between object storage and metadata database
- **Metadata Persistence**: Stores object metadata in SQL Server without storing actual content
- **Smart Naming Strategy**: Automatic bucket naming based on Channel and Operation enums
- **Collision-Free Filenames**: Generates unique filenames using timestamp and GUID
- **RESTful API**: Clean API for Upload, Download, Delete, and List operations
- **NuGet Package**: Reusable library for company-wide adoption

## Architecture

### Project Structure

```
FileStore/
├── src/
│   ├── FileStore.Storage/          # NuGet Package Library
│   │   ├── Brokers/                # Storage broker implementations
│   │   ├── Data/                   # EF Core DbContext and migrations
│   │   ├── Enums/                  # Channel and Operation enums
│   │   ├── Extensions/             # DI extensions
│   │   ├── Models/                 # Data models and DTOs
│   │   └── Services/               # Business logic services
│   └── FileStore.API/              # ASP.NET Core Web API
│       ├── Controllers/            # API controllers
│       └── DTOs/                   # API data transfer objects
└── README.md
```

### Key Components

1. **Storage Broker Pattern**: Abstraction layer for different storage providers
2. **Naming Strategy**: Consistent bucket and object key generation
3. **Metadata Service**: SQL Server persistence for object metadata
4. **Transaction Management**: Ensures data consistency

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- SQL Server (LocalDB, Express, or full version)
- AWS Account with S3 access (or MinIO/compatible service)

### Installation

#### 1. Clone the Repository

```bash
git clone https://github.com/yourcompany/FileStore.git
cd FileStore
```

#### 2. Install the NuGet Package (For Library Usage)

```bash
dotnet add package FileStore.Storage
```

Or build from source:

```bash
cd src/FileStore.Storage
dotnet pack -c Release
```

#### 3. Database Setup

Run the SQL migration script:

```bash
sqlcmd -S localhost -i src/FileStore.Storage/Data/Migrations/001_InitialCreate.sql
```

Or use EF Core migrations:

```bash
cd src/FileStore.API
dotnet ef database update --project ../FileStore.Storage
```

#### 4. Configure AWS Credentials

**Option A: AWS Credentials File** (~/.aws/credentials)
```ini
[default]
aws_access_key_id = YOUR_ACCESS_KEY
aws_secret_access_key = YOUR_SECRET_KEY
```

**Option B: Environment Variables**
```bash
export AWS_ACCESS_KEY_ID=YOUR_ACCESS_KEY
export AWS_SECRET_ACCESS_KEY=YOUR_SECRET_KEY
export AWS_REGION=us-east-1
```

#### 5. Update Configuration

Edit `src/FileStore.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "FileStore": "Server=localhost;Database=FileStoreDb;Integrated Security=true;"
  },
  "AWS": {
    "Region": "us-east-1",
    "ServiceURL": null
  }
}
```

For MinIO or other S3-compatible services:

```json
{
  "AWS": {
    "Region": "us-east-1",
    "ServiceURL": "http://localhost:9000"
  }
}
```

#### 6. Run the API

```bash
cd src/FileStore.API
dotnet run
```

The API will be available at:
- HTTP: `http://localhost:5000`
- HTTPS: `https://localhost:5001`
- Swagger UI: `https://localhost:5001/swagger`

## Usage

### API Endpoints

#### 1. Upload Object

```bash
POST /api/storage/upload
Content-Type: multipart/form-data

Parameters:
- File: [binary]
- Channel: [1-7] (1=Web, 2=Mobile, 3=Desktop, 4=API, 5=Integration, 6=Batch, 7=Admin)
- Operation: [1-9] (1=UserUploads, 2=Documents, 3=Images, 4=Videos, etc.)
- TrackSize: true/false (optional, default: true)
- Metadata: JSON object (optional)
```

Example with curl:

```bash
curl -X POST "https://localhost:5001/api/storage/upload" \
  -F "File=@/path/to/file.pdf" \
  -F "Channel=1" \
  -F "Operation=2"
```

Response:
```json
{
  "objectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "objectKey": "invoice_20250109150623_a1b2c3d4.pdf",
  "bucketName": "web-documents",
  "fullStorageUrl": "https://web-documents.s3.us-east-1.amazonaws.com/invoice_20250109150623_a1b2c3d4.pdf",
  "sizeInBytes": 245632,
  "eTag": "\"d41d8cd98f00b204e9800998ecf8427e\"",
  "uploadedAt": "2025-01-09T15:06:23.456Z"
}
```

#### 2. Download Object

```bash
GET /api/storage/download/{objectId}
```

Example:

```bash
curl -O "https://localhost:5001/api/storage/download/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

#### 3. Get Object Metadata

```bash
GET /api/storage/metadata/{objectId}
```

Response:
```json
{
  "objectId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
  "objectKey": "invoice_20250109150623_a1b2c3d4.pdf",
  "bucketName": "web-documents",
  "originalFileName": "invoice.pdf",
  "fullStorageUrl": "https://web-documents.s3.us-east-1.amazonaws.com/invoice_20250109150623_a1b2c3d4.pdf",
  "contentType": "application/pdf",
  "sizeInBytes": 245632,
  "createdAt": "2025-01-09T15:06:23.456Z",
  "lastAccessedAt": "2025-01-09T16:30:12.789Z"
}
```

#### 4. Delete Object

```bash
DELETE /api/storage/{objectId}
```

Example:

```bash
curl -X DELETE "https://localhost:5001/api/storage/a1b2c3d4-e5f6-7890-abcd-ef1234567890"
```

#### 5. List Objects

```bash
GET /api/storage/list?channel=1&operation=2&page=1&pageSize=50
```

### Using the NuGet Package

#### 1. Install the Package

```bash
dotnet add package FileStore.Storage
```

#### 2. Configure in Your Application

```csharp
using FileStore.Storage.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add FileStore services
builder.Services.AddFileStore(builder.Configuration);

var app = builder.Build();
```

#### 3. Use in Your Code

```csharp
using FileStore.Storage.Enums;
using FileStore.Storage.Models;
using FileStore.Storage.Services;

public class MyService
{
    private readonly IObjectStorageService _storageService;

    public MyService(IObjectStorageService storageService)
    {
        _storageService = storageService;
    }

    public async Task<UploadResponse> UploadFileAsync(Stream fileStream, string fileName)
    {
        var request = new UploadRequest
        {
            Content = fileStream,
            FileName = fileName,
            Channel = Channel.Web,
            Operation = Operation.UserUploads,
            ContentType = "application/octet-stream",
            TrackSize = true
        };

        return await _storageService.UploadObjectAsync(request);
    }

    public async Task<GetObjectResponse> DownloadFileAsync(string objectId)
    {
        return await _storageService.GetObjectAsync(objectId);
    }

    public async Task DeleteFileAsync(string objectId)
    {
        await _storageService.DeleteObjectAsync(objectId);
    }
}
```

## Enums Reference

### Channel Enum

| Value | Int | String | Description |
|-------|-----|--------|-------------|
| Web | 1 | "web" | Web applications |
| Mobile | 2 | "mobile" | Mobile applications |
| Desktop | 3 | "desktop" | Desktop applications |
| API | 4 | "api" | API integrations |
| Integration | 5 | "integration" | Third-party integrations |
| Batch | 6 | "batch" | Batch processes |
| Admin | 7 | "admin" | Administrative operations |

### Operation Enum

| Value | Int | String | Description |
|-------|-----|--------|-------------|
| UserUploads | 1 | "user-uploads" | User-uploaded files |
| Documents | 2 | "documents" | Document files |
| Images | 3 | "images" | Image files |
| Videos | 4 | "videos" | Video files |
| Exports | 5 | "exports" | Exported data |
| Reports | 6 | "reports" | Generated reports |
| Backups | 7 | "backups" | Backup files |
| Temp | 8 | "temp" | Temporary files |
| Archives | 9 | "archives" | Archived files |

## Bucket Naming Convention

Buckets are automatically named using the pattern: `{channel}-{operation}`

Examples:
- Web + Documents = `web-documents`
- Mobile + Images = `mobile-images`
- API + UserUploads = `api-user-uploads`

## Filename Generation

Files are automatically renamed to avoid collisions using the pattern:
`{sanitized_filename}_{timestamp}_{guid}{extension}`

Example:
- Original: `my invoice (2024).pdf`
- Generated: `my_invoice_2024_20250109150623_a1b2c3d4.pdf`

## Database Schema

### StorageBuckets Table

Stores bucket metadata:
- `Id`: Primary key
- `BucketName`: Unique bucket name
- `ChannelId`, `ChannelName`: Channel enum values
- `OperationId`, `OperationName`: Operation enum values
- `CreatedAt`, `LastAccessedAt`: Timestamps

### StorageObjects Table

Stores object metadata (NOT content):
- `Id`: Primary key
- `ObjectId`: Unique GUID identifier
- `BucketId`: Foreign key to StorageBuckets
- `ObjectKey`: S3 object key
- `OriginalFileName`: Original file name
- `FullStorageUrl`: Complete S3 URL
- `ContentType`: MIME type
- `SizeInBytes`: File size (optional)
- `ETag`: S3 ETag
- `IsDeleted`: Soft delete flag
- Timestamps: `CreatedAt`, `LastModifiedAt`, `LastAccessedAt`, `DeletedAt`

### BucketTags & ObjectTags Tables (Scope 2)

For lifecycle management and hot/cold storage transitions:
- Tag key-value pairs
- Used for S3 lifecycle policies

## Advanced Configuration

### Custom Naming Strategy

Implement `INamingStrategy` to customize bucket and filename generation:

```csharp
public class CustomNamingStrategy : INamingStrategy
{
    public string GenerateBucketName(Channel channel, Operation operation)
    {
        return $"mycompany-{channel.ToStringValue()}-{operation.ToStringValue()}";
    }

    public string GenerateObjectKey(string originalFileName)
    {
        // Your custom logic
    }

    public string SanitizeFileName(string fileName)
    {
        // Your custom logic
    }
}

// Register in DI
services.AddScoped<INamingStrategy, CustomNamingStrategy>();
```

### Custom Storage Broker

Implement `IObjectStorageBroker` for other storage providers:

```csharp
public class AzureBlobStorageBroker : IObjectStorageBroker
{
    // Implement interface methods for Azure Blob Storage
}

// Register in DI
services.AddScoped<IObjectStorageBroker, AzureBlobStorageBroker>();
```

## Scope 2: Lifecycle Management (Future)

The system is prepared for lifecycle management features:

- **Hot/Cold Storage Transitions**: Automatically move objects between storage tiers
- **TTL (Time To Live)**: Automatically delete objects after a specified period
- **Tagging System**: Already implemented in database schema
- **S3 Lifecycle Policies**: Can be configured using tags

## Performance Considerations

1. **Size Tracking**: Disable `TrackSize` for large files to improve performance
2. **Database Indexing**: Properly indexed for fast queries
3. **Soft Deletes**: Objects are soft-deleted for potential recovery
4. **Streaming**: Uses streams for memory-efficient file handling

## Security Best Practices

1. **IAM Roles**: Use IAM roles instead of access keys in production
2. **Bucket Policies**: Restrict S3 bucket access
3. **HTTPS Only**: Always use HTTPS for API communication
4. **SQL Injection**: Uses parameterized queries via EF Core
5. **Input Validation**: All inputs are validated

## Troubleshooting

### Common Issues

1. **"Unable to connect to database"**
   - Check connection string in appsettings.json
   - Ensure SQL Server is running

2. **"Access Denied" from S3**
   - Verify AWS credentials
   - Check IAM permissions (s3:PutObject, s3:GetObject, s3:DeleteObject)

3. **"Bucket already exists but is not owned by you"**
   - Choose a different bucket naming strategy
   - Buckets must be globally unique in AWS S3

## Building the NuGet Package

```bash
cd src/FileStore.Storage
dotnet pack -c Release -o ../../artifacts
```

The NuGet package will be generated in the `artifacts/` directory.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Submit a pull request

## License

MIT License - See LICENSE file for details

## Support

For issues and questions:
- GitHub Issues: https://github.com/yourcompany/FileStore/issues
- Email: support@yourcompany.com

## Roadmap

- [ ] Scope 2: Lifecycle management with hot/cold storage
- [ ] Azure Blob Storage broker implementation
- [ ] Google Cloud Storage broker implementation
- [ ] Batch operations (bulk upload/delete)
- [ ] Pre-signed URLs for direct client uploads
- [ ] CDN integration
- [ ] Compression support
- [ ] Virus scanning integration
