using System;

namespace ImageSquareResizer;

internal sealed class Localization
{
    private static readonly Localization English = new("en");
    private static readonly Localization Russian = new("ru");

    private Localization(string language)
    {
        Language = language;
    }

    public string Language { get; }

    public bool IsRussian =>
        string.Equals(Language, "ru", StringComparison.OrdinalIgnoreCase);

    public static Localization For(string? language)
    {
        return string.Equals(AppSettings.NormalizeLanguage(language), "ru", StringComparison.OrdinalIgnoreCase)
            ? Russian
            : English;
    }

    public string ResizeModeLabel => IsRussian ? "Размер" : "Size";
    public string ResizeAuto => IsRussian ? "Авто" : "Auto";
    public string ResizeMusicCover => IsRussian ? "Обложка" : "Cover";
    public string ResizeModeToolTip => IsRussian
        ? "Авто — ближайшая сотня по изображению. Обложка — ближайший стандарт: 1400, 1200, 1000, 700, 600, 500."
        : "Auto — nearest 100 px image-based size. Cover — nearest standard size: 1400, 1200, 1000, 700, 600, 500.";

    public string QualityLabel => IsRussian ? "Качество" : "Quality";

    public string SharpModeLabel => IsRussian ? "Резкость" : "Sharp";
    public string SharpStandard => IsRussian ? "Стандартная" : "Standard";
    public string SharpIncreased => IsRussian ? "Повышенная" : "Increased";
    public string SharpHigh => IsRussian ? "Высокая" : "High";
    public string SharpMaximum => IsRussian ? "Максимальная" : "Maximum";
    public string SharpModeToolTip => IsRussian
        ? "Добавит дополнительную резкость после изменения размера"
        : "Adds extra sharpness after resizing";

    public string SmartMode => IsRussian ? "Умный режим" : "Smart mode";
    public string SmartModeToolTip => IsRussian
        ? "Программа попробует дорисовать недостающую сторону цветом фона, в противном случае используется обычное сжатие/растяжение до квадрата"
        : "Tries to fill the missing side with a background color; otherwise uses regular square resizing";

    public string ManualMode => IsRussian ? "Ручной режим" : "Manual mode";
    public string ManualModeToolTip => IsRussian
        ? "Изображение можно будет открыть в предпросмотре, кроп-рамку можно двигать мышью или стрелками"
        : "Opens the image in preview; the crop frame can be moved with the mouse or arrow keys";

    public string OpenFileButton => IsRussian ? "Открыть файл" : "Open file";
    public string SaveButton => IsRussian ? "Сохранить" : "Save";
    public string CenterCropButtonToolTip => IsRussian ? "Центрировать рамку" : "Center crop frame";
    public string SettingsButtonToolTip => IsRussian ? "Настройки" : "Settings";

    public string OpenDialogTitle => IsRussian ? "Выберите изображение" : "Select image";
    public string OpenDialogFilter => IsRussian
        ? "Изображения|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.tif;*.tiff|Все файлы|*.*"
        : "Images|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.tif;*.tiff|All files|*.*";

    public string ManualSingleFileMessage => IsRussian
        ? "В ручном режиме выберите один файл."
        : "Select one file in manual mode.";

    public string ManualModeTitle => IsRussian ? "Ручной режим" : "Manual mode";

    public string InvalidQualityMessage => IsRussian
        ? "Введите число качества от 1 до 100."
        : "Enter a quality value from 1 to 100.";

    public string InvalidValueTitle => IsRussian ? "Некорректное значение" : "Invalid value";

    public string ProcessingStatus => IsRussian ? "Обработка..." : "Processing...";
    public string SavingStatus => IsRussian ? "Сохранение..." : "Saving...";

    public string UnknownError => IsRussian ? "Неизвестная ошибка." : "Unknown error.";
    public string ProcessingErrorTitle => IsRussian ? "Ошибка обработки" : "Processing error";
    public string OpenErrorTitle => IsRussian ? "Ошибка открытия" : "Open error";
    public string OpenImageErrorTitle => IsRussian ? "Ошибка открытия изображения" : "Image open error";
    public string SaveErrorTitle => IsRussian ? "Ошибка сохранения" : "Save error";
    public string SaveErrorStatus => IsRussian ? "Ошибка сохранения." : "Save error.";

    public string DoneStatus => IsRussian ? "Готово." : "Done.";

    public string DoneWithFile(string fileName) =>
        IsRussian ? $"Готово: {fileName}" : $"Done: {fileName}";

    public string CreatedFilesStatus(int count) =>
        IsRussian ? $"Готово. Создано файлов: {count}" : $"Done. Created files: {count}";

    public string FileAlreadyCorrectSize => IsRussian
        ? "Файл уже нужного размера."
        : "The file already has the required size.";

    public string ProcessingSummary(int created, int alreadyCorrect, int failed) =>
        IsRussian
            ? $"Создано: {created}, уже готово: {alreadyCorrect}, ошибок: {failed}"
            : $"Created: {created}, already ready: {alreadyCorrect}, errors: {failed}";

    public string ManualPreviewStatus => IsRussian
        ? "Ручной режим: настройте квадрат и нажмите «Сохранить»."
        : "Manual mode: adjust the square and click Save.";

    public string EmptyPath => IsRussian ? "Пустой путь к файлу." : "Empty file path.";
    public string FileNotFound => IsRussian ? "Файл не найден." : "File not found.";

    public string UnsupportedFormat(string extension) =>
        IsRussian ? $"Неподдерживаемый формат: {extension}" : $"Unsupported format: {extension}";

    public string InvalidImageSize => IsRussian
        ? "Некорректный размер изображения."
        : "Invalid image size.";

    public string StartupErrorTitle(string productName) =>
        IsRussian ? "Ошибка запуска " + productName : productName + " startup error";
}
