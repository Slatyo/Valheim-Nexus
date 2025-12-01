using System;
using System.Text;
using Nexus.Network;
using UnityEngine;

namespace Nexus.UI
{
    /// <summary>
    /// In-game debug overlay to display network statistics
    /// Toggle with configurable key (default F2)
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private static DebugOverlay _instance;
        private bool _visible;
        private Rect _windowRect;
        private GUIStyle _windowStyle;
        private GUIStyle _labelStyle;
        private GUIStyle _headerStyle;
        private GUIStyle _valueStyle;
        private GUIStyle _buttonStyle;
        private GUIStyle _resultGoodStyle;
        private GUIStyle _resultBadStyle;
        private bool _stylesInitialized;

        // Update interval for stats
        private float _lastUpdate;
        private const float UpdateInterval = 0.5f;

        // Cached stats strings
        private string _bandwidthStats = "";
        private string _compressionStats = "";
        private string _queueStats = "";
        private string _qualityStats = "";
        private string _zdoStats = "";
        private string _testStatus = "";
        private bool _showTestResults = false;
        private Vector2 _scrollPosition;

        public static void Create()
        {
            if (_instance != null) return;

            var go = new GameObject("NexusDebugOverlay");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<DebugOverlay>();
        }

        public static void Destroy()
        {
            if (_instance != null)
            {
                UnityEngine.Object.Destroy(_instance.gameObject);
                _instance = null;
            }
        }

        public static void Toggle()
        {
            if (_instance != null)
            {
                _instance._visible = !_instance._visible;
            }
        }

        public static bool IsVisible => _instance != null && _instance._visible;

        private void Awake()
        {
            _windowRect = new Rect(10, 10, 350, 480);
            _visible = false;
        }

        private void Update()
        {
            // Check for toggle key
            KeyCode toggleKey = GetToggleKey();
            if (Input.GetKeyDown(toggleKey))
            {
                _visible = !_visible;
                Plugin.Log?.LogInfo($"[Nexus] Debug overlay {(_visible ? "shown" : "hidden")}");
            }

            // Update stats periodically when visible
            if (_visible && Time.time - _lastUpdate > UpdateInterval)
            {
                _lastUpdate = Time.time;
                UpdateStats();
            }
        }

        private KeyCode GetToggleKey()
        {
            // Get from config or default to F6
            string keyName = Plugin.ConfigManager?.OverlayToggleKey?.Value ?? "F6";
            if (Enum.TryParse<KeyCode>(keyName, true, out var key))
            {
                return key;
            }
            return KeyCode.F6;
        }

        private void UpdateStats()
        {
            var sb = new StringBuilder();

            // Bandwidth stats
            sb.Clear();
            if (Plugin.BandwidthManager != null)
            {
                var bw = Plugin.BandwidthManager;
                sb.AppendLine($"Send Rate: {FormatBytes(bw.GetCurrentSendRate())}/s");
                sb.AppendLine($"Recv Rate: {FormatBytes(bw.GetCurrentReceiveRate())}/s");
                sb.AppendLine($"Total Sent: {FormatBytes(bw.TotalBytesSent)}");
                sb.AppendLine($"Total Recv: {FormatBytes(bw.TotalBytesReceived)}");
                sb.AppendLine($"Send Limit: {FormatBytes(Plugin.ConfigManager?.SendRateLimit?.Value ?? 0)}/s");
            }
            else
            {
                sb.AppendLine("Not available");
            }
            _bandwidthStats = sb.ToString();

            // Compression stats
            sb.Clear();
            if (Plugin.CompressionManager != null)
            {
                var cm = Plugin.CompressionManager;
                sb.AppendLine($"Enabled: {(Plugin.ConfigManager?.EnableCompression?.Value ?? false)}");
                sb.AppendLine($"Threshold: {Plugin.ConfigManager?.CompressionThreshold?.Value ?? 0} bytes");
                sb.AppendLine($"Packets Compressed: {cm.PacketsCompressed}");
                sb.AppendLine($"Bytes Saved: {FormatBytes(cm.GetBytesSaved())}");
                sb.AppendLine($"Avg Ratio: {cm.GetCompressionRatio():P1}");
            }
            else
            {
                sb.AppendLine("Not available");
            }
            _compressionStats = sb.ToString();

            // Queue stats
            sb.Clear();
            if (Plugin.QueueManager != null)
            {
                var qm = Plugin.QueueManager;
                sb.AppendLine($"Active Connections: {qm.ConnectionCount}");
                sb.AppendLine($"Total Queued: {FormatBytes(qm.CurrentQueueSize)}");
                sb.AppendLine($"Max Queue Size: {FormatBytes(Plugin.ConfigManager?.MaxQueueSize?.Value ?? 0)}");
            }
            else
            {
                sb.AppendLine("Not available");
            }
            _queueStats = sb.ToString();

            // Quality stats
            sb.Clear();
            if (Plugin.NetworkStats != null)
            {
                var ns = Plugin.NetworkStats;
                sb.AppendLine($"Quality Score: {ns.QualityScore}/100");
                sb.AppendLine($"Avg Ping: {ns.PingAverage:F1} ms");
                sb.AppendLine($"Packet Loss: {ns.PacketLossPercent:F1}%");
            }
            else
            {
                sb.AppendLine("Not available");
            }
            _qualityStats = sb.ToString();

            // ZDO stats
            sb.Clear();
            var zdoman = ZDOMan.instance;
            if (zdoman != null)
            {
                sb.AppendLine($"ZDO Count: {zdoman.m_objectsByID?.Count ?? 0}");
                sb.AppendLine($"Update Rate: {Plugin.ConfigManager?.DefaultUpdateRate?.Value ?? 100}%");
                sb.AppendLine($"Auto Adjust: {(Plugin.ConfigManager?.AutoAdjustUpdateRate?.Value ?? false)}");
            }
            else
            {
                sb.AppendLine("Not in game");
            }
            _zdoStats = sb.ToString();
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _windowStyle = new GUIStyle(GUI.skin.window)
            {
                padding = new RectOffset(10, 10, 20, 10)
            };

            _headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontStyle = FontStyle.Bold,
                fontSize = 13,
                normal = { textColor = new Color(1f, 0.85f, 0.4f) }
            };

            _labelStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = Color.white }
            };

            _valueStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                normal = { textColor = new Color(0.7f, 1f, 0.7f) }
            };

            _buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };

            _resultGoodStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.4f, 1f, 0.4f) }
            };

            _resultBadStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(1f, 0.4f, 0.4f) }
            };

            _stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!_visible) return;

            InitStyles();

            _windowRect = GUI.Window(
                94857, // Unique window ID
                _windowRect,
                DrawWindow,
                "Nexus Network Stats",
                _windowStyle
            );
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Tab buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(_showTestResults ? "Stats" : "[Stats]", _buttonStyle, GUILayout.Width(80)))
            {
                _showTestResults = false;
            }
            if (GUILayout.Button(_showTestResults ? "[Test]" : "Test", _buttonStyle, GUILayout.Width(80)))
            {
                _showTestResults = true;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            if (_showTestResults)
            {
                DrawTestTab();
            }
            else
            {
                DrawStatsTab();
            }

            GUILayout.FlexibleSpace();

            // Footer
            GUILayout.Label($"Press {Plugin.ConfigManager?.OverlayToggleKey?.Value ?? "F6"} to close", _labelStyle);

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 20));
        }

        private void DrawStatsTab()
        {
            // Bandwidth Section
            GUILayout.Label("Bandwidth", _headerStyle);
            GUILayout.Label(_bandwidthStats, _valueStyle);
            GUILayout.Space(5);

            // Compression Section
            GUILayout.Label("Compression", _headerStyle);
            GUILayout.Label(_compressionStats, _valueStyle);
            GUILayout.Space(5);

            // Queue Section
            GUILayout.Label("Queue", _headerStyle);
            GUILayout.Label(_queueStats, _valueStyle);
            GUILayout.Space(5);

            // Quality Section
            GUILayout.Label("Connection Quality", _headerStyle);
            GUILayout.Label(_qualityStats, _valueStyle);
            GUILayout.Space(5);

            // ZDO Section
            GUILayout.Label("ZDO Sync", _headerStyle);
            GUILayout.Label(_zdoStats, _valueStyle);
        }

        private void DrawTestTab()
        {
            var diag = Plugin.Diagnostics;
            if (diag == null)
            {
                GUILayout.Label("Diagnostics not available", _labelStyle);
                return;
            }

            // Test controls
            GUILayout.Label("Performance Test", _headerStyle);
            GUILayout.Space(5);

            if (diag.Status == NetworkDiagnostics.TestStatus.Running)
            {
                // Show progress bar
                float progress = diag.TestProgress;
                GUILayout.Label($"Testing... {progress * 100:F0}%", _labelStyle);

                // Simple progress bar
                GUILayout.BeginHorizontal();
                GUILayout.Box("", GUILayout.Width(progress * 300), GUILayout.Height(20));
                GUILayout.EndHorizontal();
            }
            else
            {
                // Run test button
                if (GUILayout.Button("Run Network Test (5s)", _buttonStyle, GUILayout.Height(30)))
                {
                    diag.StartTest();
                }
            }

            GUILayout.Space(10);

            // Show results if available
            if (diag.LastResults != null && diag.Status != NetworkDiagnostics.TestStatus.Running)
            {
                var results = diag.LastResults;

                if (!results.Success)
                {
                    GUILayout.Label($"Test failed: {results.ErrorMessage}", _resultBadStyle);
                    return;
                }

                // Overall result
                GUILayout.Label("Results", _headerStyle);
                var overallStyle = results.OverallResult <= NetworkDiagnostics.TestResult.Good ? _resultGoodStyle : _resultBadStyle;
                GUILayout.Label($"Overall: {results.OverallResult} (Score: {results.QualityScore}/100)", overallStyle);
                GUILayout.Space(5);

                // Scrollable results area
                _scrollPosition = GUILayout.BeginScrollView(_scrollPosition, GUILayout.Height(200));

                // Latency
                GUILayout.Label("Latency", _headerStyle);
                var pingStyle = results.PingResult <= NetworkDiagnostics.TestResult.Good ? _resultGoodStyle : _resultBadStyle;
                GUILayout.Label($"  Ping: {results.PingAverage:F1}ms ({results.PingResult})", pingStyle);
                GUILayout.Label($"  Jitter: {results.PingJitter:F1}ms", _valueStyle);
                GUILayout.Space(3);

                // Bandwidth
                GUILayout.Label("Bandwidth", _headerStyle);
                var bwStyle = results.BandwidthResult <= NetworkDiagnostics.TestResult.Good ? _resultGoodStyle : _resultBadStyle;
                GUILayout.Label($"  Send: {FormatBytes(results.SendRateAverage)}/s ({results.BandwidthResult})", bwStyle);
                GUILayout.Label($"  Recv: {FormatBytes(results.RecvRateAverage)}/s", _valueStyle);
                GUILayout.Label($"  Throughput: {FormatBytes(results.ThroughputBytesPerSecond)}/s", _valueStyle);
                GUILayout.Space(3);

                // World
                GUILayout.Label("World State", _headerStyle);
                var zdoStyle = results.ZdoResult <= NetworkDiagnostics.TestResult.Good ? _resultGoodStyle : _resultBadStyle;
                GUILayout.Label($"  ZDO Count: {results.ZdoCount} ({results.ZdoResult})", zdoStyle);
                GUILayout.Space(3);

                // Compression
                GUILayout.Label("Compression", _headerStyle);
                GUILayout.Label($"  Enabled: {(results.CompressionEnabled ? "Yes" : "No")}", _valueStyle);
                GUILayout.Label($"  Savings: {results.CompressionRatio:P1}", _valueStyle);

                GUILayout.EndScrollView();
            }
            else if (diag.Status == NetworkDiagnostics.TestStatus.Idle)
            {
                GUILayout.Label("Click 'Run Network Test' to analyze", _labelStyle);
                GUILayout.Label("your connection performance.", _labelStyle);
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
}
