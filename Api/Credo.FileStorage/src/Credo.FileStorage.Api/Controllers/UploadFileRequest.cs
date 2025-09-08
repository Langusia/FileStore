using System.ComponentModel;

namespace Credo.FileStorage.Api.Controllers;

public record UploadFileRequest
{
    [DefaultValue("default")] public string Channel { get; set; }
    [DefaultValue("default")] public string Operation { get; set; }
    [DefaultValue(90)] public int TransitionAfterDays { get; set; }
    public int? ExpirationAfterDays { get; set; }
    public IFormFile file { get; set; }
}