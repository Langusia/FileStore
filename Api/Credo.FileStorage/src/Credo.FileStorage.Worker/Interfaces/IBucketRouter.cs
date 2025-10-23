using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Interfaces;

public interface IBucketRouter
{
    string GetBucketName(DocumentMetadata metadata);
    string GetObjectKey(DocumentMetadata metadata);
}