namespace Credo.FileStorage.Worker.Models;

public class FailedMigrationInfo
{
    public long ContentId { get; set; }
    public string ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
    public DateTime LastAttempt { get; set; }
}