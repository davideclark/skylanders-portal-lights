using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class MagicParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 240;
        private const float EMISSION_RATE = 20.0f;
        private float _emissionAccumulator = 0.0f;

        public void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime, ParticleSide side)
        {
            _emissionAccumulator += EMISSION_RATE * deltaTime;
            int toEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= toEmit;

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                float x = GetXPositionForSide(side, canvasSize.Width);

                particles.Add(new Particle
                {
                    Position = new Vector2(x, (float)(Random.Shared.NextDouble() * canvasSize.Height)),
                    Velocity = Vector2.Zero, // Stationary sparkles
                    Size = (float)(Random.Shared.NextDouble() * 24 + 8), // 8-32 pixels (8x - twice as big)
                    Opacity = 0.0f, // Start invisible
                    Life = 1.0f,
                    PhaseOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Scale = 0.0f,
                    Color = GetMagicColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                p.Life -= deltaTime * 0.8f; // 1.25 second lifetime

                // Twinkle animation: fade in, stay, fade out
                if (p.Life > 0.7f)
                {
                    // Fade in (first 30% of life)
                    p.Opacity = (1.0f - p.Life) / 0.3f * 0.9f;
                    p.Scale = (1.0f - p.Life) / 0.3f;
                }
                else if (p.Life > 0.3f)
                {
                    // Stay bright (middle 40% of life)
                    p.Opacity = 0.9f + (float)Math.Sin(time * 8 + p.PhaseOffset) * 0.1f;
                    p.Scale = 1.0f;
                }
                else
                {
                    // Fade out (last 30% of life)
                    p.Opacity = p.Life / 0.3f * 0.9f;
                    p.Scale = p.Life / 0.3f;
                }

                if (p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);
                var size = p.Size * p.Scale;

                // Draw star shape (simple cross)
                ds.DrawLine(
                    new Vector2(p.Position.X - size, p.Position.Y),
                    new Vector2(p.Position.X + size, p.Position.Y),
                    color, 2.0f
                );
                ds.DrawLine(
                    new Vector2(p.Position.X, p.Position.Y - size),
                    new Vector2(p.Position.X, p.Position.Y + size),
                    color, 2.0f
                );

                // Add glow
                ds.FillEllipse(p.Position, size * 0.6f, size * 0.6f,
                    Color.FromArgb((byte)(p.Opacity * 100), p.Color.R, p.Color.G, p.Color.B));
            }
        }

        private Color GetMagicColor()
        {
            return Color.FromArgb(255, 255, 100, 255); // Bright purple/magenta
        }

        private float GetXPositionForSide(ParticleSide side, double canvasWidth)
        {
            return side switch
            {
                ParticleSide.Left => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5),
                ParticleSide.Right => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5 + canvasWidth * 0.5),
                _ => (float)(Random.Shared.NextDouble() * canvasWidth)
            };
        }
    }
}
