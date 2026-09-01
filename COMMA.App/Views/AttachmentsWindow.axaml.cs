using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using COMMA.App.Models;
using COMMA.App.Services.Attachments;

namespace COMMA.App.Views;

public partial class AttachmentsWindow : Window
{
    private readonly ProductionCard card;
    private readonly OrderAttachmentManager manager;
    private readonly bool ownsManager;

    public AttachmentsWindow()
        : this(
            new ProductionCard(),
            new OrderAttachmentManager(),
            ownsManager: true)
    {
    }

    public AttachmentsWindow(
        ProductionCard card,
        OrderAttachmentManager manager)
        : this(card, manager, ownsManager: false)
    {
    }

    private AttachmentsWindow(
        ProductionCard card,
        OrderAttachmentManager manager,
        bool ownsManager)
    {
        this.card = card;
        this.manager = manager;
        this.ownsManager = ownsManager;
        DataContext = card;

        InitializeComponent();

        AddButton.Click += OnAddButtonClick;
        RemoveButton.Click += OnRemoveButtonClick;
        MoveUpButton.Click += OnMoveUpButtonClick;
        MoveDownButton.Click += OnMoveDownButtonClick;
        CloseButton.Click += (_, _) => Close();
        AttachmentsList.SelectionChanged += (_, _) => UpdateButtonStates();
        card.Attachments.CollectionChanged += OnAttachmentsCollectionChanged;
        Closed += OnClosed;

        UpdateButtonStates();
    }

    private void OnAttachmentsCollectionChanged(
        object? sender,
        NotifyCollectionChangedEventArgs e)
    {
        UpdateButtonStates();
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        card.Attachments.CollectionChanged -= OnAttachmentsCollectionChanged;

        if (ownsManager)
            manager.Dispose();
    }

    private async void OnAddButtonClick(object? sender, RoutedEventArgs e)
    {
        var files = await StorageProvider.OpenFilePickerAsync(
            new FilePickerOpenOptions
            {
                Title = "Dodaj załączniki do zlecenia",
                AllowMultiple = true,
                FileTypeFilter =
                [
                    new FilePickerFileType("Obsługiwane załączniki")
                    {
                        Patterns = ["*.pdf", "*.jpg", "*.jpeg", "*.png"],
                        MimeTypes = ["application/pdf", "image/jpeg", "image/png"]
                    }
                ]
            });

        if (files.Count == 0)
            return;

        var paths = files
            .Select(file => file.TryGetLocalPath())
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Cast<string>()
            .ToList();

        if (paths.Count == 0)
            return;

        var errors = manager.AddFiles(paths, card.Attachments);

        if (card.Attachments.Count > 0)
            AttachmentsList.SelectedItem = card.Attachments[^1];

        UpdateButtonStates();

        if (errors.Count > 0)
            await ShowErrors(errors);
    }

    private void OnRemoveButtonClick(object? sender, RoutedEventArgs e)
    {
        if (AttachmentsList.SelectedItem is not OrderAttachmentMetadata selected)
            return;

        var oldIndex = card.Attachments.IndexOf(selected);
        manager.Remove(selected, card.Attachments);

        if (card.Attachments.Count > 0)
        {
            AttachmentsList.SelectedIndex = Math.Min(
                oldIndex,
                card.Attachments.Count - 1);
        }

        UpdateButtonStates();
    }

    private void OnMoveUpButtonClick(object? sender, RoutedEventArgs e)
    {
        MoveSelectedAttachment(-1);
    }

    private void OnMoveDownButtonClick(object? sender, RoutedEventArgs e)
    {
        MoveSelectedAttachment(1);
    }

    private void MoveSelectedAttachment(int offset)
    {
        if (AttachmentsList.SelectedItem is not OrderAttachmentMetadata selected)
            return;

        var oldIndex = card.Attachments.IndexOf(selected);
        if (!manager.Move(selected, offset, card.Attachments))
            return;

        AttachmentsList.SelectedIndex = oldIndex + offset;
        UpdateButtonStates();
    }

    private void UpdateButtonStates()
    {
        var selectedIndex = AttachmentsList.SelectedIndex;
        AddButton.IsEnabled =
            card.Attachments.Count < OrderAttachmentLimits.MaximumAttachmentCount;
        RemoveButton.IsEnabled = selectedIndex >= 0;
        MoveUpButton.IsEnabled = selectedIndex > 0;
        MoveDownButton.IsEnabled =
            selectedIndex >= 0 && selectedIndex < card.Attachments.Count - 1;
    }

    private async Task ShowErrors(IReadOnlyList<string> errors)
    {
        var dialog = new Window
        {
            Title = "Nie dodano części załączników",
            Width = 560,
            Height = 260,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        var closeButton = new Button
        {
            Content = "OK",
            Width = 90,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var grid = new Grid
        {
            Margin = new Avalonia.Thickness(22),
            RowDefinitions = new RowDefinitions("*,Auto"),
            RowSpacing = 16
        };
        var message = new TextBlock
        {
            Text = string.Join(Environment.NewLine, errors),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap
        };
        Grid.SetRow(message, 0);
        Grid.SetRow(closeButton, 1);
        grid.Children.Add(message);
        grid.Children.Add(closeButton);
        dialog.Content = grid;
        closeButton.Click += (_, _) => dialog.Close();
        await dialog.ShowDialog(this);
    }
}
