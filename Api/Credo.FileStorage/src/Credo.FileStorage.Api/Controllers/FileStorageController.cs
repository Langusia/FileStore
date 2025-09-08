using Credo.Core.FileStorage.Models.Upload;
using Credo.Core.FileStorage.Storage;
using Microsoft.AspNetCore.Mvc;

namespace Credo.FileStorage.Api.Controllers;

[Route("api/file-storage")]
public class FileStorageController(IObjectStorage os) : ControllerBase
{
    [HttpPost("file")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] UploadFileRequest request, CancellationToken cancellationToken)
    {
        var s = await os.Upload(new AliasArgs(request.Channel, request.Operation),
            UploadFile.FromStream(request.file.OpenReadStream(), request.file.FileName, request.file.ContentType),
            ct: cancellationToken);

        if (request.file.Length == 0)
            return BadRequest("File is missing.");

        return Ok("File uploaded successfully.");
    }
}