using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using Custos.WPF.ViewModels;

namespace Custos.WPF;

public partial class SplashWindow : Window
{
    private readonly SplashViewModel _viewModel;

    public bool ShouldExitApp { get; set; }

    public SplashWindow(SplashViewModel viewModel)
    {
        InitializeComponent();

        _viewModel = viewModel;
        DataContext = viewModel;

        // Subscribe to close request from ViewModel
        _viewModel.CloseRequested = shouldExit =>
        {
            if (shouldExit)
                CloseWithUpdate();
            else
                CloseNormally();
        };

        Loaded += OnLoaded;
        MouseLeftButtonDown += OnMouseLeftButtonDown;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Fade in
            Opacity = 0;
            var fadeIn = new DoubleAnimation
            {
                From = 0,
                To = 1,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            BeginAnimation(OpacityProperty, fadeIn);

            // Start indeterminate animation
            if (TryFindResource("IndeterminateProgress") is Storyboard storyboard)
            {
                storyboard.Begin();
            }

            // Run initialization and get result
            var result = await _viewModel.InitializeAndWaitAsync();

            // Set result and close
            ShouldExitApp = result.ShouldExit;

            if (!result.ShowUpdatePanel)
            {
                DialogResult = true;
            }
            // If update panel is shown, wait for user action (buttons will close)
        }
        catch
        {
            ShouldExitApp = false;
            DialogResult = true;
        }
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
            DragMove();
    }

    // Called from XAML buttons
    public void CloseWithUpdate()
    {
        ShouldExitApp = true;
        DialogResult = true;
    }

    public void CloseNormally()
    {
        ShouldExitApp = false;
        DialogResult = true;
    }

    protected override void OnClosed(EventArgs e)
    {
        MouseLeftButtonDown -= OnMouseLeftButtonDown;
        Loaded -= OnLoaded;
        base.OnClosed(e);
    }
}
