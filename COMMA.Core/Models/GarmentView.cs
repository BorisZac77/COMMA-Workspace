namespace COMMA.Core.Models;

public enum GarmentView
{
    Front,
    Back,
    RightSide,
    LeftSide
}

public static class GarmentViewExtensions
{
    public static string Title(this GarmentView view) =>
        view switch
        {
            GarmentView.Front => "PRZÓD",
            GarmentView.Back => "TYŁ",
            GarmentView.RightSide => "PRAWY BOK",
            GarmentView.LeftSide => "LEWY BOK",
            _ => string.Empty
        };

    public static int Order(this GarmentView view) =>
        view switch
        {
            GarmentView.Front => 0,
            GarmentView.Back => 1,
            GarmentView.RightSide => 2,
            GarmentView.LeftSide => 3,
            _ => int.MaxValue
        };
}