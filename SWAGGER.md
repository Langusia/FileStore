# Swagger API Documentation

The FileStore API provides comprehensive Swagger/OpenAPI documentation for all endpoints.

## Accessing Swagger UI

### Development
```
http://localhost:5000/swagger
```

### Production
```
https://filestore.company.com/swagger
```

## OpenAPI Specification

The OpenAPI (Swagger) JSON specification is available at:
```
http://localhost:5000/api-docs/v1/swagger.json
```

## Features

### Enhanced Documentation
- ✅ **XML Comments** - Comprehensive descriptions for all endpoints
- ✅ **Request/Response Examples** - Clear examples for all operations
- ✅ **Parameter Descriptions** - Detailed parameter documentation
- ✅ **Response Codes** - All possible HTTP status codes documented
- ✅ **Operation IDs** - Stable IDs for client code generation
- ✅ **Tags** - Endpoints grouped by functionality
- ✅ **Schemas** - Full model documentation with examples

### Swagger UI Features
- **Try It Out** - Test API endpoints directly from the browser
- **Deep Linking** - Share links to specific operations
- **Filter** - Search through operations
- **Request Duration** - See API response times
- **Model Expansion** - Inspect request/response models
- **Server Selection** - Switch between Dev/Production environments

## API Endpoints

### Storage Operations

#### Upload Object
```http
POST /buckets/{bucket}/objects
Content-Type: multipart/form-data

Parameters:
- bucket (path, required): Bucket name
- file (form, required): File to upload (max 100MB)
- channel (form, required): Business channel (e.g., "loans", "cards")
- operation (form, required): Business operation (e.g., "agreements", "statements")
- businessEntityId (form, optional): Business entity ID
- tags (form, optional): JSON metadata tags

Response 201:
{
  "objectId": "b9f78e7d-5c22-4b73-9a27-...",
  "bucket": "documents",
  "size": 102400,
  "hash": "a3f5d9...",
  "contentType": "application/pdf",
  "createdAt": "2025-11-27T10:30:00Z"
}
```

#### Download Object
```http
GET /buckets/{bucket}/objects/{objectId}

Parameters:
- bucket (path, required): Bucket name
- objectId (path, required): Object ID (GUID)

Response 200:
File stream with appropriate Content-Type header
Supports HTTP Range requests
```

#### Get Metadata
```http
HEAD /buckets/{bucket}/objects/{objectId}

Parameters:
- bucket (path, required): Bucket name
- objectId (path, required): Object ID (GUID)

Response 200:
Headers:
- X-Object-Id: b9f78e7d-5c22-4b73-9a27-...
- X-Content-Length: 102400
- X-Content-Type: application/pdf
- X-Hash: a3f5d9...
- X-Tier: Hot
- X-Created-At: 2025-11-27T10:30:00Z
- X-Last-Accessed-At: 2025-11-27T14:15:00Z
```

#### Delete Object
```http
DELETE /buckets/{bucket}/objects/{objectId}

Parameters:
- bucket (path, required): Bucket name
- objectId (path, required): Object ID (GUID)

Response 204:
No content (successful deletion)
```

#### List Objects
```http
GET /buckets/{bucket}/objects?maxKeys=100&continuationToken=...

Parameters:
- bucket (path, required): Bucket name
- prefix (query, optional): Prefix filter (reserved for future)
- continuationToken (query, optional): Pagination token
- maxKeys (query, optional): Max results (default: 1000, max: 1000)

Response 200:
{
  "bucket": "documents",
  "objects": [
    {
      "objectId": "b9f78e7d-5c22-4b73-9a27-...",
      "bucket": "documents",
      "size": 102400,
      "contentType": "application/pdf",
      "hash": "a3f5d9...",
      "tier": 0,
      "createdAt": "2025-11-27T10:30:00Z",
      "lastAccessedAt": "2025-11-27T14:15:00Z"
    }
  ],
  "isTruncated": false,
  "nextContinuationToken": null
}
```

#### Change Tier
```http
POST /buckets/{bucket}/objects/{objectId}/tier
Content-Type: application/json

{
  "tier": 1
}

Parameters:
- bucket (path, required): Bucket name
- objectId (path, required): Object ID (GUID)
- tier (body, required): 0=Hot, 1=Cold

Response 204:
No content (successful tier change)
```

### Health
```http
GET /health

Response 200:
{
  "status": "healthy",
  "timestamp": "2025-11-27T15:30:00Z"
}
```

## Client Code Generation

The OpenAPI specification can be used to generate client SDKs:

### Using OpenAPI Generator
```bash
# Install OpenAPI Generator
npm install -g @openapitools/openapi-generator-cli

# Download spec
curl http://localhost:5000/api-docs/v1/swagger.json -o filestore-api.json

# Generate C# client
openapi-generator-cli generate \
  -i filestore-api.json \
  -g csharp \
  -o ./FileStore.Client

# Generate TypeScript client
openapi-generator-cli generate \
  -i filestore-api.json \
  -g typescript-axios \
  -o ./filestore-client-ts

# Generate Python client
openapi-generator-cli generate \
  -i filestore-api.json \
  -g python \
  -o ./filestore-client-py
```

### Using NSwag (C#)
```bash
# Install NSwag
dotnet tool install -g NSwag.ConsoleCore

# Generate C# client
nswag openapi2csclient \
  /input:http://localhost:5000/api-docs/v1/swagger.json \
  /output:FileStoreClient.cs \
  /namespace:FileStore.Client
```

## Swagger Configuration

The Swagger configuration includes:

```csharp
// Program.cs
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "FileStore API",
        Description = "S3-like internal object storage service",
        Contact = new OpenApiContact
        {
            Name = "FileStore Team",
            Email = "filestore@company.com"
        }
    });

    // Enable XML comments
    options.IncludeXmlComments(xmlPath);

    // Enable annotations
    options.EnableAnnotations();

    // Custom operation IDs
    options.CustomOperationIds(...);

    // Server information
    options.AddServer(new OpenApiServer { ... });
});
```

## Best Practices

1. **Always review the Swagger docs** before integrating
2. **Use the "Try It Out" feature** to test endpoints
3. **Generate clients** from the OpenAPI spec for type safety
4. **Check response schemas** to understand data structures
5. **Review error responses** to handle edge cases

## Security (Future)

When authentication is added:
- JWT Bearer tokens will be required
- Swagger UI will include authorization header input
- OAuth2/OpenID Connect flows will be documented

## Troubleshooting

### Swagger UI not loading
- Check that you're accessing `/swagger` not `/swagger/index.html`
- Ensure the API is running: `curl http://localhost:5000/health`

### XML comments not showing
- Verify `GenerateDocumentationFile` is true in `.csproj`
- Rebuild the project: `dotnet build`
- Check XML file exists in output directory

### "Try It Out" returns CORS error
- CORS is not configured by default (internal API)
- Add CORS policy if needed for browser-based clients

## Additional Resources

- **OpenAPI Specification**: https://swagger.io/specification/
- **Swagger UI**: https://swagger.io/tools/swagger-ui/
- **OpenAPI Generator**: https://openapi-generator.tech/
- **NSwag**: https://github.com/RicoSuter/NSwag
