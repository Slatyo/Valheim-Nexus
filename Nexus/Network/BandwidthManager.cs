using System;
using System.Collections.Generic;

namespace Nexus.Network
{
    /// <summary>
    /// Manages bandwidth limits and rate limiting for network traffic
    /// </summary>
    public class BandwidthManager
    {
        // Current rate limits
        private int _sendRateLimit;
        private int _receiveRateLimit;

        // Tracking for rate limiting
        private long _bytesSentThisSecond;
        private long _bytesReceivedThisSecond;
        private float _lastResetTime;

        // Statistics
        private long _totalBytesSent;
        private long _totalBytesReceived;
        private readonly Queue<long> _sendRateHistory = new Queue<long>();
        private readonly Queue<long> _receiveRateHistory = new Queue<long>();

        private const int RATE_HISTORY_SIZE = 10;

        public BandwidthManager()
        {
            RefreshLimits();
            _lastResetTime = UnityEngine.Time.time;
        }

        /// <summary>
        /// Refresh rate limits from configuration
        /// </summary>
        public void RefreshLimits()
        {
            if (Plugin.ConfigManager != null)
            {
                _sendRateLimit = Plugin.ConfigManager.GetEffectiveSendRateLimit();
                _receiveRateLimit = Plugin.ConfigManager.GetEffectiveReceiveRateLimit();

                LogDebug($"Bandwidth limits refreshed - Send: {FormatBytes(_sendRateLimit)}/s, Receive: {FormatBytes(_receiveRateLimit)}/s");
            }
        }

        /// <summary>
        /// Check if we can send data without exceeding rate limit
        /// </summary>
        public bool CanSend(int bytes)
        {
            ResetCountersIfNeeded();

            if (_sendRateLimit == int.MaxValue)
                return true;

            return (_bytesSentThisSecond + bytes) <= _sendRateLimit;
        }

        /// <summary>
        /// Check if we can receive data without exceeding rate limit
        /// </summary>
        public bool CanReceive(int bytes)
        {
            ResetCountersIfNeeded();

            if (_receiveRateLimit == int.MaxValue)
                return true;

            return (_bytesReceivedThisSecond + bytes) <= _receiveRateLimit;
        }

        /// <summary>
        /// Record bytes sent
        /// </summary>
        public void RecordSent(int bytes)
        {
            ResetCountersIfNeeded();
            _bytesSentThisSecond += bytes;
            _totalBytesSent += bytes;
        }

        /// <summary>
        /// Record bytes received
        /// </summary>
        public void RecordReceived(int bytes)
        {
            ResetCountersIfNeeded();
            _bytesReceivedThisSecond += bytes;
            _totalBytesReceived += bytes;
        }

        /// <summary>
        /// Get the current send rate (bytes per second)
        /// </summary>
        public long GetCurrentSendRate()
        {
            if (_sendRateHistory.Count == 0) return 0;

            long total = 0;
            foreach (var rate in _sendRateHistory)
                total += rate;

            return total / _sendRateHistory.Count;
        }

        /// <summary>
        /// Get the current receive rate (bytes per second)
        /// </summary>
        public long GetCurrentReceiveRate()
        {
            if (_receiveRateHistory.Count == 0) return 0;

            long total = 0;
            foreach (var rate in _receiveRateHistory)
                total += rate;

            return total / _receiveRateHistory.Count;
        }

        /// <summary>
        /// Get remaining send capacity this second
        /// </summary>
        public long GetRemainingSendCapacity()
        {
            if (_sendRateLimit == int.MaxValue)
                return int.MaxValue;

            return Math.Max(0, _sendRateLimit - _bytesSentThisSecond);
        }

        /// <summary>
        /// Get remaining receive capacity this second
        /// </summary>
        public long GetRemainingReceiveCapacity()
        {
            if (_receiveRateLimit == int.MaxValue)
                return int.MaxValue;

            return Math.Max(0, _receiveRateLimit - _bytesReceivedThisSecond);
        }

        /// <summary>
        /// Get utilization percentage of send bandwidth
        /// </summary>
        public float GetSendUtilization()
        {
            if (_sendRateLimit == int.MaxValue || _sendRateLimit == 0)
                return 0;

            return (float)_bytesSentThisSecond / _sendRateLimit * 100f;
        }

        /// <summary>
        /// Get utilization percentage of receive bandwidth
        /// </summary>
        public float GetReceiveUtilization()
        {
            if (_receiveRateLimit == int.MaxValue || _receiveRateLimit == 0)
                return 0;

            return (float)_bytesReceivedThisSecond / _receiveRateLimit * 100f;
        }

        public long TotalBytesSent => _totalBytesSent;
        public long TotalBytesReceived => _totalBytesReceived;
        public int SendRateLimit => _sendRateLimit;
        public int ReceiveRateLimit => _receiveRateLimit;

        private void ResetCountersIfNeeded()
        {
            float currentTime = UnityEngine.Time.time;

            if (currentTime - _lastResetTime >= 1.0f)
            {
                // Store in history
                _sendRateHistory.Enqueue(_bytesSentThisSecond);
                _receiveRateHistory.Enqueue(_bytesReceivedThisSecond);

                // Keep history limited
                while (_sendRateHistory.Count > RATE_HISTORY_SIZE)
                    _sendRateHistory.Dequeue();
                while (_receiveRateHistory.Count > RATE_HISTORY_SIZE)
                    _receiveRateHistory.Dequeue();

                // Reset counters
                _bytesSentThisSecond = 0;
                _bytesReceivedThisSecond = 0;
                _lastResetTime = currentTime;
            }
        }

        public void Cleanup()
        {
            _sendRateHistory.Clear();
            _receiveRateHistory.Clear();
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }

        private void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.DebugMode?.Value == true)
            {
                Plugin.Log.LogDebug($"[Bandwidth] {message}");
            }
        }
    }
}
