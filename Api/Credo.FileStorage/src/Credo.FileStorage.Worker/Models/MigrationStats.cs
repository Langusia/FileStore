namespace Credo.FileStorage.Worker.Models;

public class MigrationStats
{
    public int TotalProcessed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int InProgress { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? LastProcessedAt { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMB => TotalSizeBytes / 1024.0 / 1024.0;
}