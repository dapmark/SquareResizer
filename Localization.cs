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
    public string ResizeAutoToolTip => IsRussian
        ? "Подбирает размер по изображению и округляет его выбранным шагом"
        : "Chooses a size from the image and rounds it by the selected step";

    public string ResizeMusicCoverToolTip => IsRussian
        ? "Подбирает ближайший стандартный размер обложки: 500, 600, 700, 1000, 1200, 1400"
        : "Chooses the nearest standard cover size: 500, 600, 700, 1000, 1200, 1400";

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
        ? "Дорисует фон к короткой стороне, если разница сторон укладывается в лимиты"
        : "Adds background to the short side if the side difference fits the limits";

    public string ManualMode => IsRussian ? "Ручной режим" : "Manual mode";
    public string ManualModeToolTip => IsRussian
        ? "Позволяет вручную выбрать область изображения"
        : "Lets you choose the image area manually";

    public string OpenFileButton => IsRussian ? "Открыть файл" : "Open file";
    public string SelectFileButton => IsRussian ? "Выберите файл" : "Select file";
    public string DropOrText => IsRussian ? "или" : "or";
    public string DropHereText => IsRussian ? "перенесите его сюда" : "drop it here";
    public string SaveButton => IsRussian ? "Сохранить" : "Save";
    public string CloseFileMenuItem => IsRussian ? "Закрыть файл" : "Close file";
    public string CenterCropButtonToolTip => IsRussian ? "Центрировать рамку" : "Center crop frame";
    public string SettingsButtonToolTip => IsRussian ? "Настройки" : "Settings";
    public string AdvancedSettingsButtonText => IsRussian ? "Дополнительно" : "Advanced";

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
    public string SaveErrorStatus => IsRussian ? "Ошибка сохранения" : "Save error";

    public string DoneStatus => IsRussian ? "Готово" : "Done";

    public string DoneWithFile(string fileName) =>
        IsRussian ? $"Готово: {fileName}" : $"Done: {fileName}";

    public string CreatedFilesStatus(int count) =>
        IsRussian ? $"Готово – создано файлов: {count}" : $"Done – created files: {count}";

    public string FileAlreadyCorrectSize => IsRussian
        ? "Файл уже нужного размера"
        : "The file already has the required size";

    public string ProcessingSummary(int created, int alreadyCorrect, int failed) =>
        IsRussian
            ? $"Создано: {created}, уже готово: {alreadyCorrect}, ошибок: {failed}"
            : $"Created: {created}, already ready: {alreadyCorrect}, errors: {failed}";

    public string ManualPreviewStatus => IsRussian
        ? "Ручной режим: настройте квадрат и нажмите «Сохранить»"
        : "Manual mode: adjust the square and click Save";

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
