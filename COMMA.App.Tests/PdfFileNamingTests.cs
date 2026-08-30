using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
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

    [Theory]
    [InlineData(3)]
    [InlineData(4)]
    public void LoadedPdf_SelectedEmptyFolder_SavesOnlyInSelectedFolder(
        int restoredFormatVersion)
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = directory.GetPath("source-a");
        var outputDirectory = directory.GetPath("output-b");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(outputDirectory);

        var restoredData = new COMMA.App.Services.Pdf.CommaOrderData
        {
            FormatVersion = restoredFormatVersion,
            OrderName = "TEST NOWA KARTA"
        };
        var sourcePath = Path.Combine(
            sourceDirectory,
            "TEST NOWA KARTA.pdf");
        File.WriteAllBytes(sourcePath, "source-pdf"u8.ToArray());
        var sourceHash = GetSha256(sourcePath);

        var plan = CreateSavePlan(
            outputDirectory,
            restoredData.OrderName,
            sourcePath,
            isSameDocument: true,
            outputFolderSelectedSincePdfLoad: true);

        Assert.False(GetPlanValue<bool>(plan, "HasConflict"));
        Assert.Equal(
            outputDirectory,
            GetPlanValue<string>(plan, "OutputDirectory"));

        var outputPath =
            GetPlanValue<string>(plan, "OverwriteOutputFile");
        Assert.Equal(
            Path.Combine(outputDirectory, "TEST NOWA KARTA.pdf"),
            outputPath);

        File.WriteAllBytes(outputPath, "new-pdf"u8.ToArray());

        Assert.True(File.Exists(outputPath));
        Assert.Equal(sourceHash, GetSha256(sourcePath));
        Assert.Equal("source-pdf"u8.ToArray(), File.ReadAllBytes(sourcePath));
    }

    [Fact]
    public void SameNameExistingOnlyInSourceFolder_DoesNotCreateFalseConflict()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = directory.GetPath("source-a");
        var outputDirectory = directory.GetPath("output-b");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(outputDirectory);

        var sourcePath = Path.Combine(sourceDirectory, "ORDER.pdf");
        File.WriteAllBytes(sourcePath, "source"u8.ToArray());

        var plan = CreateSavePlan(
            outputDirectory,
            "ORDER",
            sourcePath,
            isSameDocument: true,
            outputFolderSelectedSincePdfLoad: true);

        Assert.False(GetPlanValue<bool>(plan, "HasConflict"));
        Assert.Equal(
            Path.Combine(outputDirectory, "ORDER.pdf"),
            GetPlanValue<string>(plan, "OverwriteOutputFile"));
    }

    [Fact]
    public void SameNameExistingInSelectedFolder_ConflictAndBothChoicesStayThere()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = directory.GetPath("source-a");
        var outputDirectory = directory.GetPath("output-b");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(outputDirectory);

        var sourcePath = Path.Combine(sourceDirectory, "ORDER.pdf");
        var targetPath = Path.Combine(outputDirectory, "ORDER.pdf");
        File.WriteAllBytes(sourcePath, "source"u8.ToArray());
        File.WriteAllBytes(targetPath, "target-old"u8.ToArray());
        File.WriteAllBytes(
            Path.Combine(outputDirectory, "ORDER_1.pdf"),
            "target-number-one"u8.ToArray());
        var sourceHash = GetSha256(sourcePath);

        var plan = CreateSavePlan(
            outputDirectory,
            "ORDER",
            sourcePath,
            isSameDocument: true,
            outputFolderSelectedSincePdfLoad: true);

        Assert.True(GetPlanValue<bool>(plan, "HasConflict"));
        Assert.Equal(
            targetPath,
            GetPlanValue<string>(plan, "OverwriteOutputFile"));
        Assert.Equal(
            "ORDER_2.pdf",
            GetPlanValue<string>(plan, "SuggestedPdfFileName"));

        var createNewPath =
            GetPlanValue<string>(plan, "CreateNewOutputFile");
        File.WriteAllBytes(createNewPath, "target-new"u8.ToArray());
        Assert.Equal(outputDirectory, Path.GetDirectoryName(createNewPath));
        Assert.True(File.Exists(Path.Combine(outputDirectory, "ORDER_2.pdf")));

        File.WriteAllBytes(
            GetPlanValue<string>(plan, "OverwriteOutputFile"),
            "target-overwritten"u8.ToArray());

        Assert.Equal(
            "target-overwritten"u8.ToArray(),
            File.ReadAllBytes(targetPath));
        Assert.Equal(sourceHash, GetSha256(sourcePath));
        Assert.Equal("source"u8.ToArray(), File.ReadAllBytes(sourcePath));
    }

    [Fact]
    public void CancelledFolderSelection_KeepsPreviousDestinationAndOverrideState()
    {
        using var directory = new TemporaryDirectory();
        var outputDirectory = directory.GetPath("output-b");
        Directory.CreateDirectory(outputDirectory);

        var viewModel = (MainViewModel)
            RuntimeHelpers.GetUninitializedObject(typeof(MainViewModel));

        Assert.True(
            InvokePrivateInstance<bool>(
                viewModel,
                "TryApplyPdfOutputFolderSelection",
                outputDirectory));

        Assert.False(
            InvokePrivateInstance<bool>(
                viewModel,
                "TryApplyPdfOutputFolderSelection",
                (object?)null));

        Assert.Equal(outputDirectory, viewModel.PdfOutputPath);

        var selectionField = typeof(MainViewModel).GetField(
            "pdfOutputFolderSelectedSincePdfLoad",
            BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(selectionField);
        Assert.True(Assert.IsType<bool>(selectionField.GetValue(viewModel)));
    }

    [Fact]
    public void WithoutNewFolderSelection_LoadedPdfKeepsExistingSafeSaveBehaviour()
    {
        using var directory = new TemporaryDirectory();
        var sourceDirectory = directory.GetPath("source-a");
        var savedOutputDirectory = directory.GetPath("saved-output");
        Directory.CreateDirectory(sourceDirectory);
        Directory.CreateDirectory(savedOutputDirectory);

        var sourcePath = Path.Combine(sourceDirectory, "ORDER.pdf");
        File.WriteAllBytes(sourcePath, "source"u8.ToArray());

        var plan = CreateSavePlan(
            savedOutputDirectory,
            "ORDER",
            sourcePath,
            isSameDocument: true,
            outputFolderSelectedSincePdfLoad: false);

        Assert.True(GetPlanValue<bool>(plan, "HasConflict"));
        Assert.Equal(
            sourceDirectory,
            GetPlanValue<string>(plan, "OutputDirectory"));
        Assert.Equal(
            sourcePath,
            GetPlanValue<string>(plan, "OverwriteOutputFile"));
    }

    private static object CreateSavePlan(
        string selectedOutputDirectory,
        string orderName,
        string loadedPdfPath,
        bool isSameDocument,
        bool outputFolderSelectedSincePdfLoad)
    {
        var method = typeof(MainViewModel).GetMethod(
            "CreatePdfSavePlan",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);

        return Assert.IsAssignableFrom<object>(
            method.Invoke(
                null,
                [
                    selectedOutputDirectory,
                    orderName,
                    loadedPdfPath,
                    isSameDocument,
                    outputFolderSelectedSincePdfLoad
                ]));
    }

    private static T GetPlanValue<T>(object plan, string propertyName)
    {
        var property = plan.GetType().GetProperty(propertyName);
        Assert.NotNull(property);
        return Assert.IsType<T>(property.GetValue(plan));
    }

    private static TResult InvokePrivateInstance<TResult>(
        MainViewModel viewModel,
        string methodName,
        params object?[] arguments)
    {
        var method = typeof(MainViewModel).GetMethod(
            methodName,
            BindingFlags.NonPublic | BindingFlags.Instance);

        Assert.NotNull(method);

        return Assert.IsType<TResult>(
            method.Invoke(viewModel, arguments));
    }

    private static string GetSha256(string path)
    {
        return Convert.ToHexString(
                SHA256.HashData(File.ReadAllBytes(path)))
            .ToLowerInvariant();
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
