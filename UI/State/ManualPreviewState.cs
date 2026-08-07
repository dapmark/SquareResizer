using System.Threading;
using System.Windows;

namespace ImageSquareResizer;

internal sealed class ManualPreviewState
{
    public bool IsLoaded { get; set; }

    public bool IsDragging { get; set; }

    public ManualCropDragMode DragMode { get; set; } = ManualCropDragMode.None;

    public string? SourcePath { get; set; }

    public int ImageWidth { get; set; }

    public int ImageHeight { get; set; }

    public int CropSize { get; set; }

    public int CropX { get; set; }

    public int CropY { get; set; }

    public ManualResultState? SavedResultState { get; set; }

    public double PreviewLeft { get; set; }

    public double PreviewTop { get; set; }

    public double PreviewScaleX { get; set; } = 1.0;

    public double PreviewScaleY { get; set; } = 1.0;

    public Point DragStartPoint { get; set; }

    public int DragStartCropX { get; set; }

    public int DragStartCropY { get; set; }

    public int DragStartCropSize { get; set; }

    public SemaphoreSlim FileSizeEstimateGate { get; } = new(1, 1);

    public CancellationTokenSource? FileSizeEstimateCancellation { get; set; }

    public long? EstimatedFileSizeBytes { get; set; }

    public bool IsFileSizeEstimatePending { get; set; }

    public int FileSizeEstimateRequestId { get; set; }

    public void ResetPreview()
    {
        IsLoaded = false;
        IsDragging = false;
        DragMode = ManualCropDragMode.None;
        SourcePath = null;
        ImageWidth = 0;
        ImageHeight = 0;
        CropSize = 0;
        CropX = 0;
        CropY = 0;
        SavedResultState = null;
        PreviewLeft = 0;
        PreviewTop = 0;
        PreviewScaleX = 1.0;
        PreviewScaleY = 1.0;
        DragStartPoint = default;
        DragStartCropX = 0;
        DragStartCropY = 0;
        DragStartCropSize = 0;
    }
}
