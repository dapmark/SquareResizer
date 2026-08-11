using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace ImageSquareResizer;

internal static class HoverTip
{
    private const int InitialShowDelayMilliseconds = 1000;
    private const int InteractiveCloseDelayMilliseconds = 100;
    private const int CursorPollIntervalMilliseconds = 30;
    private const int FadeInDurationMilliseconds = 80;
    private const int FadeOutDurationMilliseconds = 70;
    private const double PopupHorizontalOffset = 12;
    private const double PopupVerticalOffset = 18;
    private const double InteractiveCorridorTolerance = 4;
    private const double TooltipMaxWidth = 420;

    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(HoverTip),
        new PropertyMetadata(null, OnTextChanged));

    public static readonly DependencyProperty KeepOpenOverTipProperty = DependencyProperty.RegisterAttached(
        "KeepOpenOverTip",
        typeof(bool),
        typeof(HoverTip),
        new PropertyMetadata(false));

    public static readonly DependencyProperty ShowWhenTrimmedProperty = DependencyProperty.RegisterAttached(
        "ShowWhenTrimmed",
        typeof(bool),
        typeof(HoverTip),
        new PropertyMetadata(false));

    private static readonly DependencyProperty IsAttachedProperty = DependencyProperty.RegisterAttached(
        "IsAttached",
        typeof(bool),
        typeof(HoverTip),
        new PropertyMetadata(false));

    private static readonly DependencyProperty ScrollViewerHookedProperty = DependencyProperty.RegisterAttached(
        "ScrollViewerHooked",
        typeof(bool),
        typeof(HoverTip),
        new PropertyMetadata(false));

    private static readonly DependencyProperty SuppressUntilMouseLeaveProperty = DependencyProperty.RegisterAttached(
        "SuppressUntilMouseLeave",
        typeof(bool),
        typeof(HoverTip),
        new PropertyMetadata(false));

    private static readonly DispatcherTimer ShowTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(InitialShowDelayMilliseconds)
    };

    private static readonly DispatcherTimer CursorTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(CursorPollIntervalMilliseconds)
    };

    private static readonly DispatcherTimer InteractiveCloseTimer = new()
    {
        Interval = TimeSpan.FromMilliseconds(InteractiveCloseDelayMilliseconds)
    };

    private static Popup? popup;
    private static Border? popupBorder;
    private static TextBlock? popupText;
    private static FrameworkElement? pendingOwner;
    private static FrameworkElement? activeOwner;
    private static Window? ownerWindow;
    private static bool isClosing;
    private static bool pointerEnteredPopup;
    private static bool blockedAfterScroll;
    private static NativePoint scrollCursorPosition;
    private static bool applicationExitHooked;

    static HoverTip()
    {
        ShowTimer.Tick += ShowTimer_OnTick;
        CursorTimer.Tick += CursorTimer_OnTick;
        InteractiveCloseTimer.Tick += InteractiveCloseTimer_OnTick;
    }

    public static void SetText(DependencyObject element, string? value)
    {
        element.SetValue(TextProperty, value);
    }

    public static string? GetText(DependencyObject element)
    {
        return (string?)element.GetValue(TextProperty);
    }

    public static void SetKeepOpenOverTip(DependencyObject element, bool value)
    {
        element.SetValue(KeepOpenOverTipProperty, value);
    }

    public static bool GetKeepOpenOverTip(DependencyObject element)
    {
        return (bool)element.GetValue(KeepOpenOverTipProperty);
    }

    public static void SetShowWhenTrimmed(DependencyObject element, bool value)
    {
        element.SetValue(ShowWhenTrimmedProperty, value);
    }

    public static bool GetShowWhenTrimmed(DependencyObject element)
    {
        return (bool)element.GetValue(ShowWhenTrimmedProperty);
    }

    public static void CloseImmediately()
    {
        CloseImmediatelyCore(clearScrollBlock: true);
    }

    public static void DismissUntilMouseLeave(FrameworkElement owner)
    {
        owner.SetValue(SuppressUntilMouseLeaveProperty, true);
        CloseImmediatelyCore(clearScrollBlock: false);
    }

    private static void OnTextChanged(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (dependencyObject is not FrameworkElement owner)
        {
            return;
        }

        bool hasText = !string.IsNullOrWhiteSpace(eventArgs.NewValue as string);
        bool isAttached = (bool)owner.GetValue(IsAttachedProperty);

        if (hasText && !isAttached)
        {
            AttachOwner(owner);
            return;
        }

        if (!hasText && isAttached)
        {
            DetachOwner(owner);
        }

        if (ReferenceEquals(owner, activeOwner) && hasText && popupText is not null)
        {
            popupText.Text = eventArgs.NewValue as string ?? string.Empty;
        }
    }

    private static void AttachOwner(FrameworkElement owner)
    {
        owner.SetValue(IsAttachedProperty, true);
        owner.MouseEnter += Owner_OnMouseEnter;
        owner.MouseLeave += Owner_OnMouseLeave;
        owner.MouseMove += Owner_OnMouseMove;
        owner.PreviewMouseWheel += Owner_OnPreviewMouseWheel;
        owner.Loaded += Owner_OnLoaded;
        owner.Unloaded += Owner_OnUnloaded;
        owner.IsVisibleChanged += Owner_OnIsVisibleChanged;
        owner.IsEnabledChanged += Owner_OnIsEnabledChanged;

        HookContainingScrollViewer(owner);
        HookApplicationExit();
    }

    private static void DetachOwner(FrameworkElement owner)
    {
        owner.SetValue(IsAttachedProperty, false);
        owner.MouseEnter -= Owner_OnMouseEnter;
        owner.MouseLeave -= Owner_OnMouseLeave;
        owner.MouseMove -= Owner_OnMouseMove;
        owner.PreviewMouseWheel -= Owner_OnPreviewMouseWheel;
        owner.Loaded -= Owner_OnLoaded;
        owner.Unloaded -= Owner_OnUnloaded;
        owner.IsVisibleChanged -= Owner_OnIsVisibleChanged;
        owner.IsEnabledChanged -= Owner_OnIsEnabledChanged;

        if (ReferenceEquals(owner, pendingOwner))
        {
            CancelPendingShow();
        }

        if (ReferenceEquals(owner, activeOwner))
        {
            CloseImmediatelyCore(clearScrollBlock: false);
        }
    }

    private static void Owner_OnLoaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is FrameworkElement owner)
        {
            HookContainingScrollViewer(owner);
        }
    }

    private static void Owner_OnUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not FrameworkElement owner)
        {
            return;
        }

        if (ReferenceEquals(owner, pendingOwner))
        {
            CancelPendingShow();
        }

        if (ReferenceEquals(owner, activeOwner))
        {
            CloseImmediatelyCore(clearScrollBlock: false);
        }
    }

    private static void Owner_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.NewValue is not false || sender is not FrameworkElement owner)
        {
            return;
        }

        if (ReferenceEquals(owner, pendingOwner))
        {
            CancelPendingShow();
        }

        if (ReferenceEquals(owner, activeOwner))
        {
            CloseImmediatelyCore(clearScrollBlock: false);
        }
    }

    private static void Owner_OnIsEnabledChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.NewValue is not false || sender is not FrameworkElement owner)
        {
            return;
        }

        if (ReferenceEquals(owner, pendingOwner))
        {
            CancelPendingShow();
        }

        if (ReferenceEquals(owner, activeOwner))
        {
            CloseImmediatelyCore(clearScrollBlock: false);
        }
    }

    private static void Owner_OnMouseEnter(object sender, MouseEventArgs eventArgs)
    {
        if (sender is not FrameworkElement owner || blockedAfterScroll || !CanShowForOwner(owner))
        {
            return;
        }

        if (ReferenceEquals(owner, activeOwner) && popup?.IsOpen == true)
        {
            if (isClosing)
            {
                CancelFadeOut();
            }

            return;
        }

        BeginPendingShow(owner);
    }

    private static void Owner_OnMouseLeave(object sender, MouseEventArgs eventArgs)
    {
        if (sender is not FrameworkElement owner)
        {
            return;
        }

        owner.ClearValue(SuppressUntilMouseLeaveProperty);

        if (ReferenceEquals(owner, pendingOwner))
        {
            CancelPendingShow();
        }

        if (!ReferenceEquals(owner, activeOwner) || popup?.IsOpen != true)
        {
            return;
        }

        if (GetKeepOpenOverTip(owner))
        {
            StartInteractiveCloseDelay();
            return;
        }

        BeginFadeOut();
    }

    private static void Owner_OnMouseMove(object sender, MouseEventArgs eventArgs)
    {
        if (sender is not FrameworkElement owner || !GetCursorPos(out NativePoint cursorPosition))
        {
            return;
        }

        if (blockedAfterScroll)
        {
            if (cursorPosition.X == scrollCursorPosition.X && cursorPosition.Y == scrollCursorPosition.Y)
            {
                return;
            }

            blockedAfterScroll = false;
        }

        if (activeOwner is null && pendingOwner is null && CanShowForOwner(owner) && IsCursorInsideOwner(owner, cursorPosition))
        {
            BeginPendingShow(owner);
        }
    }

    private static void Owner_OnPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
    {
        BlockAfterScroll();
    }

    private static void ScrollViewer_OnScrollChanged(object sender, ScrollChangedEventArgs eventArgs)
    {
        if (eventArgs.HorizontalChange != 0 || eventArgs.VerticalChange != 0)
        {
            BlockAfterScroll();
        }
    }

    private static void ScrollViewer_OnUnloaded(object sender, RoutedEventArgs eventArgs)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        scrollViewer.ScrollChanged -= ScrollViewer_OnScrollChanged;
        scrollViewer.Unloaded -= ScrollViewer_OnUnloaded;
        scrollViewer.ClearValue(ScrollViewerHookedProperty);
    }

    private static void BeginPendingShow(FrameworkElement owner)
    {
        if (ReferenceEquals(owner, pendingOwner) && ShowTimer.IsEnabled)
        {
            return;
        }

        if (activeOwner is not null && !ReferenceEquals(activeOwner, owner))
        {
            CloseImmediatelyCore(clearScrollBlock: false);
        }

        CancelPendingShow();
        pendingOwner = owner;
        AttachWindowLifecycle(Window.GetWindow(owner));
        ShowTimer.Start();
    }

    private static void CancelPendingShow()
    {
        ShowTimer.Stop();
        pendingOwner = null;

        if (activeOwner is null)
        {
            DetachWindowLifecycle();
            StopCursorTimerIfIdle();
        }
    }

    private static void ShowTimer_OnTick(object? sender, EventArgs eventArgs)
    {
        ShowTimer.Stop();

        FrameworkElement? owner = pendingOwner;
        pendingOwner = null;

        if (owner is null || blockedAfterScroll || !CanShowForOwner(owner) || !GetCursorPos(out NativePoint cursorPosition) || !IsCursorInsideOwner(owner, cursorPosition))
        {
            if (activeOwner is null)
            {
                DetachWindowLifecycle();
            }

            StopCursorTimerIfIdle();
            return;
        }

        OpenPopup(owner);
    }

    private static void OpenPopup(FrameworkElement owner)
    {
        EnsurePopup();

        string text = GetText(owner) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text) || popup is null || popupBorder is null || popupText is null)
        {
            return;
        }

        StopAllCloseActivity();
        activeOwner = owner;
        pointerEnteredPopup = false;
        isClosing = false;
        AttachWindowLifecycle(Window.GetWindow(owner));

        popup.PlacementTarget = owner;
        popupText.Text = text;
        popupBorder.Background = FindBrush(owner, "WindowBackgroundBrush", null, Brushes.White);
        popupBorder.BorderBrush = FindBrush(owner, "WindowBorderBrush", null, Brushes.Gray);
        popupText.Foreground = FindBrush(owner, "MainTextBrush", null, Brushes.Black);
        bool keepOpenOverTip = GetKeepOpenOverTip(owner);
        popup.IsHitTestVisible = keepOpenOverTip;
        popupBorder.IsHitTestVisible = keepOpenOverTip;
        popupBorder.BeginAnimation(UIElement.OpacityProperty, null);
        popupBorder.Opacity = 0;

        popup.IsOpen = true;
        popupBorder.UpdateLayout();
        CursorTimer.Start();
        AnimateOpacity(0, 1, FadeInDurationMilliseconds, null);
    }

    private static void EnsurePopup()
    {
        if (popup is not null)
        {
            return;
        }

        popupText = new TextBlock
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 12,
            FontWeight = FontWeights.Normal,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = TooltipMaxWidth,
            SnapsToDevicePixels = true
        };

        popupBorder = new Border
        {
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(8, 5, 8, 5),
            SnapsToDevicePixels = true,
            UseLayoutRounding = true,
            Child = popupText
        };

        popup = new Popup
        {
            Placement = PlacementMode.MousePoint,
            HorizontalOffset = PopupHorizontalOffset,
            VerticalOffset = PopupVerticalOffset,
            AllowsTransparency = true,
            StaysOpen = true,
            PopupAnimation = PopupAnimation.None,
            IsHitTestVisible = false,
            Child = popupBorder
        };
    }

    private static void CursorTimer_OnTick(object? sender, EventArgs eventArgs)
    {
        if (!GetCursorPos(out NativePoint cursorPosition))
        {
            CloseImmediatelyCore(clearScrollBlock: true);
            return;
        }

        if (blockedAfterScroll)
        {
            if (cursorPosition.X != scrollCursorPosition.X || cursorPosition.Y != scrollCursorPosition.Y)
            {
                blockedAfterScroll = false;
                StopCursorTimerIfIdle();
            }

            return;
        }

        FrameworkElement? owner = activeOwner;
        if (owner is null || popup?.IsOpen != true)
        {
            StopCursorTimerIfIdle();
            return;
        }

        if (!owner.IsVisible || !owner.IsLoaded || !owner.IsEnabled)
        {
            CloseImmediatelyCore(clearScrollBlock: true);
            return;
        }

        bool keepOpenOverTip = GetKeepOpenOverTip(owner);
        bool insideOwner = IsCursorInsideOwner(owner, cursorPosition);

        if (!keepOpenOverTip)
        {
            if (insideOwner)
            {
                if (isClosing)
                {
                    CancelFadeOut();
                }
            }
            else if (!isClosing)
            {
                BeginFadeOut();
            }

            return;
        }

        bool insidePopup = IsCursorInsidePopup(cursorPosition);
        if (insidePopup)
        {
            pointerEnteredPopup = true;
        }

        bool insideCorridor = !pointerEnteredPopup && IsCursorInsideTransitionCorridor(owner, cursorPosition);
        if (insideOwner || insidePopup || insideCorridor)
        {
            InteractiveCloseTimer.Stop();

            if (isClosing)
            {
                CancelFadeOut();
            }

            return;
        }

        StartInteractiveCloseDelay();
    }

    private static void StartInteractiveCloseDelay()
    {
        if (InteractiveCloseTimer.IsEnabled || isClosing)
        {
            return;
        }

        InteractiveCloseTimer.Start();
    }

    private static void InteractiveCloseTimer_OnTick(object? sender, EventArgs eventArgs)
    {
        InteractiveCloseTimer.Stop();

        FrameworkElement? owner = activeOwner;
        if (owner is null || !GetCursorPos(out NativePoint cursorPosition))
        {
            BeginFadeOut();
            return;
        }

        bool insideOwner = IsCursorInsideOwner(owner, cursorPosition);
        bool insidePopup = IsCursorInsidePopup(cursorPosition);
        bool insideCorridor = !pointerEnteredPopup && IsCursorInsideTransitionCorridor(owner, cursorPosition);

        if (!insideOwner && !insidePopup && !insideCorridor)
        {
            BeginFadeOut();
        }
    }

    private static void BeginFadeOut()
    {
        if (popup?.IsOpen != true || popupBorder is null || isClosing)
        {
            return;
        }

        InteractiveCloseTimer.Stop();
        isClosing = true;
        double from = popupBorder.Opacity;
        AnimateOpacity(from, 0, FadeOutDurationMilliseconds, (_, _) =>
        {
            if (isClosing)
            {
                CloseImmediatelyCore(clearScrollBlock: false);
            }
        });
    }

    private static void CancelFadeOut()
    {
        if (popupBorder is null)
        {
            return;
        }

        double currentOpacity = popupBorder.Opacity;
        isClosing = false;
        InteractiveCloseTimer.Stop();
        popupBorder.BeginAnimation(UIElement.OpacityProperty, null);
        popupBorder.Opacity = currentOpacity;
        AnimateOpacity(currentOpacity, 1, FadeInDurationMilliseconds, null);
    }

    private static void AnimateOpacity(double from, double to, int durationMilliseconds, EventHandler? completed)
    {
        if (popupBorder is null)
        {
            return;
        }

        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = TimeSpan.FromMilliseconds(durationMilliseconds),
            FillBehavior = FillBehavior.HoldEnd
        };

        if (completed is not null)
        {
            animation.Completed += completed;
        }

        popupBorder.BeginAnimation(UIElement.OpacityProperty, animation, HandoffBehavior.SnapshotAndReplace);
    }

    private static void StopAllCloseActivity()
    {
        InteractiveCloseTimer.Stop();
        isClosing = false;

        if (popupBorder is not null)
        {
            popupBorder.BeginAnimation(UIElement.OpacityProperty, null);
        }
    }

    private static void CloseImmediatelyCore(bool clearScrollBlock)
    {
        ShowTimer.Stop();
        InteractiveCloseTimer.Stop();
        pendingOwner = null;
        isClosing = false;
        pointerEnteredPopup = false;

        if (popupBorder is not null)
        {
            popupBorder.BeginAnimation(UIElement.OpacityProperty, null);
            popupBorder.Opacity = 0;
        }

        if (popup is not null)
        {
            popup.IsOpen = false;
            popup.PlacementTarget = null;
        }

        activeOwner = null;
        DetachWindowLifecycle();

        if (clearScrollBlock)
        {
            blockedAfterScroll = false;
        }

        StopCursorTimerIfIdle();
    }

    private static void BlockAfterScroll()
    {
        if (!GetCursorPos(out scrollCursorPosition))
        {
            CloseImmediatelyCore(clearScrollBlock: true);
            return;
        }

        blockedAfterScroll = true;
        CloseImmediatelyCore(clearScrollBlock: false);
        CursorTimer.Start();
    }

    private static void StopCursorTimerIfIdle()
    {
        if (!blockedAfterScroll && activeOwner is null && pendingOwner is null)
        {
            CursorTimer.Stop();
        }
    }

    private static bool CanShowForOwner(FrameworkElement owner)
    {
        if ((bool)owner.GetValue(SuppressUntilMouseLeaveProperty) ||
            !owner.IsLoaded || !owner.IsVisible || !owner.IsEnabled || string.IsNullOrWhiteSpace(GetText(owner)))
        {
            return false;
        }

        return !GetShowWhenTrimmed(owner) || IsTextTrimmed(owner);
    }

    private static bool IsTextTrimmed(FrameworkElement owner)
    {
        if (owner is not TextBlock textBlock || string.IsNullOrEmpty(textBlock.Text) || textBlock.ActualWidth <= 0)
        {
            return false;
        }

        var typeface = new Typeface(
            textBlock.FontFamily,
            textBlock.FontStyle,
            textBlock.FontWeight,
            textBlock.FontStretch);

        var formattedText = new FormattedText(
            textBlock.Text,
            System.Globalization.CultureInfo.CurrentUICulture,
            textBlock.FlowDirection,
            typeface,
            textBlock.FontSize,
            textBlock.Foreground,
            VisualTreeHelper.GetDpi(textBlock).PixelsPerDip);

        return formattedText.WidthIncludingTrailingWhitespace > textBlock.ActualWidth + 0.5;
    }

    private static bool IsCursorInsideOwner(FrameworkElement owner, NativePoint cursorPosition)
    {
        return TryGetScreenBounds(owner, out Rect bounds) && bounds.Contains(cursorPosition.X, cursorPosition.Y);
    }

    private static bool IsCursorInsidePopup(NativePoint cursorPosition)
    {
        return popupBorder is not null && TryGetScreenBounds(popupBorder, out Rect bounds) && bounds.Contains(cursorPosition.X, cursorPosition.Y);
    }

    private static bool IsCursorInsideTransitionCorridor(FrameworkElement owner, NativePoint cursorPosition)
    {
        if (popupBorder is null || !TryGetScreenBounds(owner, out Rect ownerBounds) || !TryGetScreenBounds(popupBorder, out Rect popupBounds))
        {
            return false;
        }

        Rect corridor;

        if (popupBounds.Top >= ownerBounds.Bottom)
        {
            double startX = Math.Clamp(popupBounds.Left, ownerBounds.Left, ownerBounds.Right);
            double endX = Math.Clamp(ownerBounds.Right, popupBounds.Left, popupBounds.Right);
            double left = Math.Min(startX, endX) - InteractiveCorridorTolerance;
            double right = Math.Max(startX, endX) + InteractiveCorridorTolerance;
            corridor = new Rect(left, ownerBounds.Bottom, Math.Max(0, right - left), Math.Max(0, popupBounds.Top - ownerBounds.Bottom));
        }
        else if (popupBounds.Bottom <= ownerBounds.Top)
        {
            double startX = Math.Clamp(popupBounds.Left, ownerBounds.Left, ownerBounds.Right);
            double endX = Math.Clamp(ownerBounds.Right, popupBounds.Left, popupBounds.Right);
            double left = Math.Min(startX, endX) - InteractiveCorridorTolerance;
            double right = Math.Max(startX, endX) + InteractiveCorridorTolerance;
            corridor = new Rect(left, popupBounds.Bottom, Math.Max(0, right - left), Math.Max(0, ownerBounds.Top - popupBounds.Bottom));
        }
        else if (popupBounds.Left >= ownerBounds.Right)
        {
            double startY = Math.Clamp(popupBounds.Top, ownerBounds.Top, ownerBounds.Bottom);
            double endY = Math.Clamp(ownerBounds.Bottom, popupBounds.Top, popupBounds.Bottom);
            double top = Math.Min(startY, endY) - InteractiveCorridorTolerance;
            double bottom = Math.Max(startY, endY) + InteractiveCorridorTolerance;
            corridor = new Rect(ownerBounds.Right, top, Math.Max(0, popupBounds.Left - ownerBounds.Right), Math.Max(0, bottom - top));
        }
        else if (popupBounds.Right <= ownerBounds.Left)
        {
            double startY = Math.Clamp(popupBounds.Top, ownerBounds.Top, ownerBounds.Bottom);
            double endY = Math.Clamp(ownerBounds.Bottom, popupBounds.Top, popupBounds.Bottom);
            double top = Math.Min(startY, endY) - InteractiveCorridorTolerance;
            double bottom = Math.Max(startY, endY) + InteractiveCorridorTolerance;
            corridor = new Rect(popupBounds.Right, top, Math.Max(0, ownerBounds.Left - popupBounds.Right), Math.Max(0, bottom - top));
        }
        else
        {
            return false;
        }

        corridor.Inflate(InteractiveCorridorTolerance, InteractiveCorridorTolerance);
        return corridor.Contains(cursorPosition.X, cursorPosition.Y);
    }

    private static bool TryGetScreenBounds(FrameworkElement element, out Rect bounds)
    {
        bounds = Rect.Empty;

        if (!element.IsLoaded || element.ActualWidth <= 0 || element.ActualHeight <= 0)
        {
            return false;
        }

        try
        {
            Point topLeft = element.PointToScreen(new Point(0, 0));
            Point bottomRight = element.PointToScreen(new Point(element.ActualWidth, element.ActualHeight));
            bounds = new Rect(topLeft, bottomRight);
            return !bounds.IsEmpty;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    private static Brush FindBrush(FrameworkElement owner, string primaryKey, string? fallbackKey, Brush defaultBrush)
    {
        if (owner.TryFindResource(primaryKey) is Brush primaryBrush)
        {
            return primaryBrush;
        }

        if (fallbackKey is not null && owner.TryFindResource(fallbackKey) is Brush fallbackBrush)
        {
            return fallbackBrush;
        }

        return defaultBrush;
    }

    private static void HookContainingScrollViewer(FrameworkElement owner)
    {
        DependencyObject? current = owner;

        while (current is not null)
        {
            if (current is ScrollViewer scrollViewer)
            {
                if (!(bool)scrollViewer.GetValue(ScrollViewerHookedProperty))
                {
                    scrollViewer.SetValue(ScrollViewerHookedProperty, true);
                    scrollViewer.ScrollChanged += ScrollViewer_OnScrollChanged;
                    scrollViewer.Unloaded += ScrollViewer_OnUnloaded;
                }

                return;
            }

            current = VisualTreeHelper.GetParent(current);
        }
    }

    private static void AttachWindowLifecycle(Window? window)
    {
        if (ReferenceEquals(ownerWindow, window))
        {
            return;
        }

        DetachWindowLifecycle();
        ownerWindow = window;

        if (ownerWindow is null)
        {
            return;
        }

        ownerWindow.Deactivated += OwnerWindow_OnDeactivated;
        ownerWindow.Closed += OwnerWindow_OnClosed;
        ownerWindow.IsVisibleChanged += OwnerWindow_OnIsVisibleChanged;
        ownerWindow.PreviewMouseWheel += OwnerWindow_OnPreviewMouseWheel;
    }

    private static void DetachWindowLifecycle()
    {
        if (ownerWindow is null)
        {
            return;
        }

        ownerWindow.Deactivated -= OwnerWindow_OnDeactivated;
        ownerWindow.Closed -= OwnerWindow_OnClosed;
        ownerWindow.IsVisibleChanged -= OwnerWindow_OnIsVisibleChanged;
        ownerWindow.PreviewMouseWheel -= OwnerWindow_OnPreviewMouseWheel;
        ownerWindow = null;
    }

    private static void OwnerWindow_OnPreviewMouseWheel(object sender, MouseWheelEventArgs eventArgs)
    {
        BlockAfterScroll();
    }

    private static void OwnerWindow_OnDeactivated(object? sender, EventArgs eventArgs)
    {
        CloseImmediatelyCore(clearScrollBlock: true);
    }

    private static void OwnerWindow_OnClosed(object? sender, EventArgs eventArgs)
    {
        CloseImmediatelyCore(clearScrollBlock: true);
    }

    private static void OwnerWindow_OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.NewValue is false)
        {
            CloseImmediatelyCore(clearScrollBlock: true);
        }
    }

    private static void HookApplicationExit()
    {
        if (applicationExitHooked || Application.Current is null)
        {
            return;
        }

        applicationExitHooked = true;
        Application.Current.Exit += Application_OnExit;
    }

    private static void Application_OnExit(object sender, ExitEventArgs eventArgs)
    {
        CloseImmediatelyCore(clearScrollBlock: true);
    }

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out NativePoint point);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
