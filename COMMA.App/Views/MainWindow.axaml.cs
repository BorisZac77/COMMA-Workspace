using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using COMMA.App.Services;
using COMMA.App.ViewModels;

namespace COMMA.App.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        Title =
            $"COMMA Workspace — v{GetApplicationVersion()}";

        NormalizeDrawingsButton.Click +=
            OnNormalizeDrawingsButtonClick;

        ProductionEntriesButton.Click +=
            OnProductionEntriesButtonClick;

        ClearOrderDataButton.Click +=
            OnClearOrderDataButtonClick;
    }


    private static string GetApplicationVersion()
    {
        var assembly =
            Assembly.GetExecutingAssembly();

        var informationalVersion =
            assembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;

        if (!string.IsNullOrWhiteSpace(
                informationalVersion))
        {
            var plusIndex =
                informationalVersion.IndexOf(
                    '+');

            if (plusIndex >= 0)
            {
                informationalVersion =
                    informationalVersion[..plusIndex];
            }

            return informationalVersion;
        }

        var version =
            assembly
                .GetName()
                .Version;

        if (version == null)
            return "0.0.0";

        return
            $"{version.Major}." +
            $"{version.Minor}." +
            $"{version.Build}";
    }


    private void OnClearOrderDataButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        viewModel.ClearCurrentOrder();
    }


    private async void OnProductionEntriesButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        if (viewModel.ProductionCard is not { } productionCard)
            return;

        var dialog =
            new ProductionEntriesWindow(
                productionCard);

        await dialog.ShowDialog<bool>(
            this);
    }


    private void OnNormalizeDrawingsButtonClick(
        object? sender,
        RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
            return;

        OpenDrawingNormalizationDialog(
            viewModel);
    }


    private async void OpenDrawingNormalizationDialog(
        MainViewModel viewModel)
    {
        var libraryPath =
            viewModel.LibraryPath;

        var productCount =
            viewModel.Products.Count;

        var dialog =
            new Window
            {
                Width = 760,
                Height = 640,
                MinWidth = 760,
                MinHeight = 640,
                MaxWidth = 760,
                MaxHeight = 640,
                CanResize = false,

                WindowStartupLocation =
                    WindowStartupLocation.CenterOwner,

                Title =
                    "Normalizacja rysunków technicznych"
            };


        var titleText =
            new TextBlock
            {
                Text =
                    "NORMALIZACJA RYSUNKÓW TECHNICZNYCH",

                FontSize = 22,

                FontWeight =
                    Avalonia.Media.FontWeight.Bold
            };


        var libraryText =
            new TextBlock
            {
                Text =
                    $"Biblioteka:\n{libraryPath}\n\n" +
                    $"Produkty w bibliotece: {productCount}",

                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            };


        var infoText =
            new TextBlock
            {
                Text =
                    "NORMALIZUJ NOWE\n" +
                    "Przetwarza tylko rysunki, których nie ma jeszcze " +
                    "w folderze _normalized.\n\n" +

                    "NORMALIZUJ WSZYSTKIE\n" +
                    "Ponownie przetwarza całą bibliotekę i nadpisuje " +
                    "wcześniej znormalizowane rysunki.\n\n" +

                    "W obu trybach program:\n" +
                    "• zawsze używa ORYGINALNYCH rysunków\n" +
                    "• FRONT ustala wysokość referencyjną produktu\n" +
                    "• BACK / RIGHT / LEFT dostają tę samą wysokość\n" +
                    "• zachowuje oryginalne proporcje\n" +
                    "• nie nadpisuje oryginalnych plików\n" +
                    "• zapisuje wyniki w folderze _normalized",

                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            };


        var statusText =
            new TextBlock
            {
                Text =
                    "Wybierz sposób normalizacji.",

                TextWrapping =
                    Avalonia.Media.TextWrapping.Wrap
            };


        var buttonBackground =
            new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse(
                    "#F7F8FA"));


        var buttonBorder =
            new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse(
                    "#C9CDD3"));


        var buttonForeground =
            new Avalonia.Media.SolidColorBrush(
                Avalonia.Media.Color.Parse(
                    "#3F4348"));


        var normalizeNewButton =
            new Button
            {
                Content =
                    "NORMALIZUJ NOWE",

                Width = 190,
                Height = 52,

                Padding =
                    new Avalonia.Thickness(
                        14,
                        0),

                Background =
                    buttonBackground,

                BorderBrush =
                    buttonBorder,

                BorderThickness =
                    new Avalonia.Thickness(1),

                CornerRadius =
                    new Avalonia.CornerRadius(7),

                FontSize = 12,

                FontWeight =
                    Avalonia.Media.FontWeight.SemiBold,

                Foreground =
                    buttonForeground,

                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center,

                VerticalContentAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };


        var normalizeAllButton =
            new Button
            {
                Content =
                    "NORMALIZUJ WSZYSTKIE",

                Width = 210,
                Height = 52,

                Padding =
                    new Avalonia.Thickness(
                        14,
                        0),

                Background =
                    buttonBackground,

                BorderBrush =
                    buttonBorder,

                BorderThickness =
                    new Avalonia.Thickness(1),

                CornerRadius =
                    new Avalonia.CornerRadius(7),

                FontSize = 12,

                FontWeight =
                    Avalonia.Media.FontWeight.SemiBold,

                Foreground =
                    buttonForeground,

                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center,

                VerticalContentAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };


        var cancelButton =
            new Button
            {
                Content =
                    "ANULUJ",

                Width = 130,
                Height = 52,

                Padding =
                    new Avalonia.Thickness(
                        14,
                        0),

                Background =
                    buttonBackground,

                BorderBrush =
                    buttonBorder,

                BorderThickness =
                    new Avalonia.Thickness(1),

                CornerRadius =
                    new Avalonia.CornerRadius(7),

                FontSize = 12,

                FontWeight =
                    Avalonia.Media.FontWeight.SemiBold,

                Foreground =
                    buttonForeground,

                HorizontalContentAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center,

                VerticalContentAlignment =
                    Avalonia.Layout.VerticalAlignment.Center
            };


        async Task RunNormalization(
            bool onlyNew)
        {
            if (string.IsNullOrWhiteSpace(
                    viewModel.LibraryPath) ||
                !Directory.Exists(
                    viewModel.LibraryPath))
            {
                statusText.Text =
                    "Nie znaleziono biblioteki.";

                return;
            }


            normalizeNewButton.IsEnabled =
                false;

            normalizeAllButton.IsEnabled =
                false;

            cancelButton.IsEnabled =
                false;


            statusText.Text =
                onlyNew
                    ? "Normalizacja nowych rysunków w toku..."
                    : "Normalizacja całej biblioteki w toku...";


            var productFolders =
                Directory
                    .GetDirectories(
                        viewModel.LibraryPath)
                    .ToList();


            var totalProducts =
                productFolders.Count;

            var processedProducts =
                0;

            var totalDrawings =
                0;

            var normalizedCount =
                0;

            var errorCount =
                0;


            await Task.Run(
                () =>
                {
                    foreach (var productFolder
                             in productFolders)
                    {
                        var drawingsFolder =
                            Path.Combine(
                                productFolder,
                                "Drawings");


                        string sourceFolder;
                        string outputFolder;


                        if (Directory.Exists(
                                drawingsFolder))
                        {
                            sourceFolder =
                                drawingsFolder;

                            outputFolder =
                                Path.Combine(
                                    drawingsFolder,
                                    "_normalized");
                        }
                        else
                        {
                            sourceFolder =
                                productFolder;

                            outputFolder =
                                Path.Combine(
                                    productFolder,
                                    "_normalized");
                        }


                        var drawings =
                            DrawingScanner
                                .Scan(
                                    sourceFolder)
                                .Where(drawing =>
                                    !string.IsNullOrWhiteSpace(
                                        drawing.FullPath) &&
                                    File.Exists(
                                        drawing.FullPath))
                                .ToList();


                        totalDrawings +=
                            drawings.Count;


                        if (drawings.Count == 0)
                        {
                            processedProducts++;

                            continue;
                        }


                        (
                            int NormalizedCount,
                            int ErrorCount
                        ) result;


                        if (onlyNew)
                        {
                            result =
                                DrawingNormalizer
                                    .NormalizeNewProduct(
                                        drawings,
                                        outputFolder);
                        }
                        else
                        {
                            result =
                                DrawingNormalizer
                                    .NormalizeProduct(
                                        drawings,
                                        outputFolder);
                        }


                        normalizedCount +=
                            result.NormalizedCount;

                        errorCount +=
                            result.ErrorCount;

                        processedProducts++;
                    }
                });


            statusText.Text =
                $"Gotowe.\n\n" +
                $"Tryb: " +
                $"{(onlyNew ? "TYLKO NOWE" : "WSZYSTKIE")}\n" +
                $"Produkty w bibliotece: {totalProducts}\n" +
                $"Sprawdzono produktów: {processedProducts}\n" +
                $"Rysunki w bibliotece: {totalDrawings}\n" +
                $"Znormalizowano: {normalizedCount}\n" +
                $"Błędy: {errorCount}\n\n" +
                (
                    onlyNew
                        ? "Istniejące rysunki w _normalized zostały pominięte."
                        : "Cała biblioteka została ponownie znormalizowana."
                );


            normalizeNewButton.IsEnabled =
                true;

            normalizeAllButton.IsEnabled =
                true;

            cancelButton.IsEnabled =
                true;
        }


        normalizeNewButton.Click +=
            async (_, _) =>
            {
                await RunNormalization(
                    onlyNew: true);
            };


        normalizeAllButton.Click +=
            async (_, _) =>
            {
                await RunNormalization(
                    onlyNew: false);
            };


        cancelButton.Click +=
            (_, _) =>
            {
                dialog.Close();
            };


        var buttons =
            new StackPanel
            {
                Orientation =
                    Avalonia.Layout.Orientation.Horizontal,

                Spacing = 10,

                HorizontalAlignment =
                    Avalonia.Layout.HorizontalAlignment.Center
            };


        buttons.Children.Add(
            normalizeNewButton);

        buttons.Children.Add(
            normalizeAllButton);

        buttons.Children.Add(
            cancelButton);


        var scrollContent =
            new StackPanel
            {
                Spacing = 18
            };


        scrollContent.Children.Add(
            infoText);

        scrollContent.Children.Add(
            statusText);


        var scrollViewer =
            new ScrollViewer
            {
                VerticalScrollBarVisibility =
                    Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,

                HorizontalScrollBarVisibility =
                    Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,

                Content =
                    scrollContent
            };


        var content =
            new Grid
            {
                Margin =
                    new Avalonia.Thickness(24),

                RowDefinitions =
                    new RowDefinitions(
                        "Auto,Auto,*,Auto"),

                RowSpacing = 18
            };


        Grid.SetRow(
            titleText,
            0);

        content.Children.Add(
            titleText);


        Grid.SetRow(
            libraryText,
            1);

        content.Children.Add(
            libraryText);


        Grid.SetRow(
            scrollViewer,
            2);

        content.Children.Add(
            scrollViewer);


        Grid.SetRow(
            buttons,
            3);

        content.Children.Add(
            buttons);


        dialog.Content =
            content;


        await dialog.ShowDialog(
            this);
    }
}
