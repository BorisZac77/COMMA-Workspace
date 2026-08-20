using System.Collections.Generic;

namespace COMMA.Core.Models;

public enum DrawingLayout
{
    Single,
    TwoHorizontal,
    ThreeMixed,
    Grid
}

public static class DrawingLayoutExtensions
{
    public static DrawingLayout GetLayout(IReadOnlyList<DrawingFile> drawings)
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
}