using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Storage;
using Credo.Core.Shared.Abstractions;
using Credo.Core.Shared.Extensions;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Messages;
using Credo.FileStorage.Api.Filters;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Credo.FileStorage.Api.Controllers;

[Route("api/v1/storage")]
[TypeFilter(typeof(RequiredHeaderFilter), IsReusable = true, Order = 1)]
public sealed class StorageController(ISender sender, IFileStorage fileStorage) : ApiController(sender)
{
    [HttpPost("upload")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiServiceResponse<string>>> Upload(
        [FromForm] IFormFile file,
        [FromQuery] string channel,
        [FromQuery] string operation,
        [FromQuery] int transitionAfterDays = 90,
        [FromQuery] int? expirationAfterDays = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new BadRequestApiServiceResponse("File is missing"));
        }

        await using var stream = file.OpenReadStream();
        return await Result
            .Create(true)
            .Bind(_ => Sender.Send(new Credo.FileStorage.Application.Features.Storage.Commands.Upload.UploadFileCommand
            {
                Stream = stream,
                FileName = file.FileName,
                ContentType = file.ContentType,
                Channel = channel,
                Operation = operation,
                TransitionAfterDays = transitionAfterDays,
                ExpirationAfterDays = expirationAfterDays
            }, cancellationToken))
            .Match(
                _ => Ok(new SuccessApiServiceResponse<string>("File uploaded successfully")),
                HandleFailure<string>
            );
    }

    [HttpGet("download/{channel}/{operation}/{objectName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Download(
        string channel,
        string operation,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var client = new Client(channel, operation);
        var bytes = await fileStorage.Get(client, objectName, cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            //var s = new NotFoundApiServiceResponse("File not found");
            return NotFound();
        }

        return File(new MemoryStream(bytes), "application/octet-stream", objectName);
    }

    [HttpDelete("{channel}/{operation}/{objectName}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiServiceResponse<string>>> Delete(
        string channel,
        string operation,
        string objectName,
        CancellationToken cancellationToken = default)
    {
        var client = new Client(channel, operation);
        await fileStorage.Delete(client, objectName, cancellationToken);
        return Ok(new SuccessApiServiceResponse<string>("File deleted successfully"));
    }
}