using FileStore.API.DTOs;
using FileStore.Storage.Enums;
using FileStore.Storage.Models;
using FileStore.Storage.Services;
using Microsoft.AspNetCore.Mvc;

namespace FileStore.API.Controllers;

/// <summary>
/// Controller for object storage operations.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class StorageController : ControllerBase
{
    private readonly IObjectStorageService _storageService;
    private readonly ILogger<StorageController> _logger;

    public StorageController(
        IObjectStorageService storageService,
        ILogger<StorageController> logger)
    {
        _storageService = storageService ?? throw new ArgumentNullException(nameof(storageService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Uploads a file to object storage.
    /// </summary>
    /// <param name="request">The upload request containing file and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Upload response with object details.</returns>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(UploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<UploadResponseDto>> UploadObject(
        [FromForm] UploadRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            _logger.LogInformation(
                "Upload request received: FileName={FileName}, Channel={Channel}, Operation={Operation}",
                request.File.FileName,
                request.Channel,
                request.Operation);

            var channel = (Channel)request.Channel;
            var operation = (Operation)request.Operation;

            await using var stream = request.File.OpenReadStream();

            var uploadRequest = new UploadRequest
            {
                Content = stream,
                FileName = request.File.FileName,
                Channel = channel,
                Operation = operation,
                ContentType = request.File.ContentType,
                Metadata = request.Metadata,
                TrackSize = request.TrackSize
            };

            var response = await _storageService.UploadObjectAsync(uploadRequest, cancellationToken);

            var dto = new UploadResponseDto
            {
                ObjectId = response.ObjectId,
                ObjectKey = response.ObjectKey,
                BucketName = response.BucketName,
                FullStorageUrl = response.FullStorageUrl,
                SizeInBytes = response.SizeInBytes,
                ETag = response.ETag,
                UploadedAt = response.UploadedAt
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading object");
            return StatusCode(500, new { error = "Failed to upload object", message = ex.Message });
        }
    }

    /// <summary>
    /// Downloads an object from storage.
    /// </summary>
    /// <param name="objectId">The unique object identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file content.</returns>
    [HttpGet("download/{objectId}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetObject(
        string objectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Download request received: ObjectId={ObjectId}", objectId);

            var response = await _storageService.GetObjectAsync(objectId, cancellationToken);

            return File(
                response.Content,
                response.ContentType ?? "application/octet-stream",
                fileDownloadName: Path.GetFileName(response.ObjectKey));
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object not found: ObjectId={ObjectId}", objectId);
            return NotFound(new { error = "Object not found", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading object: ObjectId={ObjectId}", objectId);
            return StatusCode(500, new { error = "Failed to download object", message = ex.Message });
        }
    }

    /// <summary>
    /// Gets metadata for an object without downloading its content.
    /// </summary>
    /// <param name="objectId">The unique object identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Object metadata.</returns>
    [HttpGet("metadata/{objectId}")]
    [ProducesResponseType(typeof(ObjectMetadataDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ObjectMetadataDto>> GetObjectMetadata(
        string objectId,
        CancellationToken cancellationToken)
    {
        try
        {
            var metadata = await _storageService.GetObjectMetadataAsync(objectId, cancellationToken);

            if (metadata == null)
            {
                return NotFound(new { error = "Object not found" });
            }

            var dto = new ObjectMetadataDto
            {
                ObjectId = metadata.ObjectId,
                ObjectKey = metadata.ObjectKey,
                BucketName = metadata.BucketName,
                OriginalFileName = metadata.OriginalFileName,
                FullStorageUrl = metadata.FullStorageUrl,
                ContentType = metadata.ContentType,
                SizeInBytes = metadata.SizeInBytes,
                CreatedAt = metadata.CreatedAt,
                LastModifiedAt = metadata.LastModifiedAt,
                LastAccessedAt = metadata.LastAccessedAt,
                Metadata = metadata.Metadata
            };

            return Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting object metadata: ObjectId={ObjectId}", objectId);
            return StatusCode(500, new { error = "Failed to get metadata", message = ex.Message });
        }
    }

    /// <summary>
    /// Deletes an object from storage.
    /// </summary>
    /// <param name="objectId">The unique object identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Delete operation result.</returns>
    [HttpDelete("{objectId}")]
    [ProducesResponseType(typeof(DeleteResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<DeleteResponseDto>> DeleteObject(
        string objectId,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Delete request received: ObjectId={ObjectId}", objectId);

            var response = await _storageService.DeleteObjectAsync(objectId, cancellationToken);

            var dto = new DeleteResponseDto
            {
                ObjectId = response.ObjectId,
                ObjectKey = response.ObjectKey,
                BucketName = response.BucketName,
                Success = response.Success,
                DeletedAt = response.DeletedAt,
                Message = response.Message
            };

            return Ok(dto);
        }
        catch (FileNotFoundException ex)
        {
            _logger.LogWarning(ex, "Object not found: ObjectId={ObjectId}", objectId);
            return NotFound(new { error = "Object not found", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting object: ObjectId={ObjectId}", objectId);
            return StatusCode(500, new { error = "Failed to delete object", message = ex.Message });
        }
    }

    /// <summary>
    /// Lists objects with optional filtering by channel and operation.
    /// </summary>
    /// <param name="channel">Optional channel filter.</param>
    /// <param name="operation">Optional operation filter.</param>
    /// <param name="page">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 50, max: 100).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Paginated list of objects.</returns>
    [HttpGet("list")]
    [ProducesResponseType(typeof(List<ObjectMetadataDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ObjectMetadataDto>>> ListObjects(
        [FromQuery] int? channel,
        [FromQuery] int? operation,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate and limit page size
            pageSize = Math.Min(pageSize, 100);
            page = Math.Max(page, 1);

            Channel? channelEnum = channel.HasValue ? (Channel)channel.Value : null;
            Operation? operationEnum = operation.HasValue ? (Operation)operation.Value : null;

            var objects = await _storageService.ListObjectsAsync(
                channelEnum,
                operationEnum,
                page,
                pageSize,
                cancellationToken);

            var dtos = objects.Select(o => new ObjectMetadataDto
            {
                ObjectId = o.ObjectId,
                ObjectKey = o.ObjectKey,
                BucketName = o.BucketName,
                OriginalFileName = o.OriginalFileName,
                FullStorageUrl = o.FullStorageUrl,
                ContentType = o.ContentType,
                SizeInBytes = o.SizeInBytes,
                CreatedAt = o.CreatedAt,
                LastModifiedAt = o.LastModifiedAt,
                LastAccessedAt = o.LastAccessedAt,
                Metadata = o.Metadata
            }).ToList();

            return Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing objects");
            return StatusCode(500, new { error = "Failed to list objects", message = ex.Message });
        }
    }
}
