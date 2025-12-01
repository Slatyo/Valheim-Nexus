using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Nexus.Network
{
    /// <summary>
    /// Network diagnostics and performance testing
    /// </summary>
    public class NetworkDiagnostics
    {
        public enum TestStatus
        {
            Idle,
            Running,
            Completed,
            Failed
        }

        public enum TestResult
        {
            Unknown,
            Excellent,
            Good,
            Fair,
            Poor
        }

        // Test state
        private TestStatus _status = TestStatus.Idle;
        private float _testStartTime;
        private float _testDuration = 5f; // 5 second test
        private int _testPhase;

        // Test measurements
        private List<float> _pingMeasurements = new List<float>();
        private List<long> _sendRateMeasurements = new List<long>();
        private List<long> _recvRateMeasurements = new List<long>();
        private long _bytesAtStart;
        private long _bytesAtEnd;
        private int _zdoCountAtStart;
        private int _zdoCountAtEnd;

        // Results
        private DiagnosticResults _lastResults;

        public TestStatus Status => _status;
        public DiagnosticResults LastResults => _lastResults;
        public float TestProgress => _status == TestStatus.Running ?
            Mathf.Clamp01((Time.time - _testStartTime) / _testDuration) : 0f;

        /// <summary>
        /// Start a diagnostic test
        /// </summary>
        public bool StartTest()
        {
            if (_status == TestStatus.Running)
            {
                Plugin.Log.LogWarning("[Diagnostics] Test already running");
                return false;
            }

            if (ZNet.instance == null)
            {
                Plugin.Log.LogWarning("[Diagnostics] Not connected to a server");
                _lastResults = new DiagnosticResults
                {
                    Success = false,
                    ErrorMessage = "Not connected to a server. Join a world first."
                };
                _status = TestStatus.Failed;
                return false;
            }

            Plugin.Log.LogInfo("[Diagnostics] Starting network performance test...");

            _status = TestStatus.Running;
            _testStartTime = Time.time;
            _testPhase = 0;

            // Clear previous measurements
            _pingMeasurements.Clear();
            _sendRateMeasurements.Clear();
            _recvRateMeasurements.Clear();

            // Record starting values
            _bytesAtStart = (Plugin.BandwidthManager?.TotalBytesSent ?? 0) +
                           (Plugin.BandwidthManager?.TotalBytesReceived ?? 0);
            _zdoCountAtStart = ZDOMan.instance?.m_objectsByID?.Count ?? 0;

            return true;
        }

        /// <summary>
        /// Update the test (call from Plugin.Update)
        /// </summary>
        public void Update()
        {
            if (_status != TestStatus.Running)
                return;

            float elapsed = Time.time - _testStartTime;

            // Collect measurements every 0.25 seconds
            if (_testPhase < (int)(elapsed / 0.25f))
            {
                _testPhase = (int)(elapsed / 0.25f);
                CollectMeasurement();
            }

            // Check if test is complete
            if (elapsed >= _testDuration)
            {
                CompleteTest();
            }
        }

        private void CollectMeasurement()
        {
            // Collect ping
            if (Plugin.NetworkStats != null)
            {
                _pingMeasurements.Add(Plugin.NetworkStats.PingAverage);
            }

            // Collect bandwidth
            if (Plugin.BandwidthManager != null)
            {
                _sendRateMeasurements.Add(Plugin.BandwidthManager.GetCurrentSendRate());
                _recvRateMeasurements.Add(Plugin.BandwidthManager.GetCurrentReceiveRate());
            }
        }

        private void CompleteTest()
        {
            _status = TestStatus.Completed;

            // Record ending values
            _bytesAtEnd = (Plugin.BandwidthManager?.TotalBytesSent ?? 0) +
                         (Plugin.BandwidthManager?.TotalBytesReceived ?? 0);
            _zdoCountAtEnd = ZDOMan.instance?.m_objectsByID?.Count ?? 0;

            // Calculate results
            _lastResults = CalculateResults();

            Plugin.Log.LogInfo($"[Diagnostics] Test completed - Overall: {_lastResults.OverallResult}");
        }

        private DiagnosticResults CalculateResults()
        {
            var results = new DiagnosticResults
            {
                Success = true,
                TestDuration = _testDuration,
                Timestamp = DateTime.Now
            };

            // Ping analysis
            if (_pingMeasurements.Count > 0)
            {
                float sum = 0, min = float.MaxValue, max = float.MinValue;
                foreach (var p in _pingMeasurements)
                {
                    sum += p;
                    if (p < min) min = p;
                    if (p > max) max = p;
                }
                results.PingAverage = sum / _pingMeasurements.Count;
                results.PingMin = min;
                results.PingMax = max;
                results.PingJitter = max - min;

                // Rate ping
                if (results.PingAverage < 50) results.PingResult = TestResult.Excellent;
                else if (results.PingAverage < 100) results.PingResult = TestResult.Good;
                else if (results.PingAverage < 200) results.PingResult = TestResult.Fair;
                else results.PingResult = TestResult.Poor;
            }

            // Bandwidth analysis
            if (_sendRateMeasurements.Count > 0)
            {
                long sum = 0, max = 0;
                foreach (var r in _sendRateMeasurements)
                {
                    sum += r;
                    if (r > max) max = r;
                }
                results.SendRateAverage = sum / _sendRateMeasurements.Count;
                results.SendRatePeak = max;
            }

            if (_recvRateMeasurements.Count > 0)
            {
                long sum = 0, max = 0;
                foreach (var r in _recvRateMeasurements)
                {
                    sum += r;
                    if (r > max) max = r;
                }
                results.RecvRateAverage = sum / _recvRateMeasurements.Count;
                results.RecvRatePeak = max;
            }

            // Total throughput
            results.TotalBytesTransferred = _bytesAtEnd - _bytesAtStart;
            results.ThroughputBytesPerSecond = (long)(results.TotalBytesTransferred / _testDuration);

            // Rate bandwidth
            int sendLimit = Plugin.ConfigManager?.SendRateLimit?.Value ?? 512000;
            float utilizationPercent = sendLimit > 0 ? (float)results.SendRateAverage / sendLimit * 100f : 0;

            if (utilizationPercent < 50) results.BandwidthResult = TestResult.Excellent;
            else if (utilizationPercent < 75) results.BandwidthResult = TestResult.Good;
            else if (utilizationPercent < 90) results.BandwidthResult = TestResult.Fair;
            else results.BandwidthResult = TestResult.Poor;

            // ZDO analysis
            results.ZdoCount = _zdoCountAtEnd;
            results.ZdoChange = _zdoCountAtEnd - _zdoCountAtStart;

            if (results.ZdoCount < 5000) results.ZdoResult = TestResult.Excellent;
            else if (results.ZdoCount < 10000) results.ZdoResult = TestResult.Good;
            else if (results.ZdoCount < 20000) results.ZdoResult = TestResult.Fair;
            else results.ZdoResult = TestResult.Poor;

            // Compression stats
            if (Plugin.CompressionManager != null)
            {
                results.CompressionRatio = Plugin.CompressionManager.GetCompressionRatio();
                results.CompressionEnabled = Plugin.CompressionManager.IsCompressionEnabled();
            }

            // Quality score from NetworkStats
            results.QualityScore = Plugin.NetworkStats?.QualityScore ?? 0;

            // Calculate overall result
            int score = 0;
            int count = 0;

            if (results.PingResult != TestResult.Unknown) { score += (int)results.PingResult; count++; }
            if (results.BandwidthResult != TestResult.Unknown) { score += (int)results.BandwidthResult; count++; }
            if (results.ZdoResult != TestResult.Unknown) { score += (int)results.ZdoResult; count++; }

            if (count > 0)
            {
                int avg = score / count;
                results.OverallResult = (TestResult)avg;
            }

            return results;
        }

        /// <summary>
        /// Get a formatted report of the last test
        /// </summary>
        public string GetReport()
        {
            if (_lastResults == null)
                return "No test results available. Run a test first.";

            if (!_lastResults.Success)
                return $"Test failed: {_lastResults.ErrorMessage}";

            var sb = new StringBuilder();
            sb.AppendLine("=== NEXUS NETWORK DIAGNOSTICS ===");
            sb.AppendLine($"Test Duration: {_lastResults.TestDuration:F1}s");
            sb.AppendLine($"Timestamp: {_lastResults.Timestamp:HH:mm:ss}");
            sb.AppendLine();

            sb.AppendLine($"OVERALL: {GetResultEmoji(_lastResults.OverallResult)} {_lastResults.OverallResult}");
            sb.AppendLine($"Quality Score: {_lastResults.QualityScore}/100");
            sb.AppendLine();

            sb.AppendLine("--- LATENCY ---");
            sb.AppendLine($"{GetResultEmoji(_lastResults.PingResult)} Ping: {_lastResults.PingAverage:F1}ms (min: {_lastResults.PingMin:F1}, max: {_lastResults.PingMax:F1})");
            sb.AppendLine($"   Jitter: {_lastResults.PingJitter:F1}ms");
            sb.AppendLine();

            sb.AppendLine("--- BANDWIDTH ---");
            sb.AppendLine($"{GetResultEmoji(_lastResults.BandwidthResult)} Send: {FormatBytes(_lastResults.SendRateAverage)}/s (peak: {FormatBytes(_lastResults.SendRatePeak)}/s)");
            sb.AppendLine($"   Recv: {FormatBytes(_lastResults.RecvRateAverage)}/s (peak: {FormatBytes(_lastResults.RecvRatePeak)}/s)");
            sb.AppendLine($"   Throughput: {FormatBytes(_lastResults.ThroughputBytesPerSecond)}/s");
            sb.AppendLine();

            sb.AppendLine("--- WORLD STATE ---");
            sb.AppendLine($"{GetResultEmoji(_lastResults.ZdoResult)} ZDO Count: {_lastResults.ZdoCount} ({(_lastResults.ZdoChange >= 0 ? "+" : "")}{_lastResults.ZdoChange} during test)");
            sb.AppendLine();

            sb.AppendLine("--- COMPRESSION ---");
            sb.AppendLine($"   Enabled: {(_lastResults.CompressionEnabled ? "Yes" : "No")}");
            sb.AppendLine($"   Ratio: {_lastResults.CompressionRatio:P1} saved");
            sb.AppendLine();

            sb.AppendLine("--- RECOMMENDATIONS ---");
            foreach (var rec in GetRecommendations())
            {
                sb.AppendLine($"   * {rec}");
            }

            return sb.ToString();
        }

        private List<string> GetRecommendations()
        {
            var recs = new List<string>();

            if (_lastResults == null)
                return recs;

            if (_lastResults.PingResult == TestResult.Poor)
                recs.Add("High latency detected. Consider a server closer to your location.");

            if (_lastResults.PingJitter > 50)
                recs.Add("High jitter detected. Connection may be unstable.");

            if (_lastResults.BandwidthResult == TestResult.Poor)
                recs.Add("Bandwidth near limit. Consider increasing SendRateLimit or enabling compression.");

            if (_lastResults.ZdoResult == TestResult.Poor)
                recs.Add("High ZDO count. Large bases/many items can cause lag.");

            if (!_lastResults.CompressionEnabled)
                recs.Add("Compression is disabled. Enable it to reduce bandwidth usage.");

            if (_lastResults.CompressionRatio < 0.1f && _lastResults.CompressionEnabled)
                recs.Add("Low compression ratio. Data may already be compressed or too small.");

            if (_lastResults.QualityScore < 50)
                recs.Add("Low quality score. Check your internet connection.");

            if (recs.Count == 0)
                recs.Add("Network performance looks good!");

            return recs;
        }

        private string GetResultEmoji(TestResult result)
        {
            switch (result)
            {
                case TestResult.Excellent: return "[OK]";
                case TestResult.Good: return "[OK]";
                case TestResult.Fair: return "[!!]";
                case TestResult.Poor: return "[XX]";
                default: return "[??]";
            }
        }

        private string FormatBytes(long bytes)
        {
            if (bytes >= 1048576)
                return $"{bytes / 1048576.0:F2} MB";
            if (bytes >= 1024)
                return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
    }

    /// <summary>
    /// Results from a diagnostic test
    /// </summary>
    public class DiagnosticResults
    {
        public bool Success;
        public string ErrorMessage;
        public DateTime Timestamp;
        public float TestDuration;

        // Ping
        public float PingAverage;
        public float PingMin;
        public float PingMax;
        public float PingJitter;
        public NetworkDiagnostics.TestResult PingResult;

        // Bandwidth
        public long SendRateAverage;
        public long SendRatePeak;
        public long RecvRateAverage;
        public long RecvRatePeak;
        public long TotalBytesTransferred;
        public long ThroughputBytesPerSecond;
        public NetworkDiagnostics.TestResult BandwidthResult;

        // ZDO
        public int ZdoCount;
        public int ZdoChange;
        public NetworkDiagnostics.TestResult ZdoResult;

        // Compression
        public bool CompressionEnabled;
        public float CompressionRatio;

        // Overall
        public int QualityScore;
        public NetworkDiagnostics.TestResult OverallResult;
    }
}
