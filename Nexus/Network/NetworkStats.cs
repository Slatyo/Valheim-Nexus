using System;
using UnityEngine;

namespace Nexus.Network
{
    /// <summary>
    /// Tracks and displays network statistics
    /// </summary>
    public class NetworkStats
    {
        // Timing
        private float _lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.5f;

        // Snapshot data for display
        private long _displaySendRate;
        private long _displayReceiveRate;
        private float _displaySendUtilization;
        private float _displayReceiveUtilization;
        private float _displayCompressionRatio;
        private int _displayQueueSize;

        // Connection quality tracking
        private float _pingAverage;
        private float _packetLossPercent;
        private int _qualityScore; // 0-100

        public NetworkStats()
        {
            _lastUpdateTime = Time.time;
        }

        /// <summary>
        /// Update statistics (called from Plugin.Update)
        /// </summary>
        public void Update()
        {
            if (Time.time - _lastUpdateTime < UPDATE_INTERVAL)
                return;

            _lastUpdateTime = Time.time;

            // Update snapshot data
            if (Plugin.BandwidthManager != null)
            {
                _displaySendRate = Plugin.BandwidthManager.GetCurrentSendRate();
                _displayReceiveRate = Plugin.BandwidthManager.GetCurrentReceiveRate();
                _displaySendUtilization = Plugin.BandwidthManager.GetSendUtilization();
                _displayReceiveUtilization = Plugin.BandwidthManager.GetReceiveUtilization();
            }

            if (Plugin.CompressionManager != null)
            {
                _displayCompressionRatio = Plugin.CompressionManager.GetCompressionRatio();
            }

            if (Plugin.QueueManager != null)
            {
                _displayQueueSize = Plugin.QueueManager.CurrentQueueSize;
            }

            // Calculate connection quality score
            CalculateQualityScore();
        }

        /// <summary>
        /// Calculate overall connection quality score
        /// </summary>
        private void CalculateQualityScore()
        {
            int score = 100;

            // Penalize high bandwidth utilization
            if (_displaySendUtilization > 90f) score -= 20;
            else if (_displaySendUtilization > 75f) score -= 10;

            if (_displayReceiveUtilization > 90f) score -= 20;
            else if (_displayReceiveUtilization > 75f) score -= 10;

            // Penalize high packet loss
            if (_packetLossPercent > 5f) score -= 30;
            else if (_packetLossPercent > 2f) score -= 15;
            else if (_packetLossPercent > 0.5f) score -= 5;

            // Penalize high ping
            if (_pingAverage > 200f) score -= 20;
            else if (_pingAverage > 100f) score -= 10;
            else if (_pingAverage > 50f) score -= 5;

            _qualityScore = Math.Max(0, Math.Min(100, score));
        }

        /// <summary>
        /// Get formatted statistics string for display
        /// </summary>
        public string GetStatsDisplay()
        {
            return $"Nexus Network Stats\n" +
                   $"Send: {FormatBytes(_displaySendRate)}/s ({_displaySendUtilization:F1}%)\n" +
                   $"Recv: {FormatBytes(_displayReceiveRate)}/s ({_displayReceiveUtilization:F1}%)\n" +
                   $"Compression: {_displayCompressionRatio * 100:F1}% saved\n" +
                   $"Queue: {FormatBytes(_displayQueueSize)}\n" +
                   $"Quality: {_qualityScore}/100";
        }

        /// <summary>
        /// Get brief one-line stats
        /// </summary>
        public string GetBriefStats()
        {
            return $"TX:{FormatBytesShort(_displaySendRate)}/s RX:{FormatBytesShort(_displayReceiveRate)}/s Q:{_qualityScore}";
        }

        /// <summary>
        /// Record ping measurement
        /// </summary>
        public void RecordPing(float pingMs)
        {
            // Simple exponential moving average
            _pingAverage = _pingAverage * 0.8f + pingMs * 0.2f;
        }

        /// <summary>
        /// Record packet loss
        /// </summary>
        public void RecordPacketLoss(float lossPercent)
        {
            _packetLossPercent = _packetLossPercent * 0.8f + lossPercent * 0.2f;
        }

        public long DisplaySendRate => _displaySendRate;
        public long DisplayReceiveRate => _displayReceiveRate;
        public float DisplaySendUtilization => _displaySendUtilization;
        public float DisplayReceiveUtilization => _displayReceiveUtilization;
        public float DisplayCompressionRatio => _displayCompressionRatio;
        public int DisplayQueueSize => _displayQueueSize;
        public float PingAverage => _pingAverage;
        public float PacketLossPercent => _packetLossPercent;
        public int QualityScore => _qualityScore;

        public void Cleanup()
        {
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private static string FormatBytesShort(long bytes)
        {
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F1}M";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F0}K";
            return $"{bytes}B";
        }
    }
}
