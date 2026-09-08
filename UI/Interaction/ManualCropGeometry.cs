using System;
using System.Windows;

namespace ImageSquareResizer;

internal readonly record struct ManualCrop(int X, int Y, int Size);

internal static class ManualCropGeometry
{
    public const double CornerHitSize = 12.0;
    public const double SideHitSize = 8.0;
    public const int MinimumCropSize = 32;

    public static ManualCrop Center(int imageWidth, int imageHeight, int cropSize)
    {
        return new ManualCrop(
            Math.Max(0, (imageWidth - cropSize) / 2),
            Math.Max(0, (imageHeight - cropSize) / 2),
            cropSize);
    }

    public static bool IsCentered(ManualCrop crop, int imageWidth, int imageHeight)
    {
        if (crop.Size <= 0)
        {
            return true;
        }

        return crop == Center(imageWidth, imageHeight, crop.Size);
    }

    public static ManualCrop RotateClockwise(ManualCrop crop, int imageWidth, int imageHeight)
    {
        if (crop.Size <= 0 || imageWidth <= 0 || imageHeight <= 0)
        {
            return crop;
        }

        return new ManualCrop(
            ClampCoordinate(imageHeight - crop.Y - crop.Size, imageHeight - crop.Size),
            ClampCoordinate(crop.X, imageWidth - crop.Size),
            crop.Size);
    }

    public static ManualCrop Move(
        ManualCrop crop,
        int imageWidth,
        int imageHeight,
        int deltaX,
        int deltaY)
    {
        return crop with
        {
            X = ClampCoordinate(crop.X + deltaX, imageWidth - crop.Size),
            Y = ClampCoordinate(crop.Y + deltaY, imageHeight - crop.Size)
        };
    }

    public static ManualCrop MoveToStart(ManualCrop crop)
    {
        return crop with { X = 0, Y = 0 };
    }

    public static ManualCrop MoveToEnd(ManualCrop crop, int imageWidth, int imageHeight)
    {
        return crop with
        {
            X = Math.Max(0, imageWidth - crop.Size),
            Y = Math.Max(0, imageHeight - crop.Size)
        };
    }

    public static ManualCrop MoveByMouse(
        ManualCrop startCrop,
        Point startPoint,
        Point currentPoint,
        double previewScaleX,
        double previewScaleY,
        int imageWidth,
        int imageHeight)
    {
        double deltaX = currentPoint.X - startPoint.X;
        double deltaY = currentPoint.Y - startPoint.Y;
        int offsetX = (int)Math.Round(deltaX / previewScaleX);
        int offsetY = (int)Math.Round(deltaY / previewScaleY);

        return new ManualCrop(
            ClampCoordinate(startCrop.X + offsetX, imageWidth - startCrop.Size),
            ClampCoordinate(startCrop.Y + offsetY, imageHeight - startCrop.Size),
            startCrop.Size);
    }

    public static ManualCrop Resize(
        ManualCropDragMode mode,
        ManualCrop startCrop,
        Point startImagePoint,
        Point currentImagePoint,
        int imageWidth,
        int imageHeight,
        bool fromCenter)
    {
        int minSize = GetMinimumCropSize(imageWidth, imageHeight);

        return fromCenter
            ? ResizeFromCenter(
                mode,
                startCrop,
                startImagePoint,
                currentImagePoint,
                imageWidth,
                imageHeight,
                minSize)
            : mode switch
            {
                ManualCropDragMode.ResizeTopLeft => ResizeFromTopLeft(startCrop, currentImagePoint, minSize),
                ManualCropDragMode.ResizeTopRight => ResizeFromTopRight(startCrop, currentImagePoint, imageWidth, minSize),
                ManualCropDragMode.ResizeBottomLeft => ResizeFromBottomLeft(startCrop, currentImagePoint, imageHeight, minSize),
                ManualCropDragMode.ResizeBottomRight => ResizeFromBottomRight(startCrop, currentImagePoint, imageWidth, imageHeight, minSize),
                ManualCropDragMode.ResizeLeft => ResizeFromLeft(startCrop, currentImagePoint, imageHeight, minSize),
                ManualCropDragMode.ResizeRight => ResizeFromRight(startCrop, currentImagePoint, imageWidth, imageHeight, minSize),
                ManualCropDragMode.ResizeTop => ResizeFromTop(startCrop, currentImagePoint, imageWidth, minSize),
                ManualCropDragMode.ResizeBottom => ResizeFromBottom(startCrop, currentImagePoint, imageWidth, imageHeight, minSize),
                _ => startCrop
            };
    }

    public static Point AdjustDragPoint(Point startPoint, Point currentPoint, double factor)
    {
        return new Point(
            startPoint.X + (currentPoint.X - startPoint.X) * factor,
            startPoint.Y + (currentPoint.Y - startPoint.Y) * factor);
    }

    public static Point PreviewToImagePoint(
        Point point,
        double previewLeft,
        double previewTop,
        double previewScaleX,
        double previewScaleY)
    {
        return new Point(
            (point.X - previewLeft) / previewScaleX,
            (point.Y - previewTop) / previewScaleY);
    }

    public static ManualCropDragMode HitTest(Point point, Rect cropRect)
    {
        if (IsNearPoint(point, cropRect.Left, cropRect.Top))
        {
            return ManualCropDragMode.ResizeTopLeft;
        }

        if (IsNearPoint(point, cropRect.Right, cropRect.Top))
        {
            return ManualCropDragMode.ResizeTopRight;
        }

        if (IsNearPoint(point, cropRect.Left, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeBottomLeft;
        }

        if (IsNearPoint(point, cropRect.Right, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeBottomRight;
        }

        if (IsNearVerticalSide(point, cropRect.Left, cropRect.Top, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeLeft;
        }

        if (IsNearVerticalSide(point, cropRect.Right, cropRect.Top, cropRect.Bottom))
        {
            return ManualCropDragMode.ResizeRight;
        }

        if (IsNearHorizontalSide(point, cropRect.Top, cropRect.Left, cropRect.Right))
        {
            return ManualCropDragMode.ResizeTop;
        }

        if (IsNearHorizontalSide(point, cropRect.Bottom, cropRect.Left, cropRect.Right))
        {
            return ManualCropDragMode.ResizeBottom;
        }

        return cropRect.Contains(point) ? ManualCropDragMode.Move : ManualCropDragMode.None;
    }

    private static ManualCrop ResizeFromCenter(
        ManualCropDragMode mode,
        ManualCrop startCrop,
        Point startImagePoint,
        Point currentImagePoint,
        int imageWidth,
        int imageHeight,
        int minSize)
    {
        double centerX = startCrop.X + startCrop.Size / 2.0;
        double centerY = startCrop.Y + startCrop.Size / 2.0;
        double deltaX = currentImagePoint.X - startImagePoint.X;
        double deltaY = currentImagePoint.Y - startImagePoint.Y;

        double rawSize = mode switch
        {
            ManualCropDragMode.ResizeTopLeft => startCrop.Size + 2.0 * Math.Min(-deltaX, -deltaY),
            ManualCropDragMode.ResizeTopRight => startCrop.Size + 2.0 * Math.Min(deltaX, -deltaY),
            ManualCropDragMode.ResizeBottomLeft => startCrop.Size + 2.0 * Math.Min(-deltaX, deltaY),
            ManualCropDragMode.ResizeBottomRight => startCrop.Size + 2.0 * Math.Min(deltaX, deltaY),
            ManualCropDragMode.ResizeLeft => startCrop.Size - 2.0 * deltaX,
            ManualCropDragMode.ResizeRight => startCrop.Size + 2.0 * deltaX,
            ManualCropDragMode.ResizeTop => startCrop.Size - 2.0 * deltaY,
            ManualCropDragMode.ResizeBottom => startCrop.Size + 2.0 * deltaY,
            _ => startCrop.Size
        };

        double maxHalfSize = Math.Min(
            Math.Min(centerX, imageWidth - centerX),
            Math.Min(centerY, imageHeight - centerY));
        int maxSize = Math.Max(1, (int)Math.Floor(maxHalfSize * 2.0));
        int size = GetClampedCenteredSize(rawSize, minSize, maxSize, startCrop.Size);

        return new ManualCrop(
            (int)Math.Round(centerX - size / 2.0),
            (int)Math.Round(centerY - size / 2.0),
            size);
    }

    private static ManualCrop ResizeFromTopLeft(ManualCrop startCrop, Point point, int minSize)
    {
        int anchorX = startCrop.X + startCrop.Size;
        int anchorY = startCrop.Y + startCrop.Size;
        int maxSize = Math.Min(anchorX, anchorY);
        int size = GetClampedSize(Math.Min(anchorX - point.X, anchorY - point.Y), minSize, maxSize);
        return new ManualCrop(anchorX - size, anchorY - size, size);
    }

    private static ManualCrop ResizeFromTopRight(
        ManualCrop startCrop,
        Point point,
        int imageWidth,
        int minSize)
    {
        int anchorX = startCrop.X;
        int anchorY = startCrop.Y + startCrop.Size;
        int maxSize = Math.Min(imageWidth - anchorX, anchorY);
        int size = GetClampedSize(Math.Min(point.X - anchorX, anchorY - point.Y), minSize, maxSize);
        return new ManualCrop(anchorX, anchorY - size, size);
    }

    private static ManualCrop ResizeFromBottomLeft(
        ManualCrop startCrop,
        Point point,
        int imageHeight,
        int minSize)
    {
        int anchorX = startCrop.X + startCrop.Size;
        int anchorY = startCrop.Y;
        int maxSize = Math.Min(anchorX, imageHeight - anchorY);
        int size = GetClampedSize(Math.Min(anchorX - point.X, point.Y - anchorY), minSize, maxSize);
        return new ManualCrop(anchorX - size, anchorY, size);
    }

    private static ManualCrop ResizeFromBottomRight(
        ManualCrop startCrop,
        Point point,
        int imageWidth,
        int imageHeight,
        int minSize)
    {
        int anchorX = startCrop.X;
        int anchorY = startCrop.Y;
        int maxSize = Math.Min(imageWidth - anchorX, imageHeight - anchorY);
        int size = GetClampedSize(Math.Min(point.X - anchorX, point.Y - anchorY), minSize, maxSize);
        return new ManualCrop(anchorX, anchorY, size);
    }

    private static ManualCrop ResizeFromLeft(
        ManualCrop startCrop,
        Point point,
        int imageHeight,
        int minSize)
    {
        int right = startCrop.X + startCrop.Size;
        int centerY = startCrop.Y + startCrop.Size / 2;
        int maxSize = Math.Min(right, Math.Min(centerY * 2, (imageHeight - centerY) * 2));
        int size = GetClampedSize(right - point.X, minSize, maxSize);
        return new ManualCrop(
            right - size,
            ClampCoordinate(centerY - size / 2, imageHeight - size),
            size);
    }

    private static ManualCrop ResizeFromRight(
        ManualCrop startCrop,
        Point point,
        int imageWidth,
        int imageHeight,
        int minSize)
    {
        int left = startCrop.X;
        int centerY = startCrop.Y + startCrop.Size / 2;
        int maxSize = Math.Min(imageWidth - left, Math.Min(centerY * 2, (imageHeight - centerY) * 2));
        int size = GetClampedSize(point.X - left, minSize, maxSize);
        return new ManualCrop(
            left,
            ClampCoordinate(centerY - size / 2, imageHeight - size),
            size);
    }

    private static ManualCrop ResizeFromTop(
        ManualCrop startCrop,
        Point point,
        int imageWidth,
        int minSize)
    {
        int bottom = startCrop.Y + startCrop.Size;
        int centerX = startCrop.X + startCrop.Size / 2;
        int maxSize = Math.Min(bottom, Math.Min(centerX * 2, (imageWidth - centerX) * 2));
        int size = GetClampedSize(bottom - point.Y, minSize, maxSize);
        return new ManualCrop(
            ClampCoordinate(centerX - size / 2, imageWidth - size),
            bottom - size,
            size);
    }

    private static ManualCrop ResizeFromBottom(
        ManualCrop startCrop,
        Point point,
        int imageWidth,
        int imageHeight,
        int minSize)
    {
        int top = startCrop.Y;
        int centerX = startCrop.X + startCrop.Size / 2;
        int maxSize = Math.Min(imageHeight - top, Math.Min(centerX * 2, (imageWidth - centerX) * 2));
        int size = GetClampedSize(point.Y - top, minSize, maxSize);
        return new ManualCrop(
            ClampCoordinate(centerX - size / 2, imageWidth - size),
            top,
            size);
    }

    private static int GetMinimumCropSize(int imageWidth, int imageHeight)
    {
        int maxCropSize = Math.Min(imageWidth, imageHeight);
        return Math.Min(MinimumCropSize, Math.Max(1, maxCropSize));
    }

    private static int GetClampedSize(double rawSize, int minSize, int maxSize)
    {
        maxSize = Math.Max(1, maxSize);
        minSize = Math.Clamp(minSize, 1, maxSize);
        int size = (int)Math.Round(rawSize);
        return Math.Clamp(size, minSize, maxSize);
    }

    private static int GetClampedCenteredSize(double rawSize, int minSize, int maxSize, int startSize)
    {
        int parity = Math.Abs(startSize) % 2;
        maxSize = Math.Max(1, maxSize);

        if (maxSize % 2 != parity)
        {
            maxSize--;
        }

        maxSize = Math.Max(1, maxSize);
        minSize = Math.Clamp(minSize, 1, maxSize);

        if (minSize % 2 != parity)
        {
            minSize++;
        }

        if (minSize > maxSize)
        {
            minSize = maxSize;
        }

        int size = 2 * (int)Math.Round((rawSize - parity) / 2.0, MidpointRounding.AwayFromZero) + parity;
        return Math.Clamp(size, minSize, maxSize);
    }

    private static int ClampCoordinate(int value, int maxValue)
    {
        return Math.Clamp(value, 0, Math.Max(0, maxValue));
    }

    private static bool IsNearPoint(Point point, double x, double y)
    {
        return Math.Abs(point.X - x) <= CornerHitSize &&
               Math.Abs(point.Y - y) <= CornerHitSize;
    }

    private static bool IsNearVerticalSide(Point point, double sideX, double top, double bottom)
    {
        double sideStart = top + CornerHitSize;
        double sideEnd = bottom - CornerHitSize;

        if (sideStart > sideEnd)
        {
            sideStart = top;
            sideEnd = bottom;
        }

        return Math.Abs(point.X - sideX) <= SideHitSize &&
               point.Y >= sideStart &&
               point.Y <= sideEnd;
    }

    private static bool IsNearHorizontalSide(Point point, double sideY, double left, double right)
    {
        double sideStart = left + CornerHitSize;
        double sideEnd = right - CornerHitSize;

        if (sideStart > sideEnd)
        {
            sideStart = left;
            sideEnd = right;
        }

        return Math.Abs(point.Y - sideY) <= SideHitSize &&
               point.X >= sideStart &&
               point.X <= sideEnd;
    }
}
