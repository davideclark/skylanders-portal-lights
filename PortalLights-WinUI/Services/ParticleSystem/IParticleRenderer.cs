using Microsoft.Graphics.Canvas;
using System.Collections.Generic;
using Windows.Foundation;

namespace PortalLights.WinUI.Services.ParticleSystem
{
    public interface IParticleRenderer
    {
        void Render(CanvasDrawingSession ds, List<Particle> particles, Size canvasSize);
        void EmitParticles(List<Particle> particles, Size canvasSize, float deltaTime);
        void UpdateParticles(List<Particle> particles, float deltaTime, Size canvasSize);
    }
}
