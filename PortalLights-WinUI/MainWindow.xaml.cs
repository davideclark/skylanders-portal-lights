using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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
using Windows.UI;
using Windows.UI.Text;

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

        // Test mode fields (for keyboard-driven particle testing)
        private Dictionary<ElementType, FigureInfo> _testFiguresLeft = new();
        private Dictionary<ElementType, FigureInfo> _testFiguresRight = new();
        private List<FigureInfo> _lastRealFigures = new();

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
                    System.Diagnostics.Debug.WriteLine($"  - {fig.Name} ({fig.Element}) on {fig.PortalName}");
                }
            }

            // Store real figures
            _lastRealFigures = e.Figures.ToList();

            // Merge with test figures and update
            RefreshParticlesWithTestFigures();
        }

        private void RefreshParticlesWithTestFigures()
        {
            // Merge real figures with test figures
            var allFigures = new List<FigureInfo>();
            allFigures.AddRange(_lastRealFigures);
            allFigures.AddRange(_testFiguresLeft.Values);
            allFigures.AddRange(_testFiguresRight.Values);

            System.Diagnostics.Debug.WriteLine($"[TEST MODE] RefreshParticles: {_lastRealFigures.Count} real + {_testFiguresLeft.Count} test-left + {_testFiguresRight.Count} test-right = {allFigures.Count} total");

            // Update all visual elements
            UpdateBackground(allFigures);
            UpdateFigurePanels(allFigures);
            _particleEngine.SetActiveElements(allFigures);
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

        private void UpdateFigurePanels(IReadOnlyList<FigureInfo> figures)
        {
            // Separate figures by portal product ID
            var pspcFigures = figures.Where(f => f.PortalProductId == 0x0150).ToList();
            var xboxFigures = figures.Where(f => f.PortalProductId == 0x1F17).ToList();

            System.Diagnostics.Debug.WriteLine($"UpdateFigurePanels: PS/PC={pspcFigures.Count}, Xbox={xboxFigures.Count}");

            PopulatePanel(LeftFigurePanel, pspcFigures);
            PopulatePanel(RightFigurePanel, xboxFigures);

            // Hide center panel if figures present, show if no figures
            if (figures.Count > 0)
            {
                InfoPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                FigureInfoText.Text = "Place a Skylanders figure on the portal";
                InfoPanel.Visibility = Visibility.Visible;
            }
        }

        private void PopulatePanel(StackPanel panel, List<FigureInfo> figures)
        {
            panel.Children.Clear();

            if (figures.Count == 0)
            {
                panel.Visibility = Visibility.Collapsed;
                return;
            }

            panel.Visibility = Visibility.Visible;

            foreach (var figure in figures)
            {
                var figureDisplay = CreateFigureDisplay(figure);
                panel.Children.Add(figureDisplay);
            }
        }

        private UIElement CreateFigureDisplay(FigureInfo figure)
        {
            var container = new StackPanel
            {
                Spacing = 8,
                Padding = new Thickness(10)
            };

            // Get element color for accent
            var (r, g, b) = FigureInfo.GetElementColor(figure.Element);
            var accentColor = Color.FromArgb(255, r, g, b);
            var accentBrush = new SolidColorBrush(accentColor);

            // Figure name with element color accent
            var nameText = new TextBlock
            {
                Text = figure.Name,
                Foreground = accentBrush,
                FontSize = 28,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };
            container.Children.Add(nameText);

            // Element type
            var elementText = new TextBlock
            {
                Text = figure.Element.ToString(),
                Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                FontSize = 18
            };
            container.Children.Add(elementText);

            // Show stats if decryption succeeded
            if (figure.DecryptionSucceeded && figure.Level.HasValue)
            {
                // Level display
                var levelText = new TextBlock
                {
                    Text = figure.GetLevelDisplay(),
                    Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255)),
                    FontSize = 22,
                    FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
                    Margin = new Thickness(0, 8, 0, 0)
                };
                container.Children.Add(levelText);

                // Experience display with progress bar
                if (figure.Experience.HasValue && figure.MaxExperience.HasValue)
                {
                    var xpText = new TextBlock
                    {
                        Text = figure.GetExperienceDisplay(),
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        FontSize = 16
                    };
                    container.Children.Add(xpText);

                    // Progress bar
                    var progressBar = new ProgressBar
                    {
                        Value = figure.GetExperienceProgress() * 100,
                        Maximum = 100,
                        Height = 8,
                        Width = 200,
                        Foreground = accentBrush,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    container.Children.Add(progressBar);
                }

                // Gold display
                if (figure.Gold.HasValue)
                {
                    var goldText = new TextBlock
                    {
                        Text = $"Gold: {figure.Gold.Value}",
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 215, 0)),
                        FontSize = 16,
                        Margin = new Thickness(0, 4, 0, 0)
                    };
                    container.Children.Add(goldText);
                }

                // Playtime display
                if (figure.PlaytimeSeconds.HasValue)
                {
                    var hours = figure.PlaytimeSeconds.Value / 3600;
                    var minutes = (figure.PlaytimeSeconds.Value % 3600) / 60;
                    var playtimeText = new TextBlock
                    {
                        Text = $"Playtime: {hours}h {minutes}m",
                        Foreground = new SolidColorBrush(Color.FromArgb(200, 255, 255, 255)),
                        FontSize = 16
                    };
                    container.Children.Add(playtimeText);
                }
            }
            // Note: "Reading stats..." message disabled since decryption is currently disabled
            // else if (!figure.DecryptionSucceeded && figure.Level == null)
            // {
            //     var loadingText = new TextBlock
            //     {
            //         Text = "Reading stats...",
            //         Foreground = new SolidColorBrush(Color.FromArgb(150, 255, 255, 255)),
            //         FontSize = 14,
            //         FontStyle = Windows.UI.Text.FontStyle.Italic,
            //         Margin = new Thickness(0, 4, 0, 0)
            //     };
            //     container.Children.Add(loadingText);
            // }

            return container;
        }

        private void ToggleTestElement(ElementType element, bool isRightSide)
        {
            var targetDict = isRightSide ? _testFiguresRight : _testFiguresLeft;
            var portalId = isRightSide ? 0x1F17 : 0x0150;
            var portalName = isRightSide ? "Test (Right)" : "Test (Left)";

            if (targetDict.ContainsKey(element))
            {
                // Toggle OFF - remove from test figures
                targetDict.Remove(element);
                System.Diagnostics.Debug.WriteLine($"[TEST MODE] Removed {element} from {(isRightSide ? "right" : "left")} side");
            }
            else
            {
                // Toggle ON - add to test figures
                var testFigure = new FigureInfo(
                    name: $"Test {element}",
                    element: element,
                    portalProductId: portalId,
                    portalName: portalName
                );
                targetDict[element] = testFigure;
                System.Diagnostics.Debug.WriteLine($"[TEST MODE] Added {element} to {(isRightSide ? "right" : "left")} side");
            }

            // Refresh particle engine with combined figures
            RefreshParticlesWithTestFigures();
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
                else
                {
                    // Check for Ctrl modifier using keyboard state
                    var ctrlState = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control);
                    bool isCtrlPressed = (ctrlState & Windows.UI.Core.CoreVirtualKeyStates.Down) == Windows.UI.Core.CoreVirtualKeyStates.Down;

                    // Function key mapping (F1-F8 = VirtualKey.F1 through F8)
                    ElementType? targetElement = e.Key switch
                    {
                        Windows.System.VirtualKey.F1 => ElementType.Magic,
                        Windows.System.VirtualKey.F2 => ElementType.Water,
                        Windows.System.VirtualKey.F3 => ElementType.Fire,
                        Windows.System.VirtualKey.F4 => ElementType.Life,
                        Windows.System.VirtualKey.F5 => ElementType.Earth,
                        Windows.System.VirtualKey.F6 => ElementType.Air,
                        Windows.System.VirtualKey.F7 => ElementType.Undead,
                        Windows.System.VirtualKey.F8 => ElementType.Tech,
                        _ => null
                    };

                    if (targetElement.HasValue)
                    {
                        ToggleTestElement(targetElement.Value, isCtrlPressed);
                        e.Handled = true;
                    }
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
            _particleEngine?.Render(args.DrawingSession, size);
            sender.Invalidate(); // Request next frame for continuous animation
        }
    }
}
