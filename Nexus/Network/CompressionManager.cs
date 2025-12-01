using System;
using System.Collections.Generic;
using Nexus.Utils;

namespace Nexus.Network
{
    /// <summary>
    /// Manages packet compression for network traffic
    /// </summary>
    public class CompressionManager
    {
        // Track compression statistics
        private long _totalBytesBeforeCompression;
        private long _totalBytesAfterCompression;
        private long _packetsCompressed;
        private long _packetsSkipped;

        // Track connected Nexus peers for compression compatibility
        private readonly HashSet<long> _nexusPeers = new HashSet<long>();

        // Compression header to identify Nexus-compressed packets
        private const byte NEXUS_COMPRESSION_HEADER = 0x4E; // 'N' for Nexus

        public CompressionManager()
        {
        }

        /// <summary>
        /// Register a peer as Nexus-enabled (supports compression)
        /// </summary>
        public void RegisterNexusPeer(long peerId)
        {
            if (_nexusPeers.Add(peerId))
            {
                LogDebug($"Registered Nexus peer: {peerId}");
            }
        }

        /// <summary>
        /// Unregister a peer (disconnected or not Nexus-enabled)
        /// </summary>
        public void UnregisterNexusPeer(long peerId)
        {
            if (_nexusPeers.Remove(peerId))
            {
                LogDebug($"Unregistered Nexus peer: {peerId}");
            }
        }

        /// <summary>
        /// Check if a peer supports Nexus compression
        /// </summary>
        public bool IsNexusPeer(long peerId)
        {
            return _nexusPeers.Contains(peerId);
        }

        /// <summary>
        /// Compress data for sending to a peer
        /// Returns original data if compression not applicable
        /// </summary>
        public byte[] CompressForPeer(long peerId, byte[] data)
        {
            if (data == null || data.Length == 0)
                return data;

            // Check if compression is enabled
            if (!IsCompressionEnabled())
            {
                _packetsSkipped++;
                return data;
            }

            // Check if peer supports compression
            if (!IsNexusPeer(peerId))
            {
                _packetsSkipped++;
                return data;
            }

            // Check minimum threshold
            int threshold = Plugin.ConfigManager?.CompressionThreshold?.Value ?? 128;
            if (data.Length < threshold)
            {
                _packetsSkipped++;
                return data;
            }

            try
            {
                byte[] compressed = CompressionHelper.Compress(data);

                // Only use compression if it actually reduced size
                if (compressed.Length < data.Length)
                {
                    _totalBytesBeforeCompression += data.Length;
                    _totalBytesAfterCompression += compressed.Length;
                    _packetsCompressed++;

                    // Add header to identify as compressed
                    byte[] result = new byte[compressed.Length + 5];
                    result[0] = NEXUS_COMPRESSION_HEADER;
                    BitConverter.GetBytes(data.Length).CopyTo(result, 1);
                    compressed.CopyTo(result, 5);

                    return result;
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Compression failed: {ex.Message}");
            }

            _packetsSkipped++;
            return data;
        }

        /// <summary>
        /// Decompress data received from a peer
        /// Returns original data if not compressed
        /// </summary>
        public byte[] DecompressFromPeer(byte[] data)
        {
            if (data == null || data.Length < 6)
                return data;

            // Check for Nexus compression header
            if (data[0] != NEXUS_COMPRESSION_HEADER)
                return data;

            try
            {
                int originalLength = BitConverter.ToInt32(data, 1);
                byte[] compressed = new byte[data.Length - 5];
                Array.Copy(data, 5, compressed, 0, compressed.Length);

                byte[] decompressed = CompressionHelper.Decompress(compressed);

                if (decompressed.Length != originalLength)
                {
                    Plugin.Log.LogWarning($"Decompression size mismatch: expected {originalLength}, got {decompressed.Length}");
                }

                return decompressed;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"Decompression failed: {ex.Message}");
                return data;
            }
        }

        /// <summary>
        /// Check if compression is enabled
        /// </summary>
        public bool IsCompressionEnabled()
        {
            // Check JSON config for server-enforced setting
            if (Config.JsonConfig.Instance?.ForceCompression == true)
                return true;

            // Check BepInEx config
            return Plugin.ConfigManager?.EnableCompression?.Value ?? true;
        }

        /// <summary>
        /// Get compression ratio (0.0 to 1.0, higher is better)
        /// </summary>
        public float GetCompressionRatio()
        {
            if (_totalBytesBeforeCompression == 0)
                return 0;

            return 1f - ((float)_totalBytesAfterCompression / _totalBytesBeforeCompression);
        }

        /// <summary>
        /// Get total bytes saved by compression
        /// </summary>
        public long GetBytesSaved()
        {
            return _totalBytesBeforeCompression - _totalBytesAfterCompression;
        }

        public long PacketsCompressed => _packetsCompressed;
        public long PacketsSkipped => _packetsSkipped;
        public long TotalBytesBeforeCompression => _totalBytesBeforeCompression;
        public long TotalBytesAfterCompression => _totalBytesAfterCompression;
        public int NexusPeerCount => _nexusPeers.Count;

        public void Cleanup()
        {
            _nexusPeers.Clear();
        }

        private void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.DebugMode?.Value == true)
            {
                Plugin.Log.LogDebug($"[Compression] {message}");
            }
        }
    }
}
