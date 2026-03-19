using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.Interfaces;
using NutritionMonitor.UI.Session;
using static OpenTK.Graphics.OpenGL.GL;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.Backup;

public class BackupForm : UserControl
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
    private static readonly Color DangerLight = Color.FromArgb(254, 226, 226);
    private static readonly Color DangerHover = Color.FromArgb(190, 40, 40);
    private static readonly Color SuccessGreen = Color.FromArgb(0, 168, 150);
    private static readonly Color SuccessLight = Color.FromArgb(209, 250, 229);
    private static readonly Color AmberColor = Color.FromArgb(245, 158, 11);
    private static readonly Color AmberLight = Color.FromArgb(254, 243, 199);
    private static readonly Color LogBg = Color.FromArgb(22, 32, 50);
    private static readonly Color LogText = Color.FromArgb(180, 220, 200);
    private static readonly Color LogMuted = Color.FromArgb(90, 120, 100);

    // ── Layout ────────────────────────────────────────────────────────────────
    private TableLayoutPanel _outerLayout = null!;
    private Panel _headerPanel = null!;
    private Panel _bodyPanel = null!;
    private Panel _logPanel = null!;
    private Panel _statusBar = null!;

    // ── Export card controls ──────────────────────────────────────────────────
    private Label _lblExportPath = null!;
    private Button _btnBrowseExport = null!;
    private Button _btnExport = null!;
    private Label _lblExportStatus = null!;

    // ── Import card controls ──────────────────────────────────────────────────
    private Label _lblImportPath = null!;
    private Button _btnBrowseImport = null!;
    private Button _btnImport = null!;
    private Label _lblImportStatus = null!;

    // ── Activity log ──────────────────────────────────────────────────────────
    private RichTextBox _rtbLog = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Label _lblStatus = null!;
    private Label _lblLastAction = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private string _exportPath = string.Empty;
    private string _importPath = string.Empty;
    private readonly Panel _parentContentArea;

    // ─────────────────────────────────────────────────────────────────────────
    public BackupForm(Panel parentContentArea)
    {
        _parentContentArea = parentContentArea;
        BuildControl();
        LogActivity("System", "Backup & Restore module loaded.");
        LogActivity("Info", $"Logged in as: {(SessionManager.IsLoggedIn ? SessionManager.Current.FullName : "Unknown")}");
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Control Construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildControl()
    {
        BackColor = BgColor;

        _outerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = BgColor,
            Padding = new Padding(0)
        };
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));   // header
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 260f));  // cards row
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // activity log
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));   // status bar

        BuildHeader();
        BuildCardsRow();
        BuildActivityLog();
        BuildStatusBar();

        _outerLayout.Controls.Add(_headerPanel, 0, 0);
        _outerLayout.Controls.Add(_bodyPanel, 0, 1);
        _outerLayout.Controls.Add(_logPanel, 0, 2);
        _outerLayout.Controls.Add(_statusBar, 0, 3);

        Controls.Add(_outerLayout);
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

            // Left accent bar
            using var bar = new SolidBrush(TealAccent);
            g.FillRectangle(bar, 0, 0, 5, _headerPanel.Height);

            // Subtle right gradient
            var rect = new Rectangle(
                _headerPanel.Width - 220, 0, 220, _headerPanel.Height);
            using var grad = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.Transparent, TealLight,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(grad, rect);

            // Bottom border
            using var pen = new Pen(BorderLight, 1);
            g.DrawLine(pen, 0, _headerPanel.Height - 1,
                _headerPanel.Width, _headerPanel.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = "Backup & Restore",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(500, 36),
            Location = new Point(20, 12),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Export all student and meal log records to a JSON file, " +
                        "or restore data from a previous backup.  " +
                        "⚠  Import will attempt to add records — duplicates are skipped.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(700, 22),
            Location = new Point(20, 52),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _headerPanel.Controls.AddRange(new Control[] { lblTitle, lblSub });
    }

    // ── Cards Row ─────────────────────────────────────────────────────────────

    private void BuildCardsRow()
    {
        _bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 16, 20, 0)
        };

        var cardsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BgColor
        };
        cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        cardsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        cardsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        var exportCard = BuildExportCard();
        var importCard = BuildImportCard();

        cardsLayout.Controls.Add(exportCard, 0, 0);
        cardsLayout.Controls.Add(importCard, 1, 0);
        _bodyPanel.Controls.Add(cardsLayout);
    }

    // ── Export Card ───────────────────────────────────────────────────────────

    private Panel BuildExportCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 10, 0)
        };

        card.Paint += (s, e) => PaintCard(e.Graphics, card, TealAccent);

        // Icon area
        var iconLbl = new Label
        {
            Text = "💾",
            Font = new Font("Segoe UI", 28f),
            ForeColor = TealAccent,
            AutoSize = false,
            Size = new Size(60, 60),
            Location = new Point(20, 16),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblCardTitle = new Label
        {
            Text = "Export Backup",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(300, 28),
            Location = new Point(88, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblDesc = new Label
        {
            Text = "Exports all students and meal logs\nto a structured JSON backup file.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(300, 38),
            Location = new Point(88, 46),
            TextAlign = ContentAlignment.TopLeft
        };

        // Path row
        var lblPathLabel = MakeFieldLabel("SAVE TO", new Point(20, 98));

        var pathRow = new Panel
        {
            Location = new Point(20, 118),
            Size = new Size(0, 36),
            BackColor = Color.FromArgb(246, 248, 252)
        };
        pathRow.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                pathRow.Width - 1, pathRow.Height - 1);
        };

        _lblExportPath = new Label
        {
            Text = "No file selected…",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        pathRow.Controls.Add(_lblExportPath);

        _btnBrowseExport = MakeSecondaryButton("Browse…", 80);
        _btnBrowseExport.Dock = DockStyle.Right;

        pathRow.Controls.Add(_btnBrowseExport);

        // Status label
        _lblExportStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SuccessGreen,
            AutoSize = false,
            Size = new Size(0, 18),
            Location = new Point(20, 162),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Export button
        _btnExport = MakePrimaryButton("💾  Export Now", TealAccent, 160);
        _btnExport.Location = new Point(20, 185);

        // Wire events
        _btnBrowseExport.Click += BrowseExportPath;
        _btnExport.Click += async (_, _) => await RunExportAsync();

        // Anchor/resize
        card.Resize += (_, _) =>
        {
            int w = card.Width - 40;
            if (w < 10) return;
            pathRow.Width = w;
            _lblExportStatus.Width = w;
            _btnExport.Width = Math.Min(160, w);
        };

        card.Controls.AddRange(new Control[]
        {
            iconLbl, lblCardTitle, lblDesc,
            lblPathLabel, pathRow,
            _lblExportStatus, _btnExport
        });

        return card;
    }

    // ── Import Card ───────────────────────────────────────────────────────────

    private Panel BuildImportCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(10, 0, 0, 0)
        };

        card.Paint += (s, e) => PaintCard(e.Graphics, card, AmberColor);

        var iconLbl = new Label
        {
            Text = "📂",
            Font = new Font("Segoe UI", 28f),
            ForeColor = AmberColor,
            AutoSize = false,
            Size = new Size(60, 60),
            Location = new Point(20, 16),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblCardTitle = new Label
        {
            Text = "Restore Backup",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(300, 28),
            Location = new Point(88, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblDesc = new Label
        {
            Text = "Imports students and meal logs\nfrom a JSON backup file.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(300, 38),
            Location = new Point(88, 46),
            TextAlign = ContentAlignment.TopLeft
        };

        // Warning badge
        var warnPanel = new Panel
        {
            Location = new Point(20, 90),
            Size = new Size(0, 26),
            BackColor = AmberLight
        };

        warnPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(250, 200, 80), 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                warnPanel.Width - 1, warnPanel.Height - 1);
        };

        var warnLbl = new Label
        {
            Text = "⚠  Existing records will NOT be overwritten. Duplicates are skipped.",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(140, 90, 0),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        warnPanel.Controls.Add(warnLbl);

        // Path row
        var lblPathLabel = MakeFieldLabel("LOAD FROM", new Point(20, 124));

        var pathRow = new Panel
        {
            Location = new Point(20, 144),
            Size = new Size(0, 36),
            BackColor = Color.FromArgb(246, 248, 252)
        };
        pathRow.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                pathRow.Width - 1, pathRow.Height - 1);
        };

        _lblImportPath = new Label
        {
            Text = "No file selected…",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };
        pathRow.Controls.Add(_lblImportPath);

        _btnBrowseImport = MakeSecondaryButton("Browse…", 80);
        _btnBrowseImport.Dock = DockStyle.Right;
        pathRow.Controls.Add(_btnBrowseImport);

        // Status label
        _lblImportStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SuccessGreen,
            AutoSize = false,
            Size = new Size(0, 18),
            Location = new Point(20, 188),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Import button
        _btnImport = MakePrimaryButton("📂  Restore Now", AmberColor, 160);
        _btnImport.Location = new Point(20, 210);
        _btnImport.MouseEnter += (_, _) =>
            _btnImport.BackColor = Color.FromArgb(210, 130, 0);
        _btnImport.MouseLeave += (_, _) =>
            _btnImport.BackColor = AmberColor;

        // Wire events
        _btnBrowseImport.Click += BrowseImportPath;
        _btnImport.Click += async (_, _) => await RunImportAsync();

        // Anchor/resize
        card.Resize += (_, _) =>
        {
            int w = card.Width - 40;
            if (w < 10) return;
            warnPanel.Width = w;
            pathRow.Width = w;
            _lblImportStatus.Width = w;
            _btnImport.Width = Math.Min(160, w);
        };

        card.Controls.AddRange(new Control[]
        {
            iconLbl, lblCardTitle, lblDesc, warnPanel,
            lblPathLabel, pathRow,
            _lblImportStatus, _btnImport
        });

        return card;
    }

    // ── Activity Log ──────────────────────────────────────────────────────────

    private void BuildActivityLog()
    {
        _logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 12, 20, 0)
        };

        var logCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = LogBg
        };

        logCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 80, 60), 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                logCard.Width - 1, logCard.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, logCard.Width, 3);
        };

        var logHeaderPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.FromArgb(15, 24, 40),
            Padding = new Padding(14, 0, 14, 0)
        };

        logHeaderPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 70, 55), 1);
            e.Graphics.DrawLine(pen, 0, logHeaderPanel.Height - 1,
                logHeaderPanel.Width, logHeaderPanel.Height - 1);
        };

        var lblLogTitle = new Label
        {
            Text = "●  ACTIVITY LOG",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = TealAccent,
            AutoSize = true,
            Location = new Point(0, 0),
            Height = 34,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var btnClearLog = new Button
        {
            Text = "Clear",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(90, 120, 100),
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(48, 22),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.Click += (_, _) =>
        {
            _rtbLog.Clear();
            LogActivity("System", "Log cleared.");
        };

        logHeaderPanel.Controls.Add(lblLogTitle);
        logHeaderPanel.Resize += (_, _) =>
            btnClearLog.Location = new Point(
                logHeaderPanel.Width - btnClearLog.Width - 14,
                (logHeaderPanel.Height - btnClearLog.Height) / 2);
        logHeaderPanel.Controls.Add(btnClearLog);

        _rtbLog = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = LogBg,
            ForeColor = LogText,
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Padding = new Padding(10, 6, 10, 6),
            WordWrap = true
        };

        logCard.Controls.Add(_rtbLog);
        logCard.Controls.Add(logHeaderPanel);
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

        _lblLastAction = new Label
        {
            Text = "No backup or restore has been run this session.",
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

        _statusBar.Controls.AddRange(new Control[] { _lblLastAction, _lblStatus });
        _statusBar.Resize += (_, _) =>
            _lblStatus.Location = new Point(
                _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  File Browse Handlers
    // ─────────────────────────────────────────────────────────────────────────

    private void BrowseExportPath(object? sender, EventArgs e)
    {
        using var dlg = new SaveFileDialog
        {
            Title = "Save Backup File",
            Filter = "JSON Backup (*.json)|*.json|All Files (*.*)|*.*",
            FileName = $"NutritionBackup_{DateTime.Now:yyyyMMdd_HHmmss}.json",
            DefaultExt = "json",
            InitialDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        _exportPath = dlg.FileName;
        _lblExportPath.Text = ShortenPath(_exportPath, 55);
        _lblExportPath.ForeColor = TextDark;
        _lblExportStatus.Text = string.Empty;

        LogActivity("Export", $"Save path selected: {_exportPath}");
    }

    private void BrowseImportPath(object? sender, EventArgs e)
    {
        using var dlg = new OpenFileDialog
        {
            Title = "Open Backup File",
            Filter = "JSON Backup (*.json)|*.json|All Files (*.*)|*.*",
            DefaultExt = "json",
            InitialDirectory = Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments)
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        _importPath = dlg.FileName;
        _lblImportPath.Text = ShortenPath(_importPath, 55);
        _lblImportPath.ForeColor = TextDark;
        _lblImportStatus.Text = string.Empty;

        // Show file info
        try
        {
            var fi = new FileInfo(_importPath);
            LogActivity("Import",
                $"File selected: {_importPath}");
            LogActivity("Import",
                $"File size: {fi.Length / 1024.0:F1} KB  |  " +
                $"Modified: {fi.LastWriteTime:MMM dd, yyyy HH:mm}");
        }
        catch { /* non-critical */ }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Export Logic
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunExportAsync()
    {
        if (string.IsNullOrWhiteSpace(_exportPath))
        {
            SetCardStatus(_lblExportStatus,
                "⚠  Please choose a save location first.", AmberColor);
            LogActivity("Export", "Aborted — no path selected.");
            return;
        }

        SetLoading(_btnExport, true, "Exporting…");
        SetStatus("Exporting data…", TextMuted);
        LogActivity("Export", $"Starting export to: {_exportPath}");

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var backupService = scope.ServiceProvider
                .GetRequiredService<IBackupService>();

            var (success, message) = await backupService.ExportAsync(_exportPath);

            if (success)
            {
                var fi = new FileInfo(_exportPath);
                var size = fi.Exists ? $"{fi.Length / 1024.0:F1} KB" : "—";

                SetCardStatus(_lblExportStatus,
                    $"✅  Export successful  ({size})", SuccessGreen);
                SetStatus("Export complete.", TealAccent);

                _lblLastAction.Text =
                    $"Last export: {DateTime.Now:MMM dd, yyyy  HH:mm:ss}  →  " +
                    ShortenPath(_exportPath, 50);

                LogActivity("Export", $"✅ Success — {message}");
                LogActivity("Export", $"File size: {size}");

                SerilogLog.Information(
                    "Backup exported by {User} to {Path}",
                    SessionManager.Current.FullName, _exportPath);
            }
            else
            {
                SetCardStatus(_lblExportStatus,
                    $"❌  {message}", DangerRed);
                SetStatus("Export failed.", DangerRed);
                LogActivity("Export", $"❌ Failed: {message}");
            }
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Export failed.");
            SetCardStatus(_lblExportStatus,
                "❌  Unexpected error. Check logs.", DangerRed);
            SetStatus("Export error.", DangerRed);
            LogActivity("Export", $"❌ Exception: {ex.Message}");
        }
        finally
        {
            SetLoading(_btnExport, false, "💾  Export Now");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Import Logic
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunImportAsync()
    {
        if (string.IsNullOrWhiteSpace(_importPath))
        {
            SetCardStatus(_lblImportStatus,
                "⚠  Please select a backup file first.", AmberColor);
            LogActivity("Import", "Aborted — no file selected.");
            return;
        }

        // Confirm before import
        var confirm = MessageBox.Show(
            "This will attempt to import all students and meal logs " +
            "from the selected backup file.\n\n" +
            "Existing records will NOT be overwritten — only new records " +
            "will be added.\n\n" +
            "Do you want to continue?",
            "Confirm Restore",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (confirm != DialogResult.Yes)
        {
            LogActivity("Import", "Cancelled by user.");
            return;
        }

        SetLoading(_btnImport, true, "Restoring…");
        SetStatus("Importing data…", TextMuted);
        LogActivity("Import", $"Starting import from: {_importPath}");

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var backupService = scope.ServiceProvider
                .GetRequiredService<IBackupService>();

            var (success, message) = await backupService.ImportAsync(_importPath);

            if (success)
            {
                SetCardStatus(_lblImportStatus,
                    $"✅  {message}", SuccessGreen);
                SetStatus("Restore complete.", TealAccent);

                _lblLastAction.Text =
                    $"Last import: {DateTime.Now:MMM dd, yyyy  HH:mm:ss}  ←  " +
                    ShortenPath(_importPath, 50);

                LogActivity("Import", $"✅ Success — {message}");

                SerilogLog.Information(
                    "Backup imported by {User} from {Path}",
                    SessionManager.Current.FullName, _importPath);
            }
            else
            {
                SetCardStatus(_lblImportStatus,
                    $"❌  {message}", DangerRed);
                SetStatus("Import failed.", DangerRed);
                LogActivity("Import", $"❌ Failed: {message}");
            }
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Import failed.");
            SetCardStatus(_lblImportStatus,
                "❌  Unexpected error. Check logs.", DangerRed);
            SetStatus("Import error.", DangerRed);
            LogActivity("Import", $"❌ Exception: {ex.Message}");
        }
        finally
        {
            SetLoading(_btnImport, false, "📂  Restore Now");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Activity Log Writer
    // ─────────────────────────────────────────────────────────────────────────

    private void LogActivity(string category, string message)
    {
        if (_rtbLog == null) return;

        string timestamp = DateTime.Now.ToString("HH:mm:ss");

        // Color-code by category
        Color catColor = category switch
        {
            "Export" => TealAccent,
            "Import" => AmberColor,
            "System" => Color.FromArgb(130, 160, 200),
            "Info" => Color.FromArgb(100, 140, 180),
            _ => LogText
        };

        _rtbLog.SuspendLayout();

        // Timestamp
        _rtbLog.SelectionStart = _rtbLog.TextLength;
        _rtbLog.SelectionLength = 0;
        _rtbLog.SelectionColor = LogMuted;
        _rtbLog.AppendText($"[{timestamp}] ");

        // Category tag
        _rtbLog.SelectionColor = catColor;
        _rtbLog.AppendText($"[{category.ToUpperInvariant()}] ");

        // Message
        _rtbLog.SelectionColor = LogText;
        _rtbLog.AppendText($"{message}\n");

        _rtbLog.ResumeLayout();
        _rtbLog.ScrollToCaret();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Paint Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void PaintCard(
        Graphics g, Panel card, Color accentColor)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // Top accent bar
        using var topBar = new SolidBrush(accentColor);
        g.FillRectangle(topBar, 0, 0, card.Width, 4);

        // Border
        using var pen = new Pen(BorderLight, 1);
        g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI State Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static void SetCardStatus(Label lbl, string text, Color color)
    {
        lbl.Text = text;
        lbl.ForeColor = color;
    }

    private void SetLoading(Button btn, bool loading, string label)
    {
        btn.Enabled = !loading;
        btn.Text = label;
        Application.DoEvents();
    }

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Factory Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static Button MakePrimaryButton(
        string text, Color bg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            BackColor = bg,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 40),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }

    private static Button MakeSecondaryButton(string text, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = TextMid,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 36),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 1;
        btn.FlatAppearance.BorderColor = BorderLight;
        return btn;
    }

    private static Label MakeFieldLabel(string text, Point location) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = false,
        Size = new Size(300, 18),
        Location = location,
        TextAlign = ContentAlignment.BottomLeft
    };

    private static string ShortenPath(string path, int maxLen)
    {
        if (path.Length <= maxLen) return path;
        var parts = path.Split(Path.DirectorySeparatorChar);
        if (parts.Length <= 2) return path;
        return $"{parts[0]}{Path.DirectorySeparatorChar}…" +
               $"{Path.DirectorySeparatorChar}{parts[^2]}" +
               $"{Path.DirectorySeparatorChar}{parts[^1]}";
    }
}