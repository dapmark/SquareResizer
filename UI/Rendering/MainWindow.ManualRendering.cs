using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace ImageSquareResizer;

public partial class MainWindow
{
    private void OnPreviewHostSizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateManualPreviewLayout();
    }

    private void UpdateManualPreviewLayout()
    {
        if (!manualState.IsLoaded ||
            manualState.ImageWidth <= 0 ||
            manualState.ImageHeight <= 0 ||
            manualState.CropSize <= 0)
        {
            return;
        }

        double hostWidth = PreviewHost.ActualWidth;
        double hostHeight = PreviewHost.ActualHeight;

        if (hostWidth <= 0 || hostHeight <= 0)
        {
            return;
        }

        DpiScale dpi = VisualTreeHelper.GetDpi(PreviewHost);
        ManualPreviewLayout layout = ManualPreviewLayoutCalculator.Calculate(
            hostWidth,
            hostHeight,
            manualState.ImageWidth,
            manualState.ImageHeight,
            GetManualCrop(),
            dpi.DpiScaleX,
            dpi.DpiScaleY);

        manualState.PreviewLeft = layout.PreviewLeft;
        manualState.PreviewTop = layout.PreviewTop;
        manualState.PreviewScaleX = layout.ScaleX;
        manualState.PreviewScaleY = layout.ScaleY;

        PreviewImage.Width = layout.RenderedWidth;
        PreviewImage.Height = layout.RenderedHeight;
        PreviewImage.Margin = new Thickness(layout.PreviewLeft, layout.PreviewTop, 0, 0);

        CropCanvas.Width = hostWidth;
        CropCanvas.Height = hostHeight;
        CropCanvas.Clip = new RectangleGeometry(new Rect(
            layout.PreviewLeft,
            layout.PreviewTop,
            layout.RenderedWidth,
            layout.RenderedHeight));

        ApplyManualCropFrame(layout);
        UpdateManualCropAdorners(layout.CropRect);
    }

    private void ApplyManualCropFrame(ManualPreviewLayout layout)
    {
        Rect cropRect = layout.CropRect;

        CropOverlay.Width = cropRect.Width;
        CropOverlay.Height = cropRect.Height;
        Canvas.SetLeft(CropOverlay, cropRect.Left);
        Canvas.SetTop(CropOverlay, cropRect.Top);

        CropBorderTop.Width = cropRect.Width;
        CropBorderTop.Height = layout.HorizontalBorderThickness;
        Canvas.SetLeft(CropBorderTop, cropRect.Left);
        Canvas.SetTop(CropBorderTop, cropRect.Top);

        CropBorderBottom.Width = cropRect.Width;
        CropBorderBottom.Height = layout.HorizontalBorderThickness;
        Canvas.SetLeft(CropBorderBottom, cropRect.Left);
        Canvas.SetTop(CropBorderBottom, cropRect.Bottom - layout.HorizontalBorderThickness);

        CropBorderLeft.Width = layout.VerticalBorderThickness;
        CropBorderLeft.Height = cropRect.Height;
        Canvas.SetLeft(CropBorderLeft, cropRect.Left);
        Canvas.SetTop(CropBorderLeft, cropRect.Top);

        CropBorderRight.Width = layout.VerticalBorderThickness;
        CropBorderRight.Height = cropRect.Height;
        Canvas.SetLeft(CropBorderRight, cropRect.Right - layout.VerticalBorderThickness);
        Canvas.SetTop(CropBorderRight, cropRect.Top);

        CropCenterVerticalLine.StrokeThickness = layout.VerticalGuideThickness;
        CropCenterHorizontalLine.StrokeThickness = layout.HorizontalGuideThickness;
    }

    private void UpdateManualCropAdorners(Rect cropRect)
    {
        double centerX = cropRect.Left + cropRect.Width / 2.0;
        double centerY = cropRect.Top + cropRect.Height / 2.0;

        CropCenterVerticalLine.X1 = centerX;
        CropCenterVerticalLine.Y1 = cropRect.Top;
        CropCenterVerticalLine.X2 = centerX;
        CropCenterVerticalLine.Y2 = cropRect.Bottom;

        CropCenterHorizontalLine.X1 = cropRect.Left;
        CropCenterHorizontalLine.Y1 = centerY;
        CropCenterHorizontalLine.X2 = cropRect.Right;
        CropCenterHorizontalLine.Y2 = centerY;

        PositionManualCropBadge(cropRect);

        PositionCropHandle(CropHandleTopLeft, cropRect.Left, cropRect.Top);
        PositionCropHandle(CropHandleTopRight, cropRect.Right, cropRect.Top);
        PositionCropHandle(CropHandleBottomLeft, cropRect.Left, cropRect.Bottom);
        PositionCropHandle(CropHandleBottomRight, cropRect.Right, cropRect.Bottom);
        PositionCropHandle(CropHandleTopSide, centerX, cropRect.Top);
        PositionCropHandle(CropHandleRightSide, cropRect.Right, centerY);
        PositionCropHandle(CropHandleBottomSide, centerX, cropRect.Bottom);
        PositionCropHandle(CropHandleLeftSide, cropRect.Left, centerY);
    }

    private void PositionManualCropBadge(Rect cropRect)
    {
        CropSizeTextBlock.Text = GetManualCropBadgeText();
        CropSizeBadge.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        double badgeLeft = cropRect.Left + 6;
        double badgeTop = cropRect.Top + 6;
        double badgeWidth = CropSizeBadge.DesiredSize.Width;
        double badgeHeight = CropSizeBadge.DesiredSize.Height;

        if (badgeLeft + badgeWidth > cropRect.Right - 6)
        {
            badgeLeft = cropRect.Right - badgeWidth - 6;
        }

        if (badgeTop + badgeHeight > cropRect.Bottom - 6)
        {
            badgeTop = cropRect.Bottom - badgeHeight - 6;
        }

        Canvas.SetLeft(CropSizeBadge, Math.Max(cropRect.Left + 2, badgeLeft));
        Canvas.SetTop(CropSizeBadge, Math.Max(cropRect.Top + 2, badgeTop));
    }

    private void OnCropSizeBadgeMouseEnter(object sender, MouseEventArgs e)
    {
        AnimateCropSizeBadgeOpacity(0.0, TimeSpan.FromMilliseconds(80));
    }

    private void OnCropSizeBadgeMouseLeave(object sender, MouseEventArgs e)
    {
        AnimateCropSizeBadgeOpacity(1.0, TimeSpan.FromMilliseconds(120));
    }

    private void AnimateCropSizeBadgeOpacity(double targetOpacity, TimeSpan duration)
    {
        var animation = new DoubleAnimation
        {
            To = targetOpacity,
            Duration = new Duration(duration),
            EasingFunction = new QuadraticEase
            {
                EasingMode = EasingMode.EaseInOut
            }
        };

        CropSizeBadge.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void PositionCropHandle(FrameworkElement handle, double centerX, double centerY)
    {
        double width = double.IsNaN(handle.Width) || handle.Width <= 0 ? 9.0 : handle.Width;
        double height = double.IsNaN(handle.Height) || handle.Height <= 0 ? 9.0 : handle.Height;

        Canvas.SetLeft(handle, centerX - width / 2.0);
        Canvas.SetTop(handle, centerY - height / 2.0);
    }
}
