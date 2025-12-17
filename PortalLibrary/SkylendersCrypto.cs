using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace PortalLibrary
{
    public class CharacterStats
    {
        public int Level { get; set; }
        public int Experience { get; set; }
        public int MaxExperience { get; set; }
        public int Gold { get; set; }
        public uint? PlaytimeSeconds { get; set; }
        public List<string> Skills { get; set; } = new List<string>();
        public bool DecryptionSucceeded { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public double GetExperienceProgress()
        {
            if (MaxExperience == 0) return 0.0;
            return (double)Experience / MaxExperience;
        }

        public string GetLevelDisplay()
        {
            if (!DecryptionSucceeded) return "Level Unknown";
            return $"Level {Level}";
        }

        public string GetExperienceDisplay()
        {
            if (!DecryptionSucceeded) return "";
            return $"{Experience}/{MaxExperience} XP";
        }
    }

    public static class SkylendersCrypto
    {
        // Activision copyright string used in key derivation (35 bytes)
        // " Copyright (C) 2010 Activision. All Rights Reserved."
        private static readonly byte[] ACTIVISION_COPYRIGHT = new byte[]
        {
            0x20, 0x43, 0x6F, 0x70, 0x79, 0x72, 0x69, 0x67, 0x68, 0x74, 0x20, 0x28, 0x43, 0x29, 0x20, 0x32,
            0x30, 0x31, 0x30, 0x20, 0x41, 0x63, 0x74, 0x69, 0x76, 0x69, 0x73, 0x69, 0x6F, 0x6E, 0x2E, 0x20,
            0x41, 0x6C, 0x6C, 0x20, 0x52, 0x69, 0x67, 0x68, 0x74, 0x73, 0x20, 0x52, 0x65, 0x73, 0x65, 0x72,
            0x76, 0x65, 0x64, 0x2E
        };

        // Experience thresholds for each level
        private static readonly (int minXp, int maxXp)[] LEVEL_THRESHOLDS = new[]
        {
            (0, 199),           // Level 1
            (200, 799),         // Level 2
            (800, 1999),        // Level 3
            (2000, 3999),       // Level 4
            (4000, 6999),       // Level 5
            (7000, 11999),      // Level 6
            (12000, 19999),     // Level 7
            (20000, 32999),     // Level 8
            (33000, 63499),     // Level 9
            (63500, 101000)     // Level 10 (max)
        };

        /// <summary>
        /// Generates an AES key using MD5 hash of sector 0 data + block index + copyright string
        /// </summary>
        /// <param name="sector0Data">First 32 bytes from blocks 0-1 (sector 0)</param>
        /// <param name="blockIndex">Block number to decrypt (8-63 for encrypted blocks)</param>
        /// <returns>16-byte AES key</returns>
        public static byte[] GenerateAesKey(byte[] sector0Data, byte blockIndex)
        {
            if (sector0Data == null || sector0Data.Length != 32)
                throw new ArgumentException("Sector 0 data must be exactly 32 bytes", nameof(sector0Data));

            // Create hash input: 32 bytes (sector 0) + 1 byte (block index) + 35 bytes (copyright)
            byte[] hashInput = new byte[68];
            Array.Copy(sector0Data, 0, hashInput, 0, 32);
            hashInput[32] = blockIndex;
            Array.Copy(ACTIVISION_COPYRIGHT, 0, hashInput, 33, 35);

            using (MD5 md5 = MD5.Create())
            {
                return md5.ComputeHash(hashInput);
            }
        }

        /// <summary>
        /// Decrypts a single 16-byte block using AES-128 ECB mode
        /// </summary>
        /// <param name="encryptedData">16-byte encrypted block</param>
        /// <param name="aesKey">16-byte AES key from GenerateAesKey</param>
        /// <returns>16-byte decrypted block</returns>
        public static byte[] DecryptBlock(byte[] encryptedData, byte[] aesKey)
        {
            if (encryptedData == null || encryptedData.Length != 16)
                throw new ArgumentException("Encrypted data must be exactly 16 bytes", nameof(encryptedData));

            if (aesKey == null || aesKey.Length != 16)
                throw new ArgumentException("AES key must be exactly 16 bytes", nameof(aesKey));

            using (Aes aes = Aes.Create())
            {
                aes.Key = aesKey;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.None;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decrypted = new byte[16];
                    decryptor.TransformBlock(encryptedData, 0, 16, decrypted, 0);
                    return decrypted;
                }
            }
        }

        /// <summary>
        /// Calculates CRC-16 checksum for validation (CCITT polynomial)
        /// </summary>
        /// <param name="data">Data to checksum</param>
        /// <param name="offset">Starting offset in data</param>
        /// <param name="length">Number of bytes to checksum</param>
        /// <returns>16-bit checksum</returns>
        public static ushort CalculateCRC16(byte[] data, int offset, int length)
        {
            ushort crc = 0xFFFF;
            const ushort polynomial = 0x1021;

            for (int i = offset; i < offset + length; i++)
            {
                crc ^= (ushort)(data[i] << 8);
                for (int j = 0; j < 8; j++)
                {
                    if ((crc & 0x8000) != 0)
                        crc = (ushort)((crc << 1) ^ polynomial);
                    else
                        crc = (ushort)(crc << 1);
                }
            }

            return crc;
        }

        /// <summary>
        /// Calculates character level from experience points
        /// </summary>
        /// <param name="experience">Current experience points</param>
        /// <returns>Character level (1-10)</returns>
        public static int CalculateLevel(int experience)
        {
            // Validate experience is in reasonable range
            if (experience < 0) return 1;
            if (experience > 101000) return 10; // Max level with max XP

            // Find level based on experience thresholds
            for (int level = 0; level < LEVEL_THRESHOLDS.Length; level++)
            {
                if (experience >= LEVEL_THRESHOLDS[level].minXp && experience <= LEVEL_THRESHOLDS[level].maxXp)
                {
                    return level + 1; // Levels are 1-indexed
                }
            }

            // If experience exceeds all thresholds, return max level
            return 10;
        }

        /// <summary>
        /// Gets the maximum experience for the current level
        /// </summary>
        /// <param name="level">Character level (1-10)</param>
        /// <returns>Maximum experience for this level</returns>
        public static int GetMaxExperienceForLevel(int level)
        {
            if (level < 1 || level > 10) return 0;
            return LEVEL_THRESHOLDS[level - 1].maxXp;
        }

        /// <summary>
        /// Parses character statistics from decrypted Area 0 blocks
        /// </summary>
        /// <param name="decryptedBlocks">Decrypted blocks 8-10 and 12-14 (Area 0, excluding sector trailers)</param>
        /// <returns>Parsed character stats</returns>
        public static CharacterStats ParseCharacterStats(byte[] decryptedBlocks)
        {
            var stats = new CharacterStats();

            try
            {
                if (decryptedBlocks == null || decryptedBlocks.Length < 16)
                {
                    stats.ErrorMessage = "Insufficient decrypted data";
                    return stats;
                }

                // Debug: print first 16 bytes
                System.Diagnostics.Debug.WriteLine($"Decrypted data (first 16 bytes): {BitConverter.ToString(decryptedBlocks, 0, Math.Min(16, decryptedBlocks.Length))}");

                // Parse experience (offset 0x00, 2 bytes, little-endian)
                stats.Experience = BitConverter.ToUInt16(decryptedBlocks, 0);

                // Validate experience is in reasonable range
                if (stats.Experience < 0 || stats.Experience > 101000)
                {
                    stats.ErrorMessage = "Invalid experience value (corrupted data)";
                    return stats;
                }

                // Calculate level from experience
                stats.Level = CalculateLevel(stats.Experience);
                stats.MaxExperience = GetMaxExperienceForLevel(stats.Level);

                // Parse gold (offset 0x03, 2 bytes, little-endian)
                if (decryptedBlocks.Length >= 5)
                {
                    stats.Gold = BitConverter.ToUInt16(decryptedBlocks, 3);
                    System.Diagnostics.Debug.WriteLine($"Gold parsed: {stats.Gold}");
                }

                // Parse playtime (offset 0x05, 4 bytes, little-endian)
                // Note: Area 0 contains SSA playtime data
                if (decryptedBlocks.Length >= 9)
                {
                    stats.PlaytimeSeconds = BitConverter.ToUInt32(decryptedBlocks, 5);
                    System.Diagnostics.Debug.WriteLine($"Playtime seconds parsed: {stats.PlaytimeSeconds} ({stats.PlaytimeSeconds / 3600}h {(stats.PlaytimeSeconds % 3600) / 60}m)");
                }

                // Skills parsing is game-specific and complex - leave empty for now
                // Can be enhanced in future based on specific game data formats

                stats.DecryptionSucceeded = true;
            }
            catch (Exception ex)
            {
                stats.ErrorMessage = $"Parsing error: {ex.Message}";
                stats.DecryptionSucceeded = false;
            }

            return stats;
        }

        /// <summary>
        /// Complete decryption workflow: decrypt multiple blocks and parse stats
        /// </summary>
        /// <param name="sector0Data">32 bytes from blocks 0-1</param>
        /// <param name="encryptedBlocks">Dictionary of block index to encrypted 16-byte block data</param>
        /// <returns>Parsed character stats</returns>
        public static CharacterStats DecryptAndParseStats(byte[] sector0Data, Dictionary<byte, byte[]> encryptedBlocks)
        {
            var stats = new CharacterStats();

            try
            {
                // Decrypt all provided blocks
                var decryptedData = new List<byte>();

                foreach (var kvp in encryptedBlocks.OrderBy(x => x.Key))
                {
                    byte blockIndex = kvp.Key;
                    byte[] encryptedBlock = kvp.Value;

                    // Skip sector trailers (blocks 11, 15, 19, etc.)
                    if ((blockIndex + 1) % 4 == 0)
                        continue;

                    byte[] aesKey = GenerateAesKey(sector0Data, blockIndex);
                    byte[] decryptedBlock = DecryptBlock(encryptedBlock, aesKey);
                    decryptedData.AddRange(decryptedBlock);
                }

                // Parse the decrypted data
                return ParseCharacterStats(decryptedData.ToArray());
            }
            catch (Exception ex)
            {
                stats.ErrorMessage = $"Decryption error: {ex.Message}";
                stats.DecryptionSucceeded = false;
                return stats;
            }
        }
    }
}
