using Credo.Core.Shared.Mediator;

namespace Credo.FileStorage.Application.Features.Storage.Commands.Upload;

public sealed record UploadFileCommand : ICommand<string>
{
    public required Stream Stream { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required string Channel { get; init; }
    public required string Operation { get; init; }
    public required int TransitionAfterDays { get; init; }
    public int? ExpirationAfterDays { get; init; }
}


