using System.Numerics;
using Windows.UI;

namespace PortalLights.WinUI.Services.ParticleSystem
{
    public class Particle
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public float Size { get; set; }
        public float Opacity { get; set; }
        public float Life { get; set; }        // 0.0 to 1.0
        public float Rotation { get; set; }
        public Color Color { get; set; }

        // Element-specific properties
        public float PhaseOffset { get; set; }  // For wave/oscillation
        public float Scale { get; set; }        // For size animation
    }
}
