using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;

namespace FileStore.Storage.Brokers;

/// <summary>
/// AWS S3 implementation of the object storage broker.
/// Can also work with S3-compatible services like MinIO, DigitalOcean Spaces, etc.
/// </summary>
public class S3ObjectStorageBroker : IObjectStorageBroker
{
    private readonly IAmazonS3 _s3Client;
    private readonly ILogger<S3ObjectStorageBroker> _logger;
    private readonly string? _serviceUrl;

    public S3ObjectStorageBroker(
        IAmazonS3 s3Client,
        ILogger<S3ObjectStorageBroker> logger,
        string? serviceUrl = null)
    {
        _s3Client = s3Client ?? throw new ArgumentNullException(nameof(s3Client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceUrl = serviceUrl;
    }

    public async Task<(string ETag, long? Size)> UploadObjectAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string? contentType = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Uploading object {ObjectKey} to bucket {BucketName}",
                objectKey,
                bucketName);

            var request = new PutObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey,
                InputStream = content,
                ContentType = contentType ?? "application/octet-stream",
                AutoCloseStream = false
            };

            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    request.Metadata.Add(kvp.Key, kvp.Value);
                }
            }

            var response = await _s3Client.PutObjectAsync(request, cancellationToken);

            _logger.LogInformation(
                "Successfully uploaded object {ObjectKey} to bucket {BucketName}. ETag: {ETag}",
                objectKey,
                bucketName,
                response.ETag);

            return (response.ETag, content.Length);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(
                ex,
                "S3 error uploading object {ObjectKey} to bucket {BucketName}: {ErrorCode}",
                objectKey,
                bucketName,
                ex.ErrorCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error uploading object {ObjectKey} to bucket {BucketName}",
                objectKey,
                bucketName);
            throw;
        }
    }

    public async Task<(Stream Content, string? ContentType, long? Size, Dictionary<string, string>? Metadata)> GetObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Retrieving object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);

            var request = new GetObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            var response = await _s3Client.GetObjectAsync(request, cancellationToken);

            var metadata = new Dictionary<string, string>();
            foreach (var key in response.Metadata.Keys)
            {
                metadata[key] = response.Metadata[key];
            }

            _logger.LogInformation(
                "Successfully retrieved object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);

            return (response.ResponseStream, response.Headers.ContentType, response.ContentLength, metadata);
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchKey")
        {
            _logger.LogWarning(
                "Object {ObjectKey} not found in bucket {BucketName}",
                objectKey,
                bucketName);
            throw new FileNotFoundException(
                $"Object '{objectKey}' not found in bucket '{bucketName}'",
                ex);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(
                ex,
                "S3 error retrieving object {ObjectKey} from bucket {BucketName}: {ErrorCode}",
                objectKey,
                bucketName,
                ex.ErrorCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error retrieving object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);
            throw;
        }
    }

    public async Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation(
                "Deleting object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);

            var request = new DeleteObjectRequest
            {
                BucketName = bucketName,
                Key = objectKey
            };

            await _s3Client.DeleteObjectAsync(request, cancellationToken);

            _logger.LogInformation(
                "Successfully deleted object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(
                ex,
                "S3 error deleting object {ObjectKey} from bucket {BucketName}: {ErrorCode}",
                objectKey,
                bucketName,
                ex.ErrorCode);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error deleting object {ObjectKey} from bucket {BucketName}",
                objectKey,
                bucketName);
            throw;
        }
    }

    public async Task<bool> BucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _s3Client.GetBucketLocationAsync(bucketName, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.ErrorCode == "NoSuchBucket")
        {
            return false;
        }
    }

    public async Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var exists = await BucketExistsAsync(bucketName, cancellationToken);
            if (!exists)
            {
                _logger.LogInformation("Creating bucket {BucketName}", bucketName);

                var request = new PutBucketRequest
                {
                    BucketName = bucketName,
                    UseClientRegion = true
                };

                await _s3Client.PutBucketAsync(request, cancellationToken);

                _logger.LogInformation("Successfully created bucket {BucketName}", bucketName);
            }
        }
        catch (AmazonS3Exception ex)
        {
            _logger.LogError(
                ex,
                "S3 error ensuring bucket {BucketName} exists: {ErrorCode}",
                bucketName,
                ex.ErrorCode);
            throw;
        }
    }

    public string GetObjectUrl(string bucketName, string objectKey)
    {
        if (!string.IsNullOrEmpty(_serviceUrl))
        {
            return $"{_serviceUrl.TrimEnd('/')}/{bucketName}/{objectKey}";
        }

        // Default AWS S3 URL format
        var region = _s3Client.Config.RegionEndpoint?.SystemName ?? "us-east-1";
        return $"https://{bucketName}.s3.{region}.amazonaws.com/{objectKey}";
    }
}
