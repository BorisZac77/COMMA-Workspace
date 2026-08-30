using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using PdfSharp;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace COMMA.App.Services.Pdf;

public static class OrderAttachmentPdfComposer
{
    public const double ImagePageMarginPoints = 18d;

    public static void Compose(
        string productionCardPdfPath,
        string outputPath,
        IReadOnlyCollection<OrderAttachmentMetadata> attachments,
        OrderAttachmentContentStore attachmentContentStore)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productionCardPdfPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(attachments);
        ArgumentNullException.ThrowIfNull(attachmentContentStore);

        if (attachments.Count == 0)
        {
            File.Copy(
                productionCardPdfPath,
                outputPath,
                overwrite: true);
            return;
        }

        using var output = new PdfDocument();
        using (var card = PdfReader.Open(
                   productionCardPdfPath,
                   PdfDocumentOpenMode.Import))
        {
            foreach (var page in card.Pages)
                output.AddPage(page);
        }

        foreach (var attachment in attachments.OrderBy(item => item.Order))
        {
            using var content = attachmentContentStore.OpenRead(attachment.Id);

            if (string.Equals(
                    attachment.Extension,
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                AppendPdf(output, content);
            }
            else
            {
                AppendImage(output, content);
            }
        }

        output.Save(outputPath);
    }

    private static void AppendPdf(
        PdfDocument output,
        Stream content)
    {
        using var attachment = PdfReader.Open(
            content,
            PdfDocumentOpenMode.Import);

        foreach (var page in attachment.Pages)
            output.AddPage(page);
    }

    private static void AppendImage(
        PdfDocument output,
        Stream content)
    {
        var page = output.AddPage();
        page.Size = PageSize.A4;
        page.Orientation = PageOrientation.Portrait;

        using var graphics = XGraphics.FromPdfPage(page);
        graphics.DrawRectangle(
            XBrushes.White,
            0,
            0,
            page.Width.Point,
            page.Height.Point);

        using var image = XImage.FromStream(content);
        var availableWidth =
            page.Width.Point - ImagePageMarginPoints * 2d;
        var availableHeight =
            page.Height.Point - ImagePageMarginPoints * 2d;
        var scale = Math.Min(
            availableWidth / image.PointWidth,
            availableHeight / image.PointHeight);
        var width = image.PointWidth * scale;
        var height = image.PointHeight * scale;
        var left = (page.Width.Point - width) / 2d;
        var top = (page.Height.Point - height) / 2d;

        graphics.DrawImage(
            image,
            left,
            top,
            width,
            height);
    }
}
