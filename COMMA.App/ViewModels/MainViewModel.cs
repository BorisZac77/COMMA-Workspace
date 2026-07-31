using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media.Imaging;
using Avalonia.Platform.Storage;
using COMMA.App.Models;
using COMMA.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace COMMA.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly LibraryScanner libraryScanner = new();

    [ObservableProperty]
    private string libraryPath = "No library selected";

    public ObservableCollection<Product> Products { get; } = new();

    public ObservableCollection<DrawingFile> Drawings { get; } = new();

    [ObservableProperty]
    private Product? selectedProduct;

    [ObservableProperty]
    private DrawingFile? selectedDrawing;

    [ObservableProperty]
    private Bitmap? selectedImage;

    partial void OnSelectedProductChanged(Product? value)
    {
        Drawings.Clear();
        SelectedDrawing = null;
        SelectedImage = null;

        if (value == null)
            return;

        foreach (var drawing in DrawingScanner.Scan(value.Folder))
            Drawings.Add(drawing);
    }

    partial void OnSelectedDrawingChanged(DrawingFile? value)
    {
        SelectedImage = null;

        if (value == null)
            return;

        SelectedImage = new Bitmap(value.FullPath);
    }

    [RelayCommand]
    private async Task OpenLibrary()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;

        if (window == null)
            return;

        var folders = await window.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                Title = "Select COMMA Library",
                AllowMultiple = false
            });

        if (folders.Count == 0)
            return;

        var path = folders[0].TryGetLocalPath();

        if (string.IsNullOrWhiteSpace(path))
            return;

        LibraryPath = path;

        Products.Clear();
        Drawings.Clear();

        SelectedProduct = null;
        SelectedDrawing = null;
        SelectedImage = null;

        foreach (var product in libraryScanner.Scan(path))
            Products.Add(product);
    }

    [RelayCommand]
    private void GeneratePdf()
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

        var outputFile = Path.Combine(desktop, "Test.pdf");

        PdfGenerator.Generate(outputFile);
    }
}