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
            DescriptionLayoutTarget.LaterPageTwoViews,
            secondGeometry.Front.Target);
        Assert.Equal(
            DescriptionLayoutTarget.LaterPageTwoViews,
            thirdGeometry.Front.Target);
        Assert.True(
            secondGeometry.Front.PdfDrawingCellHeight <
            thirdGeometry.Right.PdfDrawingCellHeight);
    }
}
