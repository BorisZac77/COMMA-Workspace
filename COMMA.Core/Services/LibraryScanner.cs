using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMMA.Core.Models;

namespace COMMA.Core.Services;

public class LibraryScanner
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    public List<Product> Scan(string libraryFolder)
    {
        var products = new List<Product>();

        if (!Directory.Exists(libraryFolder))
            return products;

        foreach (var productFolder in Directory.GetDirectories(libraryFolder))
        {
            var folderName = Path.GetFileName(productFolder);

            var product = new Product
            {
                Code = "",
                Name = folderName,
                Folder = productFolder
            };

            LoadProductInfo(product);
            LoadProductDrawings(product);
            LoadProductImagePath(product);

            product.Card.ProductCode = product.Code;
            product.Card.ProductName = product.Name;
            product.Card.ProductImagePath = product.ImagePath;

            products.Add(product);
        }

        return products
            .OrderBy(product => product.DisplayName)
            .ThenBy(product => product.DisplayCode)
            .ToList();
    }


    private static void LoadProductDrawings(Product product)
    {
        Console.WriteLine(
            $"LIBRARY PRODUCT: {product.Folder}");

        var drawingsFolder = FindSubfolder(
            product.Folder,
            "Drawings");


        Console.WriteLine(
            $"DRAWINGS FOLDER: {drawingsFolder}");


        var technicalDrawings =
            LoadDrawingsFromFolder(
                drawingsFolder);


        var rootDrawings =
            LoadDrawingsFromFolder(
                product.Folder);


        var photosFolder =
            FindSubfolder(
                product.Folder,
                "Photos");


        var photoDrawings =
            LoadDrawingsFromFolder(
                photosFolder);


        var fallbackDrawings =
            rootDrawings
                .Concat(photoDrawings)
                .ToList();


        AddPreferredView(
            product.Drawings,
            technicalDrawings,
            fallbackDrawings,
            drawing => drawing.IsFront);


        AddPreferredView(
            product.Drawings,
            technicalDrawings,
            fallbackDrawings,
            drawing => drawing.IsBack);


        AddPreferredView(
            product.Drawings,
            technicalDrawings,
            fallbackDrawings,
            drawing => drawing.IsRight);


        AddPreferredLeftView(
            product.Drawings,
            technicalDrawings,
            fallbackDrawings);


        Console.WriteLine(
            $"PRODUCT DRAWINGS COUNT: {product.Drawings.Count}");
    }


    private static List<DrawingFile> LoadDrawingsFromFolder(
        string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            !Directory.Exists(folder))
        {
            return new List<DrawingFile>();
        }

        Console.WriteLine(
            $"SCAN DRAWINGS: {folder}");

        return DrawingScanner.Scan(folder);
    }


    private static void AddPreferredView(
        ICollection<DrawingFile> destination,
        IEnumerable<DrawingFile> technicalDrawings,
        IEnumerable<DrawingFile> fallbackDrawings,
        Func<DrawingFile, bool> viewFilter)
    {
        var selectedDrawing =
            technicalDrawings.FirstOrDefault(viewFilter)
            ?? fallbackDrawings.FirstOrDefault(viewFilter);


        if (selectedDrawing == null)
            return;


        destination.Add(
            CloneDrawing(selectedDrawing));
    }


    private static void AddPreferredLeftView(
        ICollection<DrawingFile> destination,
        IEnumerable<DrawingFile> technicalDrawings,
        IEnumerable<DrawingFile> fallbackDrawings)
    {
        var technicalLeft =
            technicalDrawings.FirstOrDefault(
                drawing =>
                    drawing.IsLeft &&
                    !drawing.MirrorHorizontally);


        var fallbackLeft =
            fallbackDrawings.FirstOrDefault(
                drawing =>
                    drawing.IsLeft &&
                    !drawing.MirrorHorizontally);


        var selectedLeft =
            technicalLeft ?? fallbackLeft;


        if (selectedLeft != null)
        {
            destination.Add(
                CloneDrawing(selectedLeft));

            return;
        }


        var selectedRight =
            destination.FirstOrDefault(
                drawing => drawing.IsRight);


        if (selectedRight == null)
            return;


        destination.Add(new DrawingFile
        {
            Name = $"mirrored-{selectedRight.Name}",
            FullPath = selectedRight.FullPath,
            View = "Left",
            IsFront = false,
            IsBack = false,
            IsLeft = true,
            IsRight = false,
            MirrorHorizontally = true
        });
    }


    private static DrawingFile CloneDrawing(
        DrawingFile source)
    {
        return new DrawingFile
        {
            Name = source.Name,
            FullPath = source.FullPath,
            View = source.View,
            IsFront = source.IsFront,
            IsBack = source.IsBack,
            IsLeft = source.IsLeft,
            IsRight = source.IsRight,
            MirrorHorizontally = source.MirrorHorizontally
        };
    }


    private static void LoadProductImagePath(
        Product product)
    {
        var rootImage =
            FindPreferredProductImage(
                product.Folder);


        if (!string.IsNullOrWhiteSpace(rootImage))
        {
            SetProductImagePath(
                product,
                rootImage);

            return;
        }


        LoadFallbackImagePathFromFrontDrawing(
            product);
    }


    private static string? FindPreferredProductImage(
        string folder)
    {
        if (!Directory.Exists(folder))
            return null;


        var imageFiles =
            Directory
                .EnumerateFiles(
                    folder,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsSupportedImage)
                .OrderBy(Path.GetFileName)
                .ToList();


        if (imageFiles.Count == 0)
            return null;


        return imageFiles.FirstOrDefault(
                   file =>
                   {
                       var name =
                           NormalizeName(
                               Path.GetFileNameWithoutExtension(file));

                       return name.Contains(
                                  "front",
                                  StringComparison.OrdinalIgnoreCase)
                              ||
                              name.Contains(
                                  "przod",
                                  StringComparison.OrdinalIgnoreCase)
                              ||
                              name.Contains(
                                  "przód",
                                  StringComparison.OrdinalIgnoreCase);
                   })
               ?? imageFiles.First();
    }


    private static void LoadFallbackImagePathFromFrontDrawing(
        Product product)
    {
        var frontDrawing =
            product.Drawings.FirstOrDefault(
                drawing => drawing.IsFront);


        if (frontDrawing == null ||
            !File.Exists(frontDrawing.FullPath))
        {
            return;
        }


        SetProductImagePath(
            product,
            frontDrawing.FullPath);
    }


    private static void SetProductImagePath(
        Product product,
        string imagePath)
    {
        product.ImagePath = imagePath;
        product.Card.ProductImagePath = imagePath;
    }


    private static void LoadProductInfo(
        Product product)
    {
        var infoFile =
            Path.Combine(
                product.Folder,
                "product.txt");


        if (!File.Exists(infoFile))
            return;


        foreach (var line in File.ReadAllLines(infoFile))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;


            var parts =
                line.Split(
                    '=',
                    2,
                    StringSplitOptions.TrimEntries);


            if (parts.Length != 2)
                continue;


            switch (parts[0].ToLowerInvariant())
            {
                case "name":
                    product.Name = parts[1];
                    break;

                case "code":
                    product.Code = parts[1];
                    break;
            }
        }
    }


    private static string? FindSubfolder(
        string parentFolder,
        string expectedName)
    {
        if (!Directory.Exists(parentFolder))
            return null;


        return Directory
            .EnumerateDirectories(
                parentFolder,
                "*",
                SearchOption.TopDirectoryOnly)
            .FirstOrDefault(
                folder =>
                    string.Equals(
                        Path.GetFileName(folder),
                        expectedName,
                        StringComparison.OrdinalIgnoreCase));
    }


    private static bool IsSupportedImage(
        string file)
    {
        var extension =
            Path.GetExtension(file)
                .ToLowerInvariant();

        return SupportedImageExtensions.Contains(
            extension);
    }


    private static string NormalizeName(
        string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ");
    }
}