namespace FileStore.Core.Models;

public class ObjectLink
{
    public Guid ObjectId { get; set; }
    public string Channel { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string? BusinessEntityId { get; set; }
    public DateTime CreatedAt { get; set; }
}
