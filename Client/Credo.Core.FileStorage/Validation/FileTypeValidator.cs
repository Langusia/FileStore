using Credo.Core.FileStorage.Models;

namespace Credo.Core.FileStorage.Validation;

public sealed record TypeValidation(string Mime, short TypeCode);

public static class FileTypeValidator
{
    public static async Task<TypeValidation> ValidateOrThrowAsync(
        Stream seekable, string fileName, string? claimedContentType,
        CancellationToken ct = default)
    {
        if (!seekable.CanSeek) throw new InvalidOperationException("Stream must be seekable for validation.");

        // Read head (don’t consume stream)
        long pos = seekable.Position;
        var headBuf = new byte[Math.Min(8192, (int)Math.Max(0, seekable.Length - seekable.Position))];
        int read = await seekable.ReadAsync(headBuf, ct);
        seekable.Position = pos;
        var head = headBuf.AsSpan(0, read);

        // Step 1: sniff
        var detected = MimeSniffer.Detect(head, fileName);

        // Step 2: specialize ZIP as XLSX by peeking zip entries if needed
        if (detected == "application/x-ooxml-zip")
        {
            // Open ZipArchive safely and look for "xl/" folder
            pos = seekable.Position;
            using (var zip = new System.IO.Compression.ZipArchive(seekable, System.IO.Compression.ZipArchiveMode.Read, leaveOpen: true))
            {
                bool isXlsx = zip.Entries.Any(e => e.FullName.StartsWith("xl/", StringComparison.OrdinalIgnoreCase));
                detected = isXlsx
                    ? "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
                    : "application/octet-stream";
            }
            seekable.Position = pos; // restore
        }

        // Step 3: fallback to extension if unknown
        var ext = Path.GetExtension(fileName ?? string.Empty);
        var mimeFromExt = AllowedFileTypes.ExtToMime.TryGetValue(ext, out var m) ? m : null;

        // Canonical mime to use:
        var mime = detected != "application/octet-stream" ? detected
                 : !string.IsNullOrWhiteSpace(mimeFromExt) ? mimeFromExt!
                 : (string.IsNullOrWhiteSpace(claimedContentType) ? "application/octet-stream" : claimedContentType!);

        // Step 4: enforce allow-list
        if (!AllowedFileTypes.AllowedMimes.Contains(mime))
            throw new InvalidOperationException($"File type '{mime}' is not allowed. Allowed: {string.Join(", ", AllowedFileTypes.AllowedMimes)}");

        // Optional: if client claimed a conflicting type, you can reject here
        if (!string.IsNullOrWhiteSpace(claimedContentType) &&
            !claimedContentType.Equals(mime, StringComparison.OrdinalIgnoreCase))
        {
            // choose strict or soft behavior; strict shown here:
            throw new InvalidOperationException($"Claimed Content-Type '{claimedContentType}' does not match file bytes ('{mime}').");
        }

        // Map to your SMALLINT type code
        var typeCode = DocumentTypeCodes.From(mime, ext.TrimStart('.'));
        return new TypeValidation(mime, typeCode);
    }
}
