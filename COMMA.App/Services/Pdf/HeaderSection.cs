using COMMA.App.Models;
using COMMA.App.Layout;
using COMMA.App.Services.Branding;
using QuestPDF.Fluent;

namespace COMMA.App.Services.Pdf;

public static class HeaderSection
{
    public static void Build(
        ColumnDescriptor column,
        ProductionCard card,
        string pageNumberText)
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

                BuildOrderIdentityCell(
                    table,
                    card,
                    pageNumberText);

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

    private static void BuildOrderIdentityCell(
        TableDescriptor table,
        ProductionCard card,
        string pageNumberText)
    {
        var orderNumber =
            Safe(card.OrderNumber);

        var orderName =
            Safe(card.OrderName);

        var orderNumberFit =
            OrderHeaderTextLayout.FitNumber(
                orderNumber,
                OrderHeaderTextLayout.PdfFirstPageNumberGeometry);

        var orderNameFit =
            OrderHeaderTextLayout.FitName(
                orderName,
                OrderHeaderTextLayout.PdfFirstPageNameGeometry);

        table.Cell()
            .ColumnSpan(3)
            .Height(PdfStyles.HeaderTopRowHeight)
            .Row(row =>
            {
                row.ConstantItem(
                        PdfStyles.FirstPageHeaderOrderNumberWidth)
                    .Border(PdfStyles.StandardBorderWidth)
                    .PaddingHorizontal(
                        PdfStyles.HeaderIdentityHorizontalPadding)
                    .PaddingVertical(1)
                    .Column(column =>
                    {
                        BuildHeaderLabel(
                            column,
                            "NUMER ZLECENIA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(orderNumberFit.DisplayText)
                            .FontSize((float)orderNumberFit.FontSize)
                            .LineHeight(OrderHeaderTextLayout.LineHeight)
                            .FontColor(
                                PdfStyles.OrderNameColor)
                            .Bold();
                    });

                row.RelativeItem()
                    .Border(PdfStyles.StandardBorderWidth)
                    .PaddingHorizontal(
                        PdfStyles.HeaderIdentityHorizontalPadding)
                    .PaddingVertical(1)
                    .Column(column =>
                    {
                        BuildHeaderLabel(
                            column,
                            "NAZWA ZLECENIA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(orderNameFit.DisplayText)
                            .FontSize((float)orderNameFit.FontSize)
                            .LineHeight(OrderHeaderTextLayout.LineHeight)
                            .FontColor(
                                PdfStyles.OrderNameColor)
                            .Bold();
                    });

                row.ConstantItem(
                        PdfStyles.FirstPageHeaderPageNumberWidth)
                    .Border(PdfStyles.StandardBorderWidth)
                    .Padding(1)
                    .Column(column =>
                    {
                        BuildHeaderLabel(
                            column,
                            "STRONA");

                        column.Item()
                            .ExtendVertical()
                            .AlignCenter()
                            .AlignMiddle()
                            .Text(pageNumberText)
                            .FontSize(13)
                            .Bold();
                    });
            });
    }

    private static void BuildHeaderLabel(
        ColumnDescriptor column,
        string label)
    {
        column.Item()
            .Height(PdfStyles.HeaderOrderLabelHeight)
            .AlignCenter()
            .AlignMiddle()
            .Text(label)
            .FontSize(
                PdfStyles.HeaderOrderLabelFontSize)
            .Bold();
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

    private static string Safe(
        string? value)
    {
        return value?.Trim() ??
               string.Empty;
    }
}
