using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
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
        Assert.Empty(data.Attachments);

        var expectedGarments = new[]
        {
            new ExpectedGarment(
                "0510 T-Time t-shirt", "", "",
                true, true, true, true, false),
            new ExpectedGarment(
                "0525 Poloshirt stretch polo", "", "",
                false, true, true, true, false),
            new ExpectedGarment(
                "Burnwood Cap czapka", "", "",
                true, true, true, true, false),
            new ExpectedGarment(
                "Bluza nierozpinana", "", "",
                true, false, false, false, true),
            new ExpectedGarment(
                "Larkford polo", "", "",
                false, true, true, true, false),
            new ExpectedGarment(
                "LA Cap czapka", "", "",
                true, true, true, true, false),
            new ExpectedGarment(
                "Koszula me\u0328ska kro\u0301tki re\u0328kaw", "", "",
                true, true, true, false, false),
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
