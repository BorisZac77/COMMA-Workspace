using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using UglyToad.PdfPig;

namespace COMMA.App.Services.Pdf;

public static class CommaPdfDataReader
{
    private const string EmbeddedDataFileName =
        "comma-card.json";

    private const string EmbeddedDataKey =
        "comma-card-data";

    private const string HiddenDataBeginMarker =
        "%COMMA-WORKSPACE-DATA-BEGIN";

    private const string HiddenDataEndMarker =
        "%COMMA-WORKSPACE-DATA-END";

    private const string ExpectedFormat =
        "COMMA Workspace Production Card";


    public static CommaCardData Read(
        string pdfPath)
    {
        if (string.IsNullOrWhiteSpace(pdfPath))
        {
            throw new ArgumentException(
                "Nie podano ścieżki do pliku PDF.",
                nameof(pdfPath));
        }

        if (!File.Exists(pdfPath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono pliku PDF.",
                pdfPath);
        }

        /*
         * FORMAT 3.0
         *
         * Najpierw sprawdzamy niewidoczne dane COMMA
         * zapisane na końcu PDF.
         *
         * Nie są one załącznikiem PDF,
         * dlatego Acrobat nie pokazuje panelu
         * Załączniki.
         */
        var hiddenData =
            TryReadHiddenData(
                pdfPath);

        if (hiddenData != null)
            return hiddenData;


        /*
         * FORMAT 1 / 2
         *
         * Fallback dla starszych kart COMMA Workspace,
         * które przechowywały comma-card.json
         * jako zwykły załącznik PDF.
         */
        using var document =
            PdfDocument.Open(
                pdfPath);

        if (!document.Advanced.TryGetEmbeddedFiles(
                out var embeddedFiles) ||
            embeddedFiles.Count == 0)
        {
            throw new InvalidOperationException(
                "Ten PDF nie zawiera danych COMMA Workspace.");
        }

        var commaFile =
            embeddedFiles.FirstOrDefault(file =>
                string.Equals(
                    file.Name,
                    EmbeddedDataFileName,
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    file.Name,
                    EmbeddedDataKey,
                    StringComparison.OrdinalIgnoreCase));

        if (commaFile != null)
        {
            var data =
                TryReadCommaData(
                    commaFile.Bytes.ToArray());

            if (data != null)
                return data;
        }

        foreach (var embeddedFile in embeddedFiles)
        {
            var data =
                TryReadCommaData(
                    embeddedFile.Bytes.ToArray());

            if (data != null)
                return data;
        }

        throw new InvalidOperationException(
            "PDF zawiera dane, ale nie znaleziono poprawnej karty COMMA Workspace.");
    }


    private static CommaCardData? TryReadHiddenData(
        string pdfPath)
    {
        try
        {
            var bytes =
                File.ReadAllBytes(
                    pdfPath);

            if (bytes.Length == 0)
                return null;

            /*
             * Latin1 mapuje każdy bajt 1:1 na znak,
             * dzięki czemu możemy bezpiecznie
             * przeszukać również binarny PDF.
             */
            var pdfText =
                Encoding.Latin1.GetString(
                    bytes);

            var beginIndex =
                pdfText.LastIndexOf(
                    HiddenDataBeginMarker,
                    StringComparison.Ordinal);

            var endIndex =
                beginIndex >= 0
                    ? pdfText.IndexOf(
                        HiddenDataEndMarker,
                        beginIndex + HiddenDataBeginMarker.Length,
                        StringComparison.Ordinal)
                    : pdfText.LastIndexOf(
                        HiddenDataEndMarker,
                        StringComparison.Ordinal);

            if (beginIndex < 0)
                return null;

            var dataStart =
                beginIndex +
                HiddenDataBeginMarker.Length;

            if (endIndex < 0 ||
                endIndex <= dataStart)
            {
                return null;
            }

            var encodedBlock =
                pdfText.Substring(
                    dataStart,
                    endIndex - dataStart);

            var base64Builder =
                new StringBuilder();

            var lines =
                encodedBlock.Split(
                    new[]
                    {
                        "\r\n",
                        "\n",
                        "\r"
                    },
                    StringSplitOptions.RemoveEmptyEntries);

            foreach (var rawLine in lines)
            {
                var line =
                    rawLine.Trim();

                if (string.IsNullOrWhiteSpace(
                        line))
                {
                    continue;
                }

                if (line.StartsWith(
                        "%",
                        StringComparison.Ordinal))
                {
                    line =
                        line.Substring(1);
                }

                line =
                    line.Trim();

                if (!string.IsNullOrWhiteSpace(
                        line))
                {
                    base64Builder.Append(
                        line);
                }
            }

            if (base64Builder.Length == 0)
                return null;

            var jsonBytes =
                Convert.FromBase64String(
                    base64Builder.ToString());

            return TryReadCommaData(
                jsonBytes);
        }
        catch
        {
            return null;
        }
    }


    private static CommaCardData? TryReadCommaData(
        byte[] bytes)
    {
        try
        {
            if (bytes.Length == 0)
                return null;

            var json =
                Encoding.UTF8.GetString(
                    bytes);

            var options =
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive =
                        true
                };

            var data =
                JsonSerializer.Deserialize<CommaCardData>(
                    json,
                    options);

            if (data == null)
                return null;

            if (!string.Equals(
                    data.Format,
                    ExpectedFormat,
                    StringComparison.Ordinal))
            {
                return null;
            }

            return data;
        }
        catch
        {
            return null;
        }
    }
}


public sealed class CommaCardData
{
    public string Format { get; set; } = "";

    public int FormatVersion { get; set; }

    public string ApplicationVersion { get; set; } = "";

    public DateTime SavedUtc { get; set; }

    public string OrderName { get; set; } = "";

    public string Customer { get; set; } = "";

    public string ReceivedDate { get; set; } = "";

    public string DueDate { get; set; } = "";

    public string ProductionType { get; set; } = "";


    /*
     * Pola starego formatu.
     *
     * Pozostają dla kompatybilności
     * z kartami COMMA Workspace 1.0 / 2.0.
     */
    public string ProductCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string Colour { get; set; } = "";

    public string Size { get; set; } = "";

    public string Quantity { get; set; } = "";

    public string Notes { get; set; } = "";

    public bool ShowFront { get; set; }

    public bool ShowBack { get; set; }

    public bool ShowLeft { get; set; }

    public bool ShowRight { get; set; }


    /*
     * FORMAT 3.0
     *
     * Wszystkie pozycje odzieży
     * w jednym zleceniu.
     */
    public List<CommaGarmentData> Garments
    {
        get;
        set;
    } = new();


    public List<CommaProductionEntryData> ProductionEntries
    {
        get;
        set;
    } = new();
}


public sealed class CommaGarmentData
{
    public string ProductCode { get; set; } = "";

    public string ProductName { get; set; } = "";

    public string Name { get; set; } = "";

    public string Colour { get; set; } = "";

    public string Variant { get; set; } = "";

    public bool ShowFront { get; set; }

    public bool ShowBack { get; set; }

    public bool ShowRight { get; set; }

    public bool ShowLeft { get; set; }

    public bool StartNewPage { get; set; }
}


public sealed class CommaProductionEntryData
{
    public int Number { get; set; }

    public string LogoName { get; set; } = "";

    public string Dimension { get; set; } = "";

    public List<CommaColourData> Colours
    {
        get;
        set;
    } = new();
}


public sealed class CommaColourData
{
    public int Number { get; set; }

    public string Value { get; set; } = "";
}
