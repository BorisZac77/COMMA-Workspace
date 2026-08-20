using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using COMMA.Core.Models;

namespace COMMA.DrawingsGenerator.Services;

public class TechnicalDrawingsImportService
{
    public string Import(
        Product product,
        string sourcePath)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.Folder) ||
            !Directory.Exists(product.Folder))
        {
            throw new DirectoryNotFoundException(
                "Folder wybranego produktu nie istnieje.");
        }

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            throw new ArgumentException(
                "Nie wskazano folderu ani archiwum ZIP.",
                nameof(sourcePath));
        }

        var temporaryFolder = string.Empty;
        var sourceFolder = sourcePath;

        try
        {
            if (File.Exists(sourcePath))
            {
                if (!string.Equals(
                        Path.GetExtension(sourcePath),
                        ".zip",
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(
                        "Wybrany plik musi być archiwum ZIP.");
                }

                temporaryFolder = CreateTemporaryFolder();

                ZipFile.ExtractToDirectory(
                    sourcePath,
                    temporaryFolder);

                sourceFolder = temporaryFolder;
            }
            else if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException(
                    "Wybrany folder lub archiwum ZIP nie istnieje.");
            }

            var imageFiles = Directory
                .EnumerateFiles(
                    sourceFolder,
                    "*.png",
                    SearchOption.AllDirectories)
                .ToList();

            var frontFile = FindDrawingFile(
                imageFiles,
                "front",
                "front tech",
                "front technical",
                "front drawing");

            var backFile = FindDrawingFile(
                imageFiles,
                "back",
                "back tech",
                "back technical",
                "back drawing");

            var rightFile = FindDrawingFile(
                imageFiles,
                "right",
                "right side",
                "right tech",
                "right technical",
                "right drawing");

            var missingViews = new List<string>();

            if (frontFile == null)
                missingViews.Add("front.png");

            if (backFile == null)
                missingViews.Add("back.png");

            if (rightFile == null)
                missingViews.Add("right.png");

            if (missingViews.Count > 0)
            {
                throw new FileNotFoundException(
                    "Brakuje wymaganych rysunków PNG: " +
                    string.Join(", ", missingViews) +
                    ".");
            }

            ValidatePngFile(frontFile!);
            ValidatePngFile(backFile!);
            ValidatePngFile(rightFile!);

            var drawingsFolder = Path.Combine(
                product.Folder,
                "Drawings");

            Directory.CreateDirectory(drawingsFolder);

            var stagingFolder = Path.Combine(
                product.Folder,
                $".DrawingsImport-{Guid.NewGuid():N}");

            Directory.CreateDirectory(stagingFolder);

            try
            {
                CopyDrawing(
                    frontFile!,
                    stagingFolder,
                    "front.png");

                CopyDrawing(
                    backFile!,
                    stagingFolder,
                    "back.png");

                CopyDrawing(
                    rightFile!,
                    stagingFolder,
                    "right.png");

                ReplaceDrawing(
                    stagingFolder,
                    drawingsFolder,
                    "front.png");

                ReplaceDrawing(
                    stagingFolder,
                    drawingsFolder,
                    "back.png");

                ReplaceDrawing(
                    stagingFolder,
                    drawingsFolder,
                    "right.png");

                DeleteOldViewFiles(
                    drawingsFolder,
                    "front",
                    "front.png");

                DeleteOldViewFiles(
                    drawingsFolder,
                    "back",
                    "back.png");

                DeleteOldViewFiles(
                    drawingsFolder,
                    "right",
                    "right.png");
            }
            finally
            {
                TryDeleteDirectory(stagingFolder);
            }

            return drawingsFolder;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(temporaryFolder))
                TryDeleteDirectory(temporaryFolder);
        }
    }

    private static string CreateTemporaryFolder()
    {
        var temporaryFolder = Path.Combine(
            Path.GetTempPath(),
            "COMMA Drawings Generator",
            "Import",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(temporaryFolder);

        return temporaryFolder;
    }

    private static string? FindDrawingFile(
        IEnumerable<string> files,
        params string[] expectedNames)
    {
        var fileList = files
            .OrderBy(Path.GetFileName)
            .ToList();

        foreach (var expectedName in expectedNames)
        {
            var exactMatch = fileList.FirstOrDefault(
                file =>
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
            var partialMatch = fileList.FirstOrDefault(
                file =>
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

    private static void ValidatePngFile(
        string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono pliku rysunku.",
                filePath);
        }

        if (!string.Equals(
                Path.GetExtension(filePath),
                ".png",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException(
                $"Plik {Path.GetFileName(filePath)} nie jest plikiem PNG.");
        }

        var fileInfo = new FileInfo(filePath);

        if (fileInfo.Length == 0)
        {
            throw new InvalidDataException(
                $"Plik {fileInfo.Name} jest pusty.");
        }

        using var stream = File.OpenRead(filePath);

        Span<byte> signature = stackalloc byte[8];

        if (stream.Read(signature) != signature.Length)
        {
            throw new InvalidDataException(
                $"Plik {fileInfo.Name} jest uszkodzony.");
        }

        byte[] expectedSignature =
        {
            137,
            80,
            78,
            71,
            13,
            10,
            26,
            10
        };

        if (!signature.SequenceEqual(expectedSignature))
        {
            throw new InvalidDataException(
                $"Plik {fileInfo.Name} nie zawiera prawidłowego obrazu PNG.");
        }
    }

    private static void CopyDrawing(
        string sourceFile,
        string destinationFolder,
        string destinationFileName)
    {
        var destinationPath = Path.Combine(
            destinationFolder,
            destinationFileName);

        File.Copy(
            sourceFile,
            destinationPath,
            true);
    }

    private static void ReplaceDrawing(
        string stagingFolder,
        string drawingsFolder,
        string fileName)
    {
        var sourcePath = Path.Combine(
            stagingFolder,
            fileName);

        var destinationPath = Path.Combine(
            drawingsFolder,
            fileName);

        File.Copy(
            sourcePath,
            destinationPath,
            true);
    }

    private static void DeleteOldViewFiles(
        string drawingsFolder,
        string viewName,
        string preservedFileName)
    {
        var files = Directory
            .EnumerateFiles(
                drawingsFolder,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(file =>
                IsImageFile(file) &&
                IsMatchingView(file, viewName) &&
                !string.Equals(
                    Path.GetFileName(file),
                    preservedFileName,
                    StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch
            {
                // Brak możliwości usunięcia starego pliku
                // nie przerywa poprawnego importu nowych rysunków.
            }
        }
    }

    private static bool IsMatchingView(
        string filePath,
        string viewName)
    {
        var normalizedName = NormalizeName(
            Path.GetFileNameWithoutExtension(filePath));

        return normalizedName.Contains(
            NormalizeName(viewName),
            StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsImageFile(
        string filePath)
    {
        var extension = Path
            .GetExtension(filePath)
            .ToLowerInvariant();

        return extension is ".png" or ".jpg" or ".jpeg";
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
            // Folder tymczasowy może zostać usunięty później.
        }
    }
}