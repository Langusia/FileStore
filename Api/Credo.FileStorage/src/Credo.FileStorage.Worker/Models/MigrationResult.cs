namespace Credo.FileStorage.Worker.Models;

public class MigrationResult
{
    public bool Success { get; set; }
    public int TotalProcessed { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public TimeSpan Duration { get; set; }
    public long TotalBytesProcessed { get; set; }
    public double TotalMBProcessed => TotalBytesProcessed / 1024.0 / 1024.0;
    public List<string> Errors { get; set; } = new();
}