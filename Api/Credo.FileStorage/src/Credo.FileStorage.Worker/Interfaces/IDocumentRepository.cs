using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Interfaces;

public interface IDocumentRepository
{
    IAsyncEnumerable<DocumentMetadata> GetPendingMetadataAsync(
        int batchSize,
        CancellationToken cancellationToken = default);
    
    Task<byte[]> GetDocumentContentAsync(
        long documentId,
        CancellationToken cancellationToken = default);
    
    Task<int> GetPendingCountAsync(CancellationToken cancellationToken = default);
}