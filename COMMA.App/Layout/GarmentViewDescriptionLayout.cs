using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using SkiaSharp;

namespace COMMA.App.Layout;

public static class GarmentViewDescriptionLayout
{
    public const double PreviewLargeFontSize = 13;
    public const double PreviewMediumFontSize = 12;
    public const double PreviewMinimumFontSize = 11;

    public const float PdfLargeFontSize = 11f;
    public const float PdfMediumFontSize = 10f;
    public const float PdfMinimumFontSize = 9f;

    public const double PreviewLineHeight = 1.2;

    public const double PreviewBottomSafetyMargin =
        PreviewMinimumFontSize * PreviewLineHeight;

    public const double PdfBottomSafetyMargin =
        PdfMinimumFontSize * PdfStyles.DrawingDescriptionLineHeight;

    public const double MultiDrawingPreviewDescriptionGap = 9;
    public const double MultiDrawingPreviewDescriptionTopMargin = 7;

    private const float PdfDrawingTopGap = 7f;
    private const float GarmentTitleHeight = 21f;
    private const double PreviewScale = 620.0 / PdfStyles.PageWidth;
    private const double FitTolerance = 0.25;
    private const double EditorInputSafetyMargin = 0.25;

    private static readonly SKTypeface MeasurementTypeface =
        SKTypeface.FromFamilyName("Arial") ?? SKTypeface.Default;

    public static DescriptionMeasurement MeasurePreview(
        string? description,
        double availableTextWidth,
        double availableTextHeight)
    {
        return Measure(
            description,
            availableTextWidth,
            availableTextHeight,
            PreviewLineHeight,
            PreviewLargeFontSize,
            PreviewMediumFontSize,
            PreviewMinimumFontSize);
    }

    public static DescriptionMeasurement MeasurePdf(
        string? description,
        double availableTextWidth,
        double availableTextHeight)
    {
        return Measure(
            description,
            availableTextWidth,
            availableTextHeight,
            PdfStyles.DrawingDescriptionLineHeight,
            PdfLargeFontSize,
            PdfMediumFontSize,
            PdfMinimumFontSize);
    }

    public static double GetPreviewFontSize(
        string? description,
        double availableTextWidth,
        double availableTextHeight)
    {
        return MeasurePreview(
                description,
                availableTextWidth,
                availableTextHeight)
            .FontSize;
    }

    public static float GetPdfFontSize(
        string? description,
        double availableTextWidth,
        double availableTextHeight)
    {
        return (float)MeasurePdf(
                description,
                availableTextWidth,
                availableTextHeight)
            .FontSize;
    }

    public static bool FitsEditorTargets(
        string? description,
        int selectedDrawingCount)
    {
        return FitsTargets(
            description,
            GetReferenceGeometry(
                GetLegacyTarget(
                    selectedDrawingCount)),
            0);
    }

    public static bool FitsEditorTargets(
        string? description,
        DescriptionLayoutTarget target)
    {
        return FitsEditorTargets(
            description,
            GetReferenceGeometry(target));
    }

    public static bool FitsEditorTargets(
        string? description,
        DescriptionTargetGeometry geometry)
    {
        return FitsTargets(
            description,
            geometry,
            0);
    }

    public static bool FitsInteractiveEditorTargets(
        string? description,
        int selectedDrawingCount)
    {
        return FitsTargets(
            description,
            GetReferenceGeometry(
                GetLegacyTarget(
                    selectedDrawingCount)),
            EditorInputSafetyMargin);
    }

    public static bool FitsInteractiveEditorTargets(
        string? description,
        DescriptionLayoutTarget target)
    {
        return FitsInteractiveEditorTargets(
            description,
            GetReferenceGeometry(target));
    }

    public static bool FitsInteractiveEditorTargets(
        string? description,
        DescriptionTargetGeometry geometry)
    {
        return FitsTargets(
            description,
            geometry,
            EditorInputSafetyMargin);
    }

    private static bool FitsTargets(
        string? description,
        DescriptionTargetGeometry geometry,
        double additionalSafetyMargin)
    {
        var preview = MeasurePreview(
            description,
            GetPreviewTextWidth(geometry),
            Math.Max(
                0,
                GetPreviewTextHeight(geometry) -
                additionalSafetyMargin));
        var pdf = MeasurePdf(
            description,
            GetPdfTextWidth(geometry),
            Math.Max(
                0,
                GetPdfTextHeight(geometry) -
                additionalSafetyMargin));

        return preview.Fits && pdf.Fits;
    }

    public static string LimitTextChange(
        string? acceptedText,
        string? proposedText,
        int selectedDrawingCount)
    {
        return LimitTextChange(
            acceptedText,
            proposedText,
            selectedDrawingCount,
            FitsEditorTargets);
    }

    public static string LimitInteractiveEditorTextChange(
        string? acceptedText,
        string? proposedText,
        int selectedDrawingCount)
    {
        return LimitTextChange(
            acceptedText,
            proposedText,
            selectedDrawingCount,
            FitsInteractiveEditorTargets);
    }

    public static string LimitInteractiveEditorTextChange(
        string? acceptedText,
        string? proposedText,
        DescriptionTargetGeometry geometry)
    {
        return LimitTextChange(
            acceptedText,
            proposedText,
            geometry,
            FitsInteractiveEditorTargets);
    }

    public static string LimitInteractiveEditorTextChange(
        string? acceptedText,
        string? proposedText,
        DescriptionLayoutTarget target)
    {
        return LimitTextChange(
            acceptedText,
            proposedText,
            target,
            FitsInteractiveEditorTargets);
    }

    private static string LimitTextChange(
        string? acceptedText,
        string? proposedText,
        int selectedDrawingCount,
        Func<string?, int, bool> fits)
    {
        var accepted = NormalizeLineEndings(
            acceptedText);
        var proposed = NormalizeLineEndings(
            proposedText);

        if (string.Equals(
                accepted,
                proposed,
                StringComparison.Ordinal) ||
            fits(
                proposed,
                selectedDrawingCount))
        {
            return proposed;
        }

        var prefixLength = GetCommonPrefixLength(
            accepted,
            proposed);
        var suffixLength = GetCommonSuffixLength(
            accepted,
            proposed,
            prefixLength);
        var proposedInsertionLength =
            proposed.Length - prefixLength - suffixLength;

        // A pure deletion must remain usable even when an older description
        // does not fit. Replacements are still measured because a shorter
        // string can occupy more lines after inserting line breaks.
        if (proposedInsertionLength <= 0)
            return proposed;

        var left = proposed[..prefixLength];
        var insertion = proposed.Substring(
            prefixLength,
            proposedInsertionLength);
        var right = suffixLength == 0
            ? ""
            : proposed[^suffixLength..];
        var acceptedInsertionLength = 0;

        foreach (var insertionEnd in GetTextElementEnds(insertion))
        {
            var candidate =
                left +
                insertion[..insertionEnd] +
                right;

            if (!fits(
                    candidate,
                    selectedDrawingCount))
            {
                break;
            }

            acceptedInsertionLength =
                insertionEnd;
        }

        return left +
               insertion[..acceptedInsertionLength] +
               right;
    }

    private static string LimitTextChange(
        string? acceptedText,
        string? proposedText,
        DescriptionTargetGeometry geometry,
        Func<string?, DescriptionTargetGeometry, bool> fits)
    {
        var accepted = NormalizeLineEndings(acceptedText);
        var proposed = NormalizeLineEndings(proposedText);

        if (string.Equals(accepted, proposed, StringComparison.Ordinal) ||
            fits(proposed, geometry))
        {
            return proposed;
        }

        var prefixLength = GetCommonPrefixLength(accepted, proposed);
        var suffixLength = GetCommonSuffixLength(
            accepted,
            proposed,
            prefixLength);
        var proposedInsertionLength =
            proposed.Length - prefixLength - suffixLength;

        if (proposedInsertionLength <= 0)
            return proposed;

        var left = proposed[..prefixLength];
        var insertion = proposed.Substring(prefixLength, proposedInsertionLength);
        var right = suffixLength == 0 ? "" : proposed[^suffixLength..];
        var acceptedInsertionLength = 0;

        foreach (var insertionEnd in GetTextElementEnds(insertion))
        {
            var candidate = left + insertion[..insertionEnd] + right;

            if (!fits(candidate, geometry))
                break;

            acceptedInsertionLength = insertionEnd;
        }

        return left + insertion[..acceptedInsertionLength] + right;
    }

    private static string LimitTextChange(
        string? acceptedText,
        string? proposedText,
        DescriptionLayoutTarget target,
        Func<string?, DescriptionLayoutTarget, bool> fits)
    {
        var accepted = NormalizeLineEndings(
            acceptedText);
        var proposed = NormalizeLineEndings(
            proposedText);

        if (string.Equals(
                accepted,
                proposed,
                StringComparison.Ordinal) ||
            fits(
                proposed,
                target))
        {
            return proposed;
        }

        var prefixLength = GetCommonPrefixLength(
            accepted,
            proposed);
        var suffixLength = GetCommonSuffixLength(
            accepted,
            proposed,
            prefixLength);
        var proposedInsertionLength =
            proposed.Length - prefixLength - suffixLength;

        if (proposedInsertionLength <= 0)
            return proposed;

        var left = proposed[..prefixLength];
        var insertion = proposed.Substring(
            prefixLength,
            proposedInsertionLength);
        var right = suffixLength == 0
            ? ""
            : proposed[^suffixLength..];
        var acceptedInsertionLength = 0;

        foreach (var insertionEnd in GetTextElementEnds(insertion))
        {
            var candidate =
                left +
                insertion[..insertionEnd] +
                right;

            if (!fits(
                    candidate,
                    target))
            {
                break;
            }

            acceptedInsertionLength =
                insertionEnd;
        }

        return left +
               insertion[..acceptedInsertionLength] +
               right;
    }

    public static double GetReferencePreviewTextWidth(
        DescriptionLayoutKind layout)
    {
        return GetReferencePdfTextWidth(layout) *
               PreviewScale;
    }

    public static double GetReferencePreviewTextWidth(
        DescriptionLayoutTarget target)
    {
        return GetPreviewTextWidth(
            GetReferenceGeometry(target));
    }

    public static double GetReferencePreviewTextHeight(
        DescriptionLayoutKind layout)
    {
        return GetReferencePreviewTextHeight(
            layout == DescriptionLayoutKind.FourViews
                ? DescriptionLayoutTarget.LaterPageFourViews
                : DescriptionLayoutTarget.FirstPageTwoViews);
    }

    public static double GetReferencePreviewTextHeight(
        DescriptionLayoutTarget target)
    {
        return GetPreviewTextHeight(
            GetReferenceGeometry(target));
    }

    public static double GetPreviewTextWidth(
        DescriptionTargetGeometry geometry)
    {
        return GetPdfTextWidth(geometry) *
               PreviewScale;
    }

    public static double GetPreviewTextHeight(
        DescriptionTargetGeometry geometry)
    {
        var height =
            GetPdfTextHeightBeforeSafety(geometry) *
            PreviewScale;

        if (GetLayoutKind(geometry.Target) == DescriptionLayoutKind.FourViews)
        {
            height -=
                MultiDrawingPreviewDescriptionGap -
                PdfStyles.MultiDrawingDescriptionTopGap *
                PreviewScale;
        }

        return Math.Max(
            0,
            height - PreviewBottomSafetyMargin);
    }

    public static double GetReferencePdfTextWidth(
        DescriptionLayoutKind layout)
    {
        _ = layout;

        return PdfStyles.AvailableContentWidth / 2d -
               PdfStyles.DrawingCellPadding * 2d -
               PdfStyles.DrawingDescriptionHorizontalPadding * 2d;
    }

    public static double GetReferencePdfTextWidth(
        DescriptionLayoutTarget target)
    {
        return GetPdfTextWidth(
            GetReferenceGeometry(target));
    }

    public static double GetReferencePdfTextHeight(
        DescriptionLayoutKind layout)
    {
        return GetReferencePdfTextHeight(
            layout == DescriptionLayoutKind.FourViews
                ? DescriptionLayoutTarget.LaterPageFourViews
                : DescriptionLayoutTarget.FirstPageTwoViews);
    }

    public static double GetReferencePdfTextHeight(
        DescriptionLayoutTarget target)
    {
        return GetPdfTextHeight(
            GetReferenceGeometry(target));
    }

    public static double GetPdfTextWidth(
        DescriptionTargetGeometry geometry)
    {
        return Math.Max(
            1,
            geometry.PdfDrawingCellWidth -
            PdfStyles.DrawingCellPadding * 2d -
            PdfStyles.DrawingDescriptionHorizontalPadding * 2d);
    }

    public static double GetPdfTextHeight(
        DescriptionTargetGeometry geometry)
    {
        return Math.Max(
            0,
            GetPdfTextHeightBeforeSafety(geometry) -
            PdfBottomSafetyMargin);
    }

    private static double GetPdfTextHeightBeforeSafety(
        DescriptionTargetGeometry geometry)
    {
        var layout = GetLayoutKind(geometry.Target);
        var cellHeight = geometry.PdfDrawingCellHeight;
        var maximumImageHeight =
            layout == DescriptionLayoutKind.TwoViews
                ? GetTwoViewMaximumImageHeight()
                : PdfStyles.MultiDrawingMaximumHeight;
        var descriptionTopGap =
            layout == DescriptionLayoutKind.TwoViews
                ? PdfStyles.DrawingDescriptionTopGap
                : PdfStyles.MultiDrawingDescriptionTopGap;

        return Math.Max(
            0,
            cellHeight -
            PdfStyles.DrawingTitleHeight -
            PdfDrawingTopGap -
            PdfStyles.DrawingCellPadding * 2d -
            maximumImageHeight -
            descriptionTopGap);
    }

    public static DescriptionLayoutKind GetLayoutKind(
        int selectedDrawingCount)
    {
        return selectedDrawingCount >= 3
            ? DescriptionLayoutKind.FourViews
            : DescriptionLayoutKind.TwoViews;
    }

    public static DescriptionLayoutKind GetLayoutKind(
        DescriptionLayoutTarget target)
    {
        return target is DescriptionLayoutTarget.LaterPageThreeViews or
            DescriptionLayoutTarget.LaterPageFourViews
            ? DescriptionLayoutKind.FourViews
            : DescriptionLayoutKind.TwoViews;
    }

    public static DescriptionLayoutTarget GetTarget(
        bool isFirstPage,
        int selectedDrawingCount)
    {
        if (isFirstPage && selectedDrawingCount > 2)
            throw new ArgumentOutOfRangeException(
                nameof(selectedDrawingCount),
                "Pierwsza strona może zawierać maksymalnie dwa rzuty.");

        return (isFirstPage, selectedDrawingCount) switch
        {
            (true, <= 1) => DescriptionLayoutTarget.FirstPageOneView,
            (true, _) => DescriptionLayoutTarget.FirstPageTwoViews,
            (false, <= 1) => DescriptionLayoutTarget.LaterPageOneView,
            (false, 2) => DescriptionLayoutTarget.LaterPageTwoViews,
            (false, 3) => DescriptionLayoutTarget.LaterPageThreeViews,
            _ => DescriptionLayoutTarget.LaterPageFourViews
        };
    }

    public static bool IsFirstPage(
        DescriptionLayoutTarget target)
    {
        return target is DescriptionLayoutTarget.FirstPageOneView or
            DescriptionLayoutTarget.FirstPageTwoViews;
    }

    public static double GetPageGarmentAreaHeight(
        bool isFirstPage)
    {
        return isFirstPage
            ? PdfStyles.AvailableContentHeight -
              PdfStyles.HeaderHeight -
              PdfStyles.HandwrittenSectionHeight -
              PdfStyles.SectionGap * 2d -
              PdfStyles.PageSafetyReserve
            : PdfStyles.AvailableContentHeight -
              PdfStyles.HeaderHeight -
              PdfStyles.SectionGap -
              PdfStyles.PageSafetyReserve;
    }

    public static double GetDrawingCellHeight(
        DescriptionLayoutTarget target)
    {
        var isFirstPage = IsFirstPage(target);
        var drawingAreaHeight =
            GetPageGarmentAreaHeight(isFirstPage) -
            GarmentTitleHeight;
        var rowCount =
            GetLayoutKind(target) == DescriptionLayoutKind.FourViews
                ? 2d
                : 1d;

        return drawingAreaHeight /
               rowCount;
    }

    public static DescriptionTargetGeometry GetReferenceGeometry(
        DescriptionLayoutTarget target)
    {
        var fullWidth = target is DescriptionLayoutTarget.FirstPageOneView or
            DescriptionLayoutTarget.LaterPageOneView;

        return new DescriptionTargetGeometry(
            target,
            fullWidth
                ? PdfStyles.AvailableContentWidth
                : PdfStyles.AvailableContentWidth / 2d,
            GetDrawingCellHeight(target));
    }

    public static DescriptionTargetGeometry GetTargetGeometry(
        OrderPageLayout page,
        OrderGarmentItem garment)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(garment);

        var placement = page.Placements.FirstOrDefault(candidate =>
            ReferenceEquals(candidate.Garment, garment));

        if (placement == null)
            throw new ArgumentException("Pozycja nie należy do wskazanej strony.", nameof(garment));

        return GetTargetGeometry(page, placement, placement.Drawings[0]);
    }

    public static DescriptionTargetGeometry GetTargetGeometry(
        OrderPageLayout page,
        OrderPageGarmentPlacement placement,
        DrawingFile drawing)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(drawing);

        if (!placement.Drawings.Contains(drawing))
            throw new ArgumentException("Rzut nie należy do wskazanego rozmieszczenia.", nameof(drawing));

        var availableHeight =
            GetPageGarmentAreaHeight(page.IsFirstPage);
        var garmentHeight = availableHeight;
        var garmentWidth = (double)PdfStyles.AvailableContentWidth;
        var placements = page.Placements;

        if (placements.Count == 2)
        {
            garmentHeight =
                (availableHeight - 4d) / 2d;
        }
        else if (placements.Count >= 3)
        {
            garmentHeight =
                (availableHeight - 4d) / 2d;

            if (placements.Count >= 4)
            {
                garmentWidth =
                    (PdfStyles.AvailableContentWidth - 4d) / 2d;
            }
            else
            {
                var twoDrawingPlacement = placements.SingleOrDefault(item =>
                    item.DrawingCount == 2);
                var isBalanced =
                    placements.Sum(item => item.DrawingCount) == 4 &&
                    twoDrawingPlacement != null &&
                    placements.Count(item => item.DrawingCount == 1) == 2;
                var usesFullWidth = isBalanced
                    ? ReferenceEquals(placement, twoDrawingPlacement)
                    : ReferenceEquals(placement, placements[0]);

                if (!usesFullWidth)
                {
                    garmentWidth =
                        (PdfStyles.AvailableContentWidth - 4d) / 2d;
                }
            }
        }

        var target = GetTarget(
            page.IsFirstPage,
            placement.DrawingCount);
        var rows = DrawingLayoutEngine.GetRows(placement.Drawings);
        var rowCount = Math.Max(1, rows.Count);
        var cellHeight =
            Math.Max(1, garmentHeight - GarmentTitleHeight) /
            rowCount;
        var row = rows.First(item =>
            ReferenceEquals(item.First, drawing) ||
            ReferenceEquals(item.Second, drawing));
        var cellWidth =
            ReferenceEquals(row.First, drawing) && row.FirstColumnSpan == 2
                ? garmentWidth
                : garmentWidth / 2d;

        return new DescriptionTargetGeometry(
            target,
            cellWidth,
            cellHeight);
    }

    public static string GetDescription(
        OrderGarmentItem garment,
        DrawingFile drawing)
    {
        ArgumentNullException.ThrowIfNull(garment);
        ArgumentNullException.ThrowIfNull(drawing);

        var description = drawing.IsFront
            ? garment.ViewDescriptions.Front
            : drawing.IsBack
                ? garment.ViewDescriptions.Back
                : drawing.IsRight
                    ? garment.ViewDescriptions.Right
                    : drawing.IsLeft
                        ? garment.ViewDescriptions.Left
                        : "";

        return NormalizeLineEndings(description).Trim();
    }

    private static DescriptionMeasurement Measure(
        string? description,
        double availableTextWidth,
        double availableTextHeight,
        double lineHeight,
        double largeFontSize,
        double mediumFontSize,
        double minimumFontSize)
    {
        var text = NormalizeLineEndings(description);

        if (string.IsNullOrEmpty(text))
        {
            return new DescriptionMeasurement(
                true,
                largeFontSize,
                0,
                0);
        }

        DescriptionMeasurement? minimumResult =
            null;

        foreach (var fontSize in new[]
                 {
                     largeFontSize,
                     mediumFontSize,
                     minimumFontSize
                 })
        {
            var lineCount = CountWrappedLines(
                text,
                Math.Max(1, availableTextWidth),
                fontSize);
            var textHeight =
                lineCount *
                fontSize *
                lineHeight;
            var result = new DescriptionMeasurement(
                textHeight <= availableTextHeight + FitTolerance,
                fontSize,
                lineCount,
                textHeight);

            minimumResult = result;

            if (result.Fits)
                return result;
        }

        return minimumResult!.Value;
    }

    private static int CountWrappedLines(
        string text,
        double availableTextWidth,
        double fontSize)
    {
        using var font = new SKFont(
            MeasurementTypeface,
            (float)fontSize);
        var lineCount = 0;

        foreach (var paragraph in text.Split('\n'))
        {
            lineCount += CountParagraphLines(
                paragraph,
                availableTextWidth,
                font);
        }

        return lineCount;
    }

    private static int CountParagraphLines(
        string paragraph,
        double availableTextWidth,
        SKFont font)
    {
        if (paragraph.Length == 0)
            return 1;

        var starts = StringInfo.ParseCombiningCharacters(
            paragraph);
        var lineCount = 0;
        var elementIndex = 0;

        while (elementIndex < starts.Length)
        {
            var lineStart = elementIndex;
            var lastWhitespaceBreak = -1;
            var lastFittingElement = lineStart;

            while (elementIndex < starts.Length)
            {
                var elementEnd = elementIndex + 1 < starts.Length
                    ? starts[elementIndex + 1]
                    : paragraph.Length;
                var candidate = paragraph.Substring(
                    starts[lineStart],
                    elementEnd - starts[lineStart]);

                if (font.MeasureText(candidate) <= availableTextWidth)
                {
                    lastFittingElement = elementIndex + 1;

                    if (char.IsWhiteSpace(
                            paragraph[starts[elementIndex]]))
                    {
                        lastWhitespaceBreak =
                            elementIndex + 1;
                    }

                    elementIndex++;
                    continue;
                }

                break;
            }

            lineCount++;

            if (elementIndex >= starts.Length)
                break;

            if (lastWhitespaceBreak > lineStart)
            {
                elementIndex =
                    lastWhitespaceBreak;

                continue;
            }

            elementIndex = Math.Max(
                lineStart + 1,
                lastFittingElement);
        }

        return lineCount;
    }

    private static double GetTwoViewMaximumImageHeight()
    {
        return PdfStyles.GetDrawingImageHeight(
                   PdfStyles.GetDrawingRowHeight(1)) *
               0.75;
    }

    private static int GetCommonPrefixLength(
        string first,
        string second)
    {
        var length = Math.Min(
            first.Length,
            second.Length);
        var index = 0;

        while (index < length &&
               first[index] == second[index])
        {
            index++;
        }

        return index;
    }

    private static int GetCommonSuffixLength(
        string first,
        string second,
        int prefixLength)
    {
        var maximum = Math.Min(
            first.Length - prefixLength,
            second.Length - prefixLength);
        var length = 0;

        while (length < maximum &&
               first[^(length + 1)] == second[^(length + 1)])
        {
            length++;
        }

        return length;
    }

    private static IEnumerable<int> GetTextElementEnds(
        string value)
    {
        var starts = StringInfo.ParseCombiningCharacters(
            value);

        for (var index = 0; index < starts.Length; index++)
        {
            yield return index + 1 < starts.Length
                ? starts[index + 1]
                : value.Length;
        }
    }

    private static string NormalizeLineEndings(
        string? value)
    {
        return (value ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static DescriptionLayoutTarget GetLegacyTarget(
        int selectedDrawingCount)
    {
        return selectedDrawingCount >= 3
            ? DescriptionLayoutTarget.LaterPageFourViews
            : DescriptionLayoutTarget.FirstPageTwoViews;
    }
}

public enum DescriptionLayoutKind
{
    TwoViews,
    FourViews
}

public enum DescriptionLayoutTarget
{
    FirstPageOneView,
    FirstPageTwoViews,
    LaterPageOneView,
    LaterPageTwoViews,
    LaterPageThreeViews,
    LaterPageFourViews
}

public readonly record struct DescriptionTargetGeometry(
    DescriptionLayoutTarget Target,
    double PdfDrawingCellWidth,
    double PdfDrawingCellHeight);

public enum GarmentViewKind
{
    Front,
    Back,
    Right,
    Left
}

public readonly record struct GarmentViewSelection(
    bool Front,
    bool Back,
    bool Right,
    bool Left)
{
    public int Count =>
        (Front ? 1 : 0) +
        (Back ? 1 : 0) +
        (Right ? 1 : 0) +
        (Left ? 1 : 0);
}

public readonly record struct GarmentViewDescriptionGeometrySet(
    DescriptionTargetGeometry Front,
    DescriptionTargetGeometry Back,
    DescriptionTargetGeometry Right,
    DescriptionTargetGeometry Left)
{
    public DescriptionTargetGeometry Get(GarmentViewKind view) =>
        view switch
        {
            GarmentViewKind.Front => Front,
            GarmentViewKind.Back => Back,
            GarmentViewKind.Right => Right,
            _ => Left
        };
}

public readonly record struct DescriptionMeasurement(
    bool Fits,
    double FontSize,
    int LineCount,
    double TextHeight);
