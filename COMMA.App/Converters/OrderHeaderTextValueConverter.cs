using System;
using System.Globalization;
using Avalonia.Data.Converters;
using COMMA.App.Layout;

namespace COMMA.App.Converters;

public sealed class OrderHeaderTextValueConverter : IValueConverter
{
    public object Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture)
    {
        var mode = Enum.Parse<OrderHeaderTextMode>(
            parameter?.ToString() ?? string.Empty,
            ignoreCase: false);
        var text = value?.ToString();
        var fit = mode switch
        {
            OrderHeaderTextMode.FirstNumberText or
            OrderHeaderTextMode.FirstNumberFontSize =>
                OrderHeaderTextLayout.FitNumber(
                    text,
                    OrderHeaderTextLayout.PreviewFirstPageNumberGeometry),
            OrderHeaderTextMode.FirstNameText or
            OrderHeaderTextMode.FirstNameFontSize or
            OrderHeaderTextMode.FirstNameLineHeight =>
                OrderHeaderTextLayout.FitName(
                    text,
                    OrderHeaderTextLayout.PreviewFirstPageNameGeometry),
            OrderHeaderTextMode.LaterNumberText or
            OrderHeaderTextMode.LaterNumberFontSize =>
                OrderHeaderTextLayout.FitNumber(
                    text,
                    OrderHeaderTextLayout.PreviewLaterPageNumberGeometry),
            OrderHeaderTextMode.LaterNameText or
            OrderHeaderTextMode.LaterNameFontSize or
            OrderHeaderTextMode.LaterNameLineHeight =>
                OrderHeaderTextLayout.FitName(
                    text,
                    OrderHeaderTextLayout.PreviewLaterPageNameGeometry),
            _ => throw new ArgumentOutOfRangeException(nameof(parameter))
        };

        if (mode is OrderHeaderTextMode.FirstNameLineHeight or
            OrderHeaderTextMode.LaterNameLineHeight)
        {
            return fit.FontSize * OrderHeaderTextLayout.LineHeight;
        }

        return mode is OrderHeaderTextMode.FirstNumberFontSize or
            OrderHeaderTextMode.FirstNameFontSize or
            OrderHeaderTextMode.LaterNumberFontSize or
            OrderHeaderTextMode.LaterNameFontSize
                ? fit.FontSize
                : fit.DisplayText;
    }

    public object ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture) =>
        throw new NotSupportedException();
}

public enum OrderHeaderTextMode
{
    FirstNumberText,
    FirstNumberFontSize,
    FirstNameText,
    FirstNameFontSize,
    FirstNameLineHeight,
    LaterNumberText,
    LaterNumberFontSize,
    LaterNameText,
    LaterNameFontSize,
    LaterNameLineHeight
}
