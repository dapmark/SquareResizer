using System.Windows;

namespace ImageSquareResizer;

public partial class MainWindow : Window
{
    private readonly ManualPreviewState manualState = new();

    private AppSettings currentSettings;
    private Localization text;
    private bool isApplyingSettingsToUi;
    private bool hasQualityValidationError;

    public MainWindow()
        : this(AppSettings.Load())
    {
    }

    internal MainWindow(AppSettings settings)
    {
        currentSettings = settings;
        text = Localization.For(settings.Language);

        InitializeComponent();

        Title = AppVersion.WindowTitle;
        ApplySettingsToUi();

        DataObject.AddPastingHandler(QualityTextBox, OnQualityPaste);
        PreviewHost.SizeChanged += OnPreviewHostSizeChanged;

        ApplyTheme();
        SourceInitialized += OnSourceInitialized;
    }

    internal void OpenManualStartupFiles(string[] paths)
    {
        OpenManualFiles(paths);
    }
}
