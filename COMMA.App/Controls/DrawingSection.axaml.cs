using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services;

namespace COMMA.App.Controls;

public partial class DrawingSection : UserControl
{

    public static readonly StyledProperty<ProductionCard?> ProductionCardProperty =
        AvaloniaProperty.Register<DrawingSection, ProductionCard?>(
            nameof(ProductionCard));

    public static readonly StyledProperty<OrderGarmentItem?> GarmentProperty =
        AvaloniaProperty.Register<DrawingSection, OrderGarmentItem?>(
            nameof(Garment));

    public static readonly StyledProperty<DescriptionTargetGeometry>
        DescriptionGeometryProperty =
        AvaloniaProperty.Register<DrawingSection, DescriptionTargetGeometry>(
            nameof(DescriptionGeometry),
            GarmentViewDescriptionLayout.GetReferenceGeometry(
                DescriptionLayoutTarget.FirstPageTwoViews));

    public static readonly StyledProperty<OrderPageGarmentPlacement?> PlacementProperty =
        AvaloniaProperty.Register<DrawingSection, OrderPageGarmentPlacement?>(
            nameof(Placement));

    public static readonly StyledProperty<OrderPageLayout?> PageProperty =
        AvaloniaProperty.Register<DrawingSection, OrderPageLayout?>(
            nameof(Page));


    private ProductionCard? subscribedCard;
    private OrderGarmentItem? subscribedGarment;
    private bool isAttachedToVisualTree;

    public DrawingSection()
    {
        InitializeComponent();
        RebuildLayout();
    }


    public ProductionCard? ProductionCard
    {
        get => GetValue(ProductionCardProperty);
        set => SetValue(ProductionCardProperty, value);
    }


    public OrderGarmentItem? Garment
    {
        get => GetValue(GarmentProperty);
        set => SetValue(GarmentProperty, value);
    }


    public DescriptionTargetGeometry DescriptionGeometry
    {
        get => GetValue(DescriptionGeometryProperty);
        set => SetValue(DescriptionGeometryProperty, value);
    }

    public OrderPageGarmentPlacement? Placement
    {
        get => GetValue(PlacementProperty);
        set => SetValue(PlacementProperty, value);
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

        if (change.Property == ProductionCardProperty)
        {
            if (isAttachedToVisualTree)
            {
                SubscribeToProductionCard(
                    change.NewValue as ProductionCard);
            }

            RebuildLayout();
            return;
        }

        if (change.Property == GarmentProperty)
        {
            if (isAttachedToVisualTree)
            {
                SubscribeToGarment(
                    change.NewValue as OrderGarmentItem);
            }

            RebuildLayout();
            return;
        }

        if (change.Property == DescriptionGeometryProperty ||
            change.Property == PlacementProperty ||
            change.Property == PageProperty)
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

        SubscribeToProductionCard(
            ProductionCard);

        SubscribeToGarment(
            Garment);
    }


    protected override void OnDetachedFromVisualTree(
        VisualTreeAttachmentEventArgs e)
    {
        UnsubscribeFromProductionCard();
        UnsubscribeFromGarment();

        isAttachedToVisualTree =
            false;

        base.OnDetachedFromVisualTree(e);
    }


    private void SubscribeToProductionCard(
        ProductionCard? card)
    {
        if (ReferenceEquals(
                subscribedCard,
                card))
            return;

        UnsubscribeFromProductionCard();

        subscribedCard =
            card;

        if (subscribedCard != null)
        {
            subscribedCard.PropertyChanged +=
                OnProductionCardPropertyChanged;
        }
    }


    private void UnsubscribeFromProductionCard()
    {
        if (subscribedCard == null)
            return;

        subscribedCard.PropertyChanged -=
            OnProductionCardPropertyChanged;

        subscribedCard =
            null;
    }


    private void SubscribeToGarment(
        OrderGarmentItem? garment)
    {
        if (ReferenceEquals(
                subscribedGarment,
                garment))
            return;

        UnsubscribeFromGarment();

        subscribedGarment =
            garment;

        if (subscribedGarment != null)
        {
            subscribedGarment.PropertyChanged +=
                OnGarmentPropertyChanged;

            subscribedGarment.ViewDescriptions.PropertyChanged +=
                OnGarmentViewDescriptionPropertyChanged;
        }
    }


    private void UnsubscribeFromGarment()
    {
        if (subscribedGarment == null)
            return;

        subscribedGarment.PropertyChanged -=
            OnGarmentPropertyChanged;

        subscribedGarment.ViewDescriptions.PropertyChanged -=
            OnGarmentViewDescriptionPropertyChanged;

        subscribedGarment =
            null;
    }


    private void OnProductionCardPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ProductionCard.SelectedDrawings) ||
            e.PropertyName == nameof(ProductionCard.ShowFront) ||
            e.PropertyName == nameof(ProductionCard.ShowBack) ||
            e.PropertyName == nameof(ProductionCard.ShowLeft) ||
            e.PropertyName == nameof(ProductionCard.ShowRight))
        {
            RebuildLayout();
        }
    }


    private void OnGarmentPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrderGarmentItem.SelectedDrawings) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowFront) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowBack) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowLeft) ||
            e.PropertyName == nameof(OrderGarmentItem.ShowRight))
        {
            RebuildLayout();
        }
    }


    private void OnGarmentViewDescriptionPropertyChanged(
        object? sender,
        PropertyChangedEventArgs e)
    {
        RebuildLayout();
    }


    private void RebuildLayout()
    {
        if (DrawingHost == null)
            return;

        IReadOnlyList<DrawingLayoutRow> rows;

        if (Placement != null)
        {
            rows = DrawingLayoutEngine.GetRows(Placement.Drawings);
        }
        else if (Garment != null)
        {
            rows =
                DrawingLayoutEngine.GetRows(
                    Garment);
        }
        else if (ProductionCard != null)
        {
            rows =
                DrawingLayoutEngine.GetRows(
                    ProductionCard);
        }
        else
        {
            DrawingHost.Content =
                BuildEmptyLayout();

            return;
        }

        if (rows.Count == 0)
        {
            DrawingHost.Content =
                BuildEmptyLayout();

            return;
        }

        DrawingHost.Content =
            BuildRowsLayout(
                rows,
                Garment);
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
                        "WYBIERZ CO NAJMNIEJ JEDEN RZUT",

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


    private Control BuildRowsLayout(
        IReadOnlyList<DrawingLayoutRow> rows,
        OrderGarmentItem? garment)
    {
        var drawingCount =
            rows.Sum(row =>
                row.Second == null
                    ? 1
                    : 2);

        var cropDrawingImage =
            drawingCount >= 3;

        var grid =
            new Grid
            {
                RowSpacing =
                    6,

                ColumnSpacing =
                    6
            };

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        grid.ColumnDefinitions.Add(
            new ColumnDefinition(
                GridLength.Star));

        for (
            var rowIndex = 0;
            rowIndex < rows.Count;
            rowIndex++)
        {
            grid.RowDefinitions.Add(
                new RowDefinition(
                    GridLength.Star));

            var layoutRow =
                rows[rowIndex];
            var firstGeometry =
                GetDescriptionGeometry(
                    layoutRow.First);

            var firstDrawingBox =
                CreateDrawingBox(
                    layoutRow.First,
                    garment,
                    cropDrawingImage,
                    firstGeometry);

            Grid.SetRow(
                firstDrawingBox,
                rowIndex);

            Grid.SetColumn(
                firstDrawingBox,
                0);

            if (layoutRow.FirstColumnSpan == 2)
            {
                Grid.SetColumnSpan(
                    firstDrawingBox,
                    2);
            }

            grid.Children.Add(
                firstDrawingBox);

            if (layoutRow.Second == null)
                continue;

            var secondGeometry =
                GetDescriptionGeometry(
                    layoutRow.Second);

            var secondDrawingBox =
                CreateDrawingBox(
                    layoutRow.Second,
                    garment,
                    cropDrawingImage,
                    secondGeometry);

            Grid.SetRow(
                secondDrawingBox,
                rowIndex);

            Grid.SetColumn(
                secondDrawingBox,
                1);

            grid.Children.Add(
                secondDrawingBox);
        }

        return grid;
    }

    private DescriptionTargetGeometry GetDescriptionGeometry(
        DrawingFile drawing)
    {
        return Page != null && Placement != null
            ? GarmentViewDescriptionLayout.GetTargetGeometry(
                Page,
                Placement,
                drawing)
            : DescriptionGeometry;
    }


    private static DrawingBox CreateDrawingBox(
        DrawingFile drawing,
        OrderGarmentItem? garment,
        bool cropDrawingImage,
        DescriptionTargetGeometry descriptionGeometry)
    {
        var description =
            garment == null
                ? ""
                : GarmentViewDescriptionLayout.GetDescription(
                    garment,
                    drawing);
        var maximumDrawingHeight =
            GarmentViewDescriptionLayout.GetPreviewMaximumImageHeight(
                descriptionGeometry);
        var maximumDrawingWidth =
            GarmentViewDescriptionLayout.GetPreviewMaximumImageWidth(
                descriptionGeometry);

        return new DrawingBox
        {
            Title =
                GetDrawingTitle(
                    drawing),

            Description =
                description,

            DescriptionFontSize =
                GarmentViewDescriptionLayout.PreviewLargeFontSize,

            DescriptionGeometry =
                descriptionGeometry,

            DescriptionTopMargin =
                cropDrawingImage
                    ? GarmentViewDescriptionLayout
                        .MultiDrawingPreviewDescriptionTopMargin
                    : 1,

            ImagePath =
                drawing.FullPath,

            ScaleX =
                drawing.MirrorHorizontally
                    ? -1
                    : 1,

            MaxDrawingWidth =
                maximumDrawingWidth,

            MaxDrawingHeight =
                maximumDrawingHeight,

            CroppedImageData =
                cropDrawingImage
                    ? DrawingImageCropper.TryCreateCroppedPng(
                        drawing.FullPath)
                    : null
        };
    }


    private static string GetDrawingTitle(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return "PRZÓD";

        if (drawing.IsBack)
            return "TYŁ";

        if (drawing.IsRight)
            return "PRAWY BOK";

        if (drawing.IsLeft)
            return "LEWY BOK";

        return "RYSUNEK TECHNICZNY";
    }
}
