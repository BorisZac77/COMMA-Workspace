using System;

namespace COMMA.App.Layout;

public sealed class GarmentViewDescriptionInputController
{
    private const string CapacityProbe = "i";

    private string acceptedText;
    private DescriptionTargetGeometry geometry;

    public GarmentViewDescriptionInputController(
        string? initialText,
        int selectedDrawingCount)
        : this(
            initialText,
            GetLegacyTarget(
                selectedDrawingCount))
    {
    }

    public GarmentViewDescriptionInputController(
        string? initialText,
        DescriptionLayoutTarget target)
        : this(
            initialText,
            GarmentViewDescriptionLayout.GetReferenceGeometry(target))
    {
    }

    public GarmentViewDescriptionInputController(
        string? initialText,
        DescriptionTargetGeometry geometry)
    {
        acceptedText =
            NormalizeLineEndings(initialText);
        this.geometry =
            geometry;
        IsAtCapacity =
            !GarmentViewDescriptionLayout.FitsInteractiveEditorTargets(
                acceptedText,
                geometry);
    }

    public string AcceptedText =>
        acceptedText;

    public bool IsAtCapacity { get; private set; }

    public int SelectedDrawingCount =>
        GarmentViewDescriptionLayout.GetLayoutKind(geometry.Target) ==
        DescriptionLayoutKind.FourViews
            ? 4
            : 2;

    public DescriptionLayoutTarget Target =>
        geometry.Target;

    public DescriptionTargetGeometry Geometry =>
        geometry;

    public bool IsAcceptedText(
        string? text)
    {
        return string.Equals(
            acceptedText,
            NormalizeLineEndings(text),
            StringComparison.Ordinal);
    }

    public void UpdateSelectedDrawingCount(
        int drawingCount)
    {
        UpdateTarget(
            GetLegacyTarget(
                drawingCount));
    }

    public void UpdateTarget(
        DescriptionLayoutTarget newTarget)
    {
        UpdateGeometry(
            GarmentViewDescriptionLayout.GetReferenceGeometry(newTarget));
    }

    public void UpdateGeometry(
        DescriptionTargetGeometry newGeometry)
    {
        if (geometry == newGeometry)
            return;

        geometry =
            newGeometry;
        IsAtCapacity =
            IsAtCapacity ||
            !GarmentViewDescriptionLayout.FitsInteractiveEditorTargets(
                acceptedText,
                geometry);
    }

    public DescriptionInputChange Apply(
        string? proposedText,
        int selectedDrawingCount)
    {
        return Apply(
            proposedText,
            GetLegacyTarget(
                selectedDrawingCount));
    }

    public DescriptionInputChange Apply(
        string? proposedText,
        DescriptionLayoutTarget newTarget)
    {
        return Apply(
            proposedText,
            GarmentViewDescriptionLayout.GetReferenceGeometry(newTarget));
    }

    public DescriptionInputChange Apply(
        string? proposedText,
        DescriptionTargetGeometry newGeometry)
    {
        var proposed =
            NormalizeLineEndings(proposedText);
        var previous =
            acceptedText;
        UpdateGeometry(
            newGeometry);
        var operation =
            GetOperation(
                previous,
                proposed);

        if (string.Equals(
                previous,
                proposed,
                StringComparison.Ordinal))
        {
            return new DescriptionInputChange(
                previous,
                previous.Length,
                true);
        }

        if (IsAtCapacity)
        {
            return ApplyAtCapacity(
                previous,
                proposed,
                operation);
        }

        var limited =
            GarmentViewDescriptionLayout.LimitInteractiveEditorTextChange(
                previous,
                proposed,
                geometry);
        var wasFullyAccepted =
            string.Equals(
                proposed,
                limited,
                StringComparison.Ordinal);

        acceptedText =
            limited;
        IsAtCapacity =
            !wasFullyAccepted &&
            operation.InsertedLength > 0;

        return new DescriptionInputChange(
            limited,
            GetCaretIndexAfterChange(
                previous,
                limited,
                operation.PrefixLength),
            wasFullyAccepted);
    }

    private DescriptionInputChange ApplyAtCapacity(
        string previous,
        string proposed,
        DescriptionTextOperation operation)
    {
        var isPureDeletion =
            operation.RemovedLength > 0 &&
            operation.InsertedLength == 0;
        var isShorterReplacement =
            operation.RemovedLength >
            operation.InsertedLength &&
            operation.InsertedLength > 0;

        if (isPureDeletion)
        {
            return AcceptShortening(
                previous,
                proposed,
                operation);
        }

        if (isShorterReplacement &&
            GarmentViewDescriptionLayout.FitsInteractiveEditorTargets(
                proposed,
                geometry))
        {
            return AcceptShortening(
                previous,
                proposed,
                operation);
        }

        if (operation.InsertedLength ==
                operation.RemovedLength &&
            operation.InsertedLength > 0 &&
            GarmentViewDescriptionLayout.FitsInteractiveEditorTargets(
                proposed,
                geometry))
        {
            acceptedText =
                proposed;

            return new DescriptionInputChange(
                proposed,
                GetCaretIndexAfterChange(
                    previous,
                    proposed,
                    operation.PrefixLength),
                true);
        }

        return new DescriptionInputChange(
            previous,
            operation.PrefixLength,
            false);
    }

    private DescriptionInputChange AcceptShortening(
        string previous,
        string proposed,
        DescriptionTextOperation operation)
    {
        acceptedText =
            proposed;
        IsAtCapacity =
            !HasFreeCapacity(
                proposed);

        return new DescriptionInputChange(
            proposed,
            GetCaretIndexAfterChange(
                previous,
                proposed,
                operation.PrefixLength),
            true);
    }

    private bool HasFreeCapacity(
        string text)
    {
        return GarmentViewDescriptionLayout.FitsInteractiveEditorTargets(
            text + CapacityProbe,
            geometry);
    }

    private static int GetCaretIndexAfterChange(
        string previous,
        string current,
        int operationStart)
    {
        if (string.Equals(
                previous,
                current,
                StringComparison.Ordinal))
        {
            return operationStart;
        }

        var suffixLength = 0;
        var maximumSuffixLength = Math.Min(
            previous.Length,
            current.Length);

        while (suffixLength < maximumSuffixLength &&
               previous[^(suffixLength + 1)] ==
               current[^(suffixLength + 1)])
        {
            suffixLength++;
        }

        return current.Length -
               suffixLength;
    }

    private static DescriptionTextOperation GetOperation(
        string previous,
        string proposed)
    {
        var prefixLength = 0;
        var prefixMaximum = Math.Min(
            previous.Length,
            proposed.Length);

        while (prefixLength < prefixMaximum &&
               previous[prefixLength] == proposed[prefixLength])
        {
            prefixLength++;
        }

        var suffixLength = 0;
        var suffixMaximum = Math.Min(
            previous.Length - prefixLength,
            proposed.Length - prefixLength);

        while (suffixLength < suffixMaximum &&
               previous[^(suffixLength + 1)] ==
               proposed[^(suffixLength + 1)])
        {
            suffixLength++;
        }

        return new DescriptionTextOperation(
            prefixLength,
            previous.Length - prefixLength - suffixLength,
            proposed.Length - prefixLength - suffixLength);
    }

    private static string NormalizeLineEndings(
        string? value)
    {
        return (value ?? "")
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
    }

    private static DescriptionLayoutTarget GetLegacyTarget(
        int selectedDrawingCount)
    {
        return GarmentViewDescriptionLayout.GetTarget(
            selectedDrawingCount < 3,
            selectedDrawingCount);
    }
}

internal readonly record struct DescriptionTextOperation(
    int PrefixLength,
    int RemovedLength,
    int InsertedLength);

public readonly record struct DescriptionInputChange(
    string Text,
    int CaretIndex,
    bool WasFullyAccepted);
