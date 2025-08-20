namespace Credo.FileStorage.Domain.Models;

public class Todo
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public TodoStatus Status { get; set; }
}

public enum TodoStatus
{
    New,
    InProgress,
    Done
}