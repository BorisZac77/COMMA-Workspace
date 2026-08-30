using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using COMMA.App.Layout;
using COMMA.App.Models;

namespace COMMA.App.Views;

public partial class GarmentEditorWindow : Window
{
    private const double DefaultWidth = 720;
    private const double DefaultHeight = 780;
    private const double WindowsWorkingAreaMargin = 32;

    private readonly OrderGarmentItem garment;
    private readonly bool isFirstGarment;
    private readonly Func<GarmentViewSelection, bool, GarmentViewDescriptionGeometrySet>?
        descriptionTargetResolver;
    private readonly Dictionary<TextBox, GarmentViewDescriptionTextBoxController>
        descriptionInputControllers =
        new();

    private GarmentViewSelection acceptedDrawingSelection;
    private GarmentViewDescriptionGeometrySet currentDescriptionGeometries;
    private bool acceptedStartNewPage;
    private bool isRevertingDrawingSelection;
    private bool isRevertingStartNewPage;
    private bool isShowingLayoutMessage;

    public GarmentEditorWindow()
        : this(new OrderGarmentItem())
    {
    }

    public GarmentEditorWindow(
        OrderGarmentItem garment,
        bool isFirstGarment = false,
        Func<GarmentViewSelection, bool, GarmentViewDescriptionGeometrySet>? descriptionTargetResolver = null)
    {
        InitializeComponent();

        this.garment = garment;
        this.isFirstGarment =
            isFirstGarment;
        this.descriptionTargetResolver =
            descriptionTargetResolver;

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

        FrontDescriptionTextBox.Text =
            garment.ViewDescriptions.Front;

        BackDescriptionTextBox.Text =
            garment.ViewDescriptions.Back;

        RightDescriptionTextBox.Text =
            garment.ViewDescriptions.Right;

        LeftDescriptionTextBox.Text =
            garment.ViewDescriptions.Left;

        StartNewPageCheckBox.IsChecked =
            garment.StartNewPage;

        if (isFirstGarment)
        {
            StartNewPageCheckBox.IsChecked =
                false;

            StartNewPageCheckBox.IsEnabled =
                false;
        }

        currentDescriptionGeometries =
            GetDescriptionGeometries(
                GetDrawingSelection(),
                GetStartNewPage());

        RegisterDescriptionTextBox(
            FrontDescriptionTextBox,
            GarmentViewKind.Front);
        RegisterDescriptionTextBox(
            BackDescriptionTextBox,
            GarmentViewKind.Back);
        RegisterDescriptionTextBox(
            RightDescriptionTextBox,
            GarmentViewKind.Right);
        RegisterDescriptionTextBox(
            LeftDescriptionTextBox,
            GarmentViewKind.Left);

        acceptedDrawingSelection =
            GetDrawingSelection();
        acceptedStartNewPage =
            GetStartNewPage();

        FrontCheckBox.IsCheckedChanged +=
            OnDrawingSelectionChanged;
        BackCheckBox.IsCheckedChanged +=
            OnDrawingSelectionChanged;
        RightCheckBox.IsCheckedChanged +=
            OnDrawingSelectionChanged;
        LeftCheckBox.IsCheckedChanged +=
            OnDrawingSelectionChanged;
        StartNewPageCheckBox.IsCheckedChanged +=
            OnStartNewPageChanged;

        CancelButton.Click +=
            (_, _) =>
            {
                Close(false);
            };

        SaveButton.Click +=
            async (_, _) =>
            {
                if (string.IsNullOrWhiteSpace(
                        garment.Name))
                {
                    return;
                }

                var frontDescription =
                    FrontDescriptionTextBox.Text ?? "";

                var backDescription =
                    BackDescriptionTextBox.Text ?? "";

                var rightDescription =
                    RightDescriptionTextBox.Text ?? "";

                var leftDescription =
                    LeftDescriptionTextBox.Text ?? "";

                var selection =
                    GetDrawingSelection();

                if (selection.Count == 0)
                    return;

                if (TryGetNonFittingSelectedDescription(
                        selection,
                        GetDescriptionGeometries(
                            selection,
                            GetStartNewPage()),
                        out var fieldName))
                {
                    await ShowDescriptionTooLongMessageAsync(
                        fieldName);

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

                garment.ViewDescriptions.Front =
                    frontDescription;

                garment.ViewDescriptions.Back =
                    backDescription;

                garment.ViewDescriptions.Right =
                    rightDescription;

                garment.ViewDescriptions.Left =
                    leftDescription;

                Close(true);
            };
    }

    public OrderGarmentItem Garment =>
        garment;


    private void RegisterDescriptionTextBox(
        TextBox textBox,
        GarmentViewKind view)
    {
        descriptionInputControllers[textBox] =
            new GarmentViewDescriptionTextBoxController(
                textBox,
                () => currentDescriptionGeometries.Get(view));
    }


    private async void OnDrawingSelectionChanged(
        object? sender,
        RoutedEventArgs e)
    {
        if (isRevertingDrawingSelection)
            return;

        var proposedSelection =
            GetDrawingSelection();
        var proposedGeometries =
            GetDescriptionGeometries(
                proposedSelection,
                GetStartNewPage());

        if (TryGetNonFittingSelectedDescription(
                proposedSelection,
                proposedGeometries,
                out var fieldName))
        {
            RestoreDrawingSelection(
                acceptedDrawingSelection);

            await ShowDescriptionLayoutMessageAsync(
                fieldName);

            return;
        }

        acceptedDrawingSelection =
            proposedSelection;
        currentDescriptionGeometries = proposedGeometries;
        UpdateDescriptionGeometries(proposedGeometries);
    }


    private async void OnStartNewPageChanged(
        object? sender,
        RoutedEventArgs e)
    {
        if (isRevertingStartNewPage)
            return;

        var selection =
            GetDrawingSelection();
        var proposedStartNewPage =
            GetStartNewPage();
        var proposedGeometries =
            GetDescriptionGeometries(
                selection,
                proposedStartNewPage);

        if (TryGetNonFittingSelectedDescription(
                selection,
                proposedGeometries,
                out var fieldName))
        {
            isRevertingStartNewPage =
                true;
            StartNewPageCheckBox.IsChecked =
                acceptedStartNewPage;
            isRevertingStartNewPage =
                false;

            await ShowDescriptionPageMessageAsync(
                fieldName);

            return;
        }

        acceptedStartNewPage =
            proposedStartNewPage;
        currentDescriptionGeometries = proposedGeometries;
        UpdateDescriptionGeometries(proposedGeometries);
    }


    private bool TryGetNonFittingSelectedDescription(
        GarmentViewSelection selection,
        GarmentViewDescriptionGeometrySet geometries,
        out string fieldName)
    {
        var descriptions = new[]
        {
            (Selected: selection.Front, Name: "FRONT", View: GarmentViewKind.Front, TextBox: FrontDescriptionTextBox),
            (Selected: selection.Back, Name: "BACK", View: GarmentViewKind.Back, TextBox: BackDescriptionTextBox),
            (Selected: selection.Right, Name: "RIGHT", View: GarmentViewKind.Right, TextBox: RightDescriptionTextBox),
            (Selected: selection.Left, Name: "LEFT", View: GarmentViewKind.Left, TextBox: LeftDescriptionTextBox)
        };

        foreach (var description in descriptions)
        {
            if (!description.Selected ||
                descriptionInputControllers[description.TextBox]
                    .IsCurrentTextValidForCommit(geometries.Get(description.View)))
            {
                continue;
            }

            fieldName =
                description.Name;

            return true;
        }

        fieldName =
            "";

        return false;
    }


    private void RestoreDrawingSelection(
        GarmentViewSelection selection)
    {
        isRevertingDrawingSelection =
            true;

        FrontCheckBox.IsChecked =
            selection.Front;
        BackCheckBox.IsChecked =
            selection.Back;
        RightCheckBox.IsChecked =
            selection.Right;
        LeftCheckBox.IsChecked =
            selection.Left;

        isRevertingDrawingSelection =
            false;
    }


    private async Task ShowDescriptionLayoutMessageAsync(
        string fieldName)
    {
        if (isShowingLayoutMessage)
            return;

        isShowingLayoutMessage =
            true;

        var dialog = new Window
        {
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,
            Title = "Opisy rzutów"
        };
        var message = new TextBlock
        {
            Text =
                $"Przed dodaniem kolejnego rzutu skróć opis {fieldName}, " +
                "aby mieścił się w układzie czterech rzutów.",
            TextWrapping =
                TextWrapping.Wrap,
            FontSize = 13
        };
        var closeButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment =
                HorizontalAlignment.Right
        };
        var content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 18
        };

        Grid.SetRow(
            message,
            0);
        Grid.SetRow(
            closeButton,
            1);

        content.Children.Add(
            message);
        content.Children.Add(
            closeButton);

        dialog.Content =
            content;

        closeButton.Click +=
            (_, _) => dialog.Close();

        await dialog.ShowDialog(
            this);

        isShowingLayoutMessage =
            false;
    }


    private async Task ShowDescriptionTooLongMessageAsync(
        string fieldName)
    {
        if (isShowingLayoutMessage)
            return;

        isShowingLayoutMessage =
            true;

        var dialog = new Window
        {
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,
            Title = "Opisy rzutów"
        };
        var message = new TextBlock
        {
            Text =
                $"Skróć opis {fieldName}, aby mieścił się " +
                "w dostępnej przestrzeni pod rysunkiem.",
            TextWrapping =
                TextWrapping.Wrap,
            FontSize = 13
        };
        var closeButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment =
                HorizontalAlignment.Right
        };
        var content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 18
        };

        Grid.SetRow(message, 0);
        Grid.SetRow(closeButton, 1);
        content.Children.Add(message);
        content.Children.Add(closeButton);
        dialog.Content = content;
        closeButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);

        isShowingLayoutMessage =
            false;
    }


    private async Task ShowDescriptionPageMessageAsync(
        string fieldName)
    {
        if (isShowingLayoutMessage)
            return;

        isShowingLayoutMessage =
            true;

        var dialog = new Window
        {
            Width = 460,
            Height = 190,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,
            Title = "Opisy rzutów"
        };
        var message = new TextBlock
        {
            Text =
                $"Przed zmianą położenia pozycji skróć opis {fieldName}, " +
                "aby mieścił się na docelowej stronie.",
            TextWrapping =
                TextWrapping.Wrap,
            FontSize = 13
        };
        var closeButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment =
                HorizontalAlignment.Right
        };
        var content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 18
        };

        Grid.SetRow(message, 0);
        Grid.SetRow(closeButton, 1);
        content.Children.Add(message);
        content.Children.Add(closeButton);
        dialog.Content = content;
        closeButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);

        isShowingLayoutMessage =
            false;
    }


    private GarmentViewSelection GetDrawingSelection()
    {
        return new GarmentViewSelection(
            FrontCheckBox.IsChecked == true,
            BackCheckBox.IsChecked == true,
            RightCheckBox.IsChecked == true,
            LeftCheckBox.IsChecked == true);
    }


    private bool GetStartNewPage()
    {
        return !isFirstGarment &&
               StartNewPageCheckBox.IsChecked == true;
    }


    private GarmentViewDescriptionGeometrySet GetDescriptionGeometries(
        GarmentViewSelection selection,
        bool startNewPage)
    {
        if (descriptionTargetResolver != null)
        {
            return descriptionTargetResolver(
                selection,
                startNewPage);
        }

        if (!isFirstGarment || selection.Count <= 2)
        {
            var target = GarmentViewDescriptionLayout.GetTarget(
                isFirstGarment,
                selection.Count);
            var geometry = GarmentViewDescriptionLayout.GetReferenceGeometry(target);
            return new GarmentViewDescriptionGeometrySet(
                geometry, geometry, geometry, geometry);
        }

        var first = GarmentViewDescriptionLayout.GetReferenceGeometry(
            DescriptionLayoutTarget.FirstPageTwoViews);
        var later = GarmentViewDescriptionLayout.GetReferenceGeometry(
            DescriptionLayoutTarget.LaterPageTwoViews);
        var selectedIndex = 0;

        DescriptionTargetGeometry Next(bool selected)
        {
            if (!selected)
                return first;

            return selectedIndex++ < 2 ? first : later;
        }

        return new GarmentViewDescriptionGeometrySet(
            Next(selection.Front),
            Next(selection.Back),
            Next(selection.Right),
            Next(selection.Left));
    }

    private void UpdateDescriptionGeometries(
        GarmentViewDescriptionGeometrySet geometries)
    {
        descriptionInputControllers[FrontDescriptionTextBox]
            .UpdateGeometry(geometries.Front);
        descriptionInputControllers[BackDescriptionTextBox]
            .UpdateGeometry(geometries.Back);
        descriptionInputControllers[RightDescriptionTextBox]
            .UpdateGeometry(geometries.Right);
        descriptionInputControllers[LeftDescriptionTextBox]
            .UpdateGeometry(geometries.Left);
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
