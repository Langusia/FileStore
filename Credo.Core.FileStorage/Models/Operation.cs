namespace Credo.Core.FileStorage.Models;

public record Operation
{
    public Operation(Guid Id, string Alias, string Name, int DictionaryId, Guid StorageOperationId)
    {
        this.Id = Id;
        this.Alias = Alias;
        this.Name = Name;
        this.DictionaryId = DictionaryId;
        this.StorageOperationId = StorageOperationId;
    }

    public Operation()
    {
    }

    public Guid Id { get; init; }
    public string Alias { get; init; }
    public string Name { get; init; }
    public int DictionaryId { get; init; }
    public Guid StorageOperationId { get; init; }

    public void Deconstruct(out Guid Id, out string Alias, out string Name, out int DictionaryId)
    {
        Id = this.Id;
        Alias = this.Alias;
        Name = this.Name;
        DictionaryId = this.DictionaryId;
    }
}