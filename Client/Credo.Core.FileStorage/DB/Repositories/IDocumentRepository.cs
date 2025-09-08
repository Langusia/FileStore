using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.Models;
using Credo.Core.FileStorage.Models.Upload;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IDocumentsRepository
{
    /// <summary>
    /// Inserts a row to doc.Documents and returns the new Document Id.
    /// </summary>
    Task<Guid> InsertAsync(DocumentCreate create, CancellationToken ct = default);

    // (optional) convenience reads you may want later
    Task<Document?> TryGetAsync(Guid id, CancellationToken ct = default);
    Task<Document?> TryGetAsync(string bucket, string objKey, CancellationToken ct = default);
}