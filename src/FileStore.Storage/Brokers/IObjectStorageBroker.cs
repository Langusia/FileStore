using FileStore.Storage.Models;

namespace FileStore.Storage.Brokers;

/// <summary>
/// Interface for object storage brokers.
/// Implementations of this interface provide different storage backends (S3, Azure Blob, MinIO, etc.)
/// This allows for interchangeable storage providers.
/// </summary>
public interface IObjectStorageBroker
{
    /// <summary>
    /// Uploads an object to storage.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The key (path) for the object in the bucket.</param>
    /// <param name="content">The content stream to upload.</param>
    /// <param name="contentType">Optional content type.</param>
    /// <param name="metadata">Optional metadata key-value pairs.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload result with ETag and other information.</returns>
    Task<(string ETag, long? Size)> UploadObjectAsync(
        string bucketName,
        string objectKey,
        Stream content,
        string? contentType = null,
        Dictionary<string, string>? metadata = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an object from storage.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The key (path) of the object in the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The object content stream and metadata.</returns>
    Task<(Stream Content, string? ContentType, long? Size, Dictionary<string, string>? Metadata)> GetObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an object from storage.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The key (path) of the object in the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteObjectAsync(
        string bucketName,
        string objectKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a bucket exists.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the bucket exists, false otherwise.</returns>
    Task<bool> BucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a bucket if it doesn't exist.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to create.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task EnsureBucketExistsAsync(
        string bucketName,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the full URL for an object.
    /// </summary>
    /// <param name="bucketName">The name of the bucket.</param>
    /// <param name="objectKey">The key (path) of the object.</param>
    /// <returns>The full URL to access the object.</returns>
    string GetObjectUrl(string bucketName, string objectKey);
}
