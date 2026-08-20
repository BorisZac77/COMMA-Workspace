using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SkiaSharp;
using COMMA.App.Models;

namespace COMMA.App.Services;

public static class DrawingScanner
{
    public static List<DrawingFile> Scan(string productFolder)
    {
        Console.WriteLine($"DRAWING SCAN START: {productFolder}");

        if (!Directory.Exists(productFolder))
        {
            Console.WriteLine("FOLDER NOT FOUND");
            return new List<DrawingFile>();
        }

        var files =
            Directory
                .EnumerateFiles(
                    productFolder,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsSupportedImage)
                .ToList();

        Console.WriteLine($"IMAGES FOUND: {files.Count}");

        foreach (var file in files)
        {
            Console.WriteLine($"IMAGE: {file}");
        }

        var drawings =
            files
                .Select(CreateDrawingFile)
                .Where(drawing =>
                    drawing.IsFront ||
                    drawing.IsBack ||
                    drawing.IsRight ||
                    drawing.IsLeft)
                .ToList();

        var result =
            new List<DrawingFile>();

        AddFirst(result, drawings, d => d.IsFront);
        AddFirst(result, drawings, d => d.IsBack);
        AddFirst(result, drawings, d => d.IsRight);
        AddFirst(result, drawings, d => d.IsLeft);

        AddMirroredLeftIfMissing(
            result,
            productFolder);

        Console.WriteLine($"DRAWINGS RETURNED: {result.Count}");

        return result
            .OrderBy(GetOrder)
            .ThenBy(drawing => drawing.Name)
            .ToList();
    }

    private static void AddFirst(
        ICollection<DrawingFile> destination,
        IEnumerable<DrawingFile> source,
        Func<DrawingFile, bool> selector)
    {
        var drawing =
            source.FirstOrDefault(selector);

        if (drawing != null)
            destination.Add(drawing);
    }

    private static DrawingFile CreateDrawingFile(
        string file)
    {
        var name =
            Path
                .GetFileNameWithoutExtension(file)
                .ToLowerInvariant();

        return new DrawingFile
        {
            Name = Path.GetFileName(file),

            FullPath = file,

            View = GetView(name),

            IsFront =
                Contains(name, "front", "przod", "przód"),

            IsBack =
                Contains(name, "back", "tyl", "tył"),

            IsRight =
                Contains(name, "right", "prawy"),

            IsLeft =
                Contains(name, "left", "lewy"),

            MirrorHorizontally = false
        };
    }

    private static void AddMirroredLeftIfMissing(
        ICollection<DrawingFile> drawings,
        string productFolder)
    {
        Console.WriteLine(
            $"MIRROR CHECK FOLDER: {productFolder}");

        if (drawings.Any(d => d.IsLeft))
        {
            Console.WriteLine(
                "LEFT ALREADY EXISTS");

            return;
        }

        var right =
            drawings.FirstOrDefault(
                d => d.IsRight);

        if (right == null)
        {
            Console.WriteLine(
                "RIGHT DRAWING NOT FOUND");

            return;
        }

        Console.WriteLine(
            $"RIGHT SOURCE: {right.FullPath}");

        var leftPath =
            Path.Combine(
                productFolder,
                "left.png");

        Console.WriteLine(
            $"LEFT TARGET: {leftPath}");

        if (!File.Exists(leftPath))
        {
            Console.WriteLine(
                "CREATING LEFT IMAGE");

            CreateMirrorImage(
                right.FullPath,
                leftPath);
        }
        else
        {
            Console.WriteLine(
                "LEFT FILE ALREADY EXISTS");
        }

        if (File.Exists(leftPath))
        {
            Console.WriteLine(
                "LEFT CREATED OK");

            drawings.Add(
                new DrawingFile
                {
                    Name = "left.png",
                    FullPath = leftPath,
                    View = "Left",
                    IsFront = false,
                    IsBack = false,
                    IsRight = false,
                    IsLeft = true,
                    MirrorHorizontally = true
                });
        }
        else
        {
            Console.WriteLine(
                "LEFT CREATION FAILED");
        }
    }

    private static void CreateMirrorImage(
        string sourcePath,
        string destinationPath)
    {
        using var source =
            SKBitmap.Decode(sourcePath);

        if (source == null)
        {
            Console.WriteLine(
                "SOURCE BITMAP ERROR");

            return;
        }

        using var mirrored =
            new SKBitmap(
                source.Width,
                source.Height);

        using var canvas =
            new SKCanvas(mirrored);

        canvas.Clear(
            SKColors.White);

        canvas.Scale(
            -1,
            1,
            source.Width / 2f,
            source.Height / 2f);

        canvas.DrawBitmap(
            source,
            0,
            0);

        using var image =
            SKImage.FromBitmap(
                mirrored);

        using var data =
            image.Encode(
                SKEncodedImageFormat.Png,
                100);

        using var stream =
            File.Open(
                destinationPath,
                FileMode.Create,
                FileAccess.Write);

        data.SaveTo(stream);

        Console.WriteLine(
            "IMAGE SAVED");
    }

    private static string GetView(string name)
    {
        if (Contains(name, "front", "przod", "przód"))
            return "Front";

        if (Contains(name, "back", "tyl", "tył"))
            return "Back";

        if (Contains(name, "right", "prawy"))
            return "Right";

        if (Contains(name, "left", "lewy"))
            return "Left";

        return "Drawing";
    }

    private static bool Contains(
        string value,
        params string[] keywords)
    {
        return keywords.Any(
            keyword =>
                value.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsSupportedImage(string file)
    {
        var extension =
            Path.GetExtension(file)
                .ToLowerInvariant();

        return extension == ".png"
            || extension == ".jpg"
            || extension == ".jpeg";
    }

    private static int GetOrder(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return 0;

        if (drawing.IsBack)
            return 1;

        if (drawing.IsRight)
            return 2;

        if (drawing.IsLeft)
            return 3;

        return 100;
    }
}