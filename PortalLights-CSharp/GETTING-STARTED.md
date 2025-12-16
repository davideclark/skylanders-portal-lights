# Getting Started with Portal Lights (C#)

## Two Versions Available

I've created **TWO versions** of this program so you can choose based on your needs:

## 🟢 Version 1: HID Version (RECOMMENDED - TRY THIS FIRST!)

**File:** `PortalLights-HID.csproj`

### Pros:
✅ **NO Zadig needed** - works with Windows built-in drivers
✅ **Completely safe** - no driver modifications
✅ **Keeps your portals working** with official games
✅ **Easiest setup** - just plug in and run

### Cons:
❌ Only works if your devices are HID-compliant (most are!)
❌ Might not detect all portal models

### Quick Start:
```bash
cd PortalLights-CSharp
dotnet run --project PortalLights-HID.csproj
```

See `README-HID.md` for full details.

---

## 🟡 Version 2: LibUSB Version (Fallback Option)

**File:** `PortalLights.csproj`

### Pros:
✅ **Works with ALL portals** - even non-HID devices
✅ **More control** - direct USB communication
✅ **More reliable** - bypasses HID layer

### Cons:
❌ **Requires Zadig** - you must install WinUSB drivers
❌ **Breaks official games** - until you restore original drivers
❌ **Modifies system drivers** - some users are uncomfortable with this

### Quick Start:
```bash
cd PortalLights-CSharp

# First time: Install drivers with Zadig (see README-CSHARP.md)

dotnet run --project PortalLights.csproj
```

See `README-CSHARP.md` for full details.

---

## 📋 Decision Guide

### Start with HID version if:
- You want the easiest, safest option
- You still want to use official games
- Your portals show as "HID" devices in Device Manager
- You don't want to install Zadig

### Switch to LibUSB version if:
- HID version doesn't detect your portals
- Your devices don't appear as HID in Device Manager
- You need guaranteed compatibility
- You don't mind using Zadig

---

## 🔍 How to Check Your Device Type

1. Plug in your portal
2. Open **Device Manager** (Win + X → Device Manager)
3. Look for your device:
   - **Under "Human Interface Devices"** → Use HID version ✅
   - **Under "Universal Serial Bus controllers"** → Use LibUSB version
   - **Under "Other devices" with yellow warning** → Install drivers

---

## ⚙️ Building Either Version

Both versions use the same source files, just different project configurations:

### HID Version:
```bash
dotnet build PortalLights-HID.csproj
dotnet run --project PortalLights-HID.csproj
```

### LibUSB Version:
```bash
dotnet build PortalLights.csproj
dotnet run --project PortalLights.csproj
```

---

## 🛠️ Prerequisites (Both Versions)

1. **.NET 6.0 SDK or later**
   - Download: https://dotnet.microsoft.com/download
   - Check if installed: `dotnet --version`

2. **Your Portal Device**
   - Disney Infinity Base (VID: 0x0e6f, PID: 0x0129)
   - Skylanders Portal (VID: 0x1430, PID: 0x0150)

3. **For LibUSB version only:** Zadig tool
   - Download: https://zadig.akeo.ie/

---

## 📚 Documentation

- `README-HID.md` - Complete guide for HID version
- `README-CSHARP.md` - Complete guide for LibUSB version
- **Read the appropriate README before proceeding!**

---

## ❓ FAQ

**Q: Is Zadig safe?**
A: Yes, Zadig is a legitimate open-source tool. However, it DOES modify system drivers, which some users prefer to avoid. That's why I created the HID version!

**Q: Which version should I try first?**
A: Always try the HID version first. It's safer and easier. Only use LibUSB if HID doesn't work.

**Q: Can I switch between versions?**
A: Yes! You can build and run either version. Just make sure you haven't installed WinUSB drivers if you want to use the HID version.

**Q: Will this break my portals?**
A:
- **HID version:** No, completely safe
- **LibUSB version:** Only if you install WinUSB drivers - but you can restore them

**Q: Can I use the portals with official games after this?**
A:
- **HID version:** Yes, always!
- **LibUSB version:** Only if you restore the original drivers (uninstall WinUSB)

**Q: What if neither version works?**
A: Your portal might have unusual firmware or be a different model. Check Device Manager to see if Windows recognizes it at all.

---

## 🎮 Happy Portal Lighting!

Choose your version, follow the README, and enjoy your colorful light show!
