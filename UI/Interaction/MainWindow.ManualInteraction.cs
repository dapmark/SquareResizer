using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!manualState.IsLoaded)
        {
            return;
        }

        Point point = e.GetPosition(PreviewHost);
        manualState.DragMode = GetManualCropDragMode(point);

        if (manualState.DragMode == ManualCropDragMode.None)
        {
            return;
        }

        ManualCrop crop = GetManualCrop();
        manualState.IsDragging = true;
        manualState.DragStartPoint = point;
        manualState.DragStartCropX = crop.X;
        manualState.DragStartCropY = crop.Y;
        manualState.DragStartCropSize = crop.Size;

        PreviewHost.CaptureMouse();
        PreviewHost.Focus();
        PreviewHost.Cursor = GetManualCropCursor(manualState.DragMode);

        e.Handled = true;
    }

    private void OnPreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!manualState.IsLoaded || manualState.PreviewScaleX <= 0 || manualState.PreviewScaleY <= 0)
        {
            return;
        }

        Point currentPoint = e.GetPosition(PreviewHost);

        if (!manualState.IsDragging)
        {
            UpdateManualPreviewCursor(currentPoint);
            return;
        }

        ManualCrop previousCrop = GetManualCrop();
        ManualCrop crop = manualState.DragMode == ManualCropDragMode.Move
            ? GetMovedManualCrop(currentPoint)
            : GetResizedManualCrop(currentPoint);

        if (crop != previousCrop)
        {
            ApplyManualCrop(crop);
            InvalidateManualFileSizeEstimate();
            UpdateManualPreviewLayout();
            UpdateManualActionButtons();
        }

        e.Handled = true;
    }

    private void OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!manualState.IsDragging)
        {
            return;
        }

        manualState.IsDragging = false;
        manualState.DragMode = ManualCropDragMode.None;
        PreviewHost.ReleaseMouseCapture();
        UpdateManualPreviewCursor(e.GetPosition(PreviewHost));
        ScheduleManualFileSizeEstimate();

        e.Handled = true;
    }

    private ManualCrop GetMovedManualCrop(Point currentPoint)
    {
        Point adjustedPoint = GetAdjustedManualDragPoint(currentPoint);

        return ManualCropGeometry.MoveByMouse(
            GetManualDragStartCrop(),
            manualState.DragStartPoint,
            adjustedPoint,
            manualState.PreviewScaleX,
            manualState.PreviewScaleY,
            manualState.ImageWidth,
            manualState.ImageHeight);
    }

    private ManualCrop GetResizedManualCrop(Point currentPoint)
    {
        Point adjustedPoint = GetAdjustedManualDragPoint(currentPoint);
        Point startImagePoint = PreviewPointToImagePoint(manualState.DragStartPoint);
        Point currentImagePoint = PreviewPointToImagePoint(adjustedPoint);

        return ManualCropGeometry.Resize(
            manualState.DragMode,
            GetManualDragStartCrop(),
            startImagePoint,
            currentImagePoint,
            manualState.ImageWidth,
            manualState.ImageHeight,
            Keyboard.Modifiers.HasFlag(ModifierKeys.Alt));
    }

    private Point GetAdjustedManualDragPoint(Point currentPoint)
    {
        double factor = 1.0;

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            factor = 0.25;
        }
        else if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            factor = 2.0;
        }

        return ManualCropGeometry.AdjustDragPoint(manualState.DragStartPoint, currentPoint, factor);
    }

    private Point PreviewPointToImagePoint(Point point)
    {
        return ManualCropGeometry.PreviewToImagePoint(
            point,
            manualState.PreviewLeft,
            manualState.PreviewTop,
            manualState.PreviewScaleX,
            manualState.PreviewScaleY);
    }

    private void UpdateManualPreviewCursor(Point point)
    {
        PreviewHost.Cursor = GetManualCropCursor(GetManualCropDragMode(point));
    }

    private ManualCropDragMode GetManualCropDragMode(Point point)
    {
        return TryGetManualCropScreenRect(out Rect cropRect)
            ? ManualCropGeometry.HitTest(point, cropRect)
            : ManualCropDragMode.None;
    }

    private bool TryGetManualCropScreenRect(out Rect cropRect)
    {
        cropRect = Rect.Empty;

        if (!manualState.IsLoaded ||
            manualState.PreviewScaleX <= 0 ||
            manualState.PreviewScaleY <= 0 ||
            manualState.CropSize <= 0)
        {
            return false;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(PreviewHost);
        cropRect = ManualPreviewLayoutCalculator.CalculateCropRect(
            manualState.PreviewLeft,
            manualState.PreviewTop,
            manualState.PreviewScaleX,
            manualState.PreviewScaleY,
            GetManualCrop(),
            dpi.DpiScaleX,
            dpi.DpiScaleY);
        return !cropRect.IsEmpty;
    }

    private static Cursor? GetManualCropCursor(ManualCropDragMode mode)
    {
        return mode switch
        {
            ManualCropDragMode.Move => Cursors.SizeAll,
            ManualCropDragMode.ResizeTopLeft => Cursors.SizeNWSE,
            ManualCropDragMode.ResizeBottomRight => Cursors.SizeNWSE,
            ManualCropDragMode.ResizeTopRight => Cursors.SizeNESW,
            ManualCropDragMode.ResizeBottomLeft => Cursors.SizeNESW,
            ManualCropDragMode.ResizeLeft => Cursors.SizeWE,
            ManualCropDragMode.ResizeRight => Cursors.SizeWE,
            ManualCropDragMode.ResizeTop => Cursors.SizeNS,
            ManualCropDragMode.ResizeBottom => Cursors.SizeNS,
            _ => null
        };
    }

    private void OnWindowPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!manualState.IsLoaded)
        {
            return;
        }

        if (e.Key == Key.Escape)
        {
            CloseManualPreview();
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.S)
        {
            if (HasManualUnsavedChanges())
            {
                SaveManualPreview();
            }

            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control) && e.Key == Key.Home)
        {
            CenterManualCrop();
            e.Handled = true;
            return;
        }

        if (Keyboard.FocusedElement == QualityTextBox)
        {
            return;
        }

        int step = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift) ? 10 : 1;
        bool handled = true;

        switch (e.Key)
        {
            case Key.Left:
                MoveManualCrop(-step, 0);
                break;

            case Key.Right:
                MoveManualCrop(step, 0);
                break;

            case Key.Up:
                MoveManualCrop(0, -step);
                break;

            case Key.Down:
                MoveManualCrop(0, step);
                break;

            case Key.Home:
                MoveManualCropToStart();
                break;

            case Key.End:
                MoveManualCropToEnd();
                break;

            default:
                handled = false;
                break;
        }

        if (handled)
        {
            e.Handled = true;
        }
    }

    private void CenterManualCrop()
    {
        if (manualState.CropSize <= 0)
        {
            return;
        }

        ApplyManualCrop(ManualCropGeometry.Center(
            manualState.ImageWidth,
            manualState.ImageHeight,
            manualState.CropSize));

        CompleteManualCropKeyboardChange();
    }

    private void MoveManualCrop(int deltaX, int deltaY)
    {
        ApplyManualCrop(ManualCropGeometry.Move(
            GetManualCrop(),
            manualState.ImageWidth,
            manualState.ImageHeight,
            deltaX,
            deltaY));

        CompleteManualCropKeyboardChange();
    }

    private void MoveManualCropToStart()
    {
        ApplyManualCrop(ManualCropGeometry.MoveToStart(GetManualCrop()));
        CompleteManualCropKeyboardChange();
    }

    private void MoveManualCropToEnd()
    {
        ApplyManualCrop(ManualCropGeometry.MoveToEnd(
            GetManualCrop(),
            manualState.ImageWidth,
            manualState.ImageHeight));

        CompleteManualCropKeyboardChange();
    }

    private void CompleteManualCropKeyboardChange()
    {
        UpdateManualPreviewLayout();
        UpdateManualActionButtons();
        ScheduleManualFileSizeEstimate();
    }

    private ManualCrop GetManualCrop()
    {
        return new ManualCrop(manualState.CropX, manualState.CropY, manualState.CropSize);
    }

    private ManualCrop GetManualDragStartCrop()
    {
        return new ManualCrop(
            manualState.DragStartCropX,
            manualState.DragStartCropY,
            manualState.DragStartCropSize);
    }

    private void ApplyManualCrop(ManualCrop crop)
    {
        manualState.CropX = crop.X;
        manualState.CropY = crop.Y;
        manualState.CropSize = crop.Size;
    }
}
