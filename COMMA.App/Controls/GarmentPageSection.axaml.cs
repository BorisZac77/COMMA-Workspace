using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using COMMA.App.Layout;
using COMMA.App.Models;

namespace COMMA.App.Controls;

public partial class GarmentPageSection : UserControl
{
    public static readonly StyledProperty<OrderPageLayout?> PageProperty =
        AvaloniaProperty.Register<GarmentPageSection, OrderPageLayout?>(
            nameof(Page));

    private OrderPageLayout? subscribedPage;
    private bool isAttachedToVisualTree;

    public GarmentPageSection()
    {
        InitializeComponent();
        RebuildLayout();
    }

    public OrderPageLayout? Page
    {
        get => GetValue(PageProperty);
        set => SetValue(PageProperty, value);
    }

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property != PageProperty)
            return;

        if (isAttachedToVisualTree)
        {
            SubscribeToPage(
                change.NewValue as OrderPageLayout);
        }

        RebuildLayout();
    }

    protected override void OnAttachedToVisualTree(
        VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (isAttachedToVisualTree)
            return;

        isAttachedToVisualTree =
            true;

        SubscribeToPage(
            Page);
    }

    protected override void OnDetachedFromVisualTree(
        VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromPage();

        isAttachedToVisualTree =
            false;

        base.OnDetachedFromVisualTree(e);
    }

    private void SubscribeToPage(
        OrderPageLayout? page)
    {
        if (ReferenceEquals(
                subscribedPage,
                page))
            return;

        UnsubscribeFromPage();

        subscribedPage =
            page;

        if (subscribedPage == null)
            return;

        foreach (var garment in subscribedPage.Garments)
        {
            garment.PropertyChanged +=
                OnGarmentPropertyChanged;
        }
    }

    private void UnsubscribeFromPage()
    {
        if (subscribedPage == null)
            return;

        foreach (var garment in subscribedPage.Garments)
        {
            garment.PropertyChanged -=
                OnGarmentPropertyChanged;
        }

        subscribedPage =
            null;
    }

    private void OnGarmentPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        RebuildLayout();
    }

    private void RebuildLayout()
    {
        if (PageHost == null)
            return;

        if (Page == null ||
            Page.Garments.Count == 0)
        {
            PageHost.Content =
                BuildEmptyLayout();

            return;
        }

        PageHost.Content =
            BuildPageLayout(
                Page);
    }

    private static Control BuildEmptyLayout()
    {
        return new Border
        {
            BorderBrush =
                Brushes.Black,

            BorderThickness =
                new Thickness(1),

            Child =
                new TextBlock
                {
                    Text =
                        "DODAJ ODZIEŻ DO ZLECENIA",

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    VerticalAlignment =
                        VerticalAlignment.Center,

                    FontSize =
                        15,

                    FontWeight =
                        FontWeight.Bold
                }
        };
    }

    private static Control BuildPageLayout(
        OrderPageLayout page)
    {
        return page.Garments.Count switch
        {
            1 => BuildSingleGarmentLayout(
                page.Garments[0]),

            2 => BuildTwoGarmentLayout(
                page.Garments[0],
                page.Garments[1]),

            3 => BuildThreeGarmentLayout(
                page.Garments[0],
                page.Garments[1],
                page.Garments[2]),

            _ => BuildFourGarmentLayout(
                page.Garments[0],
                page.Garments[1],
                page.Garments[2],
                page.Garments[3])
        };
    }

    private static Control BuildSingleGarmentLayout(
        OrderGarmentItem garment)
    {
        return CreateGarmentBox(
            garment);
    }

    private static Control BuildTwoGarmentLayout(
        OrderGarmentItem first,
        OrderGarmentItem second)
    {
        var grid =
            new Grid
            {
                RowSpacing =
                    6
            };

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        var firstBox =
            CreateGarmentBox(
                first);

        var secondBox =
            CreateGarmentBox(
                second);

        Grid.SetRow(
            firstBox,
            0);

        Grid.SetRow(
            secondBox,
            1);

        grid.Children.Add(
            firstBox);

        grid.Children.Add(
            secondBox);

        return grid;
    }

    private static Control BuildThreeGarmentLayout(
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third)
    {
        var garments =
            new[]
            {
                first,
                second,
                third
            };

        var totalDrawingCount =
            garments.Sum(
                garment =>
                    garment.SelectedDrawingCount);

        var twoDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.SelectedDrawingCount == 2)
                .ToList();

        var oneDrawingGarments =
            garments
                .Where(
                    garment =>
                        garment.SelectedDrawingCount == 1)
                .ToList();

        if (totalDrawingCount == 4 &&
            twoDrawingGarments.Count == 1 &&
            oneDrawingGarments.Count == 2)
        {
            return BuildBalancedThreeGarmentLayout(
                first,
                second,
                third,
                twoDrawingGarments[0],
                oneDrawingGarments);
        }

        var grid =
            new Grid
            {
                RowSpacing =
                    6,

                ColumnSpacing =
                    6
            };

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        var firstBox =
            CreateGarmentBox(
                first);

        Grid.SetRow(
            firstBox,
            0);

        Grid.SetColumn(
            firstBox,
            0);

        Grid.SetColumnSpan(
            firstBox,
            2);

        var secondBox =
            CreateGarmentBox(
                second);

        Grid.SetRow(
            secondBox,
            1);

        Grid.SetColumn(
            secondBox,
            0);

        var thirdBox =
            CreateGarmentBox(
                third);

        Grid.SetRow(
            thirdBox,
            1);

        Grid.SetColumn(
            thirdBox,
            1);

        grid.Children.Add(
            firstBox);

        grid.Children.Add(
            secondBox);

        grid.Children.Add(
            thirdBox);

        return grid;
    }

    private static Control BuildBalancedThreeGarmentLayout(
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third,
        OrderGarmentItem twoDrawingGarment,
        System.Collections.Generic.IReadOnlyList<OrderGarmentItem> oneDrawingGarments)
    {
        var grid =
            new Grid
            {
                RowSpacing =
                    6,

                ColumnSpacing =
                    6
            };

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        var twoDrawingBox =
            CreateGarmentBox(
                twoDrawingGarment);

        var firstSingleBox =
            CreateGarmentBox(
                oneDrawingGarments[0]);

        var secondSingleBox =
            CreateGarmentBox(
                oneDrawingGarments[1]);

        var twoDrawingGarmentIsFirst =
            ReferenceEquals(
                twoDrawingGarment,
                first);

        var twoDrawingGarmentIsThird =
            ReferenceEquals(
                twoDrawingGarment,
                third);

        if (twoDrawingGarmentIsFirst)
        {
            Grid.SetRow(
                twoDrawingBox,
                0);

            Grid.SetColumn(
                twoDrawingBox,
                0);

            Grid.SetColumnSpan(
                twoDrawingBox,
                2);

            Grid.SetRow(
                firstSingleBox,
                1);

            Grid.SetColumn(
                firstSingleBox,
                0);

            Grid.SetRow(
                secondSingleBox,
                1);

            Grid.SetColumn(
                secondSingleBox,
                1);
        }
        else
        {
            Grid.SetRow(
                firstSingleBox,
                0);

            Grid.SetColumn(
                firstSingleBox,
                0);

            Grid.SetRow(
                secondSingleBox,
                0);

            Grid.SetColumn(
                secondSingleBox,
                1);

            Grid.SetRow(
                twoDrawingBox,
                1);

            Grid.SetColumn(
                twoDrawingBox,
                0);

            Grid.SetColumnSpan(
                twoDrawingBox,
                2);
        }

        grid.Children.Add(
            firstSingleBox);

        grid.Children.Add(
            secondSingleBox);

        grid.Children.Add(
            twoDrawingBox);

        return grid;
    }

    private static Control BuildFourGarmentLayout(
        OrderGarmentItem first,
        OrderGarmentItem second,
        OrderGarmentItem third,
        OrderGarmentItem fourth)
    {
        var grid =
            new Grid
            {
                RowSpacing =
                    6,

                ColumnSpacing =
                    6
            };

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.RowDefinitions.Add(
            new RowDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        var garments =
            new[]
            {
                first,
                second,
                third,
                fourth
            };

        for (
            var index = 0;
            index < garments.Length;
            index++)
        {
            var box =
                CreateGarmentBox(
                    garments[index]);

            Grid.SetRow(
                box,
                index / 2);

            Grid.SetColumn(
                box,
                index % 2);

            grid.Children.Add(
                box);
        }

        return grid;
    }

    private static Control CreateGarmentBox(
        OrderGarmentItem garment)
    {
        var grid =
            new Grid
            {
                RowDefinitions =
                    new RowDefinitions(
                        "30,*")
            };

        var titleBorder =
            new Border
            {
                BorderBrush =
                    Brushes.Black,

                BorderThickness =
                    new Thickness(1),

                Padding =
                    new Thickness(
                        5,
                        2)
            };

        titleBorder.Child =
            new TextBlock
            {
                Text =
                    garment.DisplayName,

                HorizontalAlignment =
                    HorizontalAlignment.Center,

                VerticalAlignment =
                    VerticalAlignment.Center,

                TextAlignment =
                    TextAlignment.Center,

                TextWrapping =
                    TextWrapping.Wrap,

                FontSize =
                    12,

                FontWeight =
                    FontWeight.Bold
            };

        Grid.SetRow(
            titleBorder,
            0);

        grid.Children.Add(
            titleBorder);

        var drawingSection =
            new DrawingSection
            {
                Garment =
                    garment
            };

        Grid.SetRow(
            drawingSection,
            1);

        grid.Children.Add(
            drawingSection);

        return grid;
    }
}
