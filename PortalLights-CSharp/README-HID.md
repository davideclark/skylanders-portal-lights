# Portal Lights - C# HID Version (NO ZADIG REQUIRED!)

A C# port of the Portal Lights application that controls Disney Infinity Bases and Skylanders Portals of Power on Windows using the built-in HID drivers.

## ✅ Advantages of This Version

- **NO Zadig required** - Uses Windows' built-in HID drivers
- **NO driver replacement** - Your devices continue to work with official games
- **Safer** - No system driver modifications needed
- **Easier setup** - Just plug in and run!

## ⚠️ Important Note

This version ONLY works if your devices are HID-compliant. Most USB gaming peripherals are, but if this doesn't work, you'll need to use the LibUSB version with Zadig.

## Requirements

- Windows 10/11 (64-bit recommended)
- .NET 6.0 SDK or later
- Disney Infinity Base and/or Skylanders Portal of Power
- **No special drivers needed!**

## Installation & Setup

### 1. Install .NET SDK

Download and install the .NET 6.0 SDK (or later) from:
https://dotnet.microsoft.com/download

### 2. That's it!

No driver installation needed. Windows' built-in HID drivers handle everything.

### 3. Build the Project

Open a command prompt or PowerShell in the `PortalLights-CSharp` directory and run:

```bash
dotnet build PortalLights-HID.csproj
```

### 4. Run the Application

```bash
dotnet run --project PortalLights-HID.csproj
```

Or run the compiled executable:

```bash
cd bin\Debug\net6.0-windows
PortalLights-HID.exe
```

## Usage

When you run the application:
1. It will scan for HID devices matching the portal vendor/product IDs
2. If found, it will start a light show that randomly changes the portal colors
3. Press **Ctrl+C** to stop the program

## Troubleshooting

### "No portals found"

**First, check Device Manager:**
1. Open Device Manager (Win + X, then select Device Manager)
2. Plug in your portal
3. Look for the device under:
   - "Human Interface Devices" (HID) - **This version will work**
   - "Universal Serial Bus controllers" - **You need the LibUSB version**
   - "Other devices" or with a yellow warning - **Driver issue**

**If the device is listed under "Human Interface Devices":**
- Make sure it's plugged in properly
- Try unplugging and replugging
- Try a different USB port
- Try running as Administrator

**If the device is NOT a HID device:**
- Your portal is not HID-compliant
- Use the LibUSB version instead (PortalLights.csproj)
- You'll need Zadig for that version

### "Access denied" error

- Try running as Administrator
- Make sure no other program is using the device
- Close any official game software that might be accessing the portal

### The portal works with games but not with this app

This is normal - your portal might not expose a HID interface. Use the LibUSB version instead.

## Which Version Should I Use?

### Use THIS (HID) version if:
✅ You want the easiest setup
✅ You don't want to modify system drivers
✅ You want to keep using official games
✅ Your devices show up as HID in Device Manager

### Use the LibUSB version if:
❌ This HID version doesn't detect your portals
❌ Your devices don't show as HID devices
❌ You need low-level USB control
❌ You're comfortable using Zadig

## Device Information

### Disney Infinity Base
- Vendor ID: `0x0e6f`
- Product ID: `0x0129`
- Has 3 light platforms

### Skylanders Portal of Power
- Vendor ID: `0x1430`
- Product ID: `0x0150`
- Single RGB light (some models support left/right)

## Features

The C# HID version includes:
- Automatic detection of all connected HID portals
- Random color light show
- Support for multiple devices simultaneously
- Clean shutdown with proper device cleanup
- Better error handling and reporting

## API Methods

### InfinityPortalHID
- `SetColour(platform, r, g, b)` - Set solid color (platform 1-3)
- `FlashColour(platform, r, g, b)` - Flash a color
- `FadeColour(platform, r, g, b)` - Fade to a color

### SkylandersPortalHID
- `SetColour(r, g, b)` - Set the main portal color
- `SetLeftColour(r, g, b)` - Set left side color
- `SetRightColour(r, g, b)` - Set right side color
- `Reset()` - Reset the portal
- `GetFigures()` - Get information about placed figures

## Technical Details

This version uses:
- **HidLibrary** (https://github.com/mikeobrien/HidLibrary) - A modern .NET HID wrapper
- Windows' native HID drivers
- HID Output Reports for sending commands
- HID Input Reports for receiving data

The HID approach adds a report ID byte (0x00) at the beginning of each packet and uses the HID protocol instead of raw USB bulk/control transfers.

## License

This is a port of the original C++ project. Same license applies.

## Need Help?

1. **Check Device Manager** to see how Windows recognizes your device
2. **Try both versions** - HID first (easier), then LibUSB if needed
3. **Run as Administrator** if you get permission errors
4. **Check the issues** in the original repository

Remember: This is hardware reverse engineering, so results may vary by device model and firmware version!
