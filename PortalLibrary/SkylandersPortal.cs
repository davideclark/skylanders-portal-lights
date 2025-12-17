using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using LibUsbDotNet;
using LibUsbDotNet.Main;

namespace PortalLibrary
{
    public enum ElementType
    {
        Magic,      // Cyan/Teal
        Water,      // Blue
        Fire,       // Red
        Life,       // Green
        Earth,      // Brown/Orange
        Air,        // Yellow/White
        Undead,     // Purple
        Tech,       // Orange
        Unknown     // White
    }

    public class FigureInfo
    {
        public string Name { get; set; }
        public ElementType Element { get; set; }

        // Portal tracking
        public int PortalProductId { get; set; }
        public string PortalName { get; set; } = string.Empty;

        // Encrypted stats (nullable for graceful degradation)
        public int? Level { get; set; }
        public int? Experience { get; set; }
        public int? MaxExperience { get; set; }
        public int? Gold { get; set; }
        public uint? PlaytimeSeconds { get; set; }
        public List<string>? Skills { get; set; }
        public bool DecryptionSucceeded { get; set; }

        public FigureInfo(string name, ElementType element, int portalProductId = 0, string portalName = "")
        {
            Name = name;
            Element = element;
            PortalProductId = portalProductId;
            PortalName = portalName;
        }

        public string GetLevelDisplay()
        {
            if (!DecryptionSucceeded || !Level.HasValue) return "Level Unknown";
            return $"Level {Level.Value}";
        }

        public string GetExperienceDisplay()
        {
            if (!DecryptionSucceeded || !Experience.HasValue || !MaxExperience.HasValue) return "";
            return $"{Experience.Value}/{MaxExperience.Value} XP";
        }

        public double GetExperienceProgress()
        {
            if (!Experience.HasValue || !MaxExperience.HasValue || MaxExperience.Value == 0) return 0.0;
            return (double)Experience.Value / MaxExperience.Value;
        }

        public static (byte r, byte g, byte b) GetElementColor(ElementType element)
        {
            return element switch
            {
                ElementType.Magic => (0, 255, 255),      // Cyan
                ElementType.Water => (0, 100, 255),      // Blue
                ElementType.Fire => (255, 50, 0),        // Red/Orange
                ElementType.Life => (0, 255, 0),         // Green
                ElementType.Earth => (200, 120, 0),      // Brown/Orange
                ElementType.Air => (255, 255, 100),      // Yellow
                ElementType.Undead => (150, 0, 255),     // Purple
                ElementType.Tech => (255, 150, 0),       // Orange
                _ => (255, 255, 255)                     // White for unknown
            };
        }
    }

    public class SkylandersPortal : IDisposable
    {
        private UsbDevice usbDevice;
        private UsbEndpointWriter? writer;
        private UsbEndpointReader? reader;
        private bool isXboxPortal;
        private string portalName;
        private int productId; // Actual product ID of connected portal
        private Dictionary<byte, FigureInfo> detectedFigures; // Maps figure index (0-15) to FigureInfo
        private Dictionary<string, (DateTime lastRead, CharacterStats stats)> figureStatsCache; // Cache for decrypted stats (key: "index_figureId")
        private Dictionary<byte, int> figureMissingCount; // Track how many cycles a figure has been missing (debouncing)
        private const int VENDOR_ID = 0x1430;
        private const int PRODUCT_ID = 0x0150;
        private const int XBOX_PRODUCT_ID = 0x1F17;
        private const int CACHE_EXPIRY_SECONDS = 30; // Cache stats for 30 seconds
        private const int DEBOUNCE_CYCLES = 3; // Number of missing cycles before removing a figure (with 500ms polling = 1.5s)

        // Public properties
        public string PortalName => portalName;
        public Dictionary<byte, FigureInfo> DetectedFigures => detectedFigures;
        public int FigureCount => detectedFigures.Count;
        public bool IsXboxPortal => isXboxPortal;

        // Backward compatibility properties
        public bool HasFigure => detectedFigures.Count > 0;
        public FigureInfo? CurrentFigure => detectedFigures.Count > 0 ? detectedFigures.Values.First() : null;

        // Character database - maps variant IDs (single byte) to names and elements
        private static readonly Dictionary<int, FigureInfo> CharacterDatabase = new Dictionary<int, FigureInfo>
        {
            // Spyro's Adventure - Variant IDs
            { 0x00, new FigureInfo("Whirlwind", ElementType.Air) },
            { 0x01, new FigureInfo("Sonic Boom", ElementType.Air) },
            { 0x02, new FigureInfo("Warnado", ElementType.Air) },
            { 0x03, new FigureInfo("Lightning Rod", ElementType.Air) },
            { 0x04, new FigureInfo("Bash", ElementType.Earth) },
            { 0x05, new FigureInfo("Terrafin", ElementType.Earth) },
            { 0x06, new FigureInfo("Dino-Rang", ElementType.Earth) },
            { 0x07, new FigureInfo("Prism Break", ElementType.Earth) },
            { 0x08, new FigureInfo("Sunburn", ElementType.Fire) },
            { 0x09, new FigureInfo("Eruptor", ElementType.Fire) },
            { 0x0A, new FigureInfo("Ignitor", ElementType.Fire) },
            { 0x0B, new FigureInfo("Flameslinger", ElementType.Fire) },
            { 0x0C, new FigureInfo("Zap", ElementType.Water) },
            { 0x0D, new FigureInfo("Wham-Shell", ElementType.Water) },
            { 0x0E, new FigureInfo("Gill Grunt", ElementType.Water) },
            { 0x0F, new FigureInfo("Slam Bam", ElementType.Water) },
            { 0x10, new FigureInfo("Spyro", ElementType.Magic) },
            { 0x11, new FigureInfo("Voodood", ElementType.Magic) },
            { 0x12, new FigureInfo("Double Trouble", ElementType.Magic) },
            { 0x13, new FigureInfo("Trigger Happy", ElementType.Tech) },
            { 0x14, new FigureInfo("Drobot", ElementType.Tech) },
            { 0x15, new FigureInfo("Drill Sergeant", ElementType.Tech) },
            { 0x16, new FigureInfo("Boomer", ElementType.Tech) },
            { 0x17, new FigureInfo("Wrecking Ball", ElementType.Magic) },
            { 0x18, new FigureInfo("Camo", ElementType.Life) },
            { 0x19, new FigureInfo("Zook", ElementType.Life) },
            { 0x1A, new FigureInfo("Stealth Elf", ElementType.Life) },
            { 0x1B, new FigureInfo("Stump Smash", ElementType.Life) },
            { 0x1C, new FigureInfo("Dark Spyro", ElementType.Magic) },
            { 0x1D, new FigureInfo("Hex", ElementType.Undead) },
            { 0x1E, new FigureInfo("Chop Chop", ElementType.Undead) },
            { 0x1F, new FigureInfo("Ghost Roaster", ElementType.Undead) },
            { 0x20, new FigureInfo("Cynder", ElementType.Undead) },
            // Giants
            { 0x64, new FigureInfo("Jet-Vac", ElementType.Air) },
            { 0x65, new FigureInfo("Swarm", ElementType.Air) },
            { 0x66, new FigureInfo("Crusher", ElementType.Earth) },
            { 0x67, new FigureInfo("Flashwing", ElementType.Earth) },
            { 0x68, new FigureInfo("Hot Head", ElementType.Fire) },
            { 0x69, new FigureInfo("Hot Dog", ElementType.Fire) },
            { 0x6A, new FigureInfo("Chill", ElementType.Water) },
            { 0x6B, new FigureInfo("Thumpback", ElementType.Water) },
            { 0x6C, new FigureInfo("Pop Fizz", ElementType.Magic) },
            { 0x6D, new FigureInfo("Ninjini", ElementType.Magic) },
            { 0x6E, new FigureInfo("Bouncer", ElementType.Tech) },
            { 0x6F, new FigureInfo("Sprocket", ElementType.Tech) },
            { 0x70, new FigureInfo("Tree Rex", ElementType.Life) },
            { 0x71, new FigureInfo("Shroomboom", ElementType.Life) },
            { 0x72, new FigureInfo("Eye-Brawl", ElementType.Undead) },
            { 0x73, new FigureInfo("Fright Rider", ElementType.Undead) },
        };

        public SkylandersPortal(UsbDevice device, int productId, string name)
        {
            usbDevice = device;
            this.productId = productId;
            isXboxPortal = (productId == XBOX_PRODUCT_ID);
            portalName = name;
            detectedFigures = new Dictionary<byte, FigureInfo>();
            figureStatsCache = new Dictionary<string, (DateTime lastRead, CharacterStats stats)>();
            figureMissingCount = new Dictionary<byte, int>();

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

            // Open interrupt endpoints for reading
            // Xbox portal: EP02 OUT, EP01 IN
            // PS/PC portal: Try EP01 IN for reading (writes still use control transfer)
            if (isXboxPortal)
            {
                writer = usbDevice.OpenEndpointWriter(WriteEndpointID.Ep02);
                reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
            }
            else
            {
                // PS/PC portal: try interrupt read endpoint
                try
                {
                    reader = usbDevice.OpenEndpointReader(ReadEndpointID.Ep01);
                }
                catch
                {
                    Console.WriteLine($"[{portalName}] Could not open interrupt read endpoint, will use control transfers");
                }
            }

            // Activate portal and query for figures
            Reset();
            Thread.Sleep(100); // Give portal time to reset
            Set();
            Thread.Sleep(100); // Give portal time to activate

            // For PS/PC portal, do a warmup period to stabilize communication
            if (!isXboxPortal)
            {
                Console.WriteLine($"[{portalName}] Initializing PS/PC portal, warming up...");
                for (int i = 0; i < 5; i++)
                {
                    ReadData(); // Clear any stale data
                    Thread.Sleep(50);
                }
                // Send status command to ensure portal is in correct mode
                byte[] statusCmd = new byte[32];
                statusCmd[0] = (byte)'S';
                WriteData(statusCmd);
                Thread.Sleep(100);
                Console.WriteLine($"[{portalName}] PS/PC portal ready");
            }
        }

        public static UsbDevice FindDevice()
        {
            UsbDeviceFinder finder = new UsbDeviceFinder(VENDOR_ID, PRODUCT_ID);
            return UsbDevice.OpenUsbDevice(finder);
        }

        public void SetColour(byte r, byte g, byte b)
        {
            byte[] data = new byte[32];
            data[0] = (byte)'C';
            data[1] = r;
            data[2] = g;
            data[3] = b;
            WriteData(data);
        }

        public void SetLeftColour(byte r, byte g, byte b)
        {
            // Use fade command with 0ms fade time for immediate color change
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = 0x02;  // Left position
            data[2] = 0x00;  // Fade time low byte (0ms)
            data[3] = 0x00;  // Fade time high byte (0ms)
            data[4] = r;
            data[5] = g;
            data[6] = b;
            WriteData(data);
        }

        public void SetRightColour(byte r, byte g, byte b)
        {
            // Use fade command with 0ms fade time for immediate color change
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = 0x00;  // Right position
            data[2] = 0x00;  // Fade time low byte (0ms)
            data[3] = 0x00;  // Fade time high byte (0ms)
            data[4] = r;
            data[5] = g;
            data[6] = b;
            WriteData(data);
        }

        public void SetCenterColour(byte r, byte g, byte b)
        {
            // Use fade command with 0ms fade time for immediate color change
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = 0x01;  // Center/trap position
            data[2] = 0x00;  // Fade time low byte (0ms)
            data[3] = 0x00;  // Fade time high byte (0ms)
            data[4] = r;
            data[5] = g;
            data[6] = b;
            WriteData(data);
        }

        public void FadeColour(byte r, byte g, byte b, ushort fadeTimeMs)
        {
            // 'J' command with fade timing (little-endian u16 milliseconds)
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = (byte)(fadeTimeMs & 0xFF);        // Fade time low byte
            data[2] = (byte)((fadeTimeMs >> 8) & 0xFF); // Fade time high byte
            data[3] = r;
            data[4] = g;
            data[5] = b;
            WriteData(data);
        }

        public void FadeLeftColour(byte r, byte g, byte b, ushort fadeTimeMs)
        {
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = 0x02;  // Left position
            data[2] = (byte)(fadeTimeMs & 0xFF);
            data[3] = (byte)((fadeTimeMs >> 8) & 0xFF);
            data[4] = r;
            data[5] = g;
            data[6] = b;
            WriteData(data);
        }

        public void FadeRightColour(byte r, byte g, byte b, ushort fadeTimeMs)
        {
            byte[] data = new byte[32];
            data[0] = (byte)'J';
            data[1] = 0x00;  // Right position
            data[2] = (byte)(fadeTimeMs & 0xFF);
            data[3] = (byte)((fadeTimeMs >> 8) & 0xFF);
            data[4] = r;
            data[5] = g;
            data[6] = b;
            WriteData(data);
        }

        public void Reset()
        {
            byte[] data = new byte[32];
            data[0] = (byte)'R';
            WriteData(data);
        }

        public void Set()
        {
            byte[] data = new byte[32];
            data[0] = (byte)'A';
            data[1] = 0x01;
            WriteData(data);
        }

        public void ActivateSpeaker()
        {
            byte[] data = new byte[32];
            data[0] = 0x4d;
            data[1] = 0x01;
            WriteData(data);
        }

        public void FlashTrapLight()
        {
            byte[] data = new byte[32];
            data[0] = 0x51;
            data[1] = 0x10;
            data[2] = 0x08;
            WriteData(data);
        }

        public byte[] ReadFigureBlock(byte figureIndex, byte blockIndex)
        {
            // Drain any stale responses from buffer first
            for (int i = 0; i < 3; i++)
            {
                ReadData();
                Thread.Sleep(5);
            }

            // Send Query command: 'Q' + figure_index (0x10-0x1F) + block_index (0x00-0x3F)
            // Returns 16 bytes of NFC block data
            byte[] queryCmd = new byte[32];
            queryCmd[0] = (byte)'Q';  // Query command
            queryCmd[1] = figureIndex; // Figure index (0x10-0x1F for actual queries)
            queryCmd[2] = blockIndex;  // Block index (0-63 for MIFARE Classic 1K)

            WriteData(queryCmd);
            Thread.Sleep(200); // Give portal more time to read NFC tag

            // Keep reading until we get a Query response (0x51 = 'Q')
            int offset = isXboxPortal ? 2 : 0;
            byte[] lastResponse = null;

            for (int retry = 0; retry < 20; retry++)
            {
                byte[] response = ReadData();

                if (response != null && response.Length > offset + 2)
                {
                    // Look for Query response (0x51 = 'Q')
                    if (response[offset] == 0x51 || response[offset] == (byte)'Q')
                    {
                        lastResponse = response;

                        // Verify this response is for our figure index and block
                        if (response.Length > offset + 18) // Need at least command + index + block + 16 data bytes
                        {
                            byte responseFigureIndex = response[offset + 1];
                            byte responseBlockIndex = response[offset + 2];

                            if (responseFigureIndex == figureIndex && responseBlockIndex == blockIndex)
                            {
                                // Found matching response with data
                                return response;
                            }
                        }
                    }
                }

                Thread.Sleep(100);
            }
            // Return last response even if not fully validated
            return lastResponse ?? new byte[32];
        }

        private FigureInfo? IdentifyFigure(byte figureIndex)
        {
            try
            {
                int offset = isXboxPortal ? 2 : 0;

                // Portal Query command uses indices 0x10-0x1F (not 0x00-0x0F)
                // Add 0x10 to convert status index to query index
                byte queryIndex = (byte)(figureIndex + 0x10);

                // Read block 1 from Sector 0 (contains Figure ID at offset 0x00 and Variant ID at offset 0x0C)
                // Block numbering: Sector 0 = blocks 0,1,2,3; we want block 1
                byte[] block1Response = ReadFigureBlock(queryIndex, 1);

                // Clear buffer but don't send Set() - let portal continue in its current mode
                for (int i = 0; i < 3; i++)
                {
                    ReadData();
                    Thread.Sleep(5);
                }

                // Parse response
                if (block1Response != null && block1Response.Length > offset + 3)
                {
                    // Check if response is a Query response (0x51 = 'Q')
                    if (block1Response[offset] == 0x51 || block1Response[offset] == (byte)'Q')
                    {
                        // Query response format: [header?] 'Q' figure_index block_index [16 bytes of block data]
                        // Verify this is the response we're looking for
                        if (block1Response.Length >= offset + 3 + 16) // Need command + index + block + 16 data bytes
                        {
                            int dataOffset = offset + 3; // Skip command byte, figure index, block index

                            // Read Figure ID as 16-bit little-endian integer from offset 0x00
                            // Per documentation: Figure ID is a 16-bit integer at offset 0x00
                            ushort figureId16 = (ushort)(block1Response[dataOffset + 0x00] | (block1Response[dataOffset + 0x01] << 8));

                            // For backward compatibility with database, use low byte only
                            // (most figures have high byte = 0x00)
                            byte figureId = block1Response[dataOffset + 0x00];

                            // Create cache key combining index and figure ID
                            string cacheKey = $"{figureIndex}_{figureId}";
                            System.Diagnostics.Debug.WriteLine($"[{portalName}] Read figureId: 0x{figureId:X2} (full 16-bit: 0x{figureId16:X4}) at index {figureIndex:X2}");
                            System.Diagnostics.Debug.WriteLine($"[{portalName}] Cache key: {cacheKey}, Cache contains: {figureStatsCache.ContainsKey(cacheKey)}, Total cache entries: {figureStatsCache.Count}");

                            FigureInfo? figureInfo = null;

                            // Use Figure ID to look up character in database
                            if (CharacterDatabase.TryGetValue(figureId, out FigureInfo? templateInfo))
                            {
                                // Create new instance with portal info
                                figureInfo = new FigureInfo(templateInfo.Name, templateInfo.Element, productId, portalName);
                            }
                            else
                            {
                                // Unknown character - create generic info
                                figureInfo = new FigureInfo($"Unknown (ID:0x{figureId:X2})", ElementType.Unknown, productId, portalName);
                            }

                            // Check cache for stats - if recent, use cached data
                            if (figureStatsCache.TryGetValue(cacheKey, out var cachedEntry))
                            {
                                double cacheAgeSeconds = (DateTime.Now - cachedEntry.lastRead).TotalSeconds;
                                System.Diagnostics.Debug.WriteLine($"[{portalName}] Cache found for {figureInfo.Name}, age: {cacheAgeSeconds:F1}s (expiry: {CACHE_EXPIRY_SECONDS}s)");

                                if (cacheAgeSeconds < CACHE_EXPIRY_SECONDS)
                                {
                                    // Use cached stats
                                    PopulateFigureStats(figureInfo, cachedEntry.stats);
                                    System.Diagnostics.Debug.WriteLine($"[{portalName}] ✓ Using cached stats for {figureInfo.Name}");
                                    return figureInfo;
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{portalName}] Cache expired for {figureInfo.Name}, re-reading");
                                }
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"[{portalName}] No cache entry for {figureInfo.Name} (key: {cacheKey})");
                            }

                            // TODO: Decrypt character stats (disabled for now due to reliability issues)
                            // The encrypted block reading is:
                            // 1. Slow (2+ seconds) causing concurrent IdentifyFigure calls
                            // 2. Causing unstable Block 1 reads (figureId changes from 0x19 to 0xDF)
                            // 3. Decryption still producing garbage despite copyright string fix
                            // Need to investigate why Block 1 reads are unstable before re-enabling
                            /*
                            try
                            {
                                System.Diagnostics.Debug.WriteLine($"[{portalName}] Reading encrypted blocks for {figureInfo.Name}...");

                                // Read sector 0 blocks 0-1 for AES key derivation (32 bytes total)
                                byte[] block0Response = ReadFigureBlock(queryIndex, 0);
                                byte[] sector0Data = new byte[32];

                                // Extract block 0 data (16 bytes)
                                if (block0Response != null && block0Response.Length >= offset + 3 + 16)
                                {
                                    Array.Copy(block0Response, offset + 3, sector0Data, 0, 16);
                                }

                                // Extract block 1 data (16 bytes) - we already have this from earlier
                                Array.Copy(block1Response, offset + 3, sector0Data, 16, 16);

                                // Read encrypted Area 0 blocks: 8, 9, 10, 12, 13, 14 (skip sector trailers 11 and 15)
                                var encryptedBlocks = new Dictionary<byte, byte[]>();

                                for (byte blockIdx = 8; blockIdx <= 14; blockIdx++)
                                {
                                    // Skip sector trailers (11, 15)
                                    if (blockIdx == 11) continue;

                                    byte[] blockResponse = ReadFigureBlock(queryIndex, blockIdx);
                                    if (blockResponse != null && blockResponse.Length >= offset + 3 + 16)
                                    {
                                        byte[] blockData = new byte[16];
                                        Array.Copy(blockResponse, offset + 3, blockData, 0, 16);
                                        encryptedBlocks[blockIdx] = blockData;
                                    }

                                    Thread.Sleep(50); // Reduced delay between reads
                                }

                                // Decrypt and parse stats
                                CharacterStats stats = SkylendersCrypto.DecryptAndParseStats(sector0Data, encryptedBlocks);

                                // Cache the stats with compound key
                                figureStatsCache[cacheKey] = (DateTime.Now, stats);

                                // Populate figure info with stats
                                PopulateFigureStats(figureInfo, stats);

                                System.Diagnostics.Debug.WriteLine($"[{portalName}] Successfully decrypted stats for {figureInfo.Name}: Level {stats.Level}, XP {stats.Experience}");
                            }
                            catch (Exception decryptEx)
                            {
                                Console.WriteLine($"[{portalName}] Failed to decrypt figure stats: {decryptEx.Message}");
                                // Continue with basic info - stats will be null
                            }
                            */

                            return figureInfo;
                        }
                    }
                }

                // Fallback if reading failed
                return new FigureInfo("Skylander", ElementType.Magic, productId, portalName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{portalName}] Error reading figure: {ex.Message}");
                return null;
            }
        }

        private void PopulateFigureStats(FigureInfo figureInfo, CharacterStats stats)
        {
            if (stats.DecryptionSucceeded)
            {
                figureInfo.Level = stats.Level;
                figureInfo.Experience = stats.Experience;
                figureInfo.MaxExperience = stats.MaxExperience;
                figureInfo.Gold = stats.Gold;
                figureInfo.PlaytimeSeconds = stats.PlaytimeSeconds;
                figureInfo.Skills = stats.Skills;
                figureInfo.DecryptionSucceeded = true;
            }
            else
            {
                figureInfo.DecryptionSucceeded = false;
            }
        }

        public byte[] GetFigures()
        {
            byte[] data = new byte[32];
            data[0] = (byte)'Q';
            data[1] = 0x10;
            data[2] = 0x01;

            WriteData(data);

            return ReadData();
        }

        public void CheckForFigures()
        {
            // For PS/PC portal, send Status command to request status update
            if (!isXboxPortal)
            {
                try
                {
                    byte[] statusCmd = new byte[32];
                    statusCmd[0] = (byte)'S'; // Status command
                    WriteData(statusCmd);
                    Thread.Sleep(15); // Give portal more time to respond
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[{portalName}] Error sending Status command: {ex.Message}");
                    return; // Skip this check cycle
                }
            }

            // Try multiple reads to catch any async notifications from the portal
            byte[] response = new byte[32];

            // Read multiple times to catch any events
            for (int attempt = 0; attempt < 3; attempt++)
            {
                byte[] readData = ReadData();

                // Keep the most recent non-zero response
                bool hasData = false;
                for (int i = 0; i < readData.Length; i++)
                {
                    if (readData[i] != 0)
                    {
                        hasData = true;
                        break;
                    }
                }

                if (hasData)
                {
                    response = readData;
                }

                Thread.Sleep(5);
            }

            // Parse response for figure presence
            // Status response contains 4-byte little-endian u32 with 2-bit status per figure index
            // Status codes: 0b00 = NOT_PRESENT, 0b01 = PRESENT, 0b11 = ADDED, 0b10 = REMOVED
            HashSet<byte> currentlyPresent = new HashSet<byte>();

            if (response != null && response.Length > 5)
            {
                int offset = isXboxPortal ? 2 : 0; // Skip Xbox header (0B 14)

                // Check if response is 'S' status command
                if (response[offset] == 0x53 || response[offset] == (byte)'S')
                {
                    // Read 4-byte status array as little-endian u32
                    // Response format: [header?] 'S' [4 status bytes] [counter] ...
                    uint statusBits = (uint)(response[offset + 1] |
                                           (response[offset + 2] << 8) |
                                           (response[offset + 3] << 16) |
                                           (response[offset + 4] << 24));

                    // Check each figure index (0-15, each using 2 bits)
                    for (byte figureIndex = 0; figureIndex < 16; figureIndex++)
                    {
                        // Extract 2-bit status for this figure index
                        int bitPosition = figureIndex * 2;
                        byte status = (byte)((statusBits >> bitPosition) & 0b11);

                        // Check status: 0b01 (PRESENT) or 0b11 (ADDED)
                        if (status == 0b01 || status == 0b11)
                        {
                            currentlyPresent.Add(figureIndex);

                            // Reset missing count if figure is present
                            figureMissingCount[figureIndex] = 0;

                            // Identify new figures (either ADDED or PRESENT if not already tracked)
                            if (!detectedFigures.ContainsKey(figureIndex))
                            {
                                // New figure detected - identify it
                                System.Diagnostics.Debug.WriteLine($"[{portalName}] Identifying NEW figure at index {figureIndex}");
                                FigureInfo? figureInfo = IdentifyFigure(figureIndex);
                                if (figureInfo != null)
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{portalName}] Adding {figureInfo.Name} to dictionary at index {figureIndex} (GetHashCode: {figureInfo.GetHashCode()})");
                                    detectedFigures[figureIndex] = figureInfo;
                                    Console.WriteLine($"[{portalName}] *** Figure PLACED at index {figureIndex}: {figureInfo.Name} ({figureInfo.Element}) ***");
                                    System.Diagnostics.Debug.WriteLine($"[{portalName}] Dictionary now contains {detectedFigures.Count} figures: {string.Join(", ", detectedFigures.Values.Select(f => f.Name))}");
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine($"[{portalName}] IdentifyFigure returned null for index {figureIndex}");
                                }
                            }
                            else
                            {
                                // Figure already detected - verify it's still the same
                                var existingFigure = detectedFigures[figureIndex];
                                System.Diagnostics.Debug.WriteLine($"[{portalName}] Figure at index {figureIndex} already tracked as {existingFigure.Name} (GetHashCode: {existingFigure.GetHashCode()})");
                            }
                        }
                        else if (status == 0b10)
                        {
                            // Handle REMOVED event (0b10)
                            if (detectedFigures.ContainsKey(figureIndex))
                            {
                                FigureInfo removedFigure = detectedFigures[figureIndex];
                                detectedFigures.Remove(figureIndex);
                                Console.WriteLine($"[{portalName}] *** Figure REMOVED at index {figureIndex}: {removedFigure.Name} ***");
                            }
                        }
                    }
                }
            }

            // Clean up any figures that are no longer present (but didn't trigger REMOVED event)
            // Use debouncing to avoid flickering - only remove after multiple missing cycles
            List<byte> toRemove = new List<byte>();
            foreach (byte figureIndex in detectedFigures.Keys)
            {
                if (!currentlyPresent.Contains(figureIndex))
                {
                    // Increment missing count
                    if (!figureMissingCount.ContainsKey(figureIndex))
                        figureMissingCount[figureIndex] = 0;

                    figureMissingCount[figureIndex]++;

                    System.Diagnostics.Debug.WriteLine($"[{portalName}] Figure at index {figureIndex} missing for {figureMissingCount[figureIndex]} cycles");

                    // Only remove if missing for DEBOUNCE_CYCLES consecutive cycles
                    if (figureMissingCount[figureIndex] >= DEBOUNCE_CYCLES)
                    {
                        toRemove.Add(figureIndex);
                    }
                }
            }
            foreach (byte figureIndex in toRemove)
            {
                FigureInfo removedFigure = detectedFigures[figureIndex];
                detectedFigures.Remove(figureIndex);
                figureMissingCount.Remove(figureIndex);
                Console.WriteLine($"[{portalName}] *** Figure REMOVED (debounce) at index {figureIndex}: {removedFigure.Name} ***");
                System.Diagnostics.Debug.WriteLine($"[{portalName}] Removed {removedFigure.Name} after {DEBOUNCE_CYCLES} missing cycles");
            }
        }

        private void WriteData(byte[] data)
        {
            if (usbDevice == null || !usbDevice.IsOpen)
                return;

            if (isXboxPortal)
            {
                WriteDataXbox(data);
            }
            else
            {
                WriteDataPSPC(data);
            }
        }

        private void WriteDataPSPC(byte[] data)
        {
            int reportNumber = data[0];
            int failures = 0;
            bool success = false;

            UsbSetupPacket setupPacket = new UsbSetupPacket(
                (byte)(UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface | UsbCtrlFlags.Direction_Out),
                0x09,
                (short)((2 << 8) | reportNumber),
                0,
                32);

            while (!success && failures < 100)
            {
                int bytesTransferred;
                success = usbDevice.ControlTransfer(ref setupPacket, data, data.Length, out bytesTransferred);
                failures++;
            }

            if (!success)
            {
                Console.WriteLine($"PS/PC Write failed after {failures} attempts");
            }
        }

        private void WriteDataXbox(byte[] data)
        {
            if (writer == null)
            {
                Console.WriteLine("Xbox portal writer not initialized");
                return;
            }

            // Xbox portals need 0x0B 0x14 header
            byte[] packet = new byte[32];
            packet[0] = 0x0B;
            packet[1] = 0x14;

            // Copy command data after header
            Array.Copy(data, 0, packet, 2, Math.Min(data.Length, 30));

            int bytesWritten;
            ErrorCode ec = writer.Write(packet, 1000, out bytesWritten);

            if (ec != ErrorCode.None)
            {
                Console.WriteLine($"Xbox Write failed: {ec}");
            }
        }

        private byte[] ReadData()
        {
            if (usbDevice == null || !usbDevice.IsOpen)
                return new byte[32];

            if (isXboxPortal)
            {
                return ReadDataXbox();
            }
            else
            {
                return ReadDataPSPC();
            }
        }

        private byte[] ReadDataPSPC()
        {
            byte[] data = new byte[32];

            // Try interrupt read if endpoint is available (WinUSB driver)
            if (reader != null)
            {
                int bytesRead;
                ErrorCode ec = reader.Read(data, 100, out bytesRead);

                // Timeout is normal when no data available
                if (ec == ErrorCode.IoTimedOut || ec == ErrorCode.None)
                {
                    return data;
                }

                // For other errors, log but continue
                if (ec != ErrorCode.None)
                {
                    Console.WriteLine($"[{portalName}] PS/PC Read error: {ec} - recovering...");

                    // Try to reset the reader
                    try
                    {
                        reader.Reset();
                    }
                    catch
                    {
                        // Ignore reset errors
                    }
                }

                return data;
            }

            // Fall back to control transfer (original HID method)
            UsbSetupPacket setupPacket = new UsbSetupPacket(
                (byte)(UsbCtrlFlags.RequestType_Class | UsbCtrlFlags.Recipient_Interface | UsbCtrlFlags.Direction_In),
                0x01,
                (short)((1 << 8) | 0x21),
                0,
                32);

            int bytesTransferred;
            bool success = usbDevice.ControlTransfer(ref setupPacket, data, data.Length, out bytesTransferred);

            if (!success)
            {
                Console.WriteLine($"[{portalName}] PS/PC Control read failed");
            }

            return data;
        }

        private byte[] ReadDataXbox()
        {
            byte[] data = new byte[32];

            if (reader == null)
                return data;

            int bytesRead;
            ErrorCode ec = reader.Read(data, 100, out bytesRead);

            if (ec != ErrorCode.None && ec != ErrorCode.IoTimedOut)
            {
                // Ignore timeout errors as they're normal when no data available
                if (ec != ErrorCode.IoTimedOut)
                {
                    Console.WriteLine($"Xbox Read error: {ec}");
                }
            }

            return data;
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
