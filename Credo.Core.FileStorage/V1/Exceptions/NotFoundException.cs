namespace Credo.Core.FileStorage.V1.Exceptions;

public class NotFoundException : Exception
{
    public string Entity { get; }
    public string? Detail { get; }

    public NotFoundException(string entity, string? detail = null)
        : base(detail is null ? $"{entity} not found." : $"{entity} not found: {detail}")
    {
        Entity = entity;
        Detail = detail;
    }
}