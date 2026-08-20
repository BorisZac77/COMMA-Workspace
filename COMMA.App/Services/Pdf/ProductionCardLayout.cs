namespace COMMA.App.Services.Pdf;

public static class ProductionCardLayout
{
    // Rozmiar strony A4 (proporcje używane również w podglądzie)
    public const float PageWidth = 210f;
    public const float PageHeight = 297f;

    // Margines zewnętrzny
    public const float Margin = 6f;

    // Sekcje
    public const float HeaderHeight = 24f;
    public const float OrderInfoHeight = 32f;
    public const float NotesHeight = 62f;

    // Odstępy
    public const float Gap = 2f;

    // Obliczana wysokość części z rysunkami
    public static float DrawingsHeight =>
        PageHeight
        - Margin * 2
        - HeaderHeight
        - OrderInfoHeight
        - NotesHeight
        - Gap * 3;
}