using System;
using Jotunn.Entities;
using Jotunn.Managers;

namespace Nexus.Commands
{
    /// <summary>
    /// Console commands for Nexus
    /// </summary>
    public static class NexusCommands
    {
        public static void Register()
        {
            // nexus - Show status
            CommandManager.Instance.AddConsoleCommand(new NexusStatusCommand());

            // nexus_test - Run diagnostics
            CommandManager.Instance.AddConsoleCommand(new NexusTestCommand());

            // nexus_report - Show last test report
            CommandManager.Instance.AddConsoleCommand(new NexusReportCommand());

            // nexus_stats - Toggle stats overlay
            CommandManager.Instance.AddConsoleCommand(new NexusStatsCommand());

            Plugin.Log.LogInfo("Nexus console commands registered");
        }
    }

    /// <summary>
    /// Show Nexus status
    /// </summary>
    public class NexusStatusCommand : ConsoleCommand
    {
        public override string Name => "nexus";
        public override string Help => "Show Nexus network optimizer status";

        public override void Run(string[] args)
        {
            Console.instance.Print("=== Nexus Network Optimizer ===");
            Console.instance.Print($"Version: {Plugin.PluginVersion}");
            Console.instance.Print($"Connected: {(ZNet.instance != null ? "Yes" : "No")}");

            if (Plugin.ConfigManager != null)
            {
                Console.instance.Print($"Send Limit: {Plugin.ConfigManager.SendRateLimit.Value} bytes/s");
                Console.instance.Print($"Compression: {(Plugin.ConfigManager.EnableCompression.Value ? "Enabled" : "Disabled")}");
                Console.instance.Print($"Update Rate: {Plugin.ConfigManager.DefaultUpdateRate.Value}%");
            }

            if (Plugin.NetworkStats != null)
            {
                Console.instance.Print($"Quality Score: {Plugin.NetworkStats.QualityScore}/100");
            }

            Console.instance.Print("");
            Console.instance.Print("Commands:");
            Console.instance.Print("  nexus_test  - Run network performance test");
            Console.instance.Print("  nexus_report - Show last test results");
            Console.instance.Print("  nexus_stats - Toggle stats overlay");
        }
    }

    /// <summary>
    /// Run network diagnostics test
    /// </summary>
    public class NexusTestCommand : ConsoleCommand
    {
        public override string Name => "nexus_test";
        public override string Help => "Run a 5-second network performance test";

        public override void Run(string[] args)
        {
            if (Plugin.Diagnostics == null)
            {
                Console.instance.Print("Diagnostics not available");
                return;
            }

            if (Plugin.Diagnostics.Status == Network.NetworkDiagnostics.TestStatus.Running)
            {
                Console.instance.Print("Test already running...");
                return;
            }

            if (Plugin.Diagnostics.StartTest())
            {
                Console.instance.Print("Starting network performance test (5 seconds)...");
                Console.instance.Print("Use 'nexus_report' to view results when complete.");
            }
            else
            {
                Console.instance.Print("Failed to start test. Make sure you're connected to a server.");
            }
        }
    }

    /// <summary>
    /// Show last test report
    /// </summary>
    public class NexusReportCommand : ConsoleCommand
    {
        public override string Name => "nexus_report";
        public override string Help => "Show the last network test report";

        public override void Run(string[] args)
        {
            if (Plugin.Diagnostics == null)
            {
                Console.instance.Print("Diagnostics not available");
                return;
            }

            if (Plugin.Diagnostics.Status == Network.NetworkDiagnostics.TestStatus.Running)
            {
                float progress = Plugin.Diagnostics.TestProgress * 100f;
                Console.instance.Print($"Test in progress... {progress:F0}%");
                return;
            }

            string report = Plugin.Diagnostics.GetReport();
            foreach (var line in report.Split('\n'))
            {
                Console.instance.Print(line);
            }
        }
    }

    /// <summary>
    /// Toggle stats overlay
    /// </summary>
    public class NexusStatsCommand : ConsoleCommand
    {
        public override string Name => "nexus_stats";
        public override string Help => "Toggle the network stats overlay";

        public override void Run(string[] args)
        {
            UI.DebugOverlay.Toggle();
            Console.instance.Print($"Stats overlay: {(UI.DebugOverlay.IsVisible ? "Shown" : "Hidden")}");
        }
    }
}
