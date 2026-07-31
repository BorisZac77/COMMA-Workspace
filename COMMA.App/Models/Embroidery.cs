using System;

namespace COMMA.App.Models;

public class Embroidery
{
    /// <summary>
    /// Unikalny identyfikator haftu.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Numer haftu wyświetlany na karcie.
    /// </summary>
    public int Number { get; set; }

    /// <summary>
    /// Opis haftu.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Widok produktu.
    /// </summary>
    public GarmentView View { get; set; }

    /// <summary>
    /// Pozycja pozioma na rysunku (0.0 - 1.0).
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Pozycja pionowa na rysunku (0.0 - 1.0).
    /// </summary>
    public double Y { get; set; }
}