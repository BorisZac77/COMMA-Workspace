using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using COMMA.App.Layout;
using COMMA.App.Models;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.App.ViewModels;

public partial class MainViewModel
{
    private int previewPageIndex;


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
            if (OrderPages.Count == 0)
                return "";

            return
                $"{PreviewPageIndex + 1} / {OrderPages.Count}";
        }
    }


    public bool CanGoToPreviousPreviewPage =>
        OrderPages.Count > 0 &&
        PreviewPageIndex > 0;


    public bool CanGoToNextPreviewPage =>
        OrderPages.Count > 0 &&
        PreviewPageIndex <
        OrderPages.Count - 1;


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

        productionCard.Attachments.Clear();

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
                 OrderPages.Count)
        {
            previewPageIndex =
                OrderPages.Count - 1;
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


    private void NotifyPreviewPageChanged()
    {
        OnPropertyChanged(
            nameof(PreviewPageIndex));

        OnPropertyChanged(
            nameof(PreviewPage));

        OnPropertyChanged(
            nameof(PreviewPageNumberText));

        OnPropertyChanged(
            nameof(CanGoToPreviousPreviewPage));

        OnPropertyChanged(
            nameof(CanGoToNextPreviewPage));
    }
}
