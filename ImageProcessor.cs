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
    private const int SmallTargetSize = 1000;
    private const int LargeTargetSize = 1200;
    private const double LargeTargetThreshold = 1170.0;

    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".tif", ".tiff"
    };

    public static List<ProcessResult> ProcessFiles(IEnumerable<string> paths, int quality)
    {
        var results = new List<ProcessResult>();

        foreach (string path in paths)
        {
            results.Add(ProcessFile(path, quality));
        }

        return results;
    }

    public static ProcessResult ProcessFile(string sourcePath, int quality)
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

            using var image = new MagickImage(sourcePath);

            image.AutoOrient();

            int width = (int)image.Width;
            int height = (int)image.Height;
            int targetSize = GetTargetSize(width, height);

            if (width == targetSize && height == targetSize)
            {
                return new ProcessResult
                {
                    SourcePath = sourcePath,
                    Success = true,
                    AlreadyCorrectSize = true,
                    TargetSize = targetSize
                };
            }

            image.FilterType = FilterType.Lanczos;

            var geometry = new MagickGeometry((uint)targetSize, (uint)targetSize)
            {
                IgnoreAspectRatio = true
            };

            image.Resize(geometry);

            ApplyOutputSettings(image, extension, quality);

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

    private static int GetTargetSize(int width, int height)
    {
        double averageSize = (width + height) / 2.0;
        return averageSize >= LargeTargetThreshold ? LargeTargetSize : SmallTargetSize;
    }

    private static void ApplyOutputSettings(MagickImage image, string extension, int quality)
    {
        switch (extension.ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
                image.Format = MagickFormat.Jpeg;
                image.Quality = (uint)quality;
                break;

            case ".webp":
                image.Format = MagickFormat.WebP;
                image.Quality = (uint)quality;
                break;

            case ".png":
                image.Format = MagickFormat.Png;
                image.Settings.Compression = CompressionMethod.Zip;
                break;

            case ".bmp":
                image.Format = MagickFormat.Bmp;
                break;

            case ".tif":
            case ".tiff":
                image.Format = MagickFormat.Tiff;
                break;
        }
    }

    private static string CreateUniqueOutputPath(string sourcePath, int targetSize)
    {
        string directory = Path.GetDirectoryName(sourcePath) ?? "";
        string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(sourcePath);
        string extension = Path.GetExtension(sourcePath);

        string baseOutputPath = Path.Combine(
            directory,
            $"{fileNameWithoutExtension}_{targetSize}x{targetSize}{extension}");

        if (!File.Exists(baseOutputPath))
        {
            return baseOutputPath;
        }

        int counter = 2;

        while (true)
        {
            string numberedOutputPath = Path.Combine(
                directory,
                $"{fileNameWithoutExtension}_{targetSize}x{targetSize}_{counter}{extension}");

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
