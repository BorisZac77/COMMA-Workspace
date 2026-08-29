using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services.Pdf;
using COMMA.App.Tests.TestSupport;
using COMMA.App.ViewModels;
using PdfSharp.Pdf;
using QuestPDF.Fluent;

namespace COMMA.App.Tests;

public sealed class LegacyPdfCompatibilityTests
{
    [Fact]
    public void LegacyAttachedJson_IsReadWithAllBinnenBouwersEntries()
    {
        using var fixture = new LegacyPdfFixture();

        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);

        Assert.Equal(1, data.FormatVersion);
        Assert.Equal("2.0.0", data.ApplicationVersion);
        Assert.Equal("Rounders bluza", data.ProductName);
        Assert.True(data.ShowFront);
        Assert.True(data.ShowBack);
        Assert.False(data.ShowLeft);
        Assert.False(data.ShowRight);
        Assert.Equal("", data.OrderNumber);
        Assert.Empty(data.Attachments);

        Assert.Equal(2, data.ProductionEntries.Count);
        Assert.Equal("DE BINNEN", data.ProductionEntries[0].LogoName);
        Assert.Equal("7cm", data.ProductionEntries[0].Dimension);
        Assert.Equal(
            "granatowy 533c",
            Assert.Single(data.ProductionEntries[0].Colours).Value);
        Assert.Equal(
            "DE BINNEN / GOOD GEDAAN",
            data.ProductionEntries[1].LogoName);
        Assert.Equal("20cm", data.ProductionEntries[1].Dimension);
        Assert.Equal(
            "granatowy 533c",
            Assert.Single(data.ProductionEntries[1].Colours).Value);
    }

    [Fact]
    public void LegacyProduct_IsMatchedAndMappedToOneFrontBackGarment()
    {
        using var fixture = new LegacyPdfFixture();
        var data = CommaPdfDataReader.Read(fixture.OutputPdfPath);
        var product = CreateRoundersProduct();
        var viewModel = CreateViewModelWithoutConstructor(product);

        var matchedProduct = InvokeFindProductForPdf(
            viewModel,
            data.ProductCode,
            data.ProductName);

        Assert.Same(product, matchedProduct);

        var garment = InvokeCreateLegacyGarment(
            matchedProduct!,
            data);

        var garments = new[] { garment };
        var pages = OrderPageLayoutEngine.BuildPages(garments);

        Assert.Single(garments);
        Assert.Equal(2, garment.SelectedDrawingCount);
        Assert.True(garment.ShowFront);
        Assert.True(garment.ShowBack);
        Assert.False(garment.ShowLeft);
        Assert.False(garment.ShowRight);
        Assert.Equal("", garment.ViewDescriptions.Front);
        Assert.Equal("", garment.ViewDescriptions.Back);
        Assert.Equal("", garment.ViewDescriptions.Right);
        Assert.Equal("", garment.ViewDescriptions.Left);
        Assert.Single(pages);
    }

    private static Product CreateRoundersProduct()
    {
        var product = new Product
        {
            Code = "ROUND-001",
            Name = "Rounders bluza"
        };

        product.Drawings.AddRange(
        [
            new DrawingFile
            {
                Name = "Front",
                IsFront = true
            },
            new DrawingFile
            {
                Name = "Back",
                IsBack = true
            },
            new DrawingFile
            {
                Name = "Left",
                IsLeft = true
            },
            new DrawingFile
            {
                Name = "Right",
                IsRight = true
            }
        ]);

        return product;
    }

    private static MainViewModel CreateViewModelWithoutConstructor(
        Product product)
    {
        var viewModel = (MainViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(MainViewModel));

        var productsField = typeof(MainViewModel).GetField(
            "allProducts",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(productsField);
        productsField.SetValue(viewModel, new List<Product> { product });

        return viewModel;
    }

    private static Product? InvokeFindProductForPdf(
        MainViewModel viewModel,
        string? productCode,
        string? productName)
    {
        var method = typeof(MainViewModel).GetMethod(
            "FindProductForPdf",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);

        return Assert.IsType<Product>(
            method.Invoke(
                viewModel,
                [productCode, productName]));
    }

    private static OrderGarmentItem InvokeCreateLegacyGarment(
        Product product,
        CommaOrderData data)
    {
        var method = typeof(MainViewModel).GetMethod(
            "CreateLegacyGarment",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.NotNull(method);

        return Assert.IsType<OrderGarmentItem>(
            method.Invoke(null, [product, data]));
    }

    private sealed class LegacyPdfFixture : IDisposable
    {
        private readonly TemporaryDirectory directory = new();

        public LegacyPdfFixture()
        {
            var basePdfPath = directory.GetPath("base.pdf");
            var dataPath = directory.GetPath("comma-card.json");
            OutputPdfPath = directory.GetPath("legacy.pdf");

            using (var document = new PdfDocument())
            {
                document.AddPage();
                document.Save(basePdfPath);
            }

            File.WriteAllText(
                dataPath,
                JsonSerializer.Serialize(CreateLegacyData()));

            DocumentOperation
                .LoadFile(basePdfPath)
                .AddAttachment(
                    new DocumentOperation.DocumentAttachment
                    {
                        Key = "comma-card-data",
                        FilePath = dataPath,
                        AttachmentName = "comma-card.json",
                        MimeType = "application/json",
                        Description =
                            "COMMA Workspace production card data",
                        Relationship =
                            DocumentOperation
                                .DocumentAttachmentRelationship
                                .Data,
                        CreationDate = DateTime.UtcNow,
                        ModificationDate = DateTime.UtcNow,
                        Replace = true
                    })
                .Save(OutputPdfPath);
        }

        public string OutputPdfPath { get; }

        public void Dispose()
        {
            directory.Dispose();
        }

        private static object CreateLegacyData()
        {
            return new
            {
                Format = "COMMA Workspace Production Card",
                FormatVersion = 1,
                ApplicationVersion = "2.0.0",
                SavedUtc = DateTime.UtcNow,
                OrderName = "BINNEN BOUWERS",
                Customer = "Binnen Bouwers",
                DueDate = "31.08.2026",
                ProductionType = "HAFT",
                ProductCode = "ROUND-001",
                ProductName = "Rounders bluza",
                Colour = "granatowy",
                Size = "",
                Quantity = "",
                Notes = "",
                ShowFront = true,
                ShowBack = true,
                ShowLeft = false,
                ShowRight = false,
                ProductionEntries = new[]
                {
                    new
                    {
                        Number = 1,
                        LogoName = "DE BINNEN",
                        Dimension = "7cm",
                        Colours = new[]
                        {
                            new
                            {
                                Number = 1,
                                Value = "granatowy 533c"
                            }
                        }
                    },
                    new
                    {
                        Number = 2,
                        LogoName = "DE BINNEN / GOOD GEDAAN",
                        Dimension = "20cm",
                        Colours = new[]
                        {
                            new
                            {
                                Number = 1,
                                Value = "granatowy 533c"
                            }
                        }
                    }
                }
            };
        }
    }
}
