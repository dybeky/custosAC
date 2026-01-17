using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Custos.WPF.Helpers;

/// <summary>
/// Reusable animation helpers for smooth, performant UI animations
/// </summary>
public static class AnimationHelper
{
    // Standard durations for consistency
    public static readonly Duration FastDuration = new Duration(TimeSpan.FromMilliseconds(200));
    public static readonly Duration NormalDuration = new Duration(TimeSpan.FromMilliseconds(300));
    public static readonly Duration SlowDuration = new Duration(TimeSpan.FromMilliseconds(400));

    // Standard easing functions for natural motion
    public static readonly IEasingFunction EaseOutCubic = new CubicEase { EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction EaseInOutCubic = new CubicEase { EasingMode = EasingMode.EaseInOut };
    public static readonly IEasingFunction EaseOutQuart = new QuarticEase { EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction EaseOutBack = new BackEase { EasingMode = EasingMode.EaseOut, Amplitude = 0.5 };
    public static readonly IEasingFunction EaseOutElastic = new ElasticEase { EasingMode = EasingMode.EaseOut, Oscillations = 1, Springiness = 3 };

    /// <summary>
    /// Enables hardware acceleration for better animation performance
    /// </summary>
    public static void EnableHardwareAcceleration(UIElement element)
    {
        RenderOptions.SetCachingHint(element, CachingHint.Cache);
        element.CacheMode = new BitmapCache
        {
            RenderAtScale = 1,
            SnapsToDevicePixels = true,
            EnableClearType = false // Better for animations
        };
    }

    /// <summary>
    /// Disables hardware acceleration after animation completes
    /// </summary>
    public static void DisableHardwareAcceleration(UIElement element)
    {
        element.CacheMode = null;
    }

    /// <summary>
    /// Smooth fade in animation
    /// </summary>
    public static void FadeIn(UIElement element, double from = 0, double to = 1, Duration? duration = null, IEasingFunction? easing = null, EventHandler? completed = null)
    {
        element.Opacity = from;
        element.Visibility = Visibility.Visible;

        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        if (completed != null)
            animation.Completed += completed;

        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    /// <summary>
    /// Smooth fade out animation
    /// </summary>
    public static void FadeOut(UIElement element, double from = 1, double to = 0, Duration? duration = null, IEasingFunction? easing = null, bool collapseOnComplete = true)
    {
        var animation = new DoubleAnimation
        {
            From = from,
            To = to,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        if (collapseOnComplete)
        {
            animation.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
            };
        }

        element.BeginAnimation(UIElement.OpacityProperty, animation);
    }

    /// <summary>
    /// Slide + Fade transition (ideal for view changes)
    /// </summary>
    public static void SlideAndFadeIn(UIElement element, double slideDistance = 30, Duration? duration = null, IEasingFunction? easing = null)
    {
        EnableHardwareAcceleration(element);

        element.Opacity = 0;
        element.Visibility = Visibility.Visible;

        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        // Create or get TranslateTransform
        if (element.RenderTransform is not TranslateTransform)
        {
            element.RenderTransform = new TranslateTransform(0, slideDistance);
        }

        var slideAnimation = new DoubleAnimation
        {
            From = slideDistance,
            To = 0,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        slideAnimation.Completed += (s, e) => DisableHardwareAcceleration(element);

        element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        element.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
    }

    /// <summary>
    /// Slide and fade out
    /// </summary>
    public static void SlideAndFadeOut(UIElement element, double slideDistance = -30, Duration? duration = null, IEasingFunction? easing = null, bool collapseOnComplete = true)
    {
        EnableHardwareAcceleration(element);

        var fadeAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = duration ?? FastDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        // Create or get TranslateTransform
        if (element.RenderTransform is not TranslateTransform)
        {
            element.RenderTransform = new TranslateTransform(0, 0);
        }

        var slideAnimation = new DoubleAnimation
        {
            From = 0,
            To = slideDistance,
            Duration = duration ?? FastDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        if (collapseOnComplete)
        {
            slideAnimation.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                DisableHardwareAcceleration(element);
            };
        }
        else
        {
            slideAnimation.Completed += (s, e) => DisableHardwareAcceleration(element);
        }

        element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        element.RenderTransform.BeginAnimation(TranslateTransform.YProperty, slideAnimation);
    }

    /// <summary>
    /// Scale + Fade animation (great for popups/overlays)
    /// </summary>
    public static void ScaleAndFadeIn(UIElement element, double fromScale = 0.9, double toScale = 1.0, Duration? duration = null, IEasingFunction? easing = null)
    {
        EnableHardwareAcceleration(element);

        element.Opacity = 0;
        element.Visibility = Visibility.Visible;
        element.RenderTransformOrigin = new Point(0.5, 0.5);

        var fadeAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutBack
        };

        // Create or get ScaleTransform
        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransform = new ScaleTransform(fromScale, fromScale);
        }

        var scaleXAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutBack
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutBack
        };

        scaleXAnimation.Completed += (s, e) => DisableHardwareAcceleration(element);

        element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    }

    /// <summary>
    /// Scale and fade out
    /// </summary>
    public static void ScaleAndFadeOut(UIElement element, double fromScale = 1.0, double toScale = 0.95, Duration? duration = null, IEasingFunction? easing = null, bool collapseOnComplete = true)
    {
        EnableHardwareAcceleration(element);

        element.RenderTransformOrigin = new Point(0.5, 0.5);

        var fadeAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = duration ?? FastDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        // Create or get ScaleTransform
        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransform = new ScaleTransform(fromScale, fromScale);
        }

        var scaleXAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = duration ?? FastDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        var scaleYAnimation = new DoubleAnimation
        {
            From = fromScale,
            To = toScale,
            Duration = duration ?? FastDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        if (collapseOnComplete)
        {
            scaleXAnimation.Completed += (s, e) =>
            {
                element.Visibility = Visibility.Collapsed;
                DisableHardwareAcceleration(element);
            };
        }
        else
        {
            scaleXAnimation.Completed += (s, e) => DisableHardwareAcceleration(element);
        }

        element.BeginAnimation(UIElement.OpacityProperty, fadeAnimation);
        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleYAnimation);
    }

    /// <summary>
    /// Stagger animation - animate children with delay
    /// </summary>
    public static void StaggerIn(System.Collections.IEnumerable children, int delayMs = 50, Action<UIElement>? animationAction = null)
    {
        int index = 0;
        foreach (var child in children)
        {
            if (child is UIElement element)
            {
                element.Opacity = 0;
                element.Visibility = Visibility.Visible;

                var delay = TimeSpan.FromMilliseconds(index * delayMs);
                var timer = new System.Windows.Threading.DispatcherTimer
                {
                    Interval = delay
                };

                timer.Tick += (s, e) =>
                {
                    timer.Stop();

                    if (animationAction != null)
                    {
                        animationAction(element);
                    }
                    else
                    {
                        SlideAndFadeIn(element, 20, FastDuration);
                    }
                };

                timer.Start();
                index++;
            }
        }
    }

    /// <summary>
    /// Shake animation (for errors or attention)
    /// </summary>
    public static void Shake(UIElement element, double intensity = 10, int count = 3)
    {
        EnableHardwareAcceleration(element);

        if (element.RenderTransform is not TranslateTransform)
        {
            element.RenderTransform = new TranslateTransform();
        }

        var animation = new DoubleAnimationUsingKeyFrames();
        var duration = TimeSpan.FromMilliseconds(500);

        for (int i = 0; i <= count * 2; i++)
        {
            var time = TimeSpan.FromMilliseconds((duration.TotalMilliseconds / (count * 2)) * i);
            var value = i % 2 == 0 ? intensity : -intensity;

            if (i == count * 2)
                value = 0; // End at original position

            animation.KeyFrames.Add(new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(time),
                Value = value,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut }
            });
        }

        animation.Completed += (s, e) => DisableHardwareAcceleration(element);

        element.RenderTransform.BeginAnimation(TranslateTransform.XProperty, animation);
    }

    /// <summary>
    /// Pulse animation (subtle scale pulse)
    /// </summary>
    public static void Pulse(UIElement element, double toScale = 1.05)
    {
        EnableHardwareAcceleration(element);

        element.RenderTransformOrigin = new Point(0.5, 0.5);

        if (element.RenderTransform is not ScaleTransform)
        {
            element.RenderTransform = new ScaleTransform(1, 1);
        }

        var scaleXAnimation = new DoubleAnimation
        {
            From = 1.0,
            To = toScale,
            Duration = new Duration(TimeSpan.FromMilliseconds(200)),
            AutoReverse = true,
            EasingFunction = EaseOutCubic
        };

        scaleXAnimation.Completed += (s, e) => DisableHardwareAcceleration(element);

        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleXProperty, scaleXAnimation);
        element.RenderTransform.BeginAnimation(ScaleTransform.ScaleYProperty, scaleXAnimation);
    }

    /// <summary>
    /// Smooth color transition
    /// </summary>
    public static void AnimateColor(Animatable target, DependencyProperty property, Color toColor, Duration? duration = null, IEasingFunction? easing = null)
    {
        var animation = new ColorAnimation
        {
            To = toColor,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        target.BeginAnimation(property, animation);
    }

    /// <summary>
    /// Smooth number transition
    /// </summary>
    public static void AnimateDouble(Animatable target, DependencyProperty property, double to, Duration? duration = null, IEasingFunction? easing = null, EventHandler? completed = null)
    {
        var animation = new DoubleAnimation
        {
            To = to,
            Duration = duration ?? NormalDuration,
            EasingFunction = easing ?? EaseOutCubic
        };

        if (completed != null)
            animation.Completed += completed;

        target.BeginAnimation(property, animation);
    }
}
