using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using COMMA.Core.Models;

namespace COMMA.DrawingsGenerator.Services;

public class ChatGptExportService
{
    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    private const string PromptFileName =
        "prompt.txt";

    private const string SpecificationFileName =
        "specification.json";

    public string Export(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        if (string.IsNullOrWhiteSpace(product.Folder) ||
            !Directory.Exists(product.Folder))
        {
            throw new DirectoryNotFoundException(
                "Folder wybranego produktu nie istnieje.");
        }

        var photosFolder =
            FindSubfolder(product.Folder, "Photos")
            ?? FindSubfolder(product.Folder, "Product")
            ?? product.Folder;

        var frontFile = FindViewFile(
            photosFolder,
            "front",
            "przod",
            "przód");

        var backFile = FindViewFile(
            photosFolder,
            "back",
            "tyl",
            "tył");

        var rightFile = FindViewFile(
            photosFolder,
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
            missingViews.Add("right");

        if (missingViews.Count > 0)
        {
            throw new FileNotFoundException(
                "Brakuje zdjęć produktu: " +
                string.Join(", ", missingViews) +
                ".");
        }

        var desktopPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.DesktopDirectory);

        if (string.IsNullOrWhiteSpace(desktopPath) ||
            !Directory.Exists(desktopPath))
        {
            throw new DirectoryNotFoundException(
                "Nie udało się odnaleźć Biurka.");
        }

        var safeProductName =
            CreateSafeFileName(product.DisplayName);

        var exportFolder = Path.Combine(
            desktopPath,
            $"{safeProductName} - ChatGPT Export");

        var zipFilePath = Path.Combine(
            desktopPath,
            $"{safeProductName} - ChatGPT Export.zip");

        DeleteExistingExport(
            exportFolder,
            zipFilePath);

        Directory.CreateDirectory(exportFolder);

        try
        {
            CopyViewFile(
                frontFile!,
                exportFolder,
                "front");

            CopyViewFile(
                backFile!,
                exportFolder,
                "back");

            CopyViewFile(
                rightFile!,
                exportFolder,
                "right");

            var promptFilePath = Path.Combine(
                exportFolder,
                PromptFileName);

            File.WriteAllText(
                promptFilePath,
                CreatePrompt(product));

            var specificationFilePath = Path.Combine(
                exportFolder,
                SpecificationFileName);

            File.WriteAllText(
                specificationFilePath,
                CreateSpecification());

            ZipFile.CreateFromDirectory(
                exportFolder,
                zipFilePath,
                CompressionLevel.Optimal,
                false);

            return zipFilePath;
        }
        catch
        {
            TryDeleteDirectory(exportFolder);

            if (File.Exists(zipFilePath))
            {
                try
                {
                    File.Delete(zipFilePath);
                }
                catch
                {
                    // Nie przerywamy obsługi głównego błędu.
                }
            }

            throw;
        }
    }

    private static void CopyViewFile(
        string sourceFile,
        string destinationFolder,
        string outputName)
    {
        var extension = Path
            .GetExtension(sourceFile)
            .ToLowerInvariant();

        var destinationFile = Path.Combine(
            destinationFolder,
            $"{outputName}{extension}");

        File.Copy(
            sourceFile,
            destinationFile,
            true);
    }

    private static string CreatePrompt(
        Product product)
    {
        return
            "Create professional technical flat drawings of the garment " +
            "based strictly on the attached reference photographs." +
            Environment.NewLine +
            Environment.NewLine +

            $"Product name: {product.DisplayName}" +
            Environment.NewLine +

            $"Product code: {product.DisplayCode}" +
            Environment.NewLine +
            Environment.NewLine +

            "Return exactly three separate PNG image files:" +
            Environment.NewLine +
            "front.png" +
            Environment.NewLine +
            "back.png" +
            Environment.NewLine +
            "right.png" +
            Environment.NewLine +
            Environment.NewLine +

            "GENERAL REQUIREMENTS:" +
            Environment.NewLine +

            "- Create professional apparel technical flat drawings." +
            Environment.NewLine +

            "- Use thin, clean black technical lines." +
            Environment.NewLine +

            "- Use subtle, lighter lines for seams and stitching." +
            Environment.NewLine +

            "- Use a transparent background." +
            Environment.NewLine +

            "- Each image must be exactly 2048 x 2048 pixels." +
            Environment.NewLine +

            "- Keep the same line style and line thickness across all views." +
            Environment.NewLine +

            "- Center the garment in each image." +
            Environment.NewLine +

            "- Show one garment view per image only." +
            Environment.NewLine +
            Environment.NewLine +

            "ACCURACY REQUIREMENTS:" +
            Environment.NewLine +

            "- Preserve the garment shape exactly as shown in the photographs." +
            Environment.NewLine +

            "- Preserve the original proportions exactly." +
            Environment.NewLine +

            "- Do not make the garment wider, narrower, longer or shorter." +
            Environment.NewLine +

            "- Do not improve, redesign or correct the garment." +
            Environment.NewLine +

            "- Do not change the cut, silhouette or dimensions." +
            Environment.NewLine +

            "- Do not invent details that are not visible in the photographs." +
            Environment.NewLine +

            "- Do not guess hidden construction details." +
            Environment.NewLine +

            "- Reproduce all visible construction elements accurately." +
            Environment.NewLine +

            "- Preserve all visible seams and stitching." +
            Environment.NewLine +

            "- Preserve all visible panels and panel divisions." +
            Environment.NewLine +

            "- Preserve pockets exactly as shown." +
            Environment.NewLine +

            "- Preserve hoods exactly as shown." +
            Environment.NewLine +

            "- Preserve collars and their exact shape." +
            Environment.NewLine +

            "- Preserve zippers, buttons, snaps and fasteners exactly as shown." +
            Environment.NewLine +

            "- Preserve cuffs, ribbing, hems, openings and side slits." +
            Environment.NewLine +

            "- Show the same number and position of buttons as in the photographs." +
            Environment.NewLine +

            "- Elements visible only in the right-side photograph must remain " +
            "only in the right-side drawing unless they are clearly visible " +
            "in another reference photograph." +
            Environment.NewLine +
            Environment.NewLine +

            "REMOVE COMPLETELY:" +
            Environment.NewLine +

            "- manufacturer logos" +
            Environment.NewLine +

            "- branding" +
            Environment.NewLine +

            "- text" +
            Environment.NewLine +

            "- labels and tags" +
            Environment.NewLine +

            "- shadows" +
            Environment.NewLine +

            "- fabric folds" +
            Environment.NewLine +

            "- wrinkles" +
            Environment.NewLine +

            "- fabric texture" +
            Environment.NewLine +

            "- highlights and reflections" +
            Environment.NewLine +

            "- photographic background" +
            Environment.NewLine +

            "- mannequin or human body" +
            Environment.NewLine +

            "- dimensions, arrows and annotations" +
            Environment.NewLine +
            Environment.NewLine +

            "OUTPUT RULES:" +
            Environment.NewLine +

            "- Deliver front.png, back.png and right.png." +
            Environment.NewLine +

            "- Each view must be a separate image file." +
            Environment.NewLine +

            "- Do not combine the three views on one sheet." +
            Environment.NewLine +

            "- Do not add borders, titles, captions or labels." +
            Environment.NewLine +

            "- Do not return additional views or additional files.";
    }

    private static string CreateSpecification()
    {
        return
            "{" +
            Environment.NewLine +
            "  \"resolution\": 2048," +
            Environment.NewLine +
            "  \"background\": \"transparent\"," +
            Environment.NewLine +
            "  \"output\": [" +
            Environment.NewLine +
            "    \"front.png\"," +
            Environment.NewLine +
            "    \"back.png\"," +
            Environment.NewLine +
            "    \"right.png\"" +
            Environment.NewLine +
            "  ]" +
            Environment.NewLine +
            "}";
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
            var exactMatch = imageFiles.FirstOrDefault(
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
            var partialMatch = imageFiles.FirstOrDefault(
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

    private static string CreateSafeFileName(
        string value)
    {
        var safeValue = string.IsNullOrWhiteSpace(value)
            ? "Produkt"
            : value.Trim();

        foreach (var invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            safeValue = safeValue.Replace(
                invalidCharacter,
                '_');
        }

        return safeValue;
    }

    private static void DeleteExistingExport(
        string exportFolder,
        string zipFilePath)
    {
        if (Directory.Exists(exportFolder))
        {
            Directory.Delete(
                exportFolder,
                true);
        }

        if (File.Exists(zipFilePath))
            File.Delete(zipFilePath);
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
            // Folder eksportu może zostać usunięty ręcznie.
        }
    }
}