using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace Custos.WPF.Behaviors;

/// <summary>
/// Provides smooth scrolling behavior for ScrollViewer
/// </summary>
public static class SmoothScrollBehavior
{
    private const double ScrollSpeed = 1.2; // Multiplier for scroll speed
    private const int AnimationDuration = 400; // Milliseconds

    public static readonly DependencyProperty IsEnabledProperty =
        DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(false, OnIsEnabledChanged));

    public static bool GetIsEnabled(DependencyObject obj)
    {
        return (bool)obj.GetValue(IsEnabledProperty);
    }

    public static void SetIsEnabled(DependencyObject obj, bool value)
    {
        obj.SetValue(IsEnabledProperty, value);
    }

    private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer)
        {
            if ((bool)e.NewValue)
            {
                scrollViewer.PreviewMouseWheel += ScrollViewer_PreviewMouseWheel;
            }
            else
            {
                scrollViewer.PreviewMouseWheel -= ScrollViewer_PreviewMouseWheel;
            }
        }
    }

    private static void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
            return;

        // Calculate target scroll offset
        double delta = e.Delta * ScrollSpeed;
        double targetOffset = scrollViewer.VerticalOffset - delta;

        // Clamp to valid range
        targetOffset = Math.Max(0, Math.Min(scrollViewer.ScrollableHeight, targetOffset));

        // Animate to target offset
        AnimateScroll(scrollViewer, targetOffset);

        e.Handled = true;
    }

    private static void AnimateScroll(ScrollViewer scrollViewer, double targetOffset)
    {
        // Create animation with smooth easing
        var animation = new DoubleAnimation
        {
            From = scrollViewer.VerticalOffset,
            To = targetOffset,
            Duration = TimeSpan.FromMilliseconds(AnimationDuration),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Create storyboard to animate the scroll
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);

        // Use attached property for animation target
        Storyboard.SetTarget(animation, scrollViewer);
        Storyboard.SetTargetProperty(animation, new PropertyPath(ScrollOffsetProperty));

        storyboard.Begin();
    }

    // Attached property for animating ScrollViewer offset
    private static readonly DependencyProperty ScrollOffsetProperty =
        DependencyProperty.RegisterAttached(
            "ScrollOffset",
            typeof(double),
            typeof(SmoothScrollBehavior),
            new PropertyMetadata(0.0, OnScrollOffsetChanged));

    private static void OnScrollOffsetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is ScrollViewer scrollViewer && e.NewValue is double offset)
        {
            scrollViewer.ScrollToVerticalOffset(offset);
        }
    }
}
