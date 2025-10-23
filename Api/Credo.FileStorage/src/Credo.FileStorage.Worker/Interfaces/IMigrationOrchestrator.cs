using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Interfaces;

public interface IMigrationOrchestrator
{
    Task<MigrationResult> MigrateAsync(
        MigrationOptions options,
        IProgress<MigrationProgress>? progress = null,
        CancellationToken cancellationToken = default);
}