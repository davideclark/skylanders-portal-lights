using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class LifeParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 80;
        private const float EMISSION_RATE = 4.0f;
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
                        (float)(Random.Shared.NextDouble() * canvasSize.Height)
                    ),
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 15,
                        (float)(Random.Shared.NextDouble() - 0.5) * 15
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 8 + 4), // 4-12 pixels
                    Opacity = (float)(Random.Shared.NextDouble() * 0.4 + 0.3), // 0.3-0.7
                    Life = 1.0f,
                    Rotation = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    PhaseOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Color = GetLeafColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Sine wave movement (floating)
                var sineX = (float)Math.Sin(time * 0.5 + p.PhaseOffset) * 30;
                var sineY = (float)Math.Cos(time * 0.3 + p.PhaseOffset) * 20;

                p.Position += new Vector2(sineX * deltaTime, sineY * deltaTime);
                p.Position += p.Velocity * deltaTime;

                p.Rotation += deltaTime * 0.5f;
                p.Life -= deltaTime * 0.3f; // 3+ second lifetime

                // Wrap around screen
                if (p.Position.X < -20) p.Position = new Vector2((float)canvasSize.Width + 20, p.Position.Y);
                if (p.Position.X > canvasSize.Width + 20) p.Position = new Vector2(-20, p.Position.Y);
                if (p.Position.Y < -20) p.Position = new Vector2(p.Position.X, (float)canvasSize.Height + 20);
                if (p.Position.Y > canvasSize.Height + 20) p.Position = new Vector2(p.Position.X, -20);

                if (p.Life <= 0)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);

                // Draw leaf shape (simple ellipse rotated)
                var transform = Matrix3x2.CreateRotation(p.Rotation, p.Position);
                ds.Transform = transform;
                ds.FillEllipse(Vector2.Zero, p.Size * 1.5f, p.Size * 0.8f, color);
                ds.Transform = Matrix3x2.Identity;
            }
        }

        private Color GetLeafColor()
        {
            var r = Random.Shared.Next(3);
            return r switch
            {
                0 => Color.FromArgb(255, 0, 255, 0),     // Bright green
                1 => Color.FromArgb(255, 50, 200, 50),   // Medium green
                _ => Color.FromArgb(255, 150, 255, 150)  // Light green
            };
        }
    }
}
