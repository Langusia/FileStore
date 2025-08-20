using System.ComponentModel;

namespace Credo.SomeClient.API.Dtos;

public record UploadFileRequest
{
    [DefaultValue("mobile")] public string Channel { get; set; }
    [DefaultValue("utility-payment")] public string Operation { get; set; }
    [DefaultValue(90)] public int TransitionAfterDays { get; set; }
    [DefaultValue(null)] public int? ExpirationAfterDays { get; set; }
    public IFormFile file { get; set; }
}