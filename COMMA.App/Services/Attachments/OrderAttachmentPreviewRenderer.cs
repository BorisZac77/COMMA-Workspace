using System;
using System.IO;
using System.Runtime.Versioning;
using PDFtoImage;
using SkiaSharp;

namespace COMMA.App.Services.Attachments;

public static class OrderAttachmentPreviewRenderer
{
    private const int PdfPreviewDpi = 120;
    private const int ImagePreviewWidth = 992;
    private const int ImagePreviewHeight = 1403;
    private const float ImagePreviewMargin = 30f;

    public static RenderedAttachmentPage Render(
        Stream content,
        string extension,
        int pageIndex)
    {
        ArgumentNullException.ThrowIfNull(content);

        return string.Equals(
            extension,
            ".pdf",
            StringComparison.OrdinalIgnoreCase)
            ? RenderPdf(content, pageIndex)
            : RenderImage(content);
    }

    private static RenderedAttachmentPage RenderPdf(
        Stream content,
        int pageIndex)
    {
        if (OperatingSystem.IsWindows())
            return RenderPdfOnDesktop(content, pageIndex);

        if (OperatingSystem.IsMacOS())
            return RenderPdfOnDesktop(content, pageIndex);

        if (OperatingSystem.IsLinux())
            return RenderPdfOnDesktop(content, pageIndex);

        throw new PlatformNotSupportedException(
            "Podgląd stron PDF jest obsługiwany w systemach Windows, macOS i Linux.");
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("macos")]
    [SupportedOSPlatform("linux")]
    private static RenderedAttachmentPage RenderPdfOnDesktop(
        Stream content,
        int pageIndex)
    {
        using var rendered = Conversion.ToImage(
            content,
            page: pageIndex,
            options: new RenderOptions(Dpi: PdfPreviewDpi));

        return Encode(rendered);
    }

    private static RenderedAttachmentPage RenderImage(Stream content)
    {
        using var source = SKBitmap.Decode(content);
        if (source == null)
        {
            throw new InvalidDataException(
                "Nie można przygotować podglądu obrazu załącznika.");
        }

        using var page = new SKBitmap(
            ImagePreviewWidth,
            ImagePreviewHeight,
            isOpaque: true);
        using var canvas = new SKCanvas(page);
        canvas.Clear(SKColors.White);

        var availableWidth =
            ImagePreviewWidth - ImagePreviewMargin * 2f;
        var availableHeight =
            ImagePreviewHeight - ImagePreviewMargin * 2f;
        var scale = Math.Min(
            availableWidth / source.Width,
            availableHeight / source.Height);
        var width = source.Width * scale;
        var height = source.Height * scale;
        var destination = SKRect.Create(
            (ImagePreviewWidth - width) / 2f,
            (ImagePreviewHeight - height) / 2f,
            width,
            height);

        using var paint = new SKPaint
        {
            IsAntialias = true
        };
        canvas.DrawBitmap(source, destination, paint);

        return Encode(page);
    }

    private static RenderedAttachmentPage Encode(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(
            SKEncodedImageFormat.Png,
            100);
        return new RenderedAttachmentPage(
            encoded.ToArray(),
            bitmap.Width,
            bitmap.Height);
    }
}

public sealed record RenderedAttachmentPage(
    byte[] PngBytes,
    double Width,
    double Height);
