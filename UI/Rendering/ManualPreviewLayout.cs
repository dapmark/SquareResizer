using System;
using System.Windows;

namespace ImageSquareResizer;

internal readonly record struct ManualPreviewLayout(
    double PreviewLeft,
    double PreviewTop,
    double RenderedWidth,
    double RenderedHeight,
    double ScaleX,
    double ScaleY,
    Rect CropRect,
    double VerticalBorderThickness,
    double HorizontalBorderThickness,
    double VerticalGuideThickness,
    double HorizontalGuideThickness);

internal static class ManualPreviewLayoutCalculator
{
    public static ManualPreviewLayout Calculate(
        double hostWidth,
        double hostHeight,
        int imageWidth,
        int imageHeight,
        ManualCrop crop,
        double dpiScaleX,
        double dpiScaleY)
    {
        double previewScale = Math.Min(hostWidth / imageWidth, hostHeight / imageHeight);
        double renderedWidth = imageWidth * previewScale;
        double renderedHeight = imageHeight * previewScale;
        double previewLeft = (hostWidth - renderedWidth) / 2.0;
        double previewTop = (hostHeight - renderedHeight) / 2.0;

        (previewLeft, renderedWidth) = SnapSpan(previewLeft, renderedWidth, dpiScaleX);
        (previewTop, renderedHeight) = SnapSpan(previewTop, renderedHeight, dpiScaleY);

        double scaleX = renderedWidth / imageWidth;
        double scaleY = renderedHeight / imageHeight;
        Rect cropRect = CalculateCropRect(
            previewLeft,
            previewTop,
            scaleX,
            scaleY,
            crop,
            dpiScaleX,
            dpiScaleY);

        return new ManualPreviewLayout(
            previewLeft,
            previewTop,
            renderedWidth,
            renderedHeight,
            scaleX,
            scaleY,
            cropRect,
            Math.Min(2.0 / dpiScaleX, cropRect.Width),
            Math.Min(2.0 / dpiScaleY, cropRect.Height),
            1.0 / dpiScaleX,
            1.0 / dpiScaleY);
    }

    public static Rect CalculateCropRect(
        double previewLeft,
        double previewTop,
        double previewScaleX,
        double previewScaleY,
        ManualCrop crop,
        double dpiScaleX,
        double dpiScaleY)
    {
        double cropLeft = previewLeft + crop.X * previewScaleX;
        double cropTop = previewTop + crop.Y * previewScaleY;
        double cropWidth = crop.Size * previewScaleX;
        double cropHeight = crop.Size * previewScaleY;

        (cropLeft, cropWidth) = SnapSpan(cropLeft, cropWidth, dpiScaleX);
        (cropTop, cropHeight) = SnapSpan(cropTop, cropHeight, dpiScaleY);

        return new Rect(cropLeft, cropTop, cropWidth, cropHeight);
    }

    private static (double Start, double Length) SnapSpan(double start, double length, double dpiScale)
    {
        double snappedStart = Snap(start, dpiScale);
        double snappedEnd = Snap(start + length, dpiScale);
        return (snappedStart, Math.Max(0, snappedEnd - snappedStart));
    }

    private static double Snap(double value, double dpiScale)
    {
        return dpiScale > 0 ? Math.Round(value * dpiScale) / dpiScale : value;
    }
}
