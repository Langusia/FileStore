using Microsoft.AspNetCore.Mvc;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Storage;
using Credo.Core.FileStorage.V1.DB.Models.Upload;
using Credo.Core.FileStorage.V1.Storage;
using Credo.SomeClient.API.Dtos;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController : ControllerBase
{
    private readonly IFileStorage _fileStorage;
    private readonly IObjectStorage _os;

    public FileStorageController(IFileStorage fileStorage, IObjectStorage os)
    {
        _fileStorage = fileStorage;
        _os = os;
    }

//  [HttpPost]
//  public async Task<IActionResult> Upload(UploadFileRequest request, CancellationToken cancellationToken)
//  {
//      if (request.file == null || request.file.Length == 0)
//          return BadRequest("File is missing.");

//      var client = new Client(request.Channel, request.Operation);

//      await using var stream = request.file.OpenReadStream();
//      var credoFile = new CredoFile(stream, request.file.FileName, request.file.ContentType, new(request.TransitionAfterDays, request.ExpirationAfterDays));

//      await _fileStorage.Store(credoFile, client, cancellationToken);

//      return Ok("File uploaded successfully.");
//  }

    [HttpPost]
    public async Task<IActionResult> Upload1(UploadFileRequest request, CancellationToken cancellationToken)
    {
        var s = await _os.Upload(new AliasArgs("BackOffice", "ArchiveDoc"),
            UploadFile.FromStream(request.file.OpenReadStream(), request.file.FileName, request.file.ContentType),
            ct: cancellationToken);

        if (request.file == null || request.file.Length == 0)
            return BadRequest("File is missing.");

        var client = new Client(request.Channel, request.Operation);

//        await using var stream = request.file.OpenReadStream();
//        var credoFile = new CredoFile(stream, request.file.FileName, request.file.ContentType, new(request.TransitionAfterDays, request.ExpirationAfterDays));
//
        //await _fileStorage.Store(credoFile, client, cancellationToken);

        return Ok("File uploaded successfully.");
    }

    [HttpGet("{channel}/{operation}/{objectName}")]
    public async Task<IActionResult> GetFile(
        [FromRoute] string channel,
        [FromRoute] string operation,
        [FromRoute] string objectName,
        CancellationToken cancellationToken)
    {
        var client = new Client(channel, operation);
        var data = await _fileStorage.Get(client, objectName, cancellationToken);

        if (data == null || data.Length == 0)
            return NotFound("File not found.");

        return File(new MemoryStream(data), "application/octet-stream", objectName);
    }

    [HttpDelete("{channel}/{operation}/{objectName}")]
    public async Task<IActionResult> Delete(
        [FromRoute] string channel,
        [FromRoute] string operation,
        [FromRoute] string objectName,
        CancellationToken cancellationToken)
    {
        var client = new Client(channel, operation);
        await _fileStorage.Delete(client, objectName, cancellationToken);

        return Ok("File deleted successfully.");
    }
}