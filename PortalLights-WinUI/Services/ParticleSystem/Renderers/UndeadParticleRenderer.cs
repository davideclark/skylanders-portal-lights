using Microsoft.Graphics.Canvas;
using System;
using System.Collections.Generic;
using System.Numerics;
using Windows.Foundation;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem.Renderers
{
    public class UndeadParticleRenderer : IParticleRenderer
    {
        private const int MAX_PARTICLES = 70;
        private const float EMISSION_RATE = 5.0f;
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
                        (float)canvasSize.Height + 20 // Start below bottom
                    ),
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 25,
                        -(float)(Random.Shared.NextDouble() * 40 + 20) // Upward drift
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 12 + 6), // 6-18 pixels
                    Opacity = 0.0f, // Start invisible
                    Life = 1.0f,
                    PhaseOffset = (float)(Random.Shared.NextDouble() * Math.PI * 2),
                    Scale = 0.5f,
                    Color = GetUndeadColor()
                });
            }
        }

        public void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize)
        {
            var time = (float)DateTime.Now.TimeOfDay.TotalSeconds;

            for (int i = particles.Count - 1; i >= 0; i--)
            {
                var p = particles[i];

                // Wavy ghostly movement
                var sineX = (float)Math.Sin(time * 1.5 + p.PhaseOffset) * 25;
                p.Position += new Vector2(sineX * deltaTime, 0);
                p.Position += p.Velocity * deltaTime;

                p.Life -= deltaTime * 0.4f; // 2.5 second lifetime

                // Fade in and out
                if (p.Life > 0.7f)
                {
                    p.Opacity = (1.0f - p.Life) / 0.3f * 0.5f; // Fade in
                    p.Scale = 0.5f + (1.0f - p.Life) / 0.3f * 0.5f;
                }
                else if (p.Life > 0.3f)
                {
                    p.Opacity = 0.5f + (float)Math.Sin(time * 5 + p.PhaseOffset) * 0.1f;
                    p.Scale = 1.0f;
                }
                else
                {
                    p.Opacity = p.Life / 0.3f * 0.5f; // Fade out
                    p.Scale = p.Life / 0.3f;
                }

                if (p.Life <= 0 || p.Position.Y < -50)
                    particles.RemoveAt(i);
            }
        }

        public void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize)
        {
            foreach (var p in particles)
            {
                var color = Color.FromArgb((byte)(p.Opacity * 255), p.Color.R, p.Color.G, p.Color.B);
                var size = p.Size * p.Scale;

                // Draw wispy trail effect
                for (int j = 0; j < 3; j++)
                {
                    var trailOpacity = (byte)(p.Opacity * 255 * (1.0f - j * 0.33f));
                    var trailColor = Color.FromArgb(trailOpacity, p.Color.R, p.Color.G, p.Color.B);
                    var trailSize = size * (1.0f - j * 0.2f);
                    var trailOffset = new Vector2(0, j * 8);

                    ds.FillEllipse(p.Position + trailOffset, trailSize, trailSize * 0.7f, trailColor);
                }
            }
        }

        private Color GetUndeadColor()
        {
            return Color.FromArgb(255, 150, 0, 255); // Purple
        }
    }
}
