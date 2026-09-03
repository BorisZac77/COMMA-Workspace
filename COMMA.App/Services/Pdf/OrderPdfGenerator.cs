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

        ValidateDescriptionLayout(
            pages);

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
                        .Height(
                            PdfStyles.AvailableContentHeight)
                        .ShowEntire()
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
            card,
            page.PageNumberText);

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
        var orderNumber =
            SafeOrderNumber(card);

        var orderName =
            SafeOrderName(card);

        var orderNumberFit =
            OrderHeaderTextLayout.FitNumber(
                orderNumber,
                OrderHeaderTextLayout.PdfLaterPageNumberGeometry);

        var orderNameFit =
            OrderHeaderTextLayout.FitName(
                orderName,
                OrderHeaderTextLayout.PdfLaterPageNameGeometry);

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

                row.ConstantItem(
                        PdfStyles.FirstPageHeaderOrderNumberWidth)
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .PaddingHorizontal(
                        PdfStyles.HeaderIdentityHorizontalPadding)
                    .PaddingVertical(1)
                    .Column(column =>
                    {
                        BuildLaterPageHeaderLabel(
                            column,
                            "NUMER ZLECENIA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(orderNumberFit.DisplayText)
                            .FontSize((float)orderNumberFit.FontSize)
                            .LineHeight(OrderHeaderTextLayout.LineHeight)
                            .FontColor(
                                PdfStyles.OrderNameColor)
                            .Bold();
                    });

                row.RelativeItem()
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .PaddingHorizontal(
                        PdfStyles.HeaderIdentityHorizontalPadding)
                    .PaddingVertical(1)
                    .Column(column =>
                    {
                        BuildLaterPageHeaderLabel(
                            column,
                            "NAZWA ZLECENIA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(orderNameFit.DisplayText)
                            .FontSize((float)orderNameFit.FontSize)
                            .LineHeight(OrderHeaderTextLayout.LineHeight)
                            .FontColor(
                                PdfStyles.OrderNameColor)
                            .Bold();
                    });

                row.ConstantItem(
                        PdfStyles.FirstPageHeaderPageNumberWidth)
                    .Border(
                        PdfStyles.StandardBorderWidth)
                    .Padding(1)
                    .Column(column =>
                    {
                        BuildLaterPageHeaderLabel(
                            column,
                            "STRONA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(
                                page.PageNumberText)
                            .FontSize(13)
                            .Bold();
                    });
            });
    }


    private static void BuildLaterPageHeaderLabel(
        ColumnDescriptor column,
        string label)
    {
        column.Item()
            .Height(PdfStyles.HeaderOrderLabelHeight)
            .AlignCenter()
            .AlignMiddle()
            .Text(label)
            .FontSize(
                PdfStyles.HeaderOrderLabelFontSize)
            .Bold();
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


    private static string SafeOrderNumber(
        ProductionCard card)
    {
        return card.OrderNumber?.Trim() ??
               string.Empty;
    }


    // =========================================================
    // UKŁAD ODZIEŻY NA STRONIE
    // =========================================================

    private static void BuildGarmentPage(
        IContainer container,
        OrderPageLayout page,
        float availableHeight)
    {
        var placements =
            page.Placements;

        if (placements.Count == 0)
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

        switch (placements.Count)
        {
            case 1:
                BuildSingleGarmentPage(
                    container,
                    placements[0],
                    availableHeight,
                    page);

                break;

            case 2:
                BuildTwoGarmentPage(
                    container,
                    placements[0],
                    placements[1],
                    availableHeight,
                    page);

                break;

            case 3:
                BuildThreeGarmentPage(
                    container,
                    placements[0],
                    placements[1],
                    placements[2],
                    availableHeight,
                    page);

                break;

            default:
                BuildFourGarmentPage(
                    container,
                    placements[0],
                    placements[1],
                    placements[2],
                    placements[3],
                    availableHeight,
                    page);

                break;
        }
    }


    private static void BuildSingleGarmentPage(
        IContainer container,
        OrderPageGarmentPlacement garment,
        float availableHeight,
        OrderPageLayout page)
    {
        BuildGarmentBox(
            container,
            garment,
            availableHeight,
            page);
    }


    private static void BuildTwoGarmentPage(
        IContainer container,
        OrderPageGarmentPlacement first,
        OrderPageGarmentPlacement second,
        float availableHeight,
        OrderPageLayout page)
    {
        if (page.UsesPairedFirstPageGarmentLayout)
        {
            BuildGarmentPairRow(
                container,
                first,
                second,
                availableHeight,
                page);

            return;
        }

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
                            garmentHeight,
                            page));

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
                            garmentHeight,
                            page));
            });
    }


    private static void BuildThreeGarmentPage(
        IContainer container,
        OrderPageGarmentPlacement first,
        OrderPageGarmentPlacement second,
        OrderPageGarmentPlacement third,
        float availableHeight,
        OrderPageLayout page)
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
                    garment.DrawingCount);

        var twoDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.DrawingCount == 2)
                .ToList();

        var oneDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.DrawingCount == 1)
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
                availableHeight,
                page);

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
                            rowHeight,
                            page));

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
                                    rowHeight,
                                    page));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    third,
                                    rowHeight,
                                    page));
                    });
            });
    }


    private static void BuildBalancedThreeGarmentPage(
        IContainer container,
        OrderPageGarmentPlacement first,
        OrderPageGarmentPlacement second,
        OrderPageGarmentPlacement third,
        OrderPageGarmentPlacement twoDrawingGarment,
        IReadOnlyList<OrderPageGarmentPlacement> oneDrawingGarments,
        float availableHeight,
        OrderPageLayout page)
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
                                rowHeight,
                                page));

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
                                rowHeight,
                                page));

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
                                rowHeight,
                                page));

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
                                rowHeight,
                                page));

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
                            rowHeight,
                            page));

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
                            rowHeight,
                            page));
            });
    }


    private static void BuildGarmentPairRow(
        IContainer container,
        OrderPageGarmentPlacement left,
        OrderPageGarmentPlacement right,
        float rowHeight,
        OrderPageLayout page)
    {
        container
            .Row(row =>
            {
                row.RelativeItem()
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            left,
                            rowHeight,
                            page));

                row.ConstantItem(
                    GarmentGap);

                row.RelativeItem()
                    .Element(cell =>
                        BuildGarmentBox(
                            cell,
                            right,
                            rowHeight,
                            page));
            });
    }


    private static void BuildFourGarmentPage(
        IContainer container,
        OrderPageGarmentPlacement first,
        OrderPageGarmentPlacement second,
        OrderPageGarmentPlacement third,
        OrderPageGarmentPlacement fourth,
        float availableHeight,
        OrderPageLayout page)
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
                                    rowHeight,
                                    page));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    second,
                                    rowHeight,
                                    page));
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
                                    rowHeight,
                                    page));

                        row.ConstantItem(
                            GarmentGap);

                        row.RelativeItem()
                            .Element(cell =>
                                BuildGarmentBox(
                                    cell,
                                    fourth,
                                    rowHeight,
                                    page));
                    });
            });
    }


    // =========================================================
    // JEDNA POZYCJA ODZIEŻY
    // =========================================================

    private static void BuildGarmentBox(
        IContainer container,
        OrderPageGarmentPlacement placement,
        float totalHeight,
        OrderPageLayout page)
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
                            placement.Garment))
                    .FontSize(
                        PdfStyles.OrderValueFontSize)
                    .Bold();

                column.Item()
                    .Height(
                        drawingHeight)
                    .Element(drawings =>
                        BuildGarmentDrawings(
                            drawings,
                            placement,
                            drawingHeight,
                            page));
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


    private static void ValidateDescriptionLayout(
        IReadOnlyList<OrderPageLayout> pages)
    {
        foreach (var page in pages)
        {
            foreach (var placement in page.Placements)
            {
                var garment = placement.Garment;

                foreach (var view in placement.Views)
                {
                    var drawing = view.Drawing;
                    var geometry = view.Geometry;
                    var description = GarmentViewDescriptionLayout.GetDescription(
                        garment,
                        drawing);

                    if (GarmentViewDescriptionLayout.FitsEditorTargets(
                            description,
                            geometry))
                    {
                        continue;
                    }

                    throw new InvalidOperationException(
                        $"Skróć opis {DrawingLayoutEngine.GetViewName(drawing)} dla pozycji " +
                        $"\"{SafeGarmentName(garment)}\", aby mieścił się " +
                        "w dostępnej przestrzeni pod rysunkiem.");
                }
            }
        }
    }


    // =========================================================
    // RZUTY POJEDYNCZEJ ODZIEŻY
    // =========================================================

    private static void BuildGarmentDrawings(
        IContainer container,
        OrderPageGarmentPlacement placement,
        float availableHeight,
        OrderPageLayout page)
    {
        var rows =
            DrawingLayoutEngine.GetRows(
                placement.Drawings);

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
                                placement,
                                layoutRow,
                                drawingCount,
                                page));
                }
            });
    }


    private static void BuildDrawingRow(
        IContainer container,
        OrderPageGarmentPlacement placement,
        DrawingLayoutRow layoutRow,
        int drawingCount,
        OrderPageLayout page)
    {
        if (layoutRow.Second == null ||
            layoutRow.FirstColumnSpan == 2)
        {
            container
                .Element(cell =>
                    DrawDrawingCell(
                        cell,
                        placement,
                        layoutRow.First,
                        drawingCount,
                        page));

            return;
        }

        container
            .Row(row =>
            {
                row.RelativeItem()
                    .Element(cell =>
                        DrawDrawingCell(
                            cell,
                            placement,
                            layoutRow.First,
                            drawingCount,
                            page));

                row.RelativeItem()
                    .Element(cell =>
                        DrawDrawingCell(
                            cell,
                            placement,
                            layoutRow.Second,
                            drawingCount,
                            page));
            });
    }


    private static void DrawDrawingCell(
        IContainer container,
        OrderPageGarmentPlacement placement,
        DrawingFile drawing,
        int drawingCount,
        OrderPageLayout page)
    {
        var description =
            GarmentViewDescriptionLayout.GetDescription(
                placement.Garment,
                drawing);
        var descriptionTopGap =
            drawingCount >= 3
                ? PdfStyles.MultiDrawingDescriptionTopGap
                : PdfStyles.DrawingDescriptionTopGap;
        var descriptionGeometry =
            GarmentViewDescriptionLayout.GetTargetGeometry(
                page,
                placement,
                drawing);
        var renderedImageHeight =
            (float)GarmentViewDescriptionLayout.GetPdfRenderedImageHeight(
                descriptionGeometry);
        var renderedImageWidth =
            (float)GarmentViewDescriptionLayout.GetPdfRenderedImageWidth(
                descriptionGeometry);
        var descriptionTextWidth =
            GarmentViewDescriptionLayout.GetPdfTextWidth(
                descriptionGeometry);
        var descriptionMeasurement =
            GarmentViewDescriptionLayout.MeasurePdf(
                description,
                descriptionTextWidth,
                GarmentViewDescriptionLayout.GetPdfTextHeight(
                    descriptionGeometry));
        var descriptionFontSize =
            (float)descriptionMeasurement.FontSize;
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
                    .ShrinkVertical()
                    .Column(contentColumn =>
                    {
                        contentColumn.Item()
                            .Height(
                                renderedImageHeight)
                            .AlignTop()
                            .AlignCenter()
                            .Element(imageArea =>
                            {
                                var drawingArea =
                                    imageArea
                                    .Height(
                                        renderedImageHeight)
                                    .Width(
                                        renderedImageWidth)
                                    .AlignTop()
                                    .AlignCenter();

                                drawingArea
                                    .Element(image =>
                                        DrawImage(
                                            image,
                                            drawing,
                                            drawingCount >= 3));
                            });

                        contentColumn.Item()
                            .Height(
                                (float)GarmentViewDescriptionLayout.GetPdfTextHeight(
                                    descriptionGeometry) +
                                descriptionTopGap)
                            .PaddingHorizontal(
                                PdfStyles.DrawingDescriptionHorizontalPadding)
                            .PaddingTop(
                                descriptionTopGap)
                            .AlignTop()
                            .Text(description)
                            .FontSize(
                                descriptionFontSize)
                            .LineHeight(
                                PdfStyles.DrawingDescriptionLineHeight)
                            .AlignLeft();
                    });
            });
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
