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


    public static CommaOrderData Read(
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

        var hiddenData = TryReadMarkedData(
            pdfPath,
            OrderPdfV4DataEmbedder.HiddenDataBeginMarker,
            OrderPdfV4DataEmbedder.HiddenDataEndMarker,
            ReadV4Data);

        if (hiddenData != null)
            return hiddenData;

        hiddenData = TryReadMarkedData(
            pdfPath,
            HiddenDataBeginMarker,
            HiddenDataEndMarker,
            ReadV3Data);

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


    private static CommaOrderData? TryReadMarkedData(
        string pdfPath,
        string beginMarker,
        string endMarker,
        Func<byte[], CommaOrderData> readData)
    {
        var bytes = File.ReadAllBytes(pdfPath);

        if (bytes.Length == 0)
            return null;

            /*
             * Latin1 mapuje każdy bajt 1:1 na znak,
             * dzięki czemu możemy bezpiecznie
             * przeszukać również binarny PDF.
             */
        var pdfText = Encoding.Latin1.GetString(bytes);

        var beginIndex = pdfText.LastIndexOf(
            beginMarker,
            StringComparison.Ordinal);

        if (beginIndex < 0)
            return null;

        var dataStart = beginIndex + beginMarker.Length;
        var endIndex = pdfText.IndexOf(
            endMarker,
            dataStart,
            StringComparison.Ordinal);

        if (endIndex <= dataStart)
        {
            throw new InvalidDataException(
                "Dane COMMA Workspace w pliku PDF są niekompletne.");
        }

        var encodedBlock = pdfText.Substring(
            dataStart,
            endIndex - dataStart);

        var base64Builder = new StringBuilder();

        var lines = encodedBlock.Split(
            ["\r\n", "\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();

            if (line.StartsWith("%", StringComparison.Ordinal))
            {
                line = line.Substring(1).Trim();
            }

            if (line.Length > 0)
                base64Builder.Append(line);
        }

        if (base64Builder.Length == 0)
        {
            throw new InvalidDataException(
                "Dane COMMA Workspace w pliku PDF są puste.");
        }

        try
        {
            return readData(
                Convert.FromBase64String(base64Builder.ToString()));
        }
        catch (FormatException exception)
        {
            throw new InvalidDataException(
                "Dane COMMA Workspace w pliku PDF mają niepoprawne kodowanie.",
                exception);
        }
    }


    private static CommaOrderData? TryReadCommaData(
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

            using var document = JsonDocument.Parse(json);

            if (!document.RootElement.TryGetProperty(
                    nameof(CommaCardData.Format),
                    out var formatElement) ||
                !string.Equals(
                    formatElement.GetString(),
                    ExpectedFormat,
                    StringComparison.Ordinal))
            {
                return null;
            }

            if (!document.RootElement.TryGetProperty(
                    nameof(CommaCardData.FormatVersion),
                    out var versionElement) ||
                !versionElement.TryGetInt32(out var version))
            {
                return null;
            }

            if (version is not (1 or 2))
            {
                throw CreateUnsupportedVersionException(version);
            }

            var data = JsonSerializer.Deserialize<CommaCardData>(json, options);

            return data == null
                ? null
                : MapV3OrLegacy(data);
        }
        catch (NotSupportedException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static CommaOrderData ReadV4Data(byte[] bytes)
    {
        var data = Deserialize<CommaV4Manifest>(bytes);

        ValidateFormat(data.Format);

        if (data.FormatVersion != 4)
            throw CreateUnsupportedVersionException(data.FormatVersion);

        return MapV4(data);
    }

    private static CommaOrderData ReadV3Data(byte[] bytes)
    {
        var data = Deserialize<CommaCardData>(bytes);

        ValidateFormat(data.Format);

        if (data.FormatVersion != 3)
            throw CreateUnsupportedVersionException(data.FormatVersion);

        return MapV3OrLegacy(data);
    }

    private static T Deserialize<T>(byte[] bytes)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(
                       bytes,
                       new JsonSerializerOptions
                       {
                           PropertyNameCaseInsensitive = true
                       })
                   ?? throw new InvalidDataException(
                       "Dane COMMA Workspace w pliku PDF są puste.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException(
                "Dane COMMA Workspace w pliku PDF zawierają niepoprawny JSON.",
                exception);
        }
    }

    private static void ValidateFormat(string format)
    {
        if (!string.Equals(format, ExpectedFormat, StringComparison.Ordinal))
        {
            throw new InvalidDataException(
                "Plik zawiera dane w nierozpoznanym formacie.");
        }
    }

    private static NotSupportedException CreateUnsupportedVersionException(
        int version)
    {
        return new NotSupportedException(
            $"Nieobsługiwana wersja danych COMMA Workspace: {version}.");
    }

    private static CommaOrderData MapV4(CommaV4Manifest data)
    {
        return new CommaOrderData
        {
            Format = data.Format,
            FormatVersion = data.FormatVersion,
            ApplicationVersion = data.ApplicationVersion,
            SavedUtc = data.SavedUtc,
            OrderNumber = data.OrderNumber ?? "",
            OrderName = data.OrderName ?? "",
            Customer = data.Customer ?? "",
            ReceivedDate = data.ReceivedDate ?? "",
            DueDate = data.DueDate ?? "",
            ProductionType = data.ProductionType ?? "",
            ProductCode = data.ProductCode ?? "",
            ProductName = data.ProductName ?? "",
            Colour = data.Colour ?? "",
            Size = data.Size ?? "",
            Quantity = data.Quantity ?? "",
            Notes = data.Notes ?? "",
            ShowFront = data.ShowFront,
            ShowBack = data.ShowBack,
            ShowLeft = data.ShowLeft,
            ShowRight = data.ShowRight,
            Garments = data.Garments.Select(garment => new CommaOrderGarmentData
            {
                ProductCode = garment.ProductCode ?? "",
                ProductName = garment.ProductName ?? "",
                Name = garment.Name ?? "",
                Colour = garment.Colour ?? "",
                Variant = garment.Variant ?? "",
                ShowFront = garment.ShowFront,
                ShowBack = garment.ShowBack,
                ShowRight = garment.ShowRight,
                ShowLeft = garment.ShowLeft,
                StartNewPage = garment.StartNewPage,
                ViewDescriptions = new CommaOrderGarmentViewDescriptions
                {
                    Front = garment.ViewDescriptions?.Front ?? "",
                    Back = garment.ViewDescriptions?.Back ?? "",
                    Right = garment.ViewDescriptions?.Right ?? "",
                    Left = garment.ViewDescriptions?.Left ?? ""
                }
            }).ToList(),
            ProductionEntries = data.ProductionEntries.Select(MapProductionEntry).ToList(),
            Attachments = data.Attachments.Select(attachment => new CommaOrderAttachmentData
            {
                Id = attachment.Id,
                Name = attachment.Name ?? "",
                MimeType = attachment.MimeType ?? "",
                Extension = attachment.Extension ?? "",
                Order = attachment.Order,
                Length = attachment.Length,
                Sha256 = attachment.Sha256 ?? "",
                BlobEntry = attachment.BlobEntry ?? ""
            }).ToList()
        };
    }

    private static CommaOrderData MapV3OrLegacy(CommaCardData data)
    {
        return new CommaOrderData
        {
            Format = data.Format,
            FormatVersion = data.FormatVersion,
            ApplicationVersion = data.ApplicationVersion,
            SavedUtc = data.SavedUtc,
            OrderName = data.OrderName ?? "",
            Customer = data.Customer ?? "",
            ReceivedDate = data.ReceivedDate ?? "",
            DueDate = data.DueDate ?? "",
            ProductionType = data.ProductionType ?? "",
            ProductCode = data.ProductCode ?? "",
            ProductName = data.ProductName ?? "",
            Colour = data.Colour ?? "",
            Size = data.Size ?? "",
            Quantity = data.Quantity ?? "",
            Notes = data.Notes ?? "",
            ShowFront = data.ShowFront,
            ShowBack = data.ShowBack,
            ShowLeft = data.ShowLeft,
            ShowRight = data.ShowRight,
            Garments = data.Garments.Select(garment => new CommaOrderGarmentData
            {
                ProductCode = garment.ProductCode ?? "",
                ProductName = garment.ProductName ?? "",
                Name = garment.Name ?? "",
                Colour = garment.Colour ?? "",
                Variant = garment.Variant ?? "",
                ShowFront = garment.ShowFront,
                ShowBack = garment.ShowBack,
                ShowRight = garment.ShowRight,
                ShowLeft = garment.ShowLeft,
                StartNewPage = garment.StartNewPage
            }).ToList(),
            ProductionEntries = data.ProductionEntries.Select(entry =>
                new CommaOrderProductionEntryData
                {
                    Number = entry.Number,
                    LogoName = entry.LogoName ?? "",
                    Dimension = entry.Dimension ?? "",
                    Colours = entry.Colours.Select(colour => new CommaOrderColourData
                    {
                        Number = colour.Number,
                        Value = colour.Value ?? ""
                    }).ToList()
                }).ToList()
        };
    }

    private static CommaOrderProductionEntryData MapProductionEntry(
        CommaV4ProductionEntryData entry)
    {
        return new CommaOrderProductionEntryData
        {
            Number = entry.Number,
            LogoName = entry.LogoName ?? "",
            Dimension = entry.Dimension ?? "",
            Colours = entry.Colours.Select(colour => new CommaOrderColourData
            {
                Number = colour.Number,
                Value = colour.Value ?? ""
            }).ToList()
        };
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

public sealed class CommaOrderData
{
    public string Format { get; set; } = "";
    public int FormatVersion { get; set; }
    public string ApplicationVersion { get; set; } = "";
    public DateTime SavedUtc { get; set; }
    public string OrderNumber { get; set; } = "";
    public string OrderName { get; set; } = "";
    public string Customer { get; set; } = "";
    public string ReceivedDate { get; set; } = "";
    public string DueDate { get; set; } = "";
    public string ProductionType { get; set; } = "";
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
    public List<CommaOrderGarmentData> Garments { get; set; } = new();
    public List<CommaOrderProductionEntryData> ProductionEntries { get; set; } = new();
    public List<CommaOrderAttachmentData> Attachments { get; set; } = new();
}

public sealed class CommaOrderGarmentData
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
    public CommaOrderGarmentViewDescriptions ViewDescriptions { get; set; } = new();
}

public sealed class CommaOrderGarmentViewDescriptions
{
    public string Front { get; set; } = "";
    public string Back { get; set; } = "";
    public string Right { get; set; } = "";
    public string Left { get; set; } = "";
}

public sealed class CommaOrderProductionEntryData
{
    public int Number { get; set; }
    public string LogoName { get; set; } = "";
    public string Dimension { get; set; } = "";
    public List<CommaOrderColourData> Colours { get; set; } = new();
}

public sealed class CommaOrderColourData
{
    public int Number { get; set; }
    public string Value { get; set; } = "";
}

public sealed class CommaOrderAttachmentData
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string MimeType { get; set; } = "";
    public string Extension { get; set; } = "";
    public int Order { get; set; }
    public long Length { get; set; }
    public string Sha256 { get; set; } = "";
    public string BlobEntry { get; set; } = "";
}
