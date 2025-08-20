using Credo.Core.FileStorage.DB.Models.Upload;
using Credo.Core.FileStorage.Storage;
using Credo.SomeClient.API.Dtos;
using Microsoft.AspNetCore.Mvc;
 
namespace Credo.SomeClient.API;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController(IObjectStorage os) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Upload1(UploadFileRequest request, CancellationToken cancellationToken)
    {
        var s = await os.Upload(new AliasArgs("BackOffice", "ArchiveDoc"),
            UploadFile.FromStream(request.file.OpenReadStream(), request.file.FileName, request.file.ContentType),
            ct: cancellationToken);

        if (request.file.Length == 0)
            return BadRequest("File is missing.");

        return Ok("File uploaded successfully.");
    }
}