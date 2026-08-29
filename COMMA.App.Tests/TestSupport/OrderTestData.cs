using COMMA.App.Layout;
using COMMA.App.Models;

namespace COMMA.App.Tests.TestSupport;

internal static class OrderTestData
{
    public static OrderGarmentItem CreateGarment(
        int drawingCount,
        string name = "Test garment",
        bool startNewPage = false)
    {
        if (drawingCount is < 1 or > 4)
            throw new ArgumentOutOfRangeException(nameof(drawingCount));

        var garment = new OrderGarmentItem
        {
            ProductCode = $"CODE-{drawingCount}",
            Name = name,
            Colour = "Navy",
            Variant = "Standard",
            ShowFront = drawingCount >= 1,
            ShowBack = drawingCount >= 2,
            ShowRight = drawingCount >= 3,
            ShowLeft = drawingCount >= 4,
            StartNewPage = startNewPage
        };

        garment.Drawings.AddRange(
        [
            new DrawingFile
            {
                Name = "Front",
                FullPath = "/Users/test/library/front.png",
                View = "FRONT",
                IsFront = true
            },
            new DrawingFile
            {
                Name = "Back",
                FullPath = @"C:\library\back.png",
                View = "BACK",
                IsBack = true
            },
            new DrawingFile
            {
                Name = "Right",
                FullPath = "/Users/test/library/right.png",
                View = "RIGHT",
                IsRight = true
            },
            new DrawingFile
            {
                Name = "Left",
                FullPath = @"C:\library\left.png",
                View = "LEFT",
                IsLeft = true
            }
        ]);

        garment.RefreshDrawingSelection();

        return garment;
    }

    public static IReadOnlyList<OrderPageLayout> CreatePages(
        params OrderGarmentItem[] garments)
    {
        return OrderPageLayoutEngine.BuildPages(garments);
    }
}
