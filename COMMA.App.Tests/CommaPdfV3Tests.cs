using System.Text;
using System.Text.Json;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class CommaPdfV3Tests
{
    private const string BeginMarker =
        "%COMMA-WORKSPACE-DATA-BEGIN";

    private const string EndMarker =
        "%COMMA-WORKSPACE-DATA-END";

    [Fact]
    public void EmbeddedPayload_UsesV3Utf8Base64Envelope()
    {
        using var fixture = new V3PdfFixture();

        var encodedPayload = fixture.ReadEncodedPayload();
        var jsonBytes = Convert.FromBase64String(encodedPayload);
        var strictUtf8 = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true);
        var json = strictUtf8.GetString(jsonBytes);

        using var document = JsonDocument.Parse(json);
        var root = document.RootElement;

        Assert.Contains(BeginMarker, fixture.RawPdfText);
        Assert.Contains(EndMarker, fixture.RawPdfText);
        Assert.Equal(
            "COMMA Workspace Production Card",
            root.GetProperty("Format").GetString());
        Assert.Equal(3, root.GetProperty("FormatVersion").GetInt32());
        Assert.Equal("3.0.0", root.GetProperty("ApplicationVersion").GetString());
    }

    [Fact]
    public void V3RoundTrip_RestoresOrderGarmentsAndProductionEntries()
    {
        using var fixture = new V3PdfFixture();

        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);

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
        Assert.False(garment.ShowRight);
        Assert.True(garment.StartNewPage);

        Assert.Equal(4, data.ProductionEntries.Count);
        Assert.Equal("DE BINNEN", data.ProductionEntries[0].LogoName);
        Assert.Equal("7cm", data.ProductionEntries[0].Dimension);
        Assert.Equal(
            "granatowy 533c",
            Assert.Single(data.ProductionEntries[0].Colours).Value);
    }

    [Fact]
    public void V3Payload_DoesNotPersistAbsoluteProductOrDrawingPaths()
    {
        using var fixture = new V3PdfFixture();

        var jsonBytes = Convert.FromBase64String(
            fixture.ReadEncodedPayload());
        var json = Encoding.UTF8.GetString(jsonBytes);

        Assert.DoesNotContain("/Users/", json, StringComparison.Ordinal);
        Assert.DoesNotContain(@"C:\", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ProductPath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("DrawingPath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("ImagePath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("FullPath", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Folder", json, StringComparison.Ordinal);
    }

    private sealed class V3PdfFixture : IDisposable
    {
        private readonly TemporaryDirectory directory = new();

        public V3PdfFixture()
        {
            var sourcePdfPath = directory.GetPath("source.pdf");
            OutputPdfPath = directory.GetPath("output.pdf");

            File.WriteAllBytes(
                sourcePdfPath,
                "%PDF-1.4\n%%EOF\n"u8.ToArray());

            var card = CreateCard();
            var garment = CreateGarment();
            var pages = OrderTestData.CreatePages(garment);

            OrderPdfDataEmbedder.AddEmbeddedData(
                sourcePdfPath,
                OutputPdfPath,
                card,
                pages);

            RawPdfText = Encoding.Latin1.GetString(
                File.ReadAllBytes(OutputPdfPath));
        }

        public string OutputPdfPath { get; }

        public string RawPdfText { get; }

        public string ReadEncodedPayload()
        {
            var beginIndex = RawPdfText.LastIndexOf(
                BeginMarker,
                StringComparison.Ordinal);
            var endIndex = RawPdfText.IndexOf(
                EndMarker,
                beginIndex + BeginMarker.Length,
                StringComparison.Ordinal);

            Assert.True(beginIndex >= 0);
            Assert.True(endIndex > beginIndex);

            var block = RawPdfText.Substring(
                beginIndex + BeginMarker.Length,
                endIndex - beginIndex - BeginMarker.Length);

            return string.Concat(
                block
                    .Split(
                        ["\r\n", "\n", "\r"],
                        StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim().TrimStart('%')));
        }

        public void Dispose()
        {
            directory.Dispose();
        }

        private static ProductionCard CreateCard()
        {
            var card = new ProductionCard
            {
                OrderName = "BINNEN BOUWERS – ŻÓŁĆ",
                Customer = "Binnen Bouwers",
                DueDate = "31.08.2026",
                ProductionType = "HAFT",
                ProductImagePath = "/Users/test/library/product.png",
                ClientLogoPath = @"C:\library\client-logo.png",
                EmbroideryProgramPath = "/Users/test/library/program.dst",
                PrintFilePath = @"C:\library\print.pdf"
            };

            card.ProductionEntries[0].LogoName = "DE BINNEN";
            card.ProductionEntries[0].Dimension = "7cm";
            card.ProductionEntries[0].Colours.Add(
                new ProductionColourEntry(1)
                {
                    Value = "granatowy 533c"
                });

            card.ProductionEntries[1].LogoName =
                "DE BINNEN / GOOD GEDAAN";
            card.ProductionEntries[1].Dimension = "20cm";
            card.ProductionEntries[1].Colours.Add(
                new ProductionColourEntry(1)
                {
                    Value = "granatowy 533c"
                });

            return card;
        }

        private static OrderGarmentItem CreateGarment()
        {
            var garment = OrderTestData.CreateGarment(
                2,
                "Rounders bluza",
                startNewPage: true);

            garment.ProductCode = "ROUND-001";
            garment.Colour = "granatowy";
            garment.Variant = "STAFF";

            return garment;
        }
    }
}
