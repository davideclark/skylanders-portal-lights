using Microsoft.UI.Xaml.Media;
using Windows.UI;
using PortalLibrary; // For ElementType and FigureInfo
using System.Collections.Generic;
using System.Linq;

namespace PortalLights.WinUI.Services
{
    public class ThemeService
    {
        public Brush GenerateBackground(IReadOnlyList<FigureInfo> figures, double width)
        {
            if (figures.Count == 0)
                return CreateDefaultBackground();
            else if (figures.Count == 1)
                return CreateSingleElementBrush(figures[0].Element);
            else if (figures.Count == 2)
                return CreateBlendedBrush(figures[0].Element, figures[1].Element);
            else if (figures.Count == 3)
                return CreateTripleBlend(figures[0].Element, figures[1].Element, figures[2].Element);
            else if (figures.Count == 4)
                return CreateQuadrantBlend(figures);
            else
                return CreateRadialBlendAll(figures);
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

        private Brush CreateTripleBlend(ElementType e1, ElementType e2, ElementType e3)
        {
            var c1 = GetElementGradientColors(e1);
            var c2 = GetElementGradientColors(e2);
            var c3 = GetElementGradientColors(e3);

            // Create diagonal gradient with all three elements
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0),
                EndPoint = new Windows.Foundation.Point(1, 1)
            };

            brush.GradientStops.Add(new GradientStop { Color = c1.primary, Offset = 0.0 });
            brush.GradientStops.Add(new GradientStop { Color = BlendColors(c1.secondary, c2.primary), Offset = 0.33 });
            brush.GradientStops.Add(new GradientStop { Color = BlendColors(c2.secondary, c3.primary), Offset = 0.66 });
            brush.GradientStops.Add(new GradientStop { Color = c3.accent, Offset = 1.0 });

            return brush;
        }

        private Brush CreateQuadrantBlend(IReadOnlyList<FigureInfo> figures)
        {
            var colors = figures.Select(f => GetElementGradientColors(f.Element)).ToArray();

            // Create complex gradient with all 4 corners represented
            var brush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0.5),
                EndPoint = new Windows.Foundation.Point(1, 0.5)
            };

            brush.GradientStops.Add(new GradientStop { Color = colors[0].primary, Offset = 0.0 });
            brush.GradientStops.Add(new GradientStop { Color = BlendMultiple(colors[0].secondary, colors[2].primary), Offset = 0.25 });
            brush.GradientStops.Add(new GradientStop { Color = BlendMultiple(colors[0].accent, colors[1].primary, colors[2].secondary, colors[3].primary), Offset = 0.5 });
            brush.GradientStops.Add(new GradientStop { Color = BlendMultiple(colors[1].secondary, colors[3].secondary), Offset = 0.75 });
            brush.GradientStops.Add(new GradientStop { Color = colors[1].accent, Offset = 1.0 });

            return brush;
        }

        private Brush CreateRadialBlendAll(IReadOnlyList<FigureInfo> figures)
        {
            // For 5+ figures, blend all colors into a unified gradient
            var allColors = figures.SelectMany(f =>
            {
                var colors = GetElementGradientColors(f.Element);
                return new[] { colors.primary, colors.secondary, colors.accent };
            }).ToArray();

            var blendedCenter = BlendMultiple(allColors);
            var blendedOuter = BlendMultiple(figures.Select(f => GetElementGradientColors(f.Element).accent).ToArray());

            var brush = new RadialGradientBrush
            {
                Center = new Windows.Foundation.Point(0.5, 0.5),
                RadiusX = 0.8,
                RadiusY = 0.8
            };

            brush.GradientStops.Add(new GradientStop { Color = blendedCenter, Offset = 0.0 });
            brush.GradientStops.Add(new GradientStop { Color = blendedOuter, Offset = 1.0 });

            return brush;
        }

        private Color BlendMultiple(params Color[] colors)
        {
            if (colors.Length == 0) return Color.FromArgb(255, 128, 128, 128);

            int r = 0, g = 0, b = 0;
            foreach (var c in colors)
            {
                r += c.R;
                g += c.G;
                b += c.B;
            }

            return Color.FromArgb(255,
                (byte)(r / colors.Length),
                (byte)(g / colors.Length),
                (byte)(b / colors.Length));
        }
    }
}
