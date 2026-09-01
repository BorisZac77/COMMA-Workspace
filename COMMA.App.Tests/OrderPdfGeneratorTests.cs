using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SkiaSharp;
using PdfPigDocument = UglyToad.PdfPig.PdfDocument;
using PdfSharpDocumentOpenMode = PdfSharp.Pdf.IO.PdfDocumentOpenMode;
using PdfSharpReader = PdfSharp.Pdf.IO.PdfReader;

namespace COMMA.App.Tests;

public sealed class OrderPdfGeneratorTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Pdf_DrawingImagesStayWithinSeventyMillimetresAndKeepAspectRatio(
        int drawingCount)
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath($"drawing-height-{drawingCount}.pdf");
        var imagePath = CreateDrawingFixture(directory, 400, 200);
        var garment = CreateGarment(drawingCount, $"Garment {drawingCount}");

        foreach (var drawing in garment.Drawings)
            drawing.FullPath = imagePath;

        var pages = OrderPageLayoutEngine.BuildPages([garment]);

        OrderPdfGenerator.Generate(
            outputPath,
            new ProductionCard { OrderName = "DRAWING HEIGHT" },
            pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var images = pages
            .SelectMany((pagePlan, index) =>
                GetDrawingImageBounds(
                    pdf.GetPage(index + 1),
                    pagePlan.Placements.Sum(placement => placement.DrawingCount)))
            .ToList();
        var maximumHeight = 70d / 25.4d * 72d;

        Assert.Equal(drawingCount, images.Count);

        foreach (var image in images)
        {
            Assert.InRange(image.Height, 1, maximumHeight + 0.5d);
            Assert.Equal(2d, image.Width / image.Height, precision: 2);
        }
    }

    [Fact]
    public void EmptyOrderNumber_DoesNotBlockV4PackageAndFirstPageShowsOneOfOne()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = directory.GetPath("one-page-source.pdf");
        var outputPath = directory.GetPath("one-page-v4.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "",
            OrderName = "TEST ORDER"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(1, "First")
        ]);

        OrderPdfGenerator.Generate(sourcePath, card, pages);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourcePath,
            outputPath,
            card,
            pages.SelectMany(page => page.Garments).ToList());

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(1, pdf.NumberOfPages);
        Assert.Contains("1/1", WithoutSpaces(pdf.GetPage(1).Text));
        Assert.Equal("", CommaPdfDataReader.Read(outputPath).OrderNumber);
    }

    [Fact]
    public void V4Package_PreservesTwoPagesTextAndNumberingAcrossTwoReaders()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = directory.GetPath("two-pages-source.pdf");
        var outputPath = directory.GetPath("two-pages-v4.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ORDERNUMBER2026",
            OrderName = "ORDERNAMECOLOR"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(2, "First"),
            CreateGarment(3, "Second")
        ]);

        OrderPdfGenerator.Generate(sourcePath, card, pages);

        string[] sourcePageTexts;
        using (var sourcePdf = PdfPigDocument.Open(sourcePath))
        {
            sourcePageTexts = Enumerable.Range(1, sourcePdf.NumberOfPages)
                .Select(pageNumber => sourcePdf.GetPage(pageNumber).Text)
                .ToArray();
        }

        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourcePath,
            outputPath,
            card,
            pages.SelectMany(page => page.Garments).ToList());

        using (var pdf = PdfPigDocument.Open(outputPath))
        {
            Assert.Equal(2, pdf.NumberOfPages);
            Assert.Equal(sourcePageTexts[0], pdf.GetPage(1).Text);
            Assert.Equal(sourcePageTexts[1], pdf.GetPage(2).Text);
            Assert.Contains("1/2", WithoutSpaces(pdf.GetPage(1).Text));
            Assert.Contains("2/2", WithoutSpaces(pdf.GetPage(2).Text));
            Assert.Contains("ORDERNUMBER2026", pdf.GetPage(1).Text);
            Assert.Contains("ORDERNUMBER2026", pdf.GetPage(2).Text);

            AssertTextPointSize(
                pdf.GetPage(1),
                "ORDERNUMBER2026",
                OrderHeaderTextLayout.FitNumber(
                    card.OrderNumber,
                    OrderHeaderTextLayout.PdfFirstPageNumberGeometry).FontSize);
            AssertTextPointSize(
                pdf.GetPage(1),
                "ORDERNAMECOLOR",
                OrderHeaderTextLayout.FitName(
                    card.OrderName,
                    OrderHeaderTextLayout.PdfFirstPageNameGeometry).FontSize);
            AssertTextPointSize(
                pdf.GetPage(2),
                "ORDERNUMBER2026",
                OrderHeaderTextLayout.FitNumber(
                    card.OrderNumber,
                    OrderHeaderTextLayout.PdfLaterPageNumberGeometry).FontSize);
            AssertTextPointSize(
                pdf.GetPage(2),
                "ORDERNAMECOLOR",
                OrderHeaderTextLayout.FitName(
                    card.OrderName,
                    OrderHeaderTextLayout.PdfLaterPageNameGeometry).FontSize);

            for (var pageNumber = 1; pageNumber <= 2; pageNumber++)
            {
                AssertWordIsOrderBlue(
                    pdf.GetPage(pageNumber),
                    "ORDERNUMBER2026");
                AssertWordIsOrderBlue(
                    pdf.GetPage(pageNumber),
                    "ORDERNAMECOLOR");
            }
        }

        using var pdfSharp = PdfSharpReader.Open(
            outputPath,
            PdfSharpDocumentOpenMode.Import);
        Assert.Equal(2, pdfSharp.PageCount);
    }

    [Fact]
    public void ThreePagePdf_ShowsSameOrderNumberAndCorrectNumberingOnEveryPage()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("three-pages.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-THREE-PAGES",
            OrderName = "THREE PAGE TEST"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(2, "First"),
            CreateGarment(3, "Second"),
            CreateGarment(3, "Third")
        ]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(3, pdf.NumberOfPages);
        Assert.Equal(3, pages.Count);

        for (var pageNumber = 1; pageNumber <= 3; pageNumber++)
        {
            var pageText = WithoutSpaces(pdf.GetPage(pageNumber).Text);
            Assert.Contains("ZL-THREE-PAGES", pageText);
            Assert.Contains($"{pageNumber}/3", pageText);
            Assert.Equal(pageNumber, pages[pageNumber - 1].PageNumber);
            Assert.Equal(3, pages[pageNumber - 1].TotalPages);
        }
    }

    [Fact]
    public void Czcionka4_FirstPageBoundaryNeverCreatesPhysicalOverflowPages()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = directory.GetPath("czcionka4-three-pages-source.pdf");
        var outputPath = directory.GetPath("czcionka4-three-pages.pdf");
        var first = CreateGarment(4, "0510 T-Time t-shirt");
        var second = CreateGarment(4, "Second");
        var third = CreateGarment(4, "Third");
        var firstPageController =
            new GarmentViewDescriptionInputController(
                "",
                DescriptionLayoutTarget.FirstPageTwoViews);
        var firstBoundary = firstPageController.Apply(
            string.Join(
                '\n',
                Enumerable.Repeat(
                    "ghjghjghjghjghjghjghj ŻÓŁĆ WIELKIE litery",
                100)),
            DescriptionLayoutTarget.FirstPageTwoViews).Text;
        var rejected = firstPageController.Apply(
            firstBoundary + "g",
            DescriptionLayoutTarget.FirstPageTwoViews);
        var pages = OrderPageLayoutEngine.BuildPages(
            [first, second, third]);
        var continuationGeometry = pages
            .SelectMany(page => page.Placements)
            .Where(placement => ReferenceEquals(placement.Garment, first))
            .SelectMany(placement => placement.Views)
            .Single(view => view.Drawing.IsRight)
            .Geometry;
        var continuationController = new GarmentViewDescriptionInputController(
            "",
            continuationGeometry);
        var continuationBoundary = continuationController.Apply(
            string.Join('\n', Enumerable.Repeat("RIGHT LEFT ŻÓŁĆ", 100)),
            continuationGeometry).Text;

        Assert.False(rejected.WasFullyAccepted);
        Assert.Equal(firstBoundary, rejected.Text);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                firstBoundary,
                DescriptionLayoutTarget.FirstPageTwoViews));

        first.ViewDescriptions.Front = firstBoundary;
        first.ViewDescriptions.Back = firstBoundary;
        first.ViewDescriptions.Right = continuationBoundary;
        first.ViewDescriptions.Left = continuationBoundary;
        second.ViewDescriptions.Front = "Opis FRONT drugiej pozycji";
        second.ViewDescriptions.Back = "Opis BACK drugiej pozycji";
        second.ViewDescriptions.Right = "Opis RIGHT drugiej pozycji";
        second.ViewDescriptions.Left = "Opis LEFT drugiej pozycji";
        third.ViewDescriptions.Front = "Opis FRONT trzeciej pozycji";
        third.ViewDescriptions.Back = "Opis BACK trzeciej pozycji";
        third.ViewDescriptions.Right = "Opis RIGHT trzeciej pozycji";
        third.ViewDescriptions.Left = "Opis LEFT trzeciej pozycji";

        var card = new ProductionCard
        {
            OrderNumber = "CZCIONKA4",
            OrderName = "CZCIONKA4"
        };

        Assert.Equal(4, pages.Count);

        OrderPdfGenerator.Generate(sourcePath, card, pages);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourcePath,
            outputPath,
            card,
            [first, second, third]);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(pages.Count, pdf.NumberOfPages);
        Assert.Equal(4, CommaPdfDataReader.Read(outputPath).FormatVersion);
        Assert.True(pdf.Advanced.TryGetEmbeddedFiles(out var embeddedFiles));
        Assert.Contains(embeddedFiles, file =>
            file.Name == OrderPdfV4DataEmbedder.EmbeddedPackageFileName);

        var firstPageText = WithoutSpaces(pdf.GetPage(1).Text);
        Assert.Contains("PRZÓD", firstPageText);
        Assert.Contains("TYŁ", firstPageText);
        Assert.DoesNotContain("PRAWYBOK", firstPageText);
        Assert.DoesNotContain("LEWYBOK", firstPageText);
        Assert.NotEmpty(GetTextLetters(pdf.GetPage(1), firstBoundary));
        var continuationText = WithoutSpaces(pdf.GetPage(2).Text);
        Assert.Contains("PRAWYBOK", continuationText);
        Assert.Contains("LEWYBOK", continuationText);
        Assert.NotEmpty(GetTextLetters(pdf.GetPage(2), continuationBoundary));

        for (var pageNumber = 1; pageNumber <= pages.Count; pageNumber++)
        {
            var pageText = WithoutSpaces(pdf.GetPage(pageNumber).Text);
            Assert.Contains($"{pageNumber}/4", pageText);
            Assert.Contains("NUMERZLECENIA", pageText);
            Assert.Contains("NAZWAZLECENIA", pageText);
            Assert.Contains("STRONA", pageText);
        }
    }

    [Fact]
    public void GeneratorRejectsContinuationSizedDescriptionBeforeCreatingFirstPageOverflow()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("rejected-first-page-overflow.pdf");
        var garment = CreateGarment(4, "0510 T-Time t-shirt");
        var controller =
            new GarmentViewDescriptionInputController(
                "",
                DescriptionLayoutTarget.LaterPageTwoViews);
        var continuationBoundary = controller.Apply(
            new string('g', 4000),
            DescriptionLayoutTarget.LaterPageTwoViews).Text;
        garment.ViewDescriptions.Front =
            continuationBoundary;
        var pages = OrderPageLayoutEngine.BuildPages([garment]);

        var exception = Assert.Throws<InvalidOperationException>(() =>
            OrderPdfGenerator.Generate(
                outputPath,
                new ProductionCard { OrderName = "CZCIONKA4" },
                pages));

        Assert.Contains("FRONT", exception.Message, StringComparison.Ordinal);
        Assert.Contains("0510 T-Time t-shirt", exception.Message, StringComparison.Ordinal);
        Assert.False(File.Exists(outputPath));
    }

    [Fact]
    public void EmptyOrderNumber_DoesNotBlockAnyGeneratedPage()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("empty-number-two-pages.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "",
            OrderName = "EMPTY NUMBER TEST"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(2, "First"),
            CreateGarment(3, "Second")
        ]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(2, pdf.NumberOfPages);
        Assert.Contains("1/2", WithoutSpaces(pdf.GetPage(1).Text));
        Assert.Contains("2/2", WithoutSpaces(pdf.GetPage(2).Text));
    }

    [Fact]
    public void LongContinuationValues_FitWithoutChangingPageCountOrPagePlan()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("long-header-values.pdf");
        const string orderNumber =
            "ZL-2026-DLUGI-NUMER-042";
        const string orderName =
            "BARDZO DŁUGA NAZWA ZLECENIA DO TESTU DWÓCH LINII";
        var card = new ProductionCard
        {
            OrderNumber = orderNumber,
            OrderName = orderName
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(2, "First"),
            CreateGarment(3, "Second")
        ]);

        var plannedNumbers = pages
            .Select(page => page.PageNumberText)
            .ToArray();
        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(2, pdf.NumberOfPages);
        Assert.Equal(["1/2", "2/2"], plannedNumbers);

        var continuationText = WithoutSpaces(pdf.GetPage(2).Text);
        Assert.Contains(WithoutSpaces(orderNumber), continuationText);
        Assert.Contains(WithoutSpaces(orderName), continuationText);
        Assert.Contains("2/2", continuationText);
    }

    [Fact]
    public void ShortHeaderValuesAreCenteredAndContainedOnFirstAndLaterPages()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("centered-header-values.pdf");
        const string orderNumber = "324234";
        const string orderName = "PLOPSA4OK";
        var card = new ProductionCard
        {
            OrderNumber = orderNumber,
            OrderName = orderName
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(2, "First"),
            CreateGarment(3, "Second")
        ]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(2, pdf.NumberOfPages);

        AssertHeaderValueIsCenteredAndContained(
            pdf.GetPage(1),
            orderNumber,
            PdfStyles.HeaderLogoWidth,
            PdfStyles.FirstPageHeaderOrderNumberWidth,
            PdfStyles.HeaderTopRowHeight);
        AssertHeaderValueIsCenteredAndContained(
            pdf.GetPage(1),
            orderName,
            PdfStyles.HeaderLogoWidth +
            PdfStyles.FirstPageHeaderOrderNumberWidth,
            PdfStyles.AvailableContentWidth -
            PdfStyles.HeaderLogoWidth -
            PdfStyles.FirstPageHeaderOrderNumberWidth -
            PdfStyles.FirstPageHeaderPageNumberWidth,
            PdfStyles.HeaderTopRowHeight);
        AssertHeaderValueIsCenteredAndContained(
            pdf.GetPage(2),
            orderNumber,
            PdfStyles.HeaderLogoWidth,
            PdfStyles.FirstPageHeaderOrderNumberWidth,
            PdfStyles.HeaderHeight);
        AssertHeaderValueIsCenteredAndContained(
            pdf.GetPage(2),
            orderName,
            PdfStyles.HeaderLogoWidth +
            PdfStyles.FirstPageHeaderOrderNumberWidth,
            PdfStyles.AvailableContentWidth -
            PdfStyles.HeaderLogoWidth -
            PdfStyles.FirstPageHeaderOrderNumberWidth -
            PdfStyles.FirstPageHeaderPageNumberWidth,
            PdfStyles.HeaderHeight);
    }

    [Fact]
    public void LongHeaderValuesKeepSafeInsetsStayBoldAndPageNumbersAreCentered()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("long-safe-header-values.pdf");
        const string orderNumber =
            "ZL-2026-00000000000000000000000000000042";
        const string orderName =
            "BARDZO DŁUGA NAZWA ZLECENIA WYMAGAJĄCA DWÓCH PEŁNYCH LINII";
        var card = new ProductionCard
        {
            OrderNumber = orderNumber,
            OrderName = orderName
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(4, "First"),
            CreateGarment(4, "Second"),
            CreateGarment(4, "Third"),
            CreateGarment(4, "Fourth"),
            CreateGarment(4, "Fifth")
        ]);

        Assert.Equal(6, pages.Count);
        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(6, pdf.NumberOfPages);

        foreach (var pageNumber in new[] { 1, 2 })
        {
            var page = pdf.GetPage(pageNumber);
            var headerHeight = pageNumber == 1
                ? PdfStyles.HeaderTopRowHeight
                : PdfStyles.HeaderHeight;

            AssertBlueHeaderFieldHasSafeInsetsAndBoldText(
                page,
                PdfStyles.HeaderLogoWidth,
                PdfStyles.FirstPageHeaderOrderNumberWidth,
                headerHeight,
                mustBeDynamicallyReduced: true);
            AssertBlueHeaderFieldHasSafeInsetsAndBoldText(
                page,
                PdfStyles.HeaderLogoWidth +
                PdfStyles.FirstPageHeaderOrderNumberWidth,
                PdfStyles.AvailableContentWidth -
                PdfStyles.HeaderLogoWidth -
                PdfStyles.FirstPageHeaderOrderNumberWidth -
                PdfStyles.FirstPageHeaderPageNumberWidth,
                headerHeight,
                mustBeDynamicallyReduced: pageNumber == 1);
            AssertPageNumberIsCenteredBelowLabel(
                page,
                $"{pageNumber}/6",
                headerHeight);
        }
    }

    [Fact]
    public void HeaderControlDocumentsContainCompleteShortAndLongValues()
    {
        var configuredOutput = Environment.GetEnvironmentVariable(
            "COMMA_HEADER_QA_OUTPUT");
        using var temporaryDirectory = string.IsNullOrWhiteSpace(configuredOutput)
            ? new TemporaryDirectory()
            : null;
        var outputDirectory = string.IsNullOrWhiteSpace(configuredOutput)
            ? temporaryDirectory!.Path
            : Path.GetFullPath(configuredOutput);
        Directory.CreateDirectory(outputDirectory);

        var cases = new[]
        {
            new
            {
                FileName = "header-short-values.pdf",
                Number = "324234",
                Name = "PLOPSA 4.0 OK",
                GarmentCount = 2
            },
            new
            {
                FileName = "header-long-values.pdf",
                Number = "ZL-2026-00000000000000000000000000000042",
                Name =
                    "BARDZO DŁUGA NAZWA ZLECENIA WYMAGAJĄCA DWÓCH PEŁNYCH LINII",
                GarmentCount = 5
            }
        }.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(configuredOutput))
        {
            cases = cases.Where(item =>
                item.FileName == "header-long-values.pdf");
        }

        foreach (var item in cases)
        {
            var outputPath = Path.Combine(
                outputDirectory,
                item.FileName);
            var card = new ProductionCard
            {
                OrderNumber = item.Number,
                OrderName = item.Name
            };
            var garments = Enumerable.Range(1, item.GarmentCount)
                .Select(index => CreateGarment(4, $"Garment {index}"))
                .ToList();
            var pages = OrderPageLayoutEngine.BuildPages(garments);

            OrderPdfGenerator.Generate(outputPath, card, pages);

            using var pdf = PdfPigDocument.Open(outputPath);
            var expectedPageCount = item.GarmentCount + 1;
            Assert.Equal(expectedPageCount, pdf.NumberOfPages);

            for (var pageNumber = 1;
                 pageNumber <= expectedPageCount;
                 pageNumber++)
            {
                var text = WithoutSpaces(pdf.GetPage(pageNumber).Text);
                Assert.Contains(WithoutSpaces(item.Number), text);
                Assert.Contains(WithoutSpaces(item.Name), text);
            }
        }
    }

    [Fact]
    public void PreviewContinuationHeader_UsesAcceptedGeometryAndSharedBlueStyle()
    {
        var previewPath = Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "COMMA.App",
                "Controls",
                "ProductionCardPreview.axaml"));
        var document = XDocument.Load(previewPath);
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        var continuationHeader = Assert.Single(
            document.Descendants(),
            element =>
                element.Name.LocalName == "Grid" &&
                (string?)element.Attribute(x + "Name") ==
                "ContinuationHeader");

        Assert.Equal(
            "155,108.3,*,70",
            (string?)continuationHeader.Attribute("ColumnDefinitions"));

        var textBlocks = continuationHeader
            .Descendants()
            .Where(element => element.Name.LocalName == "TextBlock")
            .ToList();
        Assert.Contains(textBlocks, element =>
            (string?)element.Attribute("Text") == "NUMER ZLECENIA");
        Assert.Contains(textBlocks, element =>
            (string?)element.Attribute("Text") == "NAZWA ZLECENIA");
        Assert.Contains(textBlocks, element =>
            (string?)element.Attribute("Text") == "STRONA");

        var numberValue = Assert.Single(textBlocks, element =>
            (string?)element.Attribute(x + "Name") ==
            "ContinuationOrderNumberValue");
        Assert.Equal(
            "orderNameValue",
            (string?)numberValue.Attribute("Classes"));
        Assert.Contains(
            "LaterNumberFontSize",
            (string?)numberValue.Attribute("FontSize"));
        Assert.Contains(
            "LaterNumberText",
            (string?)numberValue.Attribute("Text"));
        Assert.Equal("1", (string?)numberValue.Attribute("MaxLines"));
        Assert.Equal("Bold", (string?)numberValue.Attribute("FontWeight"));
        Assert.Null(numberValue.Attribute("TextTrimming"));
        Assert.Equal(
            "1",
            (string?)numberValue.Attribute("Grid.Row"));
        Assert.Equal(
            "8,*",
            (string?)numberValue.Parent?.Attribute("RowDefinitions"));

        var nameValue = Assert.Single(textBlocks, element =>
            (string?)element.Attribute(x + "Name") ==
            "ContinuationOrderNameValue");
        Assert.NotNull(nameValue.Attribute("Classes.orderNameValue"));
        Assert.Contains(
            "LaterNameFontSize",
            (string?)nameValue.Attribute("FontSize"));
        Assert.Contains(
            "LaterNameText",
            (string?)nameValue.Attribute("Text"));
        Assert.Equal("2", (string?)nameValue.Attribute("MaxLines"));
        Assert.Equal("Bold", (string?)nameValue.Attribute("FontWeight"));
        Assert.Null(nameValue.Attribute("TextTrimming"));
        Assert.Equal(
            "1",
            (string?)nameValue.Attribute("Grid.Row"));
        Assert.Equal(
            "8,*",
            (string?)nameValue.Parent?.Attribute("RowDefinitions"));

        var blueStyle = Assert.Single(
            document.Descendants(),
            element =>
                element.Name.LocalName == "Style" &&
                (string?)element.Attribute("Selector") ==
                "TextBlock.orderNameValue");
        var foreground = Assert.Single(
            blueStyle.Descendants(),
            element =>
                element.Name.LocalName == "Setter" &&
                (string?)element.Attribute("Property") == "Foreground");
        Assert.Equal("#0071BC", (string?)foreground.Attribute("Value"));

        foreach (var valueName in new[]
                 {
                     "FirstOrderNumberValue",
                     "FirstOrderNameValue",
                     "ContinuationOrderNumberValue",
                     "ContinuationOrderNameValue"
                 })
        {
            var value = Assert.Single(
                document.Descendants(),
                element =>
                    element.Name.LocalName == "TextBlock" &&
                    (string?)element.Attribute(x + "Name") == valueName);
            Assert.Equal(
                "Center",
                (string?)value.Attribute("HorizontalAlignment"));
            Assert.Equal(
                "Center",
                (string?)value.Attribute("VerticalAlignment"));
            Assert.Equal(
                "Center",
                (string?)value.Attribute("TextAlignment"));
            Assert.Equal("Bold", (string?)value.Attribute("FontWeight"));

            var border = value.Parent?.Parent;
            Assert.Equal("6.25,2", (string?)border?.Attribute("Padding"));
        }

        foreach (var valueName in new[]
                 {
                     "FirstPageNumberValue",
                     "ContinuationPageNumberValue"
                 })
        {
            var value = Assert.Single(
                document.Descendants(),
                element =>
                    element.Name.LocalName == "TextBlock" &&
                    (string?)element.Attribute(x + "Name") == valueName);
            Assert.Equal("Bold", (string?)value.Attribute("FontWeight"));
            Assert.Equal(
                "Center",
                (string?)value.Attribute("HorizontalAlignment"));
            Assert.Equal(
                "Center",
                (string?)value.Attribute("VerticalAlignment"));
            Assert.Equal(
                "1",
                (string?)value.Attribute("Grid.Row"));
            Assert.Equal(
                "8,*",
                (string?)value.Parent?.Attribute("RowDefinitions"));
        }
    }

    [Fact]
    public void V4PackagePdf_HasValidFinalEofAndStartXrefWithoutMarkerTransport()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = directory.GetPath("structure-source.pdf");
        var outputPath = directory.GetPath("structure-v4.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-STRUCTURE",
            OrderName = "STRUCTURE TEST"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(1, "First")
        ]);

        OrderPdfGenerator.Generate(sourcePath, card, pages);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourcePath,
            outputPath,
            card,
            pages.SelectMany(page => page.Garments).ToList());

        var bytes = File.ReadAllBytes(outputPath);
        var text = Encoding.Latin1.GetString(bytes);

        Assert.Equal(1, CountOccurrences(text, "%%EOF"));
        Assert.DoesNotContain(
            OrderPdfV4DataEmbedder.HiddenDataBeginMarker,
            text,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            OrderPdfV4DataEmbedder.HiddenDataEndMarker,
            text,
            StringComparison.Ordinal);

        var trailerMatch = Regex.Match(
            text,
            @"startxref\s+(\d+)\s+%%EOF(?<ending>\r\n|\r|\n)?\z",
            RegexOptions.CultureInvariant);
        Assert.True(trailerMatch.Success);
        Assert.True(
            trailerMatch.Groups["ending"].Value is "" or "\r" or "\n" or "\r\n");

        var xrefOffset = long.Parse(trailerMatch.Groups[1].Value);
        Assert.InRange(xrefOffset, 0, bytes.LongLength - 1);

        var xrefTail = text.Substring((int)xrefOffset);
        Assert.True(
            xrefTail.StartsWith("xref", StringComparison.Ordinal) ||
            Regex.IsMatch(
                xrefTail,
                @"^\d+\s+\d+\s+obj",
                RegexOptions.CultureInvariant));
    }

    [Fact]
    public void Pdf_TypicalColoursAndShortDrawingDescriptionUseTenPointText()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("ten-point-content.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-FONT-10",
            OrderName = "TEN POINT CONTENT"
        };
        var colourValues = new[]
        {
            "1050",
            "20202",
            "3303",
            "44004",
            "5505"
        };
        card.ProductionEntries[0].ColoursNotes =
            string.Join('\n', colourValues);
        var denseColourValues = Enumerable.Range(1, 33)
            .Select(index => $"DENSE-{index:00}")
            .ToArray();
        card.ProductionEntries[1].ColoursNotes =
            string.Join('\n', denseColourValues);
        var garment = CreateGarment(1, "Font test garment");
        const string description = "KRÓTKI OPIS POD RYSUNKIEM";
        garment.ViewDescriptions.Front = description;
        var pages = OrderPageLayoutEngine.BuildPages([garment]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var page = pdf.GetPage(1);

        for (var index = 0; index < colourValues.Length; index++)
        {
            var numberLetters = GetTextLetters(page, $"{index + 1}.");
            var valueLetters = GetTextLetters(page, colourValues[index]);

            Assert.All(
                numberLetters,
                letter => Assert.Equal(
                    PdfStyles.ColourEntryFontSize,
                    letter.PointSize,
                    precision: 1));
            Assert.All(
                numberLetters,
                letter => Assert.Contains(
                    "Bold",
                    letter.FontName,
                    StringComparison.OrdinalIgnoreCase));
            Assert.All(
                valueLetters,
                letter => Assert.Equal(
                    PdfStyles.ColourEntryFontSize,
                    letter.PointSize,
                    precision: 1));
            Assert.All(
                valueLetters,
                letter => Assert.DoesNotContain(
                    "Bold",
                    letter.FontName,
                    StringComparison.OrdinalIgnoreCase));
        }

        var contentTop =
            page.Height -
            PdfStyles.PageMargin -
            PdfStyles.OuterBorderWidth -
            PdfStyles.PagePadding;
        var coloursAreaTop =
            contentTop -
            PdfStyles.HeaderHeight -
            PdfStyles.SectionGap -
            PdfStyles.LoggingTitleHeight -
            PdfStyles.LoggingEntriesHeight -
            PdfStyles.ColoursTitleHeight;
        var coloursAreaBottom =
            coloursAreaTop - PdfStyles.ColoursAreaHeight;

        foreach (var denseValue in denseColourValues)
        {
            var valueLetters = GetTextLetters(page, denseValue);

            Assert.All(
                valueLetters,
                letter =>
                {
                    Assert.InRange(
                        letter.PointSize,
                        0.1,
                        PdfStyles.ColourEntryFontSize - 0.1);
                    Assert.InRange(
                        (letter.BoundingBox.Bottom + letter.BoundingBox.Top) / 2d,
                        coloursAreaBottom,
                        coloursAreaTop);
                });
        }

        AssertTextPointSize(
            page,
            description,
            PdfStyles.DrawingDescriptionFontSize);
    }

    [Fact]
    public void Pdf_RendersDescriptionsForMatchingGarmentsAndViewsWithAdaptiveFonts()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("view-descriptions.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-OPISY",
            OrderName = "TEST OPISÓW RZUTÓW"
        };
        var first = CreateGarment(2, "Pierwsza");
        var second = CreateGarment(2, "Druga");
        second.StartNewPage = true;
        const string firstFront = "FRONT-PIERWSZY-żółć";
        const string firstBack = "BACK-PIERWSZY-granat";
        const string secondFront = "FRONT-DRUGI-czerwień";
        var longDescription = new string('x', 150);

        first.ViewDescriptions.Front = firstFront;
        first.ViewDescriptions.Back = firstBack;
        second.ViewDescriptions.Front = secondFront;
        second.ViewDescriptions.Back = longDescription;

        var pages = OrderPageLayoutEngine.BuildPages([first, second]);
        var plannedPageNumbers = pages
            .Select(page => page.PageNumberText)
            .ToArray();

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var firstPage = pdf.GetPage(1);
        var secondPage = pdf.GetPage(2);
        var firstPageText = firstPage.Text;
        var secondPageText = secondPage.Text;

        Assert.Equal(2, pdf.NumberOfPages);
        Assert.Equal(["1/2", "2/2"], plannedPageNumbers);
        Assert.Contains(firstFront, firstPageText);
        Assert.Contains(firstBack, firstPageText);
        Assert.Contains(secondFront, secondPageText);
        Assert.Contains(longDescription, secondPageText);
        AssertTextPointSize(
            firstPage,
            firstFront,
            GarmentViewDescriptionLayout.PdfLargeFontSize);
        AssertTextPointSize(
            secondPage,
            longDescription,
            GarmentViewDescriptionLayout.PdfLargeFontSize);
    }

    [Fact]
    public void Pdf_AllowsDescriptionsBeyondThreeLinesWhenTheyFit()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("multi-line-description.pdf");
        var card = new ProductionCard
        {
            OrderName = "TEST OPISU WIELOWIERSZOWEGO"
        };
        var garment = CreateGarment(4, "Testowa odzież");
        var description = string.Join(
            '\n',
            Enumerable.Range(1, 5)
                .Select(index => $"WIERSZ {index}"));
        garment.ViewDescriptions.Front = description;
        var pages = OrderPageLayoutEngine.BuildPages(
            [CreateGarment(2, "Leading garment"), garment]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var lines = GetTextLetters(pdf.GetPage(2), description)
            .Select(letter => Math.Round(letter.StartBaseLine.Y, 1))
            .Distinct()
            .Count();

        Assert.Equal(5, lines);
        Assert.Equal(2, pdf.NumberOfPages);
        Assert.DoesNotContain("...", pdf.GetPage(2).Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Pdf_TwoAndFourDrawingPages_KeepDescriptionsDirectlyAfterImages()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath("compact-view-descriptions.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-COMPACT-DESCRIPTIONS",
            OrderName = "COMPACT DESCRIPTION TEST"
        };
        var twoDrawings = CreateGarment(2, "Two drawings");
        var fourDrawings = CreateGarment(4, "Four drawings");
        const string shortDescription = "KRÓTKI OPIS FRONTU";
        const string secondPageShortDescription = "KRÓTKI OPIS PRAWEGO BOKU";
        var multiLineDescription =
            GarmentViewDescriptionLayout.LimitTextChange(
                "",
                string.Join(
                    ' ',
                    Enumerable.Repeat(
                        "pełny polski opis żółć",
                        200)),
                4);

        twoDrawings.ViewDescriptions.Front = shortDescription;
        twoDrawings.ViewDescriptions.Back = "Opis tyłu na pierwszej stronie";
        fourDrawings.ViewDescriptions.Front = "Opis przodu na drugiej stronie";
        fourDrawings.ViewDescriptions.Back = multiLineDescription;
        fourDrawings.ViewDescriptions.Right = secondPageShortDescription;
        fourDrawings.ViewDescriptions.Left = "";

        var pages = OrderPageLayoutEngine.BuildPages(
            [twoDrawings, fourDrawings]);
        var pagePlan = pages
            .Select(page => page.PageNumberText)
            .ToArray();

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        Assert.Equal(2, pdf.NumberOfPages);
        Assert.Equal(["1/2", "2/2"], pagePlan);

        var firstPage = pdf.GetPage(1);
        var secondPage = pdf.GetPage(2);

        AssertDescriptionImmediatelyFollowsImage(
            firstPage,
            shortDescription);
        AssertDescriptionImmediatelyFollowsImage(
            secondPage,
            secondPageShortDescription);
        AssertDescriptionImmediatelyFollowsImage(
            secondPage,
            multiLineDescription);

        AssertDrawingImagesAreCenteredInCells(
            firstPage,
            2);
        AssertDrawingImagesAreCenteredInCells(
            secondPage,
            4);
        AssertDescriptionIsLeftAligned(
            firstPage,
            shortDescription,
            GetDrawingCellLeft(firstPage, isRightColumn: false));
        AssertDescriptionIsLeftAligned(
            secondPage,
            multiLineDescription,
            GetDrawingCellLeft(secondPage, isRightColumn: true));

        AssertTextPointSize(
            firstPage,
            shortDescription,
            GarmentViewDescriptionLayout.PdfLargeFontSize);
        Assert.Contains(
            GetTextLetters(secondPage, multiLineDescription)[0].PointSize,
            new double[]
            {
                GarmentViewDescriptionLayout.PdfLargeFontSize,
                GarmentViewDescriptionLayout.PdfMediumFontSize,
                GarmentViewDescriptionLayout.PdfMinimumFontSize
            });
        Assert.True(
            GetTextLetters(secondPage, multiLineDescription)
                .Select(letter => Math.Round(letter.StartBaseLine.Y, 1))
                .Distinct()
                .Count() > 3);
        AssertDrawingImagesHaveEqualSizes(secondPage);
        var twoViewGap =
            GetDescriptionGap(firstPage, shortDescription);
        var fourViewGap =
            GetDescriptionGap(secondPage, secondPageShortDescription);

        Assert.InRange(
            fourViewGap - twoViewGap,
            5,
            7);
        Assert.DoesNotContain("...", secondPage.Text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void Pdf_DrawingGeometryIsIdenticalForEmptyShortMultilineAndMaximumDescriptions(
        int drawingCount)
    {
        using var directory = new TemporaryDirectory();
        var maximumDescription =
            GarmentViewDescriptionLayout.LimitTextChange(
                "",
                string.Join(
                    ' ',
                    Enumerable.Repeat("maksymalny opis żółć", 500)),
                drawingCount);
        var multilineDescription = string.Join(
            '\n',
            Enumerable.Repeat(
                "wiersz opisu",
                drawingCount == 2 ? 8 : 5));
        var variants = new[]
        {
            (Name: "empty", Description: ""),
            (Name: "short", Description: "krótki opis"),
            (Name: "multiline", Description: multilineDescription),
            (Name: "maximum", Description: maximumDescription)
        };
        IReadOnlyList<UglyToad.PdfPig.Core.PdfRectangle>? reference = null;

        foreach (var variant in variants)
        {
            var outputPath = directory.GetPath(
                $"geometry-{drawingCount}-{variant.Name}.pdf");
            var card = new ProductionCard
            {
                OrderName = $"GEOMETRY {drawingCount} {variant.Name}"
            };
            var garment = CreateGarment(
                drawingCount,
                $"Garment {drawingCount}");

            garment.ViewDescriptions.Front = variant.Description;
            garment.ViewDescriptions.Back = variant.Description;
            garment.ViewDescriptions.Right = variant.Description;
            garment.ViewDescriptions.Left = variant.Description;

            var pages = drawingCount == 4
                ? OrderPageLayoutEngine.BuildPages(
                    [CreateGarment(2, "Leading garment"), garment])
                : OrderPageLayoutEngine.BuildPages([garment]);
            OrderPdfGenerator.Generate(outputPath, card, pages);

            using var pdf = PdfPigDocument.Open(outputPath);
            var geometry = GetDrawingImageBounds(
                pdf.GetPage(drawingCount == 4 ? 2 : 1),
                drawingCount);

            if (reference == null)
            {
                reference = geometry;
                continue;
            }

            AssertDrawingGeometryEqual(reference, geometry);
        }

        Assert.NotNull(reference);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void EditorAcceptedMixedTextIsCompleteAndStaysInsidePdfContent(
        int drawingCount)
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath(
            $"accepted-mixed-description-{drawingCount}.pdf");
        var textBox = new Avalonia.Controls.TextBox();
        using var controller =
            new GarmentViewDescriptionTextBoxController(
                textBox,
                () => drawingCount);
        var source = string.Join(
            '\n',
            Enumerable.Repeat(
                "ghjghjghjghjghjghjghj  WIELKIE ŻÓŁĆ    koniec",
                200));
        textBox.Text = source;
        var accepted = textBox.Text ?? "";

        Assert.True(controller.IsAtCapacity);
        Assert.Equal(accepted, controller.AcceptedText);
        Assert.True(controller.IsCurrentTextValidForCommit(drawingCount));

        textBox.Text = accepted + "g";

        Assert.Equal(accepted, textBox.Text);
        Assert.Equal(accepted, controller.AcceptedText);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                accepted,
                drawingCount));

        var card = new ProductionCard
        {
            OrderName = $"ACCEPTED DESCRIPTION {drawingCount}"
        };
        var garment = CreateGarment(
            drawingCount,
            $"Garment {drawingCount}");

        if (drawingCount == 2)
            garment.ViewDescriptions.Front = accepted;
        else
            garment.ViewDescriptions.Left = accepted;

        var pages = drawingCount == 4
            ? OrderPageLayoutEngine.BuildPages(
                [CreateGarment(2, "Leading garment"), garment])
            : OrderPageLayoutEngine.BuildPages([garment]);

        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var page = pdf.GetPage(drawingCount == 4 ? 2 : 1);
        var letters = GetTextLetters(page, accepted);
        var contentBottom =
            PdfStyles.PageMargin +
            PdfStyles.OuterBorderWidth +
            PdfStyles.PagePadding;
        var lowestLetterBottom =
            letters.Min(letter => letter.BoundingBox.Bottom);

        Assert.Equal(
            accepted.Count(character => !char.IsWhiteSpace(character)),
            letters.Count);
        Assert.True(
            lowestLetterBottom >=
            contentBottom);
        AssertDrawingImagesAreCenteredInCells(
            page,
            drawingCount);
    }

    [Fact]
    public void PlopsaSecondPageFrontDescriptionUsesThreeLinesInsideLeftCell()
    {
        using var directory = new TemporaryDirectory();
        var outputPath = directory.GetPath(
            "plopsa-page-2-three-line-description.pdf");
        var card = new ProductionCard
        {
            OrderName = "PLOPSA DESCRIPTION REGRESSION"
        };
        var upper = CreateGarment(3, "0380");
        var lower = CreateGarment(
            2,
            "0386 Pro Wear Care Sweatshirt Hoodie bluza");
        var drawingFixture = CreateSquareDrawingFixture(directory);

        foreach (var drawing in upper.Drawings.Concat(lower.Drawings))
            drawing.FullPath = drawingFixture;

        var pages = OrderPageLayoutEngine.BuildPages([upper, lower]);
        var pagePlan = pages[1];
        var lowerPlacement = pagePlan.Placements[1];
        var frontGeometry = lowerPlacement.Views[0].Geometry;
        var controller =
            new GarmentViewDescriptionInputController("", frontGeometry);
        var change = controller.Apply(
            new string('W', 4000),
            frontGeometry);
        var accepted = change.Text;

        Assert.False(change.WasFullyAccepted);
        Assert.True(controller.IsAtCapacity);
        Assert.Equal(
            3,
            GarmentViewDescriptionLayout.MeasurePdf(
                accepted,
                GarmentViewDescriptionLayout.GetPdfTextWidth(frontGeometry),
                GarmentViewDescriptionLayout.GetPdfTextHeight(frontGeometry)).LineCount);
        Assert.False(
            controller.Apply(
                accepted + "W",
                frontGeometry).WasFullyAccepted);

        lower.ViewDescriptions.Front = accepted;
        OrderPdfGenerator.Generate(outputPath, card, pages);

        using var pdf = PdfPigDocument.Open(outputPath);
        var renderedPage = pdf.GetPage(2);
        var letters = GetTextLetters(renderedPage, accepted);
        var lines = letters
            .GroupBy(letter => Math.Round(letter.StartBaseLine.Y, 1))
            .ToList();
        var contentLeft =
            (renderedPage.Width - PdfStyles.AvailableContentWidth) / 2d;
        var divider =
            contentLeft + PdfStyles.AvailableContentWidth / 2d;
        var expectedTextLeft =
            contentLeft +
            PdfStyles.DrawingCellPadding +
            PdfStyles.DrawingDescriptionHorizontalPadding;
        var expectedTextRight =
            divider -
            PdfStyles.DrawingCellPadding -
            PdfStyles.DrawingDescriptionHorizontalPadding;

        Assert.Equal(3, lines.Count);
        Assert.Equal(
            accepted.Count(character => !char.IsWhiteSpace(character)),
            letters.Count);
        Assert.All(
            letters,
            letter => Assert.InRange(
                letter.BoundingBox.Left,
                expectedTextLeft - 1,
                expectedTextRight));
        Assert.InRange(
            letters.Max(letter => letter.BoundingBox.Right),
            expectedTextRight - 12,
            expectedTextRight + 1);
        Assert.All(
            lines,
            line => Assert.InRange(
                line.Min(letter => letter.BoundingBox.Left),
                expectedTextLeft - 1,
                expectedTextLeft + 1.5));
    }

    [Fact]
    public void StandardPdfWriter_ResavePreservesV4PackageAndData()
    {
        using var directory = new TemporaryDirectory();
        var sourcePath = directory.GetPath("resave-source.pdf");
        var outputPath = directory.GetPath("resave-v4.pdf");
        var resavedPath = directory.GetPath("resaved.pdf");
        var card = new ProductionCard
        {
            OrderNumber = "ZL-RESAVE",
            OrderName = "RESAVE TEST"
        };
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            CreateGarment(1, "First")
        ]);

        OrderPdfGenerator.Generate(sourcePath, card, pages);
        OrderPdfV4DataEmbedder.AddEmbeddedData(
            sourcePath,
            outputPath,
            card,
            pages.SelectMany(page => page.Garments).ToList());

        using (var document = PdfSharpReader.Open(
                   outputPath,
                   PdfSharpDocumentOpenMode.Modify))
        {
            document.Save(resavedPath);
        }

        var restored = CommaPdfDataReader.Read(resavedPath);
        Assert.Equal(4, restored.FormatVersion);
        Assert.Equal("ZL-RESAVE", restored.OrderNumber);

        using var pdf = PdfPigDocument.Open(resavedPath);
        Assert.True(pdf.Advanced.TryGetEmbeddedFiles(out var embeddedFiles));
        Assert.Contains(embeddedFiles, file =>
            string.Equals(
                file.Name,
                OrderPdfV4DataEmbedder.EmbeddedPackageFileName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static OrderGarmentItem CreateGarment(
        int drawingCount,
        string name)
    {
        var garment = OrderTestData.CreateGarment(drawingCount, name);
        var imagePath = Path.Combine(
            AppContext.BaseDirectory,
            "Assets",
            "Branding",
            "PimpLogoExact.png");

        foreach (var drawing in garment.Drawings)
        {
            drawing.FullPath = imagePath;
        }

        return garment;
    }

    private static string CreateSquareDrawingFixture(
        TemporaryDirectory directory)
    {
        var path = directory.GetPath("square-drawing-fixture.png");
        using var bitmap = new SKBitmap(
            200,
            200,
            SKColorType.Rgba8888,
            SKAlphaType.Opaque);
        using var canvas = new SKCanvas(bitmap);
        using var paint = new SKPaint
        {
            Color = SKColors.Black,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 4,
            IsAntialias = true
        };

        canvas.Clear(SKColors.White);
        canvas.DrawRect(new SKRect(20, 20, 180, 180), paint);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());

        return path;
    }

    private static string CreateDrawingFixture(
        TemporaryDirectory directory,
        int width,
        int height)
    {
        var path = directory.GetPath($"drawing-fixture-{width}x{height}.png");
        using var bitmap = new SKBitmap(
            width,
            height,
            SKColorType.Rgba8888,
            SKAlphaType.Opaque);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        canvas.Flush();

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());

        return path;
    }

    private static string WithoutSpaces(string value) =>
        value.Replace(" ", "", StringComparison.Ordinal);

    private static void AssertWordIsOrderBlue(
        UglyToad.PdfPig.Content.Page page,
        string expectedText)
    {
        var letters = page.Letters.ToList();
        var pageLetterText = string.Concat(
            letters.Select(letter => letter.Value));
        var startIndex = pageLetterText.IndexOf(
            expectedText,
            StringComparison.Ordinal);

        Assert.True(startIndex >= 0);

        foreach (var letter in letters
                     .Skip(startIndex)
                     .Take(expectedText.Length))
        {
            var rgb = letter.Color.ToRGBValues();
            Assert.Equal(0d, rgb.r, precision: 3);
            Assert.Equal(113d / 255d, rgb.g, precision: 3);
            Assert.Equal(188d / 255d, rgb.b, precision: 3);
        }
    }

    private static void AssertTextPointSize(
        UglyToad.PdfPig.Content.Page page,
        string expectedText,
        double expectedPointSize)
    {
        var letters = page.Letters.ToList();
        var pageLetterText = string.Concat(
            letters.Select(letter => letter.Value));
        var startIndex = pageLetterText.IndexOf(
            expectedText,
            StringComparison.Ordinal);

        Assert.True(startIndex >= 0);

        foreach (var letter in letters
                     .Skip(startIndex)
                     .Take(expectedText.Length))
        {
            Assert.Equal(
                expectedPointSize,
                letter.PointSize,
                precision: 1);
        }
    }

    private static void AssertBlueHeaderFieldHasSafeInsetsAndBoldText(
        UglyToad.PdfPig.Content.Page page,
        double fieldOffset,
        double fieldWidth,
        double fieldHeight,
        bool mustBeDynamicallyReduced)
    {
        var contentLeft =
            (page.Width - PdfStyles.AvailableContentWidth) / 2d;
        var contentTop =
            page.Height -
            PdfStyles.PageMargin -
            PdfStyles.OuterBorderWidth -
            PdfStyles.PagePadding;
        var fieldLeft = contentLeft + fieldOffset;
        var fieldRight = fieldLeft + fieldWidth;
        var fieldBottom = contentTop - fieldHeight;
        var letters = page.Letters
            .Where(letter =>
            {
                var rgb = letter.Color.ToRGBValues();
                var centerX =
                    (letter.BoundingBox.Left + letter.BoundingBox.Right) / 2d;
                var centerY =
                    (letter.BoundingBox.Bottom + letter.BoundingBox.Top) / 2d;

                return Math.Abs(rgb.r) < 0.001 &&
                       Math.Abs(rgb.g - 113d / 255d) < 0.001 &&
                       Math.Abs(rgb.b - 188d / 255d) < 0.001 &&
                       centerX >= fieldLeft &&
                       centerX <= fieldRight &&
                       centerY >= fieldBottom &&
                       centerY <= contentTop;
            })
            .ToList();

        Assert.NotEmpty(letters);
        Assert.True(
            letters.Min(letter => letter.BoundingBox.Left) >=
            fieldLeft + PdfStyles.HeaderIdentityHorizontalPadding - 0.5);
        Assert.True(
            letters.Max(letter => letter.BoundingBox.Right) <=
            fieldRight - PdfStyles.HeaderIdentityHorizontalPadding + 0.5);
        Assert.All(
            letters,
            letter => Assert.Contains(
                "Bold",
                letter.FontName,
                StringComparison.OrdinalIgnoreCase));

        if (mustBeDynamicallyReduced)
        {
            Assert.All(
                letters,
                letter => Assert.True(
                    letter.PointSize < OrderHeaderTextLayout.BaseFontSize));
        }
    }

    private static void AssertPageNumberIsCenteredBelowLabel(
        UglyToad.PdfPig.Content.Page page,
        string expectedText,
        double fieldHeight)
    {
        var letters = page.Letters.ToList();
        var pageLetterText = string.Concat(
            letters.Select(letter => letter.Value));
        var startIndex = pageLetterText.IndexOf(
            expectedText,
            StringComparison.Ordinal);

        Assert.True(startIndex >= 0);

        var valueLetters = letters
            .Skip(startIndex)
            .Take(expectedText.Length)
            .ToList();
        var contentLeft =
            (page.Width - PdfStyles.AvailableContentWidth) / 2d;
        var contentTop =
            page.Height -
            PdfStyles.PageMargin -
            PdfStyles.OuterBorderWidth -
            PdfStyles.PagePadding;
        var fieldLeft =
            contentLeft +
            PdfStyles.AvailableContentWidth -
            PdfStyles.FirstPageHeaderPageNumberWidth;
        var fieldRight =
            fieldLeft + PdfStyles.FirstPageHeaderPageNumberWidth;
        var fieldBottom = contentTop - fieldHeight;
        var expectedCenterX = (fieldLeft + fieldRight) / 2d;
        var expectedCenterY =
            fieldBottom +
            (fieldHeight - PdfStyles.HeaderOrderLabelHeight) / 2d;
        var actualLeft = valueLetters.Min(letter => letter.BoundingBox.Left);
        var actualRight = valueLetters.Max(letter => letter.BoundingBox.Right);
        var actualBottom = valueLetters.Min(letter => letter.BoundingBox.Bottom);
        var actualTop = valueLetters.Max(letter => letter.BoundingBox.Top);

        Assert.InRange(
            (actualLeft + actualRight) / 2d,
            expectedCenterX - 1.5,
            expectedCenterX + 1.5);
        Assert.InRange(
            (actualBottom + actualTop) / 2d,
            expectedCenterY - 3,
            expectedCenterY + 3);
        Assert.All(
            valueLetters,
            letter => Assert.Contains(
                "Bold",
                letter.FontName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static void AssertHeaderValueIsCenteredAndContained(
        UglyToad.PdfPig.Content.Page page,
        string expectedText,
        double fieldOffset,
        double fieldWidth,
        double fieldHeight)
    {
        var letters = page.Letters.ToList();
        var pageLetterText = string.Concat(
            letters.Select(letter => letter.Value));
        var startIndex = pageLetterText.IndexOf(
            expectedText,
            StringComparison.Ordinal);

        Assert.True(startIndex >= 0);

        var valueLetters = letters
            .Skip(startIndex)
            .Take(expectedText.Length)
            .ToList();
        var contentLeft =
            (page.Width - PdfStyles.AvailableContentWidth) / 2d;
        var contentTop =
            page.Height -
            PdfStyles.PageMargin -
            PdfStyles.OuterBorderWidth -
            PdfStyles.PagePadding;
        var fieldLeft = contentLeft + fieldOffset;
        var fieldRight = fieldLeft + fieldWidth;
        var fieldBottom = contentTop - fieldHeight;
        var expectedCenterX = (fieldLeft + fieldRight) / 2d;
        var expectedCenterY =
            fieldBottom +
            (fieldHeight - PdfStyles.HeaderOrderLabelHeight) / 2d;
        var actualLeft = valueLetters.Min(letter => letter.BoundingBox.Left);
        var actualRight = valueLetters.Max(letter => letter.BoundingBox.Right);
        var actualBottom = valueLetters.Min(letter => letter.BoundingBox.Bottom);
        var actualTop = valueLetters.Max(letter => letter.BoundingBox.Top);

        Assert.InRange(actualLeft, fieldLeft, fieldRight);
        Assert.InRange(actualRight, fieldLeft, fieldRight);
        Assert.InRange(actualBottom, fieldBottom, contentTop);
        Assert.InRange(actualTop, fieldBottom, contentTop);
        Assert.InRange(
            (actualLeft + actualRight) / 2d,
            expectedCenterX - 1.5,
            expectedCenterX + 1.5);
        Assert.InRange(
            (actualBottom + actualTop) / 2d,
            expectedCenterY - 3,
            expectedCenterY + 3);
    }

    private static void AssertDescriptionImmediatelyFollowsImage(
        UglyToad.PdfPig.Content.Page page,
        string expectedText)
    {
        var gap = GetDescriptionGap(
            page,
            expectedText);

        Assert.InRange(
            gap,
            0,
            GarmentViewDescriptionLayout.PdfLargeFontSize *
            PdfStyles.DrawingDescriptionLineHeight +
            PdfStyles.MultiDrawingDescriptionTopGap);
    }

    private static double GetDescriptionGap(
        UglyToad.PdfPig.Content.Page page,
        string expectedText)
    {
        var descriptionLetters =
            GetTextLetters(page, expectedText);
        var descriptionTop = descriptionLetters.Max(letter =>
            letter.BoundingBox.Top);
        var descriptionCenterX =
            (descriptionLetters.Min(letter => letter.BoundingBox.Left) +
             descriptionLetters.Max(letter => letter.BoundingBox.Right)) /
            2d;
        var nearestImageAbove = page.GetImages()
            .Where(image =>
                image.BoundingBox.Left <= descriptionCenterX &&
                image.BoundingBox.Right >= descriptionCenterX &&
                image.BoundingBox.Bottom >= descriptionTop)
            .OrderBy(image => image.BoundingBox.Bottom - descriptionTop)
            .First();

        return nearestImageAbove.BoundingBox.Bottom -
               descriptionTop;
    }

    private static void AssertDrawingImagesHaveEqualSizes(
        UglyToad.PdfPig.Content.Page page)
    {
        var drawingAreaTop =
            page.Height -
            PdfStyles.HeaderHeight -
            PdfStyles.SectionGap -
            PdfStyles.PageSafetyReserve;
        var images = page.GetImages()
            .Where(image => image.BoundingBox.Top < drawingAreaTop)
            .ToList();

        Assert.Equal(4, images.Count);

        foreach (var image in images.Skip(1))
        {
            Assert.Equal(
                images[0].BoundingBox.Width,
                image.BoundingBox.Width,
                precision: 1);
            Assert.Equal(
                images[0].BoundingBox.Height,
                image.BoundingBox.Height,
                precision: 1);
        }
    }

    private static IReadOnlyList<UglyToad.PdfPig.Core.PdfRectangle> GetDrawingImageBounds(
        UglyToad.PdfPig.Content.Page page,
        int expectedDrawingCount)
    {
        var drawingAreaTop =
            page.Height -
            PdfStyles.HeaderHeight -
            PdfStyles.SectionGap -
            PdfStyles.PageSafetyReserve;
        var bounds = page.GetImages()
            .Where(image => image.BoundingBox.Top < drawingAreaTop)
            .OrderByDescending(image => image.BoundingBox.Top)
            .ThenBy(image => image.BoundingBox.Left)
            .Select(image => image.BoundingBox)
            .ToList();

        Assert.Equal(expectedDrawingCount, bounds.Count);

        return bounds;
    }

    private static void AssertDrawingGeometryEqual(
        IReadOnlyList<UglyToad.PdfPig.Core.PdfRectangle> expected,
        IReadOnlyList<UglyToad.PdfPig.Core.PdfRectangle> actual)
    {
        Assert.Equal(expected.Count, actual.Count);

        for (var index = 0; index < expected.Count; index++)
        {
            Assert.Equal(expected[index].Width, actual[index].Width, precision: 2);
            Assert.Equal(expected[index].Height, actual[index].Height, precision: 2);
            Assert.Equal(
                (expected[index].Left + expected[index].Right) / 2d,
                (actual[index].Left + actual[index].Right) / 2d,
                precision: 2);
            Assert.Equal(expected[index].Top, actual[index].Top, precision: 2);
        }
    }

    private static void AssertDrawingImagesAreCenteredInCells(
        UglyToad.PdfPig.Content.Page page,
        int expectedDrawingCount)
    {
        var drawingAreaTop =
            page.Height -
            PdfStyles.HeaderHeight -
            PdfStyles.SectionGap -
            PdfStyles.PageSafetyReserve;
        var images = page.GetImages()
            .Where(image => image.BoundingBox.Top < drawingAreaTop)
            .OrderByDescending(image => image.BoundingBox.Top)
            .ThenBy(image => image.BoundingBox.Left)
            .ToList();

        Assert.Equal(expectedDrawingCount, images.Count);

        var expectedCenters = expectedDrawingCount == 2
            ? new[]
            {
                GetDrawingCellCenter(page, isRightColumn: false),
                GetDrawingCellCenter(page, isRightColumn: true)
            }
            : new[]
            {
                GetDrawingCellCenter(page, isRightColumn: false),
                GetDrawingCellCenter(page, isRightColumn: true),
                GetDrawingCellCenter(page, isRightColumn: false),
                GetDrawingCellCenter(page, isRightColumn: true)
            };

        for (var index = 0; index < images.Count; index++)
        {
            var imageCenter =
                (images[index].BoundingBox.Left +
                 images[index].BoundingBox.Right) / 2d;

            Assert.InRange(
                Math.Abs(
                    expectedCenters[index] - imageCenter),
                0,
                0.75);
        }
    }

    private static void AssertDescriptionIsLeftAligned(
        UglyToad.PdfPig.Content.Page page,
        string expectedText,
        double cellLeft)
    {
        var lines = GetTextLetters(page, expectedText)
            .GroupBy(letter => Math.Round(letter.StartBaseLine.Y, 1))
            .Select(line => line.Min(letter => letter.BoundingBox.Left))
            .ToList();
        var expectedLeft =
            cellLeft +
            PdfStyles.DrawingCellPadding +
            PdfStyles.DrawingDescriptionHorizontalPadding;

        foreach (var lineLeft in lines)
        {
            Assert.InRange(
                Math.Abs(
                    expectedLeft - lineLeft),
                0,
                1.25);
        }
    }

    private static double GetDrawingCellLeft(
        UglyToad.PdfPig.Content.Page page,
        bool isRightColumn)
    {
        var contentLeft =
            (page.Width - PdfStyles.AvailableContentWidth) / 2d;

        return isRightColumn
            ? contentLeft + PdfStyles.AvailableContentWidth / 2d
            : contentLeft;
    }

    private static double GetDrawingCellCenter(
        UglyToad.PdfPig.Content.Page page,
        bool isRightColumn)
    {
        return GetDrawingCellLeft(page, isRightColumn) +
               PdfStyles.AvailableContentWidth / 4d;
    }

    private static IReadOnlyList<UglyToad.PdfPig.Content.Letter> GetTextLetters(
        UglyToad.PdfPig.Content.Page page,
        string expectedText)
    {
        expectedText = string.Concat(
            expectedText.Where(character =>
                !char.IsWhiteSpace(character)));
        var letters = page.Letters
            .Where(letter =>
                letter.Value.Any(character =>
                    !char.IsWhiteSpace(character)))
            .ToList();
        var pageLetterText = string.Concat(
            letters.Select(letter => letter.Value));
        var startIndex = pageLetterText.IndexOf(
            expectedText,
            StringComparison.Ordinal);

        Assert.True(startIndex >= 0);

        return letters
            .Skip(startIndex)
            .Take(expectedText.Length)
            .ToList();
    }

    private static int CountOccurrences(
        string value,
        string fragment)
    {
        var count = 0;

        for (var index = 0; ;)
        {
            index = value.IndexOf(fragment, index, StringComparison.Ordinal);

            if (index < 0)
                return count;

            count++;
            index += fragment.Length;
        }
    }
}
