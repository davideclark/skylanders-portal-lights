using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class TechParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 110;
        private const float EMISSION_RATE = 9.0f;
        private float _emissionAccumulator = 0.0f;

        public void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime, ParticleSide side)
        {
            _emissionAccumulator += EMISSION_RATE * deltaTime;
            int toEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= toEmit;

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                // Emit from random edges
                var edge = Random.Shared.Next(4);
                Vector2 position, velocity;

                switch (edge)
                {
                    case 0: // Top
                        position = new Vector2(GetXPositionForSide(side, canvasSize.Width), 0);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, (float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    case 1: // Right
                        position = new Vector2((float)canvasSize.Width, (float)(Random.Shared.NextDouble() * canvasSize.Height));
                        velocity = new Vector2(-(float)(Random.Shared.NextDouble() * 80 + 40), (float)(Random.Shared.NextDouble() - 0.5) * 100);
                        break;
                    case 2: // Bottom
                        position = new Vector2(GetXPositionForSide(side, canvasSize.Width), (float)canvasSize.Height);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, -(float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    default: // Left
                        position = new Vector2(0, (float)(Random.Shared.NextDouble() * canvasSize.Height));
                        velocity = new Vector2((float)(Random.Shared.NextDouble() * 80 + 40), (float)(Random.Shared.NextDouble() - 0.5) * 100);
                        break;
                }

                particles.Add(new Particle
                {
                    Position = position,
                    Velocity = velocity,
                    Size = (float)(Random.Shared.NextDouble() * 12 + 8), // 8-20 pixels (4x)
                    Opacity = (float)(Random.Shared.NextDouble() * 0.7 + 0.3), // 0.3-1.0
                    Life = 1.0f,
                    Color = GetTechColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                p.Position += p.Velocity * deltaTime;
                p.Velocity *= 0.99f; // Slight damping

                p.Life -= deltaTime * 1.2f; // ~0.8 second lifetime (faster sparks)
                p.Opacity = p.Life * 0.9f;

                // Remove if out of bounds or life depleted
                if (p.Position.X < -20 || p.Position.X > canvasSize.Width + 20 ||
                    p.Position.Y < -20 || p.Position.Y > canvasSize.Height + 20 ||
                    p.Life <= 0)
                {
                    particles.RemoveAt(i);
                }
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);

                // Draw electric spark with trail
                ds.FillEllipse(p.Position, p.Size, p.Size, color);

                // Add glow
                var glowColor = Color.FromArgb((byte)(p.Opacity * 80), p.Color.R, p.Color.G, p.Color.B);
                ds.FillEllipse(p.Position, p.Size * 2, p.Size * 2, glowColor);

                // Draw small trail line
                var trailEnd = p.Position - Vector2.Normalize(p.Velocity) * p.Size * 3;
                ds.DrawLine(p.Position, trailEnd, color, 1.0f);
            }
        }

        private Color GetTechColor()
        {
            var r = Random.Shared.Next(2);
            return r switch
            {
                0 => Color.FromArgb(255, 255, 150, 0),   // Orange
                _ => Color.FromArgb(255, 255, 200, 100)  // Light orange/yellow
            };
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
