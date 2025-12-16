# Portal Lights - C# Version for Windows

A C# port of the Portal Lights application that controls Disney Infinity Bases and Skylanders Portals of Power on Windows.

## Requirements

- Windows 10/11 (64-bit recommended)
- .NET 6.0 SDK or later
- Disney Infinity Base and/or Skylanders Portal of Power
- Zadig tool for USB driver installation (see setup below)

## Installation & Setup

### 1. Install .NET SDK

Download and install the .NET 6.0 SDK (or later) from:
https://dotnet.microsoft.com/download

### 2. Install USB Drivers with Zadig

Windows requires special drivers to communicate with USB devices using LibUSB. You need to replace the default Windows driver with WinUSB using Zadig:

**IMPORTANT:** This will prevent the devices from working with the official games until you restore the original drivers!

1. Download Zadig from: https://zadig.akeo.ie/
2. Run Zadig as Administrator
3. In Zadig, go to **Options** → Check **List All Devices**
4. From the dropdown, select:
   - **Disney Infinity Base** (VID: 0x0e6f, PID: 0x0129) OR
   - **Skylanders Portal of Power** (VID: 0x1430, PID: 0x0150)
5. In the target driver box (green arrow), select **WinUSB**
6. Click **Replace Driver** (or **Install Driver**)
7. Wait for installation to complete
8. Repeat for other device if you have both

### 3. Build the Project

Open a command prompt or PowerShell in the `PortalLights-CSharp` directory and run:

```bash
dotnet restore
dotnet build
```

### 4. Run the Application

```bash
dotnet run
```

Or build and run the release version:

```bash
dotnet build -c Release
cd bin\Release\net6.0-windows\win-x64
PortalLights.exe
```

## Usage

When you run the application:
1. It will scan for connected Infinity Bases and Skylanders Portals
2. If found, it will start a light show that randomly changes the portal colors
3. Press **Ctrl+C** to stop the program

## Troubleshooting

### "No devices found"

- Make sure the portal is plugged in
- Verify the WinUSB driver is installed using Zadig
- Try unplugging and replugging the device
- Run the program as Administrator

### "Access denied" or "Error opening device"

- Make sure you installed the WinUSB driver with Zadig
- Try running as Administrator
- Check if another program is using the device

### Restore Original Drivers

If you want to use the devices with the official games again:

1. Open Device Manager
2. Find the device under "Universal Serial Bus devices" or "libusb-win32 devices"
3. Right-click → **Uninstall device** → Check "Delete the driver software"
4. Unplug and replug the device (Windows will reinstall the default driver)

Alternatively, use Zadig to reinstall the original driver.

## Device Information

### Disney Infinity Base
- Vendor ID: `0x0e6f`
- Product ID: `0x0129`
- Has 3 light platforms

### Skylanders Portal of Power
- Vendor ID: `0x1430`
- Product ID: `0x0150`
- Single RGB light

## Features

The C# version includes:
- Automatic detection of all connected portals
- Random color light show
- Support for multiple devices simultaneously
- Clean shutdown with proper USB device cleanup

## API Methods

### InfinityPortal
- `SetColour(platform, r, g, b)` - Set solid color (platform 1-3)
- `FlashColour(platform, r, g, b)` - Flash a color
- `FadeColour(platform, r, g, b)` - Fade to a color
- `GetTagId()` - Request tag information from the portal

### SkylandersPortal
- `SetColour(r, g, b)` - Set the main portal color
- `SetLeftColour(r, g, b)` - Set left side color
- `SetRightColour(r, g, b)` - Set right side color
- `Reset()` - Reset the portal
- `GetFigures()` - Get information about placed figures

## License

This is a port of the original C++ project. Same license applies.

## Notes

- The application uses LibUsbDotNet for USB communication
- Unlike the Linux version, Windows doesn't require kernel driver detaching
- Administrator privileges may be required depending on your USB driver setup
