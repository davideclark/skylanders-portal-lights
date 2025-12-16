using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PortalLights
{
    class Program
    {
        private const int INFINITY_VENDOR_ID = 0x0e6f;
        private const int INFINITY_PRODUCT_ID = 0x0129;
        private const int SKYLANDERS_VENDOR_ID = 0x1430;
        private const int SKYLANDERS_PRODUCT_ID = 0x0150;
        private const int SKYLANDERS_XBOXONE_PRODUCT_ID = 0x1F17;

        static void Main(string[] args)
        {
            Console.WriteLine("Portal Lights - Scanning for USB devices...");

            List<InfinityPortal> infinityPortals = new List<InfinityPortal>();
            List<SkylandersPortal> skylandersPortals = new List<SkylandersPortal>();

            // Find all Infinity Bases
            UsbDeviceFinder infinityFinder = new UsbDeviceFinder(INFINITY_VENDOR_ID, INFINITY_PRODUCT_ID);
            foreach (UsbRegistry regDevice in UsbDevice.AllDevices)
            {
                if (regDevice.Vid == INFINITY_VENDOR_ID && regDevice.Pid == INFINITY_PRODUCT_ID)
                {
                    UsbDevice device;
                    if (regDevice.Open(out device))
                    {
                        Console.WriteLine($"Found Infinity Portal: {regDevice.FullName}");
                        infinityPortals.Add(new InfinityPortal(device));
                    }
                }
            }

            // Find all Skylanders Portals (both PS/PC and Xbox One versions)
            int skylandersCount = 0;
            foreach (UsbRegistry regDevice in UsbDevice.AllDevices)
            {
                if (regDevice.Vid == SKYLANDERS_VENDOR_ID &&
                    (regDevice.Pid == SKYLANDERS_PRODUCT_ID || regDevice.Pid == SKYLANDERS_XBOXONE_PRODUCT_ID))
                {
                    UsbDevice device;
                    if (regDevice.Open(out device))
                    {
                        skylandersCount++;
                        string portalType = regDevice.Pid == SKYLANDERS_XBOXONE_PRODUCT_ID ? "Xbox One" : "PS/PC";
                        string portalName = $"Skylanders {portalType} #{skylandersCount}";
                        Console.WriteLine($"Found {portalName}: {regDevice.FullName}");
                        skylandersPortals.Add(new SkylandersPortal(device, regDevice.Pid, portalName));
                    }
                }
            }

            if (infinityPortals.Count == 0 && skylandersPortals.Count == 0)
            {
                Console.WriteLine("Please plug in either a Portal of Power or an Infinity Base");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nFound {infinityPortals.Count} Infinity Base(s) and {skylandersPortals.Count} Skylanders Portal(s)");
            Console.WriteLine("Starting interactive light show...");
            Console.WriteLine("Place a Skylanders figure on the portal to see the lights change!");
            Console.WriteLine("(Press Ctrl+C to stop)\n");

            Random random = new Random();
            int loopCount = 0;

            try
            {
                while (true)
                {
                    // Check for figures every 10 loops (~1 second)
                    if (loopCount % 10 == 0)
                    {
                        foreach (var portal in skylandersPortals)
                        {
                            portal.CheckForFigures();
                        }
                    }

                    // Update Skylanders Portals (single LED each)
                    foreach (var portal in skylandersPortals)
                    {
                        if (portal.FigureCount > 0)
                        {
                            // One or more figures detected - show first figure's element color
                            // Pulsing brightness effect (0.5 to 1.0 multiplier)
                            double brightness = 0.5 + 0.5 * Math.Sin(loopCount * 0.1);

                            // Get first detected figure
                            var firstFigure = portal.DetectedFigures.Values.First();
                            var (baseR, baseG, baseB) = PortalLights.FigureInfo.GetElementColor(firstFigure.Element);

                            portal.SetColour((byte)(baseR * brightness), (byte)(baseG * brightness), (byte)(baseB * brightness));
                        }
                        else
                        {
                            // No figures: Random colors
                            byte r = (byte)random.Next(256);
                            byte g = (byte)random.Next(256);
                            byte b = (byte)random.Next(256);
                            portal.SetColour(r, g, b);
                        }
                    }

                    // Update Infinity Bases (3 platforms each)
                    foreach (var portal in infinityPortals)
                    {
                        for (byte platform = 1; platform <= 3; platform++)
                        {
                            byte r = (byte)random.Next(256);
                            byte g = (byte)random.Next(256);
                            byte b = (byte)random.Next(256);
                            portal.SetColour(platform, r, g, b);
                        }
                    }

                    Thread.Sleep(100);
                    loopCount++;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\nError: {ex.Message}");
            }
            finally
            {
                // Cleanup
                Console.WriteLine("\nCleaning up...");
                foreach (var portal in infinityPortals)
                {
                    portal.Dispose();
                }
                foreach (var portal in skylandersPortals)
                {
                    portal.Dispose();
                }

                Console.WriteLine("Done!");
            }
        }
    }
}
