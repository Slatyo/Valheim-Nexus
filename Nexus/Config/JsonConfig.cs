using System;
using System.IO;
using UnityEngine;

namespace Nexus.Config
{
    /// <summary>
    /// JSON configuration for server-enforced network settings
    /// </summary>
    [Serializable]
    public class JsonConfig
    {
        // Bandwidth settings (server-enforced maximums)
        public int SendRateLimit = 512000;
        public int ReceiveRateLimit = 512000;

        // Compression settings
        public bool EnableCompression = true;
        public bool ForceCompression = false;

        // Client override permissions
        public bool AllowClientOverride = true;
        public int MaxClientSendRate = 1000000;
        public int MaxClientReceiveRate = 1000000;

        // Queue settings
        public int OutgoingQueueSize = 49152;
        public int ConnectionBufferSize = 65536;

        // Update rate settings
        public int DefaultUpdateRate = 100;
        public int MinUpdateRate = 50;
        public bool AllowAutoAdjust = true;

        private static string ConfigPath => Path.Combine(BepInEx.Paths.ConfigPath, "Nexus.json");

        private static JsonConfig _instance;
        public static JsonConfig Instance
        {
            get
            {
                if (_instance == null)
                {
                    Load();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Load config from JSON file, create default if not exists
        /// </summary>
        public static void Load()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    string json = File.ReadAllText(ConfigPath);
                    _instance = JsonUtility.FromJson<JsonConfig>(json);
                    Plugin.Log.LogInfo($"Loaded JSON config from {ConfigPath}");

                    // Validate and clamp values
                    _instance.Validate();
                }
                else
                {
                    _instance = new JsonConfig();
                    Save();
                    Plugin.Log.LogInfo($"Created default JSON config at {ConfigPath}");
                }
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to load JSON config: {ex.Message}");
                _instance = new JsonConfig();
            }
        }

        /// <summary>
        /// Save current config to JSON file
        /// </summary>
        public static void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_instance, true);
                File.WriteAllText(ConfigPath, json);
                Plugin.Log.LogInfo($"Saved JSON config to {ConfigPath}");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"Failed to save JSON config: {ex.Message}");
            }
        }

        /// <summary>
        /// Validate and clamp all values to acceptable ranges
        /// </summary>
        private void Validate()
        {
            // Clamp SendRateLimit: 0 to 10MB/s
            if (SendRateLimit < 0)
            {
                Plugin.Log.LogWarning($"SendRateLimit {SendRateLimit} is below 0, clamping to 0 (unlimited)");
                SendRateLimit = 0;
            }
            else if (SendRateLimit > 10000000)
            {
                Plugin.Log.LogWarning($"SendRateLimit {SendRateLimit} exceeds 10MB/s, clamping to 10000000");
                SendRateLimit = 10000000;
            }

            // Clamp ReceiveRateLimit: 0 to 10MB/s
            if (ReceiveRateLimit < 0)
            {
                Plugin.Log.LogWarning($"ReceiveRateLimit {ReceiveRateLimit} is below 0, clamping to 0 (unlimited)");
                ReceiveRateLimit = 0;
            }
            else if (ReceiveRateLimit > 10000000)
            {
                Plugin.Log.LogWarning($"ReceiveRateLimit {ReceiveRateLimit} exceeds 10MB/s, clamping to 10000000");
                ReceiveRateLimit = 10000000;
            }

            // Clamp queue sizes
            OutgoingQueueSize = Math.Max(16384, Math.Min(OutgoingQueueSize, 262144));
            ConnectionBufferSize = Math.Max(32768, Math.Min(ConnectionBufferSize, 524288));

            // Clamp update rates
            DefaultUpdateRate = Math.Max(25, Math.Min(DefaultUpdateRate, 200));
            MinUpdateRate = Math.Max(10, Math.Min(MinUpdateRate, 100));

            // Clamp max client rates
            MaxClientSendRate = Math.Max(0, Math.Min(MaxClientSendRate, 10000000));
            MaxClientReceiveRate = Math.Max(0, Math.Min(MaxClientReceiveRate, 10000000));
        }

        /// <summary>
        /// Reload config from file (useful for runtime changes)
        /// </summary>
        public static void Reload()
        {
            _instance = null;
            Load();
        }

        /// <summary>
        /// Get effective send rate considering server limits
        /// </summary>
        public int GetEffectiveSendRate(int clientRequested)
        {
            if (!AllowClientOverride)
            {
                return SendRateLimit;
            }

            // Client can override, but capped at server's max
            int maxAllowed = MaxClientSendRate > 0 ? MaxClientSendRate : int.MaxValue;
            return Math.Min(clientRequested, maxAllowed);
        }

        /// <summary>
        /// Get effective receive rate considering server limits
        /// </summary>
        public int GetEffectiveReceiveRate(int clientRequested)
        {
            if (!AllowClientOverride)
            {
                return ReceiveRateLimit;
            }

            // Client can override, but capped at server's max
            int maxAllowed = MaxClientReceiveRate > 0 ? MaxClientReceiveRate : int.MaxValue;
            return Math.Min(clientRequested, maxAllowed);
        }
    }
}
