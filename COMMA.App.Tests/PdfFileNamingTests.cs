using System.Reflection;
using COMMA.App.Models;
using COMMA.App.ViewModels;
using COMMA.App.Tests.TestSupport;

namespace COMMA.App.Tests;

public sealed class PdfFileNamingTests
{
    [Fact]
    public void PdfFileName_IsCreatedFromOrderName()
    {
        var fileName = InvokePrivateStatic<string>(
            "CreatePdfFileName",
            "Zlecenie STAFF 2026");

        Assert.Equal("Zlecenie STAFF 2026.pdf", fileName);
    }

    [Fact]
    public void ExistingPdfWithSameOrderName_IsTheSameDocument()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("existing.pdf");
        File.WriteAllBytes(pdfPath, "%PDF"u8.ToArray());

        var isSameDocument = InvokePrivateStatic<bool>(
            "IsSameDocument",
            pdfPath,
            "  BINNEN BOUWERS  ",
            "binnen bouwers");

        Assert.True(isSameDocument);
    }

    [Fact]
    public void ChangedOrderName_IsTreatedAsANewDocumentName()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("existing.pdf");
        File.WriteAllBytes(pdfPath, "%PDF"u8.ToArray());

        var isSameDocument = InvokePrivateStatic<bool>(
            "IsSameDocument",
            pdfPath,
            "New order name",
            "Old order name");

        Assert.False(isSameDocument);
    }

    [Fact]
    public void MissingLoadedPdf_IsNotTheSameDocument()
    {
        var isSameDocument = InvokePrivateStatic<bool>(
            "IsSameDocument",
            null,
            "BINNEN BOUWERS",
            "BINNEN BOUWERS");

        Assert.False(isSameDocument);
    }

    [Fact]
    public void OrderNumber_DoesNotAffectPdfFileName()
    {
        var firstCard = new ProductionCard
        {
            OrderNumber = "ZL-001",
            OrderName = "BINNEN BOUWERS"
        };
        var secondCard = new ProductionCard
        {
            OrderNumber = "ZL-999",
            OrderName = "BINNEN BOUWERS"
        };

        var firstName = InvokePrivateStatic<string>(
            "CreatePdfFileName",
            firstCard.OrderName);
        var secondName = InvokePrivateStatic<string>(
            "CreatePdfFileName",
            secondCard.OrderName);

        Assert.Equal(firstName, secondName);
        Assert.Equal("BINNEN BOUWERS.pdf", firstName);
    }

    [Fact]
    public void OrderNumber_DoesNotAffectSameDocumentIdentity()
    {
        using var directory = new TemporaryDirectory();
        var pdfPath = directory.GetPath("existing.pdf");
        File.WriteAllBytes(pdfPath, "%PDF"u8.ToArray());

        var loadedCard = new ProductionCard
        {
            OrderNumber = "OLD-001",
            OrderName = "BINNEN BOUWERS"
        };
        var currentCard = new ProductionCard
        {
            OrderNumber = "NEW-999",
            OrderName = "BINNEN BOUWERS"
        };

        var isSameDocument = InvokePrivateStatic<bool>(
            "IsSameDocument",
            pdfPath,
            currentCard.OrderName,
            loadedCard.OrderName);

        Assert.True(isSameDocument);
    }

    private static TResult InvokePrivateStatic<TResult>(
        string methodName,
        params object?[] arguments)
    {
        var method = typeof(MainViewModel).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return Assert.IsType<TResult>(
            method.Invoke(null, arguments));
    }
}
