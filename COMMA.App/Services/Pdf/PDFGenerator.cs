using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services;

public static class PdfGenerator
{
    private const string EmbeddedDataKey =
        "comma-card-data";

    private const string EmbeddedDataFileName =
        "comma-card.json";

    private const string EmbeddedDataMimeType =
        "application/json";

    public static void Generate(string outputPath)
    {
        Generate(
            outputPath,
            new ProductionCard
            {
                OrderName = "TEST"
            });
    }

    public static void Generate(
        string outputPath,
        ProductionCard card)
    {
        QuestPDF.Settings.License =
            LicenseType.Community;

        QuestPDF.Settings.EnableDebugging =
            true;

        var drawingRows =
            DrawingLayoutEngine.GetRows(
                card);

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

        var temporaryPdfPath =
            Path.Combine(
                outputDirectory,
                $".comma-pdf-{Guid.NewGuid():N}.pdf");

        var temporaryDataPath =
            Path.Combine(
                outputDirectory,
                $".comma-data-{Guid.NewGuid():N}.json");

        try
        {
            GenerateBasePdf(
                temporaryPdfPath,
                card,
                drawingRows);

            WriteEmbeddedCardData(
                temporaryDataPath,
                card);

            AddEmbeddedCardData(
                temporaryPdfPath,
                temporaryDataPath,
                outputPath);

            SetInitialPageMode(
                outputPath);
        }
        finally
        {
            TryDeleteFile(
                temporaryPdfPath);

            TryDeleteFile(
                temporaryDataPath);
        }
    }

    private static void GenerateBasePdf(
        string outputPath,
        ProductionCard card,
        IReadOnlyList<DrawingLayoutRow> drawingRows)
    {
        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(
                    PageSizes.A4);

                page.Margin(
                    PdfStyles.PageMargin);

                page.DefaultTextStyle(style =>
                    style.FontSize(
                        PdfStyles.DefaultFontSize));

                page.Content()
                    .Border(
                        PdfStyles.OuterBorderWidth)
                    .Padding(
                        PdfStyles.PagePadding)
                    .Column(column =>
                    {
                        HeaderSection.Build(
                            column,
                            card,
                            "1/1");

                        column.Item()
                            .PaddingTop(
                                PdfStyles.SectionGap);

                        // SEKCJA 2
                        HandwrittenSection.Build(
                            column,
                            card);

                        column.Item()
                            .PaddingTop(
                                PdfStyles.SectionGap);

                        // NAZWA PRODUKTU
                        OrderSection.Build(
                            column,
                            card);

                        column.Item()
                            .PaddingTop(
                                PdfStyles.SectionGap);

                        // SEKCJA 3
                        DrawingSection.Build(
                            column,
                            drawingRows);
                    });
            });
        })
        .GeneratePdf(
            outputPath);
    }

    private static void WriteEmbeddedCardData(
        string outputPath,
        ProductionCard card)
    {
        var cardData =
            CreateCardData(
                card);

        var options =
            new JsonSerializerOptions
            {
                WriteIndented = true
            };

        var json =
            JsonSerializer.Serialize(
                cardData,
                options);

        File.WriteAllText(
            outputPath,
            json);
    }

    private static void AddEmbeddedCardData(
        string sourcePdfPath,
        string dataPath,
        string outputPath)
    {
        if (File.Exists(outputPath))
        {
            File.Delete(
                outputPath);
        }

        DocumentOperation
            .LoadFile(
                sourcePdfPath)
            .AddAttachment(
                new DocumentOperation.DocumentAttachment
                {
                    Key =
                        EmbeddedDataKey,

                    FilePath =
                        dataPath,

                    AttachmentName =
                        EmbeddedDataFileName,

                    MimeType =
                        EmbeddedDataMimeType,

                    Description =
                        "COMMA Workspace production card data",

                    Relationship =
                        DocumentOperation
                            .DocumentAttachmentRelationship
                            .Data,

                    CreationDate =
                        DateTime.UtcNow,

                    ModificationDate =
                        DateTime.UtcNow,

                    Replace =
                        true
                })
            .Save(
                outputPath);
    }

    private static void SetInitialPageMode(
        string pdfPath)
    {
        using var document =
            PdfReader.Open(
                pdfPath,
                PdfDocumentOpenMode.Modify);

        document.PageMode =
            PdfPageMode.UseNone;

        document.Save(
            pdfPath);
    }

    private static CommaCardData CreateCardData(
        ProductionCard card)
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

        return new CommaCardData
        {
            Format =
                "COMMA Workspace Production Card",

            FormatVersion =
                1,

            ApplicationVersion =
                "2.0.0",

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

            ProductionEntries =
                entries
        };
    }

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(
                    filePath);
            }
        }
        catch
        {
        }
    }

    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }

    private sealed class CommaCardData
    {
        public string Format { get; init; } = "";

        public int FormatVersion { get; init; }

        public string ApplicationVersion { get; init; } = "";

        public DateTime SavedUtc { get; init; }

        public string OrderName { get; init; } = "";

        public string Customer { get; init; } = "";

        public string ReceivedDate { get; init; } = "";

        public string DueDate { get; init; } = "";

        public string ProductionType { get; init; } = "";

        public string ProductCode { get; init; } = "";

        public string ProductName { get; init; } = "";

        public string Colour { get; init; } = "";

        public string Size { get; init; } = "";

        public string Quantity { get; init; } = "";

        public string Notes { get; init; } = "";

        public bool ShowFront { get; init; }

        public bool ShowBack { get; init; }

        public bool ShowLeft { get; init; }

        public bool ShowRight { get; init; }

        public List<CommaProductionEntryData> ProductionEntries
        {
            get;
            init;
        } = new();
    }

    private sealed class CommaProductionEntryData
    {
        public int Number { get; init; }

        public string LogoName { get; init; } = "";

        public string Dimension { get; init; } = "";

        public List<CommaColourData> Colours
        {
            get;
            init;
        } = new();
    }

    private sealed class CommaColourData
    {
        public int Number { get; init; }

        public string Value { get; init; } = "";
    }
}
