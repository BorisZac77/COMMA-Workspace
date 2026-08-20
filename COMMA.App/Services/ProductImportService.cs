using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Services;

public class ProductImportService
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    public void Import(
        Product product,
        string zipFilePath)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.Folder) ||
            !Directory.Exists(product.Folder))
        {
            throw new DirectoryNotFoundException(
                "Folder wybranego produktu nie istnieje.");
        }

        if (string.IsNullOrWhiteSpace(zipFilePath) ||
            !File.Exists(zipFilePath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono wybranego archiwum ZIP.",
                zipFilePath);
        }

        if (!string.Equals(
                Path.GetExtension(zipFilePath),
                ".zip",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                "Wybrany plik nie jest archiwum ZIP.");
        }

        var temporaryFolder = Path.Combine(
            Path.GetTempPath(),
            "COMMA Workspace",
            "DrawingImport",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryFolder);

        try
        {
            ZipFile.ExtractToDirectory(
                zipFilePath,
                temporaryFolder);

            var imageFiles = Directory
                .EnumerateFiles(
                    temporaryFolder,
                    "*",
                    SearchOption.AllDirectories)
                .Where(IsSupportedImage)
                .ToList();

            var frontFile = FindDrawingFile(
                imageFiles,
                "front-tech",
                "front_tech",
                "front tech",
                "front");

            var backFile = FindDrawingFile(
                imageFiles,
                "back-tech",
                "back_tech",
                "back tech",
                "back");

            var rightFile = FindDrawingFile(
                imageFiles,
                "right-tech",
                "right_tech",
                "right tech",
                "right-side",
                "right side",
                "right");

            var missingViews = new List<string>();

            if (frontFile == null)
                missingViews.Add("front");

            if (backFile == null)
                missingViews.Add("back");

            if (rightFile == null)
                missingViews.Add("right");

            if (missingViews.Count > 0)
            {
                throw new FileNotFoundException(
                    "W archiwum brakuje rysunków: " +
                    string.Join(", ", missingViews) +
                    ".");
            }

            var drawingsFolder = Path.Combine(
                product.Folder,
                "Drawings");

            Directory.CreateDirectory(drawingsFolder);

            CopyDrawing(
                frontFile!,
                drawingsFolder,
                "front");

            CopyDrawing(
                backFile!,
                drawingsFolder,
                "back");

            CopyDrawing(
                rightFile!,
                drawingsFolder,
                "right");
        }
        finally
        {
            TryDeleteDirectory(temporaryFolder);
        }
    }

    private static string? FindDrawingFile(
        IEnumerable<string> files,
        params string[] expectedNames)
    {
        var fileList = files.ToList();

        foreach (var expectedName in expectedNames)
        {
            var exactMatch = fileList.FirstOrDefault(file =>
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
            var partialMatch = fileList.FirstOrDefault(file =>
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

    private static void CopyDrawing(
        string sourceFile,
        string drawingsFolder,
        string outputName)
    {
        var extension = Path
            .GetExtension(sourceFile)
            .ToLowerInvariant();

        var destinationFile = Path.Combine(
            drawingsFolder,
            $"{outputName}{extension}");

        DeleteExistingViewFiles(
            drawingsFolder,
            outputName);

        File.Copy(
            sourceFile,
            destinationFile,
            true);
    }

    private static void DeleteExistingViewFiles(
        string drawingsFolder,
        string viewName)
    {
        foreach (var extension in SupportedImageExtensions)
        {
            var filePath = Path.Combine(
                drawingsFolder,
                $"{viewName}{extension}");

            if (File.Exists(filePath))
                File.Delete(filePath);

            var techFilePath = Path.Combine(
                drawingsFolder,
                $"{viewName}-tech{extension}");

            if (File.Exists(techFilePath))
                File.Delete(techFilePath);
        }
    }

    private static bool IsSupportedImage(
        string file)
    {
        var extension = Path
            .GetExtension(file)
            .ToLowerInvariant();

        return SupportedImageExtensions.Contains(extension);
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

    private static void TryDeleteDirectory(
        string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(
                    directory,
                    true);
            }
        }
        catch
        {
            // Folder tymczasowy zostanie usunięty później przez system.
        }
    }
}