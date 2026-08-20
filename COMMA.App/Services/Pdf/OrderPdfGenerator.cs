using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMMA.App.Layout;
using COMMA.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace COMMA.App.Services.Pdf;

public static class OrderPdfGenerator
{
    private const byte WhiteThreshold =
        248;

    private const float GarmentTitleHeight =
        21f;

    private const float GarmentGap =
        4f;

    private const float DrawingTopGap =
        7f;


    public static void Generate(
        string outputPath,
        ProductionCard card,
        IReadOnlyList<OrderPageLayout> pages)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(pages);

        if (pages.Count == 0)
        {
            throw new InvalidOperationException(
                "Zlecenie nie zawiera żadnej strony do wygenerowania.");
        }

        QuestPDF.Settings.License =
            LicenseType.Community;

        QuestPDF.Settings.EnableDebugging =
            true;

        Document.Create(document =>
        {
            foreach (var orderPage in pages)
            {
                document.Page(page =>
                {
                    page.Size(
                        PageSizes.A4);

                    page.Margin(
                        PdfStyles.PageMargin);

                    page.DefaultTextStyle(style =>
                        style.FontSize(
                            PdfStyles.DefaultFontSize));

                    page.Content()
                        .Border(
                            PdfStyles.OuterBorderWidth)
                        .Padding(
                            PdfStyles.PagePadding)
                        .Column(column =>
                        {
                            if (orderPage.IsFirstPage)
                            {
                                BuildFirstPage(
                                    column,
                                    card,
                                    orderPage);
                            }
                            else
                            {
                                BuildLaterPage(
                                    column,
                                    card,
                                    orderPage);
                            }
                        });
                });
            }
        })
        .GeneratePdf(
            outputPath);
    }


    // =========================================================
    // STRONA 1
    // =========================================================

    private static void BuildFirstPage(
        ColumnDescriptor column,
        ProductionCard card,
        OrderPageLayout page)
    {
        HeaderSection.Build(
            column,
            card);

        column.Item()
            .PaddingTop(
                PdfStyles.SectionGap);

        HandwrittenSection.Build(
            column,
            card);

        column.Item()
            .PaddingTop(
                PdfStyles.SectionGap);

        var availableHeight =
            PdfStyles.AvailableContentHeight
            - PdfStyles.HeaderHeight
            - PdfStyles.HandwrittenSectionHeight
            - PdfStyles.SectionGap * 2
            - PdfStyles.PageSafetyReserve;

        column.Item()
            .Height(
                availableHeight)
            .Element(container =>
                BuildGarmentPage(
                    container,
                    page,
                    availableHeight));
    }


    // =========================================================
    // STRONA 2+
    // =========================================================

    private static void BuildLaterPage(
        ColumnDescriptor column,
        ProductionCard card,
        OrderPageLayout page)
    {
        column.Item()
            .Height(
                PdfStyles.HeaderHeight)
            .Element(container =>
                BuildLaterPageHeader(
                    container,
                    card,
                    page));

        column.Item()
            .PaddingTop(
                PdfStyles.SectionGap);

        var availableHeight =
            PdfStyles.AvailableContentHeight
            - PdfStyles.HeaderHeight
            - PdfStyles.SectionGap
            - PdfStyles.PageSafetyReserve;

        column.Item()
            .Height(
                availableHeight)
            .Element(container =>
                BuildGarmentPage(
                    container,
                    page,
                    availableHeight));
    }


    private static void BuildLaterPageHeader(
        IContainer container,
        ProductionCard card,
        OrderPageLayout page)
    {
        container
            .Row(row =>
            {
                row.ConstantItem(
                        PdfStyles.HeaderLogoWidth)
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .Padding(
                        PdfStyles.HeaderCellPadding)
                    .Element(
                        BuildPimpLogo);

                row.RelativeItem()
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .Padding(
                        PdfStyles.HeaderOrderNamePadding)
                    .Column(column =>
                    {
                        column.Item()
                            .Height(14)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text("NAZWA ZLECENIA")
                            .FontSize(
                                PdfStyles.HeaderOrderLabelFontSize)
                            .Bold();

                        column.Item()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(
                                SafeOrderName(
                                    card))
                            .FontSize(
                                PdfStyles.HeaderOrderNameFontSize)
                            .Bold();
                    });

                row.ConstantItem(88)
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .Padding(4)
                    .Column(column =>
                    {
                        column.Item()
                            .Height(14)
                            .AlignCenter()
                            .AlignMiddle()
                            .Text("STRONA")
                            .FontSize(
                                PdfStyles.HeaderOrderLabelFontSize)
                            .Bold();

                        column.Item()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(
                                page.PageNumberText)
                            .FontSize(13)
                            .Bold();
                    });
            });
    }


    private static void BuildPimpLogo(
        IContainer container)
    {
        var logoPath =
            Path.Combine(
                AppContext.BaseDirectory,
                "Assets",
                "Branding",
                "PimpLogoExact.png");

        if (!File.Exists(logoPath))
        {
            container
                .AlignCenter()
                .AlignMiddle()
                .Text("PIMP")
                .FontSize(
                    PdfStyles.LogoFontSize)
                .Bold();

            return;
        }

        container
            .AlignCenter()
            .AlignMiddle()
            .Image(
                logoPath)
            .FitArea();
    }


    private static string SafeOrderName(
        ProductionCard card)
    {
        return string.IsNullOrWhiteSpace(
                card.OrderName)
            ? "NAZWA ZLECENIA"
            : card.OrderName.Trim();
    }


    // =========================================================
    // UKŁAD ODZIEŻY NA STRONIE
    // =========================================================

    private static void BuildGarmentPage(
        IContainer container,
        OrderPageLayout page,
        float availableHeight)
    {
        var garments =
            page.Garments;

        if (garments.Count == 0)
        {
            container
                .Border(
                    PdfStyles.StandardBorderWidth)
                .AlignCenter()
                .AlignMiddle()
                .Text("BRAK ODZIEŻY")
                .Bold();

            return;
        }

        switch (garments.Count)
        {
            case 1:
                BuildSingleGarmentPage(
                    container,
                    garments[0],
                    availableHeight);

                break;

            case 2:
                BuildTwoGarmentPage(
                    container,
                    garments[0],
                    garments[1],
                    availableHeight);

                break;

            case 3:
                BuildThreeGarmentPage(
                    container,
                    garments[0],
                    garments[1],
                    garments[2],
                    availableHeight);

                break;

            default:
                BuildFourGarmentPage(
                    container,
                    garments[0],
                    garments[1],
                    garments[2],
                    garments[3],
                    availableHeight);

                break;
        }
    }


    private static void BuildSingleGarmentPage(
        IContainer container,
        OrderGarmentItem garment,
        float availableHeight)
    {
        BuildGarmentBox(
            container,
            garment,
            availableHeight);
    }


    private static void BuildTwoGarmentPage(
        IContainer container,
        OrderGarmentItem first,
        OrderGarmentItem second,
        float availableHeight)
    {
        var garmentHeight =
            (availableHeight - GarmentGap) /
            2f;

        container
            .Column(column =>
            {
                column.Item()
                    .Height(
                        garmentHeight)
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            first,
                            garmentHeight));

                column.Item()
                    .Height(
                        GarmentGap);

                column.Item()
                    .Height(
                        garmentHeight)
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            second,
                            garmentHeight));
            });
    }


    private static void BuildThreeGarmentPage(
        IContainer container,
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third,
        float availableHeight)
    {
        var garments =
            new[]
            {
                first,
                second,
                third
            };

        var totalDrawingCount =
            garments.Sum(
                garment =>
                    garment.SelectedDrawingCount);

        var twoDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.SelectedDrawingCount == 2)
                .ToList();

        var oneDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.SelectedDrawingCount == 1)
                .ToList();

        if (totalDrawingCount == 4 &&
            twoDrawingGarments.Count == 1 &&
            oneDrawingGarments.Count == 2)
        {
            BuildBalancedThreeGarmentPage(
                container,
                first,
                second,
                third,
                twoDrawingGarments[0],
                oneDrawingGarments,
                availableHeight);

            return;
        }

        var rowHeight =
            (availableHeight - GarmentGap) /
            2f;

        container
            .Column(column =>
            {
                column.Item()
                    .Height(
                        rowHeight)
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            first,
                            rowHeight));

                column.Item()
                    .Height(
                        GarmentGap);

                column.Item()
                    .Height(
                        rowHeight)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    second,
                                    rowHeight));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    third,
                                    rowHeight));
                    });
            });
    }


    private static void BuildBalancedThreeGarmentPage(
        IContainer container,
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third,
        OrderGarmentItem twoDrawingGarment,
        IReadOnlyList<OrderGarmentItem> oneDrawingGarments,
        float availableHeight)
    {
        var rowHeight =
            (availableHeight - GarmentGap) /
            2f;

        var twoDrawingGarmentIsFirst =
            ReferenceEquals(
                twoDrawingGarment,
                first);

        var twoDrawingGarmentIsThird =
            ReferenceEquals(
                twoDrawingGarment,
                third);

        container
            .Column(column =>
            {
                if (twoDrawingGarmentIsFirst)
                {
                    column.Item()
                        .Height(
                            rowHeight)
                        .Element(cell =>
                            BuildGarmentBox(
                                cell,
                                twoDrawingGarment,
                                rowHeight));

                    column.Item()
                        .Height(
                            GarmentGap);

                    column.Item()
                        .Height(
                            rowHeight)
                        .Element(row =>
                            BuildGarmentPairRow(
                                row,
                                oneDrawingGarments[0],
                                oneDrawingGarments[1],
                                rowHeight));

                    return;
                }

                if (twoDrawingGarmentIsThird)
                {
                    column.Item()
                        .Height(
                            rowHeight)
                        .Element(row =>
                            BuildGarmentPairRow(
                                row,
                                oneDrawingGarments[0],
                                oneDrawingGarments[1],
                                rowHeight));

                    column.Item()
                        .Height(
                            GarmentGap);

                    column.Item()
                        .Height(
                            rowHeight)
                        .Element(cell =>
                            BuildGarmentBox(
                                cell,
                                twoDrawingGarment,
                                rowHeight));

                    return;
                }

                column.Item()
                    .Height(
                        rowHeight)
                    .Element(row =>
                        BuildGarmentPairRow(
                            row,
                            oneDrawingGarments[0],
                            oneDrawingGarments[1],
                            rowHeight));

                column.Item()
                    .Height(
                        GarmentGap);

                column.Item()
                    .Height(
                        rowHeight)
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            twoDrawingGarment,
                            rowHeight));
            });
    }


    private static void BuildGarmentPairRow(
        IContainer container,
        OrderGarmentItem left,
        OrderGarmentItem right,
        float rowHeight)
    {
        container
            .Row(row =>
            {
                row.RelativeItem()
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            left,
                            rowHeight));

                row.ConstantItem(
                    GarmentGap);

                row.RelativeItem()
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            right,
                            rowHeight));
            });
    }


    private static void BuildFourGarmentPage(
        IContainer container,
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third,
        OrderGarmentItem fourth,
        float availableHeight)
    {
        var rowHeight =
            (availableHeight - GarmentGap) /
            2f;

        container
            .Column(column =>
            {
                column.Item()
                    .Height(
                        rowHeight)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    first,
                                    rowHeight));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    second,
                                    rowHeight));
                    });

                column.Item()
                    .Height(
                        GarmentGap);

                column.Item()
                    .Height(
                        rowHeight)
                    .Row(row =>
                    {
                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    third,
                                    rowHeight));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    fourth,
                                    rowHeight));
                    });
            });
    }


    // =========================================================
    // JEDNA POZYCJA ODZIEŻY
    // =========================================================

    private static void BuildGarmentBox(
        IContainer container,
        OrderGarmentItem garment,
        float totalHeight)
    {
        var drawingHeight =
            Math.Max(
                1f,
                totalHeight -
                GarmentTitleHeight);

        container
            .Column(column =>
            {
                column.Item()
                    .Height(
                        GarmentTitleHeight)
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(
                        SafeGarmentName(
                            garment))
                    .FontSize(
                        PdfStyles.OrderValueFontSize)
                    .Bold();

                column.Item()
                    .Height(
                        drawingHeight)
                    .Element(drawings =>
                        BuildGarmentDrawings(
                            drawings,
                            garment,
                            drawingHeight));
            });
    }


    private static string SafeGarmentName(
        OrderGarmentItem garment)
    {
        return string.IsNullOrWhiteSpace(
                garment.DisplayName)
            ? "ODZIEŻ"
            : garment.DisplayName.Trim();
    }


    // =========================================================
    // RZUTY POJEDYNCZEJ ODZIEŻY
    // =========================================================

    private static void BuildGarmentDrawings(
        IContainer container,
        OrderGarmentItem garment,
        float availableHeight)
    {
        var rows =
            DrawingLayoutEngine.GetRows(
                garment);

        if (rows.Count == 0)
        {
            container
                .Border(
                    PdfStyles.StandardBorderWidth)
                .AlignCenter()
                .AlignMiddle()
                .Text(
                    "WYBIERZ CO NAJMNIEJ JEDEN RZUT")
                .FontSize(
                    PdfStyles.FieldValueFontSize)
                .Bold();

            return;
        }

        var rowHeight =
            availableHeight /
            rows.Count;

        var drawingCount =
            rows.Sum(row =>
                row.Second == null
                    ? 1
                    : 2);

        foreach (var _ in Array.Empty<int>())
        {
        }

        container
            .Column(column =>
            {
                foreach (var layoutRow in rows)
                {
                    column.Item()
                        .Height(
                            rowHeight)
                        .Element(row =>
                            BuildDrawingRow(
                                row,
                                layoutRow,
                                rowHeight,
                                rows.Count,
                                drawingCount));
                }
            });
    }


    private static void BuildDrawingRow(
        IContainer container,
        DrawingLayoutRow layoutRow,
        float cellHeight,
        int rowCount,
        int drawingCount)
    {
        if (layoutRow.Second == null ||
            layoutRow.FirstColumnSpan == 2)
        {
            container
                .Element(cell =>
                    DrawDrawingCell(
                        cell,
                        layoutRow.First,
                        cellHeight,
                        rowCount,
                        drawingCount));

            return;
        }

        container
            .Row(row =>
            {
                row.RelativeItem()
                    .Element(cell =>
                        DrawDrawingCell(
                            cell,
                            layoutRow.First,
                            cellHeight,
                            rowCount,
                            drawingCount));

                row.RelativeItem()
                    .Element(cell =>
                        DrawDrawingCell(
                            cell,
                            layoutRow.Second,
                            cellHeight,
                            rowCount,
                            drawingCount));
            });
    }


    private static void DrawDrawingCell(
        IContainer container,
        DrawingFile drawing,
        float cellHeight,
        int rowCount,
        int drawingCount)
    {
        var maximumImageHeight =
            GetMaximumImageHeight(
                rowCount,
                drawingCount);

        container
            .Border(
                PdfStyles.StandardBorderWidth)
            .Padding(
                PdfStyles.DrawingCellPadding)
            .Column(column =>
            {
                column.Item()
                    .Height(
                        PdfStyles.DrawingTitleHeight)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(
                        GetDrawingTitle(
                            drawing))
                    .FontSize(
                        PdfStyles.DrawingTitleFontSize)
                    .Bold();

                column.Item()
                    .PaddingTop(
                        DrawingTopGap)
                    .Height(
                        Math.Max(
                            1f,
                            cellHeight
                            - PdfStyles.DrawingTitleHeight
                            - DrawingTopGap
                            - PdfStyles.DrawingCellPadding * 2))
                    .AlignTop()
                    .AlignCenter()
                    .Element(imageArea =>
                    {
                        var drawingArea =
                            imageArea
                            .MaxHeight(
                                maximumImageHeight)
                            .AlignTop()
                            .AlignCenter();

                        if (drawingCount < 3)
                        {
                            drawingArea =
                                drawingArea.MaxWidth(
                                    maximumImageHeight);
                        }

                        drawingArea
                            .Element(image =>
                                DrawImage(
                                    image,
                                    drawing,
                                    drawingCount >= 3));
                    });
            });
    }


    /*
     * Dla 1–2 rysunków wielkość maksymalna jest liczona dokładnie
     * z tej samej geometrii, której używał PDF 2.0.
     *
     * Dla 3–4 rysunków obowiązuje wspólny limit 70 mm wysokości.
     */
    private static float GetMaximumImageHeight(
        int rowCount,
        int drawingCount)
    {
        if (drawingCount >= 3)
            return PdfStyles.MultiDrawingMaximumHeight;

        var referenceRowHeight =
            PdfStyles.GetDrawingRowHeight(
                rowCount);

        var imageHeight =
            PdfStyles.GetDrawingImageHeight(
                referenceRowHeight);

        return
            imageHeight *
            0.75f;
    }


    private static void DrawImage(
        IContainer container,
        DrawingFile drawing,
        bool cropDrawingImage)
    {
        if (string.IsNullOrWhiteSpace(
                drawing.FullPath) ||
            !File.Exists(
                drawing.FullPath))
        {
            container
                .AlignCenter()
                .AlignMiddle()
                .Text("BRAK RYSUNKU")
                .FontSize(
                    PdfStyles.DefaultFontSize)
                .Bold();

            return;
        }

        var cleanedImage =
            cropDrawingImage
                ? DrawingImageCropper.TryCreateCroppedPng(
                    drawing.FullPath)
                : PrepareImageForPdf(
                    drawing.FullPath);

        if (cleanedImage.Length == 0)
        {
            DrawOriginalImage(
                container,
                drawing);

            return;
        }

        if (drawing.MirrorHorizontally)
        {
            container
                .FlipHorizontal()
                .Image(
                    cleanedImage)
                .FitArea();

            return;
        }

        container
            .Image(
                cleanedImage)
            .FitArea();
    }


    private static void DrawOriginalImage(
        IContainer container,
        DrawingFile drawing)
    {
        if (drawing.MirrorHorizontally)
        {
            container
                .FlipHorizontal()
                .Image(
                    drawing.FullPath)
                .FitArea();

            return;
        }

        container
            .Image(
                drawing.FullPath)
            .FitArea();
    }


    // =========================================================
    // CZYSZCZENIE BIAŁEGO TŁA
    // =========================================================

    private static byte[] PrepareImageForPdf(
        string filePath)
    {
        try
        {
            using var sourceBitmap =
                SKBitmap.Decode(
                    filePath);

            if (sourceBitmap == null)
                return [];

            using var cleanedBitmap =
                new SKBitmap(
                    sourceBitmap.Width,
                    sourceBitmap.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Opaque);

            for (
                var y = 0;
                y < sourceBitmap.Height;
                y++)
            {
                for (
                    var x = 0;
                    x < sourceBitmap.Width;
                    x++)
                {
                    var sourceColor =
                        sourceBitmap.GetPixel(
                            x,
                            y);

                    var alpha =
                        sourceColor.Alpha;

                    var red =
                        CompositeAgainstWhite(
                            sourceColor.Red,
                            alpha);

                    var green =
                        CompositeAgainstWhite(
                            sourceColor.Green,
                            alpha);

                    var blue =
                        CompositeAgainstWhite(
                            sourceColor.Blue,
                            alpha);

                    if (red >= WhiteThreshold &&
                        green >= WhiteThreshold &&
                        blue >= WhiteThreshold)
                    {
                        cleanedBitmap.SetPixel(
                            x,
                            y,
                            SKColors.White);

                        continue;
                    }

                    cleanedBitmap.SetPixel(
                        x,
                        y,
                        new SKColor(
                            red,
                            green,
                            blue,
                            255));
                }
            }

            using var image =
                SKImage.FromBitmap(
                    cleanedBitmap);

            using var data =
                image.Encode(
                    SKEncodedImageFormat.Png,
                    100);

            return
                data?.ToArray() ??
                [];
        }
        catch
        {
            return [];
        }
    }


    private static byte CompositeAgainstWhite(
        byte colour,
        byte alpha)
    {
        var result =
            colour * alpha +
            255 * (255 - alpha);

        return (byte)(
            (result + 127) /
            255);
    }


    private static string GetDrawingTitle(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return "PRZÓD";

        if (drawing.IsBack)
            return "TYŁ";

        if (drawing.IsRight)
            return "PRAWY BOK";

        if (drawing.IsLeft)
            return "LEWY BOK";

        return "RYSUNEK TECHNICZNY";
    }
}
