using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Services;

public class ProductExportService
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    public string Export(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.Folder) ||
            !Directory.Exists(product.Folder))
        {
            throw new DirectoryNotFoundException(
                "Folder wybranego produktu nie istnieje.");
        }

        var sourceFolder = GetPhotosFolder(product.Folder);

        var frontFile = FindViewFile(
            sourceFolder,
            "front",
            "przod",
            "przód");

        var backFile = FindViewFile(
            sourceFolder,
            "back",
            "tyl",
            "tył");

        var rightFile = FindViewFile(
            sourceFolder,
            "right",
            "right side",
            "right-side",
            "prawy",
            "prawy bok");

        var missingViews = new List<string>();

        if (frontFile == null)
            missingViews.Add("front");

        if (backFile == null)
            missingViews.Add("back");

        if (rightFile == null)
            missingViews.Add("right side");

        if (missingViews.Count > 0)
        {
            throw new FileNotFoundException(
                "Nie znaleziono wymaganych zdjęć: " +
                string.Join(", ", missingViews) +
                ".");
        }

        var desktopPath = Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);

        if (string.IsNullOrWhiteSpace(desktopPath) ||
            !Directory.Exists(desktopPath))
        {
            throw new DirectoryNotFoundException(
                "Nie udało się odnaleźć Biurka.");
        }

        var productName = GetProductName(product);
        var safeProductName = MakeSafeFileName(productName);

        var outputPath = Path.Combine(
            desktopPath,
            $"{safeProductName}.zip");

        if (File.Exists(outputPath))
            File.Delete(outputPath);

        using var archive = ZipFile.Open(
            outputPath,
            ZipArchiveMode.Create);

        AddImage(
            archive,
            frontFile!,
            "front");

        AddImage(
            archive,
            backFile!,
            "back");

        AddImage(
            archive,
            rightFile!,
            "right");

        return outputPath;
    }

    private static string GetPhotosFolder(
        string productFolder)
    {
        var photosFolder = Directory
            .EnumerateDirectories(
                productFolder,
                "*",
                SearchOption.TopDirectoryOnly)
            .FirstOrDefault(folder =>
                string.Equals(
                    Path.GetFileName(folder),
                    "Photos",
                    StringComparison.OrdinalIgnoreCase));

        return photosFolder ?? productFolder;
    }

    private static string? FindViewFile(
        string folder,
        params string[] expectedNames)
    {
        if (!Directory.Exists(folder))
            return null;

        var imageFiles = Directory
            .EnumerateFiles(
                folder,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(IsSupportedImage)
            .OrderBy(Path.GetFileName)
            .ToList();

        foreach (var expectedName in expectedNames)
        {
            var exactMatch = imageFiles.FirstOrDefault(file =>
                string.Equals(
                    NormalizeName(
                        Path.GetFileNameWithoutExtension(file)),
                    NormalizeName(expectedName),
                    StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;
        }

        foreach (var expectedName in expectedNames)
        {
            var partialMatch = imageFiles.FirstOrDefault(file =>
                NormalizeName(
                        Path.GetFileNameWithoutExtension(file))
                    .Contains(
                        NormalizeName(expectedName),
                        StringComparison.OrdinalIgnoreCase));

            if (partialMatch != null)
                return partialMatch;
        }

        return null;
    }

    private static void AddImage(
        ZipArchive archive,
        string sourceFile,
        string outputName)
    {
        var extension = Path
            .GetExtension(sourceFile)
            .ToLowerInvariant();

        archive.CreateEntryFromFile(
            sourceFile,
            $"{outputName}{extension}",
            CompressionLevel.Optimal);
    }

    private static bool IsSupportedImage(
        string file)
    {
        var extension = Path
            .GetExtension(file)
            .ToLowerInvariant();

        return SupportedImageExtensions.Contains(extension);
    }

    private static string GetProductName(
        Product product)
    {
        if (!string.IsNullOrWhiteSpace(product.Name))
            return product.Name.Trim();

        if (!string.IsNullOrWhiteSpace(product.Code))
            return product.Code.Trim();

        return Path.GetFileName(product.Folder);
    }

    private static string MakeSafeFileName(
        string value)
    {
        var safeName = value.Trim();

        foreach (var invalidCharacter in
                 Path.GetInvalidFileNameChars())
        {
            safeName = safeName.Replace(
                invalidCharacter,
                '_');
        }

        safeName = safeName
            .Replace('/', '_')
            .Replace('\\', '_')
            .Replace(':', '_');

        return string.IsNullOrWhiteSpace(safeName)
            ? "Produkt"
            : safeName;
    }

    private static string NormalizeName(
        string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace("  ", " ");
    }
}