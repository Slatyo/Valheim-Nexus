using System;
using System.Text;
using Nexus.Network;
using UnityEngine;
using UnityEngine.UI;
using Veneer.Components.Base;
using Veneer.Components.Composite;
using Veneer.Components.Primitives;
using Veneer.Core;
using Veneer.Theme;

namespace Nexus.UI
{
    /// <summary>
    /// In-game debug overlay to display network statistics using Veneer UI framework.
    /// Toggle with configurable key (default F6).
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        private static DebugOverlay _instance;
        private bool _visible;

        // Veneer UI
        private VeneerFrame _frame;
        private VeneerTabBar _tabBar;

        // Stats tab
        private GameObject _statsContent;
        private VeneerText _bandwidthStats;
        private VeneerText _compressionStats;
        private VeneerText _queueStats;
        private VeneerText _qualityStats;
        private VeneerText _zdoStats;

        // Test tab
        private GameObject _testContent;
        private VeneerText _testStatus;
        private VeneerBar _testProgressBar;
        private VeneerButton _runTestButton;
        private VeneerText _testResultsText;

        private float _lastUpdate;
        private const float UpdateInterval = 0.5f;

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
            if (_instance == null) return;
            _instance._visible = !_instance._visible;
            if (_instance._frame != null)
            {
                if (_instance._visible) _instance._frame.Show();
                else _instance._frame.Hide();
            }
        }

        public static bool IsVisible => _instance != null && _instance._visible;

        private void Awake()
        {
            _visible = false;
            if (VeneerAPI.IsReady) CreateUI();
            else VeneerAPI.OnReady += CreateUI;
        }

        private void OnDestroy()
        {
            VeneerAPI.OnReady -= CreateUI;
            if (_frame != null) UnityEngine.Object.Destroy(_frame.gameObject);
        }

        private void CreateUI()
        {
            // Compact window
            _frame = VeneerAPI.CreateWindow("nexus_stats", "Nexus Network", 280, 360);
            _frame.OnCloseClicked += () => { _visible = false; _frame.Hide(); };
            _frame.SetPadding(2, 2, 2, 2);
            _frame.Hide();

            var content = _frame.Content;
            content.gameObject.AddComponent<RectMask2D>();

            // Stats panel (create before tab bar to avoid null reference)
            _statsContent = new GameObject("StatsContent", typeof(RectTransform));
            _statsContent.transform.SetParent(content, false);
            SetupContentRect(_statsContent.GetComponent<RectTransform>());
            CreateStatsPanel(_statsContent.transform);

            // Test panel
            _testContent = new GameObject("TestContent", typeof(RectTransform));
            _testContent.transform.SetParent(content, false);
            SetupContentRect(_testContent.GetComponent<RectTransform>());
            CreateTestPanel(_testContent.transform);
            _testContent.SetActive(false);

            // Tab bar (create after content panels exist)
            _tabBar = VeneerTabBar.Create(content, 22f);
            _tabBar.RectTransform.anchorMin = new Vector2(0, 1);
            _tabBar.RectTransform.anchorMax = new Vector2(1, 1);
            _tabBar.RectTransform.pivot = new Vector2(0.5f, 1);
            _tabBar.RectTransform.anchoredPosition = Vector2.zero;
            _tabBar.RectTransform.sizeDelta = new Vector2(0, 22f);
            _tabBar.AddTabs(("stats", "Stats", 50f), ("test", "Test", 50f));
            _tabBar.OnTabSelected += OnTabSelected;
            _tabBar.SelectFirst();

            Plugin.Log?.LogInfo("[Nexus] Debug overlay created with Veneer");
        }

        private void SetupContentRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(0, 0);
            rect.offsetMax = new Vector2(0, -24f);
        }

        private void OnTabSelected(string tab)
        {
            _statsContent.SetActive(tab == "stats");
            _testContent.SetActive(tab == "test");
        }

        private void CreateStatsPanel(Transform parent)
        {
            // Scrollable area
            var scroll = CreateScrollView(parent);

            // Layout
            var layout = scroll.content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 0f;
            layout.padding = new RectOffset(4, 4, 2, 2);

            var fitter = scroll.content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Stat sections - compact
            _bandwidthStats = CreateStatRow(scroll.content, "Bandwidth", 48f);
            _compressionStats = CreateStatRow(scroll.content, "Compression", 48f);
            _queueStats = CreateStatRow(scroll.content, "Queue", 36f);
            _qualityStats = CreateStatRow(scroll.content, "Connection", 48f);
            _zdoStats = CreateStatRow(scroll.content, "ZDO", 36f);
        }

        private VeneerText CreateStatRow(RectTransform parent, string label, float valueHeight)
        {
            // Header
            var header = VeneerText.Create(parent, label);
            header.ApplyStyle(TextStyle.Header);
            header.TextColor = VeneerColors.TextGold;
            header.FontSize = VeneerConfig.GetScaledFontSize(10);
            var hl = header.gameObject.AddComponent<LayoutElement>();
            hl.preferredHeight = 14f;

            // Value
            var value = VeneerText.Create(parent, "...");
            value.TextColor = VeneerColors.Success;
            value.FontSize = VeneerConfig.GetScaledFontSize(9);
            var vl = value.gameObject.AddComponent<LayoutElement>();
            vl.preferredHeight = valueHeight;

            return value;
        }

        private void CreateTestPanel(Transform parent)
        {
            var layout = parent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlHeight = false;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 4f;
            layout.padding = new RectOffset(4, 4, 4, 4);

            // Status
            _testStatus = VeneerText.Create(parent, "Run test to analyze connection.");
            _testStatus.TextColor = VeneerColors.Text;
            _testStatus.FontSize = VeneerConfig.GetScaledFontSize(10);
            var sl = _testStatus.gameObject.AddComponent<LayoutElement>();
            sl.preferredHeight = 16f;

            // Progress bar
            _testProgressBar = VeneerBar.Create(parent, "nexus_prog", 180, 12);
            var pl = _testProgressBar.gameObject.AddComponent<LayoutElement>();
            pl.preferredHeight = 16f;
            _testProgressBar.SetValues(0, 1);
            _testProgressBar.Hide();

            // Button (use default style - Primary has text color bug)
            _runTestButton = VeneerButton.Create(parent, "Run Test (5s)", OnRunTest);
            _runTestButton.SetSize(120, 24);
            var bl = _runTestButton.gameObject.AddComponent<LayoutElement>();
            bl.preferredHeight = 26f;

            // Results label
            var resultsLabel = VeneerText.Create(parent, "Results");
            resultsLabel.ApplyStyle(TextStyle.Header);
            resultsLabel.TextColor = VeneerColors.TextGold;
            resultsLabel.FontSize = VeneerConfig.GetScaledFontSize(10);
            var rl = resultsLabel.gameObject.AddComponent<LayoutElement>();
            rl.preferredHeight = 14f;

            // Results text
            _testResultsText = VeneerText.Create(parent, "No results");
            _testResultsText.TextColor = VeneerColors.TextMuted;
            _testResultsText.FontSize = VeneerConfig.GetScaledFontSize(9);
            var tl = _testResultsText.gameObject.AddComponent<LayoutElement>();
            tl.minHeight = 120f;
        }

        private void OnRunTest()
        {
            var diag = Plugin.Diagnostics;
            if (diag != null && diag.Status != NetworkDiagnostics.TestStatus.Running)
                diag.StartTest();
        }

        private ScrollRect CreateScrollView(Transform parent)
        {
            var scrollGo = new GameObject("Scroll", typeof(RectTransform));
            scrollGo.transform.SetParent(parent, false);
            var scrollRt = scrollGo.GetComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 12f;

            var vpGo = new GameObject("Viewport", typeof(RectTransform));
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.GetComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<RectMask2D>();
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.GetComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1);
            contentRt.sizeDelta = new Vector2(0, 0);
            scroll.content = contentRt;

            return scroll;
        }

        private void Update()
        {
            if (Input.GetKeyDown(GetToggleKey()))
            {
                _visible = !_visible;
                if (_frame != null)
                {
                    if (_visible) _frame.Show();
                    else _frame.Hide();
                }
            }

            if (_visible && Time.time - _lastUpdate > UpdateInterval)
            {
                _lastUpdate = Time.time;
                UpdateStats();
                UpdateTest();
            }
        }

        private KeyCode GetToggleKey()
        {
            var key = Plugin.ConfigManager?.OverlayToggleKey?.Value ?? "F6";
            return Enum.TryParse<KeyCode>(key, true, out var k) ? k : KeyCode.F6;
        }

        private void UpdateStats()
        {
            if (_bandwidthStats == null) return;
            var sb = new StringBuilder();

            // Bandwidth
            if (Plugin.BandwidthManager != null)
            {
                var bw = Plugin.BandwidthManager;
                sb.AppendLine($"Send: {Fmt(bw.GetCurrentSendRate())}/s");
                sb.AppendLine($"Recv: {Fmt(bw.GetCurrentReceiveRate())}/s");
                sb.Append($"Limit: {Fmt(Plugin.ConfigManager?.SendRateLimit?.Value ?? 0)}/s");
            }
            else sb.Append("N/A");
            _bandwidthStats.Content = sb.ToString();

            // Compression
            sb.Clear();
            if (Plugin.CompressionManager != null)
            {
                var cm = Plugin.CompressionManager;
                sb.AppendLine($"On: {Plugin.ConfigManager?.EnableCompression?.Value ?? false}");
                sb.AppendLine($"Saved: {Fmt(cm.GetBytesSaved())}");
                sb.Append($"Ratio: {cm.GetCompressionRatio():P0}");
            }
            else sb.Append("N/A");
            _compressionStats.Content = sb.ToString();

            // Queue
            sb.Clear();
            if (Plugin.QueueManager != null)
            {
                var qm = Plugin.QueueManager;
                sb.AppendLine($"Conns: {qm.ConnectionCount}");
                sb.Append($"Queued: {Fmt(qm.CurrentQueueSize)}");
            }
            else sb.Append("N/A");
            _queueStats.Content = sb.ToString();

            // Quality
            sb.Clear();
            if (Plugin.NetworkStats != null)
            {
                var ns = Plugin.NetworkStats;
                sb.AppendLine($"Score: {ns.QualityScore}/100");
                sb.AppendLine($"Ping: {ns.PingAverage:F0}ms");
                sb.Append($"Loss: {ns.PacketLossPercent:F1}%");
            }
            else sb.Append("N/A");
            _qualityStats.Content = sb.ToString();

            // ZDO
            sb.Clear();
            var zdo = ZDOMan.instance;
            if (zdo != null)
            {
                sb.AppendLine($"Count: {zdo.m_objectsByID?.Count ?? 0}");
                sb.Append($"Rate: {Plugin.ConfigManager?.DefaultUpdateRate?.Value ?? 100}%");
            }
            else sb.Append("Not in game");
            _zdoStats.Content = sb.ToString();
        }

        private void UpdateTest()
        {
            if (_testStatus == null || !_testContent.activeSelf) return;

            var diag = Plugin.Diagnostics;
            if (diag == null)
            {
                _testStatus.Content = "Unavailable";
                return;
            }

            if (diag.Status == NetworkDiagnostics.TestStatus.Running)
            {
                var p = diag.TestProgress;
                _testStatus.Content = $"Testing... {p * 100:F0}%";
                _testProgressBar.Show();
                _testProgressBar.SetValues(p, 1f);
                _runTestButton.Interactable = false;
            }
            else
            {
                _testProgressBar.Hide();
                _runTestButton.Interactable = true;

                if (diag.LastResults != null)
                    ShowResults(diag.LastResults);
                else if (diag.Status == NetworkDiagnostics.TestStatus.Idle)
                    _testStatus.Content = "Run test to analyze.";
            }
        }

        private void ShowResults(DiagnosticResults r)
        {
            if (!r.Success)
            {
                _testStatus.Content = $"Failed: {r.ErrorMessage}";
                _testStatus.TextColor = VeneerColors.Error;
                return;
            }

            var good = r.OverallResult <= NetworkDiagnostics.TestResult.Good;
            _testStatus.Content = $"{r.OverallResult} ({r.QualityScore}/100)";
            _testStatus.TextColor = good ? VeneerColors.Success : VeneerColors.Error;

            var sb = new StringBuilder();
            sb.AppendLine($"Ping: {r.PingAverage:F0}ms ({r.PingResult})");
            sb.AppendLine($"Jitter: {r.PingJitter:F1}ms");
            sb.AppendLine($"Send: {Fmt(r.SendRateAverage)}/s");
            sb.AppendLine($"Recv: {Fmt(r.RecvRateAverage)}/s");
            sb.AppendLine($"ZDOs: {r.ZdoCount} ({r.ZdoResult})");
            sb.Append($"Compress: {r.CompressionRatio:P0}");

            _testResultsText.Content = sb.ToString();
            _testResultsText.TextColor = VeneerColors.Text;
        }

        private string Fmt(long bytes)
        {
            if (bytes >= 1048576) return $"{bytes / 1048576.0:F1}MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F1}KB";
            return $"{bytes}B";
        }
    }
}
