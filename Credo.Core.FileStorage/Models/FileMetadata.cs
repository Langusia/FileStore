namespace Credo.Core.FileStorage.Models;

public record FileMetadata
{
    public FileMetadata(
        Guid BucketMetadataId,
        string FileName,
        long Size,
        string ContentType,
        DateTime UploadedAt)
    {
        this.Id = Guid.NewGuid();
        this.BucketMetadataId = BucketMetadataId;
        this.FileName = FileName;
        this.Size = Size;
        this.ContentType = ContentType;
        this.UploadedAt = UploadedAt;
    }

    public FileMetadata()
    {
    }

    public Guid Id { get; init; }
    public Guid BucketMetadataId { get; init; }
    public string FileName { get; init; }
    public long Size { get; init; }
    public string ContentType { get; init; }
    public DateTime UploadedAt { get; init; }

    public void Deconstruct(out Guid Id, out Guid BucketMetadataId, out string FileName, out long Size, out string ContentType, out DateTime UploadedAt)
    {
        Id = this.Id;
        BucketMetadataId = this.BucketMetadataId;
        FileName = this.FileName;
        Size = this.Size;
        ContentType = this.ContentType;
        UploadedAt = this.UploadedAt;
    }
}