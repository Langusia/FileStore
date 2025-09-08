using Credo.Core.FileStorage.Validation.MimeProbes;

namespace Credo.Core.FileStorage.Validation;

public sealed class CompositeFileTypeInspector : IFileTypeInspector
{
    private readonly IReadOnlyList<IFileTypeProbe> _probes;
    private readonly FileTypeInspectorOptions _opts;

    public CompositeFileTypeInspector(IEnumerable<IFileTypeProbe> probes, FileTypeInspectorOptions? opts = null)
    {
        _probes = probes.ToList();
        _opts = opts ?? new();
    }

    public async Task<short> DetectOrThrowAsync(Stream content, string fileName, string? providedMime, CancellationToken ct)
    {
        if (!content.CanSeek) throw new ArgumentException("Stream must be seekable.", nameof(content));
        var pos = content.Position;

        // read head once
        var head = ReadHead(content, _opts.HeadBytes);
        content.Position = pos;

        foreach (var p in _probes)
        {
            var code = await p.TryDetectAsync(content, fileName, providedMime, head, _opts, ct);
            if (code.HasValue) return code.Value;
            content.Position = pos; // ensure rewind per probe
        }
        throw new InvalidOperationException("Unsupported or unrecognized file type.");
    }

    private static byte[] ReadHead(Stream s, int max)
    {
        var start = s.Position;
        var n = (int)Math.Min(max, s.Length - start);
        var buf = new byte[Math.Max(n, 0)];
        _ = s.Read(buf, 0, buf.Length);
        return buf;
    }
}
