namespace ImageSquareResizer;

internal readonly record struct ManualResultState(
    string SourcePath,
    int CropX,
    int CropY,
    int CropSize,
    int Quality,
    string ResizeMode,
    string SharpMode,
    int JpegMode,
    int AutoSizeStep,
    bool OnlineServiceCompatibility);
