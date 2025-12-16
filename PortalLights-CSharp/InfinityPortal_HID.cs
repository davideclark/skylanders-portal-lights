using System;
using HidLibrary;

namespace PortalLights
{
    public class InfinityPortalHID : IDisposable
    {
        private HidDevice device;

        private const int VENDOR_ID = 0x0e6f;
        private const int PRODUCT_ID = 0x0129;

        public InfinityPortalHID(HidDevice hidDevice)
        {
            device = hidDevice;
            device.OpenDevice();

            if (!device.IsOpen)
            {
                throw new Exception("Failed to open Infinity Portal");
            }

            // Activate the portal
            Activate();
        }

        public static HidDevice FindDevice()
        {
            var devices = HidDevices.Enumerate(VENDOR_ID, PRODUCT_ID);
            foreach (var device in devices)
            {
                return device;
            }
            return null;
        }

        public void Activate()
        {
            byte[] packet = new byte[33]; // HID reports typically need +1 byte for report ID
            packet[0] = 0x00; // Report ID (usually 0)
            packet[1] = 0xff;
            packet[2] = 0x11;
            packet[3] = 0x80;
            packet[4] = 0x00;
            packet[5] = 0x28;
            packet[6] = 0x63;
            packet[7] = 0x29;
            packet[8] = 0x20;
            packet[9] = 0x44;
            packet[10] = 0x69;
            packet[11] = 0x73;
            packet[12] = 0x6e;
            packet[13] = 0x65;
            packet[14] = 0x79;
            packet[15] = 0x20;
            packet[16] = 0x32;
            packet[17] = 0x30;
            packet[18] = 0x31;
            packet[19] = 0x33;
            packet[20] = 0xb6;
            packet[21] = 0x30;
            packet[22] = 0x6f;
            packet[23] = 0xcb;
            packet[24] = 0x40;
            packet[25] = 0x30;
            packet[26] = 0x6a;
            packet[27] = 0x44;
            packet[28] = 0x20;
            packet[29] = 0x30;
            packet[30] = 0x5c;
            packet[31] = 0x6f;
            packet[32] = 0x00;

            SendPacket(packet);
        }

        public void SetColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[33];
            packet[0] = 0x00; // Report ID

            packet[1] = 0xff;
            packet[2] = 0x06;
            packet[3] = 0x90;
            packet[4] = 0x41;
            packet[5] = platform;
            packet[6] = r;
            packet[7] = g;
            packet[8] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 1; i <= 8; i++)
            {
                checksum += packet[i];
            }
            packet[9] = (byte)(checksum & 0xFF);

            packet[13] = 0x36;
            packet[14] = 0xf1;
            packet[15] = 0x2c;
            packet[16] = 0x70;
            packet[21] = 0x36;
            packet[22] = 0xe7;
            packet[23] = 0x3c;
            packet[24] = 0x90;

            SendPacket(packet);
        }

        public void FlashColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[33];
            packet[0] = 0x00; // Report ID

            packet[1] = 0xFF;
            packet[2] = 0x09;
            packet[3] = 0x93;
            packet[4] = 0x07;
            packet[5] = platform;
            packet[6] = 0x02;
            packet[7] = 0x02;
            packet[8] = 0x06;
            packet[9] = r;
            packet[10] = g;
            packet[11] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 1; i <= 11; i++)
            {
                checksum += packet[i];
            }
            packet[12] = (byte)(checksum & 0xff);

            packet[13] = 0x36;
            packet[14] = 0xf1;
            packet[15] = 0x2c;
            packet[16] = 0x70;
            packet[21] = 0x36;
            packet[22] = 0xe7;
            packet[23] = 0x3c;
            packet[24] = 0x90;
            packet[25] = 0x28;
            packet[28] = 0x44;

            SendPacket(packet);
        }

        public void FadeColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[33];
            packet[0] = 0x00; // Report ID

            packet[1] = 0xFF;
            packet[2] = 0x08;
            packet[3] = 0x92;
            packet[4] = 0x0a;
            packet[5] = platform;
            packet[6] = 0x10;
            packet[7] = 0x02;
            packet[8] = r;
            packet[9] = g;
            packet[10] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 1; i <= 10; i++)
            {
                checksum += packet[i];
            }
            packet[11] = (byte)(checksum & 0xFF);

            packet[13] = 0x02;
            packet[14] = 0x2a;
            packet[15] = 0x32;
            packet[16] = 0x80;
            packet[21] = 0x36;
            packet[22] = 0xe7;
            packet[23] = 0x3c;
            packet[24] = 0x90;

            SendPacket(packet);
        }

        private void SendPacket(byte[] packet)
        {
            if (device != null && device.IsOpen)
            {
                bool success = device.Write(packet);
                if (!success)
                {
                    Console.WriteLine("Warning: Failed to write to Infinity Portal");
                }

                // Try to read any response
                device.ReadReport(OnReport, 10);
            }
        }

        private void OnReport(HidReport report)
        {
            if (report.Data.Length > 0)
            {
                ProcessReceivedPacket(report.Data);
            }
        }

        private void ProcessReceivedPacket(byte[] packet)
        {
            if (packet.Length < 6) return;

            if (packet[0] == 0xab)
            {
                byte platformSetting = packet[2];
                byte placedRemoved = packet[5];

                if (placedRemoved == 0x00)
                {
                    Console.WriteLine($"Tag placed on platform: {platformSetting}");
                }
                else
                {
                    Console.WriteLine($"Tag removed from platform: {platformSetting}");
                }
            }
            else if (packet[0] == 0xaa && packet[1] == 0x09)
            {
                Console.Write("Got tag info: ");
                for (int i = 10; i > 2 && i < packet.Length; i--)
                {
                    Console.Write($"{packet[i]:x} ");
                }
                Console.WriteLine();
            }
        }

        public void Dispose()
        {
            if (device != null)
            {
                if (device.IsOpen)
                {
                    device.CloseDevice();
                }
                device.Dispose();
                device = null;
            }
        }
    }
}
