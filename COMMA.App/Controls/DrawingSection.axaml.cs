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
using COMMA.App.Services.Pdf;

namespace COMMA.App.Controls;

public partial class DrawingSection : UserControl
{
    /*
     * Podgląd A4 ma 620 x 877.
     * PDF A4 ma 595.28 x 841.89 pt.
     *
     * Dzięki temu limity rysunków w podglądzie
     * odpowiadają rzeczywistej geometrii PDF.
     */
    private const double PreviewScale =
        620.0 / PdfStyles.PageWidth;


    public static readonly StyledProperty<ProductionCard?> ProductionCardProperty =
        AvaloniaProperty.Register<DrawingSection, ProductionCard?>(
            nameof(ProductionCard));

    public static readonly StyledProperty<OrderGarmentItem?> GarmentProperty =
        AvaloniaProperty.Register<DrawingSection, OrderGarmentItem?>(
            nameof(Garment));


    private ProductionCard? subscribedCard;
    private OrderGarmentItem? subscribedGarment;


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


    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == ProductionCardProperty)
        {
            SubscribeToProductionCard(
                change.NewValue as ProductionCard);

            RebuildLayout();
            return;
        }

        if (change.Property == GarmentProperty)
        {
            SubscribeToGarment(
                change.NewValue as OrderGarmentItem);

            RebuildLayout();
        }
    }


    private void SubscribeToProductionCard(
        ProductionCard? card)
    {
        if (subscribedCard != null)
        {
            subscribedCard.PropertyChanged -=
                OnProductionCardPropertyChanged;
        }

        subscribedCard =
            card;

        if (subscribedCard != null)
        {
            subscribedCard.PropertyChanged +=
                OnProductionCardPropertyChanged;
        }
    }


    private void SubscribeToGarment(
        OrderGarmentItem? garment)
    {
        if (subscribedGarment != null)
        {
            subscribedGarment.PropertyChanged -=
                OnGarmentPropertyChanged;
        }

        subscribedGarment =
            garment;

        if (subscribedGarment != null)
        {
            subscribedGarment.PropertyChanged +=
                OnGarmentPropertyChanged;
        }
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


    private void RebuildLayout()
    {
        if (DrawingHost == null)
            return;

        IReadOnlyList<DrawingLayoutRow> rows;

        if (Garment != null)
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
                rows);
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
        IReadOnlyList<DrawingLayoutRow> rows)
    {
        var drawingCount =
            rows.Sum(row =>
                row.Second == null
                    ? 1
                    : 2);

        var maxDrawingHeight =
            CalculatePdfEquivalentDrawingHeight(
                rows.Count,
                drawingCount);

        var limitDrawingWidth =
            drawingCount < 3;

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

            var firstDrawingBox =
                CreateDrawingBox(
                    layoutRow.First,
                    maxDrawingHeight,
                    limitDrawingWidth,
                    cropDrawingImage);

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

            var secondDrawingBox =
                CreateDrawingBox(
                    layoutRow.Second,
                    maxDrawingHeight,
                    limitDrawingWidth,
                    cropDrawingImage);

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


    private static double CalculatePdfEquivalentDrawingHeight(
        int rowCount,
        int drawingCount)
    {
        if (drawingCount >= 3)
        {
            return
                PdfStyles.MultiDrawingMaximumHeight *
                PreviewScale;
        }

        var rowHeight =
            PdfStyles.GetDrawingRowHeight(
                rowCount);

        var imageHeight =
            PdfStyles.GetDrawingImageHeight(
                rowHeight);

        /*
         * PDF DrawingSection.cs:
         *
         * MaxWidth(imageHeight * 0.75f)
         * MaxHeight(imageHeight * 0.75f)
         */
        var pdfMaximumSize =
            imageHeight *
            0.75;

        return
            pdfMaximumSize *
            PreviewScale;
    }


    private static DrawingBox CreateDrawingBox(
        DrawingFile drawing,
        double maxDrawingHeight,
        bool limitDrawingWidth,
        bool cropDrawingImage)
    {
        return new DrawingBox
        {
            Title =
                GetDrawingTitle(
                    drawing),

            ImagePath =
                drawing.FullPath,

            ScaleX =
                drawing.MirrorHorizontally
                    ? -1
                    : 1,

            MaxDrawingWidth =
                limitDrawingWidth
                    ? maxDrawingHeight
                    : double.PositiveInfinity,

            MaxDrawingHeight =
                maxDrawingHeight,

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
