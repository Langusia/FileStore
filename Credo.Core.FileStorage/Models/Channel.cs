namespace Credo.Core.FileStorage.Models;

public record Channel
{
    public Channel(Guid Id, string Alias, string Name, int SourceId)
    {
        this.Id = Id;
        this.Alias = Alias;
        this.Name = Name;
        this.SourceId = SourceId;
    }

    public Channel()
    {
        
    }

    public Guid Id { get; init; }
    public string Alias { get; init; }
    public string Name { get; init; }
    public int SourceId { get; init; }

    public void Deconstruct(out Guid Id, out string Alias, out string Name, out int SourceId)
    {
        Id = this.Id;
        Alias = this.Alias;
        Name = this.Name;
        SourceId = this.SourceId;
    }
}