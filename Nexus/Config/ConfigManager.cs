using BepInEx.Configuration;

namespace Nexus.Config
{
    /// <summary>
    /// Manages all BepInEx configuration settings for the Nexus mod
    /// </summary>
    public class ConfigManager
    {
        private readonly ConfigFile _config;

        // Bandwidth Settings
        public ConfigEntry<int> SendRateLimit { get; private set; }
        public ConfigEntry<int> ReceiveRateLimit { get; private set; }
        public ConfigEntry<bool> UnlimitedBandwidth { get; private set; }

        // Compression Settings
        public ConfigEntry<bool> EnableCompression { get; private set; }
        public ConfigEntry<int> CompressionLevel { get; private set; }
        public ConfigEntry<int> CompressionThreshold { get; private set; }

        // Queue Settings
        public ConfigEntry<int> OutgoingQueueSize { get; private set; }
        public ConfigEntry<int> ConnectionBufferSize { get; private set; }
        public ConfigEntry<int> MaxQueueSize { get; private set; }

        // Update Rate Settings
        public ConfigEntry<int> DefaultUpdateRate { get; private set; }
        public ConfigEntry<int> MinUpdateRate { get; private set; }
        public ConfigEntry<bool> AutoAdjustUpdateRate { get; private set; }

        // Debug Settings
        public ConfigEntry<bool> ShowNetworkStats { get; private set; }
        public ConfigEntry<bool> DebugMode { get; private set; }
        public ConfigEntry<bool> LogNetworkEvents { get; private set; }
        public ConfigEntry<string> OverlayToggleKey { get; private set; }

        public ConfigManager(ConfigFile config)
        {
            _config = config;

            // Ensure configs are saved properly
            _config.SaveOnConfigSet = false;

            BindBandwidthSettings();
            BindCompressionSettings();
            BindQueueSettings();
            BindUpdateRateSettings();
            BindDebugSettings();

            _config.Save();
            _config.SaveOnConfigSet = true;

            Plugin.Log.LogInfo("Configuration bound successfully");
        }

        private void BindBandwidthSettings()
        {
            SendRateLimit = _config.Bind(
                "1. Bandwidth",
                "SendRateLimit",
                512000,
                new ConfigDescription(
                    "Maximum send rate in bytes per second (vanilla: ~50000). Set to 0 for unlimited.",
                    new AcceptableValueRange<int>(0, 10000000),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );

            ReceiveRateLimit = _config.Bind(
                "1. Bandwidth",
                "ReceiveRateLimit",
                512000,
                new ConfigDescription(
                    "Maximum receive rate in bytes per second (vanilla: ~50000). Set to 0 for unlimited.",
                    new AcceptableValueRange<int>(0, 10000000),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );

            UnlimitedBandwidth = _config.Bind(
                "1. Bandwidth",
                "UnlimitedBandwidth",
                false,
                new ConfigDescription(
                    "Completely remove all bandwidth limits (overrides SendRateLimit and ReceiveRateLimit)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );
        }

        private void BindCompressionSettings()
        {
            EnableCompression = _config.Bind(
                "2. Compression",
                "EnableCompression",
                true,
                new ConfigDescription(
                    "Enable network packet compression between Nexus clients",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );

            CompressionLevel = _config.Bind(
                "2. Compression",
                "CompressionLevel",
                6,
                new ConfigDescription(
                    "GZip compression level (1-9). Higher = better compression but more CPU usage",
                    new AcceptableValueRange<int>(1, 9),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );

            CompressionThreshold = _config.Bind(
                "2. Compression",
                "CompressionThreshold",
                128,
                new ConfigDescription(
                    "Minimum packet size in bytes before compression is applied",
                    new AcceptableValueRange<int>(0, 4096),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );
        }

        private void BindQueueSettings()
        {
            OutgoingQueueSize = _config.Bind(
                "3. Queue",
                "OutgoingQueueSize",
                49152,
                new ConfigDescription(
                    "Size of outgoing packet queue in bytes (vanilla: ~16KB)",
                    new AcceptableValueRange<int>(16384, 262144),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );

            ConnectionBufferSize = _config.Bind(
                "3. Queue",
                "ConnectionBufferSize",
                65536,
                new ConfigDescription(
                    "Size of connection buffer for handling packet bursts in bytes",
                    new AcceptableValueRange<int>(32768, 524288),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );

            MaxQueueSize = _config.Bind(
                "3. Queue",
                "MaxQueueSize",
                131072,
                new ConfigDescription(
                    "Maximum total queue size in bytes before packets are dropped",
                    new AcceptableValueRange<int>(65536, 1048576),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );
        }

        private void BindUpdateRateSettings()
        {
            DefaultUpdateRate = _config.Bind(
                "4. Update Rate",
                "DefaultUpdateRate",
                100,
                new ConfigDescription(
                    "Default network update rate percentage (100 = vanilla)",
                    new AcceptableValueRange<int>(25, 200),
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );

            MinUpdateRate = _config.Bind(
                "4. Update Rate",
                "MinUpdateRate",
                50,
                new ConfigDescription(
                    "Minimum update rate percentage when auto-adjusting",
                    new AcceptableValueRange<int>(10, 100),
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );

            AutoAdjustUpdateRate = _config.Bind(
                "4. Update Rate",
                "AutoAdjustUpdateRate",
                true,
                new ConfigDescription(
                    "Automatically adjust update rate based on connection quality",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );
        }

        private void BindDebugSettings()
        {
            ShowNetworkStats = _config.Bind(
                "5. Debug",
                "ShowNetworkStats",
                false,
                new ConfigDescription(
                    "Show network statistics overlay (enhances F2 display)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = false }
                )
            );

            DebugMode = _config.Bind(
                "5. Debug",
                "DebugMode",
                false,
                new ConfigDescription(
                    "Enable debug logging (verbose)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true }
                )
            );

            LogNetworkEvents = _config.Bind(
                "5. Debug",
                "LogNetworkEvents",
                false,
                new ConfigDescription(
                    "Log network events (connections, disconnections, packet stats)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = true, Advanced = true }
                )
            );

            OverlayToggleKey = _config.Bind(
                "5. Debug",
                "OverlayToggleKey",
                "F6",
                new ConfigDescription(
                    "Key to toggle the network stats overlay (e.g., F6, F7, F8)",
                    null,
                    new ConfigurationManagerAttributes { IsAdminOnly = false }
                )
            );
        }

        /// <summary>
        /// Get the effective send rate limit (accounting for unlimited mode)
        /// </summary>
        public int GetEffectiveSendRateLimit()
        {
            if (UnlimitedBandwidth.Value) return int.MaxValue;
            return SendRateLimit.Value == 0 ? int.MaxValue : SendRateLimit.Value;
        }

        /// <summary>
        /// Get the effective receive rate limit (accounting for unlimited mode)
        /// </summary>
        public int GetEffectiveReceiveRateLimit()
        {
            if (UnlimitedBandwidth.Value) return int.MaxValue;
            return ReceiveRateLimit.Value == 0 ? int.MaxValue : ReceiveRateLimit.Value;
        }
    }

    /// <summary>
    /// Attribute for BepInEx ConfigurationManager integration
    /// </summary>
    public class ConfigurationManagerAttributes
    {
        public bool? IsAdminOnly;
        public bool? Advanced;
        public int? Order;
    }
}
