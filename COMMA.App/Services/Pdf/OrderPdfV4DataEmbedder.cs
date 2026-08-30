using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using QuestPDF.Fluent;

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

    public const string EmbeddedPackageFileName =
        "comma-workspace-v4.package";

    public const string EmbeddedPackageKey =
        EmbeddedPackageFileName;

    public const string ManifestEntryName =
        "manifest.json";

    private const string EmbeddedPackageMimeType =
        "application/zip";

    public static void AddEmbeddedData(
        string sourcePdfPath,
        string outputPath,
        ProductionCard card,
        IReadOnlyList<OrderGarmentItem> garments,
        OrderAttachmentContentStore? attachmentContentStore = null)
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

        var manifest = CreateManifest(
            card,
            garments);

        var temporaryPackagePath = Path.Combine(
            outputDirectory,
            $".comma-workspace-v4-{Guid.NewGuid():N}.package");
        var temporaryOutputPath = Path.Combine(
            outputDirectory,
            $".comma-workspace-v4-output-{Guid.NewGuid():N}.pdf");

        try
        {
            WritePackage(
                temporaryPackagePath,
                manifest,
                card.Attachments
                    .OrderBy(attachment => attachment.Order)
                    .ToList(),
                attachmentContentStore);

            DocumentOperation
                .LoadFile(sourcePdfPath)
                .AddAttachment(
                    new DocumentOperation.DocumentAttachment
                    {
                        Key = EmbeddedPackageKey,
                        FilePath = temporaryPackagePath,
                        AttachmentName = EmbeddedPackageFileName,
                        MimeType = EmbeddedPackageMimeType,
                        Description =
                            "COMMA Workspace 4.0 production card package",
                        Relationship =
                            DocumentOperation.DocumentAttachmentRelationship.Data,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow,
                        Replace = true
                    })
                .Save(temporaryOutputPath);

            File.Move(
                temporaryOutputPath,
                outputPath,
                overwrite: true);
        }
        finally
        {
            TryDeleteFile(temporaryPackagePath);
            TryDeleteFile(temporaryOutputPath);
        }
    }

    private static void WritePackage(
        string packagePath,
        CommaV4Manifest manifest,
        IReadOnlyList<OrderAttachmentMetadata> attachments,
        OrderAttachmentContentStore? attachmentContentStore)
    {
        using var packageStream = new FileStream(
            packagePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);

        using var archive = new ZipArchive(
            packageStream,
            ZipArchiveMode.Create,
            leaveOpen: false);

        if (attachments.Count > 0 && attachmentContentStore == null)
        {
            throw new InvalidDataException(
                "Brakuje magazynu oryginalnej zawartości załączników.");
        }

        if (attachments.Count > OrderAttachmentLimits.MaximumAttachmentCount)
        {
            throw new InvalidDataException(
                "Można zapisać maksymalnie 25 załączników.");
        }

        var usedBlobEntries = new HashSet<string>(StringComparer.Ordinal);
        var usedIds = new HashSet<Guid>();
        long totalLength = 0;
        var totalPdfPages = 0;

        for (var attachmentIndex = 0;
             attachmentIndex < attachments.Count;
             attachmentIndex++)
        {
            var attachment = attachments[attachmentIndex];
            attachment.Order = attachmentIndex;
            if (attachment.Id == Guid.Empty || !usedIds.Add(attachment.Id))
            {
                throw new InvalidDataException(
                    "Załączniki zawierają niepoprawny lub zduplikowany identyfikator.");
            }

            var blobEntry = OrderAttachmentValidator.CreateBlobEntry(
                attachment.Id,
                attachment.Extension);

            if (!usedBlobEntries.Add(blobEntry))
            {
                throw new InvalidDataException(
                    "Załączniki zawierają zduplikowaną ścieżkę BlobEntry.");
            }

            using var content = attachmentContentStore!.OpenRead(attachment.Id);
            var validated = OrderAttachmentValidator.Validate(
                attachment.Name,
                content);
            content.Position = 0;
            attachment.MimeType = validated.MimeType;
            attachment.Extension = validated.Extension;
            attachment.PdfPageCount = validated.PdfPageCount;
            totalPdfPages += validated.PdfPageCount ?? 0;

            if (totalPdfPages > OrderAttachmentLimits.MaximumTotalPdfPages)
            {
                throw new InvalidDataException(
                    "Łączna liczba stron załączników PDF przekracza limit 500.");
            }

            var entry = archive.CreateEntry(blobEntry, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            var buffer = new byte[81920];
            long length = 0;

            while (true)
            {
                var read = content.Read(buffer, 0, buffer.Length);
                if (read == 0)
                    break;

                length += read;
                if (length > OrderAttachmentLimits.MaximumFileBytes)
                {
                    throw new InvalidDataException(
                        "Załącznik przekracza maksymalny rozmiar 50 MB.");
                }

                entryStream.Write(buffer, 0, read);
                hash.AppendData(buffer, 0, read);
            }

            totalLength += length;
            if (totalLength > OrderAttachmentLimits.MaximumTotalBytes)
            {
                throw new InvalidDataException(
                    "Łączny rozmiar załączników przekracza limit 200 MB.");
            }

            var sha256 = Convert.ToHexString(
                    hash.GetHashAndReset())
                .ToLowerInvariant();
            attachment.Length = length;
            attachment.Sha256 = sha256;
            attachment.BlobEntry = blobEntry;

            var manifestAttachment = manifest.Attachments.Single(item =>
                item.Id == attachment.Id);
            manifestAttachment.Length = length;
            manifestAttachment.Sha256 = sha256;
            manifestAttachment.BlobEntry = blobEntry;
            manifestAttachment.Order = attachmentIndex;
            manifestAttachment.MimeType = validated.MimeType;
            manifestAttachment.Extension = validated.Extension;
            manifestAttachment.PdfPageCount = validated.PdfPageCount;
        }

        var manifestBytes =
            JsonSerializer.SerializeToUtf8Bytes(manifest);

        var manifestEntry = archive.CreateEntry(
            ManifestEntryName,
            CompressionLevel.Optimal);

        using var manifestStream = manifestEntry.Open();
        manifestStream.Write(manifestBytes);
    }

    private static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
        }
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
            BlobEntry = Safe(attachment.BlobEntry),
            PdfPageCount = attachment.PdfPageCount
        };
    }

    private static string Safe(string? value)
    {
        return value?.Trim() ?? string.Empty;
    }
}
