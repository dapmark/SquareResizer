using System;
using System.Collections.Generic;
using System.IO;
using ImageMagick;

namespace ImageSquareResizer;

internal sealed class ProcessResult
{
    public required string SourcePath { get; init; }
    public string? OutputPath { get; init; }
    public bool Success { get; init; }
    public bool AlreadyCorrectSize { get; init; }
    public string? ErrorMessage { get; init; }
    public int? TargetSize { get; init; }
}

internal static class ImageProcessor
{
    private const double MaxSmartPaddingPercent = 0.025;
    private const int MinSmartPaddingDiff = 12;
    private const int MaxSmartPaddingDiff = 32;
    private const int EdgeSampleInset = 3;
    private const int MaxBackgroundChannelSpread = 24;

    private static readonly int[] MusicCoverSizes = { 1400, 1200, 1000, 700, 600, 500 };

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff"
    };

    private static readonly HashSet<string> JpegExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg"
    };

    private readonly record struct RgbSample(int R, int G, int B);

    public static List<ProcessResult> ProcessFiles(IEnumerable<string> paths, int quality, string resizeMode)
    {
        return ProcessFiles(
            paths,
            quality,
            resizeMode,
            AppSettings.DefaultSmartMode,
            AppSettings.DefaultSharpMode);
    }

    public static List<ProcessResult> ProcessFiles(
        IEnumerable<string> paths,
        int quality,
        string resizeMode,
        bool smartMode)
    {
        return ProcessFiles(
            paths,
            quality,
            resizeMode,
            smartMode,
            AppSettings.DefaultSharpMode);
    }

    public static List<ProcessResult> ProcessFiles(
        IEnumerable<string> paths,
        int quality,
        string resizeMode,
        bool smartMode,
        string sharpMode)
    {
        var results = new List<ProcessResult>();

        foreach (string path in paths)
        {
            results.Add(ProcessFile(path, quality, resizeMode, smartMode, sharpMode));
        }

        return results;
    }

    public static ProcessResult ProcessFile(string sourcePath, int quality, string resizeMode)
    {
        return ProcessFile(
            sourcePath,
            quality,
            resizeMode,
            AppSettings.DefaultSmartMode,
            AppSettings.DefaultSharpMode);
    }

    public static ProcessResult ProcessFile(
        string sourcePath,
        int quality,
        string resizeMode,
        bool smartMode)
    {
        return ProcessFile(
            sourcePath,
            quality,
            resizeMode,
            smartMode,
            AppSettings.DefaultSharpMode);
    }

    public static ProcessResult ProcessFile(
        string sourcePath,
        int quality,
        string resizeMode,
        bool smartMode,
        string sharpMode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Fail(sourcePath, "Пустой путь к файлу.");
            }

            if (!File.Exists(sourcePath))
            {
                return Fail(sourcePath, "Файл не найден.");
            }

            string extension = Path.GetExtension(sourcePath);

            if (!SupportedExtensions.Contains(extension))
            {
                return Fail(sourcePath, $"Неподдерживаемый формат: {extension}");
            }

            quality = AppSettings.NormalizeQuality(quality);
            resizeMode = AppSettings.NormalizeResizeMode(resizeMode);
            sharpMode = AppSettings.NormalizeSharpMode(sharpMode);

            using var image = new MagickImage(sourcePath);

            image.AutoOrient();
            image.FilterType = FilterType.Lanczos;

            int originalWidth = (int)image.Width;
            int originalHeight = (int)image.Height;

            int targetSize;
            bool resized = false;

            if (smartMode && TryApplySmartPadding(image, originalWidth, originalHeight, out int paddedSquareSize))
            {
                targetSize = GetTargetSizeFromSquareSize(paddedSquareSize, resizeMode);

                if ((int)image.Width != targetSize || (int)image.Height != targetSize)
                {
                    ResizeSquare(image, targetSize);
                    resized = true;
                }
            }
            else
            {
                targetSize = GetTargetSizeForStretch(originalWidth, originalHeight, resizeMode);

                if (originalWidth != targetSize || originalHeight != targetSize)
                {
                    ResizeWithAspectRatioIgnored(image, targetSize);
                    resized = true;
                }
            }

            ApplySharpnessIfNeeded(image, resized, sharpMode);

            bool dimensionsAlreadyCorrect =
                (int)image.Width == originalWidth &&
                (int)image.Height == originalHeight &&
                originalWidth == targetSize &&
                originalHeight == targetSize;

            if (dimensionsAlreadyCorrect && IsJpegExtension(extension))
            {
                return new ProcessResult
                {
                    SourcePath = sourcePath,
                    Success = true,
                    AlreadyCorrectSize = true,
                    TargetSize = targetSize
                };
            }

            ApplyJpegOutputSettings(image, quality);

            string outputPath = CreateUniqueOutputPath(sourcePath, targetSize);
            image.Write(outputPath);

            return new ProcessResult
            {
                SourcePath = sourcePath,
                OutputPath = outputPath,
                Success = true,
                TargetSize = targetSize
            };
        }
        catch (Exception ex)
        {
            return Fail(sourcePath, ex.Message);
        }
    }

    public static ProcessResult ProcessManualCropFile(
        string sourcePath,
        int quality,
        string resizeMode,
        int cropX,
        int cropY,
        int cropSize)
    {
        return ProcessManualCropFile(
            sourcePath,
            quality,
            resizeMode,
            AppSettings.DefaultSharpMode,
            cropX,
            cropY,
            cropSize);
    }

    public static ProcessResult ProcessManualCropFile(
        string sourcePath,
        int quality,
        string resizeMode,
        string sharpMode,
        int cropX,
        int cropY,
        int cropSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
            {
                return Fail(sourcePath, "Пустой путь к файлу.");
            }

            if (!File.Exists(sourcePath))
            {
                return Fail(sourcePath, "Файл не найден.");
            }

            string extension = Path.GetExtension(sourcePath);

            if (!SupportedExtensions.Contains(extension))
            {
                return Fail(sourcePath, $"Неподдерживаемый формат: {extension}");
            }

            quality = AppSettings.NormalizeQuality(quality);
            resizeMode = AppSettings.NormalizeResizeMode(resizeMode);
            sharpMode = AppSettings.NormalizeSharpMode(sharpMode);

            using var image = new MagickImage(sourcePath);

            image.AutoOrient();
            image.FilterType = FilterType.Lanczos;

            int imageWidth = (int)image.Width;
            int imageHeight = (int)image.Height;
            int maxCropSize = Math.Min(imageWidth, imageHeight);

            if (maxCropSize <= 0)
            {
                return Fail(sourcePath, "Некорректный размер изображения.");
            }

            cropSize = Math.Clamp(cropSize, 1, maxCropSize);
            cropX = Math.Clamp(cropX, 0, imageWidth - cropSize);
            cropY = Math.Clamp(cropY, 0, imageHeight - cropSize);

            CropToSquare(image, cropX, cropY, cropSize);

            int targetSize = GetTargetSizeFromSquareSize(cropSize, resizeMode);
            bool resized = false;

            if ((int)image.Width != targetSize || (int)image.Height != targetSize)
            {
                ResizeSquare(image, targetSize);
                resized = true;
            }

            ApplySharpnessIfNeeded(image, resized, sharpMode);

            bool dimensionsAlreadyCorrect =
                imageWidth == cropSize &&
                imageHeight == cropSize &&
                imageWidth == targetSize &&
                imageHeight == targetSize;

            if (dimensionsAlreadyCorrect && IsJpegExtension(extension))
            {
                return new ProcessResult
                {
                    SourcePath = sourcePath,
                    Success = true,
                    AlreadyCorrectSize = true,
                    TargetSize = targetSize
                };
            }

            ApplyJpegOutputSettings(image, quality);

            string outputPath = CreateUniqueOutputPath(sourcePath, targetSize);
            image.Write(outputPath);

            return new ProcessResult
            {
                SourcePath = sourcePath,
                OutputPath = outputPath,
                Success = true,
                TargetSize = targetSize
            };
        }
        catch (Exception ex)
        {
            return Fail(sourcePath, ex.Message);
        }
    }

    private static bool TryApplySmartPadding(
        MagickImage image,
        int width,
        int height,
        out int paddedSquareSize)
    {
        paddedSquareSize = Math.Max(width, height);

        if (width == height)
        {
            return false;
        }

        if (!TryGetSafePaddingColor(image, width, height, out MagickColor backgroundColor))
        {
            return false;
        }

        image.BackgroundColor = backgroundColor;
        image.Extent((uint)paddedSquareSize, (uint)paddedSquareSize, Gravity.Center);

        return true;
    }

    private static bool TryGetSafePaddingColor(
        MagickImage image,
        int width,
        int height,
        out MagickColor backgroundColor)
    {
        backgroundColor = MagickColor.FromRgb(0, 0, 0);

        int maxSide = Math.Max(width, height);
        int diff = Math.Abs(width - height);
        int maxAllowedDiff = GetMaxAllowedSmartPaddingDiff(maxSide);

        if (diff == 0 || diff > maxAllowedDiff)
        {
            return false;
        }

        List<RgbSample> samples = GetEdgeSamples(image, width, height);

        if (!AreSamplesCloseEnough(samples))
        {
            return false;
        }

        backgroundColor = GetAverageColor(samples);
        return true;
    }

    private static int GetMaxAllowedSmartPaddingDiff(int maxSide)
    {
        int byPercent = (int)Math.Round(maxSide * MaxSmartPaddingPercent);
        return Math.Clamp(byPercent, MinSmartPaddingDiff, MaxSmartPaddingDiff);
    }

    private static List<RgbSample> GetEdgeSamples(MagickImage image, int width, int height)
    {
        int xInset = Math.Min(EdgeSampleInset, Math.Max(0, (width - 1) / 2));
        int yInset = Math.Min(EdgeSampleInset, Math.Max(0, (height - 1) / 2));

        int left = xInset;
        int right = width - 1 - xInset;
        int top = yInset;
        int bottom = height - 1 - yInset;
        int middleX = width / 2;
        int middleY = height / 2;

        return new List<RgbSample>
        {
            GetPixelColor(image, left, top),
            GetPixelColor(image, right, top),
            GetPixelColor(image, left, bottom),
            GetPixelColor(image, right, bottom),
            GetPixelColor(image, middleX, top),
            GetPixelColor(image, middleX, bottom),
            GetPixelColor(image, left, middleY),
            GetPixelColor(image, right, middleY)
        };
    }

    private static RgbSample GetPixelColor(MagickImage image, int x, int y)
    {
        var pixels = image.GetPixels();
        IMagickColor<byte>? color = pixels.GetPixel(x, y).ToColor();

        if (color == null)
        {
            return new RgbSample(0, 0, 0);
        }

        return new RgbSample(color.R, color.G, color.B);
    }

    private static bool AreSamplesCloseEnough(List<RgbSample> colors)
    {
        if (colors.Count == 0)
        {
            return false;
        }

        int minR = 255;
        int minG = 255;
        int minB = 255;

        int maxR = 0;
        int maxG = 0;
        int maxB = 0;

        foreach (RgbSample color in colors)
        {
            minR = Math.Min(minR, color.R);
            minG = Math.Min(minG, color.G);
            minB = Math.Min(minB, color.B);

            maxR = Math.Max(maxR, color.R);
            maxG = Math.Max(maxG, color.G);
            maxB = Math.Max(maxB, color.B);
        }

        return maxR - minR <= MaxBackgroundChannelSpread &&
               maxG - minG <= MaxBackgroundChannelSpread &&
               maxB - minB <= MaxBackgroundChannelSpread;
    }

    private static MagickColor GetAverageColor(List<RgbSample> colors)
    {
        int totalR = 0;
        int totalG = 0;
        int totalB = 0;

        foreach (RgbSample color in colors)
        {
            totalR += color.R;
            totalG += color.G;
            totalB += color.B;
        }

        int count = Math.Max(1, colors.Count);

        byte r = (byte)Math.Clamp((int)Math.Round((double)totalR / count), 0, 255);
        byte g = (byte)Math.Clamp((int)Math.Round((double)totalG / count), 0, 255);
        byte b = (byte)Math.Clamp((int)Math.Round((double)totalB / count), 0, 255);

        return MagickColor.FromRgb(r, g, b);
    }

    private static void CropToSquare(MagickImage image, int cropX, int cropY, int cropSize)
    {
        var geometry = new MagickGeometry((uint)cropSize, (uint)cropSize)
        {
            X = cropX,
            Y = cropY
        };

        image.Crop(geometry);
    }

    private static void ResizeSquare(MagickImage image, int targetSize)
    {
        var geometry = new MagickGeometry((uint)targetSize, (uint)targetSize);
        image.Resize(geometry);
    }

    private static void ResizeWithAspectRatioIgnored(MagickImage image, int targetSize)
    {
        var geometry = new MagickGeometry((uint)targetSize, (uint)targetSize)
        {
            IgnoreAspectRatio = true
        };

        image.Resize(geometry);
    }

    private static void ApplySharpnessIfNeeded(MagickImage image, bool resized, string sharpMode)
    {
        if (!resized)
        {
            return;
        }

        double sigma = GetSharpSigma(sharpMode);

        if (sigma <= 0)
        {
            return;
        }

        image.AdaptiveSharpen(0.0, sigma);
    }

    private static double GetSharpSigma(string sharpMode)
    {
        sharpMode = AppSettings.NormalizeSharpMode(sharpMode);

        return sharpMode switch
        {
            "increased" => 0.50,
            "high" => 0.75,
            "maximum" => 1.00,
            _ => 0.0
        };
    }

    private static int GetTargetSizeForStretch(int width, int height, string resizeMode)
    {
        int minSide = Math.Min(width, height);
        return GetTargetSizeFromSquareSize(minSide, resizeMode);
    }

    private static int GetTargetSizeFromSquareSize(int squareSize, string resizeMode)
    {
        if (!string.Equals(resizeMode, "music_cover", StringComparison.OrdinalIgnoreCase))
        {
            return squareSize;
        }

        return GetNearestMusicCoverSize(squareSize);
    }

    private static int GetNearestMusicCoverSize(int size)
    {
        int bestSize = MusicCoverSizes[0];
        int bestDiff = Math.Abs(size - bestSize);

        foreach (int candidate in MusicCoverSizes)
        {
            int diff = Math.Abs(size - candidate);

            if (diff < bestDiff || diff == bestDiff && candidate < bestSize)
            {
                bestSize = candidate;
                bestDiff = diff;
            }
        }

        return bestSize;
    }

    private static void ApplyJpegOutputSettings(MagickImage image, int quality)
    {
        image.BackgroundColor = MagickColors.White;
        image.Alpha(AlphaOption.Remove);
        image.Format = MagickFormat.Jpeg;
        image.Quality = (uint)quality;
    }

    private static bool IsJpegExtension(string extension)
    {
        return JpegExtensions.Contains(extension);
    }

    private static string CreateUniqueOutputPath(string sourcePath, int targetSize)
    {
        const string outputExtension = ".jpg";

        string directory = Path.GetDirectoryName(sourcePath) ?? "";
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);

        string baseOutputPath = Path.Combine(
            directory,
            $"{fileNameWithoutExtension}_{targetSize}x{targetSize}{outputExtension}");

        if (!File.Exists(baseOutputPath))
        {
            return baseOutputPath;
        }

        int counter = 2;

        while (true)
        {
            string numberedOutputPath = Path.Combine(
                directory,
                $"{fileNameWithoutExtension}_{targetSize}x{targetSize}_{counter}{outputExtension}");

            if (!File.Exists(numberedOutputPath))
            {
                return numberedOutputPath;
            }

            counter++;
        }
    }

    private static ProcessResult Fail(string sourcePath, string message)
    {
        return new ProcessResult
        {
            SourcePath = sourcePath,
            Success = false,
            ErrorMessage = message
        };
    }
}