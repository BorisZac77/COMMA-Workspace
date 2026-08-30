using System;
using Avalonia.Controls;

namespace COMMA.App.Layout;

public sealed class GarmentViewDescriptionTextBoxController : IDisposable
{
    private readonly TextBox textBox;
    private readonly Func<DescriptionTargetGeometry> getGeometry;
    private readonly GarmentViewDescriptionInputController inputController;
    private bool isApplyingLimit;

    public GarmentViewDescriptionTextBoxController(
        TextBox textBox,
        Func<int> getSelectedDrawingCount)
        : this(
            textBox,
            () =>
            {
                var count = getSelectedDrawingCount();

                return GarmentViewDescriptionLayout.GetTarget(
                    count < 3,
                    count);
            })
    {
    }

    public GarmentViewDescriptionTextBoxController(
        TextBox textBox,
        Func<DescriptionLayoutTarget> getTarget)
        : this(
            textBox,
            () => GarmentViewDescriptionLayout.GetReferenceGeometry(
                getTarget()))
    {
    }

    public GarmentViewDescriptionTextBoxController(
        TextBox textBox,
        Func<DescriptionTargetGeometry> getGeometry)
    {
        this.textBox =
            textBox ?? throw new ArgumentNullException(nameof(textBox));
        this.getGeometry =
            getGeometry ??
            throw new ArgumentNullException(nameof(getGeometry));
        inputController =
            new GarmentViewDescriptionInputController(
                textBox.Text,
                getGeometry());

        textBox.TextChanging +=
            OnTextChanging;
        textBox.TextChanged +=
            OnTextChanged;
    }

    public string AcceptedText =>
        inputController.AcceptedText;

    public bool IsAtCapacity =>
        inputController.IsAtCapacity;

    public int SelectedDrawingCount =>
        inputController.SelectedDrawingCount;

    public DescriptionLayoutTarget Target =>
        inputController.Target;

    public DescriptionTargetGeometry Geometry =>
        inputController.Geometry;

    public void UpdateSelectedDrawingCount(
        int selectedDrawingCount)
    {
        inputController.UpdateSelectedDrawingCount(
            selectedDrawingCount);
    }

    public void UpdateTarget(
        DescriptionLayoutTarget target)
    {
        inputController.UpdateTarget(
            target);
    }

    public void UpdateGeometry(
        DescriptionTargetGeometry geometry)
    {
        inputController.UpdateGeometry(
            geometry);
    }

    public bool IsCurrentTextValidForCommit(
        int selectedDrawingCount)
    {
        inputController.UpdateSelectedDrawingCount(
            selectedDrawingCount);

        return inputController.IsAcceptedText(textBox.Text) &&
               GarmentViewDescriptionLayout.FitsEditorTargets(
                   textBox.Text,
                   selectedDrawingCount);
    }

    public bool IsCurrentTextValidForCommit(
        DescriptionLayoutTarget target)
    {
        inputController.UpdateTarget(
            target);

        return inputController.IsAcceptedText(textBox.Text) &&
               GarmentViewDescriptionLayout.FitsEditorTargets(
                   textBox.Text,
                   target);
    }

    public bool IsCurrentTextValidForCommit(
        DescriptionTargetGeometry geometry)
    {
        inputController.UpdateGeometry(
            geometry);

        return inputController.IsAcceptedText(textBox.Text) &&
               GarmentViewDescriptionLayout.FitsEditorTargets(
                   textBox.Text,
                   geometry);
    }

    public void Dispose()
    {
        textBox.TextChanging -=
            OnTextChanging;
        textBox.TextChanged -=
            OnTextChanged;
    }

    private void OnTextChanging(
        object? sender,
        TextChangingEventArgs e)
    {
        ApplyCurrentText();
    }

    private void OnTextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        // TextChanging is the synchronous first line of defence. TextChanged
        // reconciles the final Text value as well, so a platform-specific edit,
        // paste or nested text-buffer update cannot leave the control ahead of
        // the controller's last accepted value.
        ApplyCurrentText();
    }

    private void ApplyCurrentText()
    {
        if (isApplyingLimit)
            return;

        var change =
            inputController.Apply(
                textBox.Text,
                getGeometry());

        if (change.WasFullyAccepted &&
            inputController.IsAcceptedText(textBox.Text))
        {
            return;
        }

        isApplyingLimit =
            true;

        try
        {
            textBox.Text =
                change.Text;
            textBox.CaretIndex =
                Math.Min(
                    change.CaretIndex,
                    change.Text.Length);
            textBox.SelectionStart =
                textBox.CaretIndex;
            textBox.SelectionEnd =
                textBox.CaretIndex;
        }
        finally
        {
            isApplyingLimit =
                false;
        }
    }
}
