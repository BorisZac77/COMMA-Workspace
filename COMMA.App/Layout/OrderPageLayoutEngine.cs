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

        foreach (var garment in items)
        {
            var drawingCount =
                garment.SelectedDrawingCount;

            /*
             * 3 lub 4 rzuty:
             * zawsze osobna strona.
             */
            if (drawingCount >= 3)
            {
                currentPage =
                    null;

                var page =
                    new OrderPageLayout();

                page.Garments.Add(
                    garment);

                pages.Add(
                    page);

                continue;
            }

            /*
             * Ręcznie wymuszona nowa strona.
             */
            if (garment.StartNewPage)
            {
                currentPage =
                    new OrderPageLayout();

                currentPage.Garments.Add(
                    garment);

                pages.Add(
                    currentPage);

                continue;
            }

            /*
             * Nie ma jeszcze strony,
             * do której można coś dołożyć.
             */
            if (currentPage == null)
            {
                currentPage =
                    new OrderPageLayout();

                currentPage.Garments.Add(
                    garment);

                pages.Add(
                    currentPage);

                continue;
            }

            /*
             * Maksymalnie 4 rzuty łącznie
             * na jednej stronie.
             */
            if (currentPage.DrawingCount +
                drawingCount <= 4)
            {
                currentPage.Garments.Add(
                    garment);

                continue;
            }

            /*
             * Brak miejsca:
             * zaczynamy kolejną stronę.
             */
            currentPage =
                new OrderPageLayout();

            currentPage.Garments.Add(
                garment);

            pages.Add(
                currentPage);
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
    public List<OrderGarmentItem> Garments { get; } =
        new();


    public int PageNumber { get; internal set; }


    public int TotalPages { get; internal set; }


    public bool IsFirstPage =>
        PageNumber == 1;


    public string PageNumberText =>
        TotalPages > 0
            ? $"{PageNumber} / {TotalPages}"
            : "";


    public int DrawingCount =>
        Garments.Sum(
            garment =>
                garment.SelectedDrawingCount);


    public int GarmentCount =>
        Garments.Count;


    public string GarmentNamesText =>
        string.Join(
            " + ",
            Garments
                .Select(garment =>
                    garment.DisplayName)
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