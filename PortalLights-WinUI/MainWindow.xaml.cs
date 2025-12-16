using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using PortalLights.WinUI.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortalLights.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private PortalService _portalService;
        private ThemeService _themeService;
        private bool _isFullScreen = false;

        public MainWindow()
        {
            this.InitializeComponent();
            InitializeServices();
            SetupKeyboardHandlers();
            EnterFullScreen();
        }

        private async void InitializeServices()
        {
            _themeService = new ThemeService();
            _portalService = new PortalService(DispatcherQueue);
            _portalService.FiguresChanged += OnFiguresChanged;

            // Show loading message
            FigureInfoText.Text = "Scanning for portals...";

            await _portalService.InitializeAsync();

            // Initial background (no figures)
            UpdateBackground(new List<PortalLights.FigureInfo>());
            FigureInfoText.Text = "Place a Skylanders figure on the portal";

            // Auto-hide instructions after 5 seconds
            await Task.Delay(5000);
            InstructionsPanel.Visibility = Visibility.Collapsed;
        }

        private void OnFiguresChanged(object sender, FiguresChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow: OnFiguresChanged called with {e.Figures.Count} figures");
            UpdateBackground(e.Figures);
            UpdateInfoText(e.Figures);
        }

        private void UpdateBackground(IReadOnlyList<PortalLights.FigureInfo> figures)
        {
            var brush = _themeService.GenerateBackground(figures, RootGrid.ActualWidth);

            // Smooth transition
            var storyboard = new Storyboard();
            var fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromMilliseconds(250)
            };
            Storyboard.SetTarget(fadeOut, BackgroundRect);
            Storyboard.SetTargetProperty(fadeOut, "Opacity");

            fadeOut.Completed += (s, args) =>
            {
                BackgroundRect.Fill = brush;

                var fadeIn = new DoubleAnimation
                {
                    From = 0.0,
                    To = 1.0,
                    Duration = TimeSpan.FromMilliseconds(250)
                };
                Storyboard.SetTarget(fadeIn, BackgroundRect);
                Storyboard.SetTargetProperty(fadeIn, "Opacity");

                var fadeInStoryboard = new Storyboard();
                fadeInStoryboard.Children.Add(fadeIn);
                fadeInStoryboard.Begin();
            };

            storyboard.Children.Add(fadeOut);
            storyboard.Begin();
        }

        private void UpdateInfoText(IReadOnlyList<PortalLights.FigureInfo> figures)
        {
            if (figures.Count == 0)
            {
                FigureInfoText.Text = "Place a Skylanders figure on the portal";
            }
            else if (figures.Count == 1)
            {
                FigureInfoText.Text = $"{figures[0].Name} ({figures[0].Element})";
            }
            else
            {
                var names = string.Join(" + ", figures.Select(f => f.Name));
                FigureInfoText.Text = names;
            }
        }

        private void SetupKeyboardHandlers()
        {
            RootGrid.KeyDown += (s, e) =>
            {
                if (e.Key == Windows.System.VirtualKey.Escape)
                {
                    Application.Current.Exit();
                }
                else if (e.Key == Windows.System.VirtualKey.F11)
                {
                    ToggleFullScreen();
                }
                else if (e.Key == Windows.System.VirtualKey.I)
                {
                    InfoPanel.Visibility = InfoPanel.Visibility == Visibility.Visible
                        ? Visibility.Collapsed
                        : Visibility.Visible;
                }
            };

            RootGrid.Focus(FocusState.Programmatic);
        }

        private void EnterFullScreen()
        {
            var appWindow = this.AppWindow;
            appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
            _isFullScreen = true;
        }

        private void ToggleFullScreen()
        {
            var appWindow = this.AppWindow;
            if (_isFullScreen)
            {
                appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.Default);
                _isFullScreen = false;
            }
            else
            {
                appWindow.SetPresenter(Microsoft.UI.Windowing.AppWindowPresenterKind.FullScreen);
                _isFullScreen = true;
            }
        }
    }
}
