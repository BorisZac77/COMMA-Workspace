using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using COMMA.App.Models;

namespace COMMA.App.Services;

public class LibraryScanner
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };


    public List<Product> Scan(
        string libraryFolder)
    {
        var products =
            new List<Product>();

        if (!Directory.Exists(libraryFolder))
            return products;


        foreach (var productFolder
                 in Directory.GetDirectories(libraryFolder))
        {
            var product =
                new Product
                {
                    Name =
                        Path.GetFileName(productFolder),

                    Folder =
                        productFolder
                };


            LoadProductInfo(
                product);

            LoadProductImage(
                product);

            LoadProductDrawings(
                product);


            product.Card.ProductCode =
                product.Code;

            product.Card.ProductName =
                product.Name;

            product.Card.ProductImagePath =
                product.ImagePath;


            products.Add(
                product);
        }


        return products
            .OrderBy(product => product.DisplayName)
            .ToList();
    }


    private static void LoadProductDrawings(
        Product product)
    {
        product.Drawings.Clear();


        var drawingsFolder =
            Path.Combine(
                product.Folder,
                "Drawings");


        if (Directory.Exists(drawingsFolder))
        {
            var normalizedFolder =
                Path.Combine(
                    drawingsFolder,
                    "_normalized");


            var sourceFolder =
                HasUsableNormalizedDrawings(
                    normalizedFolder)
                    ? normalizedFolder
                    : drawingsFolder;


            var drawings =
                DrawingScanner.Scan(
                    sourceFolder);


            AddDrawing(
                product.Drawings,
                drawings,
                drawing => drawing.IsFront);


            AddDrawing(
                product.Drawings,
                drawings,
                drawing => drawing.IsBack);


            AddDrawing(
                product.Drawings,
                drawings,
                drawing => drawing.IsRight);


            AddDrawing(
                product.Drawings,
                drawings,
                drawing => drawing.IsLeft);


            return;
        }


        // Awaryjnie:
        // stare biblioteki bez folderu Drawings.

        var legacyNormalizedFolder =
            Path.Combine(
                product.Folder,
                "_normalized");


        var legacySourceFolder =
            HasUsableNormalizedDrawings(
                legacyNormalizedFolder)
                ? legacyNormalizedFolder
                : product.Folder;


        var fallback =
            DrawingScanner.Scan(
                legacySourceFolder);


        product.Drawings.AddRange(
            fallback);
    }


    private static bool HasUsableNormalizedDrawings(
        string folder)
    {
        if (!Directory.Exists(folder))
            return false;


        var imageFiles =
            Directory
                .EnumerateFiles(
                    folder,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsSupportedImage)
                .ToList();


        if (imageFiles.Count == 0)
            return false;


        var hasFront =
            imageFiles.Any(file =>
                ContainsViewName(
                    file,
                    "front",
                    "przod",
                    "przód"));


        var hasBack =
            imageFiles.Any(file =>
                ContainsViewName(
                    file,
                    "back",
                    "tyl",
                    "tył"));


        var hasRight =
            imageFiles.Any(file =>
                ContainsViewName(
                    file,
                    "right",
                    "prawy"));


        return hasFront &&
               hasBack &&
               hasRight;
    }


    private static bool ContainsViewName(
        string file,
        params string[] keywords)
    {
        var name =
            Path
                .GetFileNameWithoutExtension(file)
                .ToLowerInvariant();


        return keywords.Any(
            keyword =>
                name.Contains(
                    keyword,
                    StringComparison.OrdinalIgnoreCase));
    }


    private static void AddDrawing(
        ICollection<DrawingFile> destination,
        IEnumerable<DrawingFile> source,
        Func<DrawingFile, bool> selector)
    {
        var drawing =
            source.FirstOrDefault(
                selector);


        if (drawing == null)
            return;


        destination.Add(
            drawing);
    }


    private static void LoadProductImage(
        Product product)
    {
        var image =
            Directory
                .EnumerateFiles(
                    product.Folder,
                    "*",
                    SearchOption.TopDirectoryOnly)
                .Where(IsSupportedImage)
                .FirstOrDefault(
                    file =>
                    {
                        var name =
                            Path
                                .GetFileNameWithoutExtension(file)
                                .ToLowerInvariant();


                        return name.Contains("front")
                               ||
                               name.Contains("przod")
                               ||
                               name.Contains("przód");
                    });


        if (string.IsNullOrWhiteSpace(image))
        {
            image =
                Directory
                    .EnumerateFiles(
                        product.Folder,
                        "*",
                        SearchOption.TopDirectoryOnly)
                    .Where(IsSupportedImage)
                    .FirstOrDefault();
        }


        if (string.IsNullOrWhiteSpace(image))
            return;


        product.ImagePath =
            image;


        try
        {
            product.FrontThumbnail =
                new Bitmap(image);
        }
        catch
        {
            product.FrontThumbnail =
                null;
        }
    }


    private static void LoadProductInfo(
        Product product)
    {
        var file =
            Path.Combine(
                product.Folder,
                "product.txt");


        if (!File.Exists(file))
            return;


        foreach (var line
                 in File.ReadAllLines(file))
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
                    product.Name =
                        parts[1];
                    break;

                case "code":
                    product.Code =
                        parts[1];
                    break;

                case "category":
                    product.Category =
                        parts[1];
                    break;
            }
        }
    }


    private static bool IsSupportedImage(
        string file)
    {
        return SupportedImageExtensions.Contains(
            Path.GetExtension(file)
                .ToLowerInvariant());
    }
}