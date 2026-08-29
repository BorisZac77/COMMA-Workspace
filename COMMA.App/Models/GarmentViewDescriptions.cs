using CommunityToolkit.Mvvm.ComponentModel;

namespace COMMA.App.Models;

public sealed class GarmentViewDescriptions : ObservableObject
{
    private string front = "";
    private string back = "";
    private string right = "";
    private string left = "";

    public string Front
    {
        get => front;
        set => SetProperty(ref front, value ?? "");
    }

    public string Back
    {
        get => back;
        set => SetProperty(ref back, value ?? "");
    }

    public string Right
    {
        get => right;
        set => SetProperty(ref right, value ?? "");
    }

    public string Left
    {
        get => left;
        set => SetProperty(ref left, value ?? "");
    }
}
