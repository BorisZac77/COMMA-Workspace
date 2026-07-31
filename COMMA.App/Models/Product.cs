namespace COMMA.App.Models;

public class Product
{
    public string Code { get; set; } = "";
    public string Folder { get; set; } = "";

    public override string ToString() => Code;
}