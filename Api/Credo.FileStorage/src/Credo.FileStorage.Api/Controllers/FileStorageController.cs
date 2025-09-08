using Credo.Core.FileStorage.Models.Upload;
using Credo.Core.FileStorage.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Credo.FileStorage.Api.Controllers;

[Route("api/file-storage")]
public class FileStorageController(IObjectStorage os) : ControllerBase
{
    [HttpPost]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status400BadRequest, Type = typeof(string))]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(UploadResult))]
    public async Task<ActionResult<UploadResult>> Upload([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var result = await os.Upload(new AliasArgs(request.Channel, request.Operation),
            UploadFile.FromStream(request.file.OpenReadStream(), request.file.FileName, request.file.ContentType),
            ct: cancellationToken);

        if (request.file.Length == 0)
            return BadRequest("File is missing.");

        return Ok(result);
    }

    [HttpGet("{bucket}/{**key}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    public async Task<IActionResult> GetByBucketKey(string bucket, string key, CancellationToken ct)
    {
        var obj = await os.OpenReadAsync(bucket, key, ct);
        return File(obj.Stream, obj.ContentType, obj.FileName);
    }

    [HttpGet("{documentId}")]
    [Produces("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(FileStreamResult))]
    public async Task<IActionResult> GetByBucketKey(Guid documentId, CancellationToken ct)
    {
        var obj = await os.OpenReadAsync(documentId, ct);
        return File(obj.Stream, obj.ContentType, obj.FileName);
    }
}