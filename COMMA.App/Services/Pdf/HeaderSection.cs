using COMMA.App.Models;
using COMMA.App.Services.Branding;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services.Pdf;

public static class HeaderSection
{
    public static void Build(
        ColumnDescriptor column,
        ProductionCard card)
    {
        column.Item()
            .Height(PdfStyles.HeaderHeight)
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(
                        PdfStyles.HeaderLogoWidth);

                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                BuildLogoCell(table);

                BuildOrderNameCell(
                    table,
                    card);

                BuildInformationCell(
                    table,
                    "KLIENT",
                    card.Customer);

                BuildInformationCell(
                    table,
                    "TERMIN WYKONANIA",
                    card.DueDate);

                BuildInformationCell(
                    table,
                    "RODZAJ PRODUKCJI",
                    card.ProductionType);
            });
    }

    private static void BuildLogoCell(
        TableDescriptor table)
    {
        var logoImage =
            BrandAssets.LoadCompanyLogo();

        table.Cell()
            .RowSpan(2)
            .Border(PdfStyles.StandardBorderWidth)
            .Padding(3)
            .AlignCenter()
            .AlignMiddle()
            .Element(container =>
            {
                if (logoImage.Length > 0)
                {
                    container
                        .Scale(0.80325f)
                        .Image(logoImage)
                        .FitArea();

                    return;
                }

                container
                    .AlignCenter()
                    .Text("BRAK LOGO")
                    .FontSize(8)
                    .Bold();
            });
    }

    private static void BuildOrderNameCell(
        TableDescriptor table,
        ProductionCard card)
    {
        var orderName =
            Safe(card.OrderName);

        table.Cell()
            .ColumnSpan(3)
            .Height(PdfStyles.HeaderTopRowHeight)
            .Border(PdfStyles.StandardBorderWidth)
            .Padding(PdfStyles.HeaderOrderNamePadding)
            .AlignCenter()
            .AlignMiddle()
            .Text(orderName)
            .FontSize(
                GetOrderNameFontSize(orderName))
            .FontColor("#0071BC")
            .ExtraBold();
    }

    private static void BuildInformationCell(
        TableDescriptor table,
        string title,
        string? value)
    {
        table.Cell()
            .Height(PdfStyles.HeaderInformationRowHeight)
            .Border(PdfStyles.StandardBorderWidth)
            .Padding(PdfStyles.OrderCellPadding)
            .Column(column =>
            {
                column.Item()
                    .AlignCenter()
                    .Text(title)
                    .FontSize(
                        PdfStyles.FieldTitleFontSize)
                    .Bold();

                column.Item()
                    .PaddingTop(
                        PdfStyles.OrderValueTopPadding)
                    .AlignCenter()
                    .Text(Safe(value))
                    .FontSize(
                        PdfStyles.OrderValueFontSize)
                    .Bold();
            });
    }

    private static float GetOrderNameFontSize(
        string value)
    {
        var length =
            value.Length;

        if (length <= 15)
            return 20f;

        if (length <= 25)
            return 17f;

        if (length <= 35)
            return 14f;

        return 12f;
    }

    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }
}