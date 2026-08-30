using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using COMMA.App.Layout;
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
                    Garments.Count == 0,
                descriptionTargetResolver:
                    CreateDescriptionTargetResolver(
                        Garments.Count,
                        replaceExisting: false));

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
                    index == 0,
                descriptionTargetResolver:
                    CreateDescriptionTargetResolver(
                        index,
                        replaceExisting: true));

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
                isFirstGarment: false,
                descriptionTargetResolver:
                    CreateDescriptionTargetResolver(
                        sourceIndex + 1,
                        replaceExisting: false));

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


    private Func<GarmentViewSelection, bool, GarmentViewDescriptionGeometrySet>
        CreateDescriptionTargetResolver(
            int targetIndex,
            bool replaceExisting)
    {
        return (selection, startNewPage) =>
            ResolveDescriptionTarget(
                targetIndex,
                replaceExisting,
                selection,
                startNewPage);
    }


    private GarmentViewDescriptionGeometrySet ResolveDescriptionTarget(
        int targetIndex,
        bool replaceExisting,
        GarmentViewSelection selection,
        bool startNewPage)
    {
        var planningTarget =
            CreatePlanningGarment(
                selection,
                targetIndex > 0 && startNewPage);
        var planningGarments =
            Garments.ToList();

        if (replaceExisting &&
            targetIndex >= 0 &&
            targetIndex < planningGarments.Count)
        {
            planningGarments[targetIndex] =
                planningTarget;
        }
        else
        {
            planningGarments.Insert(
                Math.Clamp(
                    targetIndex,
                    0,
                    planningGarments.Count),
                planningTarget);
        }

        var pages = OrderPageLayoutEngine.BuildPages(planningGarments);
        var fallbackTarget = GarmentViewDescriptionLayout.GetTarget(
            targetIndex == 0,
            Math.Min(2, Math.Max(1, selection.Count)));
        var fallback = GarmentViewDescriptionLayout.GetReferenceGeometry(fallbackTarget);

        DescriptionTargetGeometry Resolve(GarmentViewKind view)
        {
            var drawing = planningTarget.Drawings.First(candidate =>
                view switch
                {
                    GarmentViewKind.Front => candidate.IsFront,
                    GarmentViewKind.Back => candidate.IsBack,
                    GarmentViewKind.Right => candidate.IsRight,
                    _ => candidate.IsLeft
                });
            var plannedView = pages
                .SelectMany(page => page.Placements)
                .Where(placement => ReferenceEquals(placement.Garment, planningTarget))
                .SelectMany(placement => placement.Views)
                .FirstOrDefault(candidate => ReferenceEquals(candidate.Drawing, drawing));

            return plannedView?.Geometry ?? fallback;
        }

        return new GarmentViewDescriptionGeometrySet(
            Resolve(GarmentViewKind.Front),
            Resolve(GarmentViewKind.Back),
            Resolve(GarmentViewKind.Right),
            Resolve(GarmentViewKind.Left));
    }


    private static OrderGarmentItem CreatePlanningGarment(
        GarmentViewSelection selection,
        bool startNewPage)
    {
        var garment = new OrderGarmentItem
        {
            Name = "DESCRIPTION TARGET",
            StartNewPage = startNewPage,
            ShowFront = selection.Front,
            ShowBack = selection.Back,
            ShowRight = selection.Right,
            ShowLeft = selection.Left
        };

        garment.Drawings.AddRange(
        [
            new DrawingFile { IsFront = true },
            new DrawingFile { IsBack = true },
            new DrawingFile { IsRight = true },
            new DrawingFile { IsLeft = true }
        ]);

        return garment;
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
