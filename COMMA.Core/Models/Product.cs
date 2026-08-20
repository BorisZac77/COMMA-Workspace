using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace COMMA.Core.Models;

public class Product
{
    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string Folder { get; set; } = "";

    public string ImagePath { get; set; } = "";

    public ProductionCard Card { get; set; } = new();

    public List<DrawingFile> Drawings { get; } = new();

    public bool HasImage =>
        !string.IsNullOrWhiteSpace(ImagePath);

    public bool HasDrawings =>
        DrawingCount > 0;

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

    public int DrawingCount =>
        Drawings.Count(
            drawing =>
                IsTechnicalDrawing(drawing) &&
                !drawing.IsLeft);

    public string DrawingCountText =>
        DrawingCount switch
        {
            0 => "Brak rysunków technicznych",
            1 => "1 rysunek techniczny",
            2 => "2 rysunki techniczne",
            3 => "3 rysunki techniczne",
            _ => $"{DrawingCount} rysunków technicznych"
        };

    public override string ToString()
    {
        return DisplayName;
    }

    private static bool IsTechnicalDrawing(
        DrawingFile drawing)
    {
        if (string.IsNullOrWhiteSpace(drawing.FullPath))
            return false;

        var directoryPath =
            Path.GetDirectoryName(drawing.FullPath);

        if (string.IsNullOrWhiteSpace(directoryPath))
            return false;

        var directoryNames = directoryPath.Split(
            new[]
            {
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            },
            StringSplitOptions.RemoveEmptyEntries);

        return directoryNames.Any(
            directoryName =>
                string.Equals(
                    directoryName,
                    "Drawings",
                    StringComparison.OrdinalIgnoreCase));
    }
}