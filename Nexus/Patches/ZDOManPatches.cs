using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace Nexus.Patches
{
    /// <summary>
    /// Patches for ZDOMan to modify update rates and ZDO synchronization
    /// </summary>
    [HarmonyPatch]
    public static class ZDOManPatches
    {
        private static float _lastUpdateRateLog;
        private static int _zdosSentThisFrame;

        /// <summary>
        /// Track ZDOs being sent
        /// </summary>
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOs))]
        [HarmonyPrefix]
        public static void SendZDOs_Prefix(ZDOMan __instance, ZDOMan.ZDOPeer peer, bool flush)
        {
            _zdosSentThisFrame = 0;
        }

        /// <summary>
        /// Track ZDOs sent count after method completes
        /// </summary>
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOs))]
        [HarmonyPostfix]
        public static void SendZDOs_Postfix(ZDOMan __instance, ZDOMan.ZDOPeer peer, bool flush)
        {
            try
            {
                if (_zdosSentThisFrame > 0)
                {
                    LogDebug($"Sent {_zdosSentThisFrame} ZDOs to peer");
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        /// <summary>
        /// Track ZDOs being received
        /// </summary>
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.RPC_ZDOData))]
        [HarmonyPostfix]
        public static void RPC_ZDOData_Postfix(ZDOMan __instance, ZRpc rpc, ZPackage pkg)
        {
            try
            {
                if (__instance == null) return;

                // Log periodic update rate information
                float currentTime = UnityEngine.Time.time;
                if (currentTime - _lastUpdateRateLog > 5.0f)
                {
                    _lastUpdateRateLog = currentTime;

                    if (Plugin.ConfigManager?.DebugMode?.Value == true)
                    {
                        int updateRate = Plugin.ConfigManager?.DefaultUpdateRate?.Value ?? 100;
                        int zdoCount = __instance.m_objectsByID?.Count ?? 0;
                        Plugin.Log.LogDebug($"[ZDOMan] Update rate: {updateRate}%, ZDO count: {zdoCount}");
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }
        }

        /// <summary>
        /// Modify the base send rate if configured
        /// This patches the SendZDOs method to adjust how many ZDOs are sent per frame
        /// </summary>
        [HarmonyPatch(typeof(ZDOMan), nameof(ZDOMan.SendZDOs))]
        [HarmonyTranspiler]
        public static IEnumerable<CodeInstruction> SendZDOs_Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            // This is a placeholder transpiler that doesn't modify anything yet
            // In Phase 2, this would be used to adjust the ZDO send rate based on configuration
            // The actual implementation depends on the specific IL structure of SendZDOs

            foreach (var instruction in instructions)
            {
                yield return instruction;
            }
        }

        /// <summary>
        /// Get adjusted update rate multiplier (0.0 to 2.0)
        /// </summary>
        public static float GetUpdateRateMultiplier()
        {
            int updateRate = Plugin.ConfigManager?.DefaultUpdateRate?.Value ?? 100;
            return updateRate / 100f;
        }

        /// <summary>
        /// Check if auto-adjust is enabled and connection quality is low
        /// </summary>
        public static bool ShouldReduceUpdateRate()
        {
            if (Plugin.ConfigManager?.AutoAdjustUpdateRate?.Value != true)
                return false;

            // Check if quality score indicates poor connection
            int qualityScore = Plugin.NetworkStats?.QualityScore ?? 100;
            return qualityScore < 50;
        }

        /// <summary>
        /// Get the auto-adjusted update rate considering connection quality
        /// </summary>
        public static float GetAutoAdjustedUpdateRate()
        {
            float baseRate = GetUpdateRateMultiplier();

            if (!ShouldReduceUpdateRate())
                return baseRate;

            int qualityScore = Plugin.NetworkStats?.QualityScore ?? 100;
            int minRate = Plugin.ConfigManager?.MinUpdateRate?.Value ?? 50;

            // Scale down based on quality score
            float qualityMultiplier = qualityScore / 100f;
            float adjustedRate = baseRate * qualityMultiplier;

            // Don't go below minimum rate
            return Math.Max(adjustedRate, minRate / 100f);
        }

        private static void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.LogNetworkEvents?.Value == true)
            {
                Plugin.Log.LogDebug($"[ZDOMan] {message}");
            }
        }
    }
}
