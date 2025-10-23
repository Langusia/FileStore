namespace Credo.FileStorage.Worker.Models;

public class DocumentMetadata
{
    public long DocumentID { get; set; }
    public int? ChannelId { get; set; }
    public string DocumentName { get; set; }
    public string DocumentExt { get; set; }
    public DateTime RecordDate { get; set; }
    public string ContentType { get; set; }
    public int FileSize { get; set; }
}