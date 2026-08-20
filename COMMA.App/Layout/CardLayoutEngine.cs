namespace COMMA.App.Layout;

public static class CardLayoutEngine
{
    // QuestPDF używa punktów.
    // A4: 595,28 × 841,89 punktów.
    public const float PageWidth = 595.28f;

    public const float PageHeight = 841.89f;

    public const float PageMargin = 7f;

    public const float ContentBorder = 1f;

    public const float ContentPadding = 5f;

    public const float SectionGap = 3f;

    public const float HeaderHeight = 64f;

    public const float OrderInformationHeight = 58f;

    public const float HandwrittenFieldsHeight = 110f;

    public const float AvailableContentHeight =
        PageHeight
        - PageMargin * 2
        - ContentBorder * 2
        - ContentPadding * 2;

    public const float FixedSectionsHeight =
        HeaderHeight
        + OrderInformationHeight
        + HandwrittenFieldsHeight
        + SectionGap * 3;

    public const float DrawingSectionHeight =
        AvailableContentHeight
        - FixedSectionsHeight;

    public const float HeaderLogoWidth = 135f;

    public const float HeaderOrderNameWidth = 180f;

    public const float DrawingTitleHeight = 16f;

    public const float DrawingDescriptionHeight = 16f;

    public const float DrawingCellPadding = 3f;

    public const float DrawingImagePadding = 3f;

    public static float GetDrawingRowHeight(int rowCount)
    {
        if (rowCount <= 0)
            return DrawingSectionHeight;

        return DrawingSectionHeight / rowCount;
    }

    public static float GetDrawingImageHeight(float rowHeight)
    {
        var imageHeight =
            rowHeight
            - DrawingTitleHeight
            - DrawingDescriptionHeight
            - DrawingCellPadding * 2;

        return imageHeight > 1f
            ? imageHeight
            : 1f;
    }
}