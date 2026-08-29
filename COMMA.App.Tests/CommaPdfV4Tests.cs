using System.Text;
using System.Text.Json;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class CommaPdfV4Tests
{
    [Fact]
    public void Manifest_UsesExplicitV4IdentityAndEnvelope()
    {
        using var fixture = new V4PdfFixture();
        using var document = JsonDocument.Parse(fixture.ReadJson());
        var root = document.RootElement;

        Assert.Contains(
            OrderPdfV4DataEmbedder.HiddenDataBeginMarker,
            fixture.RawPdfText);
        Assert.Contains(
            OrderPdfV4DataEmbedder.HiddenDataEndMarker,
            fixture.RawPdfText);
        Assert.Equal(
            "COMMA Workspace Production Card",
            root.GetProperty("Format").GetString());
        Assert.Equal(4, root.GetProperty("FormatVersion").GetInt32());
        Assert.Equal("4.0.0", root.GetProperty("ApplicationVersion").GetString());
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
        Assert.Equal(2, attachment.Order);
        Assert.Equal(123456, attachment.Length);
        Assert.Equal("ABCDEF012345", attachment.Sha256);
        Assert.Equal("blobs/attachment-1.pdf", attachment.BlobEntry);
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

    private sealed class V4PdfFixture : IDisposable
    {
        public static readonly Guid AttachmentId =
            Guid.Parse("c68a58e6-8723-48af-8615-f7c63aafc1e7");

        private readonly TemporaryDirectory directory = new();

        public V4PdfFixture()
        {
            var sourcePdfPath = directory.GetPath("source.pdf");
            OutputPdfPath = directory.GetPath("output.pdf");

            File.WriteAllBytes(
                sourcePdfPath,
                "%PDF-1.4\n%%EOF\n"u8.ToArray());

            var card = CreateCard();
            var garment = CreateGarment();
            var attachment = new OrderAttachmentMetadata
            {
                Id = AttachmentId,
                Name = "instrukcja.pdf",
                MimeType = "application/pdf",
                Extension = ".pdf",
                Order = 2,
                Length = 123456,
                Sha256 = "ABCDEF012345",
                BlobEntry = "blobs/attachment-1.pdf"
            };

            card.Attachments.Add(attachment);

            OrderPdfV4DataEmbedder.AddEmbeddedData(
                sourcePdfPath,
                OutputPdfPath,
                card,
                [garment]);

            RawPdfText = Encoding.Latin1.GetString(
                File.ReadAllBytes(OutputPdfPath));
        }

        public string OutputPdfPath { get; }

        public string RawPdfText { get; }

        public string ReadJson()
        {
            var beginIndex = RawPdfText.LastIndexOf(
                OrderPdfV4DataEmbedder.HiddenDataBeginMarker,
                StringComparison.Ordinal);
            var endIndex = RawPdfText.IndexOf(
                OrderPdfV4DataEmbedder.HiddenDataEndMarker,
                beginIndex + OrderPdfV4DataEmbedder.HiddenDataBeginMarker.Length,
                StringComparison.Ordinal);

            Assert.True(beginIndex >= 0);
            Assert.True(endIndex > beginIndex);

            var block = RawPdfText.Substring(
                beginIndex + OrderPdfV4DataEmbedder.HiddenDataBeginMarker.Length,
                endIndex - beginIndex -
                OrderPdfV4DataEmbedder.HiddenDataBeginMarker.Length);
            var encoded = string.Concat(
                block
                    .Split(
                        ["\r\n", "\n", "\r"],
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim().TrimStart('%')));

            return Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
        }

        public void Dispose()
        {
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

        private static ProductionCard CreateCard()
        {
            var card = new ProductionCard
            {
                OrderNumber = "ZL-2026-0042",
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
