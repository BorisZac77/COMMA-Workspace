using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;

namespace COMMA.App.Models;

public class ProductionCard : ObservableObject
{
    private string orderName = "";
    private string orderNumber = "";
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

    public ProductionCard()
    {
        ProductionEntries = new ObservableCollection<ProductionEntry>
        {
            new(1),
            new(2),
            new(3),
            new(4)
        };

        Garments = new ObservableCollection<OrderGarmentItem>();

        Attachments = new ObservableCollection<OrderAttachmentMetadata>();
    }

    public string OrderNumber
    {
        get => orderNumber;
        set => SetProperty(ref orderNumber, value);
    }

    public string OrderName
    {
        get => orderName;
        set
        {
            if (SetProperty(ref orderName, value))
            {
                OnPropertyChanged(nameof(PreviewOrderName));
                OnPropertyChanged(nameof(IsOrderNameEmpty));
            }
        }
    }

    public string Customer
    {
        get => customer;
        set
        {
            if (SetProperty(ref customer, value))
            {
                OnPropertyChanged(nameof(PreviewCustomer));
                OnPropertyChanged(nameof(IsCustomerEmpty));
            }
        }
    }

    public string ReceivedDate
    {
        get => receivedDate;
        set
        {
            if (SetProperty(ref receivedDate, value))
            {
                OnPropertyChanged(nameof(PreviewReceivedDate));
                OnPropertyChanged(nameof(IsReceivedDateEmpty));
            }
        }
    }

    public string DueDate
    {
        get => dueDate;
        set
        {
            if (SetProperty(ref dueDate, value))
            {
                OnPropertyChanged(nameof(PreviewDueDate));
                OnPropertyChanged(nameof(IsDueDateEmpty));
            }
        }
    }

    public string ProductionType
    {
        get => productionType;
        set
        {
            if (SetProperty(ref productionType, value))
            {
                OnPropertyChanged(nameof(PreviewProductionType));
                OnPropertyChanged(nameof(IsProductionTypeEmpty));
            }
        }
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

    public string PreviewOrderName =>
        string.IsNullOrWhiteSpace(OrderName)
            ? "NAZWA"
            : OrderName.Trim();

    public string PreviewCustomer =>
        string.IsNullOrWhiteSpace(Customer)
            ? "KLIENT"
            : Customer.Trim();

    public string PreviewReceivedDate =>
        string.IsNullOrWhiteSpace(ReceivedDate)
            ? "DD.MM.RRRR"
            : ReceivedDate.Trim();

    public string PreviewDueDate =>
        string.IsNullOrWhiteSpace(DueDate)
            ? "DD.MM.RRRR"
            : DueDate.Trim();

    public string PreviewProductionType =>
        string.IsNullOrWhiteSpace(ProductionType)
            ? "HAFT"
            : ProductionType.Trim();

    public bool IsOrderNameEmpty =>
        string.IsNullOrWhiteSpace(OrderName);

    public bool IsCustomerEmpty =>
        string.IsNullOrWhiteSpace(Customer);

    public bool IsReceivedDateEmpty =>
        string.IsNullOrWhiteSpace(ReceivedDate);

    public bool IsDueDateEmpty =>
        string.IsNullOrWhiteSpace(DueDate);

    public bool IsProductionTypeEmpty =>
        string.IsNullOrWhiteSpace(ProductionType);

    public ObservableCollection<ProductionEntry> ProductionEntries { get; }

    public ObservableCollection<OrderGarmentItem> Garments { get; }

    public ObservableCollection<OrderAttachmentMetadata> Attachments { get; }

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
            2 or 3 or 4 =>
                $"{SelectedDrawingCount} wybrane rysunki",
            _ =>
                $"{SelectedDrawingCount} wybranych rysunków"
        };

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

        if (drawing.IsLeft)
            return ShowLeft;

        if (drawing.IsRight)
            return ShowRight;

        return false;
    }

    private static int GetDrawingOrder(
        DrawingFile drawing)
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
}

public class ProductionEntry : ObservableObject
{
    private string logoName = "";
    private string dimension = "";

    public ProductionEntry(
        int number)
    {
        Number = number;

        Colours = new ObservableCollection<ProductionColourEntry>();

        Colours.CollectionChanged += (_, _) =>
        {
            RenumberColours();

            OnPropertyChanged(nameof(ColoursNotes));
            OnPropertyChanged(nameof(HasContent));
        };
    }

    public int Number { get; }

    public string LogoName
    {
        get => logoName;
        set
        {
            if (SetProperty(ref logoName, value))
            {
                OnPropertyChanged(nameof(Logging));
                OnPropertyChanged(nameof(HasContent));
            }
        }
    }

    public string Dimension
    {
        get => dimension;
        set
        {
            if (SetProperty(ref dimension, value))
            {
                OnPropertyChanged(nameof(Logging));
                OnPropertyChanged(nameof(HasContent));
            }
        }
    }

    public ObservableCollection<ProductionColourEntry> Colours { get; }

    public void AddColour()
    {
        Colours.Add(
            new ProductionColourEntry(
                Colours.Count + 1));
    }

    public void RemoveColour(
        ProductionColourEntry colour)
    {
        Colours.Remove(colour);

        RenumberColours();
    }

    public string Logging
    {
        get
        {
            if (string.IsNullOrWhiteSpace(LogoName) &&
                string.IsNullOrWhiteSpace(Dimension))
            {
                return string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Dimension))
                return LogoName.Trim();

            if (string.IsNullOrWhiteSpace(LogoName))
                return Dimension.Trim();

            return $"{LogoName.Trim()}\n{Dimension.Trim()}";
        }

        set
        {
            var safeValue =
                value?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(safeValue))
            {
                LogoName = "";
                Dimension = "";
                return;
            }

            var lines =
                safeValue
                    .Split(
                        '\n',
                        System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

            if (lines.Count == 0)
            {
                LogoName = "";
                Dimension = "";
                return;
            }

            LogoName =
                lines[0];

            Dimension =
                lines.Count > 1
                    ? string.Join(
                        " ",
                        lines.Skip(1))
                    : "";
        }
    }

    public string ColoursNotes
    {
        get =>
            string.Join(
                "\n",
                Colours
                    .Where(colour =>
                        !string.IsNullOrWhiteSpace(
                            colour.Value))
                    .Select(colour =>
                        colour.Value.Trim()));

        set
        {
            Colours.Clear();

            var safeValue =
                value?.Trim() ??
                string.Empty;

            if (string.IsNullOrWhiteSpace(safeValue))
                return;

            var lines =
                safeValue
                    .Split(
                        '\n',
                        System.StringSplitOptions.RemoveEmptyEntries)
                    .Select(line => line.Trim())
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .ToList();

            foreach (var line in lines)
            {
                Colours.Add(
                    new ProductionColourEntry(
                        Colours.Count + 1)
                    {
                        Value = line
                    });
            }

            RenumberColours();
        }
    }

    public bool HasContent =>
        !string.IsNullOrWhiteSpace(LogoName) ||
        !string.IsNullOrWhiteSpace(Dimension) ||
        Colours.Any(colour =>
            !string.IsNullOrWhiteSpace(
                colour.Value));

    private void RenumberColours()
    {
        for (
            var index = 0;
            index < Colours.Count;
            index++)
        {
            Colours[index].Number =
                index + 1;
        }
    }
}

public class ProductionColourEntry : ObservableObject
{
    private int number;
    private string value = "";

    public ProductionColourEntry(
        int number)
    {
        this.number =
            number;
    }

    public int Number
    {
        get => number;
        set => SetProperty(
            ref number,
            value);
    }

    public string Value
    {
        get => value;
        set => SetProperty(
            ref this.value,
            value);
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
