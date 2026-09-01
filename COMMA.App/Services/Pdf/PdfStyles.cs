namespace COMMA.App.Services.Pdf;

public static class PdfStyles
{
    // STRONA A4 — wartości w punktach QuestPDF
    public const float PageWidth = 595.28f;
    public const float PageHeight = 841.89f;

    public const float PageMargin = 7f;
    public const float OuterBorderWidth = 1f;
    public const float PagePadding = 5f;
    public const float SectionGap = 3f;
    public const float PageSafetyReserve = 10f;

    // =========================================================
    // SEKCJA 1
    // NIE ZMIENIAMY
    // =========================================================

    public const float HeaderTopRowHeight = 37.485f;

    public const float HeaderInformationRowHeight = 41f;

    public const float HeaderHeight =
        HeaderTopRowHeight
        + HeaderInformationRowHeight;

    // =========================================================
    // SEKCJA 2
    // =========================================================

    // Nazwa produktu na dole Sekcji 2
    public const float OrderSectionHeight = 21f;

    // Całkowita wysokość Sekcji 2 pozostaje bez zmian.
    public const float HandwrittenSectionHeight = 168.515f;

    // Nagłówek LOGOWANIE
    public const float LoggingTitleHeight = 18f;

    // Było 46 pt.
    // Zmniejszamy pole nazwa logo + wymiar o 20%.
    //
    // 46 x 0,80 = 36,8 pt
    public const float LoggingEntriesHeight = 36.8f;

    // Nagłówek KOLORYSTYKA
    public const float ColoursTitleHeight = 18f;

    // Wszystko odzyskane z LOGOWANIA
    // przechodzi do pola KOLORYSTYKI.
    public const float ColoursAreaHeight =
        HandwrittenSectionHeight
        - LoggingTitleHeight
        - LoggingEntriesHeight
        - ColoursTitleHeight;

    // =========================================================
    // NAGŁÓWEK — SEKCJA 1
    // NIE ZMIENIAMY
    // =========================================================

    public const float HeaderLogoWidth = 145f;
    public const float HeaderCellPadding = 6f;
    public const float HeaderOrderNamePadding = 5f;
    public const float HeaderPageNumberWidth = 88f;
    public const float FirstPageHeaderOrderNumberWidth = 101.5f;
    public const float FirstPageHeaderPageNumberWidth = 68f;
    public const float HeaderIdentityHorizontalPadding = 6f;
    public const string OrderNameColor = "#0071BC";

    // =========================================================
    // DANE ZLECENIA — SEKCJA 1
    // NIE ZMIENIAMY
    // =========================================================

    public const float OrderCellPadding = 4f;
    public const float OrderValueTopPadding = 6f;

    // =========================================================
    // SEKCJA 2 — LOGOWANIE
    // =========================================================

    public const float LoggingCellPadding = 4f;

    public const float LoggingNumberAreaWidth = 20f;

    public const float LoggingNumberCircleSize = 15f;

    // Numer + nazwa logo w jednej linii
    public const float LoggingTopLineHeight = 17f;

    // Wymiar logo pod nazwą
    public const float LoggingDimensionHeight = 10f;

    // =========================================================
    // SEKCJA 2 — KOLORYSTYKA
    // =========================================================

    public const float ColoursCellPadding = 5f;

    public const float ColourNumberWidth = 18f;

    // Maksymalna wysokość jednego wiersza koloru.
    //
    // Dzięki temu:
    // 2 kolory nie rozchodzą się po całej komórce,
    // 3 kolory są blisko siebie,
    // 10 kolorów wygląda kompaktowo,
    // a przy większej liczbie wysokość zmniejszy się automatycznie.
    public const float ColourCompactRowHeight = 12f;

    // =========================================================
    // SEKCJA 3 — RYSUNKI
    // NIE ZMIENIAMY
    // =========================================================

    public const float DrawingTitleHeight = 18f;
    public const float DrawingPlacementHeight = 18f;
    public const float DrawingCellPadding = 4f;
    public const float DrawingImagePadding = 4f;
    public const float DrawingSafetyReserve = 4f;

    public const float DrawingDescriptionTopGap = 2f;

    public const float MultiDrawingDescriptionTopGap = 8f;

    public const float DrawingDescriptionHorizontalPadding = 6f;

    public const float DrawingDescriptionLineHeight = 1.1f;

    // 70 mm w punktach QuestPDF. Dotyczy wszystkich układów rysunków.
    public const float DrawingMaximumHeight =
        70f / 25.4f * 72f;

    public const float MultiDrawingMaximumHeight =
        DrawingMaximumHeight;

    public const float MultiDrawingImageAreaMinimumHeight =
        MultiDrawingMaximumHeight
        + DrawingImagePadding * 2;

    // =========================================================
    // RAMKI
    // =========================================================

    public const float StandardBorderWidth = 1f;
    public const float LineWidth = 0.5f;

    // =========================================================
    // CZCIONKI
    // =========================================================

    public const float DefaultFontSize = 8f;

    public const float LogoFontSize = 21f;
    public const float LogoSubtitleFontSize = 5.5f;

    public const float HeaderTitleFontSize = 18f;
    public const float HeaderOrderLabelFontSize = 7f;
    public const float HeaderOrderLabelHeight = 9f;
    public const float HeaderOrderNameFontSize = 15f;

    public const float SectionTitleFontSize = 9f;

    public const float FieldTitleFontSize = 7f;
    public const float FieldValueFontSize = 10f;

    public const float OrderValueFontSize = 12f;

    // Sekcja 2
    public const float LoggingTitleFontSize = 9f;
    public const float ColoursTitleFontSize = 9f;

    public const float LoggingNumberFontSize = 8f;

    public const float ColourEntryFontSize = 10f;

    // Wymiar logo większy niż wcześniej
    public const float LoggingDimensionFontSize = 8.5f;

    // Rysunki
    public const float DrawingTitleFontSize = 8f;
    public const float DrawingPlacementFontSize = 5.5f;
    public const float DrawingDescriptionFontSize = 10f;

    // =========================================================
    // LICZBA PÓL
    // =========================================================

    public const int MaximumProductionEntryCount = 4;
    public const int MaximumDrawingCount = 4;

    // =========================================================
    // GEOMETRIA STRONY
    // =========================================================

    public const float AvailableContentHeight =
        PageHeight
        - PageMargin * 2
        - OuterBorderWidth * 2
        - PagePadding * 2;

    public const float AvailableContentWidth =
        PageWidth
        - PageMargin * 2
        - OuterBorderWidth * 2
        - PagePadding * 2;

    public const float FixedSectionsHeight =
        HeaderHeight
        + HandwrittenSectionHeight
        + OrderSectionHeight
        + SectionGap * 3;

    public const float DrawingSectionHeight =
        AvailableContentHeight
        - FixedSectionsHeight
        - PageSafetyReserve;

    public static float GetDrawingRowHeight(
        int rowCount)
    {
        if (rowCount <= 0)
            return DrawingSectionHeight;

        return DrawingSectionHeight /
               rowCount;
    }

    public static float GetDrawingImageHeight(
        float rowHeight)
    {
        var imageHeight =
            rowHeight
            - DrawingTitleHeight
            - DrawingPlacementHeight
            - DrawingCellPadding * 2
            - DrawingSafetyReserve;

        if (imageHeight <= 1f)
            return 1f;

        return imageHeight * 0.75f;
    }
}
