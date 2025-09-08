using System.Buffers;
using System.Globalization;
using System.Text;
using CsvHelper;
using CsvHelper.Configuration;

namespace Credo.Core.FileStorage.Validation.MimeProbes;

public sealed class CsvProbe : IFileTypeProbe
{
    public async Task<short?> TryDetectAsync(Stream content, string fileName, string? providedMime,
        byte[] head, FileTypeInspectorOptions opts, CancellationToken ct)
    {
        if (!(HintsCsv(fileName, providedMime) || LooksLikeText(head, opts))) return null;
        if (!HasAnyDelimiter(head, opts.CsvDelimiters)) return null;

        // sample small window
        content.Position = 0;
        var pool = ArrayPool<byte>.Shared;
        byte[] rented = pool.Rent(opts.CsvSampleBytes);
        try
        {
            int read = await content.ReadAsync(rented.AsMemory(0, opts.CsvSampleBytes), ct);
            using var sample = new MemoryStream(rented, 0, read, writable: false, publiclyVisible: true);

            using var reader = new StreamReader(sample, new UTF8Encoding(false), detectEncodingFromByteOrderMarks: true, leaveOpen: true);

            foreach (var d in opts.CsvDelimiters)
            {
                sample.Position = 0;
                reader.DiscardBufferedData();
                reader.BaseStream.Position = 0;

                var cfg = new CsvConfiguration(CultureInfo.InvariantCulture)
                {
                    HasHeaderRecord = true,
                    Delimiter = d.ToString(),
                    DetectColumnCountChanges = true,
                    BadDataFound = null,
                    MissingFieldFound = null
                };

                using var csv = new CsvReader(reader, cfg);
                int? expected = null; int rows = 0;

                try
                {
                    if (await csv.ReadAsync())
                    {
                        csv.ReadHeader();
                        expected = csv.HeaderRecord?.Length;
                        if (expected is null || expected < opts.CsvMinCols) continue;
                    }

                    while (rows < 50 && await csv.ReadAsync())
                    {
                        rows++;
                        var cols = csv.Parser.Count;
                        if (cols < opts.CsvMinCols || cols != expected) { expected = null; break; }
                    }
                }
                catch
                {
                    continue;
                }

                if (rows >= opts.CsvMinRows && expected is >= int.MinValue) return DocumentTypeCodes.Csv;
            }

            return null;
        }
        finally
        {
            pool.Return(rented, clearArray: false);
        }
    }

    private static bool HintsCsv(string name, string? mime)
        => (!string.IsNullOrWhiteSpace(name) && name.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
        || string.Equals(mime, "text/csv", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mime, "application/csv", StringComparison.OrdinalIgnoreCase)
        || string.Equals(mime, "application/vnd.ms-excel", StringComparison.OrdinalIgnoreCase);

    private static bool LooksLikeText(byte[] head, FileTypeInspectorOptions opts)
    {
        if (head.Length == 0) return false;
        int controls = 0;
        foreach (var b in head)
        {
            if (b == 0) return false;
            if (b < 0x20 && b is not (0x09 or 0x0A or 0x0D)) controls++;
        }
        return (double)controls / head.Length < opts.MaxControlCharRatio;
    }

    private static bool HasAnyDelimiter(byte[] head, char[] delims)
    {
        // cheap ASCII scan
        var set = delims.Select(d => (byte)d).ToHashSet();
        foreach (var b in head)
            if (set.Contains(b)) return true;
        return false;
    }
}
