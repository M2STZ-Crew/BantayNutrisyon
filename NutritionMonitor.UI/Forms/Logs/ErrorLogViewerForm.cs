using NutritionMonitor.UI.Utilities;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.Logs;

public class ErrorLogViewerForm : UserControl
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly Color BgColor = Color.FromArgb(243, 246, 250);
    private static readonly Color CardBg = Color.White;
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 150);
    private static readonly Color TealHover = Color.FromArgb(0, 148, 132);
    private static readonly Color TealLight = Color.FromArgb(225, 248, 245);
    private static readonly Color TextDark = Color.FromArgb(22, 32, 50);
    private static readonly Color TextMid = Color.FromArgb(80, 100, 130);
    private static readonly Color TextMuted = Color.FromArgb(140, 160, 185);
    private static readonly Color BorderLight = Color.FromArgb(225, 232, 242);
    private static readonly Color DangerRed = Color.FromArgb(220, 60, 60);
    private static readonly Color AmberColor = Color.FromArgb(245, 158, 11);
    private static readonly Color LogBg = Color.FromArgb(18, 26, 42);
    private static readonly Color LogText = Color.FromArgb(200, 220, 210);
    private static readonly Color LogMuted = Color.FromArgb(70, 100, 85);
    private static readonly Color LogInfo = Color.FromArgb(100, 180, 240);
    private static readonly Color LogWarn = Color.FromArgb(245, 180, 60);
    private static readonly Color LogError = Color.FromArgb(240, 80, 80);
    private static readonly Color LogFatal = Color.FromArgb(255, 40, 40);
    private static readonly Color LogDebug = Color.FromArgb(130, 150, 180);

    // ── Controls ──────────────────────────────────────────────────────────────
    private Panel _headerPanel = null!;
    private Panel _toolbarPanel = null!;
    private Panel _logPanel = null!;
    private Panel _statsPanel = null!;
    private Panel _statusBar = null!;

    private ComboBox _cmbLogFile = null!;
    private ComboBox _cmbFilter = null!;
    private TextBox _txtSearch = null!;
    private Button _btnRefresh = null!;
    private Button _btnClear = null!;
    private Button _btnOpenDir = null!;
    private Button _btnCopy = null!;
    private RichTextBox _rtbLog = null!;

    // ── Stat labels ───────────────────────────────────────────────────────────
    private Label _lblInfoCount = null!;
    private Label _lblWarnCount = null!;
    private Label _lblErrorCount = null!;
    private Label _lblFatalCount = null!;
    private Label _lblTotalCount = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Label _lblStatus = null!;
    private Label _lblFileInfo = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<LogLine> _allLines = new();
    private readonly Panel _parentContentArea;

    private record LogLine(
        string Raw,
        string Level,
        string Timestamp,
        string Message);

    // ─────────────────────────────────────────────────────────────────────────
    public ErrorLogViewerForm(Panel parentContentArea)
    {
        _parentContentArea = parentContentArea;
        BuildControl();
        PopulateLogFileCombo();
        _ = LoadCurrentLogAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Control Construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildControl()
    {
        BackColor = BgColor;

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 5,
            ColumnCount = 1,
            BackColor = BgColor,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));  // header
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56f));  // toolbar
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 48f));  // stats strip
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // log viewer
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // status bar

        BuildHeader();
        BuildToolbar();
        BuildStatsStrip();
        BuildLogViewer();
        BuildStatusBar();

        layout.Controls.Add(_headerPanel, 0, 0);
        layout.Controls.Add(_toolbarPanel, 0, 1);
        layout.Controls.Add(_statsPanel, 0, 2);
        layout.Controls.Add(_logPanel, 0, 3);
        layout.Controls.Add(_statusBar, 0, 4);

        Controls.Add(layout);
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void BuildHeader()
    {
        _headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg
        };

        _headerPanel.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var bar = new SolidBrush(TealAccent);
            g.FillRectangle(bar, 0, 0, 5, _headerPanel.Height);
            var rect = new Rectangle(
                _headerPanel.Width - 200, 0, 200, _headerPanel.Height);
            using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.Transparent, TealLight,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(grad, rect);
            using var pen = new Pen(BorderLight, 1);
            g.DrawLine(pen, 0, _headerPanel.Height - 1,
                _headerPanel.Width, _headerPanel.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "Application Log Viewer",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(500, 36),
            Location = new Point(20, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Real-time view of Serilog rolling log files.  " +
                        "Logs are stored in AppData\\NutritionMonitor\\Logs\\",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(700, 20),
            Location = new Point(20, 50),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSub });
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void BuildToolbar()
    {
        _toolbarPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(0)
        };

        _toolbarPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _toolbarPanel.Height - 1,
                _toolbarPanel.Width, _toolbarPanel.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(14, 8, 14, 8)
        };

        // Log file combo
        flow.Controls.Add(MakeFlowLabel("LOG FILE"));
        _cmbLogFile = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(220, 30),
            BackColor = CardBg,
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 2, 16, 0)
        };
        _cmbLogFile.SelectedIndexChanged += async (_, _) =>
            await LoadSelectedLogAsync();
        flow.Controls.Add(_cmbLogFile);

        // Level filter
        flow.Controls.Add(MakeFlowLabel("LEVEL"));
        _cmbFilter = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(130, 30),
            BackColor = CardBg,
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(4, 2, 16, 0)
        };
        _cmbFilter.Items.AddRange(new object[]
        {
            "All Levels", "DEBUG", "INFO", "WARNING", "ERROR", "FATAL"
        });
        _cmbFilter.SelectedIndex = 0;
        _cmbFilter.SelectedIndexChanged += (_, _) => ApplyFilter();
        flow.Controls.Add(_cmbFilter);

        // Search
        flow.Controls.Add(MakeFlowLabel("SEARCH"));
        _txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 9.5f),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(246, 248, 252),
            ForeColor = TextDark,
            PlaceholderText = "Filter log text…",
            Size = new Size(180, 30),
            Margin = new Padding(4, 2, 16, 0)
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();
        flow.Controls.Add(_txtSearch);

        // Buttons
        _btnRefresh = MakeToolButton("↺  Refresh", TealAccent, Color.White, 90);
        _btnCopy = MakeToolButton("⎘  Copy All", Color.FromArgb(240, 244, 248), TextMid, 90);
        _btnOpenDir = MakeToolButton("📁  Open Folder", Color.FromArgb(240, 244, 248), TextMid, 110);
        _btnClear = MakeToolButton("🗑  Clear View", Color.FromArgb(240, 244, 248), DangerRed, 100);

        foreach (var b in new[] { _btnCopy, _btnOpenDir, _btnClear })
        {
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = BorderLight;
            b.Margin = new Padding(0, 2, 6, 0);
        }
        _btnRefresh.Margin = new Padding(0, 2, 6, 0);

        flow.Controls.AddRange(new Control[]
        {
            _btnRefresh, _btnCopy, _btnOpenDir, _btnClear
        });

        _toolbarPanel.Controls.Add(flow);

        _btnRefresh.Click += async (_, _) => await LoadSelectedLogAsync();
        _btnCopy.Click += CopyAllToClipboard;
        _btnOpenDir.Click += OpenLogDirectory;
        _btnClear.Click += (_, _) =>
        {
            _rtbLog.Clear();
            _allLines.Clear();
            UpdateStats(new List<LogLine>());
        };
    }

    // ── Stats Strip ───────────────────────────────────────────────────────────

    private void BuildStatsStrip()
    {
        _statsPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 32, 50),
            Padding = new Padding(16, 0, 16, 0)
        };

        _statsPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 60, 90), 1);
            e.Graphics.DrawLine(pen, 0, _statsPanel.Height - 1,
                _statsPanel.Width, _statsPanel.Height - 1);
        };

        _lblTotalCount = MakeStatLabel("—", TextMuted);
        _lblInfoCount = MakeStatLabel("—", LogInfo);
        _lblWarnCount = MakeStatLabel("—", LogWarn);
        _lblErrorCount = MakeStatLabel("—", LogError);
        _lblFatalCount = MakeStatLabel("—", LogFatal);

        var headers = new[]
        {
            ("Total Lines",  _lblTotalCount, TextMuted),
            ("INF",          _lblInfoCount,  LogInfo),
            ("WRN",          _lblWarnCount,  LogWarn),
            ("ERR",          _lblErrorCount, LogError),
            ("FTL",          _lblFatalCount, LogFatal),
        };

        int x = 0;
        foreach (var (title, valLbl, color) in headers)
        {
            var titleLbl = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 110, 100),
                AutoSize = false,
                Size = new Size(100, 18),
                Location = new Point(x, 6),
                TextAlign = ContentAlignment.BottomLeft
            };
            valLbl.Location = new Point(x, 26);
            valLbl.ForeColor = color;

            _statsPanel.Controls.Add(titleLbl);
            _statsPanel.Controls.Add(valLbl);
            x += 140;
        }
    }

    // ── Log Viewer ────────────────────────────────────────────────────────────

    private void BuildLogViewer()
    {
        _logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 10, 20, 0)
        };

        var logCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = LogBg
        };

        logCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 70, 55), 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                logCard.Width - 1, logCard.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, logCard.Width, 3);
        };

        var logHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(15, 24, 40)
        };

        logHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 70, 55), 1);
            e.Graphics.DrawLine(pen, 0, logHeader.Height - 1,
                logHeader.Width, logHeader.Height - 1);
        };

        var lblLogHeader = new Label
        {
            Text = "●  LOG OUTPUT",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = TealAccent,
            AutoSize = true,
            Location = new Point(14, 0),
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };
        logHeader.Controls.Add(lblLogHeader);

        var lblScrollTip = new Label
        {
            Text = "Newest entries at bottom",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = LogMuted,
            AutoSize = true,
            Height = 32,
            TextAlign = ContentAlignment.MiddleLeft
        };
        logHeader.Controls.Add(lblScrollTip);
        logHeader.Resize += (_, _) =>
            lblScrollTip.Location = new Point(
                logHeader.Width - lblScrollTip.Width - 14,
                (logHeader.Height - lblScrollTip.Height) / 2);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = LogBg,
            ForeColor = LogText,
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Both,
            Padding = new Padding(12, 6, 12, 6),
            WordWrap = false
        };

        logCard.Controls.Add(_rtbLog);
        logCard.Controls.Add(logHeader);
        _logPanel.Controls.Add(logCard);
    }

    // ── Status Bar ────────────────────────────────────────────────────────────

    private void BuildStatusBar()
    {
        _statusBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 248, 252),
            Padding = new Padding(16, 0, 16, 0)
        };

        _statusBar.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, _statusBar.Width, 0);
        };

        _lblFileInfo = new Label
        {
            Text = "No log file loaded.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(0, 0),
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TealAccent,
            AutoSize = true,
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _statusBar.Controls.AddRange(new Control[] { _lblFileInfo, _lblStatus });
        _statusBar.Resize += (_, _) =>
            _lblStatus.Location = new Point(
                _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Log File Management
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateLogFileCombo()
    {
        _cmbLogFile.Items.Clear();

        var files = AppLogger.GetLogFiles().ToList();

        if (files.Count == 0)
        {
            _cmbLogFile.Items.Add("No log files found");
            _cmbLogFile.SelectedIndex = 0;
            _cmbLogFile.Enabled = false;
            return;
        }

        foreach (var f in files)
        {
            string label = f.Name == $"app-{DateTime.Today:yyyyMMdd}.log"
                ? $"Today — {f.Name}"
                : f.Name;
            _cmbLogFile.Items.Add(new LogFileItem(label, f.FullName));
        }

        _cmbLogFile.DisplayMember = "Label";
        _cmbLogFile.SelectedIndex = 0;
    }

    private async Task LoadCurrentLogAsync()
    {
        if (_cmbLogFile.Items.Count == 0) return;
        await LoadSelectedLogAsync();
    }

    private async Task LoadSelectedLogAsync()
    {
        if (_cmbLogFile.SelectedItem is not LogFileItem item) return;

        SetStatus("Loading log file…", TextMuted);

        try
        {
            // Read with FileShare.ReadWrite so Serilog's open handle doesn't block us
            string content;
            using (var stream = new FileStream(
                item.Path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite))
            using (var reader = new StreamReader(stream))
            {
                content = await reader.ReadToEndAsync();
            }

            var lines = content
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(ParseLogLine)
                .ToList();

            _allLines = lines;
            ApplyFilter();

            var fi = new FileInfo(item.Path);
            _lblFileInfo.Text =
                $"File: {fi.Name}  ·  " +
                $"Size: {fi.Length / 1024.0:F1} KB  ·  " +
                $"Modified: {fi.LastWriteTime:MMM dd, yyyy  HH:mm:ss}  ·  " +
                $"{lines.Count} lines";

            SetStatus($"Loaded {lines.Count} lines.", TealAccent);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to read log file.");
            SetStatus("Failed to read log file.", DangerRed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Filtering & Rendering
    // ─────────────────────────────────────────────────────────────────────────

    private void ApplyFilter()
    {
        var levelFilter = _cmbFilter.SelectedItem?.ToString() ?? "All Levels";
        var searchText = _txtSearch.Text.Trim().ToLower();

        var filtered = _allLines.Where(l =>
        {
            bool levelMatch = levelFilter == "All Levels" ||
                l.Level.Contains(levelFilter, StringComparison.OrdinalIgnoreCase);

            bool searchMatch = string.IsNullOrEmpty(searchText) ||
                l.Raw.ToLower().Contains(searchText);

            return levelMatch && searchMatch;
        }).ToList();

        RenderLines(filtered);
        UpdateStats(filtered);
        SetStatus($"Showing {filtered.Count} of {_allLines.Count} lines.", TealAccent);
    }

    private void RenderLines(List<LogLine> lines)
    {
        _rtbLog.SuspendLayout();
        _rtbLog.Clear();

        foreach (var line in lines)
        {
            Color lineColor = line.Level.ToUpperInvariant() switch
            {
                var l when l.Contains("FTL") || l.Contains("FATAL") => LogFatal,
                var l when l.Contains("ERR") || l.Contains("ERROR") => LogError,
                var l when l.Contains("WRN") || l.Contains("WARN") => LogWarn,
                var l when l.Contains("INF") || l.Contains("INFO") => LogInfo,
                var l when l.Contains("DBG") || l.Contains("DEBUG") => LogDebug,
                _ => LogText
            };

            // Timestamp — muted
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.SelectionColor = LogMuted;
            _rtbLog.AppendText(line.Timestamp.Length > 0
                ? $"[{line.Timestamp}] "
                : string.Empty);

            // Level tag — colored
            if (line.Level.Length > 0)
            {
                _rtbLog.SelectionColor = lineColor;
                _rtbLog.AppendText($"[{line.Level}] ");
            }

            // Message — standard
            _rtbLog.SelectionColor = lineColor == LogText ? LogText
                : Color.FromArgb(
                    (lineColor.R + 200) / 2,
                    (lineColor.G + 220) / 2,
                    (lineColor.B + 210) / 2);
            _rtbLog.AppendText(line.Message + "\n");
        }

        _rtbLog.ResumeLayout();

        // Scroll to bottom
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.ScrollToCaret();
    }

    private static LogLine ParseLogLine(string raw)
    {
        // Serilog default format:
        // 2024-01-15 10:23:45.123 +08:00 [INF] Message text
        try
        {
            int levelStart = raw.IndexOf('[');
            int levelEnd = raw.IndexOf(']', levelStart + 1);

            if (levelStart < 0 || levelEnd < 0)
                return new LogLine(raw, "", "", raw);

            string timestamp = raw[..levelStart].Trim();
            string level = raw[(levelStart + 1)..levelEnd].Trim();
            string message = raw[(levelEnd + 1)..].Trim();

            return new LogLine(raw, level, timestamp, message);
        }
        catch
        {
            return new LogLine(raw, "", "", raw);
        }
    }

    private void UpdateStats(List<LogLine> lines)
    {
        _lblTotalCount.Text = lines.Count.ToString();
        _lblInfoCount.Text = lines.Count(l =>
            l.Level.Contains("INF", StringComparison.OrdinalIgnoreCase)).ToString();
        _lblWarnCount.Text = lines.Count(l =>
            l.Level.Contains("WRN", StringComparison.OrdinalIgnoreCase) ||
            l.Level.Contains("WARN", StringComparison.OrdinalIgnoreCase)).ToString();
        _lblErrorCount.Text = lines.Count(l =>
            l.Level.Contains("ERR", StringComparison.OrdinalIgnoreCase)).ToString();
        _lblFatalCount.Text = lines.Count(l =>
            l.Level.Contains("FTL", StringComparison.OrdinalIgnoreCase) ||
            l.Level.Contains("FATAL", StringComparison.OrdinalIgnoreCase)).ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Button Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void CopyAllToClipboard(object? sender, EventArgs e)
    {
        if (_rtbLog.TextLength == 0) return;
        Clipboard.SetText(_rtbLog.Text);
        SetStatus("Log content copied to clipboard.", TealAccent);
    }

    private void OpenLogDirectory(object? sender, EventArgs e)
    {
        string dir = AppLogger.GetLogDirectory();
        if (!Directory.Exists(dir))
        {
            MessageBox.Show(
                "Log directory does not exist yet.",
                "Not Found",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            return;
        }

        try
        {
            System.Diagnostics.Process.Start("explorer.exe", dir);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to open log directory.");
            SetStatus("Failed to open folder.", DangerRed);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    private static Label MakeStatLabel(string text, Color color) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 13f, FontStyle.Bold),
        ForeColor = color,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label MakeFlowLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
        Margin = new Padding(0, 6, 4, 0),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Button MakeToolButton(
        string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 32),
            Cursor = Cursors.Hand,
            Margin = new Padding(0, 2, 6, 0),
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    // ── Helper record for combo items ─────────────────────────────────────────
    private record LogFileItem(string Label, string Path)
    {
        public override string ToString() => Label;
    }
}