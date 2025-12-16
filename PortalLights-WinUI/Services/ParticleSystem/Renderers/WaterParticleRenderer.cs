using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class WaterParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 100;
        private const float EMISSION_RATE = 5.0f;
        private float _emissionAccumulator = 0.0f;
        private List<Ripple> _ripples = new();

        private class Ripple
        {
            public Vector2 Position { get; set; }
            public float Radius { get; set; }
            public float Life { get; set; }
        }

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
                        (float)(Random.Shared.NextDouble() - 0.5) * 10,
                        (float)(Random.Shared.NextDouble() * 100 + 80) // Downward
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 4 + 2), // 2-6 pixels
                    Opacity = 0.7f,
                    Life = 1.0f,
                    Color = Color.FromArgb(255, 100, 180, 255) // Light blue
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Apply gravity
                p.Velocity += new Vector2(0, 200 * deltaTime);
                p.Position += p.Velocity * deltaTime;

                p.Life -= deltaTime * 0.8f;

                // Create ripple when hitting bottom
                if (p.Position.Y >= canvasSize.Height - 10)
                {
                    _ripples.Add(new Ripple
                    {
                        Position = p.Position,
                        Radius = 0,
                        Life = 1.0f
                    });
                    particles.RemoveAt(i);
                }
                else if (p.Life <= 0)
                {
                    particles.RemoveAt(i);
                }
            }

            // Update ripples
            for (int i = _ripples.Count - 1; i >= 0; i--)
            {
                var r = _ripples[i];
                r.Radius += deltaTime * 80;
                r.Life -= deltaTime * 2.0f;

                if (r.Life <= 0)
                    _ripples.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            // Draw droplets
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), 100, 180, 255);
                ds.FillEllipse(p.Position, p.Size, p.Size * 1.5f, color); // Elongated
            }

            // Draw ripples
            foreach (var r in _ripples)
            {
                var opacity = (byte)(r.Life * 100);
                var color = Color.FromArgb(opacity, 100, 200, 255);
                ds.DrawCircle(r.Position, r.Radius, color, 2.0f);
            }
        }
    }
}
