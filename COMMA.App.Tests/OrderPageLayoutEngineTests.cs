using COMMA.App.Layout;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class OrderPageLayoutEngineTests
{
    [Fact]
    public void PairedFirstPageLayout_IsUsedOnlyForTwoSingleViewGarmentsOnFirstPage()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var second = OrderTestData.CreateGarment(1, "Second");
        var pairedPage = OrderPageLayoutEngine.BuildPages([first, second])[0];

        Assert.True(pairedPage.UsesPairedFirstPageGarmentLayout);

        var twoViewGarment = OrderTestData.CreateGarment(2, "Two views");
        Assert.False(OrderPageLayoutEngine.BuildPages([twoViewGarment])[0]
            .UsesPairedFirstPageGarmentLayout);

        var continuationPages = OrderPageLayoutEngine.BuildPages(
        [
            OrderTestData.CreateGarment(3, "Leading"),
            OrderTestData.CreateGarment(1, "Second")
        ]);

        Assert.False(continuationPages[1].UsesPairedFirstPageGarmentLayout);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void SingleGarment_FirstPageNeverContainsMoreThanTwoDrawings(
        int drawingCount)
    {
        var garment = OrderTestData.CreateGarment(drawingCount);

        var pages = OrderPageLayoutEngine.BuildPages([garment]);

        Assert.Equal(drawingCount > 2 ? 2 : 1, pages.Count);
        Assert.InRange(pages[0].DrawingCount, 1, 2);
        Assert.All(pages, page => Assert.Same(garment, Assert.Single(page.Garments)));
        Assert.Equal(drawingCount, pages.Sum(page => page.DrawingCount));
        Assert.Equal(
            garment.SelectedDrawings,
            pages.SelectMany(page => page.Placements.SelectMany(placement => placement.Drawings)));
    }

    [Fact]
    public void SeveralSmallGarments_ArePackedUpToFourDrawings()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var second = OrderTestData.CreateGarment(2, "Second");
        var third = OrderTestData.CreateGarment(1, "Third");

        var pages = OrderPageLayoutEngine.BuildPages(
            [first, second, third]);

        Assert.Equal(2, pages.Count);
        Assert.Equal(1, pages[0].DrawingCount);
        Assert.Equal(3, pages[1].DrawingCount);
        Assert.Equal([first], pages[0].Garments);
        Assert.Equal([second, third], pages[1].Garments);
    }

    [Fact]
    public void GarmentBoundary_OnContinuationPage_DoesNotLeaveUnusedSlots()
    {
        var first = OrderTestData.CreateGarment(1, "First");
        var large = OrderTestData.CreateGarment(3, "Large");
        var last = OrderTestData.CreateGarment(1, "Last");

        var pages = OrderPageLayoutEngine.BuildPages(
            [first, large, last]);

        Assert.Equal(2, pages.Count);
        Assert.Same(first, Assert.Single(pages[0].Garments));
        Assert.Equal([large, last], pages[1].Garments);
        Assert.Equal(4, pages[1].DrawingCount);
    }

    [Fact]
    public void ThreeThenTwoDrawings_ArePackedIntoExactlyTwoProductionPages()
    {
        var first = OrderTestData.CreateGarment(3, "First");
        var second = OrderTestData.CreateGarment(2, "Second");

        var pages = OrderPageLayoutEngine.BuildPages([first, second]);

        Assert.Equal(2, pages.Count);
        Assert.Equal(2, pages[0].DrawingCount);
        Assert.Equal(3, pages[1].DrawingCount);
        Assert.Same(first, Assert.Single(pages[0].Garments));
        Assert.Equal([first, second], pages[1].Garments);
        Assert.Equal(1, pages[1].Placements[0].DrawingCount);
        Assert.Equal(2, pages[1].Placements[1].DrawingCount);
        Assert.Same(first, pages[1].Placements[0].Garment);
        Assert.Same(second, pages[1].Placements[1].Garment);
        Assert.Equal(
            first.SelectedDrawings,
            pages
                .SelectMany(page => page.Placements)
                .Where(placement => ReferenceEquals(placement.Garment, first))
                .SelectMany(placement => placement.Drawings));
        Assert.Equal(
            second.SelectedDrawings,
            pages[1].Placements[1].Drawings);
    }

    [Fact]
    public void OneFreeSlot_ThenFourDrawingGarment_StartsOnNewPageAndStaysWhole()
    {
        var first = OrderTestData.CreateGarment(3, "First");
        var second = OrderTestData.CreateGarment(2, "Second");
        var fourDrawings = OrderTestData.CreateGarment(4, "Four drawings");

        var pages = OrderPageLayoutEngine.BuildPages(
            [first, second, fourDrawings]);

        Assert.Equal([2, 3, 4], pages.Select(page => page.DrawingCount));
        Assert.Equal([first, second], pages[1].Garments);
        Assert.Same(
            fourDrawings,
            Assert.Single(pages[2].Garments));
        Assert.Equal(
            fourDrawings.SelectedDrawings,
            Assert.Single(pages[2].Placements).Drawings);
    }

    [Fact]
    public void EveryContinuationPage_ContainsAtMostFourDrawings()
    {
        var garments = new[]
        {
            OrderTestData.CreateGarment(3, "First"),
            OrderTestData.CreateGarment(2, "Second"),
            OrderTestData.CreateGarment(4, "Third"),
            OrderTestData.CreateGarment(3, "Fourth"),
            OrderTestData.CreateGarment(4, "Fifth")
        };

        var pages = OrderPageLayoutEngine.BuildPages(garments);

        Assert.InRange(pages[0].DrawingCount, 1, 2);
        Assert.All(
            pages.Skip(1),
            page => Assert.InRange(page.DrawingCount, 1, 4));
    }

    [Fact]
    public void PlopsaDocument_CreatesExpectedSixPagePlan()
    {
        var garment0380 = OrderTestData.CreateGarment(3, "0380");
        var garment0386 = OrderTestData.CreateGarment(2, "0386");
        var garment0510 = OrderTestData.CreateGarment(3, "0510");
        var garment0388 = OrderTestData.CreateGarment(3, "0388");
        var garment0638 = OrderTestData.CreateGarment(
            1,
            "0638",
            startNewPage: true);
        var garment0637 = OrderTestData.CreateGarment(4, "0637");

        var pages = OrderPageLayoutEngine.BuildPages(
        [
            garment0380,
            garment0386,
            garment0510,
            garment0388,
            garment0638,
            garment0637
        ]);

        Assert.Equal(6, pages.Count);
        Assert.Equal([2, 3, 3, 3, 1, 4], pages.Select(page => page.DrawingCount));
        Assert.Equal([garment0380], pages[0].Garments);
        Assert.Equal([garment0380, garment0386], pages[1].Garments);
        Assert.Equal([garment0510], pages[2].Garments);
        Assert.Equal([garment0388], pages[3].Garments);
        Assert.Equal([garment0638], pages[4].Garments);
        Assert.Equal([garment0637], pages[5].Garments);
    }

    [Fact]
    public void GarmentTransitions_OnContinuationPages_UseAvailableSlotsInOrder()
    {
        var variants = new[]
        {
            (FirstCount: 3, SecondCount: 1, PageCounts: new[] { 2, 2 }),
            (FirstCount: 3, SecondCount: 3, PageCounts: new[] { 2, 4 }),
            (FirstCount: 4, SecondCount: 2, PageCounts: new[] { 2, 4 }),
            (FirstCount: 4, SecondCount: 3, PageCounts: new[] { 2, 2, 3 })
        };

        foreach (var variant in variants)
        {
            var first = OrderTestData.CreateGarment(variant.FirstCount, "First");
            var second = OrderTestData.CreateGarment(variant.SecondCount, "Second");

            var pages = OrderPageLayoutEngine.BuildPages([first, second]);

            Assert.Equal(
                variant.PageCounts,
                pages.Select(page => page.DrawingCount));
            Assert.Equal(
                Enumerable.Repeat(first, variant.FirstCount)
                    .Concat(Enumerable.Repeat(second, variant.SecondCount)),
                pages
                    .SelectMany(page => page.Placements)
                    .SelectMany(placement =>
                        placement.Drawings.Select(_ => placement.Garment)));
        }
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
    public void OneDrawingThenThree_WithStartNewPage_LeavesPreviousPagePartial()
    {
        var firstPage = OrderTestData.CreateGarment(2, "First page");
        var previous = OrderTestData.CreateGarment(1, "Previous");
        var boundary = OrderTestData.CreateGarment(
            3,
            "Boundary",
            startNewPage: true);

        var pages = OrderPageLayoutEngine.BuildPages(
            [firstPage, previous, boundary]);

        Assert.Equal([2, 1, 3], pages.Select(page => page.DrawingCount));
        Assert.Same(previous, Assert.Single(pages[1].Garments));
        Assert.Same(boundary, Assert.Single(pages[2].Garments));
        Assert.Same(boundary, pages[2].Placements[0].Garment);
    }

    [Fact]
    public void OneDrawingThenThree_WithoutStartNewPage_StillFillsAvailableSlots()
    {
        var firstPage = OrderTestData.CreateGarment(2, "First page");
        var previous = OrderTestData.CreateGarment(1, "Previous");
        var next = OrderTestData.CreateGarment(3, "Next");

        var pages = OrderPageLayoutEngine.BuildPages(
            [firstPage, previous, next]);

        Assert.Equal([2, 4], pages.Select(page => page.DrawingCount));
        Assert.Equal([previous, next], pages[1].Garments);
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

        Assert.Equal(4, pages.Count);

        for (var index = 0; index < pages.Count; index++)
        {
            Assert.Equal(index + 1, pages[index].PageNumber);
            Assert.Equal(4, pages[index].TotalPages);
            Assert.Equal($"{index + 1}/4", pages[index].PageNumberText);
        }
    }

    [Fact]
    public void FirstAndSecondPage_UseOneOfTwoAndTwoOfTwoNumbering()
    {
        var pages = OrderPageLayoutEngine.BuildPages(
        [
            OrderTestData.CreateGarment(3, "First"),
            OrderTestData.CreateGarment(3, "Second")
        ]);

        Assert.Equal(2, pages.Count);
        Assert.True(pages[0].IsFirstPage);
        Assert.Equal("1/2", pages[0].PageNumberText);
        Assert.Equal("2/2", pages[1].PageNumberText);
    }

    [Fact]
    public void AttachmentMetadata_DoesNotIncreaseProductionCardPageTotal()
    {
        var card = new COMMA.App.Models.ProductionCard();
        card.Garments.Add(OrderTestData.CreateGarment(3, "First"));
        card.Garments.Add(OrderTestData.CreateGarment(3, "Second"));
        card.Attachments.Add(new COMMA.App.Models.OrderAttachmentMetadata
        {
            Name = "future-attachment.pdf"
        });

        var pages = OrderPageLayoutEngine.BuildPages(card.Garments);

        Assert.Equal(2, pages.Count);
        Assert.All(pages, page => Assert.Equal(2, page.TotalPages));
    }

    [Fact]
    public void Czcionka4_ThreeFourViewGarments_CreateExpectedFourPagePlan()
    {
        var first = OrderTestData.CreateGarment(4, "First");
        var second = OrderTestData.CreateGarment(4, "Second");
        var third = OrderTestData.CreateGarment(4, "Third");

        var pages = OrderPageLayoutEngine.BuildPages([first, second, third]);

        Assert.Equal(4, pages.Count);
        Assert.Equal(["FRONT", "BACK"], pages[0].Placements[0].Drawings.Select(DrawingLayoutEngine.GetViewName));
        Assert.Equal(["RIGHT", "LEFT"], pages[1].Placements[0].Drawings.Select(DrawingLayoutEngine.GetViewName));
        Assert.Same(first, Assert.Single(pages[1].Garments));
        Assert.Same(second, Assert.Single(pages[2].Garments));
        Assert.Same(third, Assert.Single(pages[3].Garments));
        Assert.Equal([2, 2, 4, 4], pages.Select(page => page.DrawingCount));
        Assert.Equal(
            ["FRONT", "BACK", "RIGHT", "LEFT"],
            pages
                .SelectMany(page => page.Placements)
                .Where(placement => ReferenceEquals(placement.Garment, first))
                .SelectMany(placement => placement.Drawings)
                .Select(DrawingLayoutEngine.GetViewName));
        Assert.Equal(
            ["FRONT", "BACK", "RIGHT", "LEFT"],
            pages
                .SelectMany(page => page.Placements)
                .Where(placement => ReferenceEquals(placement.Garment, second))
                .SelectMany(placement => placement.Drawings)
                .Select(DrawingLayoutEngine.GetViewName));
        Assert.Equal(
            ["FRONT", "BACK", "RIGHT", "LEFT"],
            pages
                .SelectMany(page => page.Placements)
                .Where(placement => ReferenceEquals(placement.Garment, third))
                .SelectMany(placement => placement.Drawings)
                .Select(DrawingLayoutEngine.GetViewName));
        Assert.All(pages, page => Assert.InRange(page.DrawingCount, 1, page.IsFirstPage ? 2 : 4));
        Assert.Equal(["1/4", "2/4", "3/4", "4/4"], pages.Select(page => page.PageNumberText));
    }
}
