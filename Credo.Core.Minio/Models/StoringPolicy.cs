namespace Credo.Core.Minio.Models;

public class StoringPolicy
{
    public int? ExpirationAfterDays { get; set; }
    public int TransitionAfterDays { get; set; }
}