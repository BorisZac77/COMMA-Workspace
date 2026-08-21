using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using COMMA.App.Models;

namespace COMMA.App.Views;

public partial class ProductionEntriesWindow : Window
{
    private const double DefaultWidth = 1180;
    private const double DefaultHeight = 760;
    private const double WindowsWorkingAreaMargin = 32;

    public ProductionEntriesWindow()
    {
        InitializeComponent();

        if (OperatingSystem.IsWindows())
        {
            CanResize = false;

            ApplyWindowsSize();

            Opened +=
                (_, _) => ApplyWindowsSize();
        }

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

    private void ApplyWindowsSize()
    {
        var screen =
            Screens.ScreenFromWindow(this)
            ?? Screens.Primary;

        if (screen == null)
            return;

        var workingAreaWidth =
            screen.WorkingArea.Width /
            screen.Scaling;

        var workingAreaHeight =
            screen.WorkingArea.Height /
            screen.Scaling;

        var targetWidth =
            Math.Min(
                DefaultWidth,
                Math.Max(
                    1,
                    workingAreaWidth - WindowsWorkingAreaMargin));

        var targetHeight =
            Math.Min(
                DefaultHeight,
                Math.Max(
                    1,
                    workingAreaHeight - WindowsWorkingAreaMargin));

        Width = targetWidth;
        MinWidth = targetWidth;
        MaxWidth = targetWidth;

        Height = targetHeight;
        MinHeight = targetHeight;
        MaxHeight = targetHeight;
    }
}
