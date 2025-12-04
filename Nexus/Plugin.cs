using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Jotunn.Utils;
using Nexus.Commands;
using Nexus.Config;
using Nexus.Network;
using Nexus.UI;

namespace Nexus
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    [BepInDependency("com.slaty.munin")]
    [BepInDependency("com.slatyo.veneer")]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGUID = "com.nexus.valheim";
        public const string PluginName = "Nexus";
        public const string PluginVersion = "1.0.0";

        public static Plugin Instance { get; private set; }
        public static ManualLogSource Log { get; private set; }

        private Harmony _harmony;

        public static ConfigManager ConfigManager { get; private set; }
        public static BandwidthManager BandwidthManager { get; private set; }
        public static CompressionManager CompressionManager { get; private set; }
        public static QueueManager QueueManager { get; private set; }
        public static NetworkStats NetworkStats { get; private set; }
        public static NetworkDiagnostics Diagnostics { get; private set; }

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            Log.LogInfo($"{PluginName} v{PluginVersion} is loading...");

            // Load configuration
            ConfigManager = new ConfigManager(Config);
            JsonConfig.Load();

            Log.LogInfo($"Config loaded - SendRateLimit: {JsonConfig.Instance.SendRateLimit} bytes/s, Compression: {JsonConfig.Instance.EnableCompression}");

            // Initialize managers
            BandwidthManager = new BandwidthManager();
            CompressionManager = new CompressionManager();
            QueueManager = new QueueManager();
            NetworkStats = new NetworkStats();
            Diagnostics = new NetworkDiagnostics();

            // Apply Harmony patches
            _harmony = new Harmony(PluginGUID);
            _harmony.PatchAll();

            // Register console commands
            NexusCommands.Register();

            // Create debug overlay
            DebugOverlay.Create();

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded successfully");
            Log.LogInfo($"Press {ConfigManager.OverlayToggleKey.Value} to toggle network stats overlay");
            Log.LogInfo("Type 'munin nexus' in console for commands");
        }

        private void Update()
        {
            NetworkStats?.Update();
            Diagnostics?.Update();
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            NexusCommands.Unregister();
            DebugOverlay.Destroy();
            BandwidthManager?.Cleanup();
            CompressionManager?.Cleanup();
            QueueManager?.Cleanup();
            NetworkStats?.Cleanup();
        }

        public static bool IsServer() => ZNet.instance != null && ZNet.instance.IsServer();
        public static bool IsClient() => ZNet.instance != null && !ZNet.instance.IsServer();
        public static bool IsSinglePlayer() => ZNet.instance != null && !ZNet.instance.IsDedicated() && ZNet.instance.GetNrOfPlayers() <= 1;
        public static bool IsDedicatedServer() => ZNet.instance != null && ZNet.instance.IsDedicated();
    }
}
