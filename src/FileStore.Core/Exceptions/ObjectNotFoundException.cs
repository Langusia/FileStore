namespace FileStore.Core.Exceptions;

public class ObjectNotFoundException : StorageException
{
    public ObjectNotFoundException(Guid objectId)
        : base($"Object {objectId} not found") { }

    public ObjectNotFoundException(string bucket, Guid objectId)
        : base($"Object {objectId} not found in bucket {bucket}") { }
}
