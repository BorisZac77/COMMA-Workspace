using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using COMMA.App.ViewModels;
using PdfSharp.Pdf;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using UglyToad.PdfPig;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;

namespace COMMA.App.Tests;

public sealed class OrderAttachmentTests
{
    private const string PngBase64 =
        "iVBORw0KGgoAAAANSUhEUgAAAAIAAAACCAIAAAD91JpzAAAAAXNSR0IArs4c6QAAAERlWElmTU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAA6ABAAMAAAABAAEAAKACAAQAAAABAAAAAqADAAQAAAABAAAAAgAAAADtGLyqAAAAE0lEQVQIHWMUqbjDwMDABMRAAAAQQgFsVyfLgwAAAABJRU5ErkJggg==";

    private const string JpegBase64 =
        "/9j/4AAQSkZJRgABAQAASABIAAD/4QBMRXhpZgAATU0AKgAAAAgAAYdpAAQAAAABAAAAGgAAAAAAA6ABAAMAAAABAAEAAKACAAQAAAABAAAAAqADAAQAAAABAAAAAgAAAAD/7QA4UGhvdG9zaG9wIDMuMAA4QklNBAQAAAAAAAA4QklNBCUAAAAAABDUHYzZjwCyBOmACZjs+EJ+/8AAEQgAAgACAwEiAAIRAQMRAf/EAB8AAAEFAQEBAQEBAAAAAAAAAAABAgMEBQYHCAkKC//EALUQAAIBAwMCBAMFBQQEAAABfQECAwAEEQUSITFBBhNRYQcicRQygZGhCCNCscEVUtHwJDNicoIJChYXGBkaJSYnKCkqNDU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6g4SFhoeIiYqSk5SVlpeYmZqio6Slpqeoqaqys7S1tre4ubrCw8TFxsfIycrS09TV1tfY2drh4uPk5ebn6Onq8fLz9PX29/j5+v/EAB8BAAMBAQEBAQEBAQEAAAAAAAABAgMEBQYHCAkKC//EALURAAIBAgQEAwQHBQQEAAECdwABAgMRBAUhMQYSQVEHYXETIjKBCBRCkaGxwQkjM1LwFWJy0QoWJDThJfEXGBkaJicoKSo1Njc4OTpDREVGR0hJSlNUVVZXWFlaY2RlZmdoaWpzdHV2d3h5eoKDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uLj5OXm5+jp6vLz9PX29/j5+v/bAEMAAgICAgICAwICAwUDAwMFBgUFBQUGCAYGBgYGCAoICAgICAgKCgoKCgoKCgwMDAwMDA4ODg4ODw8PDw8PDw8PD//bAEMBAgICBAQEBwQEBxALCQsQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEP/dAAQAAf/aAAwDAQACEQMRAD8A8Xooor/Qw/hc/9k=";

    [Fact]
    public void AttachmentFolder_DoesNotChangePdfOutputStartOrDestinationFolder()
    {
        using var directory = new TemporaryDirectory();
        var cardFolder = directory.GetPath("cards-a");
        var attachmentFolder = directory.GetPath("attachments-b");
        Directory.CreateDirectory(cardFolder);
        Directory.CreateDirectory(attachmentFolder);
        var attachmentPath = Path.Combine(attachmentFolder, "image.png");
        File.WriteAllBytes(
            attachmentPath,
            Convert.FromBase64String(PngBase64));

        using var viewModel = new MainViewModel
        {
            PdfOutputPath = cardFolder,
            ProductionCard = new ProductionCard
            {
                OrderName = "ORDER"
            }
        };

        viewModel.AttachmentManager.AddFile(
            attachmentPath,
            viewModel.ProductionCard.Attachments);

        var startFolderMethod = typeof(MainViewModel).GetMethod(
            "GetEffectivePdfOutputPath",
            BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(startFolderMethod);
        var startFolder = Assert.IsType<string>(
            startFolderMethod.Invoke(viewModel, null));

        var savePlanMethod = typeof(MainViewModel).GetMethod(
            "CreatePdfSavePlan",
            BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(savePlanMethod);
        var savePlan = Assert.IsAssignableFrom<object>(
            savePlanMethod.Invoke(
                null,
                [cardFolder, "ORDER", null, false, false, null]));
        var outputDirectoryProperty = savePlan.GetType().GetProperty(
            "OutputDirectory");
        Assert.NotNull(outputDirectoryProperty);

        Assert.Equal(cardFolder, viewModel.PdfOutputPath);
        Assert.Equal(cardFolder, startFolder);
        Assert.Equal(
            cardFolder,
            Assert.IsType<string>(
                outputDirectoryProperty.GetValue(savePlan)));
        Assert.NotEqual(attachmentFolder, startFolder);
    }

    [Fact]
    public void Manager_AcceptsPdfPngJpgAndJpegFromMultiSelection()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var attachments = new ObservableCollection<OrderAttachmentMetadata>();
        var paths = CreateSupportedFiles(directory);

        var errors = manager.AddFiles(paths, attachments);

        Assert.Empty(errors);
        Assert.Equal(4, attachments.Count);
        Assert.Equal([0, 1, 2, 3], attachments.Select(item => item.Order));
        Assert.Equal(
            ["application/pdf", "image/png", "image/jpeg", "image/jpeg"],
            attachments.Select(item => item.MimeType));
        Assert.Equal(2, attachments[0].PdfPageCount);
        Assert.All(attachments, item => Assert.True(manager.ContentStore.Contains(item.Id)));
        Assert.All(attachments, item => Assert.Equal(64, item.Sha256.Length));
    }

    [Fact]
    public void Manager_RejectsUnsupportedFalseExtensionCorruptAndEncryptedFiles()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var attachments = new ObservableCollection<OrderAttachmentMetadata>();
        var unsupported = directory.GetPath("notes.txt");
        var falsePdf = directory.GetPath("false.pdf");
        var corruptPng = directory.GetPath("corrupt.png");
        var encryptedPdf = directory.GetPath("encrypted.pdf");
        File.WriteAllText(unsupported, "tekst");
        File.WriteAllBytes(falsePdf, Convert.FromBase64String(PngBase64));
        File.WriteAllBytes(corruptPng, [137, 80, 78, 71, 13, 10, 26, 10]);
        CreateEncryptedPdf(encryptedPdf);

        var errors = manager.AddFiles(
            [unsupported, falsePdf, corruptPng, encryptedPdf],
            attachments);

        Assert.Empty(attachments);
        Assert.Equal(4, errors.Count);
        Assert.Contains(errors, error => error.Contains("nieobsługiwany", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("nie jest zgodna", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("uszkodzony", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(
            errors,
            error => error.Contains("zaszyfrowany", StringComparison.OrdinalIgnoreCase) ||
                     error.Contains("hasłem", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Manager_EnforcesCountFileTotalPageAndImagePixelLimits()
    {
        using var directory = new TemporaryDirectory();
        var pngPath = directory.GetPath("small.png");
        File.WriteAllBytes(pngPath, Convert.FromBase64String(PngBase64));

        using (var manager = new OrderAttachmentManager())
        {
            var attachments = new ObservableCollection<OrderAttachmentMetadata>();
            for (var index = 0; index < OrderAttachmentLimits.MaximumAttachmentCount; index++)
                manager.AddFile(pngPath, attachments);

            var exception = Assert.Throws<InvalidDataException>(
                () => manager.AddFile(pngPath, attachments));
            Assert.Contains("25", exception.Message, StringComparison.Ordinal);
        }

        var tooLarge = directory.GetPath("large.png");
        using (var stream = new FileStream(tooLarge, FileMode.CreateNew))
            stream.SetLength(OrderAttachmentLimits.MaximumFileBytes + 1);
        using (var manager = new OrderAttachmentManager())
        {
            Assert.Throws<InvalidDataException>(
                () => manager.AddFile(
                    tooLarge,
                    new ObservableCollection<OrderAttachmentMetadata>()));
        }

        using (var manager = new OrderAttachmentManager())
        {
            var attachments = new ObservableCollection<OrderAttachmentMetadata>
            {
                new() { Length = OrderAttachmentLimits.MaximumTotalBytes }
            };
            Assert.Throws<InvalidDataException>(() => manager.AddFile(pngPath, attachments));
        }

        var onePagePdf = directory.GetPath("one-page.pdf");
        CreatePdf(onePagePdf, 1);
        using (var manager = new OrderAttachmentManager())
        {
            var attachments = new ObservableCollection<OrderAttachmentMetadata>
            {
                new() { PdfPageCount = OrderAttachmentLimits.MaximumTotalPdfPages }
            };
            var exception = Assert.Throws<InvalidDataException>(
                () => manager.AddFile(onePagePdf, attachments));
            Assert.Contains("500", exception.Message, StringComparison.Ordinal);
        }

        var tooManyPages = directory.GetPath("201-pages.pdf");
        CreatePdf(tooManyPages, 201);
        using (var manager = new OrderAttachmentManager())
        {
            Assert.Throws<InvalidDataException>(
                () => manager.AddFile(
                    tooManyPages,
                    new ObservableCollection<OrderAttachmentMetadata>()));
        }

        var hugePng = Convert.FromBase64String(PngBase64);
        WriteBigEndian(hugePng, 16, 10001);
        WriteBigEndian(hugePng, 20, 10000);
        WritePngHeaderCrc(hugePng);
        var hugePngPath = directory.GetPath("huge.png");
        File.WriteAllBytes(hugePngPath, hugePng);
        using (var manager = new OrderAttachmentManager())
        {
            var exception = Assert.Throws<InvalidDataException>(
                () => manager.AddFile(
                    hugePngPath,
                    new ObservableCollection<OrderAttachmentMetadata>()));
            Assert.Contains("100 megapikseli", exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void MoveRemoveAndClear_NormalizeOrderAndDeleteManagedContent()
    {
        using var directory = new TemporaryDirectory();
        using var manager = new OrderAttachmentManager();
        var attachments = new ObservableCollection<OrderAttachmentMetadata>();
        manager.AddFiles(CreateSupportedFiles(directory), attachments);
        var first = attachments[0];
        var last = attachments[^1];

        Assert.True(manager.Move(last, -1, attachments));
        Assert.Equal([0, 1, 2, 3], attachments.Select(item => item.Order));
        Assert.True(manager.ContentStore.Contains(first.Id));

        manager.Remove(first, attachments);
        Assert.False(manager.ContentStore.Contains(first.Id));
        Assert.Equal([0, 1, 2], attachments.Select(item => item.Order));

        var remainingIds = attachments.Select(item => item.Id).ToArray();
        manager.Clear(attachments);
        Assert.Empty(attachments);
        Assert.All(remainingIds, id => Assert.False(manager.ContentStore.Contains(id)));
    }

    [Fact]
    public void Move_ReordersObjectsNormalizesOrderAndStopsAtCollectionEdges()
    {
        using var manager = new OrderAttachmentManager();
        var alpha = new OrderAttachmentMetadata { Name = "alpha.pdf", Order = 0 };
        var bravo = new OrderAttachmentMetadata { Name = "bravo.png", Order = 1 };
        var charlie = new OrderAttachmentMetadata { Name = "charlie.jpg", Order = 2 };
        var attachments = new ObservableCollection<OrderAttachmentMetadata>
        {
            alpha,
            bravo,
            charlie
        };

        Assert.True(manager.Move(bravo, -1, attachments));
        Assert.Equal(
            ["bravo.png", "alpha.pdf", "charlie.jpg"],
            attachments.Select(item => item.Name));
        Assert.Equal([0, 1, 2], attachments.Select(item => item.Order));

        Assert.True(manager.Move(bravo, 1, attachments));
        Assert.Equal(
            ["alpha.pdf", "bravo.png", "charlie.jpg"],
            attachments.Select(item => item.Name));
        Assert.Equal([0, 1, 2], attachments.Select(item => item.Order));

        Assert.False(manager.Move(alpha, -1, attachments));
        Assert.False(manager.Move(charlie, 1, attachments));
        Assert.Equal(
            ["alpha.pdf", "bravo.png", "charlie.jpg"],
            attachments.Select(item => item.Name));
        Assert.Equal([0, 1, 2], attachments.Select(item => item.Order));
    }

    [Fact]
    public void AttachmentsWindow_RebindsVisibleOrderAndKeepsMovedItemSelected()
    {
        var viewsDirectory = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "COMMA.App", "Views"));
        var xaml = XDocument.Load(
            Path.Combine(viewsDirectory, "AttachmentsWindow.axaml"));
        var codeBehind = File.ReadAllText(
            Path.Combine(viewsDirectory, "AttachmentsWindow.axaml.cs"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var attachmentsList = Assert.Single(
            xaml.Descendants(),
            element =>
                element.Name.LocalName == "ListBox" &&
                (string?)element.Attribute(x + "Name") == "AttachmentsList");

        Assert.Equal(
            "{Binding Attachments}",
            (string?)attachmentsList.Attribute("ItemsSource"));
        Assert.Contains(
            "AttachmentsList.ItemsSource = card.Attachments.ToArray();",
            codeBehind,
            StringComparison.Ordinal);
        Assert.Contains(
            "AttachmentsList.SelectedItem = selected;",
            codeBehind,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "AttachmentsList.SelectedIndex = oldIndex + offset;",
            codeBehind,
            StringComparison.Ordinal);

        var moveIndex = codeBehind.IndexOf(
            "manager.Move(selected, offset, card.Attachments)",
            StringComparison.Ordinal);
        var refreshIndex = codeBehind.IndexOf(
            "RefreshAttachmentsList(selected);",
            StringComparison.Ordinal);
        var rebindIndex = codeBehind.IndexOf(
            "AttachmentsList.ItemsSource = card.Attachments.ToArray();",
            StringComparison.Ordinal);
        var selectionIndex = codeBehind.IndexOf(
            "AttachmentsList.SelectedItem = selected;",
            StringComparison.Ordinal);

        Assert.True(moveIndex >= 0 && moveIndex < refreshIndex);
        Assert.True(refreshIndex < rebindIndex);
        Assert.True(rebindIndex < selectionIndex);
    }

    [Fact]
    public void ClearCurrentOrder_RemovesMetadataAndManagedCopies()
    {
        using var directory = new TemporaryDirectory();
        using var viewModel = new MainViewModel
        {
            ProductionCard = new ProductionCard()
        };
        var pngPath = directory.GetPath("clear.png");
        File.WriteAllBytes(pngPath, Convert.FromBase64String(PngBase64));
        var attachment = viewModel.AttachmentManager.AddFile(
            pngPath,
            viewModel.ProductionCard.Attachments);
        Assert.Equal("ZAŁĄCZNIKI (1)", viewModel.AttachmentsButtonText);

        viewModel.ClearCurrentOrder();

        Assert.Empty(viewModel.ProductionCard.Attachments);
        Assert.False(viewModel.AttachmentManager.ContentStore.Contains(attachment.Id));
        Assert.Equal("ZAŁĄCZNIKI (0)", viewModel.AttachmentsButtonText);
    }

    [Fact]
    public void MainWindow_AttachmentsButtonStaysInExistingFooterRow()
    {
        var path = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..", "..", "..", "..",
                "COMMA.App", "Views", "MainWindow.axaml"));
        var document = XDocument.Load(path);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var button = Assert.Single(
            document.Descendants(),
            element =>
                element.Name.LocalName == "Button" &&
                (string?)element.Attribute(x + "Name") == "AttachmentsButton");

        Assert.Equal("150", (string?)button.Attribute("Width"));
        Assert.Equal("52", (string?)button.Attribute("Height"));
        Assert.Equal(
            "{Binding AttachmentsButtonText}",
            (string?)button.Attribute("Content"));
        Assert.DoesNotContain(
            button.Ancestors(),
            ancestor => (string?)ancestor.Attribute(x + "Name") == "OrderDataGrid");
    }

    [Fact]
    public void Package_RoundTripsOriginalBytesAfterSourcesAreDeletedReorderedAndRemoved()
    {
        using var directory = new TemporaryDirectory();
        var sourceCardPdf = directory.GetPath("card-source.pdf");
        var firstOutput = directory.GetPath("card-first.pdf");
        var secondOutput = directory.GetPath("card-second.pdf");
        var sourcePaths = CreateSupportedFiles(directory);
        var expectedBytes = sourcePaths.ToDictionary(
            path => Path.GetFileName(path)!,
            File.ReadAllBytes,
            StringComparer.Ordinal);

        var card = new ProductionCard { OrderName = "ATTACHMENTS ROUND TRIP" };
        var garment = OrderTestData.CreateGarment(2, "Attachment garment");
        var pages = OrderPageLayoutEngine.BuildPages([garment]);
        OrderPdfGenerator.Generate(sourceCardPdf, card, pages);
        using var firstManager = new OrderAttachmentManager();
        Assert.Empty(firstManager.AddFiles(sourcePaths, card.Attachments));
        var initialPagePlan = pages.Count;

        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourceCardPdf,
            firstOutput,
            card,
            [garment],
            firstManager.ContentStore);

        foreach (var sourcePath in sourcePaths)
            File.Delete(sourcePath);

        using var firstRead = CommaPdfDataReader.Read(firstOutput);
        Assert.Equal(4, firstRead.Attachments.Count);
        Assert.NotNull(firstRead.AttachmentContentStore);
        AssertAttachmentBytes(firstRead, expectedBytes);

        var secondCard = new ProductionCard { OrderName = firstRead.OrderName };
        foreach (var attachment in firstRead.Attachments.OrderBy(item => item.Order))
            secondCard.Attachments.Add(ToRuntimeMetadata(attachment));

        using var secondManager = new OrderAttachmentManager();
        secondManager.ReplaceContentStore(firstRead.DetachAttachmentContentStore());
        var moved = secondCard.Attachments[^1];
        Assert.True(secondManager.Move(moved, -3, secondCard.Attachments));
        var removed = secondCard.Attachments.Single(item => item.Extension == ".jpeg");
        secondManager.Remove(removed, secondCard.Attachments);

        OrderPdfV4DataEmbedder.AddEmbeddedData(
            firstOutput,
            secondOutput,
            secondCard,
            [garment],
            secondManager.ContentStore);

        using var secondRead = CommaPdfDataReader.Read(secondOutput);
        Assert.Equal(3, secondRead.Attachments.Count);
        Assert.Equal([0, 1, 2], secondRead.Attachments.Select(item => item.Order));
        Assert.DoesNotContain(secondRead.Attachments, item => item.Id == removed.Id);
        AssertAttachmentBytes(
            secondRead,
            expectedBytes.Where(pair => pair.Key != removed.Name)
                .ToDictionary(
                    pair => pair.Key,
                    pair => pair.Value,
                    StringComparer.Ordinal));

        using var visualPdf = PdfPigDocument.Open(secondOutput);
        Assert.Equal(initialPagePlan, visualPdf.NumberOfPages);
        Assert.Equal(initialPagePlan, OrderPageLayoutEngine.BuildPages([garment]).Count);

        using var package = OpenPackage(secondOutput);
        var names = package.Entries.Select(entry => entry.FullName).ToArray();
        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, names.Length);
        Assert.Contains(OrderPdfV4DataEmbedder.ManifestEntryName, names);

        var manifestJson = ReadManifest(package);
        Assert.DoesNotContain("Base64", manifestJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(directory.Path, manifestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("/Users/", manifestJson, StringComparison.Ordinal);
        Assert.DoesNotContain("C:\\", manifestJson, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("path-traversal")]
    [InlineData("duplicate")]
    [InlineData("missing")]
    [InlineData("hash-mismatch")]
    [InlineData("length-mismatch")]
    public void Reader_RejectsUnsafeDuplicateAndMissingBlobEntries(string mode)
    {
        using var directory = new TemporaryDirectory();
        var sourcePdf = directory.GetPath("source.pdf");
        var packagePath = directory.GetPath("malicious.package");
        var outputPdf = directory.GetPath("malicious.pdf");
        CreatePdf(sourcePdf, 1);
        CreateInvalidPackage(packagePath, mode);

        DocumentOperation.LoadFile(sourcePdf)
            .AddAttachment(new DocumentOperation.DocumentAttachment
            {
                Key = OrderPdfV4DataEmbedder.EmbeddedPackageKey,
                FilePath = packagePath,
                AttachmentName = OrderPdfV4DataEmbedder.EmbeddedPackageFileName,
                MimeType = "application/zip",
                Relationship = DocumentOperation.DocumentAttachmentRelationship.Data
            })
            .Save(outputPdf);

        Assert.Throws<InvalidDataException>(() => CommaPdfDataReader.Read(outputPdf));
    }

    private static IReadOnlyList<string> CreateSupportedFiles(TemporaryDirectory directory)
    {
        var pdf = directory.GetPath($"document-{Guid.NewGuid():N}.pdf");
        var png = directory.GetPath($"image-{Guid.NewGuid():N}.png");
        var jpg = directory.GetPath($"photo-{Guid.NewGuid():N}.jpg");
        var jpeg = directory.GetPath($"photo-{Guid.NewGuid():N}.jpeg");
        CreatePdf(pdf, 2);
        File.WriteAllBytes(png, Convert.FromBase64String(PngBase64));
        File.WriteAllBytes(jpg, Convert.FromBase64String(JpegBase64));
        File.WriteAllBytes(jpeg, Convert.FromBase64String(JpegBase64));
        return [pdf, png, jpg, jpeg];
    }

    private static void CreatePdf(string path, int pageCount)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        Document.Create(document =>
        {
            for (var index = 0; index < pageCount; index++)
            {
                var pageNumber = index + 1;
                document.Page(page => page.Content().Text($"Page {pageNumber}"));
            }
        }).GeneratePdf(path);
    }

    private static void CreateEncryptedPdf(string path)
    {
        using var document = new PdfSharp.Pdf.PdfDocument();
        document.AddPage();
        document.SecuritySettings.UserPassword = "secret";
        document.SecuritySettings.OwnerPassword = "owner";
        document.Save(path);
    }

    private static void WriteBigEndian(byte[] bytes, int offset, uint value)
    {
        bytes[offset] = (byte)(value >> 24);
        bytes[offset + 1] = (byte)(value >> 16);
        bytes[offset + 2] = (byte)(value >> 8);
        bytes[offset + 3] = (byte)value;
    }

    private static void WritePngHeaderCrc(byte[] bytes)
    {
        uint crc = 0xFFFFFFFF;
        for (var index = 12; index < 29; index++)
        {
            crc ^= bytes[index];
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        WriteBigEndian(bytes, 29, crc ^ 0xFFFFFFFF);
    }

    private static string GetSha256(byte[] bytes) =>
        Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static void AssertAttachmentBytes(
        CommaOrderData data,
        IReadOnlyDictionary<string, byte[]> expectedBytes)
    {
        Assert.NotNull(data.AttachmentContentStore);

        foreach (var attachment in data.Attachments)
        {
            var expected = expectedBytes[attachment.Name];
            using var stream = data.AttachmentContentStore.OpenRead(attachment.Id);
            using var content = new MemoryStream();
            stream.CopyTo(content);
            Assert.Equal(expected, content.ToArray());
            Assert.Equal(expected.LongLength, attachment.Length);
            Assert.Equal(GetSha256(expected), attachment.Sha256);
        }
    }

    private static OrderAttachmentMetadata ToRuntimeMetadata(
        CommaOrderAttachmentData attachment) =>
        new()
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
        };

    private static ZipArchive OpenPackage(string pdfPath)
    {
        using var document = PdfPigDocument.Open(pdfPath);
        Assert.True(document.Advanced.TryGetEmbeddedFiles(out var embeddedFiles));
        var package = Assert.Single(
            embeddedFiles,
            file => file.Name == OrderPdfV4DataEmbedder.EmbeddedPackageFileName);
        return new ZipArchive(
            new MemoryStream(package.Bytes.ToArray()),
            ZipArchiveMode.Read);
    }

    private static string ReadManifest(ZipArchive package)
    {
        var entry = package.GetEntry(OrderPdfV4DataEmbedder.ManifestEntryName);
        Assert.NotNull(entry);
        using var reader = new StreamReader(entry.Open(), new UTF8Encoding(false, true));
        return reader.ReadToEnd();
    }

    private static void CreateInvalidPackage(string path, string mode)
    {
        var id = Guid.NewGuid();
        var blobEntry = mode == "path-traversal"
            ? "../outside.pdf"
            : OrderAttachmentValidator.CreateBlobEntry(id, ".pdf");
        var content = "%PDF-1.4\n%%EOF\n"u8.ToArray();
        var manifest = new CommaV4Manifest
        {
            Format = OrderPdfV4DataEmbedder.FormatName,
            FormatVersion = 4,
            ApplicationVersion = "4.0.0",
            Attachments =
            [
                new CommaV4AttachmentMetadata
                {
                    Id = id,
                    Name = "bad.pdf",
                    MimeType = "application/pdf",
                    Extension = ".pdf",
                    Order = 0,
                    Length = mode == "length-mismatch"
                        ? content.Length + 1
                        : content.Length,
                    Sha256 = mode == "hash-mismatch"
                        ? new string('0', 64)
                        : GetSha256(content),
                    BlobEntry = blobEntry,
                    PdfPageCount = 1
                }
            ]
        };

        using var stream = new FileStream(path, FileMode.CreateNew);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Create);
        var manifestEntry = archive.CreateEntry(OrderPdfV4DataEmbedder.ManifestEntryName);
        using (var manifestStream = manifestEntry.Open())
            JsonSerializer.Serialize(manifestStream, manifest);

        if (mode != "missing")
        {
            var blob = archive.CreateEntry(blobEntry);
            using var blobStream = blob.Open();
            blobStream.Write(content);
        }

        if (mode == "duplicate")
        {
            var duplicate = archive.CreateEntry(blobEntry);
            using var duplicateStream = duplicate.Open();
            duplicateStream.Write(content);
        }
    }
}
