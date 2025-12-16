# Portal Lights Development Session Notes

**Last Updated:** 2025-12-15

## 🎯 Current Status Summary

### ✅ **What's Working:**

1. **Both Skylanders Portals Fully Operational**
   - **PS/PC Portal** (VID:1430, PID:0150) - Random color light show ✅
   - **Xbox One Portal** (VID:1430, PID:1F17) - Random color light show ✅
   - Both use WinUSB drivers installed via Zadig
   - Both controlled simultaneously with LibUsbDotNet

2. **Xbox One Protocol Implemented**
   - Interrupt transfers working (endpoint 0x02 OUT, 0x81 IN)
   - Packet header `0x0B 0x14` correctly added
   - No write errors - communication is clean

3. **Project Structure**
   - `PortalLights.csproj` - LibUSB version (what you should run)
   - `PortalLights-HID.csproj` - HID version (don't use - portals no longer HID)
   - Fixed project file conflicts

### ❌ **Still Working On: Figure Detection**

**The Problem:**
- Portals respond to commands but don't report figure placement/removal
- Tried multiple approaches:
  - ✗ 'Q' query command - returns `51 00 01` (doesn't change with figures)
  - ✗ 'S' status command - returns `53 00 00 00 00 [counter] 01 00` (counter increments but nothing else changes when figure placed)
  - Currently testing: Reading without queries to catch async notifications

**Latest Observations:**
- Xbox One portal responds: `0B 14 53 00 00 00 00 [counter] 01 00`
- PS/PC portal: No responses showing (control transfer read issue)
- Byte 7 is a counter that increments each query
- NO bytes change when figures are placed/removed

### 📋 **Next Steps to Try:**

1. **Different Command Approach**
   - Try 'J' command for figure detection
   - Research if portal needs specific activation sequence
   - Check if we need to read from different endpoint

2. **Continuous Reading**
   - Set up background thread to continuously read
   - Portal might send events only when state changes
   - Current approach might be missing timing

3. **Alternative: Use C++ Version as Reference**
   - Original C++ code in repo might have working detection
   - Could compare USB traffic with working implementation

4. **USB Traffic Capture**
   - Use Wireshark + USBPcap to capture traffic
   - Compare with official game to see protocol

### 💾 **Key Files & Commands:**

**To Run:**
```bash
cd C:\Users\david\source\repos\Disney-Infinity-and-Skylanders-Lighting\PortalLights-CSharp
dotnet run --project PortalLights.csproj
```

**Critical Files:**
- `SkylandersPortal.cs:138` - CheckForFigures() method (currently experimental)
- `Program.cs:78-84` - Figure check loop (checks every ~1 second)
- `Program.cs:90-104` - Light pattern logic (random vs pulsing blue)

**Current Detection Logic:**
- Checks every ~1 second (every 10 loop iterations)
- Reads 3 times per check to catch async notifications
- Looking for meaningful data in responses
- Debug output shows all responses with non-zero bytes

### 🔧 **Technical Details:**

**Xbox One Portal Communication:**
- Write: Interrupt transfer EP02 with `0x0B 0x14` header + command data
- Read: Interrupt transfer EP81 (100ms timeout)
- Responses include the `0x0B 0x14` header (skip first 2 bytes when parsing)

**PS/PC Portal Communication:**
- Write: Control transfer (HID class, request 0x09)
- Read: Control transfer (request 0x01)
- Currently not seeing read responses (need to investigate why)

**Device IDs:**
- Skylanders PS/PC: VID 0x1430, PID 0x0150
- Skylanders Xbox One: VID 0x1430, PID 0x1F17
- Disney Infinity: VID 0x0e6f, PID 0x0129 (supported but not tested)

### 🎨 **Current Behavior:**
- Random RGB colors on both portals (changes every 100ms)
- Would change to pulsing blue when figure detected (once working)
- No errors, smooth operation
- Console shows debug output of portal responses

---

## 🛠️ **Development Journey:**

### What We Accomplished:

1. **Fixed LibUsbDotNet Detection**
   - Problem: Devices not detected with libusbK drivers
   - Solution: Switched to WinUSB drivers using Zadig
   - Result: Both portals now detected ✅

2. **Implemented Xbox One Support**
   - Problem: Xbox One portal failed writes (different protocol)
   - Research: Found it uses interrupt transfers vs control transfers
   - Solution: Implemented separate write/read methods for Xbox portals
   - Result: Both PS/PC and Xbox One portals working ✅

3. **Added Figure Detection Framework**
   - Created CheckForFigures() method
   - Added state tracking (hasFigure property)
   - Implemented different light patterns based on figure presence
   - Status: Framework ready, detection logic needs refinement ❌

### Zadig Driver Setup:

Both portals configured with:
- Driver: WinUSB
- Replaced: Original HID drivers
- Note: Portals won't work with official games until drivers reverted

### Project Files Modified:

1. `PortalLights.csproj`
   - Added exclusions for HID version files
   - Using LibUsbDotNet 2.2.29

2. `SkylandersPortal.cs`
   - Added Xbox One detection (by PID)
   - Separate write methods: WriteDataPSPC() and WriteDataXbox()
   - Separate read methods: ReadDataPSPC() and ReadDataXbox()
   - Added CheckForFigures() with debug output
   - Added hasFigure state tracking

3. `Program.cs`
   - Updated to pass productId and name to SkylandersPortal constructor
   - Added figure checking in main loop
   - Added conditional light patterns (random vs pulsing blue)

---

## 🔍 **Figure Detection Research Notes:**

### Skylanders Protocol Commands:
- 'C' (0x43) - Set color
- 'R' (0x52) - Reset
- 'A' (0x41) - Activate (called with 0x01 parameter)
- 'Q' (0x51) - Query (tried - doesn't show figure changes)
- 'S' (0x53) - Status (tried - shows counter but no figure data)
- 'J' (0x4A) - Unknown (to try next)

### Response Patterns Observed:

**'Q' Query Response:**
```
0B 14 51 00 01 00 00 00 00 00
      ^^                       - Command echo
         ^^ ^^                 - Unknown status bytes (don't change)
```

**'S' Status Response:**
```
0B 14 53 00 00 00 00 13 01 00
      ^^                       - Command echo
         ^^ ^^ ^^ ^^           - All zeros (expected figure data?)
                     ^^        - Counter (13, 14, 15... increments)
                        ^^     - Always 0x01
```

### Hypotheses:
1. Portal might send async events (not query/response)
2. Need different command or activation sequence
3. Might need to read continuously on separate thread
4. PS/PC portal might work differently than Xbox One

---

## 📚 **Documentation Created:**

- `ZADIG-SETUP.md` - Complete Zadig driver installation guide
- `README.md` - Updated with C# version instructions
- `SESSION-NOTES.md` - This file

---

## 🚀 **When Resuming Tomorrow:**

1. **Quick Start:** Run `dotnet run --project PortalLights.csproj` to see current state
2. **Verify:** Both portals should show random colors
3. **Next:** Try one of the approaches in "Next Steps to Try" section above
4. **Alternative:** If figure detection too complex, can remove it and keep working light show

**Main Achievement:** Two different Skylanders portal types (PS/PC and Xbox One) working simultaneously with full RGB control - this alone is a success! 🎉
