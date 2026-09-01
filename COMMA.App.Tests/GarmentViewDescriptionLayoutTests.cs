using System.Xml.Linq;
using Avalonia.Controls;
using COMMA.App.Layout;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class GarmentViewDescriptionLayoutTests
{
    [Fact]
    public void PageTargetsUseActualFirstAndContinuationPageHeights()
    {
        Assert.Equal(
            552.89,
            GarmentViewDescriptionLayout.GetPageGarmentAreaHeight(true),
            precision: 3);
        Assert.Equal(
            724.405,
            GarmentViewDescriptionLayout.GetPageGarmentAreaHeight(false),
            precision: 3);
        Assert.Equal(
            531.89,
            GarmentViewDescriptionLayout.GetDrawingCellHeight(
                DescriptionLayoutTarget.FirstPageTwoViews),
            precision: 3);
        Assert.DoesNotContain(
            "FirstPageFourViews",
            Enum.GetNames<DescriptionLayoutTarget>());
        Assert.Equal(
            703.405,
            GarmentViewDescriptionLayout.GetDrawingCellHeight(
                DescriptionLayoutTarget.LaterPageTwoViews),
            precision: 3);
        Assert.Equal(
            351.703,
            GarmentViewDescriptionLayout.GetDrawingCellHeight(
                DescriptionLayoutTarget.LaterPageFourViews),
            precision: 3);
    }

    [Fact]
    public void LaterTwoViewControllerUsesItsAdditionalPhysicalHeight()
    {
        var source = string.Join(
            '\n',
            Enumerable.Repeat(
                "ghjghjghjghjghjghjghj ŻÓŁĆ WIELKIE litery",
                100));
        var laterController =
            new GarmentViewDescriptionInputController(
                "",
                DescriptionLayoutTarget.LaterPageTwoViews);
        var laterBoundary = laterController.Apply(
            source,
            DescriptionLayoutTarget.LaterPageTwoViews).Text;
        var firstController =
            new GarmentViewDescriptionInputController(
                "",
                DescriptionLayoutTarget.FirstPageTwoViews);
        var firstBoundary = firstController.Apply(
            source,
            DescriptionLayoutTarget.FirstPageTwoViews).Text;
        var rejected = firstController.Apply(
            firstBoundary + "g",
            DescriptionLayoutTarget.FirstPageTwoViews);

        Assert.True(firstBoundary.Length < laterBoundary.Length);
        Assert.False(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                laterBoundary,
                DescriptionLayoutTarget.FirstPageTwoViews));
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                firstBoundary,
                DescriptionLayoutTarget.FirstPageTwoViews));
        Assert.False(rejected.WasFullyAccepted);
        Assert.Equal(firstBoundary, rejected.Text);
    }

    [Fact]
    public void TargetGeometryUsesActualGarmentPlacementFromPagePlan()
    {
        var first = OrderTestData.CreateGarment(4, "First");
        var pages = OrderPageLayoutEngine.BuildPages([first]);
        var firstPage = pages[0];
        var laterPage = pages[1];
        var firstGeometry =
            GarmentViewDescriptionLayout.GetTargetGeometry(
                firstPage,
                firstPage.Placements[0],
                firstPage.Placements[0].Drawings[0]);
        var continuationGeometry =
            GarmentViewDescriptionLayout.GetTargetGeometry(
                laterPage,
                laterPage.Placements[0],
                laterPage.Placements[0].Drawings[0]);

        Assert.Equal(
            DescriptionLayoutTarget.FirstPageTwoViews,
            firstGeometry.Target);
        Assert.Equal(
            DescriptionLayoutTarget.LaterPageTwoViews,
            continuationGeometry.Target);
        Assert.Equal(
            531.89,
            firstGeometry.PdfDrawingCellHeight,
            precision: 3);
        Assert.True(
            firstGeometry.PdfDrawingCellHeight <
            continuationGeometry.PdfDrawingCellHeight);
    }

    [Fact]
    public void PlopsaSecondPageFrontDescriptionUsesThreeFullLeftCellLines()
    {
        var upper = OrderTestData.CreateGarment(3, "0380");
        var lower = OrderTestData.CreateGarment(
            2,
            "0386 Pro Wear Care Sweatshirt Hoodie bluza");
        var page = OrderPageLayoutEngine.BuildPages([upper, lower])[1];
        var upperPlacement = page.Placements[0];
        var lowerPlacement = page.Placements[1];
        var frontGeometry = lowerPlacement.Views[0].Geometry;
        var backGeometry = lowerPlacement.Views[1].Geometry;

        Assert.Equal(1, upperPlacement.DrawingCount);
        Assert.Equal(2, lowerPlacement.DrawingCount);
        Assert.Equal(
            PdfStyles.AvailableContentWidth / 2d,
            frontGeometry.PdfDrawingCellWidth,
            precision: 3);
        Assert.Equal(
            frontGeometry.PdfDrawingCellWidth,
            backGeometry.PdfDrawingCellWidth,
            precision: 3);
        Assert.Equal(
            frontGeometry.PdfDrawingCellWidth -
            PdfStyles.DrawingCellPadding * 2d -
            PdfStyles.DrawingDescriptionHorizontalPadding * 2d,
            GarmentViewDescriptionLayout.GetPdfTextWidth(frontGeometry),
            precision: 3);

        var typedController =
            new GarmentViewDescriptionInputController("", frontGeometry);
        var typed = FillByTypingUntilRejected(
            typedController,
            frontGeometry,
            "Długi opis PLOPSA pod rzutem PRZÓD wykorzystuje całą szerokość lewej komórki. ");

        AssertThreeLineBoundary(typed, frontGeometry);
        Assert.False(
            typedController.Apply(
                typed + "X",
                frontGeometry).WasFullyAccepted);

        var pasteController =
            new GarmentViewDescriptionInputController("", frontGeometry);
        var pasted = pasteController.Apply(
            string.Concat(Enumerable.Repeat(
                "Wklejony opis ze spacjami dla przedniego rzutu. ",
                100)),
            frontGeometry);

        Assert.False(pasted.WasFullyAccepted);
        AssertThreeLineBoundary(pasted.Text, frontGeometry);

        var middle = pasted.Text.Length / 2;
        var edited = pasteController.Apply(
            pasted.Text.Remove(middle, 1).Insert(middle, "i"),
            frontGeometry);

        Assert.True(edited.WasFullyAccepted);
        AssertThreeLineBoundary(edited.Text, frontGeometry);

        var unbrokenController =
            new GarmentViewDescriptionInputController("", frontGeometry);
        var unbroken = unbrokenController.Apply(
            new string('W', 4000),
            frontGeometry);

        Assert.False(unbroken.WasFullyAccepted);
        AssertThreeLineBoundary(unbroken.Text, frontGeometry);

        var manualController =
            new GarmentViewDescriptionInputController("", frontGeometry);
        const string threeManualLines =
            "pierwsza linia\ndruga linia\ntrzecia linia";
        var manual = manualController.Apply(
            threeManualLines,
            frontGeometry);

        Assert.True(manual.WasFullyAccepted);
        Assert.Equal(threeManualLines, manual.Text);
        AssertThreeLineBoundary(manual.Text, frontGeometry);
        Assert.False(
            manualController.Apply(
                threeManualLines + "\nczwarta linia",
                frontGeometry).WasFullyAccepted);
    }

    [Fact]
    public void TwoViewDescriptionCapacityKeepsLegacyImageReservationAtSeventyMillimetreRendering()
    {
        var geometry =
            GarmentViewDescriptionLayout.GetReferenceGeometry(
                DescriptionLayoutTarget.LaterPageTwoViews);
        var renderedImageHeight =
            GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(geometry);
        var legacyReservedImageHeight =
            PdfStyles.GetDrawingImageHeight(
                PdfStyles.GetDrawingRowHeight(1)) *
            0.75;
        var expectedTextHeight =
            geometry.PdfDrawingCellHeight -
            PdfStyles.DrawingTitleHeight -
            7d -
            PdfStyles.DrawingCellPadding * 2d -
            legacyReservedImageHeight -
            PdfStyles.DrawingDescriptionTopGap;
        var textHeightUsingRenderedImage =
            geometry.PdfDrawingCellHeight -
            PdfStyles.DrawingTitleHeight -
            7d -
            PdfStyles.DrawingCellPadding * 2d -
            renderedImageHeight -
            PdfStyles.DrawingDescriptionTopGap;

        Assert.Equal(
            70d / 25.4d * 72d,
            renderedImageHeight,
            precision: 3);
        Assert.Equal(
            expectedTextHeight,
            GarmentViewDescriptionLayout.GetPdfTextHeight(geometry),
            precision: 3);
        Assert.True(expectedTextHeight < textHeightUsingRenderedImage);
    }

    [Fact]
    public void PlopsaThreeViewPageAcceptsManualLinesUntilPhysicalBottom()
    {
        var first = OrderTestData.CreateGarment(2, "Strona 1");
        var second = OrderTestData.CreateGarment(
            2,
            "Strona 2",
            startNewPage: true);
        var garment = OrderTestData.CreateGarment(
            3,
            "0510 trzy rzuty",
            startNewPage: true);
        var page = OrderPageLayoutEngine.BuildPages([first, second, garment])[2];
        var placement = Assert.Single(page.Placements);
        var upperGeometry = placement.Views[0].Geometry;
        var controller =
            new GarmentViewDescriptionInputController("", upperGeometry);
        var multilineSource = string.Join(
            '\n',
            Enumerable.Range(1, 100)
                .Select(index => $"linia {index}"));
        var pasted = controller.Apply(
            multilineSource,
            upperGeometry);
        var measurement = GarmentViewDescriptionLayout.MeasurePdf(
            pasted.Text,
            GarmentViewDescriptionLayout.GetPdfTextWidth(upperGeometry),
            GarmentViewDescriptionLayout.GetPdfTextHeight(upperGeometry));

        Assert.Equal(3, placement.DrawingCount);
        Assert.False(pasted.WasFullyAccepted);
        Assert.True(measurement.Fits);
        Assert.True(measurement.LineCount > 3);
        Assert.Contains('\n', pasted.Text);
        Assert.True(controller.IsAtCapacity);
        Assert.False(
            controller.Apply(
                pasted.Text + "\njeszcze jedna linia",
                upperGeometry).WasFullyAccepted);

        var severalEnters =
            "pierwsza\n\n\ndruga\ntrzecia\nczwarta";
        var enterController =
            new GarmentViewDescriptionInputController("", upperGeometry);
        var entered = enterController.Apply(
            severalEnters,
            upperGeometry);

        Assert.True(entered.WasFullyAccepted);
        Assert.Equal(severalEnters, entered.Text);
        Assert.Equal(
            6,
            GarmentViewDescriptionLayout.MeasurePdf(
                entered.Text,
                GarmentViewDescriptionLayout.GetPdfTextWidth(upperGeometry),
                GarmentViewDescriptionLayout.GetPdfTextHeight(upperGeometry)).LineCount);

        const string trailingEnters = "pierwsza\n\n\n";
        garment.ViewDescriptions.Front = trailingEnters;

        Assert.Equal(
            trailingEnters,
            GarmentViewDescriptionLayout.GetDescription(
                garment,
                placement.Drawings[0]));
        Assert.Equal(
            4,
            GarmentViewDescriptionLayout.MeasurePdf(
                trailingEnters,
                GarmentViewDescriptionLayout.GetPdfTextWidth(upperGeometry),
                GarmentViewDescriptionLayout.GetPdfTextHeight(upperGeometry)).LineCount);

        var middle = pasted.Text.Length / 2;
        var edited = controller.Apply(
            pasted.Text.Remove(middle, 1).Insert(middle, "i"),
            upperGeometry);

        Assert.True(edited.WasFullyAccepted);
        Assert.Equal(pasted.Text.Length, edited.Text.Length);
    }

    [Fact]
    public void FullWidthFieldCalculatesLineCapacityFromRemainingHeight()
    {
        var first = OrderTestData.CreateGarment(2, "Strona 1");
        var garment = OrderTestData.CreateGarment(
            1,
            "Pełna szerokość",
            startNewPage: true);
        var page = OrderPageLayoutEngine.BuildPages([first, garment])[1];
        var geometry = Assert.Single(page.Placements).Views[0].Geometry;
        var controller =
            new GarmentViewDescriptionInputController("", geometry);
        var source = string.Join(
            '\n',
            Enumerable.Repeat("x", 1000));
        var accepted = controller.Apply(source, geometry);
        var measurement = GarmentViewDescriptionLayout.MeasurePdf(
            accepted.Text,
            GarmentViewDescriptionLayout.GetPdfTextWidth(geometry),
            GarmentViewDescriptionLayout.GetPdfTextHeight(geometry));
        var previewCapacity = (int)Math.Floor(
            (GarmentViewDescriptionLayout.GetPreviewTextHeight(geometry) - 0.25) /
            (GarmentViewDescriptionLayout.PreviewMinimumFontSize *
             GarmentViewDescriptionLayout.PreviewLineHeight));
        var pdfCapacity = (int)Math.Floor(
            (GarmentViewDescriptionLayout.GetPdfTextHeight(geometry) - 0.25) /
            (GarmentViewDescriptionLayout.PdfMinimumFontSize *
             PdfStyles.DrawingDescriptionLineHeight));

        Assert.Equal(PdfStyles.AvailableContentWidth, geometry.PdfDrawingCellWidth);
        Assert.False(accepted.WasFullyAccepted);
        Assert.True(measurement.LineCount > 3);
        Assert.Equal(Math.Min(previewCapacity, pdfCapacity), measurement.LineCount);
    }

    [Theory]
    [InlineData(1, DescriptionLayoutTarget.LaterPageOneView)]
    [InlineData(2, DescriptionLayoutTarget.LaterPageTwoViews)]
    [InlineData(3, DescriptionLayoutTarget.LaterPageThreeViews)]
    [InlineData(4, DescriptionLayoutTarget.LaterPageFourViews)]
    public void LaterPagesExposeGeometryForEverySupportedViewCount(
        int drawingCount,
        DescriptionLayoutTarget expectedTarget)
    {
        var first = OrderTestData.CreateGarment(2, "First");
        var later = OrderTestData.CreateGarment(
            drawingCount,
            "Later",
            startNewPage: true);
        var page = OrderPageLayoutEngine.BuildPages([first, later])[1];
        var placement = Assert.Single(page.Placements);

        Assert.Equal(drawingCount, placement.Views.Count);
        Assert.All(placement.Views, view =>
        {
            Assert.Equal(2, view.PageNumber);
            Assert.False(view.IsFirstPage);
            Assert.Equal(expectedTarget, view.Geometry.Target);
        });
    }

    [Fact]
    public void SplitFourViewGarmentAssignsEachDescriptionItsActualPageGeometry()
    {
        var garment = OrderTestData.CreateGarment(4, "Split");
        var pages = OrderPageLayoutEngine.BuildPages([garment]);
        var views = pages.SelectMany(page => page.Placements)
            .SelectMany(placement => placement.Views)
            .ToDictionary(view => DrawingLayoutEngine.GetViewName(view.Drawing));

        Assert.Equal(4, views.Count);
        Assert.Equal(1, views["FRONT"].PageNumber);
        Assert.Equal(1, views["BACK"].PageNumber);
        Assert.Equal(2, views["RIGHT"].PageNumber);
        Assert.Equal(2, views["LEFT"].PageNumber);
        Assert.Equal(DescriptionLayoutTarget.FirstPageTwoViews, views["FRONT"].Geometry.Target);
        Assert.Equal(DescriptionLayoutTarget.FirstPageTwoViews, views["BACK"].Geometry.Target);
        Assert.Equal(DescriptionLayoutTarget.LaterPageTwoViews, views["RIGHT"].Geometry.Target);
        Assert.Equal(DescriptionLayoutTarget.LaterPageTwoViews, views["LEFT"].Geometry.Target);
    }

    [Fact]
    public void ExistingFourViewTextBoxBoundaryPassesItsFinalSaveValidation()
    {
        const string productCode = "0510";
        const string productName = "T-Time t-shirt";
        var selectedDrawingCount = 4;
        var front = new TextBox();
        using var controller =
            new GarmentViewDescriptionTextBoxController(
                front,
                () => selectedDrawingCount);

        var accepted = "";

        for (var index = 0; index < 4000; index++)
        {
            front.Text = accepted + "g";

            if (front.Text == accepted)
                break;

            accepted = front.Text ?? "";
        }

        Assert.NotEmpty(accepted);
        Assert.Equal(productCode, "0510");
        Assert.Equal(productName, "T-Time t-shirt");
        Assert.Equal(4, controller.SelectedDrawingCount);
        Assert.Equal(accepted, controller.AcceptedText);
        Assert.True(controller.IsAtCapacity);
        Assert.True(
            controller.IsCurrentTextValidForCommit(
                selectedDrawingCount));

        front.Text = accepted + "g";

        Assert.Equal(accepted, front.Text);
        Assert.True(
            controller.IsCurrentTextValidForCommit(
                selectedDrawingCount));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void ProductionTextBoxControllersKeepEveryViewAtTheSameFinalBoundary(
        int drawingCount)
    {
        var fields = new[]
        {
            new TextBox(),
            new TextBox(),
            new TextBox(),
            new TextBox()
        };
        var controllers = fields
            .Select(
                field => new GarmentViewDescriptionTextBoxController(
                    field,
                    () => drawingCount))
            .ToArray();

        try
        {
            var sources = new[]
            {
                new string('g', 4000),
                string.Join('\n', Enumerable.Repeat("tekst ze spacjami", 200)),
                string.Join('\n', Enumerable.Repeat("ZAŻÓŁĆ GĘŚLĄ JAŹŃ", 200)),
                string.Join("\n\n", Enumerable.Repeat("puste wiersze", 200))
            };

            for (var index = 0; index < fields.Length; index++)
            {
                fields[index].Text = sources[index];

                Assert.True(controllers[index].IsAtCapacity);
                Assert.Equal(fields[index].Text, controllers[index].AcceptedText);
                Assert.Equal(drawingCount, controllers[index].SelectedDrawingCount);
                Assert.True(
                    controllers[index].IsCurrentTextValidForCommit(
                        drawingCount));
            }
        }
        finally
        {
            foreach (var controller in controllers)
                controller.Dispose();
        }
    }

    [Fact]
    public void DescriptionAllowsMoreThanThreeRenderedLinesWhenTheyFit()
    {
        const string description =
            "pierwsza\ndruga\ntrzecia\nczwarta\npiąta";
        var layout =
            DescriptionLayoutKind.FourViews;
        var preview = GarmentViewDescriptionLayout.MeasurePreview(
            description,
            GarmentViewDescriptionLayout.GetReferencePreviewTextWidth(layout),
            GarmentViewDescriptionLayout.GetReferencePreviewTextHeight(layout));
        var pdf = GarmentViewDescriptionLayout.MeasurePdf(
            description,
            GarmentViewDescriptionLayout.GetReferencePdfTextWidth(layout),
            GarmentViewDescriptionLayout.GetReferencePdfTextHeight(layout));

        Assert.True(preview.Fits);
        Assert.True(pdf.Fits);
        Assert.Equal(5, preview.LineCount);
        Assert.Equal(5, pdf.LineCount);
    }

    [Fact]
    public void FourViewSpacingUsesNineDipAndEightPointsWithoutChangingTwoViews()
    {
        Assert.Equal(
            9,
            GarmentViewDescriptionLayout.MultiDrawingPreviewDescriptionGap);
        Assert.Equal(
            7,
            GarmentViewDescriptionLayout.MultiDrawingPreviewDescriptionTopMargin);
        Assert.Equal(8, PdfStyles.MultiDrawingDescriptionTopGap);
        Assert.Equal(2, PdfStyles.DrawingDescriptionTopGap);

        var source = new string('x', 4000);
        var accepted = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            source,
            4);

        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                accepted,
                4));
        Assert.Equal(
            accepted,
            GarmentViewDescriptionLayout.LimitTextChange(
                accepted,
                accepted + "x",
                4));
    }

    [Fact]
    public void AdaptiveFontsKeepAcceptedPreviewAndPdfSizes()
    {
        const string text = "opis";

        Assert.Equal(
            GarmentViewDescriptionLayout.PreviewLargeFontSize,
            GarmentViewDescriptionLayout.MeasurePreview(text, 200, 100).FontSize);
        Assert.Equal(
            GarmentViewDescriptionLayout.PreviewMediumFontSize,
            GarmentViewDescriptionLayout.MeasurePreview(
                text,
                200,
                GarmentViewDescriptionLayout.PreviewMediumFontSize *
                GarmentViewDescriptionLayout.PreviewLineHeight).FontSize);
        Assert.Equal(
            GarmentViewDescriptionLayout.PreviewMinimumFontSize,
            GarmentViewDescriptionLayout.MeasurePreview(
                text,
                200,
                GarmentViewDescriptionLayout.PreviewMinimumFontSize *
                GarmentViewDescriptionLayout.PreviewLineHeight).FontSize);

        Assert.Equal(
            GarmentViewDescriptionLayout.PdfLargeFontSize,
            GarmentViewDescriptionLayout.MeasurePdf(text, 200, 100).FontSize);
        Assert.Equal(
            GarmentViewDescriptionLayout.PdfMediumFontSize,
            GarmentViewDescriptionLayout.MeasurePdf(text, 200, 11.5).FontSize);
        Assert.Equal(
            GarmentViewDescriptionLayout.PdfMinimumFontSize,
            GarmentViewDescriptionLayout.MeasurePdf(text, 200, 10).FontSize);
    }

    [Fact]
    public void InputAcceptsLastFittingCharacterAndRejectsTheNextOne()
    {
        var accepted = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            new string('x', 4000),
            4);

        Assert.NotEmpty(accepted);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                accepted,
                4));
        Assert.Equal(
            accepted,
            GarmentViewDescriptionLayout.LimitTextChange(
                accepted,
                accepted + "x",
                4));
    }

    [Theory]
    [InlineData("ghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghjghj")]
    [InlineData("WIELKIEPOLSKIEŻÓŁĆWIELKIEPOLSKIEŻÓŁĆWIELKIEPOLSKIEŻÓŁĆ")]
    [InlineData("krótki tekst ze spacjami i polskimi znakami: ąęłńóśźż")]
    [InlineData("pierwszy\n\ndrugi\n\n\ntrzeci")]
    [InlineData("tekst ze spacjami na końcu                     ")]
    public void InputControllerAtomicallyLimitsRealEditingCases(
        string fragment)
    {
        var controller =
            new GarmentViewDescriptionInputController("", 4);
        var source = string.Join(
            '\n',
            Enumerable.Repeat(fragment, 100));
        var pasted = controller.Apply(source, 4);

        Assert.NotEmpty(pasted.Text);
        Assert.False(pasted.WasFullyAccepted);
        Assert.Equal(pasted.Text, controller.AcceptedText);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                pasted.Text,
                4));

        var boundary =
            pasted.Text;
        DescriptionInputChange rejected;

        do
        {
            rejected = controller.Apply(
                boundary + "X",
                4);

            if (rejected.WasFullyAccepted)
                boundary = rejected.Text;
        }
        while (rejected.WasFullyAccepted);

        Assert.False(rejected.WasFullyAccepted);
        Assert.Equal(boundary, rejected.Text);
        Assert.Equal(boundary, controller.AcceptedText);

        var shortened = controller.Apply(
            boundary[..^1],
            4);

        Assert.True(shortened.WasFullyAccepted);
        Assert.Equal(boundary[..^1], shortened.Text);

        var restored = controller.Apply(
            shortened.Text + boundary[^1],
            4);

        Assert.True(restored.WasFullyAccepted);
        Assert.Equal(boundary, restored.Text);
    }

    [Fact]
    public void InputControllerPreservesPrefixSuffixAndCaretDuringPasteReplacement()
    {
        const string initial = "PRZÓD  KONIEC";
        var controller =
            new GarmentViewDescriptionInputController(initial, 4);
        var insertion = string.Join(
            '\n',
            Enumerable.Repeat(
                "ghjghjghjghjghjghjghj",
                100));
        var change = controller.Apply(
            "PRZÓD " + insertion + " KONIEC",
            4);

        Assert.False(change.WasFullyAccepted);
        Assert.StartsWith("PRZÓD ghj", change.Text, StringComparison.Ordinal);
        Assert.EndsWith(" KONIEC", change.Text, StringComparison.Ordinal);
        Assert.Equal(
            change.Text.Length - " KONIEC".Length,
            change.CaretIndex);
    }

    [Fact]
    public void ShorterReplacementWithLineBreaksCannotBypassMeasurement()
    {
        var accepted = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            new string('g', 4000),
            4);
        var replaceLength =
            accepted.Length / 2;
        var proposed =
            accepted[..10] +
            string.Join('\n', Enumerable.Repeat("X", 20)) +
            accepted[(10 + replaceLength)..];

        Assert.True(proposed.Length < accepted.Length);

        var controller =
            new GarmentViewDescriptionInputController(accepted, 4);
        var change =
            controller.Apply(proposed, 4);

        Assert.False(change.WasFullyAccepted);
        Assert.NotEqual(proposed, change.Text);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                change.Text,
                4));
    }

    [Fact]
    public void AvaloniaTextChangingSynchronouslyRestoresLastAcceptedText()
    {
        var textBox = new TextBox();
        var controller =
            new GarmentViewDescriptionInputController("", 4);
        var isApplying = false;

        textBox.TextChanging += (_, _) =>
        {
            if (isApplying)
                return;

            var change = controller.Apply(
                textBox.Text,
                4);

            if (change.WasFullyAccepted)
                return;

            isApplying = true;
            textBox.Text = change.Text;
            textBox.CaretIndex = change.CaretIndex;
            textBox.SelectionStart = change.CaretIndex;
            textBox.SelectionEnd = change.CaretIndex;
            isApplying = false;
        };

        textBox.Text = new string('g', 4000);
        var accepted = textBox.Text ?? "";

        Assert.Equal(controller.AcceptedText, accepted);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                accepted,
                4));

        textBox.Text = accepted + "g";

        Assert.Equal(accepted, textBox.Text);
        Assert.Equal(accepted, controller.AcceptedText);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void FullStateRejectsInsertionsAtEveryCursorPositionUntilActualShortening(
        int drawingCount)
    {
        var controller =
            new GarmentViewDescriptionInputController(
                "",
                drawingCount);
        var fullText = FillByTypingUntilRejected(
            controller,
            drawingCount);
        var firstNewLine =
            fullText.IndexOf('\n');
        var positions = new[]
        {
            0,
            fullText.Length / 2,
            firstNewLine >= 0 ? firstNewLine + 1 : fullText.Length / 3,
            fullText.Length - 1
        };

        Assert.True(controller.IsAtCapacity);

        foreach (var position in positions)
        {
            var insertion = controller.Apply(
                fullText.Insert(position, "X"),
                drawingCount);

            Assert.False(insertion.WasFullyAccepted);
            Assert.Equal(fullText, insertion.Text);
            Assert.Equal(fullText, controller.AcceptedText);
            Assert.True(controller.IsAtCapacity);
        }

        var paste = controller.Apply(
            fullText.Insert(fullText.Length / 2, "WKLEJONY TEKST"),
            drawingCount);
        Assert.False(paste.WasFullyAccepted);
        Assert.Equal(fullText, paste.Text);

        var longerReplacement = controller.Apply(
            fullText.Remove(5, 1).Insert(5, "DWA"),
            drawingCount);
        Assert.False(longerReplacement.WasFullyAccepted);
        Assert.Equal(fullText, longerReplacement.Text);

        var shortened = fullText;

        while (controller.IsAtCapacity && shortened.Length > 1)
        {
            shortened = shortened.Remove(
                shortened.Length / 2,
                1);
            var deletion = controller.Apply(
                shortened,
                drawingCount);

            Assert.True(deletion.WasFullyAccepted);
        }

        Assert.False(controller.IsAtCapacity);

        var resumed = controller.Apply(
            shortened + "i",
            drawingCount);

        Assert.True(resumed.WasFullyAccepted);
        Assert.Equal(shortened + "i", resumed.Text);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                resumed.Text,
                drawingCount));

        var secondBoundary = resumed.Text;

        while (true)
        {
            var next = controller.Apply(
                secondBoundary + "g",
                drawingCount);

            if (!next.WasFullyAccepted)
            {
                Assert.True(controller.IsAtCapacity);
                Assert.Equal(secondBoundary, next.Text);
                break;
            }

            secondBoundary = next.Text;
        }
    }

    [Fact]
    public void FourDescriptionFieldsKeepIndependentCapacityStateAcrossFocusChanges()
    {
        var controllers = Enumerable.Range(0, 4)
            .Select(_ => new GarmentViewDescriptionInputController("", 4))
            .ToArray();
        var fullTexts = controllers
            .Select(controller => FillByTypingUntilRejected(controller, 4))
            .ToArray();

        Assert.All(controllers, controller => Assert.True(controller.IsAtCapacity));

        for (var index = 0; index < controllers.Length; index++)
        {
            var unchanged = controllers[index].Apply(
                fullTexts[index],
                4);

            Assert.True(unchanged.WasFullyAccepted);
            Assert.True(controllers[index].IsAtCapacity);
        }

        var shortened = controllers[0].Apply(
            fullTexts[0][..^10],
            4);

        Assert.True(shortened.WasFullyAccepted);
        Assert.False(controllers[0].IsAtCapacity);
        Assert.All(controllers.Skip(1), controller => Assert.True(controller.IsAtCapacity));
    }

    [Theory]
    [InlineData(2)]
    [InlineData(4)]
    public void RealAvaloniaTextBoxRejectsMiddleInsertionAfterCaretMove(
        int drawingCount)
    {
        var textBox = new TextBox();
        var controller =
            new GarmentViewDescriptionInputController(
                "",
                drawingCount);
        var isApplying = false;

        textBox.TextChanging += (_, _) =>
        {
            if (isApplying)
                return;

            var change = controller.Apply(
                textBox.Text,
                drawingCount);

            if (change.WasFullyAccepted)
                return;

            isApplying = true;
            textBox.Text = change.Text;
            textBox.CaretIndex = change.CaretIndex;
            textBox.SelectionStart = change.CaretIndex;
            textBox.SelectionEnd = change.CaretIndex;
            isApplying = false;
        };

        textBox.Text = string.Join(
            '\n',
            Enumerable.Repeat(
                "ghjghjghjghjghjghjghjghj",
                100));
        var fullText =
            textBox.Text ?? "";

        Assert.True(controller.IsAtCapacity);

        foreach (var position in new[]
                 {
                     0,
                     fullText.Length / 2,
                     fullText.Length - 1
                 })
        {
            textBox.CaretIndex = position;
            textBox.SelectionStart = position;
            textBox.SelectionEnd = position;
            textBox.Text = fullText.Insert(position, "X");

            Assert.Equal(fullText, textBox.Text);
            Assert.True(controller.IsAtCapacity);
        }
    }

    [Fact]
    public void DescriptionHeightComesFromCellAndRenderedImage()
    {
        var compact = new DescriptionTargetGeometry(
            DescriptionLayoutTarget.LaterPageThreeViews,
            200,
            240,
            0.5);
        var wideImage = compact with { ImageAspectRatio = 4 };

        Assert.True(
            GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(compact) >
            GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(wideImage));
        Assert.True(
            GarmentViewDescriptionLayout.GetPdfTextHeight(compact) <
            GarmentViewDescriptionLayout.GetPdfTextHeight(wideImage));
        Assert.Equal(
            GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(compact) * compact.ImageAspectRatio,
            GarmentViewDescriptionLayout.GetPdfRenderedImageWidth(compact),
            precision: 3);

        var acceptedSpaces = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            new string(' ', 4000),
            4);

        Assert.True(acceptedSpaces.Length < 4000);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                acceptedSpaces,
                4));
    }

    [Theory]
    [InlineData(DescriptionLayoutTarget.FirstPageOneView)]
    [InlineData(DescriptionLayoutTarget.FirstPageTwoViews)]
    [InlineData(DescriptionLayoutTarget.LaterPageOneView)]
    [InlineData(DescriptionLayoutTarget.LaterPageTwoViews)]
    [InlineData(DescriptionLayoutTarget.LaterPageThreeViews)]
    [InlineData(DescriptionLayoutTarget.LaterPageFourViews)]
    public void EveryDrawingLayoutLimitsSquareImagesToSeventyMillimetres(
        DescriptionLayoutTarget target)
    {
        var geometry =
            GarmentViewDescriptionLayout.GetReferenceGeometry(target);
        var expectedMaximumHeight = 70d / 25.4d * 72d;

        Assert.Equal(
            expectedMaximumHeight,
            GarmentViewDescriptionLayout.GetPdfMaximumImageHeight(geometry),
            precision: 3);
        Assert.Equal(
            expectedMaximumHeight,
            GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(geometry),
            precision: 3);
        Assert.Equal(
            expectedMaximumHeight,
            GarmentViewDescriptionLayout.GetPreviewMaximumImageHeight(geometry) /
            (620d / PdfStyles.PageWidth),
            precision: 3);
    }

    [Fact]
    public void ReferenceLayoutsExposeDifferentDynamicLineCapacities()
    {
        var twoViewCapacity = (int)Math.Floor(
            GarmentViewDescriptionLayout.GetReferencePreviewTextHeight(
                DescriptionLayoutKind.TwoViews) /
            (GarmentViewDescriptionLayout.PreviewMinimumFontSize *
             GarmentViewDescriptionLayout.PreviewLineHeight));
        var fourViewCapacity = (int)Math.Floor(
            GarmentViewDescriptionLayout.GetReferencePreviewTextHeight(
                DescriptionLayoutKind.FourViews) /
            (GarmentViewDescriptionLayout.PreviewMinimumFontSize *
             GarmentViewDescriptionLayout.PreviewLineHeight));

        Assert.True(twoViewCapacity > 3);
        Assert.True(fourViewCapacity > 3);
        Assert.NotEqual(twoViewCapacity, fourViewCapacity);
    }

    [Fact]
    public void PasteKeepsLongestFittingPrefixAndPreservesSuffix()
    {
        const string accepted = "Początek  koniec";
        var insertion = new string('ż', 4000);
        var proposed = "Początek " + insertion + " koniec";
        var limited = GarmentViewDescriptionLayout.LimitTextChange(
            accepted,
            proposed,
            4);

        Assert.StartsWith("Początek ż", limited, StringComparison.Ordinal);
        Assert.EndsWith(" koniec", limited, StringComparison.Ordinal);
        Assert.True(limited.Length < proposed.Length);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                limited,
                4));
    }

    [Fact]
    public void BackspaceAndDeleteRemainAvailableAtCapacity()
    {
        var accepted = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            new string('x', 4000),
            4);
        var backspace = accepted[..^1];
        var delete = accepted.Remove(
            accepted.Length / 2,
            1);

        Assert.Equal(
            backspace,
            GarmentViewDescriptionLayout.LimitTextChange(
                accepted,
                backspace,
                4));
        Assert.Equal(
            delete,
            GarmentViewDescriptionLayout.LimitTextChange(
                accepted,
                delete,
                4));
    }

    [Fact]
    public void ManualLineBreaksAreMeasuredAndLimitedByHeight()
    {
        var source = string.Join(
            '\n',
            Enumerable.Repeat("wiersz", 100));
        var accepted = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            source,
            4);
        var measurement = GarmentViewDescriptionLayout.MeasurePreview(
            accepted,
            GarmentViewDescriptionLayout.GetReferencePreviewTextWidth(
                DescriptionLayoutKind.FourViews),
            GarmentViewDescriptionLayout.GetReferencePreviewTextHeight(
                DescriptionLayoutKind.FourViews));

        Assert.True(measurement.Fits);
        Assert.True(measurement.LineCount > 3);
        Assert.True(accepted.Length < source.Length);
    }

    [Fact]
    public void ReferenceTwoViewLayoutAcceptsMoreTextThanFourViewLayout()
    {
        var source = new string('x', 4000);
        var twoViewText = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            source,
            2);
        var fourViewText = GarmentViewDescriptionLayout.LimitTextChange(
            "",
            source,
            4);

        Assert.True(twoViewText.Length > fourViewText.Length);
        Assert.True(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                twoViewText,
                2));
        Assert.False(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                twoViewText,
                4));
    }

    [Fact]
    public void ExistingTextIsNotTruncatedWhenLayoutTargetChanges()
    {
        var source = new string('x', 4000);
        var acceptedForTwoViews =
            GarmentViewDescriptionLayout.LimitTextChange(
                "",
                source,
                2);

        Assert.False(
            GarmentViewDescriptionLayout.FitsEditorTargets(
                acceptedForTwoViews,
                4));
        Assert.Equal(
            acceptedForTwoViews,
            GarmentViewDescriptionLayout.LimitTextChange(
                acceptedForTwoViews,
                acceptedForTwoViews,
                4));
    }

    [Fact]
    public void DescriptionsMapToMatchingViewAndGarment()
    {
        var first = OrderTestData.CreateGarment(4, "Pierwsza");
        var second = OrderTestData.CreateGarment(4, "Druga");

        first.ViewDescriptions.Front = "Pierwsza: przód żółty";
        first.ViewDescriptions.Back = "Pierwsza: tył granatowy";
        first.ViewDescriptions.Right = "Pierwsza: prawy bok";
        first.ViewDescriptions.Left = "Pierwsza: lewy bok";
        second.ViewDescriptions.Front = "Druga: przód";

        Assert.Equal(
            "Pierwsza: przód żółty",
            GarmentViewDescriptionLayout.GetDescription(first, first.Drawings[0]));
        Assert.Equal(
            "Pierwsza: tył granatowy",
            GarmentViewDescriptionLayout.GetDescription(first, first.Drawings[1]));
        Assert.Equal(
            "Pierwsza: prawy bok",
            GarmentViewDescriptionLayout.GetDescription(first, first.Drawings[2]));
        Assert.Equal(
            "Pierwsza: lewy bok",
            GarmentViewDescriptionLayout.GetDescription(first, first.Drawings[3]));
        Assert.Equal(
            "Druga: przód",
            GarmentViewDescriptionLayout.GetDescription(second, second.Drawings[0]));
    }

    [Fact]
    public void EditorAndPreviewUseDynamicHeightWithoutRedValidation()
    {
        var editor = XDocument.Load(GetAppPath("Views", "GarmentEditorWindow.axaml"));
        var drawingBox = XDocument.Load(GetAppPath("Controls", "DrawingBox.axaml"));
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";
        var fieldNames = editor.Descendants()
            .Where(element => element.Name.LocalName == "TextBox")
            .Select(element => (string?)element.Attribute(x + "Name"))
            .Where(name => name != null)
            .ToHashSet(StringComparer.Ordinal);
        var description = Assert.Single(
            drawingBox.Descendants(),
            element =>
                element.Name.LocalName == "TextBlock" &&
                (string?)element.Attribute(x + "Name") == "DescriptionTextBlock");
        var editorCode = File.ReadAllText(
            GetAppPath("Views", "GarmentEditorWindow.axaml.cs"));
        var textBoxControllerCode = File.ReadAllText(
            GetAppPath("Layout", "GarmentViewDescriptionTextBoxController.cs"));
        var garmentPageCode = File.ReadAllText(
            GetAppPath("Controls", "GarmentPageSection.axaml.cs"));
        var drawingSectionCode = File.ReadAllText(
            GetAppPath("Controls", "DrawingSection.axaml.cs"));
        var drawingBoxCode = File.ReadAllText(
            GetAppPath("Controls", "DrawingBox.axaml.cs"));

        Assert.Contains("FrontDescriptionTextBox", fieldNames);
        Assert.Contains("BackDescriptionTextBox", fieldNames);
        Assert.Contains("RightDescriptionTextBox", fieldNames);
        Assert.Contains("LeftDescriptionTextBox", fieldNames);
        Assert.Null(description.Attribute("MaxLines"));
        Assert.Equal("Wrap", (string?)description.Attribute("TextWrapping"));
        Assert.Equal("None", (string?)description.Attribute("TextTrimming"));
        Assert.Equal("Left", (string?)description.Attribute("TextAlignment"));
        Assert.Equal("Stretch", (string?)description.Attribute("HorizontalAlignment"));
        Assert.Equal("6,1,6,2", (string?)description.Attribute("Margin"));
        Assert.DoesNotContain("DescriptionValidationText", editor.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain("#B42318", editor.ToString(), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("GetPreviewTextHeight", drawingBoxCode, StringComparison.Ordinal);
        Assert.Contains("TextChanging", textBoxControllerCode, StringComparison.Ordinal);
        Assert.Contains("TextChanged", textBoxControllerCode, StringComparison.Ordinal);
        Assert.Contains("GarmentViewDescriptionTextBoxController", editorCode, StringComparison.Ordinal);
        Assert.Contains("Placements", garmentPageCode, StringComparison.Ordinal);
        Assert.Contains("GetTargetGeometry", drawingSectionCode, StringComparison.Ordinal);
        Assert.Contains("GetPreviewTextHeight(DescriptionGeometry)", drawingBoxCode, StringComparison.Ordinal);
        Assert.Contains("Przed dodaniem kolejnego rzutu", editorCode, StringComparison.Ordinal);

        var contentPanel = Assert.IsType<XElement>(description.Parent);
        Assert.Equal("StackPanel", contentPanel.Name.LocalName);
        Assert.Equal("DrawingContentPanel", (string?)contentPanel.Attribute(x + "Name"));
        Assert.Equal("Top", (string?)contentPanel.Attribute("VerticalAlignment"));
        Assert.Equal("Stretch", (string?)contentPanel.Attribute("HorizontalAlignment"));
        Assert.Null(description.Attribute("Grid.Row"));

        var compactChildren = contentPanel.Elements().ToList();
        Assert.Equal(2, compactChildren.Count);
        Assert.Equal("Image", compactChildren[0].Name.LocalName);
        Assert.Equal("TextBlock", compactChildren[1].Name.LocalName);
    }

    [Fact]
    public void PreviewDrawingHeightIsIndependentOfDescriptionMeasurement()
    {
        var drawingBoxCode = File.ReadAllText(
            GetAppPath("Controls", "DrawingBox.axaml.cs"));
        var editorCode = File.ReadAllText(
            GetAppPath("Views", "GarmentEditorWindow.axaml.cs"));
        var mainViewModelCode = File.ReadAllText(
            GetAppPath("ViewModels", "MainViewModel.cs"));
        var pdfGeneratorCode = File.ReadAllText(
            GetAppPath("Services/Pdf", "OrderPdfGenerator.cs"));

        Assert.Contains(
            "DrawingImage.MaxHeight =\n            MaxDrawingHeight;",
            drawingBoxCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "availableImageHeight",
            drawingBoxCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "DrawingImage.MaxWidth",
            drawingBoxCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "effectiveMaximumImageHeight",
            pdfGeneratorCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "cellHeight\n                - PdfStyles.DrawingTitleHeight\n                - DrawingTopGap\n                - PdfStyles.DrawingCellPadding * 2\n                - descriptionHeight",
            pdfGeneratorCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "ShowDescriptionTooLongMessageAsync",
            editorCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "ShowDescriptionTooLongDialog",
            mainViewModelCode,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "#B42318",
            editorCode,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddingDescriptionsDoesNotChangePagePlan()
    {
        var garments = new[]
        {
            OrderTestData.CreateGarment(3, "Pierwsza"),
            OrderTestData.CreateGarment(3, "Druga")
        };
        var before = OrderPageLayoutEngine.BuildPages(garments)
            .Select(page => page.PageNumberText)
            .ToArray();

        foreach (var garment in garments)
        {
            garment.ViewDescriptions.Front = "Opis przodu z polskimi znakami: żółć";
            garment.ViewDescriptions.Back = "Opis tyłu";
            garment.ViewDescriptions.Right = "Opis prawego boku";
        }

        var after = OrderPageLayoutEngine.BuildPages(garments)
            .Select(page => page.PageNumberText)
            .ToArray();

        Assert.Equal(["1/2", "2/2"], before);
        Assert.Equal(before, after);
    }

    private static string GetAppPath(
        string directory,
        string fileName)
    {
        return Path.GetFullPath(
            Path.Combine(
                AppContext.BaseDirectory,
                "..",
                "..",
                "..",
                "..",
                "COMMA.App",
                directory,
                fileName));
    }

    private static string FillByTypingUntilRejected(
        GarmentViewDescriptionInputController controller,
        int drawingCount)
    {
        const string source =
            "ghjghjghjghjghjghjghjghj ŻÓŁĆ\n";
        var accepted = "";

        for (var index = 0; index < 10000; index++)
        {
            var character =
                source[index % source.Length];
            var change = controller.Apply(
                accepted + character,
                drawingCount);

            if (!change.WasFullyAccepted)
            {
                Assert.Equal(accepted, change.Text);
                return accepted;
            }

            accepted = change.Text;
        }

        throw new InvalidOperationException(
            "Opis nie osiągnął stanu pełnego.");
    }

    private static string FillByTypingUntilRejected(
        GarmentViewDescriptionInputController controller,
        DescriptionTargetGeometry geometry,
        string source)
    {
        var accepted = "";

        for (var index = 0; index < 10000; index++)
        {
            var change = controller.Apply(
                accepted + source[index % source.Length],
                geometry);

            if (!change.WasFullyAccepted)
                return accepted;

            accepted = change.Text;
        }

        throw new InvalidOperationException(
            "Opis nie osiągnął granicy trzech linii.");
    }

    private static void AssertThreeLineBoundary(
        string text,
        DescriptionTargetGeometry geometry)
    {
        var preview = GarmentViewDescriptionLayout.MeasurePreview(
            text,
            GarmentViewDescriptionLayout.GetPreviewTextWidth(geometry),
            GarmentViewDescriptionLayout.GetPreviewTextHeight(geometry));
        var pdf = GarmentViewDescriptionLayout.MeasurePdf(
            text,
            GarmentViewDescriptionLayout.GetPdfTextWidth(geometry),
            GarmentViewDescriptionLayout.GetPdfTextHeight(geometry));

        Assert.True(preview.Fits);
        Assert.True(pdf.Fits);
        Assert.Equal(3, preview.LineCount);
        Assert.Equal(3, pdf.LineCount);
    }
}
