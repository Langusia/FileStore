using Credo.Core.Minio.Models;
using Minio;
using Minio.DataModel.Args;

namespace Credo.Core.Minio.Storage;

public class MinioStorage(IMinioClient client) : IMinioStorage
{
    public async Task StoreFile(FileToStore fileToStore, CancellationToken cancellationToken, StoringPolicy? storingPolicy = null)
    {
        var lc = storingPolicy?.ToLifecycleConfiguration();
        var tags = lc?.SelectTags();

        await PutBucketAsync(fileToStore.BucketName, cancellationToken, storingPolicy);
        var putObjectArgs = new PutObjectArgs()
            .WithBucket(fileToStore.BucketName)
            .WithObject(fileToStore.Name)
            .WithStreamData(fileToStore.Stream)
            .WithObjectSize(fileToStore.Stream.Length)
            .WithContentType(fileToStore.ContentType)
            .WithTagging(tags);

        await client.PutObjectAsync(putObjectArgs, cancellationToken);
    }


    public async Task<string[]> PutBucketAsync(string bucketName, CancellationToken token, StoringPolicy? storingPolicy = null)
    {
        var bucketExists = await client.BucketExistsAsync(new BucketExistsArgs().WithBucket(bucketName), token);
        if (!bucketExists)
        {
            await client.MakeBucketAsync(new MakeBucketArgs()
                .WithBucket(bucketName), token);
        }

        var existingRules = await client.GetBucketLifecycleAsync(new GetBucketLifecycleArgs().WithBucket(bucketName), token);
        var lc = existingRules.MergeLifeCycleConfiguration(storingPolicy);

        await client.SetBucketLifecycleAsync(new SetBucketLifecycleArgs()
                .WithLifecycleConfiguration(lc)
                .WithBucket(bucketName)
            , token);

        return lc.Rules.Select(x => x.ID).ToArray();
    }

    public async Task<byte[]> GetFile(string bucketName, string objectName)
    {
        var memoryStream = new MemoryStream();
        var getObjectArgs = new GetObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName)
            .WithCallbackStream(stream => stream.CopyTo(memoryStream));

        await client.GetObjectAsync(getObjectArgs);
        return memoryStream.ToArray();
    }

    public async Task DeleteFile(string bucketName, string objectName, CancellationToken cancellationToken)
    {
        var removeObjectArgs = new RemoveObjectArgs()
            .WithBucket(bucketName)
            .WithObject(objectName);
        await client.RemoveObjectAsync(removeObjectArgs, cancellationToken);
    }
}