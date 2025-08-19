using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Storage;
using Credo.Core.Shared.Library;
using Credo.Core.Shared.Mediator;

namespace Credo.FileStorage.Application.Features.Storage.Commands.Upload;

public sealed class UploadFileCommandHandler(IFileStorage fileStorage) : ICommandHandler<UploadFileCommand, string>
{
    public async Task<Result<string>> Handle(UploadFileCommand request, CancellationToken cancellationToken)
    {
        await using var stream = request.Stream;
        var credoFile = new CredoFile(stream, request.FileName, request.ContentType, new(request.TransitionAfterDays, request.ExpirationAfterDays));
        var client = new Client(request.Channel, request.Operation);

        await fileStorage.Store(credoFile, client, cancellationToken);
        return Result.Success("Guid.NewGuid()");
    }
}