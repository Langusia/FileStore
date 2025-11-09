# FileStore Development Guide

This guide will help you set up and run FileStore locally for development.

## Quick Start with Docker

The fastest way to get started is using Docker Compose, which provides both SQL Server and MinIO (S3-compatible storage).

### 1. Start Infrastructure

```bash
# Start SQL Server and MinIO
docker-compose up -d

# Check if services are running
docker-compose ps
```

Services will be available at:
- **SQL Server**: `localhost:1433` (user: `sa`, password: `YourStrong@Passw0rd`)
- **MinIO API**: `http://localhost:9000`
- **MinIO Console**: `http://localhost:9001` (user: `minioadmin`, password: `minioadmin123`)

### 2. Configure AWS Credentials for MinIO

Create or edit `~/.aws/credentials`:

```ini
[local-minio]
aws_access_key_id = minioadmin
aws_secret_access_key = minioadmin123
```

Create or edit `~/.aws/config`:

```ini
[profile local-minio]
region = us-east-1
output = json
```

### 3. Apply Database Migrations

```bash
cd src/FileStore.API

# Using dotnet ef (recommended)
dotnet ef database update --project ../FileStore.Storage

# Or manually with SQL script
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd" \
  -i ../FileStore.Storage/Data/Migrations/001_InitialCreate.sql
```

### 4. Run the API

```bash
cd src/FileStore.API

# Run with MinIO configuration
dotnet run --launch-profile "FileStore.API"

# Or with custom appsettings
cp ../../appsettings.MinIO.json appsettings.json
dotnet run
```

### 5. Access Swagger UI

Open your browser to: `https://localhost:5001/swagger`

### 6. Test Upload

```bash
# Create a test file
echo "Hello FileStore!" > test.txt

# Upload via API
curl -X POST "https://localhost:5001/api/storage/upload" \
  -k \
  -F "File=@test.txt" \
  -F "Channel=1" \
  -F "Operation=2"
```

## Development Without Docker

If you prefer to install services locally:

### SQL Server

**Windows:**
- Install SQL Server Express or LocalDB
- Connection string: `Server=(localdb)\\mssqllocaldb;Database=FileStoreDb;Integrated Security=true;`

**macOS/Linux:**
```bash
# Using Docker for SQL Server only
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" \
  -p 1433:1433 --name sqlserver \
  -d mcr.microsoft.com/mssql/server:2022-latest
```

### AWS S3

Instead of MinIO, use real AWS S3:

1. Create an S3 bucket in your AWS account
2. Configure AWS credentials:
   ```bash
   aws configure
   ```
3. Update `appsettings.json`:
   ```json
   {
     "AWS": {
       "Profile": "default",
       "Region": "us-east-1",
       "ServiceURL": null
     }
   }
   ```

## Project Structure

```
FileStore/
├── src/
│   ├── FileStore.Storage/              # NuGet Package
│   │   ├── Brokers/
│   │   │   ├── IObjectStorageBroker.cs      # Broker interface
│   │   │   └── S3ObjectStorageBroker.cs     # S3 implementation
│   │   ├── Data/
│   │   │   ├── FileStoreDbContext.cs        # EF Core context
│   │   │   └── Migrations/                   # SQL migrations
│   │   ├── Enums/
│   │   │   ├── Channel.cs                   # Channel enum
│   │   │   └── Operation.cs                 # Operation enum
│   │   ├── Extensions/
│   │   │   └── ServiceCollectionExtensions.cs # DI setup
│   │   ├── Models/
│   │   │   ├── StorageBucket.cs             # Bucket metadata
│   │   │   ├── StorageObject.cs             # Object metadata
│   │   │   ├── UploadRequest.cs             # Request models
│   │   │   └── StorageOperationModels.cs    # Response models
│   │   └── Services/
│   │       ├── NamingStrategy.cs            # Bucket/filename generation
│   │       └── ObjectStorageService.cs      # Main service
│   └── FileStore.API/                  # Web API
│       ├── Controllers/
│       │   └── StorageController.cs         # REST endpoints
│       ├── DTOs/
│       │   ├── UploadRequestDto.cs          # API DTOs
│       │   └── ObjectResponseDto.cs
│       └── Program.cs                       # API startup
├── docker-compose.yml                  # Local dev infrastructure
├── README.md                           # User documentation
└── DEVELOPMENT.md                      # This file
```

## Building the NuGet Package

```bash
cd src/FileStore.Storage

# Build
dotnet build -c Release

# Pack
dotnet pack -c Release -o ../../artifacts

# The package will be at: artifacts/FileStore.Storage.1.0.0.nupkg
```

### Publishing to NuGet.org

```bash
dotnet nuget push artifacts/FileStore.Storage.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

### Publishing to Private NuGet Feed

```bash
dotnet nuget push artifacts/FileStore.Storage.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://your-private-feed.com/nuget
```

## Testing

### Manual Testing with Swagger

1. Navigate to `https://localhost:5001/swagger`
2. Use the interactive API documentation to test endpoints

### Manual Testing with cURL

**Upload:**
```bash
curl -X POST "https://localhost:5001/api/storage/upload" \
  -k \
  -F "File=@test.pdf" \
  -F "Channel=1" \
  -F "Operation=2" \
  -F "TrackSize=true"
```

**Get Metadata:**
```bash
curl -k "https://localhost:5001/api/storage/metadata/{objectId}"
```

**Download:**
```bash
curl -k -O "https://localhost:5001/api/storage/download/{objectId}"
```

**Delete:**
```bash
curl -X DELETE -k "https://localhost:5001/api/storage/{objectId}"
```

**List:**
```bash
curl -k "https://localhost:5001/api/storage/list?page=1&pageSize=10"
```

### Testing with Postman

Import the following collection:

```json
{
  "info": {
    "name": "FileStore API",
    "schema": "https://schema.getpostman.com/json/collection/v2.1.0/collection.json"
  },
  "item": [
    {
      "name": "Upload Object",
      "request": {
        "method": "POST",
        "url": "{{baseUrl}}/api/storage/upload",
        "body": {
          "mode": "formdata",
          "formdata": [
            { "key": "File", "type": "file", "src": "" },
            { "key": "Channel", "value": "1", "type": "text" },
            { "key": "Operation", "value": "2", "type": "text" }
          ]
        }
      }
    }
  ]
}
```

## Database Management

### View Data

```bash
# Connect to SQL Server
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd"

# Query buckets
SELECT * FROM StorageBuckets;

# Query objects
SELECT * FROM StorageObjects WHERE IsDeleted = 0;

# Query with joins
SELECT
    o.ObjectId,
    o.OriginalFileName,
    o.ObjectKey,
    b.BucketName,
    o.SizeInBytes,
    o.CreatedAt
FROM StorageObjects o
JOIN StorageBuckets b ON o.BucketId = b.Id
WHERE o.IsDeleted = 0
ORDER BY o.CreatedAt DESC;
```

### Reset Database

```bash
# Drop and recreate
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd" -Q "DROP DATABASE FileStoreDb"
sqlcmd -S localhost,1433 -U sa -P "YourStrong@Passw0rd" -Q "CREATE DATABASE FileStoreDb"

# Apply migrations
cd src/FileStore.API
dotnet ef database update --project ../FileStore.Storage
```

## Debugging

### Enable Detailed Logging

Update `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.EntityFrameworkCore.Database.Command": "Information",
      "FileStore": "Trace"
    }
  }
}
```

### Debug in Visual Studio / VS Code

**Visual Studio:**
1. Open `FileStore.sln`
2. Set `FileStore.API` as startup project
3. Press F5

**VS Code:**
1. Open the `FileStore` folder
2. Install C# Dev Kit extension
3. Press F5 and select `.NET Core Launch (web)`

### Common Issues

**1. "Cannot connect to SQL Server"**
```bash
# Check if SQL Server is running
docker-compose ps sqlserver

# View logs
docker-compose logs sqlserver

# Restart
docker-compose restart sqlserver
```

**2. "MinIO connection refused"**
```bash
# Check MinIO status
docker-compose ps minio

# View logs
docker-compose logs minio

# Access MinIO console
open http://localhost:9001
```

**3. "Bucket doesn't exist"**
```bash
# List buckets in MinIO
docker exec -it filestore-minio mc ls minio

# Create bucket manually
docker exec -it filestore-minio mc mb minio/test-bucket
```

**4. "EF Core migration failed"**
```bash
# Remove migrations
dotnet ef migrations remove --project src/FileStore.Storage

# Add new migration
dotnet ef migrations add InitialCreate --project src/FileStore.Storage

# Apply
dotnet ef database update --project src/FileStore.Storage
```

## Code Style

- Follow C# coding conventions
- Use meaningful variable names
- Add XML documentation comments to public APIs
- Keep methods focused and small
- Use async/await properly
- Handle exceptions appropriately

## Git Workflow

```bash
# Create feature branch
git checkout -b feature/my-feature

# Make changes and commit
git add .
git commit -m "feat: add new feature"

# Push to remote
git push origin feature/my-feature

# Create pull request on GitHub
```

## Contribution Guidelines

1. Fork the repository
2. Create a feature branch
3. Write clean, documented code
4. Test your changes thoroughly
5. Submit a pull request with a clear description

## Performance Tips

1. **Disable size tracking for large files:**
   ```csharp
   var request = new UploadRequest
   {
       TrackSize = false  // Improves performance
   };
   ```

2. **Use pagination when listing objects:**
   ```csharp
   var objects = await _storageService.ListObjectsAsync(
       page: 1,
       pageSize: 50  // Don't use too large page sizes
   );
   ```

3. **Stream files instead of loading into memory:**
   ```csharp
   await using var stream = file.OpenReadStream();
   // Stream is used directly, not loaded into memory
   ```

## Useful Commands

```bash
# Clean and rebuild
dotnet clean && dotnet build

# Watch for changes and auto-rebuild
dotnet watch run --project src/FileStore.API

# Format code
dotnet format

# List all projects
dotnet sln list

# Add project reference
dotnet add src/FileStore.API reference src/FileStore.Storage

# Restore dependencies
dotnet restore
```

## Next Steps

- Implement unit tests
- Add integration tests
- Set up CI/CD pipeline
- Add API versioning
- Implement rate limiting
- Add authentication/authorization
- Create client SDK libraries
