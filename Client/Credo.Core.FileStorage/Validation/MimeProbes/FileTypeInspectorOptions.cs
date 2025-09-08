namespace Credo.Core.FileStorage.Validation.MimeProbes;

public sealed class FileTypeInspectorOptions
{
    public int HeadBytes { get; init; } = 560;
    public int CsvSampleBytes { get; init; } = 64 * 1024;
    public int CsvMinRows { get; init; } = 3;
    public int CsvMinCols { get; init; } = 2;
    public double MaxControlCharRatio { get; init; } = 0.02;
    public char[] CsvDelimiters { get; init; } = [',', ';', '\t', '|'];
}