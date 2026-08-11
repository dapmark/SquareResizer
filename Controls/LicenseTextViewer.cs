using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ImageSquareResizer.Controls;

public sealed class LicenseTextViewer : Control
{
    private const double WidthTolerance = 0.1;
    private const double WheelStep = 34.0;

    public static readonly DependencyProperty SelectionBrushProperty =
        DependencyProperty.Register(
            nameof(SelectionBrush),
            typeof(Brush),
            typeof(LicenseTextViewer),
            new FrameworkPropertyMetadata(Brushes.DodgerBlue, FrameworkPropertyMetadataOptions.AffectsRender));

    private readonly List<VisualLine> lines = new();
    private string text = string.Empty;
    private double pixelsPerDip = 1.0;
    private double layoutWidth = double.NaN;
    private double verticalOffset;
    private double lineHeight = 18.0;
    private int selectionAnchor;
    private int selectionCaret;
    private bool isSelecting;

    public LicenseTextViewer()
    {
        Focusable = true;
        FocusVisualStyle = null;
        Cursor = Cursors.IBeam;
        ClipToBounds = true;
        SnapsToDevicePixels = true;
        UseLayoutRounding = true;
    }

    public event EventHandler? ScrollOffsetChanged;
    public event EventHandler? SelectionChanged;

    public Brush SelectionBrush
    {
        get => (Brush)GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    public int SelectionLength => Math.Abs(selectionCaret - selectionAnchor);

    public double VerticalOffset => verticalOffset;

    public double ViewportHeight => Math.Max(0.0, ActualHeight);

    public double ExtentHeight
    {
        get
        {
            EnsureLayout();
            return lines.Count * lineHeight;
        }
    }

    public double ScrollableHeight => Math.Max(0.0, ExtentHeight - ViewportHeight);

    public double LineHeight
    {
        get
        {
            EnsureLayout();
            return lineHeight;
        }
    }

    public void SetText(string value)
    {
        text = value ?? string.Empty;
        selectionAnchor = 0;
        selectionCaret = 0;
        verticalOffset = 0.0;
        InvalidateTextLayout();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        SetText(string.Empty);
    }

    public void SelectAll()
    {
        selectionAnchor = 0;
        selectionCaret = text.Length;
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ClearSelection()
    {
        int caret = Math.Clamp(selectionCaret, 0, text.Length);
        selectionAnchor = caret;
        selectionCaret = caret;
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Copy()
    {
        int start = Math.Min(selectionAnchor, selectionCaret);
        int length = SelectionLength;
        if (length <= 0 || start < 0 || start + length > text.Length)
        {
            return;
        }

        try
        {
            Clipboard.SetText(text.Substring(start, length));
        }
        catch (ExternalException)
        {
        }
    }

    public void SetVerticalOffset(double offset)
    {
        EnsureLayout();
        double clamped = Math.Clamp(offset, 0.0, ScrollableHeight);
        if (Math.Abs(clamped - verticalOffset) < 0.01)
        {
            return;
        }

        verticalOffset = clamped;
        InvalidateVisual();
        ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ScrollBy(double delta)
    {
        SetVerticalOffset(verticalOffset + delta);
    }

    public bool ScrollByMouseWheelDelta(int delta)
    {
        if (delta == 0 || ScrollableHeight <= 0.0)
        {
            return false;
        }

        double notches = delta / 120.0;
        ScrollBy(-notches * WheelStep);
        return true;
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);
        EnsureLayout();

        if (lines.Count == 0 || ActualWidth <= 0.0 || ActualHeight <= 0.0)
        {
            return;
        }

        int firstLine = Math.Max(0, (int)Math.Floor(verticalOffset / lineHeight));
        int lastLine = Math.Min(
            lines.Count - 1,
            (int)Math.Ceiling((verticalOffset + ActualHeight) / lineHeight));

        int selectionStart = Math.Min(selectionAnchor, selectionCaret);
        int selectionEnd = Math.Max(selectionAnchor, selectionCaret);

        for (int index = firstLine; index <= lastLine; index++)
        {
            VisualLine line = lines[index];
            double y = index * lineHeight - verticalOffset;

            DrawSelectionForLine(drawingContext, line, y, selectionStart, selectionEnd);
            if (line.TextLength > 0)
            {
                drawingContext.DrawText(line.FormattedText, new Point(0.0, y));
            }
        }
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);

        if (Math.Abs(sizeInfo.NewSize.Width - sizeInfo.PreviousSize.Width) >= WidthTolerance)
        {
            InvalidateTextLayout();
        }
        else
        {
            SetVerticalOffset(verticalOffset);
        }
    }

    protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.Property == FontFamilyProperty ||
            e.Property == FontSizeProperty ||
            e.Property == FontStyleProperty ||
            e.Property == FontWeightProperty ||
            e.Property == FontStretchProperty ||
            e.Property == ForegroundProperty)
        {
            InvalidateTextLayout();
        }
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        if (ScrollByMouseWheelDelta(e.Delta))
        {
            e.Handled = true;
        }
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonDown(e);

        Focus();
        int hit = GetCharacterIndexFromPoint(e.GetPosition(this));

        if (e.ClickCount >= 2)
        {
            SelectWordAt(hit);
            e.Handled = true;
            return;
        }

        if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            selectionCaret = hit;
        }
        else
        {
            selectionAnchor = hit;
            selectionCaret = hit;
        }

        isSelecting = true;
        CaptureMouse();
        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (!isSelecting || e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        Point point = e.GetPosition(this);
        if (point.Y < 0.0)
        {
            ScrollBy(-lineHeight);
            point.Y = 0.0;
        }
        else if (point.Y > ActualHeight)
        {
            ScrollBy(lineHeight);
            point.Y = ActualHeight;
        }

        int hit = GetCharacterIndexFromPoint(point);
        if (hit != selectionCaret)
        {
            selectionCaret = hit;
            InvalidateVisual();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }

        e.Handled = true;
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseLeftButtonUp(e);

        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        isSelecting = false;
        e.Handled = true;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        bool control = Keyboard.Modifiers.HasFlag(ModifierKeys.Control);
        if (control && e.Key == Key.A)
        {
            SelectAll();
            e.Handled = true;
            return;
        }

        if (control && e.Key == Key.C)
        {
            Copy();
            e.Handled = true;
            return;
        }

        double page = Math.Max(LineHeight, ActualHeight * 0.82);
        switch (e.Key)
        {
            case Key.Up:
                ScrollBy(-LineHeight);
                e.Handled = true;
                break;
            case Key.Down:
                ScrollBy(LineHeight);
                e.Handled = true;
                break;
            case Key.PageUp:
                ScrollBy(-page);
                e.Handled = true;
                break;
            case Key.PageDown:
                ScrollBy(page);
                e.Handled = true;
                break;
            case Key.Home when control:
                SetVerticalOffset(0.0);
                e.Handled = true;
                break;
            case Key.End when control:
                SetVerticalOffset(ScrollableHeight);
                e.Handled = true;
                break;
        }
    }

    private void DrawSelectionForLine(
        DrawingContext drawingContext,
        VisualLine line,
        double y,
        int selectionStart,
        int selectionEnd)
    {
        if (selectionEnd <= selectionStart || line.TextLength <= 0)
        {
            return;
        }

        int lineStart = line.StartIndex;
        int lineEnd = line.StartIndex + line.TextLength;
        int visibleStart = Math.Max(selectionStart, lineStart);
        int visibleEnd = Math.Min(selectionEnd, lineEnd);
        if (visibleEnd <= visibleStart)
        {
            return;
        }

        int relativeStart = visibleStart - lineStart;
        int relativeLength = visibleEnd - visibleStart;

        drawingContext.PushOpacity(0.32);
        if (relativeStart == 0 && relativeLength == line.TextLength)
        {
            double width = Math.Max(1.0, line.FormattedText.WidthIncludingTrailingWhitespace);
            drawingContext.DrawRectangle(
                SelectionBrush,
                null,
                new Rect(0.0, y, width, lineHeight));
        }
        else
        {
            Geometry geometry = line.FormattedText.BuildHighlightGeometry(
                new Point(0.0, y),
                relativeStart,
                relativeLength);
            drawingContext.DrawGeometry(SelectionBrush, null, geometry);
        }
        drawingContext.Pop();
    }

    private int GetCharacterIndexFromPoint(Point point)
    {
        EnsureLayout();
        if (lines.Count == 0)
        {
            return 0;
        }

        double documentY = Math.Max(0.0, point.Y + verticalOffset);
        int lineIndex = Math.Clamp((int)Math.Floor(documentY / lineHeight), 0, lines.Count - 1);
        VisualLine line = lines[lineIndex];

        if (line.TextLength <= 0 || point.X <= 0.0)
        {
            return line.StartIndex;
        }

        double lineWidth = line.FormattedText.WidthIncludingTrailingWhitespace;
        if (point.X >= lineWidth)
        {
            return line.StartIndex + line.TextLength;
        }

        int low = 0;
        int high = line.TextLength;
        while (low < high)
        {
            int mid = low + (high - low) / 2;
            if (GetPrefixWidth(line, mid + 1) < point.X)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        int candidate = Math.Clamp(low, 0, line.TextLength);
        if (candidate > 0 && candidate < line.TextLength)
        {
            double left = GetPrefixWidth(line, candidate);
            double right = GetPrefixWidth(line, candidate + 1);
            if (point.X - left > (right - left) * 0.5)
            {
                candidate++;
            }
        }

        return Math.Clamp(line.StartIndex + candidate, line.StartIndex, line.StartIndex + line.TextLength);
    }

    private double GetPrefixWidth(VisualLine line, int count)
    {
        if (count <= 0)
        {
            return 0.0;
        }

        if (count >= line.TextLength)
        {
            return line.FormattedText.WidthIncludingTrailingWhitespace;
        }

        Geometry geometry = line.FormattedText.BuildHighlightGeometry(new Point(0.0, 0.0), 0, count);
        return Math.Max(0.0, geometry.Bounds.Right);
    }

    private void SelectWordAt(int index)
    {
        if (text.Length == 0)
        {
            selectionAnchor = 0;
            selectionCaret = 0;
            InvalidateVisual();
            SelectionChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        int clamped = Math.Clamp(index, 0, text.Length - 1);
        if (char.IsWhiteSpace(text[clamped]))
        {
            selectionAnchor = clamped;
            selectionCaret = Math.Min(text.Length, clamped + 1);
        }
        else
        {
            int start = clamped;
            int end = clamped + 1;
            while (start > 0 && !char.IsWhiteSpace(text[start - 1]))
            {
                start--;
            }

            while (end < text.Length && !char.IsWhiteSpace(text[end]))
            {
                end++;
            }

            selectionAnchor = start;
            selectionCaret = end;
        }

        InvalidateVisual();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureLayout()
    {
        double width = Math.Max(1.0, ActualWidth);
        double currentPixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (!double.IsNaN(layoutWidth) &&
            Math.Abs(layoutWidth - width) < WidthTolerance &&
            Math.Abs(pixelsPerDip - currentPixelsPerDip) < 0.001)
        {
            return;
        }

        pixelsPerDip = currentPixelsPerDip;
        BuildLines(width);
    }

    private void InvalidateTextLayout()
    {
        layoutWidth = double.NaN;
        lines.Clear();
        InvalidateVisual();
    }

    private void BuildLines(double width)
    {
        lines.Clear();
        layoutWidth = width;

        FormattedText sample = CreateFormattedText("Ag");
        lineHeight = Math.Max(1.0, sample.Height);

        if (text.Length == 0)
        {
            verticalOffset = 0.0;
            ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        int position = 0;
        while (position < text.Length)
        {
            int paragraphEnd = FindLineBreak(text, position, out int lineBreakLength);
            AddWrappedParagraph(position, paragraphEnd, width, lineBreakLength);
            position = paragraphEnd + lineBreakLength;
        }

        verticalOffset = Math.Clamp(verticalOffset, 0.0, Math.Max(0.0, lines.Count * lineHeight - ActualHeight));
        ScrollOffsetChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddWrappedParagraph(int start, int end, double width, int lineBreakLength)
    {
        if (end <= start)
        {
            lines.Add(new VisualLine(start, 0, lineBreakLength, CreateFormattedText(string.Empty)));
            return;
        }

        int position = start;
        while (position < end)
        {
            int remaining = end - position;
            int fit = Math.Max(1, FindMaximumFittingLength(position, remaining, width));

            if (fit < remaining)
            {
                int whitespaceBreak = FindLastWrapWhitespace(position, fit);
                if (whitespaceBreak > position)
                {
                    fit = whitespaceBreak - position;
                }
            }

            bool isLastVisualLine = position + fit >= end;
            int logicalLength = fit + (isLastVisualLine ? lineBreakLength : 0);
            string lineText = text.Substring(position, fit);
            lines.Add(new VisualLine(position, fit, logicalLength, CreateFormattedText(lineText)));
            position += fit;
        }
    }

    private int FindMaximumFittingLength(int start, int availableLength, double width)
    {
        if (availableLength <= 1 || MeasureRange(start, availableLength) <= width)
        {
            return availableLength;
        }

        int low = 1;
        int high = availableLength;
        int best = 1;
        while (low <= high)
        {
            int mid = low + (high - low) / 2;
            if (MeasureRange(start, mid) <= width)
            {
                best = mid;
                low = mid + 1;
            }
            else
            {
                high = mid - 1;
            }
        }

        return best;
    }

    private double MeasureRange(int start, int length)
    {
        if (length <= 0)
        {
            return 0.0;
        }

        return CreateFormattedText(text.Substring(start, length)).WidthIncludingTrailingWhitespace;
    }

    private FormattedText CreateFormattedText(string value)
    {
        return new FormattedText(
            value,
            CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface(FontFamily, FontStyle, FontWeight, FontStretch),
            FontSize,
            Foreground,
            null,
            TextFormattingMode.Display,
            pixelsPerDip);
    }

    private static int FindLineBreak(string value, int start, out int lineBreakLength)
    {
        for (int index = start; index < value.Length; index++)
        {
            if (value[index] == '\r')
            {
                lineBreakLength = index + 1 < value.Length && value[index + 1] == '\n' ? 2 : 1;
                return index;
            }

            if (value[index] == '\n')
            {
                lineBreakLength = 1;
                return index;
            }
        }

        lineBreakLength = 0;
        return value.Length;
    }

    private int FindLastWrapWhitespace(int start, int fit)
    {
        int end = Math.Min(text.Length, start + fit);
        for (int index = end - 1; index > start; index--)
        {
            if (text[index] == ' ' || text[index] == '\t')
            {
                return index + 1;
            }
        }

        return -1;
    }

    private sealed class VisualLine
    {
        public VisualLine(int startIndex, int textLength, int logicalLength, FormattedText formattedText)
        {
            StartIndex = startIndex;
            TextLength = textLength;
            LogicalLength = logicalLength;
            FormattedText = formattedText;
        }

        public int StartIndex { get; }
        public int TextLength { get; }
        public int LogicalLength { get; }
        public FormattedText FormattedText { get; }
    }
}
