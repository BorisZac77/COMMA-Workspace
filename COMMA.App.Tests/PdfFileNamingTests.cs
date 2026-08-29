using System.Reflection;
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
