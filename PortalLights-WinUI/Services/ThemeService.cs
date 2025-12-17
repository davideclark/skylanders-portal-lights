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
                StartPoint = new Windows.Foundation.Point(0, 0.5),
                EndPoint = new Windows.Foundation.Point(1, 0.5) // Horizontal gradient
            };

            // Horizontal gradient: light on left, darkest on right (where figure is)
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.accent, // Lightest
                Offset = 0.0
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.primary, // Bright/vibrant
                Offset = 0.5
            });
            brush.GradientStops.Add(new GradientStop
            {
                Color = colors.secondary, // Darkest/most saturated
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
                    Color.FromArgb(255, 0, 200, 255),    // Cyan (bright)
                    Color.FromArgb(255, 0, 100, 255),    // Deep blue (darkest)
                    Color.FromArgb(255, 100, 150, 255)   // Light blue (lightest)
                ),

                ElementType.Life => (
                    Color.FromArgb(255, 0, 255, 0),      // Bright green
                    Color.FromArgb(255, 50, 200, 50),    // Medium green
                    Color.FromArgb(255, 150, 255, 150)   // Light green
                ),

                ElementType.Earth => (
                    Color.FromArgb(255, 139, 90, 43),    // Brown (medium)
                    Color.FromArgb(255, 101, 67, 33),    // Dark brown (darkest)
                    Color.FromArgb(255, 200, 120, 0)     // Orange-brown (lightest)
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
            // Group figures by portal for cleaner blending
            var leftPortalFigures = figures.Where(f => f.PortalProductId == 0x0150).ToList();
            var rightPortalFigures = figures.Where(f => f.PortalProductId == 0x1F17).ToList();

            // If all figures on same portal, use simpler gradient
            if (leftPortalFigures.Count == 0 || rightPortalFigures.Count == 0)
            {
                // All on one portal - blend all elements
                var allColors = figures.Select(f => GetElementGradientColors(f.Element)).ToArray();
                var leftColor = allColors[0].primary;
                var centerColor = BlendMultiple(allColors.Select(c => c.secondary).ToArray());
                var rightColor = allColors[allColors.Length - 1].accent;

                var brush = new LinearGradientBrush
                {
                    StartPoint = new Windows.Foundation.Point(0, 0.5),
                    EndPoint = new Windows.Foundation.Point(1, 0.5)
                };

                brush.GradientStops.Add(new GradientStop { Color = leftColor, Offset = 0.0 });
                brush.GradientStops.Add(new GradientStop { Color = centerColor, Offset = 0.5 });
                brush.GradientStops.Add(new GradientStop { Color = rightColor, Offset = 1.0 });

                return brush;
            }

            // Portal-based gradient: left portal on left, right portal on right
            var leftColors = leftPortalFigures.Select(f => GetElementGradientColors(f.Element)).ToArray();
            var rightColors = rightPortalFigures.Select(f => GetElementGradientColors(f.Element)).ToArray();

            // Blend colors for each portal
            var leftPrimary = BlendMultiple(leftColors.Select(c => c.primary).ToArray());
            var leftSecondary = BlendMultiple(leftColors.Select(c => c.secondary).ToArray());
            var rightPrimary = BlendMultiple(rightColors.Select(c => c.primary).ToArray());
            var rightSecondary = BlendMultiple(rightColors.Select(c => c.secondary).ToArray());

            var portalBrush = new LinearGradientBrush
            {
                StartPoint = new Windows.Foundation.Point(0, 0.5),
                EndPoint = new Windows.Foundation.Point(1, 0.5)
            };

            // Left side: vibrant color at edge, fade through secondary
            portalBrush.GradientStops.Add(new GradientStop { Color = leftPrimary, Offset = 0.0 });
            portalBrush.GradientStops.Add(new GradientStop { Color = leftSecondary, Offset = 0.25 });

            // Center: darker neutral zone to avoid muddy blends
            // Desaturate the colors for a cleaner transition
            var centerLeft = Color.FromArgb(255,
                (byte)(leftSecondary.R * 0.4),
                (byte)(leftSecondary.G * 0.4),
                (byte)(leftSecondary.B * 0.4));
            var centerRight = Color.FromArgb(255,
                (byte)(rightPrimary.R * 0.4),
                (byte)(rightPrimary.G * 0.4),
                (byte)(rightPrimary.B * 0.4));

            portalBrush.GradientStops.Add(new GradientStop { Color = centerLeft, Offset = 0.45 });
            portalBrush.GradientStops.Add(new GradientStop { Color = centerRight, Offset = 0.55 });

            // Right side: fade through primary (brighter), darkest color at edge
            portalBrush.GradientStops.Add(new GradientStop { Color = rightPrimary, Offset = 0.75 });
            portalBrush.GradientStops.Add(new GradientStop { Color = rightSecondary, Offset = 1.0 });

            return portalBrush;
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
