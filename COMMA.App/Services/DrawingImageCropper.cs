using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
    private const long MaximumCacheBytes = 64L * 1024 * 1024;
    private const int MaximumCacheEntries = 128;

    private static readonly object CacheLock = new();
    private static readonly Dictionary<CacheKey, CacheEntry> Cache = new();
    private static readonly LinkedList<CacheKey> LeastRecentlyUsed = new();

    private static long cachedBytes;
    public static byte[] TryCreateCroppedPng(string filePath)
    {
        try
        {
            var file = new FileInfo(filePath);
            var key = new CacheKey(
                file.FullName,
                file.LastWriteTimeUtc.Ticks,
                file.Length);

            CacheEntry entry;

            lock (CacheLock)
            {
                if (Cache.TryGetValue(key, out entry!))
                {
                    Touch(entry);
                }
                else
                {
                    var node = LeastRecentlyUsed.AddFirst(key);
                    entry = new CacheEntry(
                        new Lazy<byte[]>(
                            () => CreateCroppedPng(key.FullPath),
                            LazyThreadSafetyMode.ExecutionAndPublication),
                        node);
                    Cache.Add(key, entry);
                }
            }

            var result = entry.Value.Value;

            lock (CacheLock)
            {
                if (!entry.HasMeasuredSize &&
                    Cache.TryGetValue(key, out var currentEntry) &&
                    ReferenceEquals(entry, currentEntry))
                {
                    entry.HasMeasuredSize = true;
                    entry.Size = result.LongLength;
                    cachedBytes += entry.Size;
                    TrimCache();
                }
            }

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static byte[] CreateCroppedPng(string filePath)
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

            using (var canvas = new SKCanvas(croppedBitmap))
            using (var paint = new SKPaint
            {
                IsAntialias = false,
                BlendMode = SKBlendMode.SrcOver
            })
            {
                canvas.Clear(SKColors.White);
                canvas.DrawBitmap(
                    sourceBitmap,
                    new SKRect(
                        cropBounds.Left,
                        cropBounds.Top,
                        cropBounds.Right,
                        cropBounds.Bottom),
                    new SKRect(
                        0,
                        0,
                        cropBounds.Width,
                        cropBounds.Height),
                    paint);
                canvas.Flush();
            }
            using var image = SKImage.FromBitmap(croppedBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var result = data?.ToArray() ?? [];

            return result;
        }
        catch
        {
            return [];
        }
    }

    private static void Touch(CacheEntry entry)
    {
        LeastRecentlyUsed.Remove(entry.Node);
        LeastRecentlyUsed.AddFirst(entry.Node);
    }

    private static void TrimCache()
    {
        var node = LeastRecentlyUsed.Last;

        while ((cachedBytes > MaximumCacheBytes || Cache.Count > MaximumCacheEntries) &&
               node != null)
        {
            var previous = node.Previous;

            if (Cache.TryGetValue(node.Value, out var entry) &&
                entry.HasMeasuredSize)
            {
                Cache.Remove(node.Value);
                LeastRecentlyUsed.Remove(node);
                cachedBytes -= entry.Size;
            }

            node = previous;
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

    private readonly record struct CacheKey(
        string FullPath,
        long LastWriteTimeUtcTicks,
        long Length);

    private sealed class CacheEntry(
        Lazy<byte[]> value,
        LinkedListNode<CacheKey> node)
    {
        public Lazy<byte[]> Value { get; } = value;
        public LinkedListNode<CacheKey> Node { get; } = node;
        public long Size { get; set; }
        public bool HasMeasuredSize { get; set; }
    }
}
