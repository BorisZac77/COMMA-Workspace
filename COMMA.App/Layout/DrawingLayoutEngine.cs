using System;
using System.Collections.Generic;
using System.Linq;
using COMMA.App.Models;

namespace COMMA.App.Layout;

public static class DrawingLayoutEngine
{
    public static IReadOnlyList<DrawingFile> GetSelectedDrawings(
        ProductionCard card)
    {
        ArgumentNullException.ThrowIfNull(card);

        return card.Drawings
            .Where(drawing => IsSelected(card, drawing))
            .OrderBy(GetDrawingOrder)
            .Take(4)
            .ToList();
    }


    public static IReadOnlyList<DrawingFile> GetSelectedDrawings(
        OrderGarmentItem garment)
    {
        ArgumentNullException.ThrowIfNull(garment);

        return garment.Drawings
            .Where(drawing => IsSelected(garment, drawing))
            .OrderBy(GetDrawingOrder)
            .Take(4)
            .ToList();
    }


    public static DrawingLayout GetLayout(
        ProductionCard card)
    {
        return GetLayout(
            GetSelectedDrawings(card));
    }


    public static DrawingLayout GetLayout(
        OrderGarmentItem garment)
    {
        return GetLayout(
            GetSelectedDrawings(garment));
    }


    public static IReadOnlyList<DrawingLayoutRow> GetRows(
        ProductionCard card)
    {
        return GetRows(
            GetSelectedDrawings(card));
    }


    public static IReadOnlyList<DrawingLayoutRow> GetRows(
        OrderGarmentItem garment)
    {
        return GetRows(
            GetSelectedDrawings(garment));
    }


    public static DrawingLayout GetLayout(
        IReadOnlyList<DrawingFile> drawings)
    {
        return drawings.Count switch
        {
            0 => DrawingLayout.Single,
            1 => DrawingLayout.Single,
            2 => DrawingLayout.TwoHorizontal,
            3 => DrawingLayout.ThreeMixed,
            _ => DrawingLayout.Grid
        };
    }


    public static IReadOnlyList<DrawingLayoutRow> GetRows(
        IReadOnlyList<DrawingFile> drawings)
    {
        return drawings.Count switch
        {
            0 => Array.Empty<DrawingLayoutRow>(),

            1 =>
            [
                new DrawingLayoutRow(
                    drawings[0],
                    null,
                    firstColumnSpan: 2)
            ],

            2 =>
            [
                new DrawingLayoutRow(
                    drawings[0],
                    drawings[1])
            ],

            3 =>
            [
                new DrawingLayoutRow(
                    drawings[0],
                    drawings[1]),

                new DrawingLayoutRow(
                    drawings[2],
                    null,
                    firstColumnSpan: 2)
            ],

            _ =>
            [
                new DrawingLayoutRow(
                    drawings[0],
                    drawings[1]),

                new DrawingLayoutRow(
                    drawings[2],
                    drawings[3])
            ]
        };
    }


    private static bool IsSelected(
        ProductionCard card,
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return card.ShowFront;

        if (drawing.IsBack)
            return card.ShowBack;

        if (drawing.IsRight)
            return card.ShowRight;

        if (drawing.IsLeft)
            return card.ShowLeft;

        return false;
    }


    private static bool IsSelected(
        OrderGarmentItem garment,
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return garment.ShowFront;

        if (drawing.IsBack)
            return garment.ShowBack;

        if (drawing.IsRight)
            return garment.ShowRight;

        if (drawing.IsLeft)
            return garment.ShowLeft;

        return false;
    }


    private static int GetDrawingOrder(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return 0;

        if (drawing.IsBack)
            return 1;

        if (drawing.IsRight)
            return 2;

        if (drawing.IsLeft)
            return 3;

        return 100;
    }

    public static string GetViewName(DrawingFile drawing)
    {
        ArgumentNullException.ThrowIfNull(drawing);

        if (drawing.IsFront)
            return "FRONT";
        if (drawing.IsBack)
            return "BACK";
        if (drawing.IsRight)
            return "RIGHT";
        if (drawing.IsLeft)
            return "LEFT";

        return "RZUT";
    }
}


public sealed class DrawingLayoutRow
{
    public DrawingLayoutRow(
        DrawingFile first,
        DrawingFile? second,
        int firstColumnSpan = 1)
    {
        First = first;
        Second = second;
        FirstColumnSpan = firstColumnSpan;
    }

    public DrawingFile First { get; }

    public DrawingFile? Second { get; }

    public int FirstColumnSpan { get; }
}
