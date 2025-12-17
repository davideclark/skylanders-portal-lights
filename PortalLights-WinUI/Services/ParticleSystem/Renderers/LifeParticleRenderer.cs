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
        private ParticleSide _currentSide = ParticleSide.Both;

        public void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime, ParticleSide side)
        {
            _currentSide = side;
            _emissionAccumulator += EMISSION_RATE * deltaTime;
            int toEmit = (int)_emissionAccumulator;
            _emissionAccumulator -= toEmit;

            if (toEmit > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[LIFE RENDERER] EmitParticles called: side={side}, toEmit={toEmit}, canvasWidth={canvasSize.Width}");
            }

            for (int i = 0; i < toEmit && particles.Count < MAX_PARTICLES; i++)
            {
                float x = GetXPositionForSide(side, canvasSize.Width);

                particles.Add(new Particle
                {
                    Position = new Vector2(x, -20), // Spawn at top of screen
                    Velocity = new Vector2(
                        (float)(Random.Shared.NextDouble() - 0.5) * 20,  // Gentle horizontal drift
                        (float)(Random.Shared.NextDouble() * 30 + 40)    // Falling downward 40-70 px/s
                    ),
                    Size = (float)(Random.Shared.NextDouble() * 32 + 16), // 16-48 pixels (4x)
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

                // Gentle swaying as leaves fall
                var sway = (float)Math.Sin(time * 2 + p.PhaseOffset) * 15;
                p.Position += new Vector2(sway * deltaTime, 0);
                p.Position += p.Velocity * deltaTime;

                // Rotate as they fall
                p.Rotation += deltaTime * 1.0f;

                // Remove when reaching bottom of screen (no life timer needed for falling)
                if (p.Position.Y > canvasSize.Height + 20)
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

                // Draw leaf shape (simple ellipse rotated) - rotate then translate
                var transform = Matrix3x2.CreateRotation(p.Rotation) * Matrix3x2.CreateTranslation(p.Position);
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

        private float GetXPositionForSide(ParticleSide side, double canvasWidth)
        {
            return side switch
            {
                // Left: spawn on left half when there are figures on both portals
                ParticleSide.Left => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5),
                // Right: spawn on right half when there are figures on both portals
                ParticleSide.Right => (float)(Random.Shared.NextDouble() * canvasWidth * 0.5 + canvasWidth * 0.5),
                // Both: spawn across entire width when only one Life figure
                _ => (float)(Random.Shared.NextDouble() * canvasWidth)
            };
        }
    }
}
