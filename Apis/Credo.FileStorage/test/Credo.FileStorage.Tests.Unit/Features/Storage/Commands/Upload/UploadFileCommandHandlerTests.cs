using Credo.Core.FileStorage.Storage;
using Credo.FileStorage.Application.Features.Storage.Commands.Upload;
using Moq;
using Xunit;

namespace Credo.FileStorage.Tests.Unit.Features.Storage.Commands.Upload;

public class UploadFileCommandHandlerTests
{
    [Fact]
    public async Task Handle_Uploads_File_Using_IFileStorage()
    {
        // Arrange
        var fileStorageMock = new Mock<IFileStorage>();
        fileStorageMock
            .Setup(x => x.Store(It.IsAny<Credo.Core.FileStorage.Models.CredoFile>(), It.IsAny<Credo.Core.FileStorage.Models.Client>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        await using var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        var command = new UploadFileCommand
        {
            Stream = ms,
            FileName = "test.txt",
            ContentType = "text/plain",
            Channel = "mobile",
            Operation = "utility-payment",
            TransitionAfterDays = 90,
            ExpirationAfterDays = null
        };

        var handler = new UploadFileCommandHandler(fileStorageMock.Object);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        fileStorageMock.Verify(x => x.Store(It.IsAny<Credo.Core.FileStorage.Models.CredoFile>(), It.IsAny<Credo.Core.FileStorage.Models.Client>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}


