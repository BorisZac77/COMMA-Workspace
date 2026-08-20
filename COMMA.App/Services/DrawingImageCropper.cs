using System;
using SkiaSharp;

namespace COMMA.App.Services;

/// <summary>
/// Przygotowuje nietrwałą, przyciętą wersję rysunku do renderowania.
/// Nie zapisuje ani nie modyfikuje pliku źródłowego.
/// </summary>
public static class DrawingImageCropper
{
    private const byte WhiteThreshold = 248;
    private const int SafetyMargin = 4;

    public static byte[] TryCreateCroppedPng(string filePath)
    {
        try
        {
            using var sourceBitmap = SKBitmap.Decode(filePath);

            if (sourceBitmap == null)
                return [];

            var contentBounds = FindContentBounds(sourceBitmap);

            if (contentBounds == null)
                return [];

            var cropBounds = AddSafetyMargin(
                contentBounds.Value,
                sourceBitmap.Width,
                sourceBitmap.Height);

            using var croppedBitmap = new SKBitmap(
                cropBounds.Width,
                cropBounds.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Opaque);

            for (var y = 0; y < cropBounds.Height; y++)
            {
                for (var x = 0; x < cropBounds.Width; x++)
                {
                    var sourceColor = sourceBitmap.GetPixel(
                        cropBounds.Left + x,
                        cropBounds.Top + y);

                    croppedBitmap.SetPixel(
                        x,
                        y,
                        CompositeAgainstWhite(sourceColor));
                }
            }

            using var image = SKImage.FromBitmap(croppedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);

            return data?.ToArray() ?? [];
        }
        catch
        {
            return [];
        }
    }

    private static SKRectI? FindContentBounds(SKBitmap bitmap)
    {
        var minX = bitmap.Width;
        var minY = bitmap.Height;
        var maxX = -1;
        var maxY = -1;

        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                if (!IsContentPixel(bitmap.GetPixel(x, y)))
                    continue;

                minX = Math.Min(minX, x);
                minY = Math.Min(minY, y);
                maxX = Math.Max(maxX, x);
                maxY = Math.Max(maxY, y);
            }
        }

        return maxX < minX || maxY < minY
            ? null
            : new SKRectI(minX, minY, maxX + 1, maxY + 1);
    }

    private static SKRectI AddSafetyMargin(
        SKRectI bounds,
        int imageWidth,
        int imageHeight)
    {
        return new SKRectI(
            Math.Max(0, bounds.Left - SafetyMargin),
            Math.Max(0, bounds.Top - SafetyMargin),
            Math.Min(imageWidth, bounds.Right + SafetyMargin),
            Math.Min(imageHeight, bounds.Bottom + SafetyMargin));
    }

    private static bool IsContentPixel(SKColor colour)
    {
        var compositedColour = CompositeAgainstWhite(colour);

        return compositedColour.Red < WhiteThreshold ||
               compositedColour.Green < WhiteThreshold ||
               compositedColour.Blue < WhiteThreshold;
    }

    private static SKColor CompositeAgainstWhite(SKColor colour)
    {
        return new SKColor(
            CompositeChannelAgainstWhite(colour.Red, colour.Alpha),
            CompositeChannelAgainstWhite(colour.Green, colour.Alpha),
            CompositeChannelAgainstWhite(colour.Blue, colour.Alpha),
            255);
    }

    private static byte CompositeChannelAgainstWhite(byte colour, byte alpha)
    {
        var result = colour * alpha + 255 * (255 - alpha);

        return (byte)((result + 127) / 255);
    }
}
