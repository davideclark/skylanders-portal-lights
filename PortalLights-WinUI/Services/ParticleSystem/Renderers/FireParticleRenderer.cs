using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class FireParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 150;
        private const float EMISSION_RATE = 8.0f; // particles per second
        private float _emissionAccumulator = 0.0f;

        public void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime)
        {
            _emissionAccumulator += EMISSION_RATE * deltaTime;
            int toEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= toEmit;

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                particles.Add(new Particle
                {
                    Position = new Vector2(
                        (float)(Random.Shared.NextDouble() * canvasSize.Width),
                        (float)canvasSize.Height // Start at bottom
                    ),
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 20, // Slight horizontal drift
                        -(float)(Random.Shared.NextDouble() * 80 + 40)  // Upward movement
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 6 + 3), // 3-9 pixels
                    Opacity = (float)Random.Shared.NextDouble() * 0.6f + 0.3f, // 0.3-0.9
                    Life = 1.0f,
                    Color = GetFireColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Physics
                p.Position += p.Velocity * deltaTime;
                p.Velocity *= 0.98f; // Damping

                // Fade out over lifetime
                p.Life -= deltaTime * 0.5f; // 2 second lifetime
                p.Opacity = p.Life * 0.7f;
                p.Size *= 1.02f; // Grow slightly

                if (p.Life <= 0 || p.Position.Y < -50)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb(
                    (byte)(p.Opacity * 255),
                    p.Color.R, p.Color.G, p.Color.B
                );

                // Create radial gradient for glow effect
                var glowColor = Color.FromArgb((byte)(p.Opacity * 100), p.Color.R, p.Color.G, p.Color.B);

                // Draw glow
                ds.FillEllipse(p.Position, p.Size * 1.5f, p.Size * 1.5f, glowColor);

                // Draw core
                ds.FillEllipse(p.Position, p.Size, p.Size, color);
            }
        }

        private Color GetFireColor()
        {
            // Random fire colors: red, orange, yellow
            var r = Random.Shared.Next(3);
            return r switch
            {
                0 => Color.FromArgb(255, 255, 100, 0),   // Orange
                1 => Color.FromArgb(255, 255, 50, 0),    // Red-orange
                _ => Color.FromArgb(255, 255, 200, 50)   // Yellow
            };
        }
    }
}
