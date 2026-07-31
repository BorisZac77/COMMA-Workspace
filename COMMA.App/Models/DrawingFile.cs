namespace COMMA.App.Models;

public class DrawingFile
{
    public string Name { get; set; } = "";

    public string FullPath { get; set; } = "";

    public override string ToString()
    {
        return Name;
    }
}