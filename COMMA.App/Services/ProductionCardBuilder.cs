using System;
using System.IO;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Services;

public class ProductionCardBuilder
{
    public ProductionCard Build(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);

        var card = new ProductionCard
        {
            OrderName = "",
            ProductCode = product.Code,
            ProductName = product.Name,
            ProductImagePath = product.ImagePath
        };

        foreach (var drawing in product.Drawings
                     .Where(IsTechnicalDrawing)
                     .OrderBy(GetDrawingOrder))
        {
            card.Drawings.Add(drawing);
        }

        LoadProductInfo(product, card);
        LoadLogos(product, card);
        DetectArtworkFiles(product, card);

        card.RefreshDrawingSelection();

        return card;
    }

    private static bool IsTechnicalDrawing(
        DrawingFile drawing)
    {
        if (string.IsNullOrWhiteSpace(drawing.FullPath))
            return false;

        if (!File.Exists(drawing.FullPath))
            return false;

        var directoryPath =
            Path.GetDirectoryName(drawing.FullPath);

        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        var directoryNames = directoryPath.Split(
            new[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);

        return directoryNames.Any(
            directoryName =>
                string.Equals(
                    directoryName,
                    "Drawings",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static int GetDrawingOrder(
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

    private static void LoadProductInfo(
        Product product,
        ProductionCard card)
    {
        var infoFile = Path.Combine(
            product.Folder,
            "product.txt");

        if (!File.Exists(infoFile))
            return;

        foreach (var line in File.ReadAllLines(infoFile))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var trimmedLine = line.Trim();

            if (trimmedLine.StartsWith("#"))
                continue;

            var parts = trimmedLine.Split(
                '=',
                2,
                StringSplitOptions.TrimEntries);

            if (parts.Length != 2)
                continue;

            var key = parts[0].ToLowerInvariant();
            var value = parts[1];

            switch (key)
            {
                case "code":
                    card.ProductCode = value;
                    break;

                case "name":
                    card.ProductName = value;
                    break;

                case "order":
                    card.OrderName = value;
                    break;

                case "customer":
                    card.Customer = value;
                    break;

                case "colour":
                case "color":
                    card.Colour = value;
                    break;

                case "size":
                    card.Size = value;
                    break;

                case "quantity":
                    card.Quantity = value;
                    break;

                case "notes":
                    card.Notes = value;
                    break;

                case "receiveddate":
                    card.ReceivedDate = value;
                    break;

                case "duedate":
                    card.DueDate = value;
                    break;

                case "productiontype":
                    card.ProductionType = value;
                    break;
            }
        }
    }

    private static void LoadLogos(
        Product product,
        ProductionCard card)
    {
        var logoFolder = Path.Combine(
            product.Folder,
            "Logos");

        if (!Directory.Exists(logoFolder))
            return;

        foreach (var file in Directory
                     .EnumerateFiles(logoFolder)
                     .Where(IsLogo)
                     .OrderBy(Path.GetFileName))
        {
            card.Logos.Add(new LogoPlacement
            {
                Name = Path.GetFileNameWithoutExtension(file),
                File = file
            });
        }
    }

    private static void DetectArtworkFiles(
        Product product,
        ProductionCard card)
    {
        if (!Directory.Exists(product.Folder))
            return;

        foreach (var file in Directory.EnumerateFiles(
                     product.Folder,
                     "*",
                     SearchOption.AllDirectories))
        {
            var fileName = Path
                .GetFileName(file)
                .ToLowerInvariant();

            var extension = Path
                .GetExtension(file)
                .ToLowerInvariant();

            if (string.IsNullOrWhiteSpace(card.ClientLogoPath) &&
                (fileName.Contains("logo") ||
                 fileName.Contains("logotyp")))
            {
                card.ClientLogoPath = file;
            }

            if (string.IsNullOrWhiteSpace(
                    card.EmbroideryProgramPath) &&
                IsEmbroideryFile(extension))
            {
                card.EmbroideryProgramPath = file;
            }

            if (string.IsNullOrWhiteSpace(card.PrintFilePath) &&
                IsPrintFile(extension))
            {
                card.PrintFilePath = file;
            }
        }
    }

    private static bool IsEmbroideryFile(
        string extension)
    {
        return extension == ".emb"
            || extension == ".dst"
            || extension == ".exp"
            || extension == ".pes";
    }

    private static bool IsPrintFile(
        string extension)
    {
        return extension == ".pdf"
            || extension == ".ai"
            || extension == ".eps"
            || extension == ".svg";
    }

    private static bool IsLogo(
        string file)
    {
        var extension = Path
            .GetExtension(file)
            .ToLowerInvariant();

        return extension == ".png"
            || extension == ".jpg"
            || extension == ".jpeg"
            || extension == ".pdf"
            || extension == ".svg"
            || extension == ".ai"
            || extension == ".eps";
    }
}