using System.Xml.Linq;

namespace COMMA.App.Tests;

public sealed class ApplicationBrandingTests
{
    [Fact]
    public void ApplicationBranding_UsesWorkspaceFourPointOneNamesAndVersions()
    {
        var appDocument = XDocument.Load(
            GetRepositoryPath("COMMA.App", "App.axaml"));
        var appRoot = Assert.IsType<XElement>(appDocument.Root);
        Assert.Equal(
            "COMMA Workspace 4.1",
            (string?)appRoot.Attribute("Name"));

        var windowDocument = XDocument.Load(
            GetRepositoryPath("COMMA.App", "Views", "MainWindow.axaml"));
        var windowRoot = Assert.IsType<XElement>(windowDocument.Root);
        Assert.Equal(
            "COMMA Workspace — v4.1.0",
            (string?)windowRoot.Attribute("Title"));
        Assert.Contains(
            windowDocument.Descendants(),
            element =>
                element.Name.LocalName == "TextBlock" &&
                (string?)element.Attribute("Text") ==
                "COMMA Workspace 4.1");

        var windowCodeBehind = File.ReadAllText(
            GetRepositoryPath(
                "COMMA.App",
                "Views",
                "MainWindow.axaml.cs"));
        Assert.Contains(
            "COMMA Workspace — v4.1.0",
            windowCodeBehind,
            StringComparison.Ordinal);
        Assert.DoesNotContain(
            "COMMA Workspace — v4.0.0",
            windowCodeBehind,
            StringComparison.Ordinal);

        var projectDocument = XDocument.Load(
            GetRepositoryPath("COMMA.App", "COMMA.App.csproj"));
        AssertProjectProperty(projectDocument, "Version", "4.1.0");
        AssertProjectProperty(
            projectDocument,
            "AssemblyVersion",
            "4.1.0.0");
        AssertProjectProperty(
            projectDocument,
            "FileVersion",
            "4.1.0.0");
        AssertProjectProperty(
            projectDocument,
            "InformationalVersion",
            "4.1.0");
        Assert.DoesNotContain(
            projectDocument.Descendants(),
            element => element.Name.LocalName == "AssemblyName");

        var buildScript = File.ReadAllText(
            GetRepositoryPath("build_app.sh"));
        Assert.Contains(
            "APP_NAME=\"COMMA Workspace 4.1.app\"",
            buildScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "<key>CFBundleName</key>\n<string>COMMA Workspace 4.1</string>",
            buildScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "<key>CFBundleDisplayName</key>\n<string>COMMA Workspace 4.1</string>",
            buildScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "<key>CFBundleVersion</key>\n<string>4.1.0</string>",
            buildScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "<key>CFBundleShortVersionString</key>\n<string>4.1.0</string>",
            buildScript,
            StringComparison.Ordinal);
        Assert.Contains(
            "codesign --force --deep --sign - \"$APP_PATH\"",
            buildScript,
            StringComparison.Ordinal);
    }

    private static void AssertProjectProperty(
        XDocument document,
        string propertyName,
        string expectedValue)
    {
        var property = Assert.Single(
            document.Descendants(),
            element => element.Name.LocalName == propertyName);
        Assert.Equal(expectedValue, property.Value);
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
