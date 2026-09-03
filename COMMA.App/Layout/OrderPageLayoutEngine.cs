using System;
using System.Collections.Generic;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Layout;

public static class OrderPageLayoutEngine
{
    public static IReadOnlyList<OrderPageLayout> BuildPages(
        IEnumerable<OrderGarmentItem> garments)
    {
        ArgumentNullException.ThrowIfNull(garments);

        var items =
            garments
                .Where(garment =>
                    garment.SelectedDrawingCount > 0)
                .ToList();

        var pages =
            new List<OrderPageLayout>();

        OrderPageLayout? currentPage =
            null;

        for (var garmentIndex = 0;
             garmentIndex < items.Count;
             garmentIndex++)
        {
            var garment = items[garmentIndex];
            var drawings =
                DrawingLayoutEngine.GetSelectedDrawings(garment);
            var drawingIndex = 0;

            if (garment.StartNewPage &&
                currentPage?.DrawingCount > 0)
            {
                currentPage = null;
            }

            while (drawingIndex < drawings.Count)
            {
                if (currentPage == null)
                {
                    currentPage = new OrderPageLayout();
                    pages.Add(currentPage);
                }

                var pageCapacity =
                    ReferenceEquals(currentPage, pages[0])
                        ? 2
                        : 4;
                var availableSlots =
                    pageCapacity - currentPage.DrawingCount;

                var remainingDrawings =
                    drawings.Count - drawingIndex;

                /*
                 * Nowej pozycji nie dzielimy tylko po to, aby wypełnić
                 * wolne miejsca poprzedniej strony, jeśli wszystkie jej
                 * rzuty mieszczą się razem na pustej stronie kontynuacji.
                 * Nie dotyczy to rzutów pozycji już rozpoczętej.
                 */
                if (currentPage.DrawingCount > 0 &&
                    drawingIndex == 0 &&
                    remainingDrawings > availableSlots &&
                    remainingDrawings <= 4)
                {
                    currentPage = null;
                    continue;
                }

                var drawingsToTake =
                    Math.Min(
                        availableSlots,
                        remainingDrawings);

                currentPage.AddPlacement(
                    garment,
                    drawings
                        .Skip(drawingIndex)
                        .Take(drawingsToTake));

                drawingIndex += drawingsToTake;

                if (currentPage.DrawingCount == pageCapacity)
                    currentPage = null;
            }
        }

        ApplyPageNumbers(
            pages);

        return pages;
    }


    private static void ApplyPageNumbers(
        IReadOnlyList<OrderPageLayout> pages)
    {
        var totalPages =
            pages.Count;

        for (
            var index = 0;
            index < totalPages;
            index++)
        {
            var page =
                pages[index];

            page.PageNumber =
                index + 1;

            page.TotalPages =
                totalPages;
        }
    }
}


public sealed class OrderPageLayout
{
    private readonly List<OrderPageGarmentPlacement> placements = new();

    public IReadOnlyList<OrderPageGarmentPlacement> Placements => placements;

    public IReadOnlyList<OrderGarmentItem> Garments =>
        placements
            .Select(placement => placement.Garment)
            .Distinct()
            .ToList();

    internal void AddPlacement(
        OrderGarmentItem garment,
        IEnumerable<DrawingFile> drawings)
    {
        var selected = drawings.ToList();

        if (selected.Count == 0)
            return;

        placements.Add(
            new OrderPageGarmentPlacement(
                this,
                garment,
                selected));
    }


    public int PageNumber { get; internal set; }


    public int TotalPages { get; internal set; }


    public bool IsFirstPage =>
        PageNumber == 1;


    public bool UsesPairedFirstPageGarmentLayout =>
        IsFirstPage &&
        placements.Count == 2 &&
        placements.All(placement => placement.DrawingCount == 1);


    public string PageNumberText =>
        TotalPages > 0
            ? $"{PageNumber}/{TotalPages}"
            : "";


    public int DrawingCount =>
        Placements.Sum(placement => placement.DrawingCount);


    public int GarmentCount =>
        Placements.Count;


    public string GarmentNamesText =>
        string.Join(
            " + ",
            Placements
                .Select(placement => placement.Garment.DisplayName)
                .Distinct()
                .Where(name =>
                    !string.IsNullOrWhiteSpace(name)));


    public string DrawingCountText =>
        DrawingCount switch
        {
            0 => "Brak rzutów",
            1 => "1 rzut",
            2 or 3 or 4 =>
                $"{DrawingCount} rzuty",
            _ =>
                $"{DrawingCount} rzutów"
        };


    public string PreviewText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(
                    GarmentNamesText))
            {
                return PageNumberText;
            }

            return
                $"{PageNumberText} — {GarmentNamesText}";
        }
    }
}

public sealed class OrderPageGarmentPlacement
{
    public OrderPageGarmentPlacement(
        OrderPageLayout page,
        OrderGarmentItem garment,
        IReadOnlyList<DrawingFile> drawings)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        Garment = garment ?? throw new ArgumentNullException(nameof(garment));
        Drawings = drawings ?? throw new ArgumentNullException(nameof(drawings));
        Views = drawings
            .Select(drawing => new OrderPageDrawingPlacement(this, drawing))
            .ToList();
    }

    public OrderPageLayout Page { get; }

    public OrderGarmentItem Garment { get; }

    public IReadOnlyList<DrawingFile> Drawings { get; }

    public IReadOnlyList<OrderPageDrawingPlacement> Views { get; }

    public int DrawingCount => Drawings.Count;
}

public sealed class OrderPageDrawingPlacement
{
    internal OrderPageDrawingPlacement(
        OrderPageGarmentPlacement garmentPlacement,
        DrawingFile drawing)
    {
        GarmentPlacement = garmentPlacement;
        Drawing = drawing;
    }

    public OrderPageGarmentPlacement GarmentPlacement { get; }

    public OrderGarmentItem Garment => GarmentPlacement.Garment;

    public DrawingFile Drawing { get; }

    public int PageNumber => GarmentPlacement.Page.PageNumber;

    public bool IsFirstPage => GarmentPlacement.Page.IsFirstPage;

    public DescriptionTargetGeometry Geometry =>
        GarmentViewDescriptionLayout.GetTargetGeometry(
            GarmentPlacement.Page,
            GarmentPlacement,
            Drawing);
}
