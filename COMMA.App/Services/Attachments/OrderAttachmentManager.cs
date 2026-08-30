using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Services.Attachments;

public sealed class OrderAttachmentManager : IDisposable
{
    public OrderAttachmentContentStore ContentStore { get; private set; } =
        new();

    public IReadOnlyList<string> AddFiles(
        IEnumerable<string> filePaths,
        ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        var errors = new List<string>();

        foreach (var filePath in filePaths)
        {
            try
            {
                AddFile(filePath, attachments);
            }
            catch (Exception exception)
            {
                errors.Add(exception.Message);
            }
        }

        NormalizeOrder(attachments);
        return errors;
    }

    public OrderAttachmentMetadata AddFile(
        string filePath,
        ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        if (attachments.Count >= OrderAttachmentLimits.MaximumAttachmentCount)
        {
            throw new InvalidDataException(
                "Można dodać maksymalnie 25 załączników.");
        }

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(
                "Nie znaleziono wybranego załącznika.",
                filePath);
        }

        var sourceLength = new FileInfo(filePath).Length;
        if (sourceLength > OrderAttachmentLimits.MaximumFileBytes)
        {
            throw new InvalidDataException(
                $"Plik „{Path.GetFileName(filePath)}” przekracza limit 50 MB.");
        }

        if (attachments.Sum(item => item.Length) + sourceLength >
            OrderAttachmentLimits.MaximumTotalBytes)
        {
            throw new InvalidDataException(
                "Łączny rozmiar załączników przekracza limit 200 MB.");
        }

        var extension =
            OrderAttachmentValidator.NormalizeExtension(
                Path.GetExtension(filePath));
        var id = Guid.NewGuid();
        var stored = ContentStore.ImportFile(id, filePath, extension);

        try
        {
            using var content = ContentStore.OpenRead(id);
            var validated = OrderAttachmentValidator.Validate(filePath, content);
            var totalPdfPages = attachments.Sum(item => item.PdfPageCount ?? 0) +
                                (validated.PdfPageCount ?? 0);

            if (totalPdfPages > OrderAttachmentLimits.MaximumTotalPdfPages)
            {
                throw new InvalidDataException(
                    "Łączna liczba stron załączników PDF przekracza limit 500.");
            }

            var metadata = new OrderAttachmentMetadata
            {
                Id = id,
                Name = Path.GetFileName(filePath),
                MimeType = validated.MimeType,
                Extension = validated.Extension,
                Order = attachments.Count,
                Length = stored.Length,
                Sha256 = stored.Sha256,
                BlobEntry = OrderAttachmentValidator.CreateBlobEntry(
                    id,
                    validated.Extension),
                PdfPageCount = validated.PdfPageCount
            };

            attachments.Add(metadata);
            return metadata;
        }
        catch
        {
            ContentStore.Remove(id);
            throw;
        }
    }

    public void Remove(
        OrderAttachmentMetadata attachment,
        ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        if (!attachments.Remove(attachment))
            return;

        ContentStore.Remove(attachment.Id);
        NormalizeOrder(attachments);
    }

    public bool Move(
        OrderAttachmentMetadata attachment,
        int offset,
        ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        var oldIndex = attachments.IndexOf(attachment);
        var newIndex = oldIndex + offset;

        if (oldIndex < 0 || newIndex < 0 || newIndex >= attachments.Count)
            return false;

        attachments.Move(oldIndex, newIndex);
        NormalizeOrder(attachments);
        return true;
    }

    public void Clear(ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        attachments.Clear();
        ContentStore.Clear();
    }

    public void ReplaceContentStore(OrderAttachmentContentStore? contentStore)
    {
        ContentStore.Dispose();
        ContentStore = contentStore ?? new OrderAttachmentContentStore();
    }

    public static void NormalizeOrder(
        ObservableCollection<OrderAttachmentMetadata> attachments)
    {
        for (var index = 0; index < attachments.Count; index++)
            attachments[index].Order = index;
    }

    public void Dispose()
    {
        ContentStore.Dispose();
    }
}
