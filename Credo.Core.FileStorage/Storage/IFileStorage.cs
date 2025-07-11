using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Storage;

public interface IFileStorage
{
    Task Store(CredoFile fileStream, Client client, CancellationToken cancellationToken);
    Task<byte[]> Get(Client client, string objectName, CancellationToken cancellationToken);
    Task Delete(Client client, string objectName, CancellationToken cancellationToken);
}