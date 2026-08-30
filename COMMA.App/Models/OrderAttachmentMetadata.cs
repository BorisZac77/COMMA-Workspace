using System;

namespace COMMA.App.Models;

public sealed class OrderAttachmentMetadata
{
    public Guid Id { get; set; }

    public string Name { get; set; } = "";

    public string MimeType { get; set; } = "";

    public string Extension { get; set; } = "";

    public int Order { get; set; }

    public long Length { get; set; }

    public string Sha256 { get; set; } = "";

    public string BlobEntry { get; set; } = "";

    public int? PdfPageCount { get; set; }

    public string DisplayType =>
        Extension.TrimStart('.').ToUpperInvariant();

    public string DisplaySize =>
        Length >= 1024 * 1024
            ? $"{Length / (1024d * 1024d):0.##} MB"
            : $"{Math.Max(1, Length / 1024d):0.##} KB";

    public string PageCountText =>
        PdfPageCount is { } pageCount
            ? $"{pageCount} str."
            : "";
}
