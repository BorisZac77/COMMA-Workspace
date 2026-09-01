using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using COMMA.App.Layout;
using COMMA.App.Models;
using COMMA.App.Services;
using COMMA.App.Services.Attachments;
using COMMA.App.Services.Pdf;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.App.ViewModels;

public partial class MainViewModel : ViewModelBase, IDisposable
{
    private const string SettingsFolderName =
        "COMMA Workspace";

    private const string LibraryPathFileName =
        "library-path.txt";

    private const string PdfOutputPathFileName =
        "pdf-output-path.txt";

    private const string DefaultPdfFileName =
        "Karta_produkcyjna";

    private readonly LibraryScanner libraryScanner = new();

    private readonly ProductionCardBuilder productionCardBuilder = new();

    private readonly List<Product> allProducts = new();

    public OrderAttachmentManager AttachmentManager { get; } = new();

    private int pdfStatusVersion;

    private string? loadedPdfPath;

    private string? loadedOrderName;

    private int? loadedPdfFormatVersion;

    private bool pdfOutputFolderSelectedSincePdfLoad;

    [ObservableProperty]
    private string libraryPath =
        "Nie wybrano biblioteki";

    [ObservableProperty]
    private string pdfOutputPath =
        GetDefaultPdfOutputPath();

    [ObservableProperty]
    private string pdfStatus = "";

    [ObservableProperty]
    private string searchText = "";

    public ObservableCollection<Product> Products { get; } = new();

    public ObservableCollection<DrawingFile> Drawings { get; } = new();

    public string AttachmentsButtonText =>
        $"ZAŁĄCZNIKI ({ProductionCard?.Attachments.Count ?? 0})";

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private DrawingFile? selectedDrawing;

    [ObservableProperty]
    private Bitmap? selectedImage;

    [ObservableProperty]
    private ProductionCard? productionCard;

    public IAsyncRelayCommand LoadPdfCommand { get; }

    public MainViewModel()
    {
        LoadPdfCommand =
            new AsyncRelayCommand(
                LoadPdf);

        Garments.CollectionChanged +=
            (_, _) =>
            {
                if (Garments.Count == 0)
                {
                    loadedPdfPath =
                        null;

                    loadedOrderName =
                        null;

                    loadedPdfFormatVersion =
                        null;

                    pdfOutputFolderSelectedSincePdfLoad =
                        false;
                }
            };

        TryLoadSavedLibrary();

        TryLoadSavedPdfOutputPath();
    }

    partial void OnProductionCardChanging(
        ProductionCard? value)
    {
        if (ProductionCard != null)
        {
            ProductionCard.Attachments.CollectionChanged -=
                OnAttachmentsCollectionChanged;
        }
    }

    partial void OnProductionCardChanged(
        ProductionCard? value)
    {
        if (value != null)
        {
            value.Attachments.CollectionChanged +=
                OnAttachmentsCollectionChanged;
        }

        OnPropertyChanged(nameof(AttachmentsButtonText));
        RebuildAttachmentPreviewPages();
    }

    private void OnAttachmentsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(AttachmentsButtonText));
        RebuildAttachmentPreviewPages();
    }

    private void AdoptLoadedAttachmentContents(CommaOrderData data)
    {
        AttachmentManager.ReplaceContentStore(
            data.DetachAttachmentContentStore());
        RebuildAttachmentPreviewPages();
    }

    public void Dispose()
    {
        AttachmentManager.Dispose();
        previewAttachmentImage?.Dispose();
        previewAttachmentImage = null;
        DisposeSelectedImage();
    }

    partial void OnSearchTextChanged(
        string value)
    {
        ApplyProductFilter();
    }

    partial void OnSelectedProductChanged(
        Product? value)
    {
        Drawings.Clear();
        SelectedDrawing = null;

        DisposeSelectedImage();

        ClearPdfStatus();

        if (value == null)
            return;

        var previousCard =
            ProductionCard;

        var newCard =
            productionCardBuilder.Build(value);

        if (previousCard != null)
        {
            newCard.OrderNumber =
                previousCard.OrderNumber;

            newCard.Customer =
                previousCard.Customer;

            newCard.OrderName =
                previousCard.OrderName;

            newCard.ReceivedDate =
                previousCard.ReceivedDate;

            newCard.DueDate =
                previousCard.DueDate;

            newCard.ProductionType =
                previousCard.ProductionType;

            newCard.Notes =
                previousCard.Notes;

            newCard.ProductionEntries.Clear();

            foreach (var entry in previousCard.ProductionEntries)
            {
                var copiedEntry =
                    new ProductionEntry(entry.Number)
                    {
                        LogoName = entry.LogoName,
                        Dimension = entry.Dimension
                    };

                foreach (var colour in entry.Colours)
                {
                    copiedEntry.Colours.Add(
                        new ProductionColourEntry(colour.Number)
                        {
                            Value = colour.Value
                        });

                    copiedEntry.Colours[^1].Number =
                        colour.Number;
                }

                newCard.ProductionEntries.Add(copiedEntry);
            }

            foreach (var attachment in previousCard.Attachments)
            {
                newCard.Attachments.Add(
                    new OrderAttachmentMetadata
                    {
                        Id = attachment.Id,
                        Name = attachment.Name,
                        MimeType = attachment.MimeType,
                        Extension = attachment.Extension,
                        Order = attachment.Order,
                        Length = attachment.Length,
                        Sha256 = attachment.Sha256,
                        BlobEntry = attachment.BlobEntry,
                        PdfPageCount = attachment.PdfPageCount
                    });
            }
        }

        ProductionCard =
            newCard;

        foreach (var drawing in ProductionCard.Drawings)
            Drawings.Add(drawing);

        if (!string.IsNullOrWhiteSpace(value.ImagePath) &&
            File.Exists(value.ImagePath))
        {
            try
            {
                SelectedImage =
                    new Bitmap(value.ImagePath);
            }
            catch
            {
                SelectedImage = null;
            }
        }
    }

    partial void OnSelectedDrawingChanged(
        DrawingFile? value)
    {
        DisposeSelectedImage();

        if (value == null)
            return;

        if (!File.Exists(value.FullPath))
            return;

        try
        {
            SelectedImage =
                new Bitmap(value.FullPath);
        }
        catch
        {
            SelectedImage = null;
        }
    }

    [RelayCommand]
    private async Task OpenLibrary()
    {
        var window =
            GetMainWindow();

        if (window == null)
            return;

        var folders =
            await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Wybierz bibliotekę COMMA",
                    AllowMultiple =
                        false
                });

        if (folders.Count == 0)
            return;

        var path =
            folders[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Directory.Exists(path))
        {
            SetPdfStatus(
                "Wybrany folder nie istnieje.");

            return;
        }

        LoadLibrary(path);

        SaveLibraryPath(path);
    }

    [RelayCommand]
    private async Task SelectPdfOutputFolder()
    {
        var window =
            GetMainWindow();

        if (window == null)
            return;

        var suggestedStartLocation =
            await window.StorageProvider.TryGetFolderFromPathAsync(
                GetEffectivePdfOutputPath());

        var folders =
            await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Wybierz folder zapisu PDF",
                    AllowMultiple =
                        false,
                    SuggestedStartLocation =
                        suggestedStartLocation
                });

        if (folders.Count == 0)
            return;

        var path =
            folders[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!Directory.Exists(path))
        {
            SetPdfStatus(
                "Wybrany folder nie istnieje.");

            return;
        }

        if (!TryApplyPdfOutputFolderSelection(
                path))
        {
            return;
        }

        SavePdfOutputPath(
            path);

        await ShowTemporaryPdfStatus(
            $"✓ Folder zapisu PDF: {path}",
            3000);
    }

    private bool TryApplyPdfOutputFolderSelection(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path) ||
            !Directory.Exists(path))
        {
            return false;
        }

        PdfOutputPath =
            path;

        pdfOutputFolderSelectedSincePdfLoad =
            true;

        return true;
    }

    [RelayCommand]
    private void RefreshLibrary()
    {
        if (string.IsNullOrWhiteSpace(LibraryPath) ||
            LibraryPath == "Nie wybrano biblioteki")
        {
            SetPdfStatus(
                "Najpierw wybierz bibliotekę.");

            return;
        }

        if (!Directory.Exists(LibraryPath))
        {
            SetPdfStatus(
                "Biblioteka nie istnieje.");

            return;
        }

        try
        {
            var currentProduct =
                SelectedProduct;

            LoadLibrary(
                LibraryPath);

            if (currentProduct == null)
                return;

            var refreshedProduct =
                Products.FirstOrDefault(product =>
                    product.Folder ==
                    currentProduct.Folder);

            if (refreshedProduct != null)
            {
                SelectedProduct =
                    refreshedProduct;
            }

            SetPdfStatus(
                "✓ Biblioteka została odświeżona.");
        }
        catch (Exception exception)
        {
            SetPdfStatus(
                "Błąd odświeżania biblioteki: " +
                exception.Message);
        }
    }

    private static Window? GetMainWindow()
    {
        if (Application.Current?.ApplicationLifetime
            is not IClassicDesktopStyleApplicationLifetime desktop)
        {
            return null;
        }

        return desktop.MainWindow;
    }

    private void TryLoadSavedLibrary()
    {
        var settingsFile =
            GetLibraryPathFile();

        if (!File.Exists(settingsFile))
            return;

        try
        {
            var savedPath =
                File.ReadAllText(
                        settingsFile)
                    .Trim();

            if (string.IsNullOrWhiteSpace(savedPath))
                return;

            if (!Directory.Exists(savedPath))
            {
                LibraryPath =
                    "Zapisana biblioteka nie istnieje";

                SetPdfStatus(
                    "Poprzednia biblioteka została usunięta " +
                    "lub przeniesiona. Kliknij ZMIEŃ BIBLIOTEKĘ " +
                    "i wskaż właściwy folder.");

                return;
            }

            LoadLibrary(
                savedPath);
        }
        catch (Exception exception)
        {
            LibraryPath =
                "Nie udało się wczytać biblioteki";

            SetPdfStatus(
                "Błąd automatycznego ładowania biblioteki: " +
                exception.Message);
        }
    }

    private void TryLoadSavedPdfOutputPath()
    {
        var defaultPath =
            GetDefaultPdfOutputPath();

        PdfOutputPath =
            defaultPath;

        var settingsFile =
            GetPdfOutputPathFile();

        if (!File.Exists(settingsFile))
            return;

        try
        {
            var savedPath =
                File.ReadAllText(
                        settingsFile)
                    .Trim();

            if (string.IsNullOrWhiteSpace(savedPath))
                return;

            if (!Directory.Exists(savedPath))
            {
                PdfOutputPath =
                    defaultPath;

                return;
            }

            PdfOutputPath =
                savedPath;
        }
        catch
        {
            PdfOutputPath =
                defaultPath;
        }
    }

    private void LoadLibrary(
        string path)
    {
        ClearCurrentSelection();

        allProducts.Clear();

        Products.Clear();

        SearchText = "";

        LibraryPath =
            path;

        ClearPdfStatus();

        foreach (var product in libraryScanner.Scan(path))
        {
            allProducts.Add(
                product);
        }

        if (allProducts.Count == 0)
        {
            SetPdfStatus(
                "W wybranym folderze nie znaleziono " +
                "katalogów z odzieżą.");

            return;
        }

        ApplyProductFilter();
    }

    private void ApplyProductFilter()
    {
        var previousSelection =
            SelectedProduct;

        var searchValue =
            SearchText.Trim();

        var filteredProducts =
            string.IsNullOrWhiteSpace(searchValue)
                ? allProducts
                : allProducts
                    .Where(product =>
                        MatchesSearch(
                            product,
                            searchValue))
                    .ToList();

        Products.Clear();

        foreach (var product in filteredProducts)
        {
            Products.Add(
                product);
        }

        if (Products.Count == 0)
        {
            SelectedProduct =
                null;

            if (string.IsNullOrWhiteSpace(searchValue))
            {
                ClearPdfStatus();
            }
            else
            {
                SetPdfStatus(
                    $"Nie znaleziono produktów dla: {searchValue}");
            }

            return;
        }

        ClearPdfStatus();

        if (previousSelection != null &&
            Products.Contains(previousSelection))
        {
            SelectedProduct =
                previousSelection;

            return;
        }

        SelectedProduct =
            Products[0];
    }

    private static bool MatchesSearch(
        Product product,
        string searchValue)
    {
        return ContainsIgnoreCase(
                   product.Name,
                   searchValue)
               || ContainsIgnoreCase(
                   product.Code,
                   searchValue)
               || ContainsIgnoreCase(
                   product.DisplayName,
                   searchValue)
               || ContainsIgnoreCase(
                   product.DisplayCode,
                   searchValue);
    }

    private static bool ContainsIgnoreCase(
        string? source,
        string searchValue)
    {
        if (string.IsNullOrWhiteSpace(source))
            return false;

        return source.Contains(
            searchValue,
            StringComparison.OrdinalIgnoreCase);
    }

    private void ClearCurrentSelection()
    {
        SelectedProduct =
            null;

        SelectedDrawing =
            null;

        ProductionCard =
            null;

        Drawings.Clear();

        DisposeSelectedImage();
    }

    private static void SaveLibraryPath(
        string path)
    {
        try
        {
            var settingsFile =
                GetLibraryPathFile();

            var settingsDirectory =
                Path.GetDirectoryName(
                    settingsFile);

            if (!string.IsNullOrWhiteSpace(
                    settingsDirectory))
            {
                Directory.CreateDirectory(
                    settingsDirectory);
            }

            File.WriteAllText(
                settingsFile,
                path);
        }
        catch
        {
        }
    }

    private static void SavePdfOutputPath(
        string path)
    {
        try
        {
            var settingsFile =
                GetPdfOutputPathFile();

            var settingsDirectory =
                Path.GetDirectoryName(
                    settingsFile);

            if (!string.IsNullOrWhiteSpace(
                    settingsDirectory))
            {
                Directory.CreateDirectory(
                    settingsDirectory);
            }

            File.WriteAllText(
                settingsFile,
                path);
        }
        catch
        {
        }
    }

    private static string GetLibraryPathFile()
    {
        var applicationDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        return Path.Combine(
            applicationDataPath,
            SettingsFolderName,
            LibraryPathFileName);
    }

    private static string GetPdfOutputPathFile()
    {
        var applicationDataPath =
            Environment.GetFolderPath(
                Environment.SpecialFolder.ApplicationData);

        return Path.Combine(
            applicationDataPath,
            SettingsFolderName,
            PdfOutputPathFileName);
    }

    private static string GetDefaultPdfOutputPath()
    {
        return Environment.GetFolderPath(
            Environment.SpecialFolder.DesktopDirectory);
    }

    private string GetEffectivePdfOutputPath()
    {
        if (!string.IsNullOrWhiteSpace(
                PdfOutputPath) &&
            Directory.Exists(
                PdfOutputPath))
        {
            return PdfOutputPath;
        }

        var defaultPath =
            GetDefaultPdfOutputPath();

        PdfOutputPath =
            defaultPath;

        return defaultPath;
    }

    private void DisposeSelectedImage()
    {
        SelectedImage?.Dispose();

        SelectedImage =
            null;
    }

    private async Task LoadPdf()
    {
        ClearPdfStatus();

        var window =
            GetMainWindow();

        if (window == null)
            return;

        var files =
            await window.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title =
                        "Wczytaj kartę produkcyjną PDF",
                    AllowMultiple =
                        false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType(
                            "Pliki PDF")
                        {
                            Patterns =
                            [
                                "*.pdf"
                            ]
                        }
                    ]
                });

        if (files.Count == 0)
            return;

        var pdfPath =
            files[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(pdfPath) ||
            !File.Exists(pdfPath))
        {
            SetPdfStatus(
                "Nie udało się otworzyć wybranego pliku PDF.");

            return;
        }

        try
        {
            using var data =
                CommaPdfDataReader.Read(
                    pdfPath);

            if (data.FormatVersion is 3 or 4 &&
                data.Garments.Count > 0)
            {
                var restoredGarments =
                    new List<(
                        OrderGarmentItem Garment,
                        Product Product)>();

                var missingProducts =
                    new List<string>();

                foreach (var garmentData in data.Garments)
                {
                    var garmentProduct =
                        FindProductForPdf(
                            garmentData.ProductCode,
                            !string.IsNullOrWhiteSpace(
                                garmentData.ProductName)
                                ? garmentData.ProductName
                                : garmentData.Name);

                    if (garmentProduct == null)
                    {
                        var missingName =
                            !string.IsNullOrWhiteSpace(
                                garmentData.Name)
                                ? garmentData.Name
                                : !string.IsNullOrWhiteSpace(
                                    garmentData.ProductName)
                                    ? garmentData.ProductName
                                    : garmentData.ProductCode;

                        missingProducts.Add(
                            string.IsNullOrWhiteSpace(
                                missingName)
                                ? "Nieznany produkt"
                                : missingName);

                        continue;
                    }

                    var garment =
                        new OrderGarmentItem();

                    garment.LoadProduct(
                        garmentProduct);

                    if (!string.IsNullOrWhiteSpace(
                            garmentData.Name))
                    {
                        garment.Name =
                            garmentData.Name;
                    }

                    garment.Colour =
                        garmentData.Colour ?? "";

                    garment.Variant =
                        garmentData.Variant ?? "";

                    garment.ShowFront =
                        garmentData.ShowFront;

                    garment.ShowBack =
                        garmentData.ShowBack;

                    garment.ShowRight =
                        garmentData.ShowRight;

                    garment.ShowLeft =
                        garmentData.ShowLeft;

                    garment.StartNewPage =
                        garmentData.StartNewPage;

                    garment.ViewDescriptions.Front =
                        garmentData.ViewDescriptions.Front;

                    garment.ViewDescriptions.Back =
                        garmentData.ViewDescriptions.Back;

                    garment.ViewDescriptions.Right =
                        garmentData.ViewDescriptions.Right;

                    garment.ViewDescriptions.Left =
                        garmentData.ViewDescriptions.Left;

                    garment.RefreshDrawingSelection();

                    restoredGarments.Add(
                        (
                            garment,
                            garmentProduct
                        ));
                }

                if (missingProducts.Count > 0)
                {
                    SetPdfStatus(
                        "Nie znaleziono w aktualnej bibliotece: " +
                        string.Join(
                            ", ",
                            missingProducts));

                    return;
                }

                if (restoredGarments.Count == 0)
                {
                    SetPdfStatus(
                        "PDF nie zawiera odzieży możliwej do odtworzenia.");

                    return;
                }

                SearchText =
                    "";

                SelectedProduct =
                    restoredGarments[0].Product;

                if (ProductionCard == null)
                {
                    SetPdfStatus(
                        "Nie udało się utworzyć karty dla produktu z PDF.");

                    return;
                }

                RestoreCardFromPdf(
                    ProductionCard,
                    data);

                Garments.Clear();

                foreach (var restored in restoredGarments)
                {
                    Garments.Add(
                        restored.Garment);
                }

                Garments[0].StartNewPage =
                    false;

                SelectedGarment =
                    Garments[0];

                RebuildOrderPages();

                SetLoadedPdfIdentity(
                    pdfPath,
                    data.OrderName,
                    data.FormatVersion);

                AdoptLoadedAttachmentContents(data);

                await ShowTemporaryPdfStatus(
                    $"✓ Wczytano kartę z PDF: {Path.GetFileName(pdfPath)}",
                    4000);

                return;
            }

            var product =
                FindProductForPdf(
                    data.ProductCode,
                    data.ProductName);

            if (product == null)
            {
                SearchText =
                    "";

                SelectedProduct =
                    null;

                ProductionCard =
                    new ProductionCard();

                RestoreLegacyCardFromPdf(
                    ProductionCard,
                    data);

                Garments.Clear();

                SelectedGarment =
                    null;

                RebuildOrderPages();

                SetLoadedPdfIdentity(
                    pdfPath,
                    data.OrderName,
                    data.FormatVersion);

                AdoptLoadedAttachmentContents(data);

                SetPdfStatus(
                    "Wczytano dane z PDF, ale nie znaleziono produktu " +
                    $"w aktualnej bibliotece: {data.ProductName}");

                return;
            }

            SearchText =
                "";

            SelectedProduct =
                product;

            if (ProductionCard == null)
            {
                SetPdfStatus(
                    "Nie udało się utworzyć karty dla produktu z PDF.");

                return;
            }

            RestoreLegacyCardFromPdf(
                ProductionCard,
                data);

            var legacyGarment =
                CreateLegacyGarment(
                    product,
                    data);

            Garments.Clear();

            Garments.Add(
                legacyGarment);

            SelectedGarment =
                legacyGarment;

            RebuildOrderPages();

            SetLoadedPdfIdentity(
                pdfPath,
                data.OrderName,
                data.FormatVersion);

            AdoptLoadedAttachmentContents(data);

            await ShowTemporaryPdfStatus(
                $"✓ Wczytano kartę z PDF: {Path.GetFileName(pdfPath)}",
                4000);
        }
        catch (Exception exception)
        {
            SetPdfStatus(
                "Nie udało się wczytać danych z PDF: " +
                exception.Message);
        }
    }

    private Product? FindProductForPdf(
        string? productCode,
        string? productName)
    {
        if (!string.IsNullOrWhiteSpace(productCode))
        {
            var byCode =
                allProducts.FirstOrDefault(product =>
                    string.Equals(
                        product.Code?.Trim(),
                        productCode.Trim(),
                        StringComparison.OrdinalIgnoreCase));

            if (byCode != null)
                return byCode;
        }

        if (!string.IsNullOrWhiteSpace(productName))
        {
            var normalizedProductName =
                NormalizeProductIdentity(
                    productName);

            var byName =
                allProducts.FirstOrDefault(product =>
                    string.Equals(
                        NormalizeProductIdentity(
                            product.Name),
                        normalizedProductName,
                        StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(
                        NormalizeProductIdentity(
                            product.DisplayName),
                        normalizedProductName,
                        StringComparison.OrdinalIgnoreCase));

            if (byName != null)
                return byName;
        }

        return null;
    }

    private static string NormalizeProductIdentity(
        string? value)
    {
        return (value ?? "")
            .Normalize(
                NormalizationForm.FormC)
            .Trim();
    }

    private static void RestoreCardFromPdf(
        ProductionCard card,
        CommaOrderData data)
    {
        card.OrderNumber = data.OrderNumber ?? "";
        card.OrderName = data.OrderName ?? "";
        card.Customer = data.Customer ?? "";
        card.ReceivedDate = data.ReceivedDate ?? "";
        card.DueDate = data.DueDate ?? "";
        card.ProductionType = data.ProductionType ?? "";
        card.ProductCode = data.ProductCode;
        card.ProductName = data.ProductName;
        card.Colour = data.Colour ?? "";
        card.Size = data.Size ?? "";
        card.Quantity = data.Quantity ?? "";
        card.Notes = data.Notes ?? "";

        card.ShowFront = data.ShowFront;
        card.ShowBack = data.ShowBack;
        card.ShowLeft = data.ShowLeft;
        card.ShowRight = data.ShowRight;

        card.Attachments.Clear();

        foreach (var sourceAttachment in data.Attachments)
        {
            card.Attachments.Add(
                new OrderAttachmentMetadata
                {
                    Id = sourceAttachment.Id,
                    Name = sourceAttachment.Name ?? "",
                    MimeType = sourceAttachment.MimeType ?? "",
                    Extension = sourceAttachment.Extension ?? "",
                    Order = sourceAttachment.Order,
                    Length = sourceAttachment.Length,
                    Sha256 = sourceAttachment.Sha256 ?? "",
                    BlobEntry = sourceAttachment.BlobEntry ?? "",
                    PdfPageCount = sourceAttachment.PdfPageCount
                });
        }

        for (var index = 0;
             index < card.ProductionEntries.Count;
             index++)
        {
            var targetEntry =
                card.ProductionEntries[index];

            var sourceEntry =
                index < data.ProductionEntries.Count
                    ? data.ProductionEntries[index]
                    : null;

            targetEntry.LogoName =
                sourceEntry?.LogoName ?? "";

            targetEntry.Dimension =
                sourceEntry?.Dimension ?? "";

            targetEntry.Colours.Clear();

            if (sourceEntry == null)
                continue;

            foreach (var sourceColour in sourceEntry.Colours)
            {
                targetEntry.Colours.Add(
                    new ProductionColourEntry(
                        sourceColour.Number)
                    {
                        Value =
                            sourceColour.Value ?? ""
                    });
            }
        }
    }

    private static void RestoreLegacyCardFromPdf(
        ProductionCard card,
        CommaOrderData data)
    {
        RestoreCardFromPdf(
            card,
            data);

        if (!string.IsNullOrWhiteSpace(
                data.ProductCode))
        {
            card.ProductCode =
                data.ProductCode;
        }

        card.ProductName =
            data.ProductName ?? "";
    }

    private static OrderGarmentItem CreateLegacyGarment(
        Product product,
        CommaOrderData data)
    {
        var garment =
            new OrderGarmentItem();

        garment.LoadProduct(
            product);

        if (!string.IsNullOrWhiteSpace(
                data.ProductName))
        {
            garment.Name =
                data.ProductName;
        }

        garment.Colour =
            data.Colour ?? "";

        garment.ShowFront =
            data.ShowFront;

        garment.ShowBack =
            data.ShowBack;

        garment.ShowRight =
            data.ShowRight;

        garment.ShowLeft =
            data.ShowLeft;

        garment.RefreshDrawingSelection();

        return garment;
    }

    private enum PdfSaveChoice
    {
        Cancel = 0,
        Overwrite = 1,
        CreateNew = 2
    }

    [RelayCommand]
    private async Task GeneratePdf()
    {
        ClearPdfStatus();

        if (ProductionCard == null)
        {
            SetPdfStatus(
                "Najpierw wybierz produkt.");

            return;
        }

        if (Garments.Count == 0)
        {
            SetPdfStatus(
                "Dodaj co najmniej jeden artykuł do zlecenia.");

            return;
        }

        if (!Garments.Any(garment =>
                garment.SelectedDrawingCount > 0))
        {
            SetPdfStatus(
                "Wybierz co najmniej jeden rzut odzieży.");

            return;
        }

        RebuildOrderPages();

        if (OrderPages.Count == 0)
        {
            SetPdfStatus(
                "Nie udało się utworzyć planu stron PDF.");

            return;
        }

        if (TryGetNonFittingViewDescription(
                out var garmentName,
                out var viewName))
        {
            var window =
                GetMainWindow();

            if (window != null)
            {
                await ShowDescriptionTooLongDialog(
                    window,
                    garmentName,
                    viewName);
            }

            return;
        }

        var defaultOutputDirectory =
            GetEffectivePdfOutputPath();

        if (!Directory.Exists(defaultOutputDirectory))
        {
            SetPdfStatus(
                "Folder zapisu PDF nie istnieje.");

            return;
        }

        var isSameDocument =
            IsSameDocument(
                loadedPdfPath,
                ProductionCard.OrderName,
                loadedOrderName);

        var savePlan =
            CreatePdfSavePlan(
                defaultOutputDirectory,
                ProductionCard.OrderName,
                loadedPdfPath,
                isSameDocument,
                pdfOutputFolderSelectedSincePdfLoad,
                loadedPdfFormatVersion);

        var outputDirectory =
            savePlan.OutputDirectory;

        var pdfFileName =
            savePlan.ExistingPdfFileName;

        var outputFile =
            savePlan.OverwriteOutputFile;

        if (savePlan.HasConflict)
        {
            var window =
                GetMainWindow();

            if (window == null)
                return;

            var saveChoice =
                await ShowPdfSaveChoiceDialog(
                    window,
                    savePlan.ExistingPdfFileName,
                    savePlan.SuggestedPdfFileName);

            if (saveChoice ==
                PdfSaveChoice.Cancel)
            {
                SetPdfStatus(
                    "Zapisywanie PDF zostało anulowane.");

                return;
            }

            if (saveChoice ==
                PdfSaveChoice.Overwrite)
            {
                outputFile =
                    savePlan.OverwriteOutputFile;

                pdfFileName =
                    Path.GetFileName(
                        outputFile);
            }
            else
            {
                pdfFileName =
                    savePlan.SuggestedPdfFileName;

                outputFile =
                    savePlan.CreateNewOutputFile;
            }
        }

        var temporaryPdfFile =
            Path.Combine(
                outputDirectory,
                $".comma-order-{Guid.NewGuid():N}.pdf");

        var temporaryEmbeddedPdfFile =
            Path.Combine(
                outputDirectory,
                $".comma-order-final-{Guid.NewGuid():N}.pdf");

        var temporaryComposedPdfFile =
            Path.Combine(
                outputDirectory,
                $".comma-order-with-attachments-{Guid.NewGuid():N}.pdf");

        var errorFile =
            Path.Combine(
                outputDirectory,
                "Test-error.txt");

        try
        {
            if (File.Exists(temporaryPdfFile))
                File.Delete(temporaryPdfFile);

            if (File.Exists(temporaryEmbeddedPdfFile))
                File.Delete(temporaryEmbeddedPdfFile);

            if (File.Exists(temporaryComposedPdfFile))
                File.Delete(temporaryComposedPdfFile);

            if (File.Exists(errorFile))
                File.Delete(errorFile);

            var pages =
                OrderPages.ToList();

            OrderPdfGenerator.Generate(
                temporaryPdfFile,
                ProductionCard,
                pages);

            if (!File.Exists(temporaryPdfFile))
            {
                throw new IOException(
                    "Tymczasowy plik PDF nie został utworzony.");
            }

            var temporaryFileInfo =
                new FileInfo(
                    temporaryPdfFile);

            if (temporaryFileInfo.Length == 0)
            {
                throw new IOException(
                    "Tymczasowy plik PDF jest pusty.");
            }

            OrderAttachmentPdfComposer.Compose(
                temporaryPdfFile,
                temporaryComposedPdfFile,
                ProductionCard.Attachments,
                AttachmentManager.ContentStore);

            OrderPdfV4DataEmbedder.AddEmbeddedData(
                temporaryComposedPdfFile,
                temporaryEmbeddedPdfFile,
                ProductionCard,
                Garments.ToList(),
                AttachmentManager.ContentStore);

            if (!File.Exists(temporaryEmbeddedPdfFile))
            {
                throw new IOException(
                    "Końcowy plik PDF nie został utworzony.");
            }

            var temporaryEmbeddedFileInfo =
                new FileInfo(
                    temporaryEmbeddedPdfFile);

            if (temporaryEmbeddedFileInfo.Length == 0)
            {
                throw new IOException(
                    "Końcowy plik PDF jest pusty.");
            }

            File.Move(
                temporaryEmbeddedPdfFile,
                outputFile,
                overwrite: true);

            if (!File.Exists(outputFile))
            {
                throw new IOException(
                    "Plik PDF nie został zapisany.");
            }

            var fileInfo =
                new FileInfo(
                    outputFile);

            if (fileInfo.Length == 0)
            {
                throw new IOException(
                    "Zapisany plik PDF jest pusty.");
            }

            SetLoadedPdfIdentity(
                outputFile,
                ProductionCard.OrderName,
                OrderPdfV4DataEmbedder.FormatVersion);

            TryDeleteFile(
                temporaryPdfFile);

            TryDeleteFile(
                temporaryComposedPdfFile);

            TryDeleteFile(
                temporaryEmbeddedPdfFile);

            await ShowTemporaryPdfStatus(
                $"✓ Karta PDF została wygenerowana: {pdfFileName}",
                3000);
        }
        catch (Exception exception)
        {
            TryDeleteFile(
                temporaryPdfFile);

            TryDeleteFile(
                temporaryComposedPdfFile);

            TryDeleteFile(
                temporaryEmbeddedPdfFile);

            var errorText =
                $"Data: {DateTime.Now:yyyy-MM-dd HH:mm:ss}" +
                Environment.NewLine +
                $"Typ błędu: {exception.GetType().FullName}" +
                Environment.NewLine +
                $"Komunikat: {exception.Message}" +
                Environment.NewLine +
                Environment.NewLine +
                exception;

            try
            {
                File.WriteAllText(
                    errorFile,
                    errorText);
            }
            catch
            {
            }

            SetPdfStatus(
                "Nie udało się wygenerować PDF. " +
                "Szczegóły zapisano w folderze zapisu PDF " +
                "w pliku Test-error.txt.");

            Console.Error.WriteLine(
                errorText);
        }
    }

    private bool TryGetNonFittingViewDescription(
        out string garmentName,
        out string viewName)
    {
        var pages =
            OrderPageLayoutEngine.BuildPages(
                Garments);

        foreach (var page in pages)
        {
            foreach (var placement in page.Placements)
            {
                var garment = placement.Garment;

                foreach (var view in placement.Views)
                {
                    var drawing = view.Drawing;
                    var geometry = view.Geometry;
                    var text = GarmentViewDescriptionLayout.GetDescription(
                        garment,
                        drawing);

                    if (GarmentViewDescriptionLayout.FitsEditorTargets(
                            text,
                            geometry))
                    {
                        continue;
                    }

                    garmentName = garment.Name;
                    viewName = DrawingLayoutEngine.GetViewName(drawing);

                    return true;
                }
            }
        }

        garmentName = "";
        viewName = "";

        return false;
    }


    private static async Task ShowDescriptionTooLongDialog(
        Window owner,
        string garmentName,
        string viewName)
    {
        var dialog = new Window
        {
            Width = 500,
            Height = 200,
            CanResize = false,
            WindowStartupLocation =
                WindowStartupLocation.CenterOwner,
            Title = "Opisy rzutów"
        };
        var message = new TextBlock
        {
            Text =
                $"Skróć opis {viewName} dla pozycji „{garmentName}”, " +
                "aby mieścił się w dostępnej przestrzeni pod rysunkiem.",
            TextWrapping = TextWrapping.Wrap,
            FontSize = 13
        };
        var closeButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var content = new Grid
        {
            Margin = new Thickness(24),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 18
        };

        Grid.SetRow(message, 0);
        Grid.SetRow(closeButton, 1);
        content.Children.Add(message);
        content.Children.Add(closeButton);
        dialog.Content = content;
        closeButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(owner);
    }


    private static async Task<PdfSaveChoice> ShowPdfSaveChoiceDialog(
        Window owner,
        string existingPdfFileName,
        string suggestedPdfFileName)
    {
        var dialog =
            new Window
            {
                Width = 620,
                Height = 310,
                MinWidth = 620,
                MinHeight = 310,
                MaxWidth = 620,
                MaxHeight = 310,
                CanResize = false,
                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,
                Title =
                    "Karta PDF już istnieje"
            };

        var mainGrid =
            new Grid
            {
                RowDefinitions =
                    new RowDefinitions(
                        "Auto,Auto,*,Auto"),
                RowSpacing = 14,
                Margin =
                    new Thickness(24)
            };

        var title =
            new TextBlock
            {
                Text =
                    "KARTA O TEJ NAZWIE JUŻ ISTNIEJE",
                FontSize = 18,
                FontWeight =
                    FontWeight.Bold
            };

        Grid.SetRow(
            title,
            0);

        mainGrid.Children.Add(
            title);

        var description =
            new TextBlock
            {
                Text =
                    "Wybierz, czy chcesz nadpisać aktualnie edytowaną kartę, " +
                    "czy utworzyć nowy plik z kolejnym wolnym numerem.",
                FontSize = 12,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse(
                            "#6F737A")),
                TextWrapping =
                    TextWrapping.Wrap
            };

        Grid.SetRow(
            description,
            1);

        mainGrid.Children.Add(
            description);

        var existingLabel =
            new TextBlock
            {
                Text =
                    "AKTUALNA KARTA",
                FontSize = 10,
                FontWeight =
                    FontWeight.Bold,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var existingValue =
            new TextBlock
            {
                Text =
                    existingPdfFileName,
                FontSize = 12,
                FontWeight =
                    FontWeight.SemiBold,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var newLabel =
            new TextBlock
            {
                Text =
                    "NOWA KARTA",
                FontSize = 10,
                FontWeight =
                    FontWeight.Bold,
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var newValue =
            new TextBlock
            {
                Text =
                    suggestedPdfFileName,
                FontSize = 12,
                FontWeight =
                    FontWeight.Bold,
                Foreground =
                    new SolidColorBrush(
                        Color.Parse(
                            "#0071BC")),
                VerticalAlignment =
                    VerticalAlignment.Center
            };

        var namesGrid =
            new Grid
            {
                RowDefinitions =
                    new RowDefinitions(
                        "Auto,Auto"),
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "150,*"),
                RowSpacing = 9
            };

        Grid.SetRow(
            existingLabel,
            0);

        Grid.SetColumn(
            existingLabel,
            0);

        Grid.SetRow(
            existingValue,
            0);

        Grid.SetColumn(
            existingValue,
            1);

        Grid.SetRow(
            newLabel,
            1);

        Grid.SetColumn(
            newLabel,
            0);

        Grid.SetRow(
            newValue,
            1);

        Grid.SetColumn(
            newValue,
            1);

        namesGrid.Children.Add(
            existingLabel);

        namesGrid.Children.Add(
            existingValue);

        namesGrid.Children.Add(
            newLabel);

        namesGrid.Children.Add(
            newValue);

        var namesPanel =
            new Border
            {
                Background =
                    new SolidColorBrush(
                        Color.Parse(
                            "#F7F8FA")),
                BorderBrush =
                    new SolidColorBrush(
                        Color.Parse(
                            "#D8DADF")),
                BorderThickness =
                    new Thickness(1),
                CornerRadius =
                    new CornerRadius(7),
                Padding =
                    new Thickness(14),
                Child =
                    namesGrid
            };

        Grid.SetRow(
            namesPanel,
            2);

        mainGrid.Children.Add(
            namesPanel);

        var buttonBackground =
            new SolidColorBrush(
                Color.Parse(
                    "#F7F8FA"));

        var buttonBorder =
            new SolidColorBrush(
                Color.Parse(
                    "#C9CDD3"));

        var buttonForeground =
            new SolidColorBrush(
                Color.Parse(
                    "#3F4348"));

        var buttons =
            new Grid
            {
                ColumnDefinitions =
                    new ColumnDefinitions(
                        "*,Auto,Auto,Auto"),
                ColumnSpacing = 10
            };

        var cancelButton =
            new Button
            {
                Width = 105,
                Height = 42,
                Content =
                    "ANULUJ",
                Background =
                    buttonBackground,
                BorderBrush =
                    buttonBorder,
                BorderThickness =
                    new Thickness(1),
                CornerRadius =
                    new CornerRadius(7),
                Foreground =
                    buttonForeground,
                FontSize = 11,
                FontWeight =
                    FontWeight.SemiBold,
                HorizontalContentAlignment =
                    HorizontalAlignment.Center,
                VerticalContentAlignment =
                    VerticalAlignment.Center
            };

        Grid.SetColumn(
            cancelButton,
            1);

        var overwriteButton =
            new Button
            {
                Width = 175,
                Height = 42,
                Content =
                    "NADPISZ AKTUALNĄ",
                Background =
                    buttonBackground,
                BorderBrush =
                    buttonBorder,
                BorderThickness =
                    new Thickness(1),
                CornerRadius =
                    new CornerRadius(7),
                Foreground =
                    buttonForeground,
                FontSize = 11,
                FontWeight =
                    FontWeight.SemiBold,
                HorizontalContentAlignment =
                    HorizontalAlignment.Center,
                VerticalContentAlignment =
                    VerticalAlignment.Center
            };

        Grid.SetColumn(
            overwriteButton,
            2);

        var createNewButton =
            new Button
            {
                Width = 175,
                Height = 42,
                Content =
                    "UTWÓRZ NOWĄ",
                CornerRadius =
                    new CornerRadius(7),
                FontSize = 11,
                FontWeight =
                    FontWeight.Bold,
                HorizontalContentAlignment =
                    HorizontalAlignment.Center,
                VerticalContentAlignment =
                    VerticalAlignment.Center
            };

        Grid.SetColumn(
            createNewButton,
            3);

        cancelButton.Click +=
            (_, _) =>
            {
                dialog.Close(
                    PdfSaveChoice.Cancel);
            };

        overwriteButton.Click +=
            (_, _) =>
            {
                dialog.Close(
                    PdfSaveChoice.Overwrite);
            };

        createNewButton.Click +=
            (_, _) =>
            {
                dialog.Close(
                    PdfSaveChoice.CreateNew);
            };

        buttons.Children.Add(
            cancelButton);

        buttons.Children.Add(
            overwriteButton);

        buttons.Children.Add(
            createNewButton);

        Grid.SetRow(
            buttons,
            3);

        mainGrid.Children.Add(
            buttons);

        dialog.Content =
            mainGrid;

        return await dialog.ShowDialog<PdfSaveChoice>(
            owner);
    }

    private static void TryDeleteFile(
        string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(
                    filePath);
            }
        }
        catch
        {
        }
    }

    private void SetLoadedPdfIdentity(
        string pdfPath,
        string? orderName,
        int formatVersion)
    {
        loadedPdfPath =
            pdfPath;

        loadedOrderName =
            orderName ?? "";

        loadedPdfFormatVersion =
            formatVersion;

        pdfOutputFolderSelectedSincePdfLoad =
            false;
    }

    private sealed record PdfSavePlan(
        string OutputDirectory,
        bool HasConflict,
        string ExistingPdfFileName,
        string SuggestedPdfFileName,
        string OverwriteOutputFile,
        string CreateNewOutputFile);

    private static PdfSavePlan CreatePdfSavePlan(
        string selectedOutputDirectory,
        string? orderName,
        string? loadedPdfPath,
        bool isSameDocument,
        bool outputFolderSelectedSincePdfLoad,
        int? loadedFormatVersion)
    {
        if (isSameDocument &&
            !outputFolderSelectedSincePdfLoad &&
            loadedFormatVersion != 3)
        {
            var loadedDirectory =
                Path.GetDirectoryName(
                    loadedPdfPath!);

            var outputDirectory =
                !string.IsNullOrWhiteSpace(
                    loadedDirectory) &&
                Directory.Exists(
                    loadedDirectory)
                    ? loadedDirectory
                    : selectedOutputDirectory;

            var existingPdfFileName =
                Path.GetFileName(
                    loadedPdfPath!);

            var loadedSuggestedPdfFileName =
                CreateNextPdfFileNameForLoadedFile(
                    outputDirectory,
                    orderName,
                    existingPdfFileName);

            return new PdfSavePlan(
                outputDirectory,
                HasConflict: true,
                existingPdfFileName,
                loadedSuggestedPdfFileName,
                loadedPdfPath!,
                Path.Combine(
                    outputDirectory,
                    loadedSuggestedPdfFileName));
        }

        var basePdfFileName =
            CreatePdfFileName(
                orderName);

        var baseOutputFile =
            Path.Combine(
                selectedOutputDirectory,
                basePdfFileName);

        var hasConflict =
            File.Exists(
                baseOutputFile);

        var suggestedPdfFileName =
            hasConflict
                ? CreateUniquePdfFileName(
                    selectedOutputDirectory,
                    orderName)
                : basePdfFileName;

        return new PdfSavePlan(
            selectedOutputDirectory,
            hasConflict,
            basePdfFileName,
            suggestedPdfFileName,
            baseOutputFile,
            Path.Combine(
                selectedOutputDirectory,
                suggestedPdfFileName));
    }

    private static bool IsSameDocument(
        string? loadedPdfPath,
        string? currentOrderName,
        string? loadedOrderName)
    {
        return
            !string.IsNullOrWhiteSpace(
                loadedPdfPath) &&
            File.Exists(
                loadedPdfPath) &&
            string.Equals(
                currentOrderName?.Trim(),
                loadedOrderName?.Trim(),
                StringComparison.OrdinalIgnoreCase);
    }

    private static string CreateNextPdfFileNameForLoadedFile(
        string outputDirectory,
        string? orderName,
        string loadedPdfFileName)
    {
        var baseFileName =
            Path.GetFileNameWithoutExtension(
                CreatePdfFileName(
                    orderName));

        var loadedFileNameWithoutExtension =
            Path.GetFileNameWithoutExtension(
                loadedPdfFileName);

        var number =
            1;

        var versionPrefix =
            $"{baseFileName}_";

        if (loadedFileNameWithoutExtension.StartsWith(
                versionPrefix,
                StringComparison.OrdinalIgnoreCase))
        {
            var versionText =
                loadedFileNameWithoutExtension[
                    versionPrefix.Length..];

            if (int.TryParse(
                    versionText,
                    out var loadedVersion) &&
                loadedVersion >= 1)
            {
                number =
                    loadedVersion + 1;
            }
        }

        while (true)
        {
            var candidate =
                $"{baseFileName}_{number}.pdf";

            var candidatePath =
                Path.Combine(
                    outputDirectory,
                    candidate);

            if (!File.Exists(candidatePath))
                return candidate;

            number++;
        }
    }

    private static string CreateUniquePdfFileName(
        string outputDirectory,
        string? orderName)
    {
        var baseFileName =
            string.IsNullOrWhiteSpace(orderName)
                ? DefaultPdfFileName
                : orderName.Trim();

        foreach (var invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            baseFileName =
                baseFileName.Replace(
                    invalidCharacter,
                    '_');
        }

        baseFileName =
            baseFileName
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace("*", "_")
                .Replace("?", "_")
                .Replace("\"", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("|", "_");

        while (baseFileName.Contains("__"))
        {
            baseFileName =
                baseFileName.Replace(
                    "__",
                    "_");
        }

        baseFileName =
            baseFileName
                .Trim()
                .Trim('.');

        if (string.IsNullOrWhiteSpace(baseFileName))
        {
            baseFileName =
                DefaultPdfFileName;
        }

        var firstCandidate =
            $"{baseFileName}.pdf";

        var firstPath =
            Path.Combine(
                outputDirectory,
                firstCandidate);

        if (!File.Exists(firstPath))
            return firstCandidate;

        var number =
            1;

        while (true)
        {
            var candidate =
                $"{baseFileName}_{number}.pdf";

            var candidatePath =
                Path.Combine(
                    outputDirectory,
                    candidate);

            if (!File.Exists(candidatePath))
                return candidate;

            number++;
        }
    }

    private static string CreatePdfFileName(
        string? orderName)
    {
        var baseFileName =
            string.IsNullOrWhiteSpace(orderName)
                ? DefaultPdfFileName
                : orderName.Trim();

        foreach (var invalidCharacter
                 in Path.GetInvalidFileNameChars())
        {
            baseFileName =
                baseFileName.Replace(
                    invalidCharacter,
                    '_');
        }

        baseFileName =
            baseFileName
                .Replace("/", "_")
                .Replace("\\", "_")
                .Replace(":", "_")
                .Replace("*", "_")
                .Replace("?", "_")
                .Replace("\"", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("|", "_");

        while (baseFileName.Contains("__"))
        {
            baseFileName =
                baseFileName.Replace(
                    "__",
                    "_");
        }

        baseFileName =
            baseFileName
                .Trim()
                .Trim('.');

        if (string.IsNullOrWhiteSpace(baseFileName))
        {
            baseFileName =
                DefaultPdfFileName;
        }

        return $"{baseFileName}.pdf";
    }

    private void SetPdfStatus(
        string message)
    {
        pdfStatusVersion++;

        PdfStatus =
            message;
    }

    private void ClearPdfStatus()
    {
        pdfStatusVersion++;

        PdfStatus =
            "";
    }

    private async Task ShowTemporaryPdfStatus(
        string message,
        int durationMilliseconds)
    {
        var currentVersion =
            ++pdfStatusVersion;

        PdfStatus =
            message;

        await Task.Delay(
            durationMilliseconds);

        if (currentVersion != pdfStatusVersion)
            return;

        PdfStatus =
            "";
    }
}
