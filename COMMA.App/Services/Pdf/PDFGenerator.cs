using System.IO;
using COMMA.App.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace COMMA.App.Services;

public static class PdfGenerator
{
    public static void Generate(string outputPath)
    {
        Generate(outputPath, new ProductionCard
        {
            OrderName = "PLOPSA"
        });
    }

    public static void Generate(string outputPath, ProductionCard card)
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
                        BuildHeader(column, card);

                        column.Item()
                            .PaddingTop(8);

                        BuildProductSection(column, card);

                        column.Item()
                            .PaddingTop(8);

                        BuildLogoSection(column, card);
                    });
            });
        })
        .GeneratePdf(outputPath);
    }

    private static void BuildHeader(ColumnDescriptor column, ProductionCard card)
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

                table.Cell()
                    .Border(1)
                    .Padding(8)
                    .AlignCenter()
                    .AlignMiddle()
                    .Image("Assets/Templates/PimpLogo.png");

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
                            .Text(card.OrderName)
                            .FontSize(24)
                            .Bold();
                    });
            });
    }

    private static void BuildProductSection(ColumnDescriptor column, ProductionCard card)
    {
        column.Item()
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(220);
                    columns.RelativeColumn();
                });

                table.Cell()
                    .Border(1)
                    .Height(240)
                    .AlignCenter()
                    .AlignMiddle()
                    .Element(container =>
                    {
                        if (!string.IsNullOrWhiteSpace(card.ProductImagePath) &&
                            File.Exists(card.ProductImagePath))
                        {
                            container.Image(card.ProductImagePath);
                        }
                        else
                        {
                            container.Text("BRAK ZDJĘCIA")
                                .Bold();
                        }
                    });

                table.Cell()
                    .Border(1)
                    .Padding(6)
                    .Table(details =>
                    {
                        details.ColumnsDefinition(c =>
                        {
                            c.ConstantColumn(150);
                            c.RelativeColumn();
                        });

                        AddRow(details, "Kod", card.ProductCode);
                        AddRow(details, "Produkt", card.ProductName);
                        AddRow(details, "Klient", card.Customer);
                        AddRow(details, "Kolor", card.Colour);
                        AddRow(details, "Rozmiar", card.Size);
                        AddRow(details, "Ilość", card.Quantity);
                        AddRow(details, "Uwagi", card.Notes);
                    });
            });
    }

    private static void BuildLogoSection(ColumnDescriptor column, ProductionCard card)
    {
        column.Item()
            .PaddingTop(8)
            .Text("LOGOTYPY")
            .FontSize(16)
            .Bold();

        column.Item()
            .Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn(2);
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                Header(table, "Nazwa");
                Header(table, "Pozycja");
                Header(table, "Szer.");
                Header(table, "Wys.");
                Header(table, "Kolory");
                Header(table, "Technika");

                foreach (var logo in card.Logos)
                {
                    Cell(table, logo.Name);
                    Cell(table, logo.Position);
                    Cell(table, logo.Width);
                    Cell(table, logo.Height);
                    Cell(table, logo.Colours);
                    Cell(table, logo.Technique);
                }

                if (card.Logos.Count == 0)
                {
                    table.Cell()
                        .ColumnSpan(6)
                        .Border(1)
                        .Padding(5)
                        .Text("Brak logotypów.");
                }
            });
    }

    private static void AddRow(TableDescriptor table, string title, string value)
    {
        table.Cell()
            .Border(1)
            .Padding(4)
            .Text(title)
            .Bold();

        table.Cell()
            .Border(1)
            .Padding(4)
            .Text(value);
    }

    private static void Header(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .Background(Colors.Grey.Lighten2)
            .Padding(4)
            .Text(text)
            .Bold();
    }

    private static void Cell(TableDescriptor table, string text)
    {
        table.Cell()
            .Border(1)
            .Padding(4)
            .Text(text);
    }
}