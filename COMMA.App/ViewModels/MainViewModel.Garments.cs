using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using COMMA.App.Models;
using COMMA.App.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.App.ViewModels;

public partial class MainViewModel
{
    public ObservableCollection<OrderGarmentItem> Garments { get; } =
        new();


    [ObservableProperty]
    private OrderGarmentItem? selectedGarment;


    [RelayCommand]
    private async Task AddGarment()
    {
        ClearPdfStatus();

        if (SelectedProduct == null)
        {
            SetPdfStatus(
                "Najpierw wybierz produkt z biblioteki.");

            return;
        }

        var garment =
            new OrderGarmentItem();

        garment.LoadProduct(
            SelectedProduct);

        var window =
            GetMainWindow();

        if (window == null)
            return;

        var dialog =
            new GarmentEditorWindow(
                garment,
                isFirstGarment:
                    Garments.Count == 0);

        var result =
            await dialog.ShowDialog<bool>(
                window);

        if (!result)
            return;

        Garments.Add(
            garment);

        SelectedGarment =
            garment;

        RebuildOrderPages();

        SetPdfStatus(
            $"✓ Dodano odzież: {garment.DisplayName}");
    }


    [RelayCommand]
    private async Task EditGarment(
        OrderGarmentItem? garment)
    {
        ClearPdfStatus();

        if (garment == null)
            return;

        var window =
            GetMainWindow();

        if (window == null)
            return;

        var index =
            Garments.IndexOf(
                garment);

        if (index < 0)
            return;

        var dialog =
            new GarmentEditorWindow(
                garment,
                isFirstGarment:
                    index == 0);

        var result =
            await dialog.ShowDialog<bool>(
                window);

        if (!result)
            return;

        if (index == 0)
            garment.StartNewPage = false;

        SelectedGarment =
            garment;

        RebuildOrderPages();

        SetPdfStatus(
            $"✓ Zmieniono odzież: {garment.DisplayName}");
    }


    [RelayCommand]
    private async Task DuplicateGarment(
        OrderGarmentItem? garment)
    {
        ClearPdfStatus();

        if (garment == null)
            return;

        var sourceIndex =
            Garments.IndexOf(
                garment);

        if (sourceIndex < 0)
            return;

        var duplicate =
            garment.Duplicate();

        var window =
            GetMainWindow();

        if (window == null)
            return;

        var dialog =
            new GarmentEditorWindow(
                duplicate,
                isFirstGarment: false);

        var result =
            await dialog.ShowDialog<bool>(
                window);

        if (!result)
            return;

        var insertIndex =
            sourceIndex + 1;

        if (insertIndex >= Garments.Count)
        {
            Garments.Add(
                duplicate);
        }
        else
        {
            Garments.Insert(
                insertIndex,
                duplicate);
        }

        SelectedGarment =
            duplicate;

        RebuildOrderPages();

        SetPdfStatus(
            $"✓ Zduplikowano odzież: {duplicate.DisplayName}");
    }


    [RelayCommand]
    private async Task RemoveGarment(
        OrderGarmentItem? garment)
    {
        ClearPdfStatus();

        if (garment == null)
            return;

        var index =
            Garments.IndexOf(
                garment);

        if (index < 0)
            return;

        var window =
            GetMainWindow();

        if (window == null)
            return;

        var confirmed =
            await ShowRemoveGarmentConfirmation(
                window,
                garment);

        if (!confirmed)
            return;

        Garments.RemoveAt(
            index);

        if (Garments.Count == 0)
        {
            SelectedGarment =
                null;

            RebuildOrderPages();

            SetPdfStatus(
                "✓ Usunięto odzież ze zlecenia.");

            return;
        }

        if (index >= Garments.Count)
            index = Garments.Count - 1;

        SelectedGarment =
            Garments[index];

        Garments[0].StartNewPage =
            false;

        RebuildOrderPages();

        SetPdfStatus(
            "✓ Usunięto odzież ze zlecenia.");
    }


    private static async Task<bool> ShowRemoveGarmentConfirmation(
        Window owner,
        OrderGarmentItem garment)
    {
        var dialog =
            new Window
            {
                Title =
                    "Usuń odzież ze zlecenia",

                Width =
                    430,

                Height =
                    220,

                MinWidth =
                    430,

                MinHeight =
                    220,

                MaxWidth =
                    430,

                MaxHeight =
                    220,

                CanResize =
                    false,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner
            };


        var title =
            new TextBlock
            {
                Text =
                    "USUNĄĆ ODZIEŻ ZE ZLECENIA?",

                FontSize =
                    17,

                FontWeight =
                    FontWeight.Bold,

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center
            };


        var garmentName =
            string.IsNullOrWhiteSpace(
                garment.DisplayName)
                ? "Wybrana pozycja"
                : garment.DisplayName;


        var description =
            new TextBlock
            {
                Text =
                    $"{garmentName}\n\n" +
                    "Produkt zostanie usunięty tylko z tego zlecenia. " +
                    "Pozostanie w bibliotece COMMA.",

                FontSize =
                    12,

                TextWrapping =
                    TextWrapping.Wrap,

                TextAlignment =
                    TextAlignment.Center,

                HorizontalAlignment =
                    HorizontalAlignment.Stretch
            };


        var cancelButton =
            new Button
            {
                Content =
                    "ANULUJ",

                Width =
                    120,

                Height =
                    40,

                HorizontalContentAlignment =
                    HorizontalAlignment.Center,

                VerticalContentAlignment =
                    VerticalAlignment.Center
            };


        var removeButton =
            new Button
            {
                Content =
                    "USUŃ",

                Width =
                    120,

                Height =
                    40,

                HorizontalContentAlignment =
                    HorizontalAlignment.Center,

                VerticalContentAlignment =
                    VerticalAlignment.Center
            };


        cancelButton.Click +=
            (_, _) =>
                dialog.Close(
                    false);


        removeButton.Click +=
            (_, _) =>
                dialog.Close(
                    true);


        var buttons =
            new StackPanel
            {
                Orientation =
                    Orientation.Horizontal,

                Spacing =
                    12,

                HorizontalAlignment =
                    HorizontalAlignment.Center
            };


        buttons.Children.Add(
            cancelButton);

        buttons.Children.Add(
            removeButton);


        var content =
            new Grid
            {
                Margin =
                    new Thickness(
                        24),

                RowDefinitions =
                    new RowDefinitions(
                        "Auto,*,Auto"),

                RowSpacing =
                    18
            };


        Grid.SetRow(
            title,
            0);

        Grid.SetRow(
            description,
            1);

        Grid.SetRow(
            buttons,
            2);


        content.Children.Add(
            title);

        content.Children.Add(
            description);

        content.Children.Add(
            buttons);


        dialog.Content =
            content;


        return await dialog.ShowDialog<bool>(
            owner);
    }


    [RelayCommand]
    private void MoveGarmentUp(
        OrderGarmentItem? garment)
    {
        ClearPdfStatus();

        if (garment == null)
            return;

        var index =
            Garments.IndexOf(
                garment);

        if (index <= 0)
            return;

        Garments.Move(
            index,
            index - 1);

        Garments[0].StartNewPage =
            false;

        SelectedGarment =
            garment;

        RebuildOrderPages();
    }


    [RelayCommand]
    private void MoveGarmentDown(
        OrderGarmentItem? garment)
    {
        ClearPdfStatus();

        if (garment == null)
            return;

        var index =
            Garments.IndexOf(
                garment);

        if (index < 0 ||
            index >= Garments.Count - 1)
        {
            return;
        }

        Garments.Move(
            index,
            index + 1);

        Garments[0].StartNewPage =
            false;

        SelectedGarment =
            garment;

        RebuildOrderPages();
    }
}