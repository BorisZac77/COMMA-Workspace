using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using COMMA.App.ViewModels;
using UglyToad.PdfPig;

namespace COMMA.App.Tests;

public sealed class Workspace3RealPdfCompatibilityTests
{
    private const string ExpectedSha256 =
        "311da389f2cb3b0afda806b2809a7ab9f5e06aa9d664496957879ab8d0111357";

    private const string BeginMarker =
        "%COMMA-WORKSPACE-DATA-BEGIN";

    private const string EndMarker =
        "%COMMA-WORKSPACE-DATA-END";

    [Fact]
    public void TestNowaKarta_RealWorkspace3Pdf_RestoresExactV3OrderData()
    {
        var fixturePath = GetFixturePath();
        var before = SnapshotFile(fixturePath);
        var bytes = File.ReadAllBytes(fixturePath);
        var rawPdf = Encoding.Latin1.GetString(bytes);

        Assert.Equal(ExpectedSha256, before.Sha256);
        Assert.Equal(837340, before.Length);

        using (var document = PdfDocument.Open(fixturePath))
        {
            Assert.Equal(8, document.NumberOfPages);
        }

        var eofIndex = rawPdf.IndexOf("%%EOF", StringComparison.Ordinal);
        var beginIndex = rawPdf.IndexOf(BeginMarker, StringComparison.Ordinal);
        var endIndex = rawPdf.IndexOf(EndMarker, StringComparison.Ordinal);

        Assert.True(eofIndex >= 0);
        Assert.Equal(1, CountOccurrences(rawPdf, "%%EOF"));
        Assert.Equal(1, CountOccurrences(rawPdf, BeginMarker));
        Assert.Equal(1, CountOccurrences(rawPdf, EndMarker));
        Assert.True(beginIndex > eofIndex);
        Assert.True(endIndex > beginIndex);

        using (var rawData = JsonDocument.Parse(
                   ReadMarkedPayload(rawPdf, beginIndex, endIndex)))
        {
            var rawGarments = rawData.RootElement.GetProperty("Garments");
            Assert.True(rawGarments[3].GetProperty("StartNewPage").GetBoolean());
            Assert.False(rawGarments[4].GetProperty("StartNewPage").GetBoolean());
        }

        var data = CommaPdfDataReader.Read(fixturePath);

        Assert.Equal("COMMA Workspace Production Card", data.Format);
        Assert.Equal(3, data.FormatVersion);
        Assert.Equal("3.0.0", data.ApplicationVersion);
        Assert.Equal("TEST NOWA KARTA", data.OrderName);
        Assert.Equal("", data.OrderNumber);
        Assert.Equal("", data.Customer);
        Assert.Equal("", data.ReceivedDate);
        Assert.Equal("", data.DueDate);
        Assert.Equal("", data.ProductionType);
        Assert.Equal("", data.ProductCode);
        Assert.Equal("0510 T-Time t-shirt", data.ProductName);
        Assert.Empty(data.Attachments);

        var expectedGarments = new[]
        {
            new ExpectedGarment(
                "0510 T-Time t-shirt", "", "",
                true, true, true, true, false),
            new ExpectedGarment(
                "0525 Poloshirt stretch polo", "", "",
                false, true, true, true, true),
            new ExpectedGarment(
                "Burnwood Cap czapka", "", "",
                true, true, true, true, true),
            new ExpectedGarment(
                "Bluza nierozpinana", "", "",
                true, false, false, false, true),
            new ExpectedGarment(
                "Larkford polo", "", "",
                false, true, true, true, true),
            new ExpectedGarment(
                "LA Cap czapka", "", "",
                true, true, true, true, true),
            new ExpectedGarment(
                "Koszula me\u0328ska kro\u0301tki re\u0328kaw", "", "",
                true, true, true, false, true),
            new ExpectedGarment(
                "Koszula me\u0328ska d\u0142ugi re\u0328kaw", "", "",
                false, false, false, true, true)
        };

        Assert.Equal(expectedGarments.Length, data.Garments.Count);

        for (var index = 0; index < expectedGarments.Length; index++)
        {
            var expected = expectedGarments[index];
            var actual = data.Garments[index];

            Assert.Equal("", actual.ProductCode);
            Assert.Equal(expected.Name, actual.ProductName);
            Assert.Equal(expected.Name, actual.Name);
            Assert.Equal(expected.Colour, actual.Colour);
            Assert.Equal(expected.Variant, actual.Variant);
            Assert.Equal(expected.ShowFront, actual.ShowFront);
            Assert.Equal(expected.ShowBack, actual.ShowBack);
            Assert.Equal(expected.ShowRight, actual.ShowRight);
            Assert.Equal(expected.ShowLeft, actual.ShowLeft);
            Assert.Equal(expected.StartNewPage, actual.StartNewPage);
            Assert.Equal("", actual.ViewDescriptions.Front);
            Assert.Equal("", actual.ViewDescriptions.Back);
            Assert.Equal("", actual.ViewDescriptions.Right);
            Assert.Equal("", actual.ViewDescriptions.Left);
        }

        Assert.Equal(4, data.ProductionEntries.Count);

        for (var index = 0; index < data.ProductionEntries.Count; index++)
        {
            var entry = data.ProductionEntries[index];
            Assert.Equal(index + 1, entry.Number);
            Assert.Equal("", entry.LogoName);
            Assert.Equal("", entry.Dimension);
            Assert.Empty(entry.Colours);
        }

        var restoredCard = new ProductionCard();
        RestoreCardThroughProductionPath(restoredCard, data);

        Assert.Equal("TEST NOWA KARTA", restoredCard.OrderName);
        Assert.Equal("", restoredCard.OrderNumber);
        Assert.Equal("", restoredCard.ProductCode);
        Assert.Equal("0510 T-Time t-shirt", restoredCard.ProductName);
        Assert.Empty(restoredCard.Attachments);
        Assert.Equal(4, restoredCard.ProductionEntries.Count);
        Assert.All(
            restoredCard.ProductionEntries,
            entry =>
            {
                Assert.Equal("", entry.LogoName);
                Assert.Equal("", entry.Dimension);
                Assert.Empty(entry.Colours);
            });

        var after = SnapshotFile(fixturePath);
        Assert.Equal(before, after);
    }

    [Fact]
    public void TestNowaKarta_RenamedCopy_IsRecognizedFromEmbeddedVersionData()
    {
        using var directory = new TemporaryDirectory();
        var renamedPath = directory.GetPath("dowolna-nazwa-bez-v3.pdf");
        File.Copy(GetFixturePath(), renamedPath);

        using var data = CommaPdfDataReader.Read(renamedPath);

        Assert.Equal(3, data.FormatVersion);
        Assert.Equal("3.0.0", data.ApplicationVersion);
        Assert.Equal("TEST NOWA KARTA", data.OrderName);
        Assert.Equal(8, data.Garments.Count);
    }

    [Fact]
    public void TestNowaKarta_MigratesToV4AndPreservesAllAvailableSemantics()
    {
        var fixturePath = GetFixturePath();
        var before = SnapshotFile(fixturePath);
        using var directory = new TemporaryDirectory();
        var cleanPdfPath = directory.GetPath("clean-visual-source.pdf");
        var migratedPath = directory.GetPath("migrated-workspace-4.pdf");
        CreateCleanPdfCopy(fixturePath, cleanPdfPath);

        using var sourceData = CommaPdfDataReader.Read(fixturePath);
        var card = RestoreRuntimeCard(sourceData);
        var garments = RestoreRuntimeGarments(sourceData);

        Assert.Empty(card.Attachments);
        AssertCurrentWorkspace4PagePlan(garments);
        Assert.True(garments.Single(garment =>
            garment.Name == "Larkford polo").StartNewPage);

        OrderPdfV4DataEmbedder.AddEmbeddedData(
            cleanPdfPath,
            migratedPath,
            card,
            garments);

        using var migratedData = CommaPdfDataReader.Read(migratedPath);

        Assert.Equal(4, migratedData.FormatVersion);
        AssertSemanticDataEqual(sourceData, migratedData);
        Assert.True(migratedData.Garments.Single(garment =>
            garment.Name == "Larkford polo").StartNewPage);
        Assert.Empty(migratedData.Attachments);
        Assert.Equal(before, SnapshotFile(fixturePath));

        File.Delete(migratedPath);
        Assert.False(File.Exists(migratedPath));
    }

    [Fact]
    public void TestNowaKarta_CanBeEditedAndSavedAsV4WithNewAttachment()
    {
        var fixturePath = GetFixturePath();
        var before = SnapshotFile(fixturePath);
        using var directory = new TemporaryDirectory();
        using var attachmentManager = new OrderAttachmentManager();
        var cleanPdfPath = directory.GetPath("clean-visual-source.pdf");
        var migratedPath = directory.GetPath("edited-workspace-4.pdf");
        CreateCleanPdfCopy(fixturePath, cleanPdfPath);

        using var sourceData = CommaPdfDataReader.Read(fixturePath);
        var card = RestoreRuntimeCard(sourceData);
        var garments = RestoreRuntimeGarments(sourceData);

        card.OrderNumber = "MIG-4-001";
        card.OrderName = "TEST NOWA KARTA PO MIGRACJI";
        card.Customer = "Klient testowy";
        card.ReceivedDate = "30.08.2026";
        card.DueDate = "15.09.2026";
        card.ProductionType = "HAFT + DRUK";
        card.Notes = "Dane zmodyfikowane po wczytaniu Workspace 3.0";
        card.ProductionEntries[0].LogoName = "NOWY WZÓR";
        card.ProductionEntries[0].Dimension = "12 x 8 cm";
        card.ProductionEntries[0].Colours.Clear();
        card.ProductionEntries[0].Colours.Add(
            new ProductionColourEntry(1) { Value = "niebieski 2935 C" });

        var movedGarment = garments[^1];
        garments.RemoveAt(garments.Count - 1);
        garments.Insert(0, movedGarment);
        movedGarment.StartNewPage = false;
        movedGarment.ShowFront = true;
        movedGarment.ShowLeft = false;
        movedGarment.ViewDescriptions.Front = "Nowy opis przodu";
        movedGarment.ViewDescriptions.Back = "Opis zachowany po Enterze\ndruga linia";
        movedGarment.RefreshDrawingSelection();

        attachmentManager.AddFile(fixturePath, card.Attachments);

        var expectedData = CreateSemanticData(card, garments);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            cleanPdfPath,
            migratedPath,
            card,
            garments,
            attachmentManager.ContentStore);

        using var migratedData = CommaPdfDataReader.Read(migratedPath);

        Assert.Equal(4, migratedData.FormatVersion);
        AssertSemanticDataEqual(expectedData, migratedData);
        var attachment = Assert.Single(migratedData.Attachments);
        Assert.Equal(Path.GetFileName(fixturePath), attachment.Name);
        Assert.Equal("application/pdf", attachment.MimeType);
        Assert.Equal(ExpectedSha256, attachment.Sha256);
        Assert.NotNull(migratedData.AttachmentContentStore);
        using (var content = migratedData.AttachmentContentStore.OpenRead(attachment.Id))
        {
            Assert.Equal(ExpectedSha256, GetSha256(content));
        }

        Assert.Equal(before, SnapshotFile(fixturePath));
        File.Delete(migratedPath);
        Assert.False(File.Exists(migratedPath));
    }

    [Fact]
    public void TestNowaKarta_PreviewPlanAndGeneratedPdfUseIdenticalPages()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("workspace-3-preview-plan.pdf");
        using var sourceData = CommaPdfDataReader.Read(GetFixturePath());
        var card = RestoreRuntimeCard(sourceData);
        var garments = RestoreRuntimeGarments(sourceData);
        var drawingPath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Branding",
            "PimpLogoExact.png");

        Assert.True(File.Exists(drawingPath));
        foreach (var drawing in garments.SelectMany(garment => garment.Drawings))
            drawing.FullPath = drawingPath;

        var previewPages = OrderPageLayoutEngine.BuildPages(garments);
        OrderPdfGenerator.Generate(outputPath, card, previewPages);

        using var pdf = PdfDocument.Open(outputPath);
        Assert.Equal(previewPages.Count, pdf.NumberOfPages);

        for (var index = 0; index < previewPages.Count; index++)
        {
            var pdfTextWithoutSpaces = NormalizePdfText(
                pdf.GetPage(index + 1).Text);
            Assert.Contains(
                previewPages[index].PageNumberText,
                pdfTextWithoutSpaces,
                StringComparison.Ordinal);

            foreach (var garment in previewPages[index].Garments)
            {
                Assert.Contains(
                    NormalizePdfText(garment.Name),
                    pdfTextWithoutSpaces,
                    StringComparison.Ordinal);
            }
        }

        File.Delete(outputPath);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void IncompleteWorkspace3Payload_ReportsReadableDataError()
    {
        using var directory = new TemporaryDirectory();
        var brokenPath = directory.GetPath("broken-workspace-3.pdf");
        File.WriteAllText(
            brokenPath,
            "%PDF-1.4\n%%EOF\n%COMMA-WORKSPACE-DATA-BEGIN\n%e30=\n",
            new UTF8Encoding(false));

        var exception = Assert.Throws<InvalidDataException>(
            () => CommaPdfDataReader.Read(brokenPath));

        Assert.Contains("niekompletne", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CorruptWorkspace3Json_ReportsReadableDataError()
    {
        using var directory = new TemporaryDirectory();
        var brokenPath = directory.GetPath("corrupt-workspace-3.pdf");
        var payload = Convert.ToBase64String("{not-json"u8.ToArray());
        File.WriteAllText(
            brokenPath,
            $"%PDF-1.4\n%%EOF\n{BeginMarker}\n%{payload}\n{EndMarker}\n",
            new UTF8Encoding(false));

        var exception = Assert.Throws<InvalidDataException>(
            () => CommaPdfDataReader.Read(brokenPath));

        Assert.Contains(
            "niepoprawny JSON",
            exception.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    private static ProductionCard RestoreRuntimeCard(CommaOrderData data)
    {
        var card = new ProductionCard();
        RestoreCardThroughProductionPath(card, data);
        return card;
    }

    private static List<OrderGarmentItem> RestoreRuntimeGarments(
        CommaOrderData data)
    {
        return data.Garments.Select(source =>
        {
            var garment = OrderTestData.CreateGarment(4, source.Name);
            garment.ProductCode = source.ProductCode;
            garment.Name = source.Name;
            garment.Colour = source.Colour;
            garment.Variant = source.Variant;
            garment.ShowFront = source.ShowFront;
            garment.ShowBack = source.ShowBack;
            garment.ShowRight = source.ShowRight;
            garment.ShowLeft = source.ShowLeft;
            garment.StartNewPage = source.StartNewPage;
            garment.ViewDescriptions.Front = source.ViewDescriptions.Front;
            garment.ViewDescriptions.Back = source.ViewDescriptions.Back;
            garment.ViewDescriptions.Right = source.ViewDescriptions.Right;
            garment.ViewDescriptions.Left = source.ViewDescriptions.Left;
            garment.RefreshDrawingSelection();
            return garment;
        }).ToList();
    }

    private static void AssertCurrentWorkspace4PagePlan(
        IReadOnlyList<OrderGarmentItem> garments)
    {
        var pages = OrderPageLayoutEngine.BuildPages(garments);

        Assert.Equal([2, 2, 3, 4, 1, 3, 4, 3, 1],
            pages.Select(page => page.DrawingCount));
        Assert.InRange(pages[0].DrawingCount, 1, 2);
        Assert.All(pages.Skip(1), page => Assert.InRange(page.DrawingCount, 1, 4));
        Assert.Equal(
            garments.SelectMany(garment => garment.SelectedDrawings),
            pages.SelectMany(page => page.Placements)
                .SelectMany(placement => placement.Drawings));

        Assert.Equal(
            [
                "0510 T-Time t-shirt",
                "0510 T-Time t-shirt",
                "0525 Poloshirt stretch polo",
                "Burnwood Cap czapka",
                "Bluza nierozpinana",
                "Larkford polo",
                "LA Cap czapka",
                "Koszula me\u0328ska kro\u0301tki re\u0328kaw",
                "Koszula me\u0328ska długi re\u0328kaw"
            ],
            pages.Select(page => Assert.Single(page.Garments).Name));

        Assert.DoesNotContain(
            pages[4].Garments,
            garment => garment.Name == "Larkford polo");
        Assert.Equal("Larkford polo", pages[5].Placements[0].Garment.Name);
    }

    private static CommaOrderData CreateSemanticData(
        ProductionCard card,
        IReadOnlyList<OrderGarmentItem> garments)
    {
        return new CommaOrderData
        {
            OrderNumber = card.OrderNumber,
            OrderName = card.OrderName,
            Customer = card.Customer,
            ReceivedDate = card.ReceivedDate,
            DueDate = card.DueDate,
            ProductionType = card.ProductionType,
            ProductCode = card.ProductCode,
            ProductName = card.ProductName,
            Colour = card.Colour,
            Size = card.Size,
            Quantity = card.Quantity,
            Notes = card.Notes,
            ShowFront = card.ShowFront,
            ShowBack = card.ShowBack,
            ShowLeft = card.ShowLeft,
            ShowRight = card.ShowRight,
            Garments = garments.Select(garment => new CommaOrderGarmentData
            {
                ProductCode = garment.ProductCode,
                ProductName = garment.Name,
                Name = garment.Name,
                Colour = garment.Colour,
                Variant = garment.Variant,
                ShowFront = garment.ShowFront,
                ShowBack = garment.ShowBack,
                ShowRight = garment.ShowRight,
                ShowLeft = garment.ShowLeft,
                StartNewPage = garment.StartNewPage,
                ViewDescriptions = new CommaOrderGarmentViewDescriptions
                {
                    Front = garment.ViewDescriptions.Front,
                    Back = garment.ViewDescriptions.Back,
                    Right = garment.ViewDescriptions.Right,
                    Left = garment.ViewDescriptions.Left
                }
            }).ToList(),
            ProductionEntries = card.ProductionEntries.Select(entry =>
                new CommaOrderProductionEntryData
                {
                    Number = entry.Number,
                    LogoName = entry.LogoName,
                    Dimension = entry.Dimension,
                    Colours = entry.Colours.Select(colour =>
                        new CommaOrderColourData
                        {
                            Number = colour.Number,
                            Value = colour.Value
                        }).ToList()
                }).ToList()
        };
    }

    private static void AssertSemanticDataEqual(
        CommaOrderData expected,
        CommaOrderData actual)
    {
        Assert.Equal(expected.OrderNumber, actual.OrderNumber);
        Assert.Equal(expected.OrderName, actual.OrderName);
        Assert.Equal(expected.Customer, actual.Customer);
        Assert.Equal(expected.ReceivedDate, actual.ReceivedDate);
        Assert.Equal(expected.DueDate, actual.DueDate);
        Assert.Equal(expected.ProductionType, actual.ProductionType);
        Assert.Equal(expected.ProductCode, actual.ProductCode);
        Assert.Equal(expected.ProductName, actual.ProductName);
        Assert.Equal(expected.Colour, actual.Colour);
        Assert.Equal(expected.Size, actual.Size);
        Assert.Equal(expected.Quantity, actual.Quantity);
        Assert.Equal(expected.Notes, actual.Notes);
        Assert.Equal(expected.ShowFront, actual.ShowFront);
        Assert.Equal(expected.ShowBack, actual.ShowBack);
        Assert.Equal(expected.ShowRight, actual.ShowRight);
        Assert.Equal(expected.ShowLeft, actual.ShowLeft);
        Assert.Equal(expected.Garments.Count, actual.Garments.Count);

        for (var index = 0; index < expected.Garments.Count; index++)
        {
            var expectedGarment = expected.Garments[index];
            var actualGarment = actual.Garments[index];
            Assert.Equal(expectedGarment.ProductCode, actualGarment.ProductCode);
            Assert.Equal(expectedGarment.ProductName, actualGarment.ProductName);
            Assert.Equal(expectedGarment.Name, actualGarment.Name);
            Assert.Equal(expectedGarment.Colour, actualGarment.Colour);
            Assert.Equal(expectedGarment.Variant, actualGarment.Variant);
            Assert.Equal(expectedGarment.ShowFront, actualGarment.ShowFront);
            Assert.Equal(expectedGarment.ShowBack, actualGarment.ShowBack);
            Assert.Equal(expectedGarment.ShowRight, actualGarment.ShowRight);
            Assert.Equal(expectedGarment.ShowLeft, actualGarment.ShowLeft);
            Assert.Equal(expectedGarment.StartNewPage, actualGarment.StartNewPage);
            Assert.Equal(expectedGarment.ViewDescriptions.Front,
                actualGarment.ViewDescriptions.Front);
            Assert.Equal(expectedGarment.ViewDescriptions.Back,
                actualGarment.ViewDescriptions.Back);
            Assert.Equal(expectedGarment.ViewDescriptions.Right,
                actualGarment.ViewDescriptions.Right);
            Assert.Equal(expectedGarment.ViewDescriptions.Left,
                actualGarment.ViewDescriptions.Left);
        }

        Assert.Equal(expected.ProductionEntries.Count, actual.ProductionEntries.Count);
        for (var index = 0; index < expected.ProductionEntries.Count; index++)
        {
            var expectedEntry = expected.ProductionEntries[index];
            var actualEntry = actual.ProductionEntries[index];
            Assert.Equal(expectedEntry.Number, actualEntry.Number);
            Assert.Equal(expectedEntry.LogoName, actualEntry.LogoName);
            Assert.Equal(expectedEntry.Dimension, actualEntry.Dimension);
            Assert.Equal(
                expectedEntry.Colours.Select(colour => (colour.Number, colour.Value)),
                actualEntry.Colours.Select(colour => (colour.Number, colour.Value)));
        }
    }

    private static string GetSha256(Stream stream)
    {
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }

    private static void CreateCleanPdfCopy(string sourcePath, string outputPath)
    {
        var bytes = File.ReadAllBytes(sourcePath);
        var eof = "%%EOF"u8;
        var eofIndex = bytes.AsSpan().IndexOf(eof);
        Assert.True(eofIndex >= 0);
        File.WriteAllBytes(
            outputPath,
            bytes.AsSpan(0, eofIndex + eof.Length).ToArray());
    }

    private static void RestoreCardThroughProductionPath(
        ProductionCard card,
        CommaOrderData data)
    {
        var method = typeof(MainViewModel).GetMethod(
            "RestoreCardFromPdf",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);
        method.Invoke(null, [card, data]);
    }

    private static FileSnapshot SnapshotFile(string path)
    {
        var information = new FileInfo(path);
        var sha256 = Convert.ToHexString(
                SHA256.HashData(File.ReadAllBytes(path)))
            .ToLowerInvariant();

        return new FileSnapshot(
            information.Length,
            information.LastWriteTimeUtc,
            sha256);
    }

    private static int CountOccurrences(string value, string searched)
    {
        var count = 0;
        var index = 0;

        while ((index = value.IndexOf(
                   searched,
                   index,
                   StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += searched.Length;
        }

        return count;
    }

    private static byte[] ReadMarkedPayload(
        string rawPdf,
        int beginIndex,
        int endIndex)
    {
        var block = rawPdf.Substring(
            beginIndex + BeginMarker.Length,
            endIndex - beginIndex - BeginMarker.Length);
        var base64 = string.Concat(
            block.Split(
                    ["\r\n", "\n", "\r"],
                    StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim().TrimStart('%')));
        return Convert.FromBase64String(base64);
    }

    private static string NormalizePdfText(string value)
    {
        return value.Normalize(NormalizationForm.FormC)
            .Replace(" ", "", StringComparison.Ordinal);
    }

    private static string GetFixturePath()
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "COMMA.App.Tests",
                "Fixtures",
                "Workspace3",
                "TestNowaKarta-v3.pdf"));
    }

    private sealed record ExpectedGarment(
        string Name,
        string Colour,
        string Variant,
        bool ShowFront,
        bool ShowBack,
        bool ShowRight,
        bool ShowLeft,
        bool StartNewPage);

    private sealed record FileSnapshot(
        long Length,
        DateTime LastWriteTimeUtc,
        string Sha256);
}
