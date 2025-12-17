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

            if (toEmit > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[AIR] EmitParticles: side={side}, toEmit={toEmit}, canvasWidth={canvasSize.Width}");
            }

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                float y = (float)(Random.Shared.NextDouble() * canvasSize.Height);
                float x = GetXPositionForSide(side, canvasSize.Width);

                // Determine horizontal velocity based on side
                float horizontalVelocity = side == ParticleSide.Right
                    ? -(float)(Random.Shared.NextDouble() * 60 + 40)  // Drift left for right side
                    : (float)(Random.Shared.NextDouble() * 60 + 40);   // Drift right for left/both

                particles.Add(new Particle
                {
                    Position = new Vector2(x, y),
                    Velocity = new Vector2(
                        horizontalVelocity,
                        (float)(Random.Shared.NextDouble() - 0.5) * 20  // Slight vertical drift
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 40 + 20), // 20-60 pixels (4x)
                    Opacity = (float)(Random.Shared.NextDouble() * 0.3 + 0.6), // 0.6-0.9 - much more visible
                    Life = 1.0f,
                    PhaseOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Scale = (float)side, // Store side info: 0=Left, 1=Right, 2=Both
                    Color = GetAirColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;
            float midpoint = (float)(canvasSize.Width * 0.5);
            // Fade distance: 3 seconds of travel at average speed (50 px/s) = ~150 pixels
            float fadeDistance = 200f;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Add wave motion
                var sineY = (float)Math.Sin(time * 2 + p.PhaseOffset) * 15;
                p.Position += new Vector2(0, sineY * deltaTime);
                p.Position += p.Velocity * deltaTime;

                // Calculate fade based on distance from boundary
                if (p.Scale == 2f) // Both - fade near right edge
                {
                    float distanceFromEdge = (float)canvasSize.Width - p.Position.X;
                    p.Life = Math.Min(1.0f, distanceFromEdge / fadeDistance);
                }
                else if (p.Scale == 0f) // Left side - fade near midpoint
                {
                    float distanceFromMidpoint = midpoint - p.Position.X;
                    p.Life = Math.Min(1.0f, distanceFromMidpoint / fadeDistance);
                }
                else // Right side (1f) - fade near midpoint
                {
                    float distanceFromMidpoint = p.Position.X - midpoint;
                    p.Life = Math.Min(1.0f, distanceFromMidpoint / fadeDistance);
                }

                // Check side boundaries (Scale stores side: 0=Left, 1=Right, 2=Both)
                bool shouldRemove = false;

                if (p.Scale == 2f) // Both - can cross entire screen
                {
                    shouldRemove = p.Position.X > canvasSize.Width + 20;
                }
                else if (p.Scale == 0f) // Left side
                {
                    shouldRemove = p.Position.X > midpoint || p.Position.X < -20;
                }
                else // Right side (1f)
                {
                    shouldRemove = p.Position.X > canvasSize.Width + 20 || p.Position.X < midpoint;
                }

                if (shouldRemove)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                // Apply fade-out effect based on Life (1.0 = full opacity, 0.0 = transparent)
                var opacity = p.Opacity * p.Life;
                var color = Color.FromArgb((byte)(opacity * 255), p.Color.R, p.Color.G, p.Color.B);

                // Draw cloud-like circular shape
                ds.FillEllipse(p.Position, p.Size, p.Size * 0.6f, color);
                ds.FillEllipse(new Vector2(p.Position.X + p.Size * 0.5f, p.Position.Y), p.Size * 0.7f, p.Size * 0.5f, color);
            }
        }

        private Color GetAirColor()
        {
            // Light cyan/blue for better contrast against yellow/white background
            return Color.FromArgb(255, 200, 230, 255);
        }

        private float GetXPositionForSide(ParticleSide side, double canvasWidth)
        {
            float result = side switch
            {
                // Left: spawn at left edge and drift right within left half
                ParticleSide.Left => -20f,
                // Right: spawn at right edge and drift left within right half
                ParticleSide.Right => (float)canvasWidth + 20f,
                // Both: spawn at far left and drift across full width
                _ => -20f
            };

            return result;
        }
    }
}
