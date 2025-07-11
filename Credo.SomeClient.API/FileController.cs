using Microsoft.AspNetCore.Mvc;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Storage;

[ApiController]
[Route("api/[controller]")]
public class FileStorageController : ControllerBase
{
    private readonly IFileStorage _fileStorage;

    public FileStorageController(IFileStorage fileStorage)
    {
        _fileStorage = fileStorage;
    }

    [HttpPost("{channel}/{operation}")]
    public async Task<IActionResult> Upload(
        [FromRoute] string channel,
        [FromRoute] string operation,
         IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
            return BadRequest("File is missing.");

        var client = new Client(channel, operation);

        await using var stream = file.OpenReadStream();
        var credoFile = new CredoFile(stream, file.FileName, file.ContentType);

        await _fileStorage.Store(credoFile, client, cancellationToken);

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