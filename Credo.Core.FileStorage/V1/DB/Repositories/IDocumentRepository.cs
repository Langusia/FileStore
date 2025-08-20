using Credo.Core.FileStorage.V1.DB.Models;
using Credo.Core.FileStorage.V1.Entities;

namespace Credo.Core.FileStorage.V1.DB.Repositories;

public interface IDocumentsRepository
{
    /// <summary>
    /// Inserts a row to doc.Documents and returns the new Document Id.
    /// </summary>
    Task<Guid> InsertAsync(DocumentCreate create, CancellationToken ct = default);

    // (optional) convenience reads you may want later
    Task<Document?> TryGetAsync(Guid id, CancellationToken ct = default);
}