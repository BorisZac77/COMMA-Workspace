using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services;

public static class PdfGenerator
{
    public static void Generate(string outputPath)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        Document.Create(document =>
        {
            document.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(8);

                page.Content()
                    .Border(1)
                    .Padding(10)
                    .Column(column =>
                    {
                        column.Item()
                            .Height(90)
                            .Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.ConstantColumn(150);
                                    columns.RelativeColumn();
                                    columns.ConstantColumn(220);
                                });

                                // LOGO
                                table.Cell()
                                    .Border(1)
                                    .Padding(8)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Image("Assets/Templates/PimpLogo.png");

                                // TYTUŁ
                                table.Cell()
                                    .Border(1)
                                    .AlignCenter()
                                    .AlignMiddle()
                                    .Text(text =>
                                    {
                                        text.AlignCenter();

                                        text.Span("KARTA PRODUKCYJNA\n")
                                            .FontSize(22)
                                            .Bold();

                                        text.Span("HAFTU")
                                            .FontSize(22)
                                            .Bold();
                                    });

                                // PRAWA KOLUMNA
                                table.Cell()
                                    .Border(1)
                                    .Padding(8)
                                    .Column(right =>
                                    {
                                        right.Item()
                                            .AlignCenter()
                                            .Text("NAZWA ZLECENIA")
                                            .FontSize(10)
                                            .Bold();

                                        right.Item()
                                            .PaddingTop(12)
                                            .AlignCenter()
                                            .Text("PLOPSA")
                                            .FontSize(24)
                                            .Bold();
                                    });
                            });
                    });
            });
        })
        .GeneratePdf(outputPath);
    }
}