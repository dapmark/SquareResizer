using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using SharpVectorsSvgIcon = SharpVectors.Converters.SvgIcon;

namespace ImageSquareResizer.Controls;

public sealed class SvgIcon : ContentControl
{
    private readonly SharpVectorsSvgIcon _renderer;

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(
            nameof(Source),
            typeof(string),
            typeof(SvgIcon),
            new FrameworkPropertyMetadata(null, OnSourceChanged));

    public static readonly DependencyProperty StretchProperty =
        DependencyProperty.Register(
            nameof(Stretch),
            typeof(Stretch),
            typeof(SvgIcon),
            new FrameworkPropertyMetadata(Stretch.Uniform, OnStretchChanged));

    public SvgIcon()
    {
        Focusable = false;
        IsTabStop = false;
        IsHitTestVisible = false;
        HorizontalContentAlignment = HorizontalAlignment.Stretch;
        VerticalContentAlignment = VerticalAlignment.Stretch;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;

        _renderer = new SharpVectorsSvgIcon
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
            Stretch = Stretch.Uniform,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false,
        };

        BindingOperations.SetBinding(
            _renderer,
            SharpVectorsSvgIcon.FillProperty,
            new Binding(nameof(Foreground))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });
        BindingOperations.SetBinding(
            _renderer,
            SharpVectorsSvgIcon.StrokeProperty,
            new Binding(nameof(Foreground))
            {
                Source = this,
                Mode = BindingMode.OneWay,
            });

        Content = _renderer;
    }

    public string? Source
    {
        get => (string?)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public Stretch Stretch
    {
        get => (Stretch)GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    private static void OnSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (SvgIcon)d;
        string? source = e.NewValue as string;
        control._renderer.UriSource = string.IsNullOrWhiteSpace(source)
            ? null
            : new Uri(source, UriKind.RelativeOrAbsolute);
    }

    private static void OnStretchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((SvgIcon)d)._renderer.Stretch = (Stretch)e.NewValue;
    }
}
