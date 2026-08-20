using Avalonia.Controls;
using COMMA.App.Models;

namespace COMMA.App.Views;

public partial class GarmentEditorWindow : Window
{
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
}
