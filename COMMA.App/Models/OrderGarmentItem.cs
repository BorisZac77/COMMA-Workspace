using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMMA.App.Models;

public class OrderGarmentItem : ObservableObject
{
    private string productCode = "";
    private string name = "";
    private string colour = "";
    private string variant = "";

    private bool showFront;
    private bool showBack;
    private bool showRight;
    private bool showLeft;

    private bool startNewPage;


    public string ProductCode
    {
        get => productCode;
        set => SetProperty(
            ref productCode,
            value);
    }


    public string Name
    {
        get => name;
        set
        {
            if (SetProperty(
                    ref name,
                    value))
            {
                OnPropertyChanged(
                    nameof(DisplayName));
            }
        }
    }


    public string Colour
    {
        get => colour;
        set
        {
            if (SetProperty(
                    ref colour,
                    value))
            {
                OnPropertyChanged(
                    nameof(DisplayName));
            }
        }
    }


    public string Variant
    {
        get => variant;
        set
        {
            if (SetProperty(
                    ref variant,
                    value))
            {
                OnPropertyChanged(
                    nameof(DisplayName));
            }
        }
    }


    public bool ShowFront
    {
        get => showFront;
        set
        {
            if (SetProperty(
                    ref showFront,
                    value))
            {
                NotifyDrawingSelectionChanged();
            }
        }
    }


    public bool ShowBack
    {
        get => showBack;
        set
        {
            if (SetProperty(
                    ref showBack,
                    value))
            {
                NotifyDrawingSelectionChanged();
            }
        }
    }


    public bool ShowRight
    {
        get => showRight;
        set
        {
            if (SetProperty(
                    ref showRight,
                    value))
            {
                NotifyDrawingSelectionChanged();
            }
        }
    }


    public bool ShowLeft
    {
        get => showLeft;
        set
        {
            if (SetProperty(
                    ref showLeft,
                    value))
            {
                NotifyDrawingSelectionChanged();
            }
        }
    }


    public bool StartNewPage
    {
        get => startNewPage;
        set => SetProperty(
            ref startNewPage,
            value);
    }


    public List<DrawingFile> Drawings { get; } =
        new();


    public string DisplayName
    {
        get
        {
            var parts =
                new List<string>();

            if (!string.IsNullOrWhiteSpace(
                    Name))
            {
                parts.Add(
                    Name.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                    Colour))
            {
                parts.Add(
                    Colour.Trim());
            }

            if (!string.IsNullOrWhiteSpace(
                    Variant))
            {
                parts.Add(
                    Variant.Trim());
            }

            return string.Join(
                " – ",
                parts);
        }
    }


    public IReadOnlyList<DrawingFile> SelectedDrawings =>
        Drawings
            .Where(
                IsDrawingSelected)
            .OrderBy(
                GetDrawingOrder)
            .ToList();


    public int SelectedDrawingCount =>
        SelectedDrawings.Count;


    public string SelectedDrawingCountText =>
        SelectedDrawingCount switch
        {
            0 =>
                "Nie wybrano rysunków",

            1 =>
                "1 wybrany rysunek",

            2 or 3 or 4 =>
                $"{SelectedDrawingCount} wybrane rysunki",

            _ =>
                $"{SelectedDrawingCount} wybranych rysunków"
        };


    public void LoadProduct(
        Product product)
    {
        ProductCode =
            product.Code;

        Name =
            product.DisplayName;

        Drawings.Clear();

        Drawings.AddRange(
            product.Drawings);

        ShowFront =
            false;

        ShowBack =
            false;

        ShowRight =
            false;

        ShowLeft =
            false;

        NotifyDrawingSelectionChanged();
    }


    public OrderGarmentItem Duplicate()
    {
        var duplicate =
            new OrderGarmentItem
            {
                ProductCode =
                    ProductCode,

                Name =
                    Name,

                Colour =
                    Colour,

                Variant =
                    Variant,

                ShowFront =
                    ShowFront,

                ShowBack =
                    ShowBack,

                ShowRight =
                    ShowRight,

                ShowLeft =
                    ShowLeft,

                StartNewPage =
                    StartNewPage
            };

        duplicate.Drawings.AddRange(
            Drawings);

        duplicate.NotifyDrawingSelectionChanged();

        return duplicate;
    }


    public void RefreshDrawingSelection()
    {
        NotifyDrawingSelectionChanged();
    }


    private bool IsDrawingSelected(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return ShowFront;

        if (drawing.IsBack)
            return ShowBack;

        if (drawing.IsRight)
            return ShowRight;

        if (drawing.IsLeft)
            return ShowLeft;

        return false;
    }


    private static int GetDrawingOrder(
        DrawingFile drawing)
    {
        if (drawing.IsFront)
            return 0;

        if (drawing.IsBack)
            return 1;

        if (drawing.IsRight)
            return 2;

        if (drawing.IsLeft)
            return 3;

        return 100;
    }


    private void NotifyDrawingSelectionChanged()
    {
        OnPropertyChanged(
            nameof(SelectedDrawings));

        OnPropertyChanged(
            nameof(SelectedDrawingCount));

        OnPropertyChanged(
            nameof(SelectedDrawingCountText));
    }
}