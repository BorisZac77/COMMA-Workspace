using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using COMMA.App.Models;

namespace COMMA.App.Services.Pdf;

public static class OrderPdfV4DataEmbedder
{
    public const string HiddenDataBeginMarker =
        "%COMMA-WORKSPACE-V4-DATA-BEGIN";

    public const string HiddenDataEndMarker =
        "%COMMA-WORKSPACE-V4-DATA-END";

    public const string FormatName =
        "COMMA Workspace Production Card";

    public const int FormatVersion =
        4;

    public const string ApplicationVersion =
        "4.0.0";

    public static void AddEmbeddedData(
        string sourcePdfPath,
        string outputPath,
        ProductionCard card,
        IReadOnlyList<OrderGarmentItem> garments)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(garments);

        if (string.IsNullOrWhiteSpace(sourcePdfPath))
        {
            throw new ArgumentException(
                "Nie podano ścieżki źródłowego pliku PDF.",
                nameof(sourcePdfPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "Nie podano ścieżki docelowego pliku PDF.",
                nameof(outputPath));
        }

        if (!File.Exists(sourcePdfPath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono źródłowego pliku PDF.",
                sourcePdfPath);
        }

        var outputDirectory =
            Path.GetDirectoryName(outputPath);

        if (string.IsNullOrWhiteSpace(outputDirectory))
        {
            outputDirectory =
                Directory.GetCurrentDirectory();
        }

        Directory.CreateDirectory(outputDirectory);

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        File.Copy(
            sourcePdfPath,
            outputPath,
            overwrite: true);

        var manifest = CreateManifest(
            card,
            garments);

        AppendManifest(
            outputPath,
            manifest);
    }

    private static void AppendManifest(
        string pdfPath,
        CommaV4Manifest manifest)
    {
        var json = JsonSerializer.Serialize(manifest);
        var jsonBytes = Encoding.UTF8.GetBytes(json);
        var base64 = Convert.ToBase64String(jsonBytes);

        using var stream = new FileStream(
            pdfPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.None);

        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(
                encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine();
        writer.WriteLine(HiddenDataBeginMarker);

        const int lineLength = 120;

        for (var index = 0; index < base64.Length; index += lineLength)
        {
            var length = Math.Min(
                lineLength,
                base64.Length - index);

            writer.Write("%");
            writer.WriteLine(base64.Substring(index, length));
        }

        writer.WriteLine(HiddenDataEndMarker);
    }

    private static CommaV4Manifest CreateManifest(
        ProductionCard card,
        IReadOnlyList<OrderGarmentItem> garments)
    {
        return new CommaV4Manifest
        {
            Format = FormatName,
            FormatVersion = FormatVersion,
            ApplicationVersion = ApplicationVersion,
            SavedUtc = DateTime.UtcNow,
            OrderNumber = Safe(card.OrderNumber),
            OrderName = Safe(card.OrderName),
            Customer = Safe(card.Customer),
            ReceivedDate = Safe(card.ReceivedDate),
            DueDate = Safe(card.DueDate),
            ProductionType = Safe(card.ProductionType),
            ProductCode = Safe(card.ProductCode),
            ProductName = Safe(card.ProductName),
            Colour = Safe(card.Colour),
            Size = Safe(card.Size),
            Quantity = Safe(card.Quantity),
            Notes = Safe(card.Notes),
            ShowFront = card.ShowFront,
            ShowBack = card.ShowBack,
            ShowLeft = card.ShowLeft,
            ShowRight = card.ShowRight,
            Garments = garments
                .Select(CreateGarmentData)
                .ToList(),
            ProductionEntries = card.ProductionEntries
                .Select(CreateProductionEntryData)
                .ToList(),
            Attachments = card.Attachments
                .OrderBy(attachment => attachment.Order)
                .Select(CreateAttachmentData)
                .ToList()
        };
    }

    private static CommaV4GarmentData CreateGarmentData(
        OrderGarmentItem garment)
    {
        return new CommaV4GarmentData
        {
            ProductCode = Safe(garment.ProductCode),
            ProductName = Safe(garment.Name),
            Name = Safe(garment.Name),
            Colour = Safe(garment.Colour),
            Variant = Safe(garment.Variant),
            ShowFront = garment.ShowFront,
            ShowBack = garment.ShowBack,
            ShowRight = garment.ShowRight,
            ShowLeft = garment.ShowLeft,
            StartNewPage = garment.StartNewPage,
            ViewDescriptions = new CommaV4GarmentViewDescriptions
            {
                Front = Safe(garment.ViewDescriptions.Front),
                Back = Safe(garment.ViewDescriptions.Back),
                Right = Safe(garment.ViewDescriptions.Right),
                Left = Safe(garment.ViewDescriptions.Left)
            }
        };
    }

    private static CommaV4ProductionEntryData CreateProductionEntryData(
        ProductionEntry entry)
    {
        return new CommaV4ProductionEntryData
        {
            Number = entry.Number,
            LogoName = Safe(entry.LogoName),
            Dimension = Safe(entry.Dimension),
            Colours = entry.Colours
                .Select(colour =>
                    new CommaV4ColourData
                    {
                        Number = colour.Number,
                        Value = Safe(colour.Value)
                    })
                .ToList()
        };
    }

    private static CommaV4AttachmentMetadata CreateAttachmentData(
        OrderAttachmentMetadata attachment)
    {
        return new CommaV4AttachmentMetadata
        {
            Id = attachment.Id,
            Name = Safe(attachment.Name),
            MimeType = Safe(attachment.MimeType),
            Extension = Safe(attachment.Extension),
            Order = attachment.Order,
            Length = attachment.Length,
            Sha256 = Safe(attachment.Sha256),
            BlobEntry = Safe(attachment.BlobEntry)
        };
    }

    private static string Safe(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
