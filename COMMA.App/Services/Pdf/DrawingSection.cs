using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMMA.App.Layout;
using COMMA.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace COMMA.App.Services.Pdf;

public static class DrawingSection
{
    private const byte WhiteThreshold = 248;

    public static void Build(
        ColumnDescriptor column,
        IReadOnlyList<DrawingLayoutRow> rows)
    {
        if (rows.Count == 0)
        {
            BuildEmptySection(column);
            return;
        }

        var rowHeight =
            PdfStyles.GetDrawingRowHeight(rows.Count);

        int drawingCount =
            rows.Sum(row =>
                row.Second == null ? 1 : 2);

        var imageScale =
            drawingCount >= 3
                ? 1.20f
                : 1f;

        column.Item()
            .Height(PdfStyles.DrawingSectionHeight)
            .Column(drawingColumn =>
            {
                foreach (var layoutRow in rows)
                {
                    drawingColumn.Item()
                        .Height(rowHeight)
                        .Element(container =>
                            BuildDrawingRow(
                                container,
                                layoutRow,
                                rowHeight,
                                imageScale));
                }
            });
    }

    private static void BuildEmptySection(
        ColumnDescriptor column)
    {
        column.Item()
            .Height(PdfStyles.DrawingSectionHeight)
            .Border(PdfStyles.StandardBorderWidth)
            .AlignCenter()
            .AlignMiddle()
            .Text("WYBIERZ CO NAJMNIEJ JEDEN RZUT")
            .FontSize(PdfStyles.FieldValueFontSize)
            .Bold();
    }

    private static void BuildDrawingRow(
        IContainer container,
        DrawingLayoutRow layoutRow,
        float rowHeight,
        float imageScale)
    {
        if (layoutRow.Second == null ||
            layoutRow.FirstColumnSpan == 2)
        {
            container.Element(cell =>
                DrawDrawingCell(
                    cell,
                    layoutRow.First,
                    rowHeight,
                    imageScale));

            return;
        }

        container.Row(row =>
        {
            row.RelativeItem()
                .Element(cell =>
                    DrawDrawingCell(
                        cell,
                        layoutRow.First,
                        rowHeight,
                        imageScale));

            row.RelativeItem()
                .Element(cell =>
                    DrawDrawingCell(
                        cell,
                        layoutRow.Second,
                        rowHeight,
                        imageScale));
        });
    }

    private static void DrawDrawingCell(
        IContainer container,
        DrawingFile drawing,
        float cellHeight,
        float imageScale)
    {
        var imageHeight =
            PdfStyles.GetDrawingImageHeight(cellHeight);

        var scaledImageHeight =
            imageHeight * imageScale;

        container
            .Border(PdfStyles.StandardBorderWidth)
            .Padding(PdfStyles.DrawingCellPadding)
            .Column(column =>
            {
                column.Item()
                    .Height(PdfStyles.DrawingTitleHeight)
                    .AlignCenter()
                    .AlignMiddle()
                    .Text(GetDrawingTitle(drawing))
                    .FontSize(PdfStyles.DrawingTitleFontSize)
                    .Bold();

                column.Item()
                    .Height(scaledImageHeight)
                    .Padding(PdfStyles.DrawingImagePadding)
                    .AlignTop()
                    .AlignCenter()
                    .Element(imageContainer =>
                    {
                        imageContainer
                            .MaxWidth(
                                scaledImageHeight * 0.75f)
                            .MaxHeight(
                                scaledImageHeight * 0.75f)
                            .AlignCenter()
                            .AlignTop()
                            .Element(img =>
                            {
                                DrawImage(
                                    img,
                                    drawing);
                            });
                    });
            });
    }

    private static void DrawImage(
        IContainer container,
        DrawingFile drawing)
    {
        if (string.IsNullOrWhiteSpace(drawing.FullPath) ||
            !File.Exists(drawing.FullPath))
        {
            container
                .AlignCenter()
                .AlignMiddle()
                .Text("BRAK RYSUNKU")
                .FontSize(PdfStyles.DefaultFontSize)
                .Bold();

            return;
        }

        var cleanedImage =
            PrepareImageForPdf(
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
                .Image(cleanedImage)
                .FitArea();

            return;
        }

        container
            .Image(cleanedImage)
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
                .Image(drawing.FullPath)
                .FitArea();

            return;
        }

        container
            .Image(drawing.FullPath)
            .FitArea();
    }

    private static byte[] PrepareImageForPdf(
        string filePath)
    {
        try
        {
            using var sourceBitmap =
                SKBitmap.Decode(filePath);

            if (sourceBitmap == null)
                return [];

            using var cleanedBitmap =
                new SKBitmap(
                    sourceBitmap.Width,
                    sourceBitmap.Height,
                    SKColorType.Rgba8888,
                    SKAlphaType.Opaque);

            for (var y = 0; y < sourceBitmap.Height; y++)
            {
                for (var x = 0; x < sourceBitmap.Width; x++)
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

            return data?.ToArray() ??
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
            (result + 127) / 255);
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