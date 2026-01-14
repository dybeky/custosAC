using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CustosAC.WPF.Helpers;

namespace CustosAC.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to content changes for smooth slide + fade animation
        MainContentControl.TargetUpdated += AnimateContentChangeHandler;

        // Fix for Windows 10 rendering issues - force layout update after load
        Loaded += OnWindowLoaded;

        // Handle DPI changes for Windows 10
        SourceInitialized += OnSourceInitialized;

        // Enable hardware acceleration for main container
        AnimationHelper.EnableHardwareAcceleration(MainContentControl);
    }

    private void AnimateContentChangeHandler(object? sender, DataTransferEventArgs e)
    {
        AnimateContentChange();
    }

    private void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        // Force a layout update to fix potential rendering issues on Windows 10
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(() =>
        {
            // Invalidate visual and force re-render
            InvalidateVisual();
            UpdateLayout();

            // Ensure sidebar is properly rendered
            MainContentContainer?.InvalidateVisual();

            // Animate initial load with slight delay for better UX
            Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
            {
                if (MainContentControl.Content != null)
                {
                    AnimateContentChange();
                }
            }));
        }));
    }

    private void OnSourceInitialized(object? sender, EventArgs e)
    {
        // Apply DPI-aware rendering settings for Windows 10 compatibility
        var presentationSource = PresentationSource.FromVisual(this);
        if (presentationSource?.CompositionTarget != null)
        {
            var matrix = presentationSource.CompositionTarget.TransformToDevice;
            if (matrix.M11 > 1 || matrix.M22 > 1)
            {
                // High DPI detected - ensure proper scaling
                RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.HighQuality);
            }
        }
    }

    private void AnimateContentChange()
    {
        // Stop any ongoing animations first
        MainContentControl.BeginAnimation(OpacityProperty, null);
        if (MainContentControl.RenderTransform is TranslateTransform transform)
        {
            transform.BeginAnimation(TranslateTransform.YProperty, null);
        }

        // Enhanced animation: slide up + fade in for smoother transitions
        MainContentControl.Opacity = 0;

        // Ensure RenderTransform exists and reset position
        if (MainContentControl.RenderTransform is not TranslateTransform)
        {
            MainContentControl.RenderTransform = new TranslateTransform(0, 0);
        }

        var translateTransform = (TranslateTransform)MainContentControl.RenderTransform;
        translateTransform.Y = 30;

        // Fade animation with improved easing
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        // Slide animation (slide up from bottom)
        var slideIn = new DoubleAnimation
        {
            From = 30,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(350),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        MainContentControl.BeginAnimation(OpacityProperty, fadeIn);
        translateTransform.BeginAnimation(TranslateTransform.YProperty, slideIn);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        // Unsubscribe from all events to prevent memory leaks
        MainContentControl.TargetUpdated -= AnimateContentChangeHandler;
        Loaded -= OnWindowLoaded;
        SourceInitialized -= OnSourceInitialized;

        base.OnClosing(e);
    }
}
