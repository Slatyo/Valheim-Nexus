using System;
using HarmonyLib;

namespace Nexus.Patches
{
    /// <summary>
    /// Patches for ZPlayFabSocket to modify bandwidth limits for crossplay connections
    /// </summary>
    [HarmonyPatch]
    public static class ZPlayFabSocketPatches
    {
        /// <summary>
        /// Patch the send rate limit in ZPlayFabSocket
        /// </summary>
        [HarmonyPatch(typeof(ZPlayFabSocket), nameof(ZPlayFabSocket.Send), new Type[] { typeof(ZPackage) })]
        [HarmonyPrefix]
        public static void Send_Prefix(ZPlayFabSocket __instance, ZPackage pkg)
        {
            try
            {
                if (__instance == null || pkg == null) return;

                int packetSize = pkg.Size();

                // Track bandwidth
                Plugin.BandwidthManager?.RecordSent(packetSize);

                LogDebug($"PlayFab Send: {packetSize} bytes");
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZPlayFabSocket] Send_Prefix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch the receive handling in ZPlayFabSocket
        /// </summary>
        [HarmonyPatch(typeof(ZPlayFabSocket), nameof(ZPlayFabSocket.Recv))]
        [HarmonyPostfix]
        public static void Recv_Postfix(ZPlayFabSocket __instance, ref ZPackage __result)
        {
            try
            {
                if (__instance == null || __result == null) return;

                int packetSize = __result.Size();

                // Track bandwidth
                Plugin.BandwidthManager?.RecordReceived(packetSize);

                LogDebug($"PlayFab Recv: {packetSize} bytes");
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZPlayFabSocket] Recv_Postfix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patch PlayFab socket close to cleanup
        /// </summary>
        [HarmonyPatch(typeof(ZPlayFabSocket), nameof(ZPlayFabSocket.Close))]
        [HarmonyPrefix]
        public static void Close_Prefix(ZPlayFabSocket __instance)
        {
            try
            {
                LogDebug("PlayFab connection closing");
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        private static void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.LogNetworkEvents?.Value == true)
            {
                Plugin.Log.LogDebug($"[ZPlayFabSocket] {message}");
            }
        }
    }
}
