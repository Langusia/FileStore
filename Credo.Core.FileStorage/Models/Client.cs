namespace Credo.Core.FileStorage.Models;

public record Client
{
    public Client(string Channel, string Operation)
    {
        this.Channel = Channel;
        this.Operation = Operation;
    }

    public Client()
    {
        
    }

    public string ToBucketName() => $"{Channel}/{Operation}";
    public string Channel { get; init; }
    public string Operation { get; init; }

    public void Deconstruct(out string Channel, out string Operation)
    {
        Channel = this.Channel;
        Operation = this.Operation;
    }
}