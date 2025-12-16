using Microsoft.Graphics.Canvas;
using Microsoft.UI.Dispatching;
using PortalLibrary;
using PortalLights.WinUI.Services.ParticleSystem.Renderers;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.Foundation;

namespace PortalLights.WinUI.Services.ParticleSystem
{
    public class ParticleEngine : IDisposable
    {
        private DispatcherQueueTimer _animationTimer;
        private Dictionary<ElementType, List<Particle>> _particlesByElement;
        private Dictionary<ElementType, float> _elementOpacity;
        private Dictionary<ElementType, IParticleRenderer> _renderers;
        private HashSet<ElementType> _activeElements;
        private DateTime _lastUpdate;
        private Size _canvasSize;
        private Queue<Particle> _particlePool;

        // Performance monitoring
        private int _frameCount = 0;
        private DateTime _lastFpsCheck = DateTime.Now;
        private const float FADE_SPEED = 2.0f; // Opacity change per second

        public ParticleEngine(DispatcherQueue dispatcher)
        {
            _animationTimer = dispatcher.CreateTimer();
            _animationTimer.Interval = TimeSpan.FromMilliseconds(16); // ~60 FPS
            _animationTimer.Tick += Update;

            _particlesByElement = new Dictionary<ElementType, List<Particle>>();
            _elementOpacity = new Dictionary<ElementType, float>();
            _activeElements = new HashSet<ElementType>();
            _renderers = new Dictionary<ElementType, IParticleRenderer>();
            _particlePool = new Queue<Particle>(500);
            _lastUpdate = DateTime.Now;

            // Initialize renderers for each element type
            _renderers[ElementType.Fire] = new FireParticleRenderer();
            _renderers[ElementType.Water] = new WaterParticleRenderer();
            _renderers[ElementType.Life] = new LifeParticleRenderer();
            _renderers[ElementType.Magic] = new MagicParticleRenderer();
            _renderers[ElementType.Air] = new AirParticleRenderer();
            _renderers[ElementType.Earth] = new EarthParticleRenderer();
            _renderers[ElementType.Undead] = new UndeadParticleRenderer();
            _renderers[ElementType.Tech] = new TechParticleRenderer();
        }

        public void SetActiveElements(IReadOnlyList<FigureInfo> figures)
        {
            _activeElements = new HashSet<ElementType>(figures.Select(f => f.Element));
            System.Diagnostics.Debug.WriteLine($"SetActiveElements: {figures.Count} figures, Active elements: {string.Join(", ", _activeElements)}");

            // Initialize or update opacity for all elements
            foreach (var element in Enum.GetValues<ElementType>())
            {
                if (element == ElementType.Unknown) continue;

                if (!_elementOpacity.ContainsKey(element))
                    _elementOpacity[element] = 0.0f;

                // Initialize particle systems for new elements
                if (_activeElements.Contains(element) && !_particlesByElement.ContainsKey(element))
                {
                    _particlesByElement[element] = new List<Particle>();
                    System.Diagnostics.Debug.WriteLine($"  Created particle system for {element}");
                }
            }

            System.Diagnostics.Debug.WriteLine($"Total particle systems: {_particlesByElement.Count}");
        }

        public void Render(CanvasDrawingSession ds, Size canvasSize)
        {
            _canvasSize = canvasSize;

            var totalParticlesRendered = 0;
            // Render all active particle systems
            foreach (var (element, particles) in _particlesByElement.ToList())
            {
                var opacity = _elementOpacity.GetValueOrDefault(element, 0.0f);
                if (opacity > 0.01f && _renderers.ContainsKey(element))
                {
                    _renderers[element].Render(ds, particles, canvasSize);
                    totalParticlesRendered += particles.Count;
                }
            }

            if (totalParticlesRendered > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Render: Drew {totalParticlesRendered} total particles across {_particlesByElement.Count} systems");
            }
        }

        private void Update(object sender, object e)
        {
            try
            {
                var now = DateTime.Now;
                var deltaTime = (float)(now - _lastUpdate).TotalSeconds;
                _lastUpdate = now;

                // Cap delta time to prevent huge jumps
                deltaTime = Math.Min(deltaTime, 0.1f);

                // Update opacity transitions
                foreach (var element in _elementOpacity.Keys.ToList())
                {
                    var targetOpacity = _activeElements.Contains(element) ? 1.0f : 0.0f;
                    var oldOpacity = _elementOpacity[element];

                    if (_elementOpacity[element] < targetOpacity)
                        _elementOpacity[element] = Math.Min(1.0f, _elementOpacity[element] + FADE_SPEED * deltaTime);
                    else if (_elementOpacity[element] > targetOpacity)
                        _elementOpacity[element] = Math.Max(0.0f, _elementOpacity[element] - FADE_SPEED * deltaTime);

                    if (oldOpacity != _elementOpacity[element])
                    {
                        System.Diagnostics.Debug.WriteLine($"  Opacity {element}: {oldOpacity:F2} → {_elementOpacity[element]:F2} (target: {targetOpacity})");
                    }
                }

                // Update and emit particles
                foreach (var (element, particles) in _particlesByElement.ToList())
                {
                    var opacity = _elementOpacity[element];

                    if (opacity > 0.01f && _renderers.ContainsKey(element))
                    {
                        var particleCountBefore = particles.Count;
                        var renderer = _renderers[element];
                        renderer.EmitParticles(particles, _canvasSize, deltaTime * opacity);
                        renderer.UpdateParticles(particles, deltaTime, _canvasSize);

                        if (particles.Count != particleCountBefore)
                        {
                            System.Diagnostics.Debug.WriteLine($"  {element}: {particleCountBefore} → {particles.Count} particles (opacity: {opacity:F2})");
                        }

                        // Apply global opacity to all particles
                        foreach (var p in particles)
                        {
                            var baseOpacity = p.Opacity;
                            p.Opacity = baseOpacity * opacity;
                        }
                    }
                    else if (opacity <= 0.01f)
                    {
                        // Remove particle system if fully faded out
                        ReturnParticlesToPool(particles);
                        _particlesByElement.Remove(element);
                    }
                }

                // Performance monitoring
                _frameCount++;
                if ((DateTime.Now - _lastFpsCheck).TotalSeconds >= 1.0)
                {
                    var fps = _frameCount;
                    _frameCount = 0;
                    _lastFpsCheck = DateTime.Now;

                    System.Diagnostics.Debug.WriteLine($"Particle Engine FPS: {fps}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Particle engine update error: {ex.Message}");
            }
        }

        private Particle GetParticle()
        {
            if (_particlePool.Count > 0)
                return _particlePool.Dequeue();
            return new Particle();
        }

        private void ReturnParticle(Particle p)
        {
            if (_particlePool.Count < 1000)
                _particlePool.Enqueue(p);
        }

        private void ReturnParticlesToPool(List<Particle> particles)
        {
            foreach (var p in particles)
            {
                ReturnParticle(p);
            }
            particles.Clear();
        }

        public void Start() => _animationTimer.Start();
        public void Stop() => _animationTimer.Stop();

        public void Dispose()
        {
            _animationTimer?.Stop();
        }
    }
}
