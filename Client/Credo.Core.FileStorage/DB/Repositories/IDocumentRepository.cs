using Credo.Core.FileStorage.DB.Models;
using Credo.Core.FileStorage.Entities;

namespace Credo.Core.FileStorage.DB.Repositories;

public interface IDocumentsRepository
{
    /// <summary>
    /// Inserts a row to doc.Documents and returns the new Document Id.
    /// </summary>
    Task<Guid> InsertAsync(DocumentCreate create, CancellationToken ct = default);

    // (optional) convenience reads you may want later
    Task<Document?> TryGetAsync(Guid id, CancellationToken ct = default);
}