using Avalonia.Controls;
using Avalonia.Interactivity;
using COMMA.App.Models;

namespace COMMA.App.Views;

public partial class ProductionEntriesWindow : Window
{
    public ProductionEntriesWindow()
    {
        InitializeComponent();

        SaveButton.Click +=
            OnSaveButtonClick;

        CancelButton.Click +=
            OnCancelButtonClick;

        ClearButton.Click +=
            OnClearButtonClick;
    }

    public ProductionEntriesWindow(
        ProductionCard productionCard)
        : this()
    {
        DataContext =
            productionCard;
    }

    private void OnAddColourButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not ProductionEntry entry)
            return;

        entry.AddColour();
    }

    private void OnRemoveColourButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (sender is not Button button)
            return;

        if (button.Tag is not ProductionColourEntry colour)
            return;

        if (DataContext is not ProductionCard card)
            return;

        foreach (var entry in card.ProductionEntries)
        {
            if (!entry.Colours.Contains(colour))
                continue;

            entry.RemoveColour(colour);

            break;
        }
    }

    private void OnClearButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not ProductionCard card)
            return;

        foreach (var entry in card.ProductionEntries)
        {
            entry.LogoName = "";
            entry.Dimension = "";
            entry.Colours.Clear();
        }
    }

    private void OnSaveButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(true);
    }

    private void OnCancelButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        Close(false);
    }
}