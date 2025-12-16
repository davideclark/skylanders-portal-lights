using System;
using HidLibrary;

namespace PortalLights
{
    public class SkylandersPortalHID : IDisposable
    {
        private HidDevice device;

        private const int VENDOR_ID = 0x1430;
        private const int PRODUCT_ID = 0x0150;

        public SkylandersPortalHID(HidDevice hidDevice)
        {
            device = hidDevice;
            device.OpenDevice();

            if (!device.IsOpen)
            {
                throw new Exception("Failed to open Skylanders Portal");
            }
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

        public void SetColour(byte r, byte g, byte b)
        {
            byte[] data = new byte[33]; // +1 for report ID
            data[0] = 0x00; // Report ID
            data[1] = (byte)'C';
            data[2] = r;
            data[3] = g;
            data[4] = b;
            WriteData(data);
        }

        public void SetLeftColour(byte r, byte g, byte b)
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = 0x4a;
            data[2] = 0x02;
            data[3] = r;
            data[4] = g;
            data[5] = b;
            WriteData(data);
        }

        public void SetRightColour(byte r, byte g, byte b)
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = 0x4a;
            data[2] = 0x00;
            data[3] = r;
            data[4] = g;
            data[5] = b;
            WriteData(data);
        }

        public void Reset()
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = (byte)'R';
            WriteData(data);
        }

        public void Set()
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = (byte)'A';
            data[2] = 0x01;
            WriteData(data);
        }

        public void ActivateSpeaker()
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = 0x4d;
            data[2] = 0x01;
            WriteData(data);
        }

        public void FlashTrapLight()
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = 0x51;
            data[2] = 0x10;
            data[3] = 0x08;
            WriteData(data);
        }

        public byte[] GetFigures()
        {
            byte[] data = new byte[33];
            data[0] = 0x00; // Report ID
            data[1] = (byte)'Q';
            data[2] = 0x10;
            data[3] = 0x01;

            WriteData(data);

            // Try to read response
            var report = device.ReadReport(1000);
            if (report != null && report.ReadStatus == HidDeviceData.ReadStatus.Success)
            {
                return report.Data;
            }

            return new byte[0];
        }

        private void WriteData(byte[] data)
        {
            if (device != null && device.IsOpen)
            {
                // For HID, we can use Write or WriteReport
                // Write() is simpler and handles the report automatically
                bool success = device.Write(data);

                if (!success)
                {
                    Console.WriteLine("Warning: Failed to write to Skylanders Portal");
                }
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
