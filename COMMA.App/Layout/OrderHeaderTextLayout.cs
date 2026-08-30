using System;
using System.IO;
using SkiaSharp;

namespace COMMA.App.Layout;

public static class OrderHeaderTextLayout
{
    public const float BaseFontSize = 18f;
    public const float PreferredSingleLineMinimumFontSize = 13f;
    public const float MinimumFontSize = 1f;
    public const float LineHeight = 1.05f;
    public const float FontSizeStep = 0.5f;
    public const double PreviewHorizontalInset =
        Services.Pdf.PdfStyles.HeaderIdentityHorizontalPadding * PreviewScale;

    private const double PreviewScale = 620d / 595.28d;
    private const double FitTolerance = 0.1d;

    private static readonly SKTypeface MeasurementTypeface =
        CreateMeasurementTypeface();

    public static HeaderTextGeometry PdfFirstPageNumberGeometry =>
        CreatePdfGeometry(
            Services.Pdf.PdfStyles.FirstPageHeaderOrderNumberWidth,
            Services.Pdf.PdfStyles.HeaderTopRowHeight);

    public static HeaderTextGeometry PdfFirstPageNameGeometry =>
        CreatePdfGeometry(
            GetPdfOrderNameWidth(),
            Services.Pdf.PdfStyles.HeaderTopRowHeight);

    public static HeaderTextGeometry PdfLaterPageNumberGeometry =>
        CreatePdfGeometry(
            Services.Pdf.PdfStyles.FirstPageHeaderOrderNumberWidth,
            Services.Pdf.PdfStyles.HeaderHeight);

    public static HeaderTextGeometry PdfLaterPageNameGeometry =>
        CreatePdfGeometry(
            GetPdfOrderNameWidth(),
            Services.Pdf.PdfStyles.HeaderHeight);

    public static HeaderTextGeometry PreviewFirstPageNumberGeometry =>
        CreatePreviewGeometry(108.3d, 44d);

    public static HeaderTextGeometry PreviewFirstPageNameGeometry =>
        CreatePreviewGeometry(274.7d, 44d);

    public static HeaderTextGeometry PreviewLaterPageNumberGeometry =>
        CreatePreviewGeometry(108.3d, 74d);

    public static HeaderTextGeometry PreviewLaterPageNameGeometry =>
        CreatePreviewGeometry(274.7d, 74d);

    public static HeaderTextFit FitNumber(
        string? value,
        HeaderTextGeometry geometry)
    {
        var text = Normalize(value);

        return FindSingleLineFit(
            text,
            geometry,
            geometry.BaseFontSize,
            geometry.MinimumFontSize);
    }

    public static HeaderTextFit FitName(
        string? value,
        HeaderTextGeometry geometry)
    {
        var text = Normalize(value);
        var singleLine = FindSingleLineFit(
            text,
            geometry,
            geometry.BaseFontSize,
            geometry.PreferredSingleLineMinimumFontSize);

        if (singleLine.Fits)
            return singleLine;

        for (var fontSize = geometry.BaseFontSize;
             fontSize >= geometry.MinimumFontSize - FitTolerance;
             fontSize -= geometry.FontSizeStep)
        {
            var twoLine = FindBestTwoLineFit(
                text,
                geometry,
                fontSize);

            if (twoLine.Fits)
                return twoLine;
        }

        return FindSingleLineFit(
            text,
            geometry,
            geometry.PreferredSingleLineMinimumFontSize - geometry.FontSizeStep,
            geometry.MinimumFontSize);
    }

    private static HeaderTextFit FindSingleLineFit(
        string text,
        HeaderTextGeometry geometry,
        double startFontSize,
        double minimumFontSize)
    {
        HeaderTextFit result = default;

        for (var fontSize = startFontSize;
             fontSize >= minimumFontSize - FitTolerance;
             fontSize -= geometry.FontSizeStep)
        {
            var width = MeasureText(text, fontSize);
            var height = fontSize * LineHeight;
            var fits =
                width <= geometry.AvailableWidth + FitTolerance &&
                height <= geometry.AvailableHeight + FitTolerance;
            result = new HeaderTextFit(
                text,
                fontSize,
                1,
                width,
                height,
                fits);

            if (fits)
                return result;
        }

        return result;
    }

    private static HeaderTextFit FindBestTwoLineFit(
        string text,
        HeaderTextGeometry geometry,
        double fontSize)
    {
        if (text.Length < 2 ||
            fontSize * LineHeight * 2 >
            geometry.AvailableHeight + FitTolerance)
        {
            return default;
        }

        HeaderTextFit best = default;
        var bestWidth = double.MaxValue;
        var bestWhitespacePenalty = int.MaxValue;

        for (var index = 1; index < text.Length; index++)
        {
            var left = text[..index].TrimEnd();
            var right = text[index..].TrimStart();

            if (left.Length == 0 || right.Length == 0)
                continue;

            var leftWidth = MeasureText(left, fontSize);
            var rightWidth = MeasureText(right, fontSize);
            var maximumWidth = Math.Max(leftWidth, rightWidth);

            if (maximumWidth > geometry.AvailableWidth + FitTolerance)
                continue;

            var whitespacePenalty =
                char.IsWhiteSpace(text[index - 1]) ||
                char.IsWhiteSpace(text[index])
                    ? 0
                    : 1;

            if (whitespacePenalty > bestWhitespacePenalty ||
                whitespacePenalty == bestWhitespacePenalty &&
                maximumWidth >= bestWidth)
            {
                continue;
            }

            bestWhitespacePenalty = whitespacePenalty;
            bestWidth = maximumWidth;
            best = new HeaderTextFit(
                left + "\n" + right,
                fontSize,
                2,
                maximumWidth,
                fontSize * LineHeight * 2,
                true);
        }

        return best;
    }

    private static double MeasureText(
        string text,
        double fontSize)
    {
        using var font = new SKFont(
            MeasurementTypeface,
            (float)fontSize);

        return font.MeasureText(text);
    }

    private static HeaderTextGeometry CreatePdfGeometry(
        double fieldWidth,
        double fieldHeight)
    {
        return new HeaderTextGeometry(
            fieldWidth -
            Services.Pdf.PdfStyles.HeaderIdentityHorizontalPadding * 2d,
            fieldHeight -
            Services.Pdf.PdfStyles.HeaderOrderLabelHeight -
            2d,
            BaseFontSize,
            PreferredSingleLineMinimumFontSize,
            MinimumFontSize,
            FontSizeStep);
    }

    private static HeaderTextGeometry CreatePreviewGeometry(
        double fieldWidth,
        double fieldHeight)
    {
        return new HeaderTextGeometry(
            fieldWidth - PreviewHorizontalInset * 2d,
            fieldHeight - 8d - 4d,
            BaseFontSize * PreviewScale,
            PreferredSingleLineMinimumFontSize * PreviewScale,
            MinimumFontSize * PreviewScale,
            FontSizeStep * PreviewScale);
    }

    private static double GetPdfOrderNameWidth()
    {
        return Services.Pdf.PdfStyles.AvailableContentWidth -
               Services.Pdf.PdfStyles.HeaderLogoWidth -
               Services.Pdf.PdfStyles.FirstPageHeaderOrderNumberWidth -
               Services.Pdf.PdfStyles.FirstPageHeaderPageNumberWidth;
    }

    private static SKTypeface CreateMeasurementTypeface()
    {
        var bundledLatoPath = Path.Combine(
            AppContext.BaseDirectory,
            "LatoFont",
            "Lato-Bold.ttf");

        if (File.Exists(bundledLatoPath))
            return SKTypeface.FromFile(bundledLatoPath);

        return SKTypeface.FromFamilyName(
                   "Lato",
                   SKFontStyle.Bold) ??
               SKTypeface.FromFamilyName(
                   null,
                   SKFontStyle.Bold) ??
               SKTypeface.Default;
    }

    private static string Normalize(string? value) =>
        string.Join(
            " ",
            (value ?? string.Empty)
            .Split(
                (char[]?)null,
                StringSplitOptions.RemoveEmptyEntries));
}

public readonly record struct HeaderTextGeometry(
    double AvailableWidth,
    double AvailableHeight,
    double BaseFontSize,
    double PreferredSingleLineMinimumFontSize,
    double MinimumFontSize,
    double FontSizeStep);

public readonly record struct HeaderTextFit(
    string DisplayText,
    double FontSize,
    int LineCount,
    double MaximumLineWidth,
    double TextHeight,
    bool Fits);
