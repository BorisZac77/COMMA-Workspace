using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace COMMA.App.Tests;

public sealed class MainWindowWindowsLayoutTests
{
    private const double FullHdWidth = 1920;
    private const double InitialWindowWidth = 1800;
    private const double WindowsWidthMargin = 16;
    private const double MainContentHorizontalMargin = 40;
    private const double MainContentColumnSpacing = 18;

    [Fact]
    public void WindowsSizing_RemainsPlatformSpecificAndDpiAware()
    {
        var source = NormalizeWhitespace(
            File.ReadAllText(
                GetRepositoryPath(
                    "COMMA.App",
                    "Views",
                    "MainWindow.axaml.cs")));

        Assert.Contains(
            "if (!OperatingSystem.IsWindows()) return;",
            source,
            StringComparison.Ordinal);
        Assert.Equal(
            2,
            Regex.Matches(
                source,
                Regex.Escape(
                    "if (!OperatingSystem.IsWindows()) return;"))
                .Count);
        Assert.Contains(
            "screen.WorkingArea.Height / screen.Scaling",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "screen.WorkingArea.Width / screen.Scaling",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "MinWidth = 1200",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "private const double CompactHeightThreshold = 820",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "private const double NormalHeightThreshold = 836",
            source,
            StringComparison.Ordinal);
        Assert.Contains(
            "Classes.Set( \"compact-height\", _isCompactHeight)",
            source,
            StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1.0, 396.0, 568.0)]
    [InlineData(1.25, 332.0, 476.0)]
    public void FullHdWindowsLayout_KeepsMiddleColumnAndPreviewSeparate(
        double scaling,
        double minimumMiddleWidth,
        double minimumPreviewWidth)
    {
        var document = LoadMainWindow();
        var mainGrid = FindNamedElement(document, "MainContentGrid");
        var columnWeights = ParseStarColumns(
            Assert.IsType<XAttribute>(
                    mainGrid.Attribute("ColumnDefinitions"))
                .Value);

        Assert.Equal([2.2, 1.15, 1.65], columnWeights);

        var logicalScreenWidth = FullHdWidth / scaling;
        var windowWidth = Math.Min(
            InitialWindowWidth,
            logicalScreenWidth - WindowsWidthMargin);
        var columnsWidth =
            windowWidth -
            MainContentHorizontalMargin -
            (2 * MainContentColumnSpacing);
        var weightSum = columnWeights.Sum();
        var middleWidth = columnsWidth * columnWeights[1] / weightSum;
        var previewWidth = columnsWidth * columnWeights[2] / weightSum;

        Assert.True(
            middleWidth >= minimumMiddleWidth,
            $"Środkowa kolumna ma tylko {middleWidth:F1} jednostek logicznych.");
        Assert.True(
            previewWidth >= minimumPreviewWidth,
            $"Podgląd ma tylko {previewWidth:F1} jednostek logicznych.");
        Assert.True(middleWidth + previewWidth < columnsWidth);
    }

    [Fact]
    public void WindowsCompactHeight_KeepsOrderListPagePlanAndFooterAccessible()
    {
        var document = LoadMainWindow();
        var normalizedXaml = NormalizeWhitespace(
            File.ReadAllText(
                GetRepositoryPath(
                    "COMMA.App",
                    "Views",
                    "MainWindow.axaml")));
        var normalizedCode = NormalizeWhitespace(
            File.ReadAllText(
                GetRepositoryPath(
                    "COMMA.App",
                    "Views",
                    "MainWindow.axaml.cs")));

        Assert.NotNull(FindNamedElement(document, "OrderDataPanel"));
        Assert.NotNull(FindNamedElement(document, "OrderListsPanel"));
        Assert.NotNull(FindNamedElement(document, "OrderGarmentsListBox"));
        Assert.NotNull(FindNamedElement(document, "PagePlanPanel"));
        Assert.NotNull(FindNamedElement(document, "BottomBar"));

        Assert.Contains(
            "OrderDataGrid.RowDefinitions = new RowDefinitions( \"Auto,*,96\")",
            normalizedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "OrderDataGrid.RowDefinitions[1].MinHeight = 160",
            normalizedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "OrderListsGrid.RowDefinitions = new RowDefinitions( \"Auto,*,Auto\")",
            normalizedCode,
            StringComparison.Ordinal);
        Assert.Contains(
            "Window.compact-height Border#BottomBar",
            normalizedXaml,
            StringComparison.Ordinal);

        var footerButtons = FindNamedElement(document, "BottomBar")
            .Descendants()
            .Where(element => element.Name.LocalName == "Button")
            .ToArray();
        var fixedFooterWidth = footerButtons.Sum(
            button => ParseDouble(button.Attribute("Width")?.Value));
        var footerSpacing = 10 * (footerButtons.Length - 1);
        var logicalWidthAt125Percent = FullHdWidth / 1.25;

        Assert.Equal(5, footerButtons.Length);
        Assert.True(
            fixedFooterWidth + footerSpacing < logicalWidthAt125Percent,
            "Dolny pasek nie mieści wszystkich przycisków przy skalowaniu 125%.");
    }

    private static XDocument LoadMainWindow()
    {
        return XDocument.Load(
            GetRepositoryPath(
                "COMMA.App",
                "Views",
                "MainWindow.axaml"));
    }

    private static XElement FindNamedElement(
        XDocument document,
        string name)
    {
        XNamespace x = "http://schemas.microsoft.com/winfx/2006/xaml";

        return Assert.Single(
            document.Descendants(),
            element => (string?)element.Attribute(x + "Name") == name);
    }

    private static double[] ParseStarColumns(string definitions)
    {
        return definitions
            .Split(',', StringSplitOptions.TrimEntries)
            .Select(value => value.TrimEnd('*'))
            .Select(ParseDouble)
            .ToArray();
    }

    private static double ParseDouble(string? value)
    {
        return double.Parse(
            Assert.IsType<string>(value),
            CultureInfo.InvariantCulture);
    }

    private static string NormalizeWhitespace(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim();
    }

    private static string GetRepositoryPath(
        params string[] segments)
    {
        return Path.GetFullPath(
            Path.Combine(
                [
                    AppContext.BaseDirectory,
                    "..",
                    "..",
                    "..",
                    "..",
                    .. segments
                ]));
    }
}
