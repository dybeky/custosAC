using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CustosAC.WPF;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        // Subscribe to content changes for fade animation
        MainContentControl.TargetUpdated += (s, e) =>
        {
            AnimateContentChange();
        };

        // Fix for Windows 10 rendering issues - force layout update after load
        Loaded += OnWindowLoaded;

        // Handle DPI changes for Windows 10
        SourceInitialized += OnSourceInitialized;
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
        var fadeIn = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };

        MainContentControl.BeginAnimation(OpacityProperty, fadeIn);
    }
}
