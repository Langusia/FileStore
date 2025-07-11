namespace Credo.Core.FileStorage.Models;

public record Client(string Channel, string Operation)
{
    public string ToBucketName() => $"{Channel}/{Operation}";
}