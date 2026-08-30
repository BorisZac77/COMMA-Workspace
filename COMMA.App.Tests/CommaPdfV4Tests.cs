using System.Text;
using System.Text.Json;
using System.IO.Compression;
using Avalonia.Controls;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;

namespace COMMA.App.Tests;

public sealed class CommaPdfV4Tests
{
    [Fact]
    public void Manifest_UsesExplicitV4IdentityAndEmbeddedPackage()
    {
        using var fixture = new V4PdfFixture();
        using var document = JsonDocument.Parse(fixture.ReadJson());
        var root = document.RootElement;

        Assert.DoesNotContain(
            OrderPdfV4DataEmbedder.HiddenDataBeginMarker,
            fixture.RawPdfText);
        Assert.DoesNotContain(
            OrderPdfV4DataEmbedder.HiddenDataEndMarker,
            fixture.RawPdfText);
        Assert.Equal(
            "COMMA Workspace Production Card",
            root.GetProperty("Format").GetString());
        Assert.Equal(4, root.GetProperty("FormatVersion").GetInt32());
        Assert.Equal("4.0.0", root.GetProperty("ApplicationVersion").GetString());
    }

    [Fact]
    public void EmbeddedPackage_IsZipContainingUtf8ManifestAndAttachmentBlob()
    {
        using var fixture = new V4PdfFixture();
        var packageBytes = fixture.ReadPackageBytes();

        using var packageStream = new MemoryStream(packageBytes, writable: false);
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read);

        Assert.Equal(2, archive.Entries.Count);
        var entry = archive.GetEntry(
            OrderPdfV4DataEmbedder.ManifestEntryName);
        Assert.NotNull(entry);
        Assert.Contains(
            archive.Entries,
            item => item.FullName ==
                OrderAttachmentValidator.CreateBlobEntry(
                    V4PdfFixture.AttachmentId,
                    ".pdf"));

        using var entryStream = entry.Open();
        using var manifestStream = new MemoryStream();
        entryStream.CopyTo(manifestStream);

        var strictUtf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
        var json = strictUtf8.GetString(manifestStream.ToArray());

        using var document = JsonDocument.Parse(json);
        Assert.Equal(4, document.RootElement.GetProperty("FormatVersion").GetInt32());
    }

    [Fact]
    public void V4RoundTrip_RestoresCanonicalOrderDescriptionsAndAttachmentMetadata()
    {
        using var fixture = new V4PdfFixture();

        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);

        Assert.Equal(4, data.FormatVersion);
        Assert.Equal("4.0.0", data.ApplicationVersion);
        Assert.Equal("ZL-2026-0042", data.OrderNumber);
        Assert.Equal("BINNEN BOUWERS – ŻÓŁĆ", data.OrderName);
        Assert.Equal("Binnen Bouwers", data.Customer);
        Assert.Equal("31.08.2026", data.DueDate);
        Assert.Equal("HAFT", data.ProductionType);

        var garment = Assert.Single(data.Garments);
        Assert.Equal("ROUND-001", garment.ProductCode);
        Assert.Equal("Rounders bluza", garment.ProductName);
        Assert.Equal("Rounders bluza", garment.Name);
        Assert.Equal("granatowy", garment.Colour);
        Assert.Equal("STAFF", garment.Variant);
        Assert.True(garment.ShowFront);
        Assert.True(garment.ShowBack);
        Assert.False(garment.ShowLeft);
        Assert.True(garment.ShowRight);
        Assert.True(garment.StartNewPage);
        Assert.Equal("Opis przodu", garment.ViewDescriptions.Front);
        Assert.Equal("Opis tyłu", garment.ViewDescriptions.Back);
        Assert.Equal("Opis prawego boku", garment.ViewDescriptions.Right);
        Assert.Equal("Opis lewego boku", garment.ViewDescriptions.Left);

        Assert.Equal(4, data.ProductionEntries.Count);
        Assert.Equal("DE BINNEN", data.ProductionEntries[0].LogoName);
        Assert.Equal("7cm", data.ProductionEntries[0].Dimension);
        Assert.Equal(
            "granatowy 533c",
            Assert.Single(data.ProductionEntries[0].Colours).Value);

        var attachment = Assert.Single(data.Attachments);
        Assert.Equal(V4PdfFixture.AttachmentId, attachment.Id);
        Assert.Equal("instrukcja.pdf", attachment.Name);
        Assert.Equal("application/pdf", attachment.MimeType);
        Assert.Equal(".pdf", attachment.Extension);
        Assert.Equal(0, attachment.Order);
        Assert.Equal(fixture.AttachmentLength, attachment.Length);
        Assert.Equal(fixture.AttachmentSha256, attachment.Sha256);
        Assert.Equal(
            OrderAttachmentValidator.CreateBlobEntry(
                V4PdfFixture.AttachmentId,
                ".pdf"),
            attachment.BlobEntry);
        Assert.Equal(1, attachment.PdfPageCount);
    }

    [Fact]
    public void PackageLoadedDescriptionEditedToControllerBoundaryPassesFinalValidation()
    {
        using var fixture = new V4PdfFixture();
        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);
        var loaded = Assert.Single(data.Garments);
        var selectedDrawingCount = 4;
        var textBox = new TextBox
        {
            Text = loaded.ViewDescriptions.Front
        };
        using var controller =
            new GarmentViewDescriptionTextBoxController(
                textBox,
                () => selectedDrawingCount);

        textBox.Text = string.Join(
            '\n',
            Enumerable.Repeat(
                "ghjghjghjghjghj żółć WIELKIE LITERY",
                100));

        Assert.NotEqual(loaded.ViewDescriptions.Front, textBox.Text);
        Assert.Equal(textBox.Text, controller.AcceptedText);
        Assert.True(controller.IsAtCapacity);
        Assert.True(
            controller.IsCurrentTextValidForCommit(
                selectedDrawingCount));
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                textBox.Text,
                selectedDrawingCount));
    }

    [Fact]
    public void EmptyOrderNumber_IsAllowedAndRoundTripsAsEmptyInV4()
    {
        using var fixture = new V4PdfFixture(orderNumber: "");

        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);

        Assert.Equal(4, data.FormatVersion);
        Assert.Equal("", data.OrderNumber);
        Assert.Equal("BINNEN BOUWERS – ŻÓŁĆ", data.OrderName);
    }

    [Fact]
    public void V4Manifest_DoesNotSerializePagePlanOrBinaryAttachmentContent()
    {
        using var fixture = new V4PdfFixture();
        var json = fixture.ReadJson();

        Assert.DoesNotContain("OrderPageLayout", json, StringComparison.Ordinal);
        Assert.DoesNotContain("PageNumber", json, StringComparison.Ordinal);
        Assert.DoesNotContain("TotalPages", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Fragments", json, StringComparison.Ordinal);
        Assert.DoesNotContain("byte[]", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Base64", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnknownNewerVersion_IsNotGuessedAsV3()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("unknown-version.pdf");
        var json = JsonSerializer.Serialize(new
        {
            Format = "COMMA Workspace Production Card",
            FormatVersion = 99,
            ApplicationVersion = "99.0.0"
        });
        var payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        var pdf = string.Join(
            '\n',
            "%PDF-1.4",
            "%%EOF",
            "%COMMA-WORKSPACE-DATA-BEGIN",
            $"%{payload}",
            "%COMMA-WORKSPACE-DATA-END");
        File.WriteAllText(pdfPath, pdf, new UTF8Encoding(false));

        var exception = Assert.Throws<NotSupportedException>(
            () => CommaPdfDataReader.Read(pdfPath));

        Assert.Contains("99", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Nieobsługiwana wersja",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void EmbeddedPackage_RequiresExactlyFormatVersionFour()
    {
        using var directory = new TemporaryDirectory();
        var sourcePdfPath = directory.GetPath("source.pdf");
        var packagePath = directory.GetPath(
            OrderPdfV4DataEmbedder.EmbeddedPackageFileName);
        var outputPdfPath = directory.GetPath("unknown-package-version.pdf");

        CreateValidSourcePdf(sourcePdfPath);
        CreatePackageWithVersion(packagePath, 99);

        DocumentOperation
            .LoadFile(sourcePdfPath)
            .AddAttachment(
                new DocumentOperation.DocumentAttachment
                {
                    Key = OrderPdfV4DataEmbedder.EmbeddedPackageKey,
                    FilePath = packagePath,
                    AttachmentName =
                        OrderPdfV4DataEmbedder.EmbeddedPackageFileName,
                    MimeType = "application/zip",
                    Relationship =
                        DocumentOperation.DocumentAttachmentRelationship.Data
                })
            .Save(outputPdfPath);

        var exception = Assert.Throws<NotSupportedException>(
            () => CommaPdfDataReader.Read(outputPdfPath));

        Assert.Contains("99", exception.Message, StringComparison.Ordinal);
        Assert.Contains(
            "Nieobsługiwana wersja",
            exception.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    public void OldV4MarkerTransport_IsStillReadable()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("old-v4-marker.pdf");
        CreateValidSourcePdf(pdfPath);
        AppendOldV4Marker(pdfPath, "OLD-V4-0042");

        var data = CommaPdfDataReader.Read(pdfPath);

        Assert.Equal(4, data.FormatVersion);
        Assert.Equal("OLD-V4-0042", data.OrderNumber);
        var garment = Assert.Single(data.Garments);
        Assert.Equal("Stary opis przodu: żółć", garment.ViewDescriptions.Front);
        Assert.Equal("Stary opis tyłu", garment.ViewDescriptions.Back);
    }

    [Fact]
    public void EmbeddedPackage_HasPriorityOverOldV4Marker()
    {
        using var fixture = new V4PdfFixture();
        AppendOldV4Marker(fixture.OutputPdfPath, "MARKER-VALUE");

        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);

        Assert.Equal("ZL-2026-0042", data.OrderNumber);
    }

    [Fact]
    public void Duplicate_DeepCopiesAllViewDescriptions()
    {
        var garment = V4PdfFixture.CreateGarment();

        var duplicate = garment.Duplicate();

        Assert.NotSame(garment.ViewDescriptions, duplicate.ViewDescriptions);
        Assert.Equal("Opis przodu", duplicate.ViewDescriptions.Front);
        Assert.Equal("Opis tyłu", duplicate.ViewDescriptions.Back);
        Assert.Equal("Opis prawego boku", duplicate.ViewDescriptions.Right);
        Assert.Equal("Opis lewego boku", duplicate.ViewDescriptions.Left);

        garment.ViewDescriptions.Front = "Zmieniony opis";
        Assert.Equal("Opis przodu", duplicate.ViewDescriptions.Front);
    }

    private static void CreateValidSourcePdf(string path)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Content().Text("COMMA Workspace v4 marker compatibility");
            });
        }).GeneratePdf(path);
    }

    private static void AppendOldV4Marker(
        string pdfPath,
        string orderNumber)
    {
        var manifest = new CommaV4Manifest
        {
            Format = OrderPdfV4DataEmbedder.FormatName,
            FormatVersion = 4,
            ApplicationVersion = OrderPdfV4DataEmbedder.ApplicationVersion,
            OrderNumber = orderNumber,
            OrderName = "OLD V4 MARKER",
            Garments =
            [
                new CommaV4GarmentData
                {
                    ProductCode = "OLD-V4-GARMENT",
                    ProductName = "Old V4 garment",
                    Name = "Old V4 garment",
                    ShowFront = true,
                    ShowBack = true,
                    ViewDescriptions = new CommaV4GarmentViewDescriptions
                    {
                        Front = "Stary opis przodu: żółć",
                        Back = "Stary opis tyłu"
                    }
                }
            ]
        };
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(manifest);
        var payload = Convert.ToBase64String(manifestBytes);

        using var stream = new FileStream(
            pdfPath,
            FileMode.Append,
            FileAccess.Write,
            FileShare.None);
        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false));

        writer.WriteLine();
        writer.WriteLine(OrderPdfV4DataEmbedder.HiddenDataBeginMarker);
        writer.WriteLine($"%{payload}");
        writer.WriteLine(OrderPdfV4DataEmbedder.HiddenDataEndMarker);
    }

    private static void CreatePackageWithVersion(
        string packagePath,
        int formatVersion)
    {
        using var packageStream = new FileStream(
            packagePath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None);
        using var archive = new ZipArchive(
            packageStream,
            ZipArchiveMode.Create);
        var manifestEntry = archive.CreateEntry(
            OrderPdfV4DataEmbedder.ManifestEntryName);
        using var manifestStream = manifestEntry.Open();

        JsonSerializer.Serialize(
            manifestStream,
            new
            {
                Format = OrderPdfV4DataEmbedder.FormatName,
                FormatVersion = formatVersion,
                ApplicationVersion = "99.0.0"
            });
    }

    private sealed class V4PdfFixture : IDisposable
    {
        public static readonly Guid AttachmentId =
            Guid.Parse("c68a58e6-8723-48af-8615-f7c63aafc1e7");

        private readonly TemporaryDirectory directory = new();
        private readonly OrderAttachmentContentStore attachmentContentStore =
            new();

        public V4PdfFixture(
            string orderNumber = "ZL-2026-0042")
        {
            var sourcePdfPath = directory.GetPath("source.pdf");
            var attachmentPath = directory.GetPath("instrukcja.pdf");
            OutputPdfPath = directory.GetPath("output.pdf");

            CreateValidSourcePdf(sourcePdfPath);
            CreateValidSourcePdf(attachmentPath);

            var stored = attachmentContentStore.ImportFile(
                AttachmentId,
                attachmentPath,
                ".pdf");
            AttachmentLength = stored.Length;
            AttachmentSha256 = stored.Sha256;

            var card = CreateCard(orderNumber);
            var garment = CreateGarment();
            var attachment = new OrderAttachmentMetadata
            {
                Id = AttachmentId,
                Name = "instrukcja.pdf",
                MimeType = "application/pdf",
                Extension = ".pdf",
                Order = 0,
                Length = stored.Length,
                Sha256 = stored.Sha256,
                BlobEntry = OrderAttachmentValidator.CreateBlobEntry(
                    AttachmentId,
                    ".pdf"),
                PdfPageCount = 1
            };

            card.Attachments.Add(attachment);

            OrderPdfV4DataEmbedder.AddEmbeddedData(
                sourcePdfPath,
                OutputPdfPath,
                card,
                [garment],
                attachmentContentStore);

            RawPdfText = Encoding.Latin1.GetString(
                File.ReadAllBytes(OutputPdfPath));
        }

        public string OutputPdfPath { get; }

        public string RawPdfText { get; }

        public long AttachmentLength { get; }

        public string AttachmentSha256 { get; }

        public string ReadJson()
        {
            var packageBytes = ReadPackageBytes();

            using var packageStream = new MemoryStream(
                packageBytes,
                writable: false);
            using var archive = new ZipArchive(
                packageStream,
                ZipArchiveMode.Read);
            var manifestEntry = archive.GetEntry(
                OrderPdfV4DataEmbedder.ManifestEntryName);
            Assert.NotNull(manifestEntry);

            using var manifestEntryStream = manifestEntry.Open();
            using var manifestStream = new MemoryStream();
            manifestEntryStream.CopyTo(manifestStream);

            return new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false,
                    throwOnInvalidBytes: true)
                .GetString(manifestStream.ToArray());
        }

        public byte[] ReadPackageBytes()
        {
            using var document = PdfDocument.Open(OutputPdfPath);
            Assert.True(document.Advanced.TryGetEmbeddedFiles(
                out var embeddedFiles));

            var package = Assert.Single(embeddedFiles, file =>
                string.Equals(
                    file.Name,
                    OrderPdfV4DataEmbedder.EmbeddedPackageFileName,
                    StringComparison.OrdinalIgnoreCase));

            return package.Bytes.ToArray();
        }

        public void Dispose()
        {
            attachmentContentStore.Dispose();
            directory.Dispose();
        }

        public static OrderGarmentItem CreateGarment()
        {
            var garment = OrderTestData.CreateGarment(
                3,
                "Rounders bluza",
                startNewPage: true);

            garment.ProductCode = "ROUND-001";
            garment.Colour = "granatowy";
            garment.Variant = "STAFF";
            garment.ViewDescriptions.Front = "Opis przodu";
            garment.ViewDescriptions.Back = "Opis tyłu";
            garment.ViewDescriptions.Right = "Opis prawego boku";
            garment.ViewDescriptions.Left = "Opis lewego boku";

            return garment;
        }

        private static ProductionCard CreateCard(
            string orderNumber)
        {
            var card = new ProductionCard
            {
                OrderNumber = orderNumber,
                OrderName = "BINNEN BOUWERS – ŻÓŁĆ",
                Customer = "Binnen Bouwers",
                DueDate = "31.08.2026",
                ProductionType = "HAFT"
            };

            card.ProductionEntries[0].LogoName = "DE BINNEN";
            card.ProductionEntries[0].Dimension = "7cm";
            card.ProductionEntries[0].Colours.Add(
                new ProductionColourEntry(1)
                {
                    Value = "granatowy 533c"
                });

            return card;
        }
    }
}
