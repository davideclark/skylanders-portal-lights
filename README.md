# Disney-Infinity-and-Skylanders-Lighting
Applications that find all USB Portals of Power and Infinity Bases and randomly change the lights

## C# Version (Windows with LibUSB)

A .NET application using LibUsbDotNet for Windows.

### Setup:
1. Install the WinUSB driver using Zadig - see [ZADIG-SETUP.md](PortalLights-CSharp/ZADIG-SETUP.md)
2. Build and run:
   ```
   cd PortalLights-CSharp
   dotnet build
   dotnet run
   ```

**Supported Devices:**
- Disney Infinity Base (VID: 0x0e6f, PID: 0x0129)
- Skylanders Portal of Power (VID: 0x1430, PID: 0x0150)

## C++ Version (Linux)

A small C++ application for Linux systems.

### Usage:

```make && sudo ./infinitylights```
