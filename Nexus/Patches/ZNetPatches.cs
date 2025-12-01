using System;
using HarmonyLib;

namespace Nexus.Patches
{
    /// <summary>
    /// Patches for ZNet to handle connection events and Nexus peer detection
    /// </summary>
    [HarmonyPatch]
    public static class ZNetPatches
    {
        /// <summary>
        /// Handle new peer connection
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.RPC_PeerInfo))]
        [HarmonyPostfix]
        public static void RPC_PeerInfo_Postfix(ZNet __instance, ZRpc rpc, ZPackage pkg)
        {
            try
            {
                if (__instance == null || rpc == null) return;

                // Find the peer that just connected
                foreach (var peer in __instance.m_peers)
                {
                    if (peer != null && peer.m_rpc == rpc)
                    {
                        long peerId = peer.m_uid;

                        // Register connection with queue manager
                        Plugin.QueueManager?.RegisterConnection(peerId);

                        // Log connection
                        if (Plugin.ConfigManager?.LogNetworkEvents?.Value == true)
                        {
                            Plugin.Log.LogInfo($"[ZNet] Peer connected: {peer.m_playerName} ({peerId})");
                        }

                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZNet] RPC_PeerInfo_Postfix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle peer disconnection
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Disconnect))]
        [HarmonyPrefix]
        public static void Disconnect_Prefix(ZNet __instance, ZNetPeer peer)
        {
            try
            {
                if (peer != null)
                {
                    long peerId = peer.m_uid;

                    // Unregister from managers
                    Plugin.QueueManager?.UnregisterConnection(peerId);
                    Plugin.CompressionManager?.UnregisterNexusPeer(peerId);

                    if (Plugin.ConfigManager?.LogNetworkEvents?.Value == true)
                    {
                        Plugin.Log.LogInfo($"[ZNet] Peer disconnected: {peer.m_playerName} ({peerId})");
                    }
                }
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZNet] Disconnect_Prefix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Handle server start
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.Awake))]
        [HarmonyPostfix]
        public static void Awake_Postfix(ZNet __instance)
        {
            try
            {
                if (__instance == null) return;

                Plugin.Log.LogInfo($"[ZNet] Network initialized - Server: {__instance.IsServer()}, Dedicated: {__instance.IsDedicated()}");

                // Refresh managers with current settings
                Plugin.BandwidthManager?.RefreshLimits();
                Plugin.QueueManager?.RefreshSettings();
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[ZNet] Awake_Postfix error: {ex.Message}");
            }
        }

        /// <summary>
        /// Handle server shutdown
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.OnDestroy))]
        [HarmonyPrefix]
        public static void OnDestroy_Prefix(ZNet __instance)
        {
            try
            {
                Plugin.Log.LogInfo("[ZNet] Network shutting down");

                // Cleanup managers
                Plugin.QueueManager?.Cleanup();
                Plugin.CompressionManager?.Cleanup();
            }
            catch (Exception ex)
            {
                if (Plugin.ConfigManager?.DebugMode?.Value == true)
                {
                    Plugin.Log.LogWarning($"[ZNet] OnDestroy_Prefix error: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Track ping/latency
        /// </summary>
        [HarmonyPatch(typeof(ZNet), nameof(ZNet.UpdateNetTime))]
        [HarmonyPostfix]
        public static void UpdateNetTime_Postfix(ZNet __instance)
        {
            try
            {
                if (__instance == null) return;

                // Get average ping from server peer if we're a client
                if (!__instance.IsServer())
                {
                    var serverPeer = __instance.GetServerPeer();
                    if (serverPeer?.m_rpc != null)
                    {
                        float rtt = serverPeer.m_rpc.GetTimeSinceLastPing();
                        Plugin.NetworkStats?.RecordPing(rtt * 1000f); // Convert to ms
                    }
                }
            }
            catch (Exception)
            {
                // Silently ignore - this runs frequently
            }
        }
    }
}
