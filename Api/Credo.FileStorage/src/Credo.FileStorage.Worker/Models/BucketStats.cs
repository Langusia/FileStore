namespace Credo.FileStorage.Worker.Models;

public class BucketStats
{
    public string BucketName { get; set; }
    public int FileCount { get; set; }
    public long TotalSizeBytes { get; set; }
    public double TotalSizeMB => TotalSizeBytes / 1024.0 / 1024.0;
    public DateTime? FirstMigration { get; set; }
    public DateTime? LastMigration { get; set; }
}