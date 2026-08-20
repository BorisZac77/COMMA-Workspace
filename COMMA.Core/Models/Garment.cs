using System;
using System.Collections.Generic;

namespace COMMA.Core.Models;

public class Garment
{
    /// <summary>
    /// Unikalny identyfikator produktu.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Nazwa produktu.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Dostępne rysunki techniczne.
    /// </summary>
    public List<GarmentDrawing> Drawings { get; set; } = new();
}