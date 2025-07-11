namespace Credo.Core.Minio.DI;

public class CredoMinioStorageConfiguration
{
    public required string Endpoint { get; set; }
    public required int Port { get; set; }
    public required string AccessKey { get; set; }
    public required string SecretKey { get; set; }
}