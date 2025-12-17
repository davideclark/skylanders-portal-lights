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
                // Emit from edges based on side
                Vector2 position, velocity;
                float midpoint = (float)(canvasSize.Width * 0.5);

                // Choose edges based on side
                int edge;
                if (side == ParticleSide.Left)
                {
                    edge = Random.Shared.Next(3); // 0=Top, 1=Bottom, 2=Left (no right edge)
                }
                else if (side == ParticleSide.Right)
                {
                    edge = Random.Shared.Next(3) + 3; // 3=Top, 4=Bottom, 5=Right (no left edge)
                }
                else // Both
                {
                    edge = Random.Shared.Next(4); // All edges
                }

                switch (edge)
                {
                    case 0: // Top (left side)
                        position = new Vector2((float)(Random.Shared.NextDouble() * midpoint), 0);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, (float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    case 1: // Bottom (left side)
                        position = new Vector2((float)(Random.Shared.NextDouble() * midpoint), (float)canvasSize.Height);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, -(float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    case 2: // Left edge
                        position = new Vector2(0, (float)(Random.Shared.NextDouble() * canvasSize.Height));
                        velocity = new Vector2((float)(Random.Shared.NextDouble() * 80 + 40), (float)(Random.Shared.NextDouble() - 0.5) * 100);
                        break;
                    case 3: // Top (right side)
                        position = new Vector2((float)(Random.Shared.NextDouble() * midpoint + midpoint), 0);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, (float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    case 4: // Bottom (right side)
                        position = new Vector2((float)(Random.Shared.NextDouble() * midpoint + midpoint), (float)canvasSize.Height);
                        velocity = new Vector2((float)(Random.Shared.NextDouble() - 0.5) * 100, -(float)(Random.Shared.NextDouble() * 80 + 40));
                        break;
                    default: // Right edge (case 5)
                        position = new Vector2((float)canvasSize.Width, (float)(Random.Shared.NextDouble() * canvasSize.Height));
                        velocity = new Vector2(-(float)(Random.Shared.NextDouble() * 80 + 40), (float)(Random.Shared.NextDouble() - 0.5) * 100);
                        break;
                }

                particles.Add(new Particle
                {
                    Position = position,
                    Velocity = velocity,
                    Size = (float)(Random.Shared.NextDouble() * 20 + 12), // 12-32 pixels - bigger sparks
                    Opacity = (float)(Random.Shared.NextDouble() * 0.3 + 0.7), // 0.7-1.0 - brighter
                    Life = 1.0f,
                    Scale = (float)side, // Store side for boundary checking
                    Color = GetTechColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            float midpoint = (float)(canvasSize.Width * 0.5);

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                p.Position += p.Velocity * deltaTime;
                p.Velocity *= 0.99f; // Slight damping

                p.Life -= deltaTime * 0.125f; // ~8 second lifetime
                p.Opacity = Math.Min(1.0f, p.Life * 1.2f); // Brighter, capped at 1.0

                // Check side boundaries (Scale stores side: 0=Left, 1=Right, 2=Both)
                bool shouldRemove = p.Life <= 0;

                if (!shouldRemove)
                {
                    if (p.Scale == 0f) // Left side - remove if crossing midpoint or out of bounds
                    {
                        shouldRemove = p.Position.X > midpoint ||
                                     p.Position.X < -20 ||
                                     p.Position.Y < -20 ||
                                     p.Position.Y > canvasSize.Height + 20;
                    }
                    else if (p.Scale == 1f) // Right side - remove if crossing midpoint or out of bounds
                    {
                        shouldRemove = p.Position.X < midpoint ||
                                     p.Position.X > canvasSize.Width + 20 ||
                                     p.Position.Y < -20 ||
                                     p.Position.Y > canvasSize.Height + 20;
                    }
                    else // Both - only remove if out of bounds
                    {
                        shouldRemove = p.Position.X < -20 ||
                                     p.Position.X > canvasSize.Width + 20 ||
                                     p.Position.Y < -20 ||
                                     p.Position.Y > canvasSize.Height + 20;
                    }
                }

                if (shouldRemove)
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
