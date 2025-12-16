using System;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PortalLights
{
    public class InfinityPortal : IDisposable
    {
        private UsbDevice usbDevice;
        private UsbEndpointWriter writer;
        private UsbEndpointReader reader;

        private const int VENDOR_ID = 0x0e6f;
        private const int PRODUCT_ID = 0x0129;

        public InfinityPortal(UsbDevice device)
        {
            usbDevice = device;

            // Open and claim the device
            if (!usbDevice.IsOpen)
            {
                usbDevice.Open();
            }

            IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
            if (wholeUsbDevice != null)
            {
                // Select config and claim interface
                wholeUsbDevice.SetConfiguration(1);
                wholeUsbDevice.ClaimInterface(0);
            }

            // Get the endpoints
            writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep01);
            reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);

            // Activate the portal
            Activate();
        }

        public static UsbDevice FindDevice()
        {
            UsbDeviceFinder finder = new UsbDeviceFinder(VENDOR_ID, PRODUCT_ID);
            return UsbDevice.OpenUsbDevice(finder);
        }

        public void Activate()
        {
            byte[] packet = new byte[32]
            {
                0xff, 0x11, 0x80, 0x00, 0x28, 0x63, 0x29, 0x20,
                0x44, 0x69, 0x73, 0x6e, 0x65, 0x79, 0x20, 0x32,
                0x30, 0x31, 0x33, 0xb6, 0x30, 0x6f, 0xcb, 0x40,
                0x30, 0x6a, 0x44, 0x20, 0x30, 0x5c, 0x6f, 0x00
            };
            SendPacket(packet);
        }

        public void SetColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[32];

            packet[0] = 0xff;
            packet[1] = 0x06;
            packet[2] = 0x90;
            packet[3] = 0x41;
            packet[4] = platform;
            packet[5] = r;
            packet[6] = g;
            packet[7] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 0; i < 8; i++)
            {
                checksum += packet[i];
            }
            packet[8] = (byte)(checksum & 0xFF);

            // Fill rest with zeros (already initialized to 0)
            packet[12] = 0x36;
            packet[13] = 0xf1;
            packet[14] = 0x2c;
            packet[15] = 0x70;
            packet[20] = 0x36;
            packet[21] = 0xe7;
            packet[22] = 0x3c;
            packet[23] = 0x90;

            SendPacket(packet);
        }

        public void FlashColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[32];

            packet[0] = 0xFF;
            packet[1] = 0x09;
            packet[2] = 0x93;
            packet[3] = 0x07;
            packet[4] = platform;
            packet[5] = 0x02;
            packet[6] = 0x02;
            packet[7] = 0x06;
            packet[8] = r;
            packet[9] = g;
            packet[10] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 0; i < 11; i++)
            {
                checksum += packet[i];
            }
            packet[11] = (byte)(checksum & 0xff);

            packet[12] = 0x36;
            packet[13] = 0xf1;
            packet[14] = 0x2c;
            packet[15] = 0x70;
            packet[20] = 0x36;
            packet[21] = 0xe7;
            packet[22] = 0x3c;
            packet[23] = 0x90;
            packet[24] = 0x28;
            packet[27] = 0x44;

            SendPacket(packet);
        }

        public void FadeColour(byte platform, byte r, byte g, byte b)
        {
            byte[] packet = new byte[32];

            packet[0] = 0xFF;
            packet[1] = 0x08;
            packet[2] = 0x92;
            packet[3] = 0x0a;
            packet[4] = platform;
            packet[5] = 0x10;
            packet[6] = 0x02;
            packet[7] = r;
            packet[8] = g;
            packet[9] = b;

            // Calculate checksum
            int checksum = 0;
            for (int i = 0; i < 10; i++)
            {
                checksum += packet[i];
            }
            packet[10] = (byte)(checksum & 0xFF);

            packet[12] = 0x02;
            packet[13] = 0x2a;
            packet[14] = 0x32;
            packet[15] = 0x80;
            packet[20] = 0x36;
            packet[21] = 0xe7;
            packet[22] = 0x3c;
            packet[23] = 0x90;

            SendPacket(packet);
        }

        public void GetTagId()
        {
            byte[] packet = new byte[32]
            {
                0xff, 0x03, 0xb4, 0x26, 0x00, 0xdc, 0x02, 0x06,
                0xff, 0x00, 0x00, 0xca, 0x36, 0xf1, 0x2c, 0x70,
                0x00, 0x00, 0x00, 0x00, 0x36, 0xe7, 0x3c, 0x90,
                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00
            };
            SendPacket(packet);
        }

        private void SendPacket(byte[] packet)
        {
            ReceivePackets();

            int bytesWritten;
            ErrorCode ec = ErrorCode.None;

            while (ec != ErrorCode.Success)
            {
                ec = writer.Write(packet, 100, out bytesWritten);
                ReceivePackets();
            }
        }

        private int ReceivePackets()
        {
            int packetsReceived = 0;
            byte[] packet = new byte[32];
            int bytesRead;
            ErrorCode ec;

            do
            {
                ec = reader.Read(packet, 10, out bytesRead);
                if (ec == ErrorCode.Success && bytesRead > 0)
                {
                    ProcessReceivedPacket(packet);
                    packetsReceived++;
                }
            } while (ec == ErrorCode.Success);

            return packetsReceived;
        }

        private void ProcessReceivedPacket(byte[] packet)
        {
            if (packet[0x00] == 0xab)
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

                GetTagId();
            }
            else if (packet[0x00] == 0xaa && packet[0x01] == 0x09)
            {
                Console.Write("Got tag info: ");
                for (int i = 10; i > 2; i--)
                {
                    Console.Write($"{packet[i]:x} ");
                }
                Console.WriteLine();
            }
        }

        public void Dispose()
        {
            if (usbDevice != null)
            {
                if (usbDevice.IsOpen)
                {
                    IUsbDevice wholeUsbDevice = usbDevice as IUsbDevice;
                    if (wholeUsbDevice != null)
                    {
                        wholeUsbDevice.ReleaseInterface(0);
                    }
                    usbDevice.Close();
                }
                usbDevice = null;
            }
            UsbDevice.Exit();
        }
    }
}
