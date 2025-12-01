using System;
using System.Collections.Generic;

namespace Nexus.Network
{
    /// <summary>
    /// Manages network queue sizes and buffer management
    /// </summary>
    public class QueueManager
    {
        // Queue size configuration
        private int _outgoingQueueSize;
        private int _connectionBufferSize;

        // Queue statistics
        private long _packetsQueued;
        private long _packetsDropped;
        private long _queueOverflows;
        private int _peakQueueSize;
        private int _currentQueueSize;

        // Per-connection queue tracking
        private readonly Dictionary<long, ConnectionQueueInfo> _connectionQueues = new Dictionary<long, ConnectionQueueInfo>();

        public QueueManager()
        {
            RefreshSettings();
        }

        /// <summary>
        /// Refresh queue settings from configuration
        /// </summary>
        public void RefreshSettings()
        {
            if (Plugin.ConfigManager != null)
            {
                _outgoingQueueSize = Plugin.ConfigManager.OutgoingQueueSize.Value;
                _connectionBufferSize = Plugin.ConfigManager.ConnectionBufferSize.Value;

                LogDebug($"Queue settings refreshed - OutgoingQueue: {_outgoingQueueSize} bytes, Buffer: {_connectionBufferSize} bytes");
            }
        }

        /// <summary>
        /// Get the configured outgoing queue size
        /// </summary>
        public int GetOutgoingQueueSize()
        {
            return _outgoingQueueSize;
        }

        /// <summary>
        /// Get the configured connection buffer size
        /// </summary>
        public int GetConnectionBufferSize()
        {
            return _connectionBufferSize;
        }

        /// <summary>
        /// Register a new connection
        /// </summary>
        public void RegisterConnection(long connectionId)
        {
            if (!_connectionQueues.ContainsKey(connectionId))
            {
                _connectionQueues[connectionId] = new ConnectionQueueInfo
                {
                    ConnectionId = connectionId,
                    QueueSize = 0,
                    MaxQueueSize = _outgoingQueueSize,
                    PacketsQueued = 0,
                    PacketsDropped = 0
                };

                LogDebug($"Registered connection queue: {connectionId}");
            }
        }

        /// <summary>
        /// Unregister a connection
        /// </summary>
        public void UnregisterConnection(long connectionId)
        {
            if (_connectionQueues.Remove(connectionId))
            {
                LogDebug($"Unregistered connection queue: {connectionId}");
            }
        }

        /// <summary>
        /// Check if a packet can be queued for a connection
        /// </summary>
        public bool CanQueue(long connectionId, int packetSize)
        {
            if (_connectionQueues.TryGetValue(connectionId, out var info))
            {
                return (info.QueueSize + packetSize) <= info.MaxQueueSize;
            }

            // Unknown connection, allow by default
            return true;
        }

        /// <summary>
        /// Record a packet being queued
        /// </summary>
        public void RecordQueued(long connectionId, int packetSize)
        {
            _packetsQueued++;

            if (_connectionQueues.TryGetValue(connectionId, out var info))
            {
                info.QueueSize += packetSize;
                info.PacketsQueued++;

                if (info.QueueSize > _peakQueueSize)
                {
                    _peakQueueSize = info.QueueSize;
                }

                _currentQueueSize = CalculateTotalQueueSize();
            }
        }

        /// <summary>
        /// Record a packet being sent (dequeued)
        /// </summary>
        public void RecordSent(long connectionId, int packetSize)
        {
            if (_connectionQueues.TryGetValue(connectionId, out var info))
            {
                info.QueueSize = Math.Max(0, info.QueueSize - packetSize);
                _currentQueueSize = CalculateTotalQueueSize();
            }
        }

        /// <summary>
        /// Record a packet being dropped due to queue overflow
        /// </summary>
        public void RecordDropped(long connectionId, int packetSize)
        {
            _packetsDropped++;
            _queueOverflows++;

            if (_connectionQueues.TryGetValue(connectionId, out var info))
            {
                info.PacketsDropped++;
            }

            LogDebug($"Packet dropped for connection {connectionId}: queue full");
        }

        /// <summary>
        /// Get queue usage percentage for a connection
        /// </summary>
        public float GetQueueUsage(long connectionId)
        {
            if (_connectionQueues.TryGetValue(connectionId, out var info))
            {
                if (info.MaxQueueSize == 0) return 0;
                return (float)info.QueueSize / info.MaxQueueSize * 100f;
            }

            return 0;
        }

        /// <summary>
        /// Get total queue size across all connections
        /// </summary>
        private int CalculateTotalQueueSize()
        {
            int total = 0;
            foreach (var info in _connectionQueues.Values)
            {
                total += info.QueueSize;
            }
            return total;
        }

        public long PacketsQueued => _packetsQueued;
        public long PacketsDropped => _packetsDropped;
        public long QueueOverflows => _queueOverflows;
        public int PeakQueueSize => _peakQueueSize;
        public int CurrentQueueSize => _currentQueueSize;
        public int ConnectionCount => _connectionQueues.Count;

        public void Cleanup()
        {
            _connectionQueues.Clear();
        }

        private void LogDebug(string message)
        {
            if (Plugin.ConfigManager?.DebugMode?.Value == true)
            {
                Plugin.Log.LogDebug($"[Queue] {message}");
            }
        }

        /// <summary>
        /// Information about a connection's queue
        /// </summary>
        private class ConnectionQueueInfo
        {
            public long ConnectionId;
            public int QueueSize;
            public int MaxQueueSize;
            public long PacketsQueued;
            public long PacketsDropped;
        }
    }
}
