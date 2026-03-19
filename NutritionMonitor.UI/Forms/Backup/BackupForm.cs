// File Path: NutritionMonitor.UI/Forms/Backup/BackupForm.cs
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.Interfaces;
using NutritionMonitor.UI.Session;
using SerilogLog = Serilog.Log;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;

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
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));   // header
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 300f));  // cards row (increased slightly to accommodate stacking)
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
            BackColor = CardBg,
            Margin = new Padding(0)
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

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 12, 20, 12),
            Margin = new Padding(0)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitle = new Label
        {
            Text = "Backup & Restore",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Export all student and meal log records to a JSON file, " +
                   "or restore data from a previous backup.  " +
                   "⚠  Import will attempt to add records — duplicates are skipped.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        headerLayout.Controls.Add(lblTitle, 0, 0);
        headerLayout.Controls.Add(lblSub, 0, 1);
        _headerPanel.Controls.Add(headerLayout);
    }

    // ── Cards Row ─────────────────────────────────────────────────────────────

    private void BuildCardsRow()
    {
        _bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 16, 20, 0),
            Margin = new Padding(0)
        };

        var cardsLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BgColor,
            Margin = new Padding(0)
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

        // Vertical Stacking
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header (Icon + Title)
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Spacer/Label
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Path row
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Spacer for status
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Button

        // Icon + Title Layout
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var iconLbl = new Label
        {
            Text = "💾",
            Font = new Font("Segoe UI", 28f),
            ForeColor = TealAccent,
            AutoSize = false,
            Size = new Size(60, 60),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0)
        };
        headerLayout.SetRowSpan(iconLbl, 2);

        var lblCardTitle = new Label
        {
            Text = "Export Backup",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 4)
        };

        var lblDesc = new Label
        {
            Text = "Exports all students and meal logs\nto a structured JSON backup file.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0)
        };

        headerLayout.Controls.Add(iconLbl, 0, 0);
        headerLayout.Controls.Add(lblCardTitle, 1, 0);
        headerLayout.Controls.Add(lblDesc, 1, 1);
        layout.Controls.Add(headerLayout, 0, 0);

        // Path label
        var lblPathLabel = MakeFieldLabel("SAVE TO");
        lblPathLabel.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblPathLabel, 0, 1);

        // Path row
        var pathRow = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 36,
            BackColor = Color.FromArgb(246, 248, 252),
            Margin = new Padding(0)
        };
        pathRow.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pathRow.Width - 1, pathRow.Height - 1);
        };

        var pathLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblExportPath = new Label
        {
            Text = "No file selected…",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Margin = new Padding(0)
        };

        _btnBrowseExport = MakeSecondaryButton("Browse…", 80);
        _btnBrowseExport.Anchor = AnchorStyles.Right;
        _btnBrowseExport.Margin = new Padding(0);

        pathLayout.Controls.Add(_lblExportPath, 0, 0);
        pathLayout.Controls.Add(_btnBrowseExport, 1, 0);
        pathRow.Controls.Add(pathLayout);
        layout.Controls.Add(pathRow, 0, 2);

        // Status label
        _lblExportStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SuccessGreen,
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(_lblExportStatus, 0, 3);

        // Export button
        _btnExport = MakePrimaryButton("💾  Export Now", TealAccent, 160);
        _btnExport.Anchor = AnchorStyles.Left;
        _btnExport.Margin = new Padding(0);

        layout.Controls.Add(_btnExport, 0, 4);

        // Wire events
        _btnBrowseExport.Click += BrowseExportPath;
        _btnExport.Click += async (_, _) => await RunExportAsync();

        card.Controls.Add(layout);
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 6,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Header
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Warning
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Path label
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Path box
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Status spacer
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // Button

        // Icon + Title Layout
        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 68f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var iconLbl = new Label
        {
            Text = "📂",
            Font = new Font("Segoe UI", 28f),
            ForeColor = AmberColor,
            AutoSize = false,
            Size = new Size(60, 60),
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0)
        };
        headerLayout.SetRowSpan(iconLbl, 2);

        var lblCardTitle = new Label
        {
            Text = "Restore Backup",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 6, 0, 4)
        };

        var lblDesc = new Label
        {
            Text = "Imports students and meal logs\nfrom a JSON backup file.",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0)
        };

        headerLayout.Controls.Add(iconLbl, 0, 0);
        headerLayout.Controls.Add(lblCardTitle, 1, 0);
        headerLayout.Controls.Add(lblDesc, 1, 1);
        layout.Controls.Add(headerLayout, 0, 0);

        // Warning badge
        var warnPanel = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 26,
            BackColor = AmberLight,
            Margin = new Padding(0, 0, 0, 16)
        };

        warnPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(250, 200, 80), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, warnPanel.Width - 1, warnPanel.Height - 1);
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
        layout.Controls.Add(warnPanel, 0, 1);

        // Path label
        var lblPathLabel = MakeFieldLabel("LOAD FROM");
        lblPathLabel.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblPathLabel, 0, 2);

        // Path row
        var pathRow = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 36,
            BackColor = Color.FromArgb(246, 248, 252),
            Margin = new Padding(0)
        };
        pathRow.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, pathRow.Width - 1, pathRow.Height - 1);
        };

        var pathLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pathLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _lblImportPath = new Label
        {
            Text = "No file selected…",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = false,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Margin = new Padding(0)
        };

        _btnBrowseImport = MakeSecondaryButton("Browse…", 80);
        _btnBrowseImport.Anchor = AnchorStyles.Right;
        _btnBrowseImport.Margin = new Padding(0);

        pathLayout.Controls.Add(_lblImportPath, 0, 0);
        pathLayout.Controls.Add(_btnBrowseImport, 1, 0);
        pathRow.Controls.Add(pathLayout);
        layout.Controls.Add(pathRow, 0, 3);

        // Status label
        _lblImportStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = SuccessGreen,
            AutoSize = true,
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
            Margin = new Padding(0, 0, 0, 8)
        };
        layout.Controls.Add(_lblImportStatus, 0, 4);

        // Import button
        _btnImport = MakePrimaryButton("📂  Restore Now", AmberColor, 160);
        _btnImport.Anchor = AnchorStyles.Left;
        _btnImport.Margin = new Padding(0);
        _btnImport.MouseEnter += (_, _) => _btnImport.BackColor = Color.FromArgb(210, 130, 0);
        _btnImport.MouseLeave += (_, _) => _btnImport.BackColor = AmberColor;

        layout.Controls.Add(_btnImport, 0, 5);

        // Wire events
        _btnBrowseImport.Click += BrowseImportPath;
        _btnImport.Click += async (_, _) => await RunImportAsync();

        card.Controls.Add(layout);
        return card;
    }

    // ── Activity Log ──────────────────────────────────────────────────────────

    private void BuildActivityLog()
    {
        _logPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 12, 20, 0),
            Margin = new Padding(0)
        };

        var logCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = LogBg
        };

        logCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 80, 60), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, logCard.Width - 1, logCard.Height - 1);
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
            e.Graphics.DrawLine(pen, 0, logHeaderPanel.Height - 1, logHeaderPanel.Width, logHeaderPanel.Height - 1);
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        var lblLogTitle = new Label
        {
            Text = "●  ACTIVITY LOG",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = TealAccent,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
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
            TabStop = false,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0)
        };
        btnClearLog.FlatAppearance.BorderSize = 0;
        btnClearLog.Click += (_, _) =>
        {
            _rtbLog.Clear();
            LogActivity("System", "Log cleared.");
        };

        headerLayout.Controls.Add(lblLogTitle, 0, 0);
        headerLayout.Controls.Add(btnClearLog, 1, 0);
        logHeaderPanel.Controls.Add(headerLayout);

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

        var statusLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        statusLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        _lblLastAction = new Label
        {
            Text = "No backup or restore has been run this session.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        _lblStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TealAccent,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0)
        };

        statusLayout.Controls.Add(_lblLastAction, 0, 0);
        statusLayout.Controls.Add(_lblStatus, 1, 0);
        _statusBar.Controls.Add(statusLayout);
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

        await SetLoadingAsync(_btnExport, true, "Exporting…");
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
            await SetLoadingAsync(_btnExport, false, "💾  Export Now");
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

        await SetLoadingAsync(_btnImport, true, "Restoring…");
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
            await SetLoadingAsync(_btnImport, false, "📂  Restore Now");
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

    private async Task SetLoadingAsync(Button btn, bool loading, string label)
    {
        btn.Enabled = !loading;
        btn.Text = label;
        await Task.Yield(); // Rule 11 implementation
    }

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
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

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
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