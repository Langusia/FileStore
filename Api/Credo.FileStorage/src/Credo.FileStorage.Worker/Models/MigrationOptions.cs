namespace Credo.FileStorage.Worker.Models;

public class MigrationOptions
{
    public int BatchSize { get; set; } = 100;
    public bool SkipExisting { get; set; } = true;
    public bool DryRun { get; set; } = false;
    public int MaxDegreeOfParallelism { get; set; } = 1;
    public int MaxRetries { get; set; } = 3;
}