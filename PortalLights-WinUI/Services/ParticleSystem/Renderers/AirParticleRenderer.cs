using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class AirParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 90;
        private const float EMISSION_RATE = 6.0f;
        private float _emissionAccumulator = 0.0f;

        public void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime, ParticleSide side)
        {
            _emissionAccumulator += EMISSION_RATE * deltaTime;
            int toEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= toEmit;

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                float y = GetYPositionForSide(side, canvasSize.Height);

                particles.Add(new Particle
                {
                    Position = new Vector2(-20, y), // Start from left edge
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() * 60 + 40), // Horizontal movement
                        (float)(Random.Shared.NextDouble() - 0.5) * 20  // Slight vertical drift
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 40 + 20), // 20-60 pixels (4x)
                    Opacity = (float)(Random.Shared.NextDouble() * 0.4 + 0.2), // 0.2-0.6
                    Life = 1.0f,
                    PhaseOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Color = GetAirColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Add wave motion
                var sineY = (float)Math.Sin(time * 2 + p.PhaseOffset) * 15;
                p.Position += new Vector2(0, sineY * deltaTime);
                p.Position += p.Velocity * deltaTime;

                p.Life -= deltaTime * 0.4f; // 2.5 second lifetime
                p.Opacity = p.Life * 0.5f;

                // Remove when off-screen
                if (p.Position.X > canvasSize.Width + 20 || p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);

                // Draw cloud-like circular shape
                ds.FillEllipse(p.Position, p.Size, p.Size * 0.6f, color);
                ds.FillEllipse(new Vector2(p.Position.X + p.Size * 0.5f, p.Position.Y), p.Size * 0.7f, p.Size * 0.5f, color);
            }
        }

        private Color GetAirColor()
        {
            // White/light yellow for air/clouds
            return Color.FromArgb(255, 245, 245, 255);
        }

        private float GetYPositionForSide(ParticleSide side, double canvasHeight)
        {
            return side switch
            {
                ParticleSide.Left => (float)(Random.Shared.NextDouble() * canvasHeight * 0.5),
                ParticleSide.Right => (float)(Random.Shared.NextDouble() * canvasHeight * 0.5 + canvasHeight * 0.5),
                _ => (float)(Random.Shared.NextDouble() * canvasHeight)
            };
        }
    }
}
