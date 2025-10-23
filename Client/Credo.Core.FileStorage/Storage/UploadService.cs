using Credo.Core.FileStorage.DB.Entities;
using Credo.Core.FileStorage.DB.Repositories;
using Credo.Core.FileStorage.Models.Upload;
using Credo.Core.FileStorage.Validation;
using Minio;
using Minio.DataModel.Args;

namespace Credo.Core.FileStorage.Storage;

/// <summary>
/// Handles the upload pipeline workflow
/// </summary>
internal sealed class UploadService(
    IMinioClient minio,
    IDocumentsRepository docs,
    IFileTypeInspector inspector)
{
    /// <summary>
    /// Validates the upload request parameters
    /// </summary>
    public static void ValidateRequest(UploadFile file)
    {
        ArgumentNullException.ThrowIfNull(file);
        ArgumentNullException.ThrowIfNull(file.Content, nameof(file.Content));
        ArgumentException.ThrowIfNullOrWhiteSpace(file.FileName, nameof(file.FileName));
    }

    /// <summary>
    /// Prepares upload context: processes file, detects type, generates names
    /// </summary>
    public async Task<UploadContext> PrepareContextAsync(
        UploadFile file,
        ChannelOperationBucket cob,
        UploadOptions? options,
        CancellationToken ct)
    {
        var safeFileName = Path.GetFileName(file.FileName);
        var bucketName = cob.Bucket.Name;
        var routeId = cob.Id;

        // Make stream seekable for type detection
        var (seekableStream, size, isTemp) = await StreamHelper.EnsureSeekableAsync(file.Content, ct);

        // Detect file type
        short typeCode;
        try
        {
            typeCode = await inspector.DetectOrThrowAsync(
                seekableStream,
                safeFileName,
                file.ContentType,
                ct);
        }
        finally
        {
            if (seekableStream.CanSeek)
                seekableStream.Position = 0;
        }

        // Determine content type and extension
        var contentType = MimeMap.ToContentType(typeCode);
        var extension = ObjectKeyGenerator.DetermineExtension(contentType, safeFileName);

        // Generate object key and determine document name
        var nowUtc = DateTime.UtcNow;
        var objectKey = ObjectKeyGenerator.Generate(options?.ObjectKeyPrefix, extension, nowUtc);
        var documentName = string.IsNullOrWhiteSpace(options?.LogicalName)
            ? safeFileName
            : options!.LogicalName!;

        return new UploadContext(
            BucketName: bucketName,
            ObjectKey: objectKey,
            DocumentName: documentName,
            ContentType: contentType,
            TypeCode: typeCode,
            Size: size,
            Stream: seekableStream,
            IsTempStream: isTemp,
            ChannelOperationBucketId: routeId,
            UploadedAtUtc: nowUtc,
            OriginalFile: file
        );
    }

    /// <summary>
    /// Uploads the file to MinIO storage with proper cleanup
    /// </summary>
    public async Task UploadToStorageAsync(UploadContext context, CancellationToken ct)
    {
        try
        {
            await minio.PutObjectAsync(new PutObjectArgs()
                .WithBucket(context.BucketName.ToLowerInvariant())
                .WithObject(context.ObjectKey)
                .WithStreamData(context.Stream)
                .WithObjectSize(context.Size)
                .WithContentType(context.ContentType), ct);
        }
        finally
        {
            await StreamHelper.CleanupStreamsAsync(
                context.Stream,
                context.IsTempStream,
                context.OriginalFile);
        }
    }

    /// <summary>
    /// Records the upload in the database with compensation on failure
    /// </summary>
    public async Task<Guid> RecordInDatabaseAsync(UploadContext context, CancellationToken ct)
    {
        try
        {
            return await docs.InsertAsync(
                new DocumentCreate(
                    ChannelOperationBucketId: context.ChannelOperationBucketId,
                    Name: context.DocumentName,
                    Address: $"{context.BucketName}/{context.ObjectKey}",
                    context.ObjectKey,
                    Size: context.Size,
                    Type: context.TypeCode),
                ct);
        }
        catch
        {
            // Compensating transaction: remove uploaded object on DB failure
            await CompensateStorageUploadAsync(context, ct);
            throw;
        }
    }

    /// <summary>
    /// Compensates a failed database insert by removing the uploaded object
    /// </summary>
    private async Task CompensateStorageUploadAsync(UploadContext context, CancellationToken ct)
    {
        try
        {
            await minio.RemoveObjectAsync(
                new RemoveObjectArgs()
                    .WithBucket(context.BucketName.ToLowerInvariant())
                    .WithObject(context.ObjectKey), ct);
        }
        catch
        {
            // Swallow compensation failure; original exception will bubble
        }
    }

    /// <summary>
    /// Creates the final upload result
    /// </summary>
    public static UploadResult CreateResult(Guid documentId, UploadContext context)
    {
        return new UploadResult(
            documentId,
            context.BucketName,
            context.ObjectKey,
            context.DocumentName,
            $"{context.BucketName}/{context.ObjectKey}",
            context.Size,
            context.TypeCode,
            context.UploadedAtUtc
        );
    }
}