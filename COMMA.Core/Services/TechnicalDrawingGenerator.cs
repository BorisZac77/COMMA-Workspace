using SkiaSharp;

namespace COMMA.Core.Services;

public static class TechnicalDrawingGenerator
{
    public static string Generate(
        string sourceImagePath)
    {
        Console.WriteLine(
            $"GENERATOR START: {sourceImagePath}");

        if (!File.Exists(sourceImagePath))
            return string.Empty;


        var directory =
            Path.GetDirectoryName(sourceImagePath);

        if (string.IsNullOrWhiteSpace(directory))
            return string.Empty;


        var outputDirectory =
            Path.Combine(
                directory,
                "_generated");

        Directory.CreateDirectory(
            outputDirectory);


        var fileName =
            Path.GetFileNameWithoutExtension(
                sourceImagePath);

        var outputPath =
            Path.Combine(
                outputDirectory,
                $"{fileName}_technical.png");


        using var source =
            SKBitmap.Decode(
                sourceImagePath);

        if (source == null)
            return string.Empty;


        using var result =
            new SKBitmap(
                source.Width,
                source.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);


        using var canvas =
            new SKCanvas(result);

        canvas.Clear(
            SKColors.White);


        var gray =
            CreateGrayBitmap(
                source);


        var blurred =
            Blur(
                gray);


        using var paint =
            new SKPaint
            {
                Color =
                    new SKColor(
                        45,
                        45,
                        45),
                StrokeWidth = 1.2f,
                Style =
                    SKPaintStyle.Stroke,
                IsAntialias = true
            };


        using var innerPaint =
            new SKPaint
            {
                Color =
                    new SKColor(
                        120,
                        120,
                        120),
                StrokeWidth = 0.7f,
                Style =
                    SKPaintStyle.Stroke,
                IsAntialias = true
            };


        for (var y = 2;
             y < source.Height - 2;
             y++)
        {
            for (var x = 2;
                 x < source.Width - 2;
                 x++)
            {
                var center =
                    blurred.GetPixel(
                        x,
                        y);

                var right =
                    blurred.GetPixel(
                        x + 2,
                        y);

                var down =
                    blurred.GetPixel(
                        x,
                        y + 2);


                var horizontal =
                    Math.Abs(
                        center.Red -
                        right.Red);


                var vertical =
                    Math.Abs(
                        center.Red -
                        down.Red);


                var edge =
                    horizontal +
                    vertical;


                if (edge > 55)
                {
                    canvas.DrawPoint(
                        x,
                        y,
                        paint);
                }
            }
        }


        using var final =
            SKImage.FromBitmap(
                result);


        using var encoded =
            final.Encode(
                SKEncodedImageFormat.Png,
                100);


        using var stream =
            File.Open(
                outputPath,
                FileMode.Create,
                FileAccess.Write);


        encoded.SaveTo(
            stream);


        Console.WriteLine(
            $"GENERATED: {outputPath}");


        return outputPath;
    }


    private static SKBitmap CreateGrayBitmap(
        SKBitmap source)
    {
        var gray =
            new SKBitmap(
                source.Width,
                source.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);


        using var canvas =
            new SKCanvas(
                gray);


        using var paint =
            new SKPaint
            {
                ColorFilter =
                    SKColorFilter.CreateColorMatrix(
                        new float[]
                        {
                            0.299f,0.587f,0.114f,0,0,
                            0.299f,0.587f,0.114f,0,0,
                            0.299f,0.587f,0.114f,0,0,
                            0,0,0,1,0
                        })
            };


        canvas.DrawBitmap(
            source,
            0,
            0,
            paint);


        return gray;
    }


    private static SKBitmap Blur(
        SKBitmap source)
    {
        var blurred =
            new SKBitmap(
                source.Width,
                source.Height,
                SKColorType.Rgba8888,
                SKAlphaType.Premul);


        using var canvas =
            new SKCanvas(
                blurred);


        using var paint =
            new SKPaint
            {
                ImageFilter =
                    SKImageFilter.CreateBlur(
                        1.5f,
                        1.5f)
            };


        canvas.DrawBitmap(
            source,
            0,
            0,
            paint);


        return blurred;
    }
}