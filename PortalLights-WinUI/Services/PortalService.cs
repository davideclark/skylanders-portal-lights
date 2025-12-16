using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using PortalLibrary; // Reference to shared library
using Microsoft.UI.Dispatching;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PortalLights.WinUI.Services
{
    public class FiguresChangedEventArgs : EventArgs
    {
        public IReadOnlyList<FigureInfo> Figures { get; init; }
    }

    public class PortalService : IDisposable
    {
        private List<SkylandersPortal> _portals = new();
        private Timer _pollTimer;
        private DispatcherQueue _dispatcher;
        private List<FigureInfo> _lastFigures = new();

        public event EventHandler<FiguresChangedEventArgs> FiguresChanged;

        public PortalService(DispatcherQueue dispatcher)
        {
            _dispatcher = dispatcher;
        }

        public async Task InitializeAsync()
        {
            await Task.Run(() =>
            {
                // Scan for portals (same logic as Program.cs)
                foreach (UsbRegistry regDevice in UsbDevice.AllDevices)
                {
                    if (regDevice.Vid == 0x1430 &&
                        (regDevice.Pid == 0x0150 || regDevice.Pid == 0x1F17))
                    {
                        System.Diagnostics.Debug.WriteLine($"Found portal device: VID={regDevice.Vid:X4} PID={regDevice.Pid:X4}");
                        if (regDevice.Open(out UsbDevice device))
                        {
                            var portal = new SkylandersPortal(device, regDevice.Pid, $"Portal #{_portals.Count + 1}");
                            _portals.Add(portal);
                            System.Diagnostics.Debug.WriteLine($"Portal opened successfully: {portal}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Failed to open portal device - may be in use by another application");
                        }
                    }
                }
                System.Diagnostics.Debug.WriteLine($"Total portals detected: {_portals.Count}");
            });

            // Start polling for figure changes
            _pollTimer = new Timer(CheckFigures, null, 0, 1000); // Check every 1 second
        }

        public IReadOnlyList<FigureInfo> GetCurrentFigures()
        {
            return _lastFigures.AsReadOnly();
        }

        private void CheckFigures(object state)
        {
            try
            {
                // Poll all portals and update LED colors
                foreach (var portal in _portals)
                {
                    portal.CheckForFigures();

                    // Update portal LED based on detected figures
                    if (portal.FigureCount > 0)
                    {
                        // One or more figures detected - show first figure's element color
                        var firstFigure = portal.DetectedFigures.Values.First();
                        var (r, g, b) = FigureInfo.GetElementColor(firstFigure.Element);
                        portal.SetColour(r, g, b);
                    }
                    else
                    {
                        // No figures: Set to dim white/off
                        portal.SetColour(20, 20, 20);
                    }
                }

                // Get current figures from all portals
                var allFigures = _portals
                    .SelectMany(p => p.DetectedFigures.Values)
                    .ToList();

                // Debug logging
                if (allFigures.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Detected {allFigures.Count} figures: {string.Join(", ", allFigures.Select(f => f.Name))}");
                }

                // Only notify if figures changed
                if (!FiguresEqual(allFigures, _lastFigures))
                {
                    System.Diagnostics.Debug.WriteLine($"Figures changed! New count: {allFigures.Count}, Old count: {_lastFigures.Count}");
                    _lastFigures = allFigures;

                    // Notify UI thread
                    _dispatcher.TryEnqueue(() =>
                    {
                        System.Diagnostics.Debug.WriteLine($"Raising FiguresChanged event with {allFigures.Count} figures");
                        FiguresChanged?.Invoke(this, new FiguresChangedEventArgs
                        {
                            Figures = allFigures.AsReadOnly()
                        });
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but continue polling
                System.Diagnostics.Debug.WriteLine($"Error checking figures: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }

        private bool FiguresEqual(List<FigureInfo> list1, List<FigureInfo> list2)
        {
            if (list1.Count != list2.Count) return false;

            for (int i = 0; i < list1.Count; i++)
            {
                if (list1[i].Name != list2[i].Name || list1[i].Element != list2[i].Element)
                    return false;
            }

            return true;
        }

        public void Dispose()
        {
            _pollTimer?.Dispose();
            foreach (var portal in _portals)
            {
                portal.Dispose();
            }
        }
    }
}
