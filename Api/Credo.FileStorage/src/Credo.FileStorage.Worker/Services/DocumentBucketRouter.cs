using Credo.FileStorage.Worker.Interfaces;
using Credo.FileStorage.Worker.Models;

namespace Credo.FileStorage.Worker.Services;

public class DocumentBucketRouter : IBucketRouter
{
    private const string DefaultBucket = "default";
    
    public string GetBucketName(DocumentMetadata metadata)
    {
        return metadata.ChannelId.HasValue 
            ? $"channel-{metadata.ChannelId.Value}" 
            : DefaultBucket;
    }
    
    public string GetObjectKey(DocumentMetadata metadata)
    {
        var date = metadata.RecordDate;
        var extension = !string.IsNullOrEmpty(metadata.DocumentExt) 
            ? metadata.DocumentExt.TrimStart('.') 
            : "bin";
        
        return $"{date:yyyy}/{date:MM}/{date:dd}/{metadata.DocumentID}.{extension}";
    }
}