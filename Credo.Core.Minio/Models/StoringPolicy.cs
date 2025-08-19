namespace Credo.Core.Minio.Models;

public class StoringPolicy
{
    public StoringPolicy()
    {
    }

    public StoringPolicy(int transitionAfterDays, int? expirationAfterDays)
    {
        TransitionAfterDays = transitionAfterDays;
        ExpirationAfterDays = expirationAfterDays;
    }

    public int TransitionAfterDays { get; set; }
    public int? ExpirationAfterDays { get; set; }
}