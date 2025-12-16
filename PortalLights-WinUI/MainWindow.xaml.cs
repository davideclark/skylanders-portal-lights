using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Input;
using Microsoft.Graphics.Canvas.UI.Xaml;
using PortalLights.WinUI.Services;
using PortalLights.WinUI.Services.ParticleSystem;
using PortalLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PortalLights.WinUI
{
    public sealed partial class MainWindow : Window
    {
        private PortalService _portalService;
        private ThemeService _themeService;
        private ParticleEngine _particleEngine;
        private bool _isFullScreen = false;
        private bool _useRect1 = true; // Track which rectangle is currently visible
        private Storyboard _currentStoryboard; // Track current animation

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
            _particleEngine = new ParticleEngine(DispatcherQueue);
            _portalService = new PortalService(DispatcherQueue);
            _portalService.FiguresChanged += OnFiguresChanged;

            // Show loading message
            FigureInfoText.Text = "Scanning for portals...";

            // Set initial background on Rect1 immediately (no animation)
            BackgroundRect1.Fill = _themeService.GenerateBackground(new List<FigureInfo>(), RootGrid.ActualWidth);
            BackgroundRect1.Opacity = 1.0;
            BackgroundRect2.Opacity = 0.0;

            await _portalService.InitializeAsync();

            FigureInfoText.Text = "Place a Skylanders figure on the portal";

            // Start particle engine
            _particleEngine.Start();

            // Auto-hide instructions after 5 seconds
            await Task.Delay(5000);
            InstructionsPanel.Visibility = Visibility.Collapsed;
        }

        private void OnFiguresChanged(object sender, FiguresChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine($"MainWindow: OnFiguresChanged called with {e.Figures.Count} figures");
            if (e.Figures.Count > 0)
            {
                foreach (var fig in e.Figures)
                {
                    System.Diagnostics.Debug.WriteLine($"  - {fig.Name} ({fig.Element})");
                }
            }
            UpdateBackground(e.Figures);
            UpdateInfoText(e.Figures);
            System.Diagnostics.Debug.WriteLine($"Calling SetActiveElements with {e.Figures.Count} figures");
            _particleEngine.SetActiveElements(e.Figures);
        }

        private void UpdateBackground(IReadOnlyList<FigureInfo> figures)
        {
            System.Diagnostics.Debug.WriteLine($"UpdateBackground called with {figures.Count} figures");
            var brush = _themeService.GenerateBackground(figures, RootGrid.ActualWidth);

            // Stop any ongoing animation and reset state
            if (_currentStoryboard != null)
            {
                System.Diagnostics.Debug.WriteLine("  Stopping ongoing animation");
                _currentStoryboard.Stop();

                // Keep the source rect visible, hide the target that was fading in
                // This prevents the snap - we'll start the new animation from the currently visible gradient
                var prevTargetRect = _useRect1 ? BackgroundRect2 : BackgroundRect1;
                var prevSourceRect = _useRect1 ? BackgroundRect1 : BackgroundRect2;

                // Keep source visible, hide the incomplete target
                prevSourceRect.Opacity = 1.0;
                prevTargetRect.Opacity = 0.0;
                // Don't toggle _useRect1 - we'll reuse the same source for the next animation

                _currentStoryboard = null;
            }

            // Determine which rectangle to use for the new background
            var targetRect = _useRect1 ? BackgroundRect2 : BackgroundRect1;
            var sourceRect = _useRect1 ? BackgroundRect1 : BackgroundRect2;

            System.Diagnostics.Debug.WriteLine($"  Using Rect{(_useRect1 ? "2" : "1")} as target, Rect{(_useRect1 ? "1" : "2")} as source");
            System.Diagnostics.Debug.WriteLine($"  Source opacity before: {sourceRect.Opacity}, Target opacity before: {targetRect.Opacity}");
            System.Diagnostics.Debug.WriteLine($"  Source has fill: {sourceRect.Fill != null}, Target has fill: {targetRect.Fill != null}");

            // Set the new brush on the target rectangle
            targetRect.Fill = brush;
            targetRect.Opacity = 0.0; // Start hidden

            // Ensure target is on top so it fades in visibly over the source
            sourceRect.SetValue(Microsoft.UI.Xaml.Controls.Canvas.ZIndexProperty, 0);
            targetRect.SetValue(Microsoft.UI.Xaml.Controls.Canvas.ZIndexProperty, 1);

            System.Diagnostics.Debug.WriteLine($"  After setup - Source opacity: {sourceRect.Opacity}, Target opacity: {targetRect.Opacity}");

            // Cross-fade: fade in the new background while the old one stays visible
            var fadeIn = new DoubleAnimation
            {
                From = 0.0,
                To = 1.0,
                Duration = TimeSpan.FromMilliseconds(500),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut }
            };

            Storyboard.SetTarget(fadeIn, targetRect);
            Storyboard.SetTargetProperty(fadeIn, "Opacity");

            fadeIn.Completed += (s, args) =>
            {
                System.Diagnostics.Debug.WriteLine("  Animation completed");
                // After fade in completes, hide the old background
                sourceRect.Opacity = 0.0;
                // Toggle which rectangle we'll use next time
                _useRect1 = !_useRect1;
                _currentStoryboard = null;
            };

            var storyboard = new Storyboard();
            storyboard.Children.Add(fadeIn);
            _currentStoryboard = storyboard;
            System.Diagnostics.Debug.WriteLine("  Starting animation...");
            storyboard.Begin();
        }

        private void UpdateInfoText(IReadOnlyList<FigureInfo> figures)
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
                var namesWithElements = string.Join(" + ", figures.Select(f => $"{f.Name} ({f.Element})"));
                FigureInfoText.Text = namesWithElements;
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

        private void OnParticleCanvasLoaded(object sender, RoutedEventArgs e)
        {
            // Kick off the animation loop by invalidating the canvas
            if (sender is CanvasControl canvas)
            {
                System.Diagnostics.Debug.WriteLine($"ParticleCanvas Loaded - Size: {canvas.ActualWidth}x{canvas.ActualHeight}");
                canvas.Invalidate();
            }
        }

        private void OnParticleDraw(CanvasControl sender, CanvasDrawEventArgs args)
        {
            var size = new Size(sender.ActualWidth, sender.ActualHeight);
            System.Diagnostics.Debug.WriteLine($"OnParticleDraw called - Canvas size: {size.Width}x{size.Height}");

            _particleEngine?.Render(args.DrawingSession, size);
            sender.Invalidate(); // Request next frame for continuous animation
        }
    }
}
