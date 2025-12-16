# Disney-Infinity-and-Skylanders-Lighting
As I only have an Skylanders Portal I have not tested the Disney Infinity functionality

There are two applications a command line application and a windows application
At start up they find all USB Portals of Power and Infinity Bases and start to colour cycle the portals led lights.
If a figure is placed on the portal the portal lights change to match the figures elemental type,  water is blue, fire is red etc.
The windows version also displays a full screen colour and the name of the figure.

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
