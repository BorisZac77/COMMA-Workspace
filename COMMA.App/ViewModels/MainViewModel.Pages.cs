using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Media.Imaging;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.App.ViewModels;

public partial class MainViewModel
{
    private int previewPageIndex;

    private readonly List<AttachmentPreviewPage> attachmentPreviewPages =
        new();

    private Bitmap? previewAttachmentImage;

    private double previewAttachmentPageWidth = 620d;

    private double previewAttachmentPageHeight = 877d;


    public ObservableCollection<OrderPageLayout> OrderPages { get; } =
        new();


    public int PreviewPageIndex
    {
        get => previewPageIndex;

        private set
        {
            if (previewPageIndex == value)
                return;

            previewPageIndex =
                value;

            NotifyPreviewPageChanged();
        }
    }


    public OrderPageLayout? PreviewPage
    {
        get
        {
            if (OrderPages.Count == 0)
                return null;

            if (PreviewPageIndex < 0 ||
                PreviewPageIndex >= OrderPages.Count)
            {
                return null;
            }

            return OrderPages[
                PreviewPageIndex];
        }
    }


    public Bitmap? PreviewAttachmentImage =>
        previewAttachmentImage;


    public double PreviewAttachmentPageWidth =>
        previewAttachmentPageWidth;


    public double PreviewAttachmentPageHeight =>
        previewAttachmentPageHeight;


    public bool IsProductionCardPreviewPage =>
        PreviewPageIndex < OrderPages.Count;


    public bool IsAttachmentPreviewPage =>
        PreviewPageIndex >= OrderPages.Count &&
        PreviewPageIndex < PreviewPhysicalPageCount;


    public int PreviewPhysicalPageCount =>
        OrderPages.Count + attachmentPreviewPages.Count;


    public int OrderPageCount =>
        OrderPages.Count;


    public string OrderPageCountText =>
        OrderPageCount switch
        {
            0 => "Brak stron",
            1 => "1 strona",
            2 or 3 or 4 =>
                $"{OrderPageCount} strony",
            _ =>
                $"{OrderPageCount} stron"
        };


    public string PreviewPageNumberText
    {
        get
        {
            if (PreviewPhysicalPageCount == 0)
                return "";

            return
                $"{PreviewPageIndex + 1} / {PreviewPhysicalPageCount}";
        }
    }


    public bool CanGoToPreviousPreviewPage =>
        PreviewPhysicalPageCount > 0 &&
        PreviewPageIndex > 0;


    public bool CanGoToNextPreviewPage =>
        PreviewPhysicalPageCount > 0 &&
        PreviewPageIndex <
        PreviewPhysicalPageCount - 1;


    public void ClearCurrentOrder()
    {
        if (ProductionCard is not { } productionCard)
            return;

        productionCard.Customer =
            string.Empty;

        productionCard.OrderName =
            string.Empty;

        productionCard.OrderNumber =
            string.Empty;

        productionCard.DueDate =
            string.Empty;

        productionCard.ProductionType =
            string.Empty;

        AttachmentManager.Clear(
            productionCard.Attachments);

        Garments.Clear();

        SelectedGarment =
            null;

        previewPageIndex =
            0;

        RebuildOrderPages();

        OnPropertyChanged(
            nameof(OrderPageCount));

        OnPropertyChanged(
            nameof(OrderPageCountText));

        NotifyPreviewPageChanged();
    }


    private void InitializePageLayoutTracking()
    {
        Garments.CollectionChanged +=
            OnGarmentsCollectionChanged;

        RebuildOrderPages();
    }


    private void OnGarmentsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        if (e.OldItems != null)
        {
            foreach (var oldItem in e.OldItems)
            {
                if (oldItem is OrderGarmentItem garment)
                {
                    garment.PropertyChanged -=
                        OnGarmentLayoutPropertyChanged;
                }
            }
        }

        if (e.NewItems != null)
        {
            foreach (var newItem in e.NewItems)
            {
                if (newItem is OrderGarmentItem garment)
                {
                    garment.PropertyChanged +=
                        OnGarmentLayoutPropertyChanged;
                }
            }
        }

        RebuildOrderPages();
    }


    private void OnGarmentLayoutPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrderGarmentItem.SelectedDrawings) ||
            e.PropertyName == nameof(OrderGarmentItem.SelectedDrawingCount) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowFront) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowBack) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowRight) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowLeft) ||
            e.PropertyName == nameof(OrderGarmentItem.StartNewPage) ||
            e.PropertyName == nameof(OrderGarmentItem.DisplayName))
        {
            RebuildOrderPages();
        }
    }


    [RelayCommand]
    private void PreviousPreviewPage()
    {
        if (!CanGoToPreviousPreviewPage)
            return;

        PreviewPageIndex--;
    }


    [RelayCommand]
    private void NextPreviewPage()
    {
        if (!CanGoToNextPreviewPage)
            return;

        PreviewPageIndex++;
    }


    private void RebuildOrderPages()
    {
        var pages =
            OrderPageLayoutEngine.BuildPages(
                Garments);

        OrderPages.Clear();

        foreach (var page in pages)
        {
            OrderPages.Add(
                page);
        }

        if (OrderPages.Count == 0)
        {
            previewPageIndex =
                0;
        }
        else if (previewPageIndex >=
                 PreviewPhysicalPageCount)
        {
            previewPageIndex =
                Math.Max(
                    0,
                    PreviewPhysicalPageCount - 1);
        }
        else if (previewPageIndex < 0)
        {
            previewPageIndex =
                0;
        }

        OnPropertyChanged(
            nameof(OrderPageCount));

        OnPropertyChanged(
            nameof(OrderPageCountText));

        NotifyPreviewPageChanged();
    }


    private void RebuildAttachmentPreviewPages()
    {
        attachmentPreviewPages.Clear();

        if (ProductionCard is { } card)
        {
            foreach (var attachment in card.Attachments
                         .OrderBy(item => item.Order))
            {
                var pageCount = string.Equals(
                    attachment.Extension,
                    ".pdf",
                    StringComparison.OrdinalIgnoreCase)
                    ? attachment.PdfPageCount ?? 0
                    : 1;

                for (var pageIndex = 0;
                     pageIndex < pageCount;
                     pageIndex++)
                {
                    attachmentPreviewPages.Add(
                        new AttachmentPreviewPage(
                            attachment,
                            pageIndex));
                }
            }
        }

        if (previewPageIndex >= PreviewPhysicalPageCount)
        {
            previewPageIndex = Math.Max(
                0,
                PreviewPhysicalPageCount - 1);
        }

        NotifyPreviewPageChanged();
    }


    private void UpdatePreviewAttachmentImage()
    {
        previewAttachmentImage?.Dispose();
        previewAttachmentImage = null;
        previewAttachmentPageWidth = 620d;
        previewAttachmentPageHeight = 877d;

        var attachmentIndex =
            PreviewPageIndex - OrderPages.Count;

        if (attachmentIndex < 0 ||
            attachmentIndex >= attachmentPreviewPages.Count)
        {
            return;
        }

        var descriptor =
            attachmentPreviewPages[attachmentIndex];

        try
        {
            using var content =
                AttachmentManager.ContentStore.OpenRead(
                    descriptor.Attachment.Id);
            var rendered =
                OrderAttachmentPreviewRenderer.Render(
                    content,
                    descriptor.Attachment.Extension,
                    descriptor.PageIndex);

            using var imageStream = new MemoryStream(
                rendered.PngBytes,
                writable: false);
            previewAttachmentImage = new Bitmap(imageStream);
            previewAttachmentPageWidth = rendered.Width;
            previewAttachmentPageHeight = rendered.Height;
        }
        catch
        {
            previewAttachmentImage = null;
        }
    }


    private void NotifyPreviewPageChanged()
    {
        UpdatePreviewAttachmentImage();

        OnPropertyChanged(
            nameof(PreviewPageIndex));

        OnPropertyChanged(
            nameof(PreviewPage));

        OnPropertyChanged(
            nameof(PreviewAttachmentImage));

        OnPropertyChanged(
            nameof(PreviewAttachmentPageWidth));

        OnPropertyChanged(
            nameof(PreviewAttachmentPageHeight));

        OnPropertyChanged(
            nameof(IsProductionCardPreviewPage));

        OnPropertyChanged(
            nameof(IsAttachmentPreviewPage));

        OnPropertyChanged(
            nameof(PreviewPhysicalPageCount));

        OnPropertyChanged(
            nameof(PreviewPageNumberText));

        OnPropertyChanged(
            nameof(CanGoToPreviousPreviewPage));

        OnPropertyChanged(
            nameof(CanGoToNextPreviewPage));
    }


    private sealed record AttachmentPreviewPage(
        OrderAttachmentMetadata Attachment,
        int PageIndex);
}
