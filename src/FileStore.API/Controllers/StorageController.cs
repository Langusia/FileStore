using FileStore.Core.Enums;
using FileStore.Core.Exceptions;
using FileStore.Core.Interfaces;
using FileStore.Core.Models;
using Microsoft.AspNetCore.Mvc;

namespace FileStore.API.Controllers;

[ApiController]
[Route("buckets/{bucket}")]
public class StorageController : ControllerBase
{
    private readonly IStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(IStorageService storageService, ILogger<StorageController> logger)
    {
        _storageService = storageService;
        _logger = logger;
    }

    [HttpPost("objects")]
    [RequestSizeLimit(104857600)] // 100MB
    public async Task<ActionResult<UploadResult>> UploadObject(
        string bucket,
        [FromForm] IFormFile file,
        [FromForm] string channel,
        [FromForm] string operation,
        [FromForm] string? businessEntityId = null,
        [FromForm] string? tags = null,
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

    [HttpGet("objects/{objectId}")]
    public async Task<IActionResult> DownloadObject(
        string bucket,
        Guid objectId,
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

    [HttpHead("objects/{objectId}")]
    public async Task<IActionResult> GetObjectMetadata(
        string bucket,
        Guid objectId,
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

    [HttpDelete("objects/{objectId}")]
    public async Task<IActionResult> DeleteObject(
        string bucket,
        Guid objectId,
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

    [HttpGet("objects")]
    public async Task<ActionResult<ListObjectsResult>> ListObjects(
        string bucket,
        [FromQuery] string? prefix = null,
        [FromQuery] string? continuationToken = null,
        [FromQuery] int maxKeys = 1000,
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

    [HttpPost("objects/{objectId}/tier")]
    public async Task<IActionResult> ChangeTier(
        string bucket,
        Guid objectId,
        [FromBody] ChangeTierRequest request,
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

public record ChangeTierRequest(StorageTier Tier);
