namespace Credo.Core.FileStorage.Models;

public record Channel(Guid Id, string Alias, string Name, int SourceId);