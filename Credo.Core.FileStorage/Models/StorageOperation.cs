namespace Credo.Core.FileStorage.Models;

public record StorageOperation
{
    public StorageOperation(Guid Id, string Alias, Guid OperationId)
    {
        this.Id = Id;
        this.Alias = Alias;
        this.OperationId = OperationId;
    }

    public StorageOperation()
    {
    }

    public Guid Id { get; init; }
    public string Alias { get; init; }
    public Guid OperationId { get; init; }

    public void Deconstruct(out Guid Id, out string Alias, out Guid OperationId)
    {
        Id = this.Id;
        Alias = this.Alias;
        OperationId = this.OperationId;
    }
}