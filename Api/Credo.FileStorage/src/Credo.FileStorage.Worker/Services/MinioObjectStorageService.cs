using Credo.FileStorage.Worker.Interfaces;
using Minio;
using Minio.DataModel.Args;

namespace Credo.FileStorage.Worker.Services;

public class MinioStorageService : IMinioStorageService
{
    private readonly IMinioClient _minioClient;
    private readonly ILogger<MinioStorageService> _logger;
    private readonly HashSet<string> _createdBuckets = new();
    
    public MinioStorageService(IMinioClient minioClient, ILogger<MinioStorageService> logger)
    {
        _minioClient = minioClient;
        _logger = logger;
    }
    
    public async Task EnsureBucketExistsAsync(string bucketName, CancellationToken cancellationToken = default)
    {
        if (_createdBuckets.Contains(bucketName))
            return;
        
        var beArgs = new BucketExistsArgs().WithBucket(bucketName);
        var exists = await _minioClient.BucketExistsAsync(beArgs, cancellationToken);
        
        if (!exists)
        {
            var mbArgs = new MakeBucketArgs().WithBucket(bucketName);
            await _minioClient.MakeBucketAsync(mbArgs, cancellationToken);
            _logger.LogInformation("Created bucket: {BucketName}", bucketName);
        }
        
        _createdBuckets.Add(bucketName);
    }
    
    public async Task UploadAsync(
        string bucketName,
        string objectKey,
        byte[] content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        await using var stream = new MemoryStream(content);
        
        var putArgs = new PutObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectKey)
            .WithStreamData(stream)
            .WithObjectSize(content.Length)
            .WithContentType(contentType ?? "application/octet-stream");
        
        await _minioClient.PutObjectAsync(putArgs, cancellationToken);
        
        _logger.LogDebug("Uploaded {Bucket}/{Key} ({Size} bytes)", 
            bucketName, objectKey, content.Length);
    }
    
    public async Task<bool> ObjectExistsAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var statArgs = new StatObjectArgs()
                .WithBucket(bucketName)
                .WithObject(objectKey);
            
            await _minioClient.StatObjectAsync(statArgs, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }
}