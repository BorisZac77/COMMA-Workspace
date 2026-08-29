using System;
using System.Collections.Generic;

namespace COMMA.App.Services.Pdf;

public sealed class CommaV4Manifest
{
    public string Format { get; set; } = "";

    public int FormatVersion { get; set; }

    public string ApplicationVersion { get; set; } = "";

    public DateTime SavedUtc { get; set; }

    public string OrderNumber { get; set; } = "";

    public string OrderName { get; set; } = "";

    public string Customer { get; set; } = "";

    public string ReceivedDate { get; set; } = "";

    public string DueDate { get; set; } = "";

    public string ProductionType { get; set; } = "";

    public string ProductCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string Colour { get; set; } = "";

    public string Size { get; set; } = "";

    public string Quantity { get; set; } = "";

    public string Notes { get; set; } = "";

    public bool ShowFront { get; set; }

    public bool ShowBack { get; set; }

    public bool ShowLeft { get; set; }

    public bool ShowRight { get; set; }

    public List<CommaV4GarmentData> Garments { get; set; } =
        new();

    public List<CommaV4ProductionEntryData> ProductionEntries { get; set; } =
        new();

    public List<CommaV4AttachmentMetadata> Attachments { get; set; } =
        new();
}

public sealed class CommaV4GarmentData
{
    public string ProductCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string Name { get; set; } = "";

    public string Colour { get; set; } = "";

    public string Variant { get; set; } = "";

    public bool ShowFront { get; set; }

    public bool ShowBack { get; set; }

    public bool ShowRight { get; set; }

    public bool ShowLeft { get; set; }

    public bool StartNewPage { get; set; }

    public CommaV4GarmentViewDescriptions ViewDescriptions { get; set; } =
        new();
}

public sealed class CommaV4GarmentViewDescriptions
{
    public string Front { get; set; } = "";

    public string Back { get; set; } = "";

    public string Right { get; set; } = "";

    public string Left { get; set; } = "";
}

public sealed class CommaV4ProductionEntryData
{
    public int Number { get; set; }

    public string LogoName { get; set; } = "";

    public string Dimension { get; set; } = "";

    public List<CommaV4ColourData> Colours { get; set; } =
        new();
}

public sealed class CommaV4ColourData
{
    public int Number { get; set; }

    public string Value { get; set; } = "";
}

public sealed class CommaV4AttachmentMetadata
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
