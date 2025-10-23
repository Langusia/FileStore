namespace Credo.FileStorage.Worker.Models;

public class MigrationProgress
{
    public int CurrentBatch { get; set; }
    public int TotalProcessed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public double PercentComplete { get; set; }
    public TimeSpan Elapsed { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public long BytesProcessed { get; set; }
    public double MBProcessed => BytesProcessed / 1024.0 / 1024.0;
}