using FileStore.Core.Enums;
using FileStore.Core.Exceptions;
using FileStore.Core.Interfaces;
using FileStore.Core.Models;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FileStore.API.Controllers;

/// <summary>
/// S3-like object storage API for managing files in buckets
/// </summary>
[ApiController]
[Route("buckets/{bucket}")]
[Produces("application/json")]
[SwaggerTag("Object storage operations with S3-like interface")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a file to the specified bucket
    /// </summary>
    /// <param name="bucket">Bucket name (e.g., "documents", "images")</param>
    /// <param name="file">File to upload (max 100MB)</param>
    /// <param name="channel">Business channel (required: e.g., "loans", "cards", "onboarding")</param>
    /// <param name="operation">Business operation (required: e.g., "agreements", "statements", "kyc")</param>
    /// <param name="businessEntityId">Optional business entity ID (e.g., "loan-12345", "customer-67890")</param>
    /// <param name="tags">Optional JSON metadata tags (e.g., {"department": "retail", "type": "contract"})</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Upload result with object ID and metadata</returns>
    /// <response code="201">File uploaded successfully</response>
    /// <response code="400">Invalid request or file size exceeds limit</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("objects")]
    [RequestSizeLimit(104857600)] // 100MB
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Upload file to bucket",
        Description = "Uploads a file with business context metadata. Files are automatically sharded and stored in HOT tier. Maximum file size is 100MB.",
        OperationId = "UploadObject",
        Tags = new[] { "Storage" }
    )]
    public async Task<ActionResult<UploadResult>> UploadObject(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromForm, SwaggerParameter("File to upload", Required = true)] IFormFile file,
        [FromForm, SwaggerParameter("Business channel (e.g., loans, cards)", Required = true)] string channel,
        [FromForm, SwaggerParameter("Business operation (e.g., agreements, statements)", Required = true)] string operation,
        [FromForm, SwaggerParameter("Business entity ID (optional)")] string? businessEntityId = null,
        [FromForm, SwaggerParameter("JSON metadata tags (optional)")] string? tags = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            Dictionary<string, string>? tagsDictionary = null;
            if (!string.IsNullOrEmpty(tags))
            {
                tagsDictionary = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(tags);
            }

            var request = new UploadRequest
            {
                Bucket = bucket,
                FileStream = file.OpenReadStream(),
                FileName = file.FileName,
                ContentType = file.ContentType,
                Channel = channel,
                Operation = operation,
                BusinessEntityId = businessEntityId,
                Tags = tagsDictionary
            };

            var result = await _storageService.UploadAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetObjectMetadata), new { bucket, objectId = result.ObjectId }, result);
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Upload failed for bucket {Bucket}", bucket);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Download a file from the specified bucket
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="objectId">Object ID (GUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>File stream with appropriate content type</returns>
    /// <response code="200">File downloaded successfully</response>
    /// <response code="404">Object not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("objects/{objectId}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Download file from bucket",
        Description = "Downloads a file by object ID. Supports HTTP range requests for partial downloads. Updates last accessed timestamp.",
        OperationId = "DownloadObject",
        Tags = new[] { "Storage" }
    )]
    public async Task<IActionResult> DownloadObject(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromRoute, SwaggerParameter("Object ID (GUID)", Required = true)] Guid objectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await _storageService.GetMetadataAsync(bucket, objectId, cancellationToken);
            if (metadata == null)
                return NotFound(new { error = $"Object {objectId} not found in bucket {bucket}" });

            var stream = await _storageService.DownloadAsync(bucket, objectId, cancellationToken);
            return File(stream, metadata.ContentType, enableRangeProcessing: true);
        }
        catch (ObjectNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object not found: {Bucket}/{ObjectId}", bucket, objectId);
            return NotFound(new { error = ex.Message });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Download failed for {Bucket}/{ObjectId}", bucket, objectId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get metadata for a file without downloading it
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="objectId">Object ID (GUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Metadata in response headers (X-Object-Id, X-Content-Length, X-Content-Type, X-Hash, X-Tier, X-Created-At, X-Last-Accessed-At)</returns>
    /// <response code="200">Metadata retrieved successfully</response>
    /// <response code="404">Object not found</response>
    /// <response code="500">Internal server error</response>
    [HttpHead("objects/{objectId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Get object metadata",
        Description = "Retrieves object metadata via response headers without downloading the file. Returns X-Object-Id, X-Content-Length, X-Content-Type, X-Hash, X-Tier, X-Created-At, and X-Last-Accessed-At headers.",
        OperationId = "GetObjectMetadata",
        Tags = new[] { "Storage" }
    )]
    public async Task<IActionResult> GetObjectMetadata(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromRoute, SwaggerParameter("Object ID (GUID)", Required = true)] Guid objectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var metadata = await _storageService.GetMetadataAsync(bucket, objectId, cancellationToken);
            if (metadata == null)
                return NotFound();

            Response.Headers.Append("X-Object-Id", metadata.ObjectId.ToString());
            Response.Headers.Append("X-Content-Length", metadata.Size.ToString());
            Response.Headers.Append("X-Content-Type", metadata.ContentType);
            Response.Headers.Append("X-Hash", metadata.Hash);
            Response.Headers.Append("X-Tier", metadata.Tier.ToString());
            Response.Headers.Append("X-Created-At", metadata.CreatedAt.ToString("O"));

            if (metadata.LastAccessedAt.HasValue)
                Response.Headers.Append("X-Last-Accessed-At", metadata.LastAccessedAt.Value.ToString("O"));

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get metadata for {Bucket}/{ObjectId}", bucket, objectId);
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Delete a file from the specified bucket
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="objectId">Object ID (GUID)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">File deleted successfully</response>
    /// <response code="404">Object not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("objects/{objectId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Delete file from bucket",
        Description = "Permanently deletes a file and all its metadata. This operation cannot be undone.",
        OperationId = "DeleteObject",
        Tags = new[] { "Storage" }
    )]
    public async Task<IActionResult> DeleteObject(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromRoute, SwaggerParameter("Object ID (GUID)", Required = true)] Guid objectId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageService.DeleteAsync(bucket, objectId, cancellationToken);
            return NoContent();
        }
        catch (ObjectNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object not found: {Bucket}/{ObjectId}", bucket, objectId);
            return NotFound(new { error = ex.Message });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Delete failed for {Bucket}/{ObjectId}", bucket, objectId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// List objects in a bucket with pagination
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="prefix">Optional prefix filter (not implemented yet)</param>
    /// <param name="continuationToken">Token for pagination (from previous response)</param>
    /// <param name="maxKeys">Maximum number of objects to return (default: 1000, max: 1000)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of objects with pagination information</returns>
    /// <response code="200">Objects listed successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("objects")]
    [ProducesResponseType(typeof(ListObjectsResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "List objects in bucket",
        Description = "Lists objects in a bucket with pagination support. Returns metadata for each object. Use continuationToken from response for next page.",
        OperationId = "ListObjects",
        Tags = new[] { "Storage" }
    )]
    public async Task<ActionResult<ListObjectsResult>> ListObjects(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromQuery, SwaggerParameter("Prefix filter (reserved for future use)")] string? prefix = null,
        [FromQuery, SwaggerParameter("Continuation token for pagination")] string? continuationToken = null,
        [FromQuery, SwaggerParameter("Maximum objects to return (default: 1000, max: 1000)")] int maxKeys = 1000,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = new ListObjectsRequest
            {
                Bucket = bucket,
                Prefix = prefix,
                ContinuationToken = continuationToken,
                MaxKeys = maxKeys
            };

            var result = await _storageService.ListObjectsAsync(request, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "List failed for bucket {Bucket}", bucket);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Change the storage tier of a file
    /// </summary>
    /// <param name="bucket">Bucket name</param>
    /// <param name="objectId">Object ID (GUID)</param>
    /// <param name="request">Tier change request (0=Hot, 1=Cold)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>No content on success</returns>
    /// <response code="204">Tier changed successfully</response>
    /// <response code="404">Object not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("objects/{objectId}/tier")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
    [SwaggerOperation(
        Summary = "Change object storage tier",
        Description = "Moves an object between Hot (0) and Cold (1) storage tiers. Hot tier is for frequently accessed files, Cold tier is for archival.",
        OperationId = "ChangeTier",
        Tags = new[] { "Storage" }
    )]
    public async Task<IActionResult> ChangeTier(
        [FromRoute, SwaggerParameter("Bucket name", Required = true)] string bucket,
        [FromRoute, SwaggerParameter("Object ID (GUID)", Required = true)] Guid objectId,
        [FromBody, SwaggerParameter("Tier change request", Required = true)] ChangeTierRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _storageService.ChangeTierAsync(bucket, objectId, request.Tier, cancellationToken);
            return NoContent();
        }
        catch (ObjectNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object not found: {Bucket}/{ObjectId}", bucket, objectId);
            return NotFound(new { error = ex.Message });
        }
        catch (StorageException ex)
        {
            _logger.LogError(ex, "Tier change failed for {Bucket}/{ObjectId}", bucket, objectId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

/// <summary>
/// Request to change storage tier
/// </summary>
/// <param name="Tier">Target storage tier (0=Hot, 1=Cold)</param>
[SwaggerSchema("Request to change object storage tier")]
public record ChangeTierRequest(
    [property: SwaggerParameter("Storage tier (0=Hot, 1=Cold)", Required = true)]
    StorageTier Tier
);
