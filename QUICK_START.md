# Quick Start Guide

Get FileStore up and running in minutes.

## Prerequisites

- .NET 8.0 SDK
- SQL Server (or use Docker)
- Basic understanding of REST APIs

## Option 1: Docker Compose (Recommended for Testing)

The fastest way to get started:

```bash
# Clone the repository
cd FileStore

# Start SQL Server and FileStore API
docker-compose up -d

# Wait for services to start (about 30 seconds)
docker-compose logs -f filestore-api

# When you see "Application started", press Ctrl+C

# Test the API
./scripts/test-api.sh

# Access Swagger UI
open http://localhost:5000/swagger
```

That's it! FileStore is now running with:
- API on http://localhost:5000
- SQL Server on localhost:1433
- Storage in ./storage/hot and ./storage/cold

## Option 2: Manual Setup (Development)

### Step 1: Database Setup

Using SQL Server:
```bash
# Create database
sqlcmd -S localhost -U sa -P YourPassword123! -Q "CREATE DATABASE FileStore"

# Run schema
sqlcmd -S localhost -U sa -P YourPassword123! -d FileStore -i database/schema.sql
```

Or use Docker just for SQL Server:
```bash
docker run -d \
  --name filestore-sqlserver \
  -e 'ACCEPT_EULA=Y' \
  -e 'SA_PASSWORD=YourPassword123!' \
  -p 1433:1433 \
  mcr.microsoft.com/mssql/server:2022-latest

# Wait 30 seconds, then run schema
sqlcmd -S localhost -U sa -P YourPassword123! -d master -Q "CREATE DATABASE FileStore"
sqlcmd -S localhost -U sa -P YourPassword123! -d FileStore -i database/schema.sql
```

### Step 2: Configure Storage Paths

Create local directories:
```bash
mkdir -p ./storage/hot ./storage/cold
```

Update `appsettings.Development.json` if needed (default paths are `./storage/hot` and `./storage/cold`).

### Step 3: Run the Application

```bash
# Build
dotnet build

# Run
dotnet run --project src/FileStore.API

# Or use watch for development
dotnet watch run --project src/FileStore.API
```

### Step 4: Test the API

Open another terminal:
```bash
# Upload a file
echo "Hello FileStore" > test.txt

curl -X POST http://localhost:5000/buckets/test-bucket/objects \
  -F "file=@test.txt" \
  -F "channel=test" \
  -F "operation=demo"

# Response will include objectId
# Example: {"objectId":"b9f78e7d-5c22-4b73-9a27-...","bucket":"test-bucket",...}

# Download the file (replace {objectId} with actual ID)
curl http://localhost:5000/buckets/test-bucket/objects/{objectId}
```

Or use the test script:
```bash
chmod +x scripts/test-api.sh
./scripts/test-api.sh
```

### Step 5: Explore Swagger UI

Open http://localhost:5000/swagger in your browser to see all available endpoints.

## Common Operations

### Upload a File

```bash
curl -X POST http://localhost:5000/buckets/documents/objects \
  -F "file=@/path/to/document.pdf" \
  -F "channel=loans" \
  -F "operation=agreements" \
  -F "businessEntityId=loan-12345"
```

### Download a File

```bash
curl http://localhost:5000/buckets/documents/objects/{objectId} \
  -o downloaded-file.pdf
```

### Get File Metadata

```bash
curl -I http://localhost:5000/buckets/documents/objects/{objectId}
```

### List Files in Bucket

```bash
curl "http://localhost:5000/buckets/documents/objects?maxKeys=10"
```

### Delete a File

```bash
curl -X DELETE http://localhost:5000/buckets/documents/objects/{objectId}
```

### Change Storage Tier

```bash
curl -X POST http://localhost:5000/buckets/documents/objects/{objectId}/tier \
  -H "Content-Type: application/json" \
  -d '{"tier": 1}'
```

Note: Tier values are `0` for Hot, `1` for Cold.

## Configuration

Key settings in `appsettings.Development.json`:

```json
{
  "Database": {
    "ConnectionString": "Server=localhost;Database=FileStore;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True"
  },
  "Storage": {
    "Backend": "SMB",
    "HotRootPath": "./storage/hot",
    "ColdRootPath": "./storage/cold",
    "MaxFileSizeMb": 100,
    "AllowedContentTypes": ["application/pdf", "image/jpeg", "image/png"]
  },
  "Tiering": {
    "Enabled": false,  // Disabled in development
    "ColdAfterDays": 365
  }
}
```

## Verify Installation

Check that everything is working:

```bash
# Health check
curl http://localhost:5000/health

# Should return: {"status":"healthy","timestamp":"2025-11-27T..."}

# Check database connection (upload a test file)
echo "test" > /tmp/test.txt
RESULT=$(curl -s -X POST http://localhost:5000/buckets/test/objects \
  -F "file=@/tmp/test.txt" \
  -F "channel=test" \
  -F "operation=test")

echo $RESULT | jq .

# Check storage (verify file was written)
ls -lh ./storage/hot/*/*/*/*
```

## Troubleshooting

### Connection refused
- Ensure SQL Server is running: `docker ps` or check SQL Server service
- Check connection string in appsettings.json

### Permission denied on storage paths
```bash
# Linux/Mac
chmod -R 755 ./storage
```

### Database not found
```bash
# Recreate database
sqlcmd -S localhost -U sa -P YourPassword123! -Q "DROP DATABASE IF EXISTS FileStore; CREATE DATABASE FileStore"
sqlcmd -S localhost -U sa -P YourPassword123! -d FileStore -i database/schema.sql
```

### Port 5000 already in use
Edit `src/FileStore.API/Properties/launchSettings.json` or set environment variable:
```bash
export ASPNETCORE_URLS="http://localhost:5001"
dotnet run --project src/FileStore.API
```

## Next Steps

- Read [README.md](README.md) for detailed documentation
- Read [ARCHITECTURE.md](ARCHITECTURE.md) to understand the design
- Read [DEPLOYMENT.md](DEPLOYMENT.md) for production deployment
- Explore the code in `src/` directories
- Check out the database schema in `database/schema.sql`

## Useful Commands

```bash
# Build solution
dotnet build

# Run tests (when added)
dotnet test

# Clean build artifacts
dotnet clean

# Restore packages
dotnet restore

# Format code
dotnet format

# Check for outdated packages
dotnet list package --outdated
```

## Development Tips

1. **Enable hot reload**:
   ```bash
   dotnet watch run --project src/FileStore.API
   ```

2. **View logs in real-time**:
   - Console output shows all requests
   - Debug level logging enabled in Development

3. **Database queries**:
   ```sql
   -- View all objects
   SELECT * FROM StoredObjects;

   -- View all links
   SELECT * FROM ObjectLinks;

   -- Join to see full picture
   SELECT o.*, l.Channel, l.Operation, l.BusinessEntityId
   FROM StoredObjects o
   LEFT JOIN ObjectLinks l ON o.ObjectId = l.ObjectId;
   ```

4. **Inspect physical files**:
   ```bash
   tree ./storage
   ```

5. **Test tiering**:
   - Enable tiering in appsettings.Development.json
   - Set ColdAfterDays to 0
   - Upload a file
   - Wait for tiering interval (default 60 minutes, or set to 1 minute for testing)
   - Check that file moved from ./storage/hot to ./storage/cold

## Integration with Your Application

### .NET Client Example

```csharp
using var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5000") };

// Upload
using var form = new MultipartFormDataContent();
form.Add(new StreamContent(fileStream), "file", fileName);
form.Add(new StringContent("loans"), "channel");
form.Add(new StringContent("agreements"), "operation");
form.Add(new StringContent("loan-12345"), "businessEntityId");

var response = await httpClient.PostAsync("/buckets/documents/objects", form);
var result = await response.Content.ReadFromJsonAsync<UploadResult>();
var objectId = result.ObjectId;

// Download
var downloadStream = await httpClient.GetStreamAsync($"/buckets/documents/objects/{objectId}");
```

### JavaScript Client Example

```javascript
// Upload
const formData = new FormData();
formData.append('file', fileInput.files[0]);
formData.append('channel', 'loans');
formData.append('operation', 'agreements');
formData.append('businessEntityId', 'loan-12345');

const response = await fetch('http://localhost:5000/buckets/documents/objects', {
  method: 'POST',
  body: formData
});

const result = await response.json();
const objectId = result.objectId;

// Download
window.location.href = `http://localhost:5000/buckets/documents/objects/${objectId}`;
```

## Need Help?

- Check the [README.md](README.md) for comprehensive documentation
- Review [ARCHITECTURE.md](ARCHITECTURE.md) for design details
- See [DEPLOYMENT.md](DEPLOYMENT.md) for production setup
- Examine the code - it's well-commented and organized
