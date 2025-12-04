using Munin;

namespace Nexus.Commands
{
    /// <summary>
    /// Console commands for Nexus via Munin.
    /// Usage: munin nexus [command]
    /// </summary>
    public static class NexusCommands
    {
        public static void Register()
        {
            Command.RegisterMany("nexus",
                new CommandConfig
                {
                    Name = "status",
                    Description = "Show Nexus network optimizer status",
                    Permission = PermissionLevel.Anyone,
                    Handler = args =>
                    {
                        var lines = new System.Collections.Generic.List<string>
                        {
                            $"<color=#{ChatColor.Gold}>Nexus Network Optimizer</color>",
                            $"Version: {Plugin.PluginVersion}",
                            $"Connected: {(ZNet.instance != null ? "Yes" : "No")}"
                        };

                        if (Plugin.ConfigManager != null)
                        {
                            lines.Add($"Send Limit: {Plugin.ConfigManager.SendRateLimit.Value} bytes/s");
                            lines.Add($"Compression: {(Plugin.ConfigManager.EnableCompression.Value ? "Enabled" : "Disabled")}");
                            lines.Add($"Update Rate: {Plugin.ConfigManager.DefaultUpdateRate.Value}%");
                        }

                        if (Plugin.NetworkStats != null)
                        {
                            lines.Add($"Quality Score: {Plugin.NetworkStats.QualityScore}/100");
                        }

                        return CommandResult.Info(string.Join("\n", lines));
                    }
                },
                new CommandConfig
                {
                    Name = "test",
                    Description = "Run a 5-second network performance test",
                    Permission = PermissionLevel.Admin,
                    Handler = args =>
                    {
                        if (Plugin.Diagnostics == null)
                            return CommandResult.Error("Diagnostics not available");

                        if (Plugin.Diagnostics.Status == Network.NetworkDiagnostics.TestStatus.Running)
                            return CommandResult.Info("Test already running...");

                        if (Plugin.Diagnostics.StartTest())
                        {
                            return CommandResult.Success("Starting network performance test (5 seconds)...\nUse 'munin nexus report' to view results when complete.");
                        }

                        return CommandResult.Error("Failed to start test. Make sure you're connected to a server.");
                    }
                },
                new CommandConfig
                {
                    Name = "report",
                    Description = "Show the last network test report",
                    Permission = PermissionLevel.Anyone,
                    Handler = args =>
                    {
                        if (Plugin.Diagnostics == null)
                            return CommandResult.Error("Diagnostics not available");

                        if (Plugin.Diagnostics.Status == Network.NetworkDiagnostics.TestStatus.Running)
                        {
                            float progress = Plugin.Diagnostics.TestProgress * 100f;
                            return CommandResult.Info($"Test in progress... {progress:F0}%");
                        }

                        return CommandResult.Info(Plugin.Diagnostics.GetReport());
                    }
                },
                new CommandConfig
                {
                    Name = "stats",
                    Description = "Toggle the network stats overlay",
                    Permission = PermissionLevel.Anyone,
                    Handler = args =>
                    {
                        UI.DebugOverlay.Toggle();
                        return CommandResult.Success($"Stats overlay: {(UI.DebugOverlay.IsVisible ? "Shown" : "Hidden")}");
                    }
                }
            );

            Plugin.Log.LogInfo("Nexus commands registered with Munin");
        }

        public static void Unregister()
        {
            Command.UnregisterMod("nexus");
        }
    }
}
