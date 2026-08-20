using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using COMMA.Core.Models;
using COMMA.Core.Services;
using COMMA.DrawingsGenerator.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.DrawingsGenerator.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private const string SettingsFolderName =
        "COMMA Drawings Generator";

    private const string LibraryPathFileName =
        "library-path.txt";

    private static readonly string[] SupportedImageExtensions =
    {
        ".png",
        ".jpg",
        ".jpeg"
    };

    private readonly LibraryScanner libraryScanner = new();

    private readonly ChatGptExportService chatGptExportService = new();

    private readonly TechnicalDrawingsImportService
        technicalDrawingsImportService = new();

    private readonly List<Product> allProducts = new();

    public ObservableCollection<Product> Products { get; } = new();

    [ObservableProperty]
    private string libraryPath = "Nie wybrano biblioteki";

    [ObservableProperty]
    private string status = "Wybierz bibliotekę produktów.";

    [ObservableProperty]
    private string searchText = "";

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private Bitmap? frontPhoto;

    [ObservableProperty]
    private Bitmap? backPhoto;

    [ObservableProperty]
    private Bitmap? rightPhoto;

    [ObservableProperty]
    private Bitmap? frontTechnicalDrawing;

    [ObservableProperty]
    private Bitmap? backTechnicalDrawing;

    [ObservableProperty]
    private Bitmap? rightTechnicalDrawing;

    [ObservableProperty]
    private string frontPhotoStatus = "Brak zdjęcia";

    [ObservableProperty]
    private string backPhotoStatus = "Brak zdjęcia";

    [ObservableProperty]
    private string rightPhotoStatus = "Brak zdjęcia";

    [ObservableProperty]
    private string frontDrawingStatus = "Brak rysunku";

    [ObservableProperty]
    private string backDrawingStatus = "Brak rysunku";

    [ObservableProperty]
    private string rightDrawingStatus = "Brak rysunku";

    [ObservableProperty]
    private int actualDrawingCount;

    [ObservableProperty]
    private string actualDrawingCountText =
        "Brak rysunków technicznych";

    public MainViewModel()
    {
        TryLoadSavedLibrary();
    }

    partial void OnSearchTextChanged(string value)
    {
        ApplyProductFilter();
    }

    partial void OnSelectedProductChanged(Product? value)
    {
        ClearProductImages();

        if (value == null)
        {
            ActualDrawingCount = 0;
            ActualDrawingCountText =
                "Brak rysunków technicznych";

            Status = Products.Count == 0
                ? "Brak produktów do wyświetlenia."
                : "Wybierz produkt.";

            return;
        }

        LoadSelectedProductImages(value);

        Status =
            $"Wybrano produkt: {value.DisplayName}. " +
            $"Zdjęcia: {GetLoadedPhotoCount()}/3. " +
            $"Rysunki techniczne: {ActualDrawingCount}/3.";
    }

    [RelayCommand]
    private async Task OpenLibrary()
    {
        var window = GetMainWindow();

        if (window == null)
        {
            Status =
                "Nie udało się otworzyć okna wyboru biblioteki.";

            return;
        }

        var folders =
            await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Wybierz bibliotekę produktów COMMA",
                    AllowMultiple = false
                });

        if (folders.Count == 0)
            return;

        var path = folders[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
        {
            Status =
                "Nie udało się odczytać ścieżki biblioteki.";

            return;
        }

        if (!Directory.Exists(path))
        {
            Status = "Wybrany folder nie istnieje.";
            return;
        }

        LoadLibrary(path);
        SaveLibraryPath(path);
    }

    [RelayCommand]
    private void RefreshLibrary()
    {
        if (string.IsNullOrWhiteSpace(LibraryPath) ||
            !Directory.Exists(LibraryPath))
        {
            Status =
                "Najpierw wybierz bibliotekę produktów.";

            return;
        }

        var selectedProductFolder =
            SelectedProduct?.Folder;

        var currentSearchText =
            SearchText;

        LoadLibrary(
            LibraryPath,
            selectedProductFolder,
            currentSearchText);
    }

    [RelayCommand]
    private void ExportToChatGpt()
    {
        if (SelectedProduct == null)
        {
            Status =
                "Najpierw wybierz produkt z biblioteki.";

            return;
        }

        if (GetLoadedPhotoCount() < 3)
        {
            Status =
                "Nie można przygotować paczki. " +
                "Produkt musi mieć zdjęcia FRONT, BACK i RIGHT.";

            return;
        }

        try
        {
            var zipFilePath =
                chatGptExportService.Export(SelectedProduct);

            Status =
                "Paczka dla ChatGPT została zapisana na Biurku: " +
                zipFilePath;
        }
        catch (Exception exception)
        {
            Status =
                "Nie udało się przygotować paczki dla ChatGPT: " +
                exception.Message;

            Console.Error.WriteLine(exception);
        }
    }

    [RelayCommand]
    private async Task ImportDrawings()
    {
        if (SelectedProduct == null)
        {
            Status =
                "Najpierw wybierz produkt z biblioteki.";

            return;
        }

        var window = GetMainWindow();

        if (window == null)
        {
            Status =
                "Nie udało się otworzyć okna wyboru pliku.";

            return;
        }

        var selectedProductFolder =
            SelectedProduct.Folder;

        var selectedProductName =
            SelectedProduct.DisplayName;

        var currentSearchText =
            SearchText;

        var sourcePath =
            await SelectImportSource(window);

        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Status =
                "Import rysunków został anulowany.";

            return;
        }

        try
        {
            ClearProductImages();

            technicalDrawingsImportService.Import(
                SelectedProduct,
                sourcePath);

            LoadLibrary(
                LibraryPath,
                selectedProductFolder,
                currentSearchText);

            Status =
                $"Rysunki techniczne produktu " +
                $"„{selectedProductName}” zostały " +
                "poprawnie zaimportowane.";
        }
        catch (Exception exception)
        {
            if (SelectedProduct != null)
                LoadSelectedProductImages(SelectedProduct);

            Status =
                "Nie udało się zaimportować rysunków: " +
                exception.Message;

            Console.Error.WriteLine(exception);
        }
    }

    private static async Task<string?> SelectImportSource(
        Window window)
    {
        var files =
            await window.StorageProvider.OpenFilePickerAsync(
                new FilePickerOpenOptions
                {
                    Title =
                        "Wybierz ZIP z rysunkami lub anuluj, " +
                        "aby wybrać folder",
                    AllowMultiple = false,
                    FileTypeFilter =
                    [
                        new FilePickerFileType("Archiwum ZIP")
                        {
                            Patterns =
                            [
                                "*.zip"
                            ]
                        }
                    ]
                });

        if (files.Count > 0)
            return files[0].TryGetLocalPath();

        var folders =
            await window.StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    Title =
                        "Wybierz folder z plikami " +
                        "front.png, back.png i right.png",
                    AllowMultiple = false
                });

        if (folders.Count == 0)
            return null;

        return folders[0].TryGetLocalPath();
    }

    private void TryLoadSavedLibrary()
    {
        var settingsFile =
            GetLibraryPathFile();

        if (!File.Exists(settingsFile))
            return;

        try
        {
            var savedPath = File
                .ReadAllText(settingsFile)
                .Trim();

            if (string.IsNullOrWhiteSpace(savedPath))
                return;

            if (!Directory.Exists(savedPath))
            {
                LibraryPath =
                    "Zapisana biblioteka nie istnieje";

                Status =
                    "Poprzednia biblioteka została usunięta, " +
                    "przeniesiona albo serwer jest niedostępny.";

                return;
            }

            LoadLibrary(savedPath);
        }
        catch (Exception exception)
        {
            LibraryPath =
                "Nie udało się wczytać biblioteki";

            Status =
                "Błąd automatycznego ładowania biblioteki: " +
                exception.Message;
        }
    }

    private void LoadLibrary(
        string path,
        string? productFolderToSelect = null,
        string? searchTextToRestore = null)
    {
        ClearProductImages();

        allProducts.Clear();
        Products.Clear();

        SelectedProduct = null;
        SearchText = "";
        LibraryPath = path;
        Status = "Wczytywanie biblioteki...";

        try
        {
            foreach (var product in libraryScanner.Scan(path))
                allProducts.Add(product);

            if (allProducts.Count == 0)
            {
                Status =
                    "W wybranym folderze nie znaleziono " +
                    "katalogów produktów.";

                return;
            }

            if (!string.IsNullOrWhiteSpace(searchTextToRestore))
                SearchText = searchTextToRestore;

            ApplyProductFilter();

            if (!string.IsNullOrWhiteSpace(
                    productFolderToSelect))
            {
                var productToSelect =
                    Products.FirstOrDefault(
                        product =>
                            string.Equals(
                                product.Folder,
                                productFolderToSelect,
                                StringComparison
                                    .OrdinalIgnoreCase));

                if (productToSelect != null)
                    SelectedProduct = productToSelect;
            }

            SelectedProduct ??=
                Products.FirstOrDefault();

            Status =
                $"Wczytano produktów: {allProducts.Count}.";
        }
        catch (Exception exception)
        {
            allProducts.Clear();
            Products.Clear();

            SelectedProduct = null;

            Status =
                "Nie udało się wczytać biblioteki: " +
                exception.Message;
        }
    }

    private void LoadSelectedProductImages(
        Product product)
    {
        var photosFolder =
            FindSubfolder(product.Folder, "Photos")
            ?? FindSubfolder(product.Folder, "Product")
            ?? product.Folder;

        var drawingsFolder =
            FindSubfolder(product.Folder, "Drawings");

        var frontPhotoPath = FindViewFile(
            photosFolder,
            "front",
            "przod",
            "przód");

        var backPhotoPath = FindViewFile(
            photosFolder,
            "back",
            "tyl",
            "tył");

        var rightPhotoPath = FindViewFile(
            photosFolder,
            "right",
            "right side",
            "right-side",
            "prawy",
            "prawy bok");

        var frontDrawingPath = FindViewFile(
            drawingsFolder,
            "front-tech",
            "front technical",
            "front drawing",
            "front");

        var backDrawingPath = FindViewFile(
            drawingsFolder,
            "back-tech",
            "back technical",
            "back drawing",
            "back");

        var rightDrawingPath = FindViewFile(
            drawingsFolder,
            "right-tech",
            "right technical",
            "right drawing",
            "right side",
            "right");

        FrontPhoto =
            LoadBitmap(frontPhotoPath);

        BackPhoto =
            LoadBitmap(backPhotoPath);

        RightPhoto =
            LoadBitmap(rightPhotoPath);

        FrontTechnicalDrawing =
            LoadBitmap(frontDrawingPath);

        BackTechnicalDrawing =
            LoadBitmap(backDrawingPath);

        RightTechnicalDrawing =
            LoadBitmap(rightDrawingPath);

        FrontPhotoStatus =
            FrontPhoto != null
                ? "Zdjęcie dostępne"
                : "Brak zdjęcia";

        BackPhotoStatus =
            BackPhoto != null
                ? "Zdjęcie dostępne"
                : "Brak zdjęcia";

        RightPhotoStatus =
            RightPhoto != null
                ? "Zdjęcie dostępne"
                : "Brak zdjęcia";

        FrontDrawingStatus =
            FrontTechnicalDrawing != null
                ? "Rysunek dostępny"
                : "Brak rysunku";

        BackDrawingStatus =
            BackTechnicalDrawing != null
                ? "Rysunek dostępny"
                : "Brak rysunku";

        RightDrawingStatus =
            RightTechnicalDrawing != null
                ? "Rysunek dostępny"
                : "Brak rysunku";

        ActualDrawingCount =
            CountLoadedTechnicalDrawings();

        ActualDrawingCountText =
            ActualDrawingCount switch
            {
                0 => "Brak rysunków technicznych",
                1 => "1 rysunek techniczny",
                2 => "2 rysunki techniczne",
                3 => "Komplet rysunków technicznych",
                _ =>
                    $"{ActualDrawingCount} " +
                    "rysunki techniczne"
            };
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
            Products.Add(product);

        if (Products.Count == 0)
        {
            SelectedProduct = null;

            Status =
                string.IsNullOrWhiteSpace(searchValue)
                    ? "Brak produktów do wyświetlenia."
                    : $"Nie znaleziono produktów dla: " +
                      searchValue;

            return;
        }

        if (previousSelection != null &&
            Products.Contains(previousSelection))
        {
            SelectedProduct = previousSelection;
            return;
        }

        SelectedProduct = Products[0];
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

    private static string? FindSubfolder(
        string parentFolder,
        string expectedName)
    {
        if (!Directory.Exists(parentFolder))
            return null;

        return Directory
            .EnumerateDirectories(
                parentFolder,
                "*",
                SearchOption.TopDirectoryOnly)
            .FirstOrDefault(
                folder =>
                    string.Equals(
                        Path.GetFileName(folder),
                        expectedName,
                        StringComparison.OrdinalIgnoreCase));
    }

    private static string? FindViewFile(
        string? folder,
        params string[] expectedNames)
    {
        if (string.IsNullOrWhiteSpace(folder) ||
            !Directory.Exists(folder))
        {
            return null;
        }

        var imageFiles = Directory
            .EnumerateFiles(
                folder,
                "*",
                SearchOption.TopDirectoryOnly)
            .Where(IsSupportedImage)
            .OrderBy(Path.GetFileName)
            .ToList();

        foreach (var expectedName in expectedNames)
        {
            var exactMatch =
                imageFiles.FirstOrDefault(
                    file =>
                        string.Equals(
                            NormalizeName(
                                Path.GetFileNameWithoutExtension(
                                    file)),
                            NormalizeName(expectedName),
                            StringComparison.OrdinalIgnoreCase));

            if (exactMatch != null)
                return exactMatch;
        }

        foreach (var expectedName in expectedNames)
        {
            var partialMatch =
                imageFiles.FirstOrDefault(
                    file =>
                        NormalizeName(
                                Path.GetFileNameWithoutExtension(
                                    file))
                            .Contains(
                                NormalizeName(expectedName),
                                StringComparison
                                    .OrdinalIgnoreCase));

            if (partialMatch != null)
                return partialMatch;
        }

        return null;
    }

    private static Bitmap? LoadBitmap(
        string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) ||
            !File.Exists(filePath))
        {
            return null;
        }

        try
        {
            return new Bitmap(filePath);
        }
        catch
        {
            return null;
        }
    }

    private int GetLoadedPhotoCount()
    {
        var count = 0;

        if (FrontPhoto != null)
            count++;

        if (BackPhoto != null)
            count++;

        if (RightPhoto != null)
            count++;

        return count;
    }

    private int CountLoadedTechnicalDrawings()
    {
        var count = 0;

        if (FrontTechnicalDrawing != null)
            count++;

        if (BackTechnicalDrawing != null)
            count++;

        if (RightTechnicalDrawing != null)
            count++;

        return count;
    }

    private void ClearProductImages()
    {
        DisposeBitmap(FrontPhoto);
        DisposeBitmap(BackPhoto);
        DisposeBitmap(RightPhoto);

        DisposeBitmap(FrontTechnicalDrawing);
        DisposeBitmap(BackTechnicalDrawing);
        DisposeBitmap(RightTechnicalDrawing);

        FrontPhoto = null;
        BackPhoto = null;
        RightPhoto = null;

        FrontTechnicalDrawing = null;
        BackTechnicalDrawing = null;
        RightTechnicalDrawing = null;

        FrontPhotoStatus = "Brak zdjęcia";
        BackPhotoStatus = "Brak zdjęcia";
        RightPhotoStatus = "Brak zdjęcia";

        FrontDrawingStatus = "Brak rysunku";
        BackDrawingStatus = "Brak rysunku";
        RightDrawingStatus = "Brak rysunku";

        ActualDrawingCount = 0;

        ActualDrawingCountText =
            "Brak rysunków technicznych";
    }

    private static void DisposeBitmap(
        Bitmap? bitmap)
    {
        bitmap?.Dispose();
    }

    private static bool IsSupportedImage(
        string file)
    {
        var extension = Path
            .GetExtension(file)
            .ToLowerInvariant();

        return SupportedImageExtensions.Contains(extension);
    }

    private static string NormalizeName(
        string value)
    {
        return value
            .Trim()
            .ToLowerInvariant()
            .Replace("_", " ")
            .Replace("-", " ")
            .Replace("  ", " ");
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

    private static void SaveLibraryPath(
        string path)
    {
        try
        {
            var settingsFile =
                GetLibraryPathFile();

            var settingsDirectory =
                Path.GetDirectoryName(settingsFile);

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
            // Brak możliwości zapisania ustawienia
            // nie może zamknąć aplikacji.
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
}