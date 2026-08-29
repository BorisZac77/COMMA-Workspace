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
}
