using System;
using Avalonia.Controls;
using COMMA.App.Models;

namespace COMMA.App.Views;

public partial class GarmentEditorWindow : Window
{
    private const double DefaultWidth = 720;
    private const double DefaultHeight = 780;
    private const double WindowsWorkingAreaMargin = 32;

    private readonly OrderGarmentItem garment;

    public GarmentEditorWindow()
        : this(new OrderGarmentItem())
    {
    }

    public GarmentEditorWindow(
        OrderGarmentItem garment,
        bool isFirstGarment = false)
    {
        InitializeComponent();

        this.garment = garment;

        if (OperatingSystem.IsWindows())
        {
            Classes.Add(
                "windows-compact");

            ApplyWindowsSize();

            Opened +=
                (_, _) => ApplyWindowsSize();
        }

        GarmentNameTextBox.Text =
            garment.Name;

        ColourTextBox.Text =
            garment.Colour;

        VariantTextBox.Text =
            garment.Variant;

        FrontCheckBox.IsChecked =
            garment.ShowFront;

        BackCheckBox.IsChecked =
            garment.ShowBack;

        RightCheckBox.IsChecked =
            garment.ShowRight;

        LeftCheckBox.IsChecked =
            garment.ShowLeft;

        StartNewPageCheckBox.IsChecked =
            garment.StartNewPage;

        if (isFirstGarment)
        {
            StartNewPageCheckBox.IsChecked =
                false;

            StartNewPageCheckBox.IsEnabled =
                false;
        }

        CancelButton.Click +=
            (_, _) =>
            {
                Close(false);
            };

        SaveButton.Click +=
            (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(
                        garment.Name))
                {
                    return;
                }

                garment.Colour =
                    ColourTextBox.Text?.Trim() ?? "";

                garment.Variant =
                    VariantTextBox.Text?.Trim() ?? "";

                garment.ShowFront =
                    FrontCheckBox.IsChecked == true;

                garment.ShowBack =
                    BackCheckBox.IsChecked == true;

                garment.ShowRight =
                    RightCheckBox.IsChecked == true;

                garment.ShowLeft =
                    LeftCheckBox.IsChecked == true;

                garment.StartNewPage =
                    !isFirstGarment &&
                    StartNewPageCheckBox.IsChecked == true;

                if (garment.SelectedDrawingCount == 0)
                {
                    return;
                }

                Close(true);
            };
    }

    public OrderGarmentItem Garment =>
        garment;


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
