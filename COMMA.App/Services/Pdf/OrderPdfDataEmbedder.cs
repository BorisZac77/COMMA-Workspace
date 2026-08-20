using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using COMMA.App.Layout;
using COMMA.App.Models;

namespace COMMA.App.Services.Pdf;

public static class OrderPdfDataEmbedder
{
    private const string HiddenDataBeginMarker =
        "%COMMA-WORKSPACE-DATA-BEGIN";

    private const string HiddenDataEndMarker =
        "%COMMA-WORKSPACE-DATA-END";

    private const string FormatName =
        "COMMA Workspace Production Card";

    private const int FormatVersion =
        3;

    private const string ApplicationVersion =
        "3.0.0";


    public static void AddEmbeddedData(
        string sourcePdfPath,
        string outputPath,
        ProductionCard card,
        IReadOnlyList<OrderPageLayout> pages)
    {
        ArgumentNullException.ThrowIfNull(card);
        ArgumentNullException.ThrowIfNull(pages);

        if (string.IsNullOrWhiteSpace(sourcePdfPath))
        {
            throw new ArgumentException(
                "Nie podano ścieżki źródłowego pliku PDF.",
                nameof(sourcePdfPath));
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException(
                "Nie podano ścieżki docelowego pliku PDF.",
                nameof(outputPath));
        }

        if (!File.Exists(sourcePdfPath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono źródłowego pliku PDF.",
                sourcePdfPath);
        }

        var outputDirectory =
            Path.GetDirectoryName(
                outputPath);

        if (string.IsNullOrWhiteSpace(
                outputDirectory))
        {
            outputDirectory =
                Directory.GetCurrentDirectory();
        }

        Directory.CreateDirectory(
            outputDirectory);

        if (File.Exists(outputPath))
        {
            File.Delete(
                outputPath);
        }

        File.Copy(
            sourcePdfPath,
            outputPath,
            overwrite: true);

        var cardData =
            CreateCardData(
                card,
                pages);

        AppendHiddenData(
            outputPath,
            cardData);
    }


    private static void AppendHiddenData(
        string pdfPath,
        CommaCardData cardData)
    {
        var options =
            new JsonSerializerOptions
            {
                WriteIndented =
                    false
            };

        var json =
            JsonSerializer.Serialize(
                cardData,
                options);

        var jsonBytes =
            Encoding.UTF8.GetBytes(
                json);

        var base64 =
            Convert.ToBase64String(
                jsonBytes);

        using var stream =
            new FileStream(
                pdfPath,
                FileMode.Append,
                FileAccess.Write,
                FileShare.None);

        using var writer =
            new StreamWriter(
                stream,
                new UTF8Encoding(
                    encoderShouldEmitUTF8Identifier: false));

        writer.WriteLine();

        writer.WriteLine(
            HiddenDataBeginMarker);

        const int lineLength =
            120;

        for (
            var index = 0;
            index < base64.Length;
            index += lineLength)
        {
            var length =
                Math.Min(
                    lineLength,
                    base64.Length - index);

            writer.Write(
                "%");

            writer.WriteLine(
                base64.Substring(
                    index,
                    length));
        }

        writer.WriteLine(
            HiddenDataEndMarker);
    }


    private static CommaCardData CreateCardData(
        ProductionCard card,
        IReadOnlyList<OrderPageLayout> pages)
    {
        var entries =
            card.ProductionEntries
                .Select(entry =>
                    new CommaProductionEntryData
                    {
                        Number =
                            entry.Number,

                        LogoName =
                            Safe(
                                entry.LogoName),

                        Dimension =
                            Safe(
                                entry.Dimension),

                        Colours =
                            entry.Colours
                                .Select(colour =>
                                    new CommaColourData
                                    {
                                        Number =
                                            colour.Number,

                                        Value =
                                            Safe(
                                                colour.Value)
                                    })
                                .ToList()
                    })
                .ToList();

        var garments =
            pages
                .SelectMany(page =>
                    page.Garments)
                .Distinct()
                .Select(garment =>
                    new CommaGarmentData
                    {
                        ProductCode =
                            Safe(
                                garment.ProductCode),

                        ProductName =
                            Safe(
                                garment.Name),

                        Name =
                            Safe(
                                garment.Name),

                        Colour =
                            Safe(
                                garment.Colour),

                        Variant =
                            Safe(
                                garment.Variant),

                        ShowFront =
                            garment.ShowFront,

                        ShowBack =
                            garment.ShowBack,

                        ShowRight =
                            garment.ShowRight,

                        ShowLeft =
                            garment.ShowLeft,

                        StartNewPage =
                            garment.StartNewPage
                    })
                .ToList();

        return new CommaCardData
        {
            Format =
                FormatName,

            FormatVersion =
                FormatVersion,

            ApplicationVersion =
                ApplicationVersion,

            SavedUtc =
                DateTime.UtcNow,

            OrderName =
                Safe(
                    card.OrderName),

            Customer =
                Safe(
                    card.Customer),

            ReceivedDate =
                Safe(
                    card.ReceivedDate),

            DueDate =
                Safe(
                    card.DueDate),

            ProductionType =
                Safe(
                    card.ProductionType),

            ProductCode =
                Safe(
                    card.ProductCode),

            ProductName =
                Safe(
                    card.ProductName),

            Colour =
                Safe(
                    card.Colour),

            Size =
                Safe(
                    card.Size),

            Quantity =
                Safe(
                    card.Quantity),

            Notes =
                Safe(
                    card.Notes),

            ShowFront =
                card.ShowFront,

            ShowBack =
                card.ShowBack,

            ShowLeft =
                card.ShowLeft,

            ShowRight =
                card.ShowRight,

            Garments =
                garments,

            ProductionEntries =
                entries
        };
    }


    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }
}