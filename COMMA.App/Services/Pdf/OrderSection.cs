using COMMA.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services.Pdf;

public static class OrderSection
{
    public static void Build(
        ColumnDescriptor column,
        ProductionCard card)
    {
        column.Item()
            .Height(PdfStyles.OrderSectionHeight)
            .AlignCenter()
            .AlignMiddle()
            .Text(Safe(card.ProductName))
            .FontSize(14)
            .Bold();
    }

    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }
}