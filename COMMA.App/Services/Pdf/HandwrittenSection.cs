using System;
using System.Linq;
using COMMA.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services.Pdf;

public static class HandwrittenSection
{
    public static void Build(
        ColumnDescriptor column,
        ProductionCard card)
    {
        column.Item()
            .Height(PdfStyles.HandwrittenSectionHeight)
            .Column(section =>
            {
                BuildLoggingTitle(
                    section);

                BuildLoggingEntries(
                    section,
                    card);

                BuildColoursTitle(
                    section);

                BuildColourEntries(
                    section,
                    card);
            });
    }

    private static void BuildLoggingTitle(
        ColumnDescriptor column)
    {
        column.Item()
            .Height(PdfStyles.LoggingTitleHeight)
            .Border(PdfStyles.StandardBorderWidth)
            .AlignCenter()
            .AlignMiddle()
            .Text("LOGOWANIE (NAZWA WZORU / WYMIAR)")
            .FontSize(PdfStyles.LoggingTitleFontSize)
            .ExtraBold();
    }

    private static void BuildLoggingEntries(
        ColumnDescriptor column,
        ProductionCard card)
    {
        column.Item()
            .Height(PdfStyles.LoggingEntriesHeight)
            .Row(row =>
            {
                for (
                    var index = 0;
                    index < PdfStyles.MaximumProductionEntryCount;
                    index++)
                {
                    var entry =
                        index < card.ProductionEntries.Count
                            ? card.ProductionEntries[index]
                            : null;

                    var number =
                        index + 1;

                    row.RelativeItem()
                        .Border(
                            PdfStyles.StandardBorderWidth)
                        .Padding(
                            PdfStyles.LoggingCellPadding)
                        .Element(container =>
                            BuildLoggingCell(
                                container,
                                entry,
                                number));
                }
            });
    }

    private static void BuildLoggingCell(
        IContainer container,
        ProductionEntry? entry,
        int number)
    {
        if (entry is null ||
            !entry.HasContent)
        {
            return;
        }

        var logoName =
            Safe(entry.LogoName);

        var dimension =
            Safe(entry.Dimension);

        container
            .Column(column =>
            {
                column.Item()
                    .Height(PdfStyles.LoggingTopLineHeight)
                    .Row(row =>
                    {
                        row.ConstantItem(
                                PdfStyles.LoggingNumberAreaWidth)
                            .PaddingRight(3)
                            .AlignMiddle()
                            .Element(numberContainer =>
                                BuildLoggingNumber(
                                    numberContainer,
                                    number));

                        row.RelativeItem()
                            .AlignLeft()
                            .AlignMiddle()
                            .ScaleToFit()
                            .Text(logoName)
                            .FontSize(
                                GetLogoNameFontSize(
                                    logoName))
                            .ExtraBold();
                    });

                column.Item()
                    .Height(PdfStyles.LoggingDimensionHeight)
                    .PaddingLeft(
                        PdfStyles.LoggingNumberAreaWidth)
                    .AlignLeft()
                    .AlignMiddle()
                    .ScaleToFit()
                    .Text(dimension)
                    .FontSize(
                        PdfStyles.LoggingDimensionFontSize)
                    .Bold();
            });
    }

    private static void BuildLoggingNumber(
        IContainer container,
        int number)
    {
        var circleSize =
            PdfStyles.LoggingNumberCircleSize;

        container
            .Width(circleSize)
            .Height(circleSize)
            .Border(PdfStyles.LineWidth)
            .CornerRadius(circleSize / 2f)
            .AlignCenter()
            .AlignMiddle()
            .Text(number.ToString())
            .FontSize(
                PdfStyles.LoggingNumberFontSize)
            .Bold();
    }

    private static void BuildColoursTitle(
        ColumnDescriptor column)
    {
        column.Item()
            .Height(PdfStyles.ColoursTitleHeight)
            .Border(PdfStyles.StandardBorderWidth)
            .AlignCenter()
            .AlignMiddle()
            .Text("KOLORYSTYKA (KOLOR / NICI)")
            .FontSize(PdfStyles.ColoursTitleFontSize)
            .ExtraBold();
    }

    private static void BuildColourEntries(
        ColumnDescriptor column,
        ProductionCard card)
    {
        column.Item()
            .Height(PdfStyles.ColoursAreaHeight)
            .Row(row =>
            {
                for (
                    var index = 0;
                    index < PdfStyles.MaximumProductionEntryCount;
                    index++)
                {
                    var entry =
                        index < card.ProductionEntries.Count
                            ? card.ProductionEntries[index]
                            : null;

                    row.RelativeItem()
                        .Border(
                            PdfStyles.StandardBorderWidth)
                        .Padding(
                            PdfStyles.ColoursCellPadding)
                        .Element(container =>
                            BuildColourColumn(
                                container,
                                entry));
                }
            });
    }

    private static void BuildColourColumn(
        IContainer container,
        ProductionEntry? entry)
    {
        if (entry is null)
            return;

        var colours =
            entry.Colours
                .Where(colour =>
                    !string.IsNullOrWhiteSpace(
                        colour.Value))
                .ToList();

        if (colours.Count == 0)
            return;

        var availableHeight =
            PdfStyles.ColoursAreaHeight
            - PdfStyles.ColoursCellPadding * 2;

        var compactRowHeight =
            PdfStyles.ColourCompactRowHeight;

        var rowHeight =
            Math.Min(
                compactRowHeight,
                availableHeight / colours.Count);

        container
            .AlignTop()
            .Column(column =>
            {
                foreach (var colour in colours)
                {
                    var value =
                        Safe(colour.Value);

                    var fontSize =
                        GetColourFontSize(
                            colours.Count,
                            value);

                    column.Item()
                        .Height(rowHeight)
                        .Row(row =>
                        {
                            row.ConstantItem(
                                    PdfStyles.ColourNumberWidth)
                                .AlignLeft()
                                .AlignMiddle()
                                .ScaleToFit()
                                .Text(
                                    $"{colour.Number}.")
                                .FontSize(fontSize)
                                .Bold();

                            row.RelativeItem()
                                .PaddingLeft(2)
                                .AlignLeft()
                                .AlignMiddle()
                                .ScaleToFit()
                                .Text(value)
                                .FontSize(fontSize);
                        });
                }
            });
    }

    private static float GetLogoNameFontSize(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 11f;

        var length =
            value.Length;

        const float maximumFontSize = 11f;
        const float minimumFontSize = 5f;
        const float availableTextWidth = 108f;
        const float averageCharacterWidthFactor = 0.62f;

        var calculatedFontSize =
            availableTextWidth /
            (length * averageCharacterWidthFactor);

        if (calculatedFontSize > maximumFontSize)
            return maximumFontSize;

        if (calculatedFontSize < minimumFontSize)
            return minimumFontSize;

        return calculatedFontSize;
    }

    private static float GetColourFontSize(
        int colourCount,
        string value)
    {
        var fontSize =
            colourCount switch
            {
                <= 10 => 8.5f,
                <= 12 => 8f,
                <= 15 => 7.5f,
                <= 18 => 7f,
                <= 22 => 6.5f,
                <= 26 => 6f,
                <= 32 => 5.5f,
                _ => 5f
            };

        if (value.Length > 22)
            fontSize -= 0.5f;

        if (value.Length > 30)
            fontSize -= 0.5f;

        return Math.Max(
            fontSize,
            4f);
    }

    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }
}