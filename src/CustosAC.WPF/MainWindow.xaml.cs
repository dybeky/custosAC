using System.Windows;
using System.Windows.Media.Animation;

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
