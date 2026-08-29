using COMMA.App.Layout;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class OrderPageLayoutEngineTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SingleGarment_UsesOnePageWithAllSelectedDrawings(
        int drawingCount)
    {
        var garment = OrderTestData.CreateGarment(drawingCount);

        var pages = OrderPageLayoutEngine.BuildPages([garment]);

        var page = Assert.Single(pages);
        Assert.Same(garment, Assert.Single(page.Garments));
        Assert.Equal(drawingCount, page.DrawingCount);
        Assert.Equal(1, page.PageNumber);
        Assert.Equal(1, page.TotalPages);
        Assert.Equal("1 / 1", page.PageNumberText);
    }

    [Fact]
    public void SeveralSmallGarments_ArePackedUpToFourDrawings()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var second = OrderTestData.CreateGarment(2, "Second");
        var third = OrderTestData.CreateGarment(1, "Third");

        var pages = OrderPageLayoutEngine.BuildPages(
            [first, second, third]);

        var page = Assert.Single(pages);
        Assert.Equal(3, page.GarmentCount);
        Assert.Equal(4, page.DrawingCount);
        Assert.Equal([first, second, third], page.Garments);
    }

    [Fact]
    public void GarmentWithThreeDrawings_AlwaysUsesASeparatePage()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var large = OrderTestData.CreateGarment(3, "Large");
        var last = OrderTestData.CreateGarment(1, "Last");

        var pages = OrderPageLayoutEngine.BuildPages(
            [first, large, last]);

        Assert.Equal(3, pages.Count);
        Assert.Same(first, Assert.Single(pages[0].Garments));
        Assert.Same(large, Assert.Single(pages[1].Garments));
        Assert.Same(last, Assert.Single(pages[2].Garments));
    }

    [Fact]
    public void StartNewPage_BeginsTheGarmentOnAnotherPage()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var second = OrderTestData.CreateGarment(
            1,
            "Second",
            startNewPage: true);

        var pages = OrderPageLayoutEngine.BuildPages([first, second]);

        Assert.Equal(2, pages.Count);
        Assert.Same(first, Assert.Single(pages[0].Garments));
        Assert.Same(second, Assert.Single(pages[1].Garments));
    }

    [Fact]
    public void PageNumbersAndTotals_AreAppliedToEveryPage()
    {
        var garments = new[]
        {
            OrderTestData.CreateGarment(3, "First"),
            OrderTestData.CreateGarment(4, "Second"),
            OrderTestData.CreateGarment(3, "Third")
        };

        var pages = OrderPageLayoutEngine.BuildPages(garments);

        Assert.Equal(3, pages.Count);

        for (var index = 0; index < pages.Count; index++)
        {
            Assert.Equal(index + 1, pages[index].PageNumber);
            Assert.Equal(3, pages[index].TotalPages);
            Assert.Equal($"{index + 1} / 3", pages[index].PageNumberText);
        }
    }
}
