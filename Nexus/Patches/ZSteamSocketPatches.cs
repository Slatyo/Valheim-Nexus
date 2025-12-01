using System;
using HarmonyLib;

namespace Nexus.Patches
{
    /// <summary>
    /// Patches for ZSteamSocket to modify bandwidth limits and enable compression
    /// </summary>
    [HarmonyPatch]
    public static class ZSteamSocketPatches
    {
        /// <summary>
        /// Patch the send rate limit in ZSteamSocket
        /// </summary>
        [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Send), new Type[] { typeof(ZPackage) })]
        [HarmonyPrefix]
        public static void Send_Prefix(ZSteamSocket __instance, ZPackage pkg)
        {
            try
            {
                if (__instance == null || pkg == null) return;

                int packetSize = pkg.Size();

                // Track bandwidth
                Plugin.BandwidthManager?.RecordSent(packetSize);

                // Track queue using the socket's hash code as identifier
                Plugin.QueueManager?.RecordQueued(__instance.GetHashCode(), packetSize);

                LogDebug($"Send: {packetSize} bytes");
            }
            catch (Exception ex)
            {
                // Silently ignore errors to not disrupt game networking
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZSteamSocket] Send_Prefix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch the receive handling in ZSteamSocket
        /// </summary>
        [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Recv))]
        [HarmonyPostfix]
        public static void Recv_Postfix(ZSteamSocket __instance, ref ZPackage __result)
        {
            try
            {
                if (__instance == null || __result == null) return;

                int packetSize = __result.Size();

                // Track bandwidth
                Plugin.BandwidthManager?.RecordReceived(packetSize);

                LogDebug($"Recv: {packetSize} bytes");
            }
            catch (Exception ex)
            {
                // Silently ignore errors to not disrupt game networking
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZSteamSocket] Recv_Postfix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch ZSteamSocket connection to register with QueueManager
        /// </summary>
        [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.StartHost))]
        [HarmonyPostfix]
        public static void StartHost_Postfix(ZSteamSocket __instance)
        {
            try
            {
                if (__instance == null) return;

                Plugin.QueueManager?.RegisterConnection(__instance.GetHashCode());
                LogDebug("Registered host connection");
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZSteamSocket] StartHost_Postfix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch socket close to unregister from QueueManager
        /// </summary>
        [HarmonyPatch(typeof(ZSteamSocket), nameof(ZSteamSocket.Close))]
        [HarmonyPrefix]
        public static void Close_Prefix(ZSteamSocket __instance)
        {
            try
            {
                if (__instance == null) return;

                Plugin.QueueManager?.UnregisterConnection(__instance.GetHashCode());
                Plugin.CompressionManager?.UnregisterNexusPeer(__instance.GetHashCode());
                LogDebug("Unregistered connection on close");
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZSteamSocket] Close_Prefix error: {ex.Message}");
                }
            }
        }

        private static void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.LogNetworkEvents?.Value == true)
            {
                Plugin.Log.LogDebug($"[ZSteamSocket] {message}");
            }
        }
    }
}
