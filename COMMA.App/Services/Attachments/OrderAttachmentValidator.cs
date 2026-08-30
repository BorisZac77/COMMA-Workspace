using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UglyToad.PdfPig;

namespace COMMA.App.Services.Attachments;

public static class OrderAttachmentValidator
{
    private static readonly HashSet<string> SupportedExtensions =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpg",
            ".jpeg",
            ".png"
        };

    public static AttachmentFileInfo Validate(
        string fileName,
        Stream content)
    {
        ArgumentNullException.ThrowIfNull(content);
        var extension = NormalizeExtension(Path.GetExtension(fileName));

        if (!SupportedExtensions.Contains(extension))
        {
            throw new InvalidDataException(
                $"Plik „{Path.GetFileName(fileName)}” ma nieobsługiwany typ. " +
                "Dozwolone są PDF, JPG, JPEG i PNG.");
        }

        if (!content.CanSeek)
        {
            throw new InvalidDataException(
                "Nie można sprawdzić zawartości załącznika.");
        }

        content.Position = 0;
        var signature = new byte[8];
        var signatureLength = content.Read(signature, 0, signature.Length);
        content.Position = 0;

        return extension switch
        {
            ".pdf" => ValidatePdf(fileName, content, signature, signatureLength),
            ".png" => ValidateImage(fileName, content, signature, signatureLength, true),
            ".jpg" or ".jpeg" =>
                ValidateImage(fileName, content, signature, signatureLength, false),
            _ => throw new InvalidDataException("Nieobsługiwany typ załącznika.")
        };
    }

    public static string NormalizeExtension(string? extension)
    {
        var value = (extension ?? "").Trim().ToLowerInvariant();
        if (value.Length > 0 && !value.StartsWith('.'))
            value = "." + value;
        return value;
    }

    public static string GetMimeType(string extension) =>
        NormalizeExtension(extension) switch
        {
            ".pdf" => "application/pdf",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };

    public static string CreateBlobEntry(Guid id, string extension) =>
        $"attachments/{id:N}{NormalizeExtension(extension)}";

    private static AttachmentFileInfo ValidatePdf(
        string fileName,
        Stream content,
        byte[] signature,
        int signatureLength)
    {
        if (signatureLength < 5 ||
            signature[0] != '%' || signature[1] != 'P' ||
            signature[2] != 'D' || signature[3] != 'F' ||
            signature[4] != '-')
        {
            throw SignatureMismatch(fileName, "PDF");
        }

        try
        {
            using var document = PdfDocument.Open(content);
            var pageCount = document.NumberOfPages;

            if (pageCount < 1)
            {
                throw new InvalidDataException(
                    $"Plik PDF „{Path.GetFileName(fileName)}” nie zawiera stron.");
            }

            if (pageCount > OrderAttachmentLimits.MaximumPdfPagesPerFile)
            {
                throw new InvalidDataException(
                    $"Plik PDF „{Path.GetFileName(fileName)}” przekracza limit 200 stron.");
            }

            content.Position = 0;
            return new AttachmentFileInfo("application/pdf", ".pdf", pageCount);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"Plik PDF „{Path.GetFileName(fileName)}” jest uszkodzony, " +
                "zaszyfrowany lub chroniony hasłem.",
                exception);
        }
    }

    private static AttachmentFileInfo ValidateImage(
        string fileName,
        Stream content,
        byte[] signature,
        int signatureLength,
        bool expectPng)
    {
        var isPng = signatureLength >= 8 &&
                    signature.AsSpan(0, 8).SequenceEqual(
                        new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 });
        var isJpeg = signatureLength >= 3 &&
                     signature[0] == 0xFF &&
                     signature[1] == 0xD8 &&
                     signature[2] == 0xFF;

        if ((expectPng && !isPng) || (!expectPng && !isJpeg))
            throw SignatureMismatch(fileName, expectPng ? "PNG" : "JPEG");

        try
        {
            var declaredPixels = expectPng
                ? ValidatePngStructure(content)
                : ValidateJpegStructure(content);

            if (declaredPixels > OrderAttachmentLimits.MaximumImagePixels)
            {
                throw new InvalidDataException(
                    $"Obraz „{Path.GetFileName(fileName)}” przekracza limit 100 megapikseli.");
            }

            content.Position = 0;
            var extension = NormalizeExtension(Path.GetExtension(fileName));
            return new AttachmentFileInfo(
                expectPng ? "image/png" : "image/jpeg",
                extension,
                null);
        }
        catch (InvalidDataException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new InvalidDataException(
                $"Obraz „{Path.GetFileName(fileName)}” jest uszkodzony.",
                exception);
        }
    }

    private static long ValidatePngStructure(Stream content)
    {
        content.Position = 8;
        var lengthBuffer = new byte[4];
        var typeBuffer = new byte[4];
        var dataBuffer = new byte[81920];
        var crcBuffer = new byte[4];
        var sawHeader = false;
        var sawImageData = false;
        var sawEnd = false;
        long pixels = 0;

        while (content.Position < content.Length)
        {
            ReadExactly(content, lengthBuffer);
            var chunkLength = ReadBigEndianUInt32(lengthBuffer);
            if (chunkLength > content.Length - content.Position - 8)
                throw new InvalidDataException("Plik PNG ma niepoprawną długość chunku.");

            ReadExactly(content, typeBuffer);
            var crc = UpdateCrc(0xFFFFFFFF, typeBuffer);
            uint remaining = chunkLength;
            byte[]? headerData = null;

            if (!sawHeader)
            {
                if (!typeBuffer.SequenceEqual("IHDR"u8.ToArray()) || chunkLength != 13)
                    throw new InvalidDataException("Plik PNG nie rozpoczyna się poprawnym IHDR.");
                headerData = new byte[13];
            }

            var headerOffset = 0;
            while (remaining > 0)
            {
                var readLength = (int)Math.Min((uint)dataBuffer.Length, remaining);
                var read = content.Read(dataBuffer, 0, readLength);
                if (read != readLength)
                    throw new EndOfStreamException();
                crc = UpdateCrc(crc, dataBuffer.AsSpan(0, read));

                if (headerData != null)
                {
                    dataBuffer.AsSpan(0, read).CopyTo(headerData.AsSpan(headerOffset));
                    headerOffset += read;
                }

                remaining -= (uint)read;
            }

            ReadExactly(content, crcBuffer);
            var expectedCrc = ReadBigEndianUInt32(crcBuffer);
            if ((crc ^ 0xFFFFFFFF) != expectedCrc)
                throw new InvalidDataException("Plik PNG zawiera uszkodzony chunk.");

            var chunkType = Encoding.ASCII.GetString(typeBuffer);
            if (chunkType == "IHDR")
            {
                if (sawHeader || headerData == null)
                    throw new InvalidDataException("Plik PNG zawiera niepoprawny IHDR.");
                var width = ReadBigEndianUInt32(headerData.AsSpan(0, 4));
                var height = ReadBigEndianUInt32(headerData.AsSpan(4, 4));
                pixels = checked((long)width * height);
                sawHeader = true;
            }
            else if (chunkType == "IDAT")
            {
                sawImageData = true;
            }
            else if (chunkType == "IEND")
            {
                if (chunkLength != 0)
                    throw new InvalidDataException("Plik PNG zawiera niepoprawny IEND.");
                sawEnd = true;
                break;
            }
        }

        if (!sawHeader || !sawImageData || !sawEnd || content.Position != content.Length)
            throw new InvalidDataException("Plik PNG jest niekompletny.");

        content.Position = 0;
        return pixels;
    }

    private static long ValidateJpegStructure(Stream content)
    {
        content.Position = 2;
        var lengthBytes = new byte[2];
        var frame = new byte[5];
        long pixels = 0;
        var sawFrame = false;
        var sawScan = false;

        while (content.Position < content.Length)
        {
            if (content.ReadByte() != 0xFF)
                continue;

            int marker;
            do
            {
                marker = content.ReadByte();
            }
            while (marker == 0xFF);

            if (marker < 0 || marker is 0xD8 or 0xD9)
                continue;

            ReadExactly(content, lengthBytes);

            var segmentLength = (lengthBytes[0] << 8) | lengthBytes[1];
            if (segmentLength < 2)
                throw new InvalidDataException("Plik JPEG zawiera niepoprawny segment.");

            if (marker is >= 0xC0 and <= 0xC3 or
                >= 0xC5 and <= 0xC7 or
                >= 0xC9 and <= 0xCB or
                >= 0xCD and <= 0xCF)
            {
                ReadExactly(content, frame);

                var height = (frame[1] << 8) | frame[2];
                var width = (frame[3] << 8) | frame[4];
                pixels = checked((long)width * height);
                sawFrame = true;
                content.Position += segmentLength - 2 - frame.Length;
                continue;
            }

            if (marker == 0xDA)
            {
                sawScan = true;
                content.Position += segmentLength - 2;
                break;
            }

            content.Position += segmentLength - 2;
        }

        var previous = -1;
        var sawEnd = false;
        while (content.Position < content.Length)
        {
            var current = content.ReadByte();
            if (previous == 0xFF && current == 0xD9)
            {
                sawEnd = true;
                break;
            }
            previous = current;
        }

        content.Position = 0;
        if (!sawFrame || !sawScan || !sawEnd)
            throw new InvalidDataException("Plik JPEG jest niekompletny.");
        return pixels;
    }

    private static uint ReadBigEndianUInt32(ReadOnlySpan<byte> value) =>
        ((uint)value[0] << 24) |
        ((uint)value[1] << 16) |
        ((uint)value[2] << 8) |
        value[3];

    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        var offset = 0;
        while (offset < buffer.Length)
        {
            var read = stream.Read(buffer, offset, buffer.Length - offset);
            if (read == 0)
                throw new EndOfStreamException();
            offset += read;
        }
    }

    private static uint UpdateCrc(uint crc, ReadOnlySpan<byte> bytes)
    {
        foreach (var value in bytes)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
                crc = (crc & 1) != 0 ? (crc >> 1) ^ 0xEDB88320 : crc >> 1;
        }

        return crc;
    }

    private static InvalidDataException SignatureMismatch(
        string fileName,
        string expectedType) =>
        new(
            $"Zawartość pliku „{Path.GetFileName(fileName)}” nie jest zgodna " +
            $"z rozszerzeniem {expectedType}.");
}

public readonly record struct AttachmentFileInfo(
    string MimeType,
    string Extension,
    int? PdfPageCount);
