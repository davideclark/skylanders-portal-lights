using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PortalLights; // For ElementType and FigureInfo
using System.Collections.Generic;

namespace PortalLights.WinUI.Services
{
    public class ThemeService
    {
        public Brush GenerateBackground(IReadOnlyList<FigureInfo> figures, double width)
        {
            if (figures.Count == 0)
            {
                return CreateDefaultBackground();
            }
            else if (figures.Count == 1)
            {
                return CreateSingleElementBrush(figures[0].Element);
            }
            else
            {
                // Blend two elements left-to-right
                return CreateBlendedBrush(figures[0].Element, figures[1].Element);
            }
        }

        private Brush CreateSingleElementBrush(ElementType element)
        {
            var colors = GetElementGradientColors(element);

            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            // Multi-stop gradient for depth
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.primary,
                Offset = 0.0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.secondary,
                Offset = 0.5
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.accent,
                Offset = 1.0
            });

            return brush;
        }

        private Brush CreateBlendedBrush(ElementType element1, ElementType element2)
        {
            var colors1 = GetElementGradientColors(element1);
            var colors2 = GetElementGradientColors(element2);

            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0.5),
                EndPoint = new Windows.Foundation.Point(1, 0.5) // Left to right
            };

            // Left side: Element 1
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors1.primary,
                Offset = 0.0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors1.secondary,
                Offset = 0.25
            });

            // Center: Blend zone
            brush.GradientStops.Add(new GradientStop
            {
                Color = BlendColors(colors1.accent, colors2.primary),
                Offset = 0.5
            });

            // Right side: Element 2
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors2.secondary,
                Offset = 0.75
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors2.accent,
                Offset = 1.0
            });

            return brush;
        }

        private (Color primary, Color secondary, Color accent) GetElementGradientColors(ElementType element)
        {
            return element switch
            {
                ElementType.Air => (
                    Color.FromArgb(255, 255, 255, 200),  // Light yellow
                    Color.FromArgb(255, 200, 230, 255),  // Sky blue
                    Color.FromArgb(255, 255, 255, 255)   // White
                ),

                ElementType.Fire => (
                    Color.FromArgb(255, 255, 100, 0),    // Orange
                    Color.FromArgb(255, 255, 50, 0),     // Red-orange
                    Color.FromArgb(255, 255, 200, 0)     // Yellow
                ),

                ElementType.Water => (
                    Color.FromArgb(255, 0, 100, 255),    // Deep blue
                    Color.FromArgb(255, 0, 200, 255),    // Cyan
                    Color.FromArgb(255, 100, 150, 255)   // Light blue
                ),

                ElementType.Life => (
                    Color.FromArgb(255, 0, 255, 0),      // Bright green
                    Color.FromArgb(255, 50, 200, 50),    // Medium green
                    Color.FromArgb(255, 150, 255, 150)   // Light green
                ),

                ElementType.Earth => (
                    Color.FromArgb(255, 139, 90, 43),    // Brown
                    Color.FromArgb(255, 200, 120, 0),    // Orange-brown
                    Color.FromArgb(255, 101, 67, 33)     // Dark brown
                ),

                ElementType.Undead => (
                    Color.FromArgb(255, 150, 0, 255),    // Purple
                    Color.FromArgb(255, 100, 0, 150),    // Dark purple
                    Color.FromArgb(255, 200, 100, 255)   // Light purple
                ),

                ElementType.Tech => (
                    Color.FromArgb(255, 255, 150, 0),    // Orange
                    Color.FromArgb(255, 255, 100, 0),    // Red-orange
                    Color.FromArgb(255, 255, 200, 100)   // Light orange
                ),

                ElementType.Magic => (
                    Color.FromArgb(255, 0, 255, 255),    // Cyan
                    Color.FromArgb(255, 0, 200, 200),    // Teal
                    Color.FromArgb(255, 150, 255, 255)   // Light cyan
                ),

                _ => (
                    Color.FromArgb(255, 200, 200, 200),  // Gray
                    Color.FromArgb(255, 150, 150, 150),
                    Color.FromArgb(255, 100, 100, 100)
                )
            };
        }

        private Color BlendColors(Color c1, Color c2)
        {
            return Color.FromArgb(
                255,
                (byte)((c1.R + c2.R) / 2),
                (byte)((c1.G + c2.G) / 2),
                (byte)((c1.B + c2.B) / 2)
            );
        }

        private Brush CreateDefaultBackground()
        {
            // No figures - subtle neutral gradient
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            brush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(255, 40, 40, 50),
                Offset = 0.0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = Color.FromArgb(255, 20, 20, 30),
                Offset = 1.0
            });

            return brush;
        }
    }
}
