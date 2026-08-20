using System;

namespace COMMA.Core.Models;

public class GarmentDrawing
{
    /// <summary>
    /// Unikalny identyfikator rysunku.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Rodzaj widoku (przód, tył, lewy bok itd.).
    /// </summary>
    public GarmentView View { get; set; }

    /// <summary>
    /// Ścieżka do pliku PNG z rysunkiem technicznym.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;
}