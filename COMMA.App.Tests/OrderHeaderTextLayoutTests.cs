using COMMA.App.Layout;
using COMMA.App.Services.Pdf;

namespace COMMA.App.Tests;

public sealed class OrderHeaderTextLayoutTests
{
    [Fact]
    public void ShortNumberAndNameUseSameLargerBaseSize()
    {
        var number = OrderHeaderTextLayout.FitNumber(
            "324234",
            OrderHeaderTextLayout.PdfFirstPageNumberGeometry);
        var name = OrderHeaderTextLayout.FitName(
            "PLOPSA 4.0 OK",
            OrderHeaderTextLayout.PdfFirstPageNameGeometry);

        Assert.True(number.Fits);
        Assert.True(name.Fits);
        Assert.Equal(OrderHeaderTextLayout.BaseFontSize, number.FontSize);
        Assert.Equal(number.FontSize, name.FontSize);
        Assert.Equal(1, number.LineCount);
        Assert.Equal(1, name.LineCount);
    }

    [Fact]
    public void HeaderGeometriesReserveRequiredHorizontalInsets()
    {
        Assert.Equal(
            PdfStyles.FirstPageHeaderOrderNumberWidth -
            PdfStyles.HeaderIdentityHorizontalPadding * 2d,
            OrderHeaderTextLayout.PdfFirstPageNumberGeometry.AvailableWidth);
        Assert.Equal(
            108.3d - OrderHeaderTextLayout.PreviewHorizontalInset * 2d,
            OrderHeaderTextLayout.PreviewFirstPageNumberGeometry.AvailableWidth,
            precision: 6);
        Assert.True(
            OrderHeaderTextLayout.PreviewHorizontalInset >=
            PdfStyles.HeaderIdentityHorizontalPadding);
    }

    [Fact]
    public void VeryLongNumberStaysCompleteOnOneLine()
    {
        const string value =
            "ZL-2026-000000000000000000000000000000000000000042";
        var fit = OrderHeaderTextLayout.FitNumber(
            value,
            OrderHeaderTextLayout.PdfFirstPageNumberGeometry);

        Assert.True(fit.Fits);
        Assert.Equal(value, fit.DisplayText);
        Assert.Equal(1, fit.LineCount);
        Assert.True(fit.FontSize < OrderHeaderTextLayout.BaseFontSize);
        AssertInside(fit, OrderHeaderTextLayout.PdfFirstPageNumberGeometry);
    }

    [Fact]
    public void LongNameFirstShrinksAndRemainsOnOneLine()
    {
        const string value = "BARDZO DLUGA NAZWA ZLECENIA";
        var fit = OrderHeaderTextLayout.FitName(
            value,
            OrderHeaderTextLayout.PdfFirstPageNameGeometry);

        Assert.True(fit.Fits);
        Assert.Equal(1, fit.LineCount);
        Assert.True(fit.FontSize < OrderHeaderTextLayout.BaseFontSize);
        Assert.True(
            fit.FontSize >=
            OrderHeaderTextLayout.PreferredSingleLineMinimumFontSize);
        AssertInside(fit, OrderHeaderTextLayout.PdfFirstPageNameGeometry);
    }

    [Fact]
    public void LongerNameUsesAtMostTwoCenteredLinesWithoutLosingText()
    {
        const string value =
            "BARDZO DŁUGA NAZWA ZLECENIA WYMAGAJĄCA DWÓCH PEŁNYCH LINII";
        var fit = OrderHeaderTextLayout.FitName(
            value,
            OrderHeaderTextLayout.PdfFirstPageNameGeometry);

        Assert.True(fit.Fits);
        Assert.Equal(2, fit.LineCount);
        Assert.Contains('\n', fit.DisplayText);
        Assert.Equal(
            WithoutWhitespace(value),
            WithoutWhitespace(fit.DisplayText));
        AssertInside(fit, OrderHeaderTextLayout.PdfFirstPageNameGeometry);
    }

    [Theory]
    [MemberData(nameof(AllHeaderGeometries))]
    public void FittedValuesStayInsideEveryFirstAndLaterHeaderField(
        HeaderTextGeometry numberGeometry,
        HeaderTextGeometry nameGeometry)
    {
        var number = OrderHeaderTextLayout.FitNumber(
            "2026-EXTREMELY-LONG-ORDER-NUMBER-00000000000042",
            numberGeometry);
        var name = OrderHeaderTextLayout.FitName(
            "NAZWA ZLECENIA O DŁUGIEJ TREŚCI DO KONTROLI GRANIC POLA",
            nameGeometry);

        Assert.True(number.Fits);
        Assert.True(name.Fits);
        Assert.Equal(1, number.LineCount);
        Assert.InRange(name.LineCount, 1, 2);
        AssertInside(number, numberGeometry);
        AssertInside(name, nameGeometry);
    }

    public static TheoryData<HeaderTextGeometry, HeaderTextGeometry>
        AllHeaderGeometries =>
        new()
        {
            {
                OrderHeaderTextLayout.PdfFirstPageNumberGeometry,
                OrderHeaderTextLayout.PdfFirstPageNameGeometry
            },
            {
                OrderHeaderTextLayout.PdfLaterPageNumberGeometry,
                OrderHeaderTextLayout.PdfLaterPageNameGeometry
            },
            {
                OrderHeaderTextLayout.PreviewFirstPageNumberGeometry,
                OrderHeaderTextLayout.PreviewFirstPageNameGeometry
            },
            {
                OrderHeaderTextLayout.PreviewLaterPageNumberGeometry,
                OrderHeaderTextLayout.PreviewLaterPageNameGeometry
            }
        };

    private static void AssertInside(
        HeaderTextFit fit,
        HeaderTextGeometry geometry)
    {
        Assert.InRange(fit.MaximumLineWidth, 0, geometry.AvailableWidth + 0.1);
        Assert.InRange(fit.TextHeight, 0, geometry.AvailableHeight + 0.1);
    }

    private static string WithoutWhitespace(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
}
