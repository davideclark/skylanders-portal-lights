using System;
using System.Collections.Generic;
using System.Threading;
using HidLibrary;

namespace PortalLights
{
    class Program
    {
        private const int INFINITY_VENDOR_ID = 0x0e6f;
        private const int INFINITY_PRODUCT_ID = 0x0129;
        private const int SKYLANDERS_VENDOR_ID = 0x1430;
        private const int SKYLANDERS_PRODUCT_ID = 0x0150;

        static void Main(string[] args)
        {
            Console.WriteLine("Portal Lights - HID Version (No Zadig Required!)");
            Console.WriteLine("Scanning for USB HID devices...\n");

            List<InfinityPortalHID> infinityPortals = new List<InfinityPortalHID>();
            List<SkylandersPortalHID> skylandersPortals = new List<SkylandersPortalHID>();

            // Find all Infinity Bases
            try
            {
                var infinityDevices = HidDevices.Enumerate(INFINITY_VENDOR_ID, INFINITY_PRODUCT_ID);
                foreach (var device in infinityDevices)
                {
                    Console.WriteLine($"Found Infinity Portal: {device.Description}");
                    try
                    {
                        infinityPortals.Add(new InfinityPortalHID(device));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Could not initialize Infinity Portal - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for Infinity Portals: {ex.Message}");
            }

            // Find all Skylanders Portals
            try
            {
                var skylandersDevices = HidDevices.Enumerate(SKYLANDERS_VENDOR_ID, SKYLANDERS_PRODUCT_ID);
                foreach (var device in skylandersDevices)
                {
                    Console.WriteLine($"Found Skylanders Portal: {device.Description}");
                    try
                    {
                        skylandersPortals.Add(new SkylandersPortalHID(device));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  Warning: Could not initialize Skylanders Portal - {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error searching for Skylanders Portals: {ex.Message}");
            }

            if (infinityPortals.Count == 0 && skylandersPortals.Count == 0)
            {
                Console.WriteLine("\nNo portals found. Please check:");
                Console.WriteLine("  1. Portal is plugged in via USB");
                Console.WriteLine("  2. Device is powered on");
                Console.WriteLine("  3. Windows recognizes the device (check Device Manager)");
                Console.WriteLine("\nIf the device shows up in Device Manager but not here,");
                Console.WriteLine("it might not be HID-compliant and may require the LibUSB version.");
                Console.WriteLine("\nPress any key to exit...");
                Console.ReadKey();
                return;
            }

            Console.WriteLine($"\nFound {infinityPortals.Count} Infinity Base(s) and {skylandersPortals.Count} Skylanders Portal(s)");

            // TEST SEQUENCE - Makes it obvious the program is controlling the portal
            Console.WriteLine("\n=== RUNNING TEST SEQUENCE ===");
            Console.WriteLine("This will prove the program is controlling your portal!\n");

            try
            {
                // Step 1: Turn OFF (Black)
                Console.WriteLine("1. Turning lights OFF...");
                foreach (var portal in skylandersPortals)
                {
                    portal.SetColour(0, 0, 0);
                }
                foreach (var portal in infinityPortals)
                {
                    for (byte platform = 1; platform <= 3; platform++)
                    {
                        portal.SetColour(platform, 0, 0, 0);
                    }
                }
                Thread.Sleep(2000);

                // Step 2: Solid RED
                Console.WriteLine("2. Setting to RED...");
                foreach (var portal in skylandersPortals)
                {
                    portal.SetColour(255, 0, 0);
                }
                foreach (var portal in infinityPortals)
                {
                    for (byte platform = 1; platform <= 3; platform++)
                    {
                        portal.SetColour(platform, 255, 0, 0);
                    }
                }
                Thread.Sleep(2000);

                // Step 3: Solid GREEN
                Console.WriteLine("3. Setting to GREEN...");
                foreach (var portal in skylandersPortals)
                {
                    portal.SetColour(0, 255, 0);
                }
                foreach (var portal in infinityPortals)
                {
                    for (byte platform = 1; platform <= 3; platform++)
                    {
                        portal.SetColour(platform, 0, 255, 0);
                    }
                }
                Thread.Sleep(2000);

                // Step 4: Solid BLUE
                Console.WriteLine("4. Setting to BLUE...");
                foreach (var portal in skylandersPortals)
                {
                    portal.SetColour(0, 0, 255);
                }
                foreach (var portal in infinityPortals)
                {
                    for (byte platform = 1; platform <= 3; platform++)
                    {
                        portal.SetColour(platform, 0, 0, 255);
                    }
                }
                Thread.Sleep(2000);

                Console.WriteLine("\n=== TEST COMPLETE! ===");
                Console.WriteLine("If you saw: OFF -> RED -> GREEN -> BLUE, the program is working!\n");
                Console.WriteLine("Starting random light show... (Press Ctrl+C to stop)\n");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Test sequence error: {ex.Message}");
            }

            Random random = new Random();
            int updateCount = 0;

            try
            {
                while (true)
                {
                    updateCount++;

                    // Update Skylanders Portals
                    foreach (var portal in skylandersPortals)
                    {
                        try
                        {
                            byte r = (byte)random.Next(256);
                            byte g = (byte)random.Next(256);
                            byte b = (byte)random.Next(256);
                            portal.SetColour(r, g, b);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating Skylanders Portal: {ex.Message}");
                        }
                    }

                    // Update Infinity Bases (3 platforms each)
                    foreach (var portal in infinityPortals)
                    {
                        try
                        {
                            for (byte platform = 1; platform <= 3; platform++)
                            {
                                byte r = (byte)random.Next(256);
                                byte g = (byte)random.Next(256);
                                byte b = (byte)random.Next(256);
                                portal.SetColour(platform, r, g, b);
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error updating Infinity Portal: {ex.Message}");
                        }
                    }

                    // Show progress every 50 updates
                    if (updateCount % 50 == 0)
                    {
                        Console.WriteLine($"Light show running... ({updateCount} updates)");
                    }

                    Thread.Sleep(100);
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
                    try
                    {
                        portal.Dispose();
                    }
                    catch { }
                }
                foreach (var portal in skylandersPortals)
                {
                    try
                    {
                        portal.Dispose();
                    }
                    catch { }
                }

                Console.WriteLine("Done!");
            }
        }
    }
}
