# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a C# application that controls RGB lighting on Disney Infinity Bases and Skylanders Portals of Power via USB.

**Main Program**: `PortalLights.csproj` (LibUSB version)

The project also includes an alternative HID implementation (`PortalLights-HID.csproj`) that doesn't require driver installation, but the LibUSB version is the primary/main codebase.

**Supported Devices:**
- Disney Infinity Base (VID: 0x0e6f, PID: 0x0129)
- Skylanders Portal of Power (VID: 0x1430, PID: 0x0150)
- Skylanders Xbox One Portal (VID: 0x1430, PID: 0x1F17)

## Build & Run Commands

### Main Program (LibUSB - Requires Zadig Driver)
```bash
cd PortalLights-CSharp
dotnet build
dotnet run
```

This is the primary implementation using `PortalLights.csproj` with LibUsbDotNet. Requires WinUSB drivers installed via Zadig (see ZADIG-SETUP.md).

### Alternative: HID Version (No Driver Required)
```bash
cd PortalLights-CSharp
dotnet build PortalLights-HID.csproj
dotnet run --project PortalLights-HID.csproj
```

The HID version uses `PortalLights-HID.csproj` and works with native Windows HID drivers (no Zadig setup needed).

### Building for Release
```bash
dotnet build -c Release
# Or for HID version
dotnet build PortalLights-HID.csproj -c Release
```

## Project Architecture

### Dual Implementation Strategy

The codebase maintains **two parallel implementations** that share the same namespace but use different USB libraries:

1. **LibUSB Version** (PortalLights.csproj) - **PRIMARY/MAIN IMPLEMENTATION**:
   - Dependencies: LibUsbDotNet (2.2.29)
   - Files: `Program.cs`, `InfinityPortal.cs`, `SkylandersPortal.cs`
   - Requires: WinUSB driver via Zadig
   - Platform: Windows x64/x86
   - Target: .NET 6.0-windows
   - **This is the main program being actively developed**

2. **HID Version** (PortalLights-HID.csproj) - **ALTERNATIVE IMPLEMENTATION**:
   - Dependencies: hidlibrary (3.3.40)
   - Files: `Program_HID.cs`, `InfinityPortal_HID.cs`, `SkylandersPortal_HID.cs`
   - Requires: No special drivers (uses Windows HID)
   - Platform: Windows (any CPU)
   - Target: .NET 8.0-windows
   - **Alternative version for users who don't want to install drivers**

Each .csproj file explicitly excludes the other version's files using `<Compile Remove="..."/>` directives.

**When making changes**: Focus on the LibUSB version (Program.cs, InfinityPortal.cs, SkylandersPortal.cs) unless specifically working on HID compatibility.

### Core Class Structure

**Portal Classes** (two variants each):
- `InfinityPortal` / `InfinityPortalHID`: Controls Disney Infinity Base
  - Three controllable platforms (1, 2, 3)
  - Methods: `Activate()`, `SetColour(platform, r, g, b)`, `FlashColour()`, `FadeColour()`, `GetTagId()`
  - Implements USB packet construction with checksums

- `SkylandersPortal` / `SkylandersPortalHID`: Controls Skylanders Portal
  - Supports both PS/PC and Xbox One portal variants
  - Methods: `SetColour(r, g, b)`, `SetLeftColour()`, `SetRightColour()`, `Reset()`, `Set()`, `CheckForFigures()`
  - Character database maps variant IDs to figure names and elements
  - Element-based color mapping for detected figures

**Figure Detection System** (LibUSB version only):
- `FigureInfo` class stores character name and element type
- `ElementType` enum: Magic, Water, Fire, Life, Earth, Air, Undead, Tech, Unknown
- Character database in `SkylandersPortal.cs` maps variant IDs (0x00-0x73) to characters
- NFC tag reading attempts to identify placed figures (simplified in current implementation)

**Program Flow**:
1. Scan USB bus for all connected portals
2. Initialize portal objects (activate devices, claim interfaces)
3. Run main loop (100ms interval):
   - LibUSB version: Random colors on all platforms, figure detection every ~1 second
   - HID version: Test sequence (OFF → RED → GREEN → BLUE), then random colors
4. Cleanup on exit: release USB interfaces and close devices

### USB Communication Details

**Infinity Base Protocol**:
- 32-byte packets with specific byte patterns
- Activation packet contains "Disney 2013" ASCII string
- Color commands use format: `[0xFF, 0x06, 0x90, 0x41, platform, R, G, B, checksum, ...]`
- Checksum is sum of first 8-11 bytes (depending on command) masked to 8 bits
- Fixed magic bytes at positions 12-15 and 20-23

**Skylanders Portal Protocol**:
- Commands are ASCII character-based: 'C' (color), 'J' (fade), 'R' (reset), 'A' (activate), 'S' (status), 'Q' (query)
- Xbox One portals require 0x0B 0x14 header on all packets
- PS/PC portals use control transfers (HID class requests)
- Portal checks for commands ~50 times per second (~20ms intervals)

**Status Response Format**:
- Contains 4-byte little-endian u32 with 2-bit status per figure index (0-15)
- Status codes: `0b00` (NOT_PRESENT), `0b01` (PRESENT), `0b11` (ADDED), `0b10` (REMOVED)
- Supports up to 16 figures simultaneously

**Query Command Format**:
- Structure: `'Q' + figure_index (0x00-0x0F) + block_index (0x00-0x3F)`
- Returns: 16 bytes of NFC block data per query
- Used to read MIFARE Classic 1K tags (64 blocks × 16 bytes = 1KB total)

**Figure Data Layout** (MIFARE Classic 1K):
- Block 1, Offset 0x00: Figure ID
- Block 1, Offset 0x0C: Variant ID (big-endian u16, contains generation + features)
- Blocks 0-7: Unencrypted (common data)
- Blocks 8-63: AES-128 ECB encrypted (game data)

**Fade Effect Command** ('J'):
- Format: `'J' + [position] + fade_time_ms (u16 little-endian) + R + G + B`
- Enables smooth color transitions over specified milliseconds

**Platform-Specific Behavior**:
- Xbox portals: Use interrupt endpoints (EP02 OUT, EP01 IN)
- PS/PC portals: Use control transfers for writes, attempt interrupt reads for status
- HID version: All communication through standard HID reports with report ID prefix (byte 0)

## Protocol Documentation Reference

The implementation is based on detailed reverse engineering documented at:
https://marijnkneppers.dev/posts/reverse-engineering-skylanders-toys-to-life-mechanics/

This reference provides comprehensive details on:
- USB communication protocol and command structure
- NFC tag format (MIFARE Classic 1K)
- Encryption algorithms (AES-128 ECB, Key A generation via CRC-48)
- Figure data layout and checksums
- Audio playback capabilities

## Recent Improvements (Based on Protocol Documentation)

### Status Response Parsing
- **Changed**: Now correctly parses 4-byte little-endian u32 with 2-bit status codes
- **Previous**: Treated bytes as individual position indicators
- **Impact**: Proper detection of ADDED (0b11) and REMOVED (0b10) transition events across 16 figure indices

### Query Command Implementation
- **Changed**: Using proper 'Q' command with figure_index + block_index
- **Previous**: Used undocumented 'M' (activate) + 'R' (read) approach
- **Impact**: Correct NFC block reading following official protocol

### Figure Identification
- **Changed**: Reads Block 1 data with correct offsets (Figure ID at 0x00, Variant ID at 0x0C)
- **Previous**: Attempted to read block 8 with incorrect interpretation
- **Impact**: Accurate character identification using variant ID lookup

### Fade Effects
- **Added**: `FadeColour()`, `FadeLeftColour()`, `FadeRightColour()` methods
- **Implementation**: Uses 'J' command with little-endian u16 fade timing
- **Impact**: Enables smooth color transitions over specified milliseconds

## Important Implementation Notes

### When Modifying Portal Control Logic

1. **Packet Format Preservation**: The exact byte patterns in activation and magic byte positions are reverse-engineered from official protocols. Changing these will break communication.

2. **Checksum Calculation**: Always recalculate checksums when modifying command packets. The checksum is the sum of specific bytes (varies by command) masked to 8 bits.

3. **Platform Detection**: Xbox One portals are detected by PID (0x1F17) and require different packet framing with 0x0B 0x14 header.

4. **NFC Reading**: Character identification reads Block 1 (Sector 0) which is unencrypted. Variant ID at offset 0x0C is big-endian. Blocks 8-63 are AES-128 ECB encrypted and require complex key derivation.

5. **Buffer Management**: When reading from portals, always drain stale responses before sending new commands. PS/PC portals especially may have buffered status updates.

6. **Status Bit-Packing**: The status response uses 2 bits per figure index (0-15) packed into a 32-bit value. Always use bit shifting to extract individual statuses.

### Adding New Features

- **New portal models**: Add VID/PID constants and detection logic in `Main()`, may require protocol investigation
- **New light effects**: Add methods to portal classes, ensure checksum calculation is correct
- **Cross-platform support**: Linux/macOS would require different USB library (libusb direct bindings)
- **Figure database expansion**: Add entries to `CharacterDatabase` dictionary in `SkylandersPortal.cs` using variant IDs from NFC dumps

### Driver Requirements

**LibUSB Version**:
- Must install WinUSB driver using Zadig (see ZADIG-SETUP.md)
- Warning: Installing WinUSB prevents portals from working with official games
- Can be reverted via Device Manager driver update

**HID Version**:
- Works with native Windows HID drivers (no setup required)
- May not work with all portal variants depending on HID compliance
- Recommended as first attempt before LibUSB version

## Repository Structure

```
PortalLights-CSharp/
├── PortalLights.csproj          # LibUSB version project file
├── PortalLights-HID.csproj      # HID version project file
├── PortalLights.sln             # Solution file
├── Program.cs                   # LibUSB main entry point
├── Program_HID.cs               # HID main entry point
├── InfinityPortal.cs            # LibUSB Infinity implementation
├── InfinityPortal_HID.cs        # HID Infinity implementation
├── SkylandersPortal.cs          # LibUSB Skylanders implementation (includes FigureInfo, ElementType)
├── SkylandersPortal_HID.cs      # HID Skylanders implementation
├── ZADIG-SETUP.md               # Driver installation guide
└── CLAUDE.md                    # This file
```

## Common Development Workflows

### Testing Changes to Portal Control
1. Make changes to appropriate portal class (`*_HID.cs` for HID version, `*.cs` for LibUSB)
2. Rebuild: `dotnet build` or `dotnet build PortalLights-HID.csproj`
3. Run with portal connected
4. Watch console output for USB errors or device detection issues

### Adding Support for New Portal Variant
1. Identify VID/PID using Zadig or Device Manager
2. Add constants to `Program.cs` / `Program_HID.cs`
3. Add detection logic in `Main()` USB enumeration loop
4. Test protocol commands (may differ from existing portals)

### Switching Between LibUSB and HID Versions
- No code sharing between versions beyond conceptual similarity
- Both versions must be maintained separately if changes affect core logic
- HID version is simpler but may have compatibility limitations
- LibUSB version has more control but requires driver installation
- we found the hid version did not work