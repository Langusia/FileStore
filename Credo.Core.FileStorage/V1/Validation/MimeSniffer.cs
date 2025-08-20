namespace Credo.Core.FileStorage.V1.Validation;

public static class MimeSniffer
{
    public static string Detect(ReadOnlySpan<byte> head, string? fileName = null)
    {
        // PDF
        if (head.Length >= 5 && head[..5].SequenceEqual("%PDF-"u8)) return "application/pdf";
        // PNG
        if (head.Length >= 8 && head[..8].SequenceEqual(new byte[]{0x89,0x50,0x4E,0x47,0x0D,0x0A,0x1A,0x0A})) return "image/png";
        // JPEG
        if (head.Length >= 3 && head[0]==0xFF && head[1]==0xD8 && head[2]==0xFF) return "image/jpeg";

        // Legacy OLE2 container (xls/doc/ppt)
        if (head.Length >= 8 && head[..8].SequenceEqual(new byte[]{0xD0,0xCF,0x11,0xE0,0xA1,0xB1,0x1A,0xE1}))
        {
            // We only allow .xls among OLE2. Require extension to be .xls.
            var ext = Path.GetExtension(fileName ?? string.Empty);
            return ext.Equals(".xls", StringComparison.OrdinalIgnoreCase)
                ? "application/vnd.ms-excel"
                : "application/octet-stream";
        }

        // OOXML (zip) – docx/xlsx/pptx start like ZIP (PK..). We only allow xlsx.
        if (head.Length >= 4 && head[..4].SequenceEqual(new byte[]{0x50,0x4B,0x03,0x04}))
        {
            // We'll check for "xl/" folder to confirm XLSX.
            return "application/x-ooxml-zip";
        }

        // CSV/text is hard to magic-detect reliably; use extension later.
        return "application/octet-stream";
    }
}