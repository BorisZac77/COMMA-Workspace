using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using COMMA.App.Models;

namespace COMMA.App.Services;

public static class DrawingNormalizer
{
    private const int CanvasSize = 1600;

    private const int DrawingAreaSize = 1400;

    private const int WhiteThreshold = 248;

    private const int AlphaThreshold = 10;


    public static (
        int NormalizedCount,
        int ErrorCount)
        NormalizeProduct(
            IEnumerable<DrawingFile> drawings,
            string outputFolder)
    {
        return NormalizeProductInternal(
            drawings,
            outputFolder,
            onlyNew: false);
    }


    public static (
        int NormalizedCount,
        int ErrorCount)
        NormalizeNewProduct(
            IEnumerable<DrawingFile> drawings,
            string outputFolder)
    {
        return NormalizeProductInternal(
            drawings,
            outputFolder,
            onlyNew: true);
    }


    private static (
        int NormalizedCount,
        int ErrorCount)
        NormalizeProductInternal(
            IEnumerable<DrawingFile> drawings,
            string outputFolder,
            bool onlyNew)
    {
        var validDrawings =
            drawings
                .Where(drawing =>
                    !string.IsNullOrWhiteSpace(
                        drawing.FullPath) &&
                    File.Exists(
                        drawing.FullPath))
                .ToList();


        if (validDrawings.Count == 0)
        {
            return (
                0,
                0);
        }


        Directory.CreateDirectory(
            outputFolder);


        var drawingInfos =
            new List<DrawingInfo>();


        foreach (var drawing
                 in validDrawings)
        {
            using var bitmap =
                SKBitmap.Decode(
                    drawing.FullPath);


            if (bitmap == null)
                continue;


            var bounds =
                FindDrawingBounds(
                    bitmap);


            if (bounds == null)
                continue;


            if (bounds.Value.Width <= 0 ||
                bounds.Value.Height <= 0)
            {
                continue;
            }


            drawingInfos.Add(
                new DrawingInfo(
                    drawing,
                    bounds.Value));
        }


        if (drawingInfos.Count == 0)
        {
            return (
                0,
                validDrawings.Count);
        }


        /*
         * FRONT nadal jest analizowany nawet przy
         * NORMALIZUJ NOWE.
         *
         * Dzięki temu nowy BACK / RIGHT / LEFT
         * dostaje prawidłową wysokość względem
         * oryginalnego FRONTU produktu.
         */
        var frontInfo =
            drawingInfos
                .FirstOrDefault(info =>
                    info.Drawing.IsFront)
            ??
            drawingInfos[0];


        var referenceScaleByWidth =
            (float)DrawingAreaSize /
            frontInfo.Bounds.Width;


        var referenceScaleByHeight =
            (float)DrawingAreaSize /
            frontInfo.Bounds.Height;


        var referenceScale =
            Math.Min(
                referenceScaleByWidth,
                referenceScaleByHeight);


        var referenceHeight =
            frontInfo.Bounds.Height *
            referenceScale;


        var normalizedCount = 0;

        var errorCount = 0;


        foreach (var info
                 in drawingInfos)
        {
            var outputFileName =
                Path.GetFileNameWithoutExtension(
                    info.Drawing.FullPath)
                + ".png";


            var destinationPath =
                Path.Combine(
                    outputFolder,
                    outputFileName);


            /*
             * NORMALIZUJ NOWE:
             *
             * jeśli gotowy plik już istnieje
             * w _normalized, nie przetwarzamy
             * go ponownie.
             */
            if (onlyNew &&
                File.Exists(
                    destinationPath))
            {
                continue;
            }


            var success =
                NormalizeToReferenceHeight(
                    info.Drawing.FullPath,
                    destinationPath,
                    referenceHeight);


            if (success)
            {
                normalizedCount++;
            }
            else
            {
                errorCount++;
            }
        }


        /*
         * Błędy dekodowania/liczenia granic
         * doliczamy tylko dla pełnej normalizacji.
         *
         * Przy NORMALIZUJ NOWE nie chcemy,
         * żeby stary problematyczny plik
         * za każdym razem raportował błąd,
         * jeśli jego wynik już istnieje.
         */
        if (!onlyNew)
        {
            errorCount +=
                validDrawings.Count -
                drawingInfos.Count;
        }


        return (
            normalizedCount,
            errorCount);
    }


    public static bool Normalize(
        string sourcePath,
        string destinationPath)
    {
        try
        {
            using var source =
                SKBitmap.Decode(
                    sourcePath);


            if (source == null)
                return false;


            var bounds =
                FindDrawingBounds(
                    source);


            if (bounds == null)
                return false;


            var drawingBounds =
                bounds.Value;


            if (drawingBounds.Width <= 0 ||
                drawingBounds.Height <= 0)
            {
                return false;
            }


            var scaleByWidth =
                (float)DrawingAreaSize /
                drawingBounds.Width;


            var scaleByHeight =
                (float)DrawingAreaSize /
                drawingBounds.Height;


            var scale =
                Math.Min(
                    scaleByWidth,
                    scaleByHeight);


            return SaveNormalizedImage(
                source,
                drawingBounds,
                destinationPath,
                scale);
        }
        catch
        {
            return false;
        }
    }


    private static bool NormalizeToReferenceHeight(
        string sourcePath,
        string destinationPath,
        float referenceHeight)
    {
        try
        {
            using var source =
                SKBitmap.Decode(
                    sourcePath);


            if (source == null)
                return false;


            var bounds =
                FindDrawingBounds(
                    source);


            if (bounds == null)
                return false;


            var drawingBounds =
                bounds.Value;


            if (drawingBounds.Width <= 0 ||
                drawingBounds.Height <= 0)
            {
                return false;
            }


            /*
             * Najważniejsza zasada:
             *
             * każdy widok tego samego produktu
             * otrzymuje wysokość FRONTU.
             *
             * Jeśli jednak po takim powiększeniu
             * rysunek byłby szerszy niż 1400 px,
             * ograniczamy go szerokością.
             *
             * Dzięki temu:
             *
             * FRONT / BACK / SIDE
             * mają tę samą wysokość wizualną,
             *
             * ale bardzo szerokie produkty
             * nadal mieszczą się na płótnie.
             */


            var scaleByReferenceHeight =
                referenceHeight /
                drawingBounds.Height;


            var scaleByMaximumWidth =
                (float)DrawingAreaSize /
                drawingBounds.Width;


            var scale =
                Math.Min(
                    scaleByReferenceHeight,
                    scaleByMaximumWidth);


            return SaveNormalizedImage(
                source,
                drawingBounds,
                destinationPath,
                scale);
        }
        catch
        {
            return false;
        }
    }


    private static bool SaveNormalizedImage(
        SKBitmap source,
        SKRectI drawingBounds,
        string destinationPath,
        float scale)
    {
        var targetWidth =
            drawingBounds.Width *
            scale;


        var targetHeight =
            drawingBounds.Height *
            scale;


        var left =
            (CanvasSize - targetWidth) /
            2f;


        var top =
            (CanvasSize - targetHeight) /
            2f;


        var sourceRect =
            new SKRect(
                drawingBounds.Left,
                drawingBounds.Top,
                drawingBounds.Right,
                drawingBounds.Bottom);


        var destinationRect =
            new SKRect(
                left,
                top,
                left + targetWidth,
                top + targetHeight);


        using var result =
            new SKBitmap(
                CanvasSize,
                CanvasSize);


        using var canvas =
            new SKCanvas(
                result);


        canvas.Clear(
            SKColors.White);


        using var sourceImage =
            SKImage.FromBitmap(
                source);


        using var paint =
            new SKPaint
            {
                IsAntialias = true
            };


        canvas.DrawImage(
            sourceImage,
            sourceRect,
            destinationRect,
            new SKSamplingOptions(
                SKCubicResampler.Mitchell),
            paint);


        var destinationDirectory =
            Path.GetDirectoryName(
                destinationPath);


        if (!string.IsNullOrWhiteSpace(
                destinationDirectory))
        {
            Directory.CreateDirectory(
                destinationDirectory);
        }


        using var image =
            SKImage.FromBitmap(
                result);


        using var data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);


        using var stream =
            File.Create(
                destinationPath);


        data.SaveTo(
            stream);


        return true;
    }


    private static SKRectI? FindDrawingBounds(
        SKBitmap bitmap)
    {
        var minX =
            bitmap.Width;


        var minY =
            bitmap.Height;


        var maxX =
            -1;


        var maxY =
            -1;


        for (var y = 0;
             y < bitmap.Height;
             y++)
        {
            for (var x = 0;
                 x < bitmap.Width;
                 x++)
            {
                var pixel =
                    bitmap.GetPixel(
                        x,
                        y);


                if (!IsDrawingPixel(
                        pixel))
                {
                    continue;
                }


                if (x < minX)
                    minX = x;


                if (x > maxX)
                    maxX = x;


                if (y < minY)
                    minY = y;


                if (y > maxY)
                    maxY = y;
            }
        }


        if (maxX < minX ||
            maxY < minY)
        {
            return null;
        }


        return new SKRectI(
            minX,
            minY,
            maxX + 1,
            maxY + 1);
    }


    private static bool IsDrawingPixel(
        SKColor pixel)
    {
        if (pixel.Alpha <=
            AlphaThreshold)
        {
            return false;
        }


        return pixel.Red <
                   WhiteThreshold
               ||
               pixel.Green <
                   WhiteThreshold
               ||
               pixel.Blue <
                   WhiteThreshold;
    }


    private sealed class DrawingInfo
    {
        public DrawingInfo(
            DrawingFile drawing,
            SKRectI bounds)
        {
            Drawing =
                drawing;

            Bounds =
                bounds;
        }


        public DrawingFile Drawing
        {
            get;
        }


        public SKRectI Bounds
        {
            get;
        }
    }
}
