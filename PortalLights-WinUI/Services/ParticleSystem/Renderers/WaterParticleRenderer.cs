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
        private const int MAX_PARTICLES = 150;
        private const float EMISSION_RATE = 8.0f;
        private float _emissionAccumulator = 0.0f;
        private List<Ripple> _ripples = new();

        private class Ripple
        {
            public Vector2 Position { get; set; }
            public float Radius { get; set; }
            public float Life { get; set; }
        }

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
                    Position = new Vector2(x, 0), // Start at top
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 10,
                        (float)(Random.Shared.NextDouble() * 100 + 80) // Downward
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 12 + 6), // 6-18 pixels - smaller droplets
                    Opacity = 0.9f, // Brighter
                    Life = 1.0f,
                    Color = Color.FromArgb(255, 200, 240, 255) // Bright cyan/white
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

                // Apply air resistance/drag - smaller droplets have more drag, fall slower
                // Larger droplets have less drag, fall faster and stretch more
                float dragCoefficient = 36.0f / p.Size; // Inverse relationship: smaller = more drag
                float drag = p.Velocity.Y * dragCoefficient * deltaTime;
                p.Velocity -= new Vector2(0, drag);

                p.Position += p.Velocity * deltaTime;

                // Create ripple and recycle droplet when hitting bottom
                if (p.Position.Y >= canvasSize.Height - 10)
                {
                    _ripples.Add(new Ripple
                    {
                        Position = new Vector2(p.Position.X, (float)canvasSize.Height),
                        Radius = 0,
                        Life = 1.0f
                    });

                    // Recycle droplet - move back to top with new random X position
                    p.Position = new Vector2(p.Position.X, 0);
                    p.Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 10,
                        (float)(Random.Shared.NextDouble() * 100 + 80)
                    );
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
            // Draw droplets - stretch based on velocity
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), 200, 240, 255);

                // Stretch factor based on falling velocity (Y velocity)
                // Cap the stretch to create realistic teardrop shapes, not extreme streaks
                float velocityFactor = Math.Min(2.5f, Math.Max(1.0f, p.Velocity.Y / 150.0f));
                float stretchHeight = p.Size * velocityFactor;

                ds.FillEllipse(p.Position, p.Size, stretchHeight, color);
            }

            // Draw ripples - brighter and thicker
            foreach (var r in _ripples)
            {
                var opacity = (byte)(r.Life * 180); // Brighter ripples
                var color = Color.FromArgb(opacity, 200, 240, 255);
                ds.DrawCircle(r.Position, r.Radius, color, 3.0f); // Thicker stroke
            }
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
