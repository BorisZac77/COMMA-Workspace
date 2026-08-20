using System.Collections.Generic;
using System.Linq;
using Avalonia.Media.Imaging;

namespace COMMA.App.Models;

public class Product
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Category { get; set; } = "";

    public string Folder { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public Bitmap? FrontThumbnail { get; set; }

    public ProductionCard Card { get; set; } = new();

    public List<DrawingFile> Drawings { get; } = new();


    public bool HasImage =>
        FrontThumbnail != null ||
        !string.IsNullOrWhiteSpace(ImagePath);


    public bool HasDrawings =>
        Drawings.Count > 0;


    public DrawingFile? FrontDrawing =>
        Drawings.FirstOrDefault(
            drawing => drawing.IsFront);


    public DrawingFile? BackDrawing =>
        Drawings.FirstOrDefault(
            drawing => drawing.IsBack);


    public DrawingFile? LeftDrawing =>
        Drawings.FirstOrDefault(
            drawing => drawing.IsLeft);


    public DrawingFile? RightDrawing =>
        Drawings.FirstOrDefault(
            drawing => drawing.IsRight);


    public string FrontDrawingPath =>
        FrontDrawing?.FullPath ?? string.Empty;


    public string BackDrawingPath =>
        BackDrawing?.FullPath ?? string.Empty;


    public string LeftDrawingPath =>
        LeftDrawing?.FullPath ?? string.Empty;


    public string RightDrawingPath =>
        RightDrawing?.FullPath ?? string.Empty;


    public string DisplayName =>
        !string.IsNullOrWhiteSpace(Name)
            ? Name
            : Code;


    public string DisplayCode =>
        string.IsNullOrWhiteSpace(Code)
            ? ""
            : Code;


    public string DisplayCategory =>
        string.IsNullOrWhiteSpace(Category)
            ? ""
            : Category;


    public int DrawingCount =>
        Drawings.Count;


    public string DrawingCountText =>
        DrawingCount switch
        {
            0 => "Brak rysunków technicznych",
            1 => "1 rysunek techniczny",
            2 or 3 or 4 => $"{DrawingCount} rysunki techniczne",
            _ => $"{DrawingCount} rysunków technicznych"
        };


    public override string ToString()
    {
        return DisplayName;
    }
}