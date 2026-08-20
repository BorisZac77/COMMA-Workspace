using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace COMMA.Core.Models;

public class ProductionCard : INotifyPropertyChanged
{
    private string orderName = "";
    private string customer = "";
    private string receivedDate = "";
    private string dueDate = "";
    private string productionType = "";
    private string productCode = "";
    private string productName = "";
    private string colour = "";
    private string size = "";
    private string quantity = "";
    private string notes = "";
    private string productImagePath = "";
    private string clientLogoPath = "";
    private string embroideryProgramPath = "";
    private string printFilePath = "";

    private bool showFront = true;
    private bool showBack = true;
    private bool showLeft = true;
    private bool showRight = true;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string OrderName
    {
        get => orderName;
        set => SetProperty(ref orderName, value);
    }

    public string Customer
    {
        get => customer;
        set => SetProperty(ref customer, value);
    }

    public string ReceivedDate
    {
        get => receivedDate;
        set => SetProperty(ref receivedDate, value);
    }

    public string DueDate
    {
        get => dueDate;
        set => SetProperty(ref dueDate, value);
    }

    public string ProductionType
    {
        get => productionType;
        set => SetProperty(ref productionType, value);
    }

    public string ProductCode
    {
        get => productCode;
        set => SetProperty(ref productCode, value);
    }

    public string ProductName
    {
        get => productName;
        set => SetProperty(ref productName, value);
    }

    public string Colour
    {
        get => colour;
        set => SetProperty(ref colour, value);
    }

    public string Size
    {
        get => size;
        set => SetProperty(ref size, value);
    }

    public string Quantity
    {
        get => quantity;
        set => SetProperty(ref quantity, value);
    }

    public string Notes
    {
        get => notes;
        set => SetProperty(ref notes, value);
    }

    public string ProductImagePath
    {
        get => productImagePath;
        set => SetProperty(ref productImagePath, value);
    }

    public string ClientLogoPath
    {
        get => clientLogoPath;
        set => SetProperty(ref clientLogoPath, value);
    }

    public string EmbroideryProgramPath
    {
        get => embroideryProgramPath;
        set => SetProperty(ref embroideryProgramPath, value);
    }

    public string PrintFilePath
    {
        get => printFilePath;
        set => SetProperty(ref printFilePath, value);
    }

    public bool ShowFront
    {
        get => showFront;
        set
        {
            if (SetProperty(ref showFront, value))
                NotifyDrawingSelectionChanged();
        }
    }

    public bool ShowBack
    {
        get => showBack;
        set
        {
            if (SetProperty(ref showBack, value))
                NotifyDrawingSelectionChanged();
        }
    }

    public bool ShowLeft
    {
        get => showLeft;
        set
        {
            if (SetProperty(ref showLeft, value))
                NotifyDrawingSelectionChanged();
        }
    }

    public bool ShowRight
    {
        get => showRight;
        set
        {
            if (SetProperty(ref showRight, value))
                NotifyDrawingSelectionChanged();
        }
    }

    public List<DrawingFile> Drawings { get; } = new();

    public List<LogoPlacement> Logos { get; } = new();

    public IReadOnlyList<DrawingFile> SelectedDrawings =>
        Drawings
            .Where(IsDrawingSelected)
            .OrderBy(GetDrawingOrder)
            .ToList();

    public int SelectedDrawingCount =>
        SelectedDrawings.Count;

    public string SelectedDrawingCountText =>
        SelectedDrawingCount switch
        {
            0 => "Nie wybrano rysunków",
            1 => "1 wybrany rysunek",
            2 or 3 or 4 => $"{SelectedDrawingCount} wybrane rysunki",
            _ => $"{SelectedDrawingCount} wybranych rysunków"
        };

    public void RefreshDrawingSelection()
    {
        NotifyDrawingSelectionChanged();
    }

    private bool IsDrawingSelected(DrawingFile drawing)
    {
        if (drawing.IsFront)
            return ShowFront;

        if (drawing.IsBack)
            return ShowBack;

        if (drawing.IsLeft)
            return ShowLeft;

        if (drawing.IsRight)
            return ShowRight;

        return false;
    }

    private static int GetDrawingOrder(DrawingFile drawing)
    {
        if (drawing.IsFront)
            return 0;

        if (drawing.IsBack)
            return 1;

        if (drawing.IsLeft)
            return 2;

        if (drawing.IsRight)
            return 3;

        return 100;
    }

    private void NotifyDrawingSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedDrawings));
        OnPropertyChanged(nameof(SelectedDrawingCount));
        OnPropertyChanged(nameof(SelectedDrawingCountText));
    }

    private bool SetProperty<T>(
        ref T field,
        T value,
        [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);

        return true;
    }

    private void OnPropertyChanged(
        [CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(
            this,
            new PropertyChangedEventArgs(propertyName));
    }
}

public class LogoPlacement
{
    public string Name { get; set; } = "";

    public string Position { get; set; } = "";

    public string Width { get; set; } = "";

    public string Height { get; set; } = "";

    public string Colours { get; set; } = "";

    public string Technique { get; set; } = "";

    public string File { get; set; } = "";
}