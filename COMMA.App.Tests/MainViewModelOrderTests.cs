using System.Reflection;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Tests.TestSupport;
using COMMA.App.ViewModels;

namespace COMMA.App.Tests;

public sealed class MainViewModelOrderTests
{
    [Fact]
    public void ClearCurrentOrder_RemovesOrderNumberAndOtherOrderData()
    {
        var viewModel = new MainViewModel
        {
            ProductionCard = new ProductionCard
            {
                OrderNumber = "ZL-2026-0042",
                OrderName = "BINNEN BOUWERS",
                Customer = "Binnen Bouwers"
            }
        };

        viewModel.ClearCurrentOrder();

        Assert.NotNull(viewModel.ProductionCard);
        Assert.Equal("", viewModel.ProductionCard.OrderNumber);
        Assert.Equal("", viewModel.ProductionCard.OrderName);
        Assert.Equal("", viewModel.ProductionCard.Customer);
    }

    [Fact]
    public void SelectingAnotherProductPreservesIndependentProductionEntries()
    {
        var viewModel = new MainViewModel();
        var previousCard = new ProductionCard();
        previousCard.ProductionEntries[0].LogoName = "Haft na piersi";
        previousCard.ProductionEntries[0].Dimension = "80 x 35 mm";
        previousCard.ProductionEntries[0].Colours.Add(
            new ProductionColourEntry(1) { Value = "Nici granatowe" });
        previousCard.ProductionEntries[0].Colours.Add(
            new ProductionColourEntry(2) { Value = "Nici białe" });
        previousCard.ProductionEntries[1].LogoName = "Logo na plecach";
        previousCard.ProductionEntries[1].Dimension = "240 x 120 mm";
        previousCard.ProductionEntries[1].Colours.Add(
            new ProductionColourEntry(1) { Value = "Pantone 186 C" });
        previousCard.ProductionEntries[1].Colours.Add(
            new ProductionColourEntry(2) { Value = "Pantone Black C" });
        viewModel.ProductionCard = previousCard;

        viewModel.SelectedProduct = new Product
        {
            Code = "NEW",
            Name = "Nowy rodzaj odzieży"
        };

        var copiedCard = Assert.IsType<ProductionCard>(viewModel.ProductionCard);
        Assert.NotSame(previousCard, copiedCard);
        Assert.Equal(previousCard.ProductionEntries.Count, copiedCard.ProductionEntries.Count);

        for (var entryIndex = 0;
             entryIndex < previousCard.ProductionEntries.Count;
             entryIndex++)
        {
            var previousEntry = previousCard.ProductionEntries[entryIndex];
            var copiedEntry = copiedCard.ProductionEntries[entryIndex];

            Assert.NotSame(previousEntry, copiedEntry);
            Assert.Equal(previousEntry.Number, copiedEntry.Number);
            Assert.Equal(previousEntry.LogoName, copiedEntry.LogoName);
            Assert.Equal(previousEntry.Dimension, copiedEntry.Dimension);
            Assert.NotSame(previousEntry.Colours, copiedEntry.Colours);
            Assert.Equal(
                previousEntry.Colours.Select(colour => (colour.Number, colour.Value)),
                copiedEntry.Colours.Select(colour => (colour.Number, colour.Value)));

            for (var colourIndex = 0;
                 colourIndex < previousEntry.Colours.Count;
                 colourIndex++)
            {
                Assert.NotSame(
                    previousEntry.Colours[colourIndex],
                    copiedEntry.Colours[colourIndex]);
            }
        }

        copiedCard.ProductionEntries[0].LogoName = "Zmienione logo";
        copiedCard.ProductionEntries[0].Colours[0].Value = "Zmienione nici";
        copiedCard.ProductionEntries[0].Colours.Add(
            new ProductionColourEntry(3) { Value = "Nowy kolor" });

        Assert.Equal("Haft na piersi", previousCard.ProductionEntries[0].LogoName);
        Assert.Equal("Nici granatowe", previousCard.ProductionEntries[0].Colours[0].Value);
        Assert.Equal(2, previousCard.ProductionEntries[0].Colours.Count);
    }

    [Fact]
    public void DescriptionDialogCheckIgnoresNewControlledTextAndIdentifiesExistingOverflow()
    {
        var viewModel = new MainViewModel();
        var controlled = OrderTestData.CreateGarment(
            4,
            "Nowa pozycja");
        var controller =
            new GarmentViewDescriptionInputController(
                "",
                DescriptionLayoutTarget.FirstPageTwoViews);
        controlled.ViewDescriptions.Front = controller.Apply(
            new string('g', 4000),
            DescriptionLayoutTarget.FirstPageTwoViews).Text;
        viewModel.Garments.Add(controlled);
        var method = typeof(MainViewModel).GetMethod(
            "TryGetNonFittingViewDescription",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        object?[] controlledArguments = [null, null];
        var controlledResult = (bool)method.Invoke(
            viewModel,
            controlledArguments)!;

        Assert.False(controlledResult);

        var existingOverflow = OrderTestData.CreateGarment(
            4,
            "0510 T-Time t-shirt");
        existingOverflow.ViewDescriptions.Front =
            string.Join(
                '\n',
                Enumerable.Repeat("ZA DŁUGI OPIS", 100));
        viewModel.Garments.Add(existingOverflow);
        object?[] overflowArguments = [null, null];
        var overflowResult = (bool)method.Invoke(
            viewModel,
            overflowArguments)!;

        Assert.True(overflowResult);
        Assert.Equal(
            "0510 T-Time t-shirt",
            overflowArguments[0]);
        Assert.Equal(
            "FRONT",
            overflowArguments[1]);
    }

    [Fact]
    public void EditorTargetResolverUsesActualFirstAndLaterPageFromPagePlan()
    {
        var viewModel = new MainViewModel();
        viewModel.Garments.Add(
            OrderTestData.CreateGarment(4, "First"));
        viewModel.Garments.Add(
            OrderTestData.CreateGarment(4, "Second"));
        viewModel.Garments.Add(
            OrderTestData.CreateGarment(4, "Third"));
        var method = typeof(MainViewModel).GetMethod(
            "ResolveDescriptionTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        var firstGeometry = (GarmentViewDescriptionGeometrySet)method.Invoke(
            viewModel,
            [0, true, new GarmentViewSelection(true, true, true, true), false])!;
        var secondGeometry = (GarmentViewDescriptionGeometrySet)method.Invoke(
            viewModel,
            [1, true, new GarmentViewSelection(true, true, true, true), false])!;
        var thirdGeometry = (GarmentViewDescriptionGeometrySet)method.Invoke(
            viewModel,
            [2, true, new GarmentViewSelection(true, true, true, true), false])!;

        Assert.Equal(
            DescriptionLayoutTarget.FirstPageTwoViews,
            firstGeometry.Front.Target);
        Assert.Equal(
            DescriptionLayoutTarget.LaterPageTwoViews,
            firstGeometry.Right.Target);
        Assert.Equal(
            DescriptionLayoutTarget.LaterPageFourViews,
            secondGeometry.Front.Target);
        Assert.Equal(
            DescriptionLayoutTarget.LaterPageFourViews,
            thirdGeometry.Front.Target);
        Assert.Equal(
            secondGeometry.Front.PdfDrawingCellHeight,
            thirdGeometry.Right.PdfDrawingCellHeight);
    }
}
