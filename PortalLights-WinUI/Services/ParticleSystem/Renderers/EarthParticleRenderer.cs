using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class EarthParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 100;
        private const float EMISSION_RATE = 7.0f;
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
                        0 // Start at top
                    ),
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 15,
                        (float)(Random.Shared.NextDouble() * 60 + 30) // Downward
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 5 + 2), // 2-7 pixels
                    Opacity = (float)(Random.Shared.NextDouble() * 0.6 + 0.3), // 0.3-0.9
                    Life = 1.0f,
                    Rotation = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Color = GetEarthColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Apply gravity
                p.Velocity += new Vector2(0, 150 * deltaTime);
                p.Position += p.Velocity * deltaTime;
                p.Rotation += deltaTime * 2.0f;

                p.Life -= deltaTime * 0.6f; // ~1.7 second lifetime

                // Remove when hitting bottom or life depleted
                if (p.Position.Y > canvasSize.Height + 20 || p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);

                // Draw rocky particles (rotated rectangles)
                var transform = Matrix3x2.CreateRotation(p.Rotation, p.Position);
                ds.Transform = transform;
                ds.FillRectangle(new Rect(-p.Size / 2, -p.Size / 2, p.Size, p.Size), color);
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private Color GetEarthColor()
        {
            var r = Random.Shared.Next(2);
            return r switch
            {
                0 => Color.FromArgb(255, 139, 90, 43),   // Brown
                _ => Color.FromArgb(255, 101, 67, 33)    // Dark brown
            };
        }
    }
}
