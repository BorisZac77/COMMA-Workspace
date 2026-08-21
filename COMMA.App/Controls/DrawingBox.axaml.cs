using System;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;

namespace COMMA.App.Controls;

public partial class DrawingBox : UserControl
{
    public static readonly StyledProperty<string> TitleProperty =
        AvaloniaProperty.Register<DrawingBox, string>(
            nameof(Title),
            string.Empty);

    public static readonly StyledProperty<string> ImagePathProperty =
        AvaloniaProperty.Register<DrawingBox, string>(
            nameof(ImagePath),
            string.Empty);

    public static readonly StyledProperty<double> ScaleXProperty =
        AvaloniaProperty.Register<DrawingBox, double>(
            nameof(ScaleX),
            1.0);

    public static readonly StyledProperty<double> MaxDrawingWidthProperty =
        AvaloniaProperty.Register<DrawingBox, double>(
            nameof(MaxDrawingWidth),
            double.PositiveInfinity);

    public static readonly StyledProperty<double> MaxDrawingHeightProperty =
        AvaloniaProperty.Register<DrawingBox, double>(
            nameof(MaxDrawingHeight),
            double.PositiveInfinity);

    public static readonly StyledProperty<byte[]?> CroppedImageDataProperty =
        AvaloniaProperty.Register<DrawingBox, byte[]?>(
            nameof(CroppedImageData));

    private Bitmap? drawingBitmap;

    private bool isAttachedToVisualTree;

    public DrawingBox()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string ImagePath
    {
        get => GetValue(ImagePathProperty);
        set => SetValue(ImagePathProperty, value);
    }

    public double ScaleX
    {
        get => GetValue(ScaleXProperty);
        set => SetValue(ScaleXProperty, value);
    }

    public double MaxDrawingWidth
    {
        get => GetValue(MaxDrawingWidthProperty);
        set => SetValue(MaxDrawingWidthProperty, value);
    }

    public double MaxDrawingHeight
    {
        get => GetValue(MaxDrawingHeightProperty);
        set => SetValue(MaxDrawingHeightProperty, value);
    }

    public byte[]? CroppedImageData
    {
        get => GetValue(CroppedImageDataProperty);
        set => SetValue(CroppedImageDataProperty, value);
    }

    protected override void OnPropertyChanged(
        AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (!isAttachedToVisualTree)
            return;

        if (change.Property == ImagePathProperty && CroppedImageData == null)
            LoadDrawingImage(change.NewValue as string);

        if (change.Property == CroppedImageDataProperty)
            LoadCroppedImage(change.NewValue as byte[]);
    }

    protected override void OnAttachedToVisualTree(
        VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (isAttachedToVisualTree)
            return;

        isAttachedToVisualTree =
            true;

        if (CroppedImageData is { Length: > 0 } imageData)
        {
            LoadCroppedImage(
                imageData);
        }
        else
        {
            LoadDrawingImage(
                ImagePath);
        }
    }

    protected override void OnDetachedFromVisualTree(
        VisualTreeAttachmentEventArgs e)
    {
        isAttachedToVisualTree =
            false;

        DisposeDrawingBitmap();

        base.OnDetachedFromVisualTree(e);
    }

    private void LoadDrawingImage(
        string? path)
    {
        DisposeDrawingBitmap();

        if (DrawingImage == null)
            return;

        DrawingImage.Source =
            null;

        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            return;

        try
        {
            drawingBitmap =
                new Bitmap(path);

            DrawingImage.Source =
                drawingBitmap;
        }
        catch (Exception)
        {
            DrawingImage.Source =
                null;
        }
    }

    private void LoadCroppedImage(byte[]? imageData)
    {
        if (imageData == null || imageData.Length == 0)
        {
            LoadDrawingImage(ImagePath);
            return;
        }

        DisposeDrawingBitmap();

        if (DrawingImage == null)
            return;

        DrawingImage.Source = null;

        try
        {
            using var stream = new MemoryStream(imageData, writable: false);
            drawingBitmap = new Bitmap(stream);
            DrawingImage.Source = drawingBitmap;
        }
        catch (Exception)
        {
            LoadDrawingImage(ImagePath);
        }
    }

    private void DisposeDrawingBitmap()
    {
        if (drawingBitmap == null)
            return;

        if (DrawingImage != null)
        {
            DrawingImage.Source =
                null;
        }

        drawingBitmap.Dispose();
        drawingBitmap =
            null;
    }
}
