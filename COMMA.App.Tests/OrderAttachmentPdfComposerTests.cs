using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using COMMA.App.ViewModels;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;

namespace COMMA.App.Tests;

public sealed class OrderAttachmentPdfComposerTests
{
    [Fact]
    public void TwoCardPagesPdfAndPngCreateFourVisiblePagesInOrder()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var cardPath = directory.GetPath("card.pdf");
        var pdfPath = directory.GetPath("attachment.pdf");
        var pngPath = directory.GetPath("image.png");
        var composedPath = directory.GetPath("composed.pdf");
        var finalPath = directory.GetPath("final.pdf");
        CreateTextPdf(cardPath, "CARD 1/2", "CARD 2/2");
        CreateTextPdf(pdfPath, "ATTACHMENT PDF");
        CreateWidePng(pngPath);
        var card = new ProductionCard();
        Assert.Empty(manager.AddFiles(
            [pdfPath, pngPath],
            card.Attachments));

        OrderAttachmentPdfComposer.Compose(
            cardPath,
            composedPath,
            card.Attachments,
            manager.ContentStore);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            composedPath,
            finalPath,
            card,
            [],
            manager.ContentStore);

        using var document = PdfPigDocument.Open(finalPath);
        Assert.Equal(4, document.NumberOfPages);
        Assert.Contains("CARD 1/2", document.GetPage(1).Text);
        Assert.Contains("CARD 2/2", document.GetPage(2).Text);
        Assert.Contains("ATTACHMENT PDF", document.GetPage(3).Text);
        Assert.NotEmpty(document.GetPage(4).GetImages());
        Assert.DoesNotContain("COMMA", document.GetPage(3).Text);
        Assert.DoesNotContain("COMMA", document.GetPage(4).Text);
        AssertValidFinalTrailer(finalPath);
    }

    [Fact]
    public void MultiPagePdfAndMoveOrderArePreserved()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var cardPath = directory.GetPath("card.pdf");
        var pdfPath = directory.GetPath("multi.pdf");
        var pngPath = directory.GetPath("first.png");
        var outputPath = directory.GetPath("output.pdf");
        CreateTextPdf(cardPath, "CARD");
        CreateTextPdf(
            pdfPath,
            "PDF PAGE 1",
            "PDF PAGE 2",
            "PDF PAGE 3");
        CreateWidePng(pngPath);
        var card = new ProductionCard();
        Assert.Empty(manager.AddFiles(
            [pdfPath, pngPath],
            card.Attachments));
        Assert.True(manager.Move(
            card.Attachments[1],
            -1,
            card.Attachments));

        OrderAttachmentPdfComposer.Compose(
            cardPath,
            outputPath,
            card.Attachments,
            manager.ContentStore);

        using var document = PdfPigDocument.Open(outputPath);
        Assert.Equal(5, document.NumberOfPages);
        Assert.NotEmpty(document.GetPage(2).GetImages());
        Assert.Contains("PDF PAGE 1", document.GetPage(3).Text);
        Assert.Contains("PDF PAGE 2", document.GetPage(4).Text);
        Assert.Contains("PDF PAGE 3", document.GetPage(5).Text);
    }

    [Fact]
    public void ImageUsesCenteredContainGeometryOnWhiteA4()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var cardPath = directory.GetPath("card.pdf");
        var pngPath = directory.GetPath("wide.png");
        var outputPath = directory.GetPath("output.pdf");
        CreateTextPdf(cardPath, "CARD");
        CreateWidePng(pngPath);
        var card = new ProductionCard();
        manager.AddFile(pngPath, card.Attachments);

        OrderAttachmentPdfComposer.Compose(
            cardPath,
            outputPath,
            card.Attachments,
            manager.ContentStore);

        using var document = PdfPigDocument.Open(outputPath);
        var page = document.GetPage(2);
        var image = Assert.Single(page.GetImages());
        Assert.Equal(2d, image.BoundingBox.Width / image.BoundingBox.Height, 2);
        Assert.Equal(
            page.Width / 2d,
            (image.BoundingBox.Left + image.BoundingBox.Right) / 2d,
            2);
        Assert.Equal(
            page.Height / 2d,
            (image.BoundingBox.Bottom + image.BoundingBox.Top) / 2d,
            2);
        Assert.True(image.BoundingBox.Left >=
                    OrderAttachmentPdfComposer.ImagePageMarginPoints);
        Assert.True(image.BoundingBox.Right <=
                    page.Width - OrderAttachmentPdfComposer.ImagePageMarginPoints);
    }

    [Fact]
    public void PackageKeepsOriginalBytesAndRegenerationDoesNotDuplicatePages()
    {
        using var directory = new TemporaryDirectory();
        using var firstManager = new OrderAttachmentManager();
        var cardPath = directory.GetPath("card.pdf");
        var attachmentPath = directory.GetPath("attachment.pdf");
        var firstComposed = directory.GetPath("first-composed.pdf");
        var firstFinal = directory.GetPath("first-final.pdf");
        var secondComposed = directory.GetPath("second-composed.pdf");
        var secondFinal = directory.GetPath("second-final.pdf");
        CreateTextPdf(cardPath, "CARD");
        CreateTextPdf(attachmentPath, "ATTACHMENT");
        var expectedBytes = File.ReadAllBytes(attachmentPath);
        var card = new ProductionCard();
        firstManager.AddFile(attachmentPath, card.Attachments);

        OrderAttachmentPdfComposer.Compose(
            cardPath,
            firstComposed,
            card.Attachments,
            firstManager.ContentStore);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            firstComposed,
            firstFinal,
            card,
            [],
            firstManager.ContentStore);

        using var loaded = CommaPdfDataReader.Read(firstFinal);
        using var secondManager = new OrderAttachmentManager();
        secondManager.ReplaceContentStore(
            loaded.DetachAttachmentContentStore());
        var secondCard = new ProductionCard();
        foreach (var attachment in loaded.Attachments)
        {
            secondCard.Attachments.Add(new OrderAttachmentMetadata
            {
                Id = attachment.Id,
                Name = attachment.Name,
                MimeType = attachment.MimeType,
                Extension = attachment.Extension,
                Order = attachment.Order,
                Length = attachment.Length,
                Sha256 = attachment.Sha256,
                BlobEntry = attachment.BlobEntry,
                PdfPageCount = attachment.PdfPageCount
            });
        }

        OrderAttachmentPdfComposer.Compose(
            cardPath,
            secondComposed,
            secondCard.Attachments,
            secondManager.ContentStore);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            secondComposed,
            secondFinal,
            secondCard,
            [],
            secondManager.ContentStore);

        using (var document = PdfPigDocument.Open(secondFinal))
            Assert.Equal(2, document.NumberOfPages);

        using var reread = CommaPdfDataReader.Read(secondFinal);
        var metadata = Assert.Single(reread.Attachments);
        using var content =
            reread.AttachmentContentStore!.OpenRead(metadata.Id);
        using var copy = new MemoryStream();
        content.CopyTo(copy);
        Assert.Equal(expectedBytes, copy.ToArray());
        Assert.Equal(
            Convert.ToHexString(SHA256.HashData(expectedBytes))
                .ToLowerInvariant(),
            metadata.Sha256);
        AssertSinglePackage(secondFinal);
        AssertValidFinalTrailer(secondFinal);
    }

    [Fact]
    public void DocumentWithoutAttachmentsKeepsCardPageCount()
    {
        using var directory = new TemporaryDirectory();
        using var store = new OrderAttachmentContentStore();
        var sourcePath = directory.GetPath("source.pdf");
        var outputPath = directory.GetPath("output.pdf");
        CreateTextPdf(sourcePath, "ONE", "TWO");

        OrderAttachmentPdfComposer.Compose(
            sourcePath,
            outputPath,
            [],
            store);

        using var document = PdfPigDocument.Open(outputPath);
        Assert.Equal(2, document.NumberOfPages);
        Assert.Contains("ONE", document.GetPage(1).Text);
        Assert.Contains("TWO", document.GetPage(2).Text);
        Assert.Equal(
            File.ReadAllBytes(sourcePath),
            File.ReadAllBytes(outputPath));
    }

    [Fact]
    public void PreviewRendererRendersEveryPdfPageAndA4ImagePage()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("preview.pdf");
        var pngPath = directory.GetPath("preview.png");
        CreateTextPdf(pdfPath, "FIRST", "SECOND");
        CreateWidePng(pngPath);

        using var firstStream = File.OpenRead(pdfPath);
        var first = OrderAttachmentPreviewRenderer.Render(
            firstStream,
            ".pdf",
            0);
        using var secondStream = File.OpenRead(pdfPath);
        var second = OrderAttachmentPreviewRenderer.Render(
            secondStream,
            ".pdf",
            1);
        using var imageStream = File.OpenRead(pngPath);
        var imagePage = OrderAttachmentPreviewRenderer.Render(
            imageStream,
            ".png",
            0);

        using var firstBitmap = SKBitmap.Decode(first.PngBytes);
        using var secondBitmap = SKBitmap.Decode(second.PngBytes);
        using var imageBitmap = SKBitmap.Decode(imagePage.PngBytes);
        Assert.NotNull(firstBitmap);
        Assert.NotNull(secondBitmap);
        Assert.NotNull(imageBitmap);
        Assert.True(firstBitmap.Width > 0);
        Assert.True(secondBitmap.Height > 0);
        Assert.Equal(992d, imagePage.Width);
        Assert.Equal(1403d, imagePage.Height);
    }

    [Fact]
    public void PreviewNavigationCountsCardThenEveryAttachmentPage()
    {
        using var directory = new TemporaryDirectory();
        using var viewModel = new MainViewModel
        {
            ProductionCard = new ProductionCard()
        };
        var pdfPath = directory.GetPath("pages.pdf");
        var pngPath = directory.GetPath("image.png");
        CreateTextPdf(pdfPath, "PDF 1", "PDF 2");
        CreateWidePng(pngPath);

        Assert.Empty(viewModel.AttachmentManager.AddFiles(
            [pdfPath, pngPath],
            viewModel.ProductionCard.Attachments));

        Assert.Equal(3, viewModel.PreviewPhysicalPageCount);
        Assert.Equal("1 / 3", viewModel.PreviewPageNumberText);
        viewModel.NextPreviewPageCommand.Execute(null);
        viewModel.NextPreviewPageCommand.Execute(null);
        Assert.Equal(2, viewModel.PreviewPageIndex);
        Assert.Equal("3 / 3", viewModel.PreviewPageNumberText);
        Assert.False(viewModel.CanGoToNextPreviewPage);
    }

    private static void CreateTextPdf(
        string path,
        params string[] pageTexts)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            foreach (var text in pageTexts)
            {
                document.Page(page =>
                {
                    page.Content().AlignCenter().AlignMiddle().Text(text);
                });
            }
        }).GeneratePdf(path);
    }

    private static void CreateWidePng(string path)
    {
        using var bitmap = new SKBitmap(
            400,
            200,
            isOpaque: true);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(
            SKEncodedImageFormat.Png,
            100);
        using var stream = File.Create(path);
        data.SaveTo(stream);
    }

    private static void AssertSinglePackage(string pdfPath)
    {
        using var document = PdfPigDocument.Open(pdfPath);
        Assert.True(document.Advanced.TryGetEmbeddedFiles(
            out var embeddedFiles));
        Assert.Single(
            embeddedFiles,
            file => file.Name ==
                    OrderPdfV4DataEmbedder.EmbeddedPackageFileName);
    }

    private static void AssertValidFinalTrailer(string pdfPath)
    {
        var bytes = File.ReadAllBytes(pdfPath);
        var text = Encoding.Latin1.GetString(bytes);
        Assert.Single(
            Regex.Matches(text, "%%EOF")
                .Cast<Match>());
        var match = Regex.Match(
            text,
            @"startxref\s+(\d+)\s+%%EOF(?:\r\n|\r|\n)?\z",
            RegexOptions.CultureInvariant);
        Assert.True(match.Success);
        var startXref = long.Parse(match.Groups[1].Value);
        Assert.InRange(startXref, 0, bytes.LongLength - 1);
    }
}
