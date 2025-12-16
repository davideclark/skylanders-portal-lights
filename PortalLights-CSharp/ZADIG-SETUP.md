# Zadig Setup Guide for Portal Lights

This guide will help you install the necessary USB drivers to use LibUSB with Disney Infinity Bases and Skylanders Portals of Power on Windows.

## What is Zadig?

Zadig is a Windows application that installs generic USB drivers (WinUSB, libusb-win32, or libusbK) for USB devices. This is required for LibUsbDotNet to communicate with the portals.

## Download Zadig

Download the latest version from: https://zadig.akeo.ie/

## USB Device Information

You'll need to install drivers for the following devices:

### Disney Infinity Base
- **Vendor ID (VID)**: `0x0e6f`
- **Product ID (PID)**: `0x0129`
- **Name**: Usually appears as "Portal Device" or similar

### Skylanders Portal of Power
- **Vendor ID (VID)**: `0x1430`
- **Product ID (PID)**: `0x0150`
- **Name**: Usually appears as "Portal of Power" or similar

## Installation Steps

### 1. Run Zadig as Administrator
- Right-click `zadig.exe` and select "Run as administrator"

### 2. Show All Devices
- In Zadig, go to **Options** > **List All Devices**

### 3. Locate Your Portal

**For Disney Infinity Base:**
- Plug in your Disney Infinity Base
- In the dropdown menu at the top, look for a device with VID `0e6f` and PID `0129`
- It might appear as "Portal Device" or "USB Input Device"

**For Skylanders Portal:**
- Plug in your Skylanders Portal of Power
- Look for a device with VID `1430` and PID `0150`
- It might appear as "Portal of Power" or "USB Input Device"

### 4. Select the Driver
- In the driver selection area (green arrow in the middle):
  - The left side shows the currently installed driver
  - The right side lets you choose the new driver
- Select **WinUSB** from the dropdown (recommended)
  - Alternative: **libusbK** also works well
  - **libusb-win32** is older but compatible

### 5. Install the Driver
- Click the **"Replace Driver"** or **"Install Driver"** button
- Wait for the installation to complete
- You should see a success message

### 6. Repeat for Additional Portals
- If you have multiple portals (e.g., both Infinity and Skylanders), repeat steps 3-5 for each device type

## Verification

After installing the drivers, you can verify they're working:

1. Build and run the PortalLights application:
   ```bash
   dotnet run
   ```

2. You should see output like:
   ```
   Portal Lights - Scanning for USB devices...
   Found Infinity Portal: ...
   Found 1 Infinity Base(s) and 0 Skylanders Portal(s)
   Starting light show...
   ```

3. The portal lights should start changing colors randomly

## Troubleshooting

### Portal Not Detected
- Make sure you ran Zadig as Administrator
- Verify the driver was installed (check Device Manager)
- Try unplugging and replugging the portal
- Restart your computer

### Device Manager Shows Error
- Open Device Manager (Win + X, then M)
- Look for your portal under "Universal Serial Bus devices" or "libusbK USB Devices"
- If there's a yellow warning icon, try reinstalling the driver

### Application Can't Open Device
- Make sure no other application is using the portal
- Close any Disney Infinity or Skylanders games
- Run the application as Administrator

### Reverting to Original Driver
If you need to use the portal with official games again:
1. Open Device Manager
2. Find the portal device
3. Right-click > "Update driver"
4. Choose "Browse my computer for drivers"
5. Choose "Let me pick from a list of available drivers"
6. Select the original HID or Xbox driver

## Important Notes

- **Warning**: Installing WinUSB/libusb drivers will prevent the portal from working with official Disney Infinity or Skylanders games until you revert the driver
- You may need to install the driver separately for each physical portal you own
- Some portals may appear as multiple USB devices - make sure to select the correct one with the VID/PID listed above

## Quick Reference

| Portal Type | VID | PID | Typical Name |
|------------|-----|-----|--------------|
| Disney Infinity | 0x0e6f | 0x0129 | Portal Device |
| Skylanders | 0x1430 | 0x0150 | Portal of Power |

## Additional Help

If you continue to have issues:
1. Check the LibUsbDotNet documentation: https://github.com/LibUsbDotNet/LibUsbDotNet
2. Ensure your .NET runtime is up to date
3. Try running with Administrator privileges
