using System;
using System.IO;
using System.IO.Compression;

namespace Nexus.Utils
{
    /// <summary>
    /// Helper class for compressing and decompressing network data
    /// </summary>
    public static class CompressionHelper
    {
        /// <summary>
        /// Compress a byte array using GZip
        /// </summary>
        public static byte[] Compress(byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            // Get compression level from config
            var level = GetCompressionLevel();

            using (var output = new MemoryStream())
            {
                using (var gzip = new GZipStream(output, level))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return output.ToArray();
            }
        }

        /// <summary>
        /// Decompress a GZip compressed byte array
        /// </summary>
        public static byte[] Decompress(byte[] compressedData)
        {
            if (compressedData == null || compressedData.Length == 0)
                return compressedData;

            using (var input = new MemoryStream(compressedData))
            using (var gzip = new GZipStream(input, CompressionMode.Decompress))
            using (var output = new MemoryStream())
            {
                gzip.CopyTo(output);
                return output.ToArray();
            }
        }

        /// <summary>
        /// Compress a ZPackage's data
        /// </summary>
        public static byte[] CompressPackage(ZPackage pkg)
        {
            if (pkg == null) return null;
            return Compress(pkg.GetArray());
        }

        /// <summary>
        /// Decompress data into a ZPackage
        /// </summary>
        public static ZPackage DecompressToPackage(byte[] compressedData)
        {
            if (compressedData == null) return null;
            byte[] decompressed = Decompress(compressedData);
            return new ZPackage(decompressed);
        }

        /// <summary>
        /// Get the compression ratio of data
        /// </summary>
        public static float GetCompressionRatio(int originalSize, int compressedSize)
        {
            if (originalSize == 0) return 0;
            return 1f - ((float)compressedSize / originalSize);
        }

        /// <summary>
        /// Write compressed data to a ZPackage (includes length header for decompression)
        /// </summary>
        public static void WriteCompressed(ZPackage pkg, byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                pkg.Write(0); // Original length
                pkg.Write(0); // Compressed length
                return;
            }

            byte[] compressed = Compress(data);

            pkg.Write(data.Length);         // Original length
            pkg.Write(compressed.Length);   // Compressed length
            pkg.Write(compressed);          // Compressed data
        }

        /// <summary>
        /// Read compressed data from a ZPackage
        /// </summary>
        public static byte[] ReadCompressed(ZPackage pkg)
        {
            int originalLength = pkg.ReadInt();
            int compressedLength = pkg.ReadInt();

            if (originalLength == 0 || compressedLength == 0)
                return new byte[0];

            byte[] compressed = pkg.ReadByteArray();
            return Decompress(compressed);
        }

        /// <summary>
        /// Check if data would benefit from compression
        /// Returns true if compression would likely reduce size
        /// </summary>
        public static bool ShouldCompress(byte[] data)
        {
            if (data == null) return false;

            // Get threshold from config
            int threshold = Plugin.ConfigManager?.CompressionThreshold?.Value ?? 128;
            if (data.Length < threshold) return false;

            // Simple entropy check - high entropy data (random/encrypted) won't compress well
            // This is a quick heuristic, not a full entropy calculation
            int uniqueBytes = 0;
            bool[] seen = new bool[256];
            int sampleSize = Math.Min(data.Length, 256);

            for (int i = 0; i < sampleSize; i++)
            {
                if (!seen[data[i]])
                {
                    seen[data[i]] = true;
                    uniqueBytes++;
                }
            }

            // If more than 80% of byte values are unique in sample, compression likely won't help
            return uniqueBytes < (sampleSize * 0.8);
        }

        /// <summary>
        /// Get compression level from configuration
        /// </summary>
        private static CompressionLevel GetCompressionLevel()
        {
            int level = Plugin.ConfigManager?.CompressionLevel?.Value ?? 6;

            // Map 1-9 scale to .NET CompressionLevel
            // Note: .NET 4.8 only has Fastest, Optimal, NoCompression
            if (level <= 3)
                return CompressionLevel.Fastest;
            else
                return CompressionLevel.Optimal;
        }

        /// <summary>
        /// Format bytes as human-readable string
        /// </summary>
        public static string FormatBytes(long bytes)
        {
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
    }
}
