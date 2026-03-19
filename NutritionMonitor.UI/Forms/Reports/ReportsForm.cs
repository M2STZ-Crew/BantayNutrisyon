// File Path: NutritionMonitor.UI/Forms/Reports/ReportsForm.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using NutritionMonitor.UI.Session;
using System.Text;
using System.Text.Json;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using System.IO;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.Reports;

public class ReportsForm : UserControl
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
    private static readonly Color AmberLight = Color.FromArgb(254, 243, 199);
    private static readonly Color StatBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color StatBlueLight = Color.FromArgb(219, 234, 254);
    private static readonly Color PreviewBg = Color.FromArgb(22, 32, 50);
    private static readonly Color PreviewText = Color.FromArgb(200, 220, 210);
    private static readonly Color PreviewMuted = Color.FromArgb(80, 110, 95);

    // ── Layout ────────────────────────────────────────────────────────────────
    private TableLayoutPanel _outerLayout = null!;
    private Panel _headerPanel = null!;
    private Panel _configPanel = null!;
    private Panel _previewPanel = null!;
    private Panel _statusBar = null!;

    // ── Config controls ───────────────────────────────────────────────────────
    private ComboBox _cmbReportType = null!;
    private ComboBox _cmbStudent = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private CheckBox _chkTxt = null!;
    private CheckBox _chkJson = null!;
    private TextBox _txtOutputDir = null!;
    private Button _btnBrowseDir = null!;
    private Button _btnGenerate = null!;
    private Button _btnPreview = null!;
    private ProgressBar _progressBar = null!;
    private Label _lblProgress = null!;

    // ── Preview ───────────────────────────────────────────────────────────────
    private RichTextBox _rtbPreview = null!;
    private Label _lblPreviewTitle = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Label _lblStatus = null!;
    private Label _lblLastReport = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<StudentDto> _students = new();
    private CancellationTokenSource? _cts = null;
    private readonly Panel _parentContentArea;

    // ── Report types ──────────────────────────────────────────────────────────
    private static readonly string[] ReportTypes =
    {
        "Full Nutrition Summary",
        "Malnutrition Risk Report",
        "Student Meal Log Report",
        "Nutrient Deficit Analysis",
        "At-Risk & Malnourished Students"
    };

    // ─────────────────────────────────────────────────────────────────────────
    public ReportsForm(Panel parentContentArea)
    {
        _parentContentArea = parentContentArea;
        BuildControl();
        _ = LoadStudentsAsync();
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
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 76f));   // header
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 320f));  // config
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // preview
        _outerLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));   // status

        BuildHeader();
        BuildConfigPanel();
        BuildPreviewPanel();
        BuildStatusBar();

        _outerLayout.Controls.Add(_headerPanel, 0, 0);
        _outerLayout.Controls.Add(_configPanel, 0, 1);
        _outerLayout.Controls.Add(_previewPanel, 0, 2);
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

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(20, 10, 20, 10),
            Margin = new Padding(0)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitle = new Label
        {
            Text = "Report Generator",
            Font = new Font("Segoe UI", 16f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Generate detailed nutrition reports asynchronously. " +
                   "The UI stays fully responsive during generation. " +
                   "Output formats: TXT and JSON.",
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

    // ── Config Panel ──────────────────────────────────────────────────────────

    private void BuildConfigPanel()
    {
        _configPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 14, 20, 0),
            Margin = new Padding(0)
        };

        var configLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = BgColor,
            Margin = new Padding(0)
        };
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
        configLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
        configLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        configLayout.Controls.Add(BuildSettingsCard(), 0, 0);
        configLayout.Controls.Add(BuildOutputCard(), 1, 0);

        _configPanel.Controls.Add(configLayout);
    }

    // ── Settings Card (left) ──────────────────────────────────────────────────

    private Panel BuildSettingsCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 10, 0)
        };

        card.Paint += (s, e) => PaintCard(e.Graphics, card, TealAccent);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(16, 12, 16, 16),
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 7; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        int row = 0;

        var lblTitle = new Label
        {
            Text = "⚙  Report Settings",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(lblTitle, 0, row++);

        // Report type
        var lblReportType = MakeFieldLabel("REPORT TYPE");
        lblReportType.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblReportType, 0, row++);

        _cmbReportType = new ComboBox
        {
            Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(246, 248, 252),
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            Margin = new Padding(0, 0, 0, 16)
        };
        _cmbReportType.Items.AddRange(ReportTypes);
        _cmbReportType.SelectedIndex = 0;
        layout.Controls.Add(_cmbReportType, 0, row++);

        // Student filter
        var lblStudent = MakeFieldLabel("STUDENT (OPTIONAL — blank = all)");
        lblStudent.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblStudent, 0, row++);

        _cmbStudent = new ComboBox
        {
            Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            BackColor = Color.FromArgb(246, 248, 252),
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            DisplayMember = "FullName",
            Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(_cmbStudent, 0, row++);

        // Date range row
        var datesLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0),
            AutoSize = true
        };
        datesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        datesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        datesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        datesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblFrom = MakeFieldLabel("FROM");
        lblFrom.Margin = new Padding(0, 0, 8, 4);
        var lblTo = MakeFieldLabel("TO");
        lblTo.Margin = new Padding(8, 0, 0, 4);
        datesLayout.Controls.Add(lblFrom, 0, 0);
        datesLayout.Controls.Add(lblTo, 1, 0);

        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddDays(-30),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 0, 8, 0)
        };

        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(8, 0, 0, 0)
        };

        datesLayout.Controls.Add(_dtpFrom, 0, 1);
        datesLayout.Controls.Add(_dtpTo, 1, 1);

        layout.Controls.Add(datesLayout, 0, row++);

        card.Controls.Add(layout);
        return card;
    }

    // ── Output Card (right) ───────────────────────────────────────────────────

    private Panel BuildOutputCard()
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(10, 0, 0, 0)
        };

        card.Paint += (s, e) => PaintCard(e.Graphics, card, StatBlue);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 10,
            Padding = new Padding(16, 12, 16, 16),
            Margin = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 10; i++) layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        int row = 0;

        var lblTitle = new Label
        {
            Text = "📤  Output Options",
            Font = new Font("Segoe UI", 10f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };
        layout.Controls.Add(lblTitle, 0, row++);

        // Format checkboxes
        var lblFormat = MakeFieldLabel("OUTPUT FORMAT");
        lblFormat.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblFormat, 0, row++);

        _chkTxt = new CheckBox
        {
            Text = "Plain Text  (.txt)",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextDark,
            Checked = true,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 4)
        };
        layout.Controls.Add(_chkTxt, 0, row++);

        _chkJson = new CheckBox
        {
            Text = "Structured JSON  (.json)",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextDark,
            Checked = true,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(_chkJson, 0, row++);

        // Output directory
        var lblDir = MakeFieldLabel("OUTPUT DIRECTORY");
        lblDir.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblDir, 0, row++);

        var dirRow = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 36,
            BackColor = Color.FromArgb(246, 248, 252),
            Margin = new Padding(0, 0, 0, 16)
        };
        dirRow.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, dirRow.Width - 1, dirRow.Height - 1);
        };

        var dirLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        dirLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        dirLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        _txtOutputDir = new TextBox
        {
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextDark,
            BackColor = Color.FromArgb(246, 248, 252),
            BorderStyle = BorderStyle.None,
            Dock = DockStyle.Fill,
            Padding = new Padding(8, 0, 0, 0),
            Margin = new Padding(8, 10, 0, 0),
            Text = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
        };

        _btnBrowseDir = new Button
        {
            Text = "Browse",
            Font = new Font("Segoe UI", 8.5f),
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = TextMid,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(70, 36),
            Dock = DockStyle.Right,
            Cursor = Cursors.Hand,
            Margin = new Padding(0)
        };
        _btnBrowseDir.FlatAppearance.BorderSize = 0;
        _btnBrowseDir.Click += BrowseOutputDir;

        dirLayout.Controls.Add(_txtOutputDir, 0, 0);
        dirLayout.Controls.Add(_btnBrowseDir, 1, 0);
        dirRow.Controls.Add(dirLayout);
        layout.Controls.Add(dirRow, 0, row++);

        // Progress bar
        _progressBar = new ProgressBar
        {
            Style = ProgressBarStyle.Marquee,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 6,
            Visible = false,
            MarqueeAnimationSpeed = 30,
            Margin = new Padding(0, 0, 0, 4)
        };
        layout.Controls.Add(_progressBar, 0, row++);

        _lblProgress = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8f),
            ForeColor = TealAccent,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(_lblProgress, 0, row++);

        // Flexible spacer to push buttons to the bottom
        layout.RowStyles[row] = new RowStyle(SizeType.Percent, 100f);
        layout.Controls.Add(new Panel { Dock = DockStyle.Fill, Margin = new Padding(0) }, 0, row++);

        // Action buttons
        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _btnPreview = MakeButton("👁  Preview", Color.FromArgb(240, 244, 248), TextMid, 110);
        _btnPreview.FlatAppearance.BorderSize = 1;
        _btnPreview.FlatAppearance.BorderColor = BorderLight;
        _btnPreview.Margin = new Padding(0, 0, 16, 0);

        _btnGenerate = MakeButton("▶  Generate Report", TealAccent, Color.White, 160);
        _btnGenerate.Margin = new Padding(0);

        btnFlow.Controls.Add(_btnPreview);
        btnFlow.Controls.Add(_btnGenerate);
        layout.Controls.Add(btnFlow, 0, row++);

        _btnPreview.Click += async (_, _) => await RunPreviewAsync();
        _btnGenerate.Click += async (_, _) => await RunGenerateAsync();

        card.Controls.Add(layout);
        return card;
    }

    // ── Preview Panel ─────────────────────────────────────────────────────────

    private void BuildPreviewPanel()
    {
        _previewPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 12, 20, 0),
            Margin = new Padding(0)
        };

        var previewCard = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = PreviewBg,
            Margin = new Padding(0)
        };

        previewCard.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 70, 55), 1);
            e.Graphics.DrawRectangle(pen, 0, 0,
                previewCard.Width - 1, previewCard.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, previewCard.Width, 3);
        };

        // Preview header bar
        var previewHeader = new Panel
        {
            Dock = DockStyle.Top,
            Height = 34,
            BackColor = Color.FromArgb(15, 24, 40),
            Padding = new Padding(14, 0, 14, 0),
            Margin = new Padding(0)
        };

        previewHeader.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 70, 55), 1);
            e.Graphics.DrawLine(pen, 0, previewHeader.Height - 1,
                previewHeader.Width, previewHeader.Height - 1);
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

        _lblPreviewTitle = new Label
        {
            Text = "●  REPORT PREVIEW",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = TealAccent,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0)
        };

        var btnClearPreview = new Button
        {
            Text = "Clear",
            Font = new Font("Segoe UI", 8f),
            ForeColor = PreviewMuted,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(48, 22),
            Cursor = Cursors.Hand,
            TabStop = false,
            Anchor = AnchorStyles.Right,
            Margin = new Padding(0)
        };
        btnClearPreview.FlatAppearance.BorderSize = 0;
        btnClearPreview.Click += (_, _) =>
        {
            _rtbPreview.Clear();
            _lblPreviewTitle.Text = "●  REPORT PREVIEW";
        };

        headerLayout.Controls.Add(_lblPreviewTitle, 0, 0);
        headerLayout.Controls.Add(btnClearPreview, 1, 0);
        previewHeader.Controls.Add(headerLayout);

        _rtbPreview = new RichTextBox
        {
            Dock = DockStyle.Fill,
            BackColor = PreviewBg,
            ForeColor = PreviewText,
            Font = new Font("Consolas", 9f),
            BorderStyle = BorderStyle.None,
            ReadOnly = true,
            ScrollBars = RichTextBoxScrollBars.Vertical,
            Padding = new Padding(14, 8, 14, 8),
            WordWrap = false
        };

        WritePreviewLine("Ready. Click 👁 Preview to see a report preview, " +
            "or ▶ Generate Report to save files.",
            PreviewMuted);

        previewCard.Controls.Add(_rtbPreview);
        previewCard.Controls.Add(previewHeader);
        _previewPanel.Controls.Add(previewCard);
    }

    // ── Status Bar ────────────────────────────────────────────────────────────

    private void BuildStatusBar()
    {
        _statusBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(245, 248, 252),
            Padding = new Padding(16, 0, 16, 0),
            Margin = new Padding(0)
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

        _lblLastReport = new Label
        {
            Text = "No reports generated this session.",
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

        statusLayout.Controls.Add(_lblLastReport, 0, 0);
        statusLayout.Controls.Add(_lblStatus, 1, 0);
        _statusBar.Controls.Add(statusLayout);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Data Loading
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadStudentsAsync()
    {
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider
                .GetRequiredService<IStudentService>();
            _students = (await svc.GetAllStudentsAsync()).ToList();

            _cmbStudent.Items.Clear();
            _cmbStudent.Items.Add("— All Students —");
            foreach (var s in _students)
                _cmbStudent.Items.Add(s);
            _cmbStudent.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load students for reports.");
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Preview
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunPreviewAsync()
    {
        await SetLoadingAsync(true, "Generating preview…");
        _rtbPreview.Clear();

        try
        {
            _cts = new CancellationTokenSource();
            var reportData = await Task.Run(
                () => BuildReportDataAsync(_cts.Token),
                _cts.Token);

            _lblPreviewTitle.Text =
                $"●  PREVIEW — {_cmbReportType.SelectedItem}  " +
                $"({DateTime.Now:HH:mm:ss})";

            RenderPreview(reportData);
            SetStatus("Preview generated.", TealAccent);
        }
        catch (OperationCanceledException)
        {
            WritePreviewLine("Preview cancelled.", PreviewMuted);
            SetStatus("Cancelled.", AmberColor);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Preview generation failed.");
            WritePreviewLine($"Error: {ex.Message}", DangerRed);
            SetStatus("Preview failed.", DangerRed);
        }
        finally
        {
            await SetLoadingAsync(false, string.Empty);
            _cts?.Dispose();
            _cts = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Generate (Save Files)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task RunGenerateAsync()
    {
        if (!_chkTxt.Checked && !_chkJson.Checked)
        {
            MessageBox.Show(
                "Please select at least one output format (TXT or JSON).",
                "No Format Selected",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        string outputDir = _txtOutputDir.Text.Trim();
        if (string.IsNullOrWhiteSpace(outputDir) || !Directory.Exists(outputDir))
        {
            MessageBox.Show(
                "The output directory does not exist. Please select a valid folder.",
                "Invalid Directory",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        await SetLoadingAsync(true, "Generating report…");
        _rtbPreview.Clear();
        SetStatus("Generating report files…", TextMuted);

        var filesWritten = new List<string>();

        try
        {
            _cts = new CancellationTokenSource();

            // Build data asynchronously — UI stays responsive
            var reportData = await Task.Run(
                () => BuildReportDataAsync(_cts.Token),
                _cts.Token);

            _cts.Token.ThrowIfCancellationRequested();

            string baseName =
                $"Report_{SanitizeFileName(_cmbReportType.SelectedItem?.ToString() ?? "Report")}" +
                $"_{DateTime.Now:yyyyMMdd_HHmmss}";

            // Write TXT asynchronously
            if (_chkTxt.Checked)
            {
                await UpdateProgressAsync("Writing TXT file…");
                string txtPath = Path.Combine(outputDir, baseName + ".txt");
                string txtContent = await Task.Run(
                    () => BuildTxtReport(reportData),
                    _cts.Token);
                await File.WriteAllTextAsync(txtPath, txtContent, _cts.Token);
                filesWritten.Add(txtPath);
                RenderPreview(reportData);
            }

            _cts.Token.ThrowIfCancellationRequested();

            // Write JSON asynchronously
            if (_chkJson.Checked)
            {
                await UpdateProgressAsync("Writing JSON file…");
                string jsonPath = Path.Combine(outputDir, baseName + ".json");
                string jsonContent = await Task.Run(
                    () => BuildJsonReport(reportData),
                    _cts.Token);
                await File.WriteAllTextAsync(jsonPath, jsonContent, _cts.Token);
                filesWritten.Add(jsonPath);
            }

            // Summary
            _lblLastReport.Text =
                $"Last report: {DateTime.Now:MMM dd, yyyy  HH:mm:ss}  " +
                $"·  {filesWritten.Count} file(s) saved to {ShortenPath(outputDir, 40)}";

            SetStatus($"Report saved — {filesWritten.Count} file(s).", TealAccent);

            WritePreviewLine(string.Empty, PreviewMuted);
            WritePreviewLine("── FILES SAVED ──────────────────────────────", PreviewMuted);
            foreach (var f in filesWritten)
                WritePreviewLine($"  ✅  {f}", TealAccent);

            SerilogLog.Information(
                "Report generated by {User}: {Type} → {Files}",
                SessionManager.Current.FullName,
                _cmbReportType.SelectedItem,
                string.Join(", ", filesWritten));
        }
        catch (OperationCanceledException)
        {
            SetStatus("Report generation cancelled.", AmberColor);
            WritePreviewLine("Generation cancelled by user.", PreviewMuted);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Report generation failed.");
            SetStatus("Report generation failed.", DangerRed);
            WritePreviewLine($"❌ Error: {ex.Message}", DangerRed);
        }
        finally
        {
            await SetLoadingAsync(false, string.Empty);
            _cts?.Dispose();
            _cts = null;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Report Data Builder (runs on background thread via Task.Run)
    // ─────────────────────────────────────────────────────────────────────────

    private async Task<ReportData> BuildReportDataAsync(CancellationToken ct)
    {
        // This method is called inside Task.Run, so we use a fresh DI scope.
        // All async DB calls here are awaited — EF Core handles thread safety
        // at the DbContext level since each scope has its own context instance.

        using var scope = ServiceLocator.CreateScope();
        var studentSvc = scope.ServiceProvider
            .GetRequiredService<IStudentService>();
        var mealSvc = scope.ServiceProvider
            .GetRequiredService<IMealLogService>();
        var analysisSvc = scope.ServiceProvider
            .GetRequiredService<INutritionAnalysisService>();

        ct.ThrowIfCancellationRequested();

        var from = _dtpFrom.Value.Date;
        var to = _dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

        var selectedStudent = _cmbStudent.SelectedItem as StudentDto;

        // Fetch students
        List<StudentDto> students;
        if (selectedStudent != null)
            students = new List<StudentDto> { selectedStudent };
        else
            students = (await studentSvc.GetAllStudentsAsync()).ToList();

        ct.ThrowIfCancellationRequested();

        // Fetch logs
        List<MealLogDto> logs;
        if (selectedStudent != null)
            logs = (await mealSvc.GetLogsByStudentAndDateRangeAsync(
                selectedStudent.Id, from, to)).ToList();
        else
            logs = (await mealSvc.GetLogsByDateRangeAsync(from, to)).ToList();

        ct.ThrowIfCancellationRequested();

        // Fetch analysis
        var analyses = (await analysisSvc.AnalyzeAllStudentsAsync(from, to))
            .ToList();

        if (selectedStudent != null)
            analyses = analyses
                .Where(a => a.StudentId == selectedStudent.Id)
                .ToList();

        ct.ThrowIfCancellationRequested();

        return new ReportData
        {
            ReportType = _cmbReportType.SelectedItem?.ToString() ?? "Report",
            GeneratedAt = DateTime.Now,
            GeneratedBy = SessionManager.IsLoggedIn
                ? SessionManager.Current.FullName : "Unknown",
            PeriodFrom = from,
            PeriodTo = _dtpTo.Value.Date,
            Students = students,
            MealLogs = logs,
            Analyses = analyses
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  TXT Report Builder
    // ─────────────────────────────────────────────────────────────────────────

    private string BuildTxtReport(ReportData data)
    {
        var sb = new StringBuilder();
        string line = new string('═', 72);
        string thin = new string('─', 72);

        sb.AppendLine(line);
        sb.AppendLine($"  NUTRITION MONITOR — {data.ReportType.ToUpperInvariant()}");
        sb.AppendLine(line);
        sb.AppendLine($"  Generated  : {data.GeneratedAt:dddd, MMMM dd, yyyy  HH:mm:ss}");
        sb.AppendLine($"  Generated By: {data.GeneratedBy}");
        sb.AppendLine($"  Period     : {data.PeriodFrom:MMM dd, yyyy}  to  {data.PeriodTo:MMM dd, yyyy}");
        sb.AppendLine($"  Students   : {data.Students.Count}");
        sb.AppendLine($"  Meal Logs  : {data.MealLogs.Count}");
        sb.AppendLine(line);
        sb.AppendLine();

        switch (data.ReportType)
        {
            case "Full Nutrition Summary":
                BuildTxtFullSummary(sb, data, thin);
                break;
            case "Malnutrition Risk Report":
            case "At-Risk & Malnourished Students":
                BuildTxtRiskReport(sb, data, thin);
                break;
            case "Student Meal Log Report":
                BuildTxtMealLogReport(sb, data, thin);
                break;
            case "Nutrient Deficit Analysis":
                BuildTxtDeficitReport(sb, data, thin);
                break;
            default:
                BuildTxtFullSummary(sb, data, thin);
                break;
        }

        sb.AppendLine();
        sb.AppendLine(line);
        sb.AppendLine("  END OF REPORT");
        sb.AppendLine($"  NutritionMonitor v1.0  ·  .NET 10  ·  DOH RENI 2015");
        sb.AppendLine(line);

        return sb.ToString();
    }

    private static void BuildTxtFullSummary(
        StringBuilder sb, ReportData data, string thin)
    {
        sb.AppendLine("  SECTION 1 — STUDENT ROSTER");
        sb.AppendLine(thin);
        sb.AppendLine(
            $"  {"#",-4} {"Student No.",-14} {"Full Name",-26} " +
            $"{"Grade",-8} {"Section",-10} {"Age",-5} {"Gender",-8}");
        sb.AppendLine(thin);

        int i = 1;
        foreach (var s in data.Students)
        {
            sb.AppendLine(
                $"  {i,-4} {s.StudentNumber,-14} {s.FullName,-26} " +
                $"{s.GradeLevel,-8} {s.Section,-10} {s.Age,-5} {s.Gender,-8}");
            i++;
        }

        sb.AppendLine();
        sb.AppendLine("  SECTION 2 — NUTRITION STATUS SUMMARY");
        sb.AppendLine(thin);

        if (data.Analyses.Count == 0)
        {
            sb.AppendLine("  No analysis data available for this period.");
            return;
        }

        int normal = data.Analyses.Count(a => a.Status == NutritionStatus.Normal);
        int atRisk = data.Analyses.Count(a => a.Status == NutritionStatus.AtRisk);
        int mal = data.Analyses.Count(a => a.Status == NutritionStatus.Malnourished);

        sb.AppendLine($"  Normal          : {normal,4}  ({normal * 100.0 / data.Analyses.Count:F1}%)");
        sb.AppendLine($"  At-Risk         : {atRisk,4}  ({atRisk * 100.0 / data.Analyses.Count:F1}%)");
        sb.AppendLine($"  Malnourished    : {mal,4}  ({mal * 100.0 / data.Analyses.Count:F1}%)");
        sb.AppendLine($"  Total Analyzed  : {data.Analyses.Count,4}");

        sb.AppendLine();
        sb.AppendLine("  SECTION 3 — PER-STUDENT STATUS");
        sb.AppendLine(thin);
        sb.AppendLine(
            $"  {"Student",-28} {"Status",-18} {"Deficit %",-12} {"Age",-5}");
        sb.AppendLine(thin);

        foreach (var a in data.Analyses.OrderByDescending(x => x.WeightedDeficitPercentage))
        {
            string status = a.Status switch
            {
                NutritionStatus.Malnourished => "[MALNOURISHED]",
                NutritionStatus.AtRisk => "[AT-RISK]",
                _ => "[NORMAL]"
            };
            sb.AppendLine(
                $"  {a.StudentName,-28} {status,-18} " +
                $"{a.WeightedDeficitPercentage:F1}%{"",-6} {a.Age,-5}");
        }
    }

    private static void BuildTxtRiskReport(
        StringBuilder sb, ReportData data, string thin)
    {
        var flagged = data.Analyses
            .Where(a => a.Status != NutritionStatus.Normal)
            .OrderByDescending(a => a.WeightedDeficitPercentage)
            .ToList();

        sb.AppendLine($"  FLAGGED STUDENTS: {flagged.Count} of {data.Analyses.Count} analyzed");
        sb.AppendLine(thin);

        if (flagged.Count == 0)
        {
            sb.AppendLine("  ✅  No at-risk or malnourished students in this period.");
            return;
        }

        foreach (var a in flagged)
        {
            string status = a.Status == NutritionStatus.Malnourished
                ? "*** MALNOURISHED ***"
                : "** AT-RISK  **";

            sb.AppendLine($"  {status}");
            sb.AppendLine($"  Student  : {a.StudentName}  (Age {a.Age}, {a.Gender})");
            sb.AppendLine($"  Deficit  : {a.WeightedDeficitPercentage:F1}%  avg weighted");

            foreach (var d in a.Deficits.OrderByDescending(x => x.DeficitPercentage))
            {
                string flag = d.DeficitPercentage >= 30 ? " !!!" :
                              d.DeficitPercentage >= 15 ? " !" : "";
                sb.AppendLine(
                    $"    {d.NutrientName,-16} " +
                    $"Actual: {d.ActualValue,8:F1} {d.Unit,-5}  " +
                    $"RENI: {d.RecommendedValue,8:F1} {d.Unit,-5}  " +
                    $"Deficit: {d.DeficitPercentage,6:F1}%{flag}");
            }

            sb.AppendLine(thin);
        }
    }

    private static void BuildTxtMealLogReport(
        StringBuilder sb, ReportData data, string thin)
    {
        sb.AppendLine($"  MEAL LOGS: {data.MealLogs.Count} entries");
        sb.AppendLine(thin);
        sb.AppendLine(
            $"  {"Date",-14} {"Student",-24} {"Meal",-12} " +
            $"{"Cal",-8} {"Prot",-7} {"Carb",-7} {"Fat",-7}");
        sb.AppendLine(thin);

        foreach (var l in data.MealLogs.OrderBy(x => x.LogDate))
        {
            sb.AppendLine(
                $"  {l.LogDate:MMM dd yyyy,-14} {l.StudentName,-24} " +
                $"{l.MealType,-12} " +
                $"{l.CaloriesKcal,7:F0} " +
                $"{l.ProteinG,6:F1}g " +
                $"{l.CarbohydratesG,6:F1}g " +
                $"{l.FatsG,6:F1}g");
        }

        if (data.MealLogs.Count > 0)
        {
            sb.AppendLine(thin);
            sb.AppendLine(
                $"  {"AVERAGES:",-38} " +
                $"{data.MealLogs.Average(l => l.CaloriesKcal),7:F0} " +
                $"{data.MealLogs.Average(l => l.ProteinG),6:F1}g " +
                $"{data.MealLogs.Average(l => l.CarbohydratesG),6:F1}g " +
                $"{data.MealLogs.Average(l => l.FatsG),6:F1}g");
        }
    }

    private static void BuildTxtDeficitReport(
        StringBuilder sb, ReportData data, string thin)
    {
        sb.AppendLine("  NUTRIENT DEFICIT ANALYSIS — DOH RENI 2015 STANDARDS");
        sb.AppendLine(thin);

        if (data.Analyses.Count == 0)
        {
            sb.AppendLine("  No analysis data available for this period.");
            return;
        }

        foreach (var a in data.Analyses.OrderByDescending(x => x.WeightedDeficitPercentage))
        {
            string status = a.Status switch
            {
                NutritionStatus.Malnourished => "MALNOURISHED",
                NutritionStatus.AtRisk => "AT-RISK",
                _ => "NORMAL"
            };

            sb.AppendLine($"  {a.StudentName}  [{status}]  " +
                          $"Avg Weighted Deficit: {a.WeightedDeficitPercentage:F1}%");

            foreach (var d in a.Deficits.OrderByDescending(x => x.DeficitPercentage))
            {
                string bar = BuildTextBar(d.DeficitPercentage, 20);
                sb.AppendLine(
                    $"    {d.NutrientName,-14} [{bar}] {d.DeficitPercentage,6:F1}%  " +
                    $"({d.ActualValue:F1}/{d.RecommendedValue:F1} {d.Unit})");
            }

            sb.AppendLine();
        }
    }

    private static string BuildTextBar(double pct, int width)
    {
        int filled = (int)Math.Round(Math.Min(pct, 100) / 100.0 * width);
        return new string('█', filled) + new string('░', width - filled);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  JSON Report Builder
    // ─────────────────────────────────────────────────────────────────────────

    private static string BuildJsonReport(ReportData data)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        var payload = new
        {
            reportType = data.ReportType,
            generatedAt = data.GeneratedAt,
            generatedBy = data.GeneratedBy,
            period = new { from = data.PeriodFrom, to = data.PeriodTo },
            summary = new
            {
                totalStudents = data.Students.Count,
                totalMealLogs = data.MealLogs.Count,
                totalAnalyzed = data.Analyses.Count,
                normalCount = data.Analyses.Count(
                    a => a.Status == NutritionStatus.Normal),
                atRiskCount = data.Analyses.Count(
                    a => a.Status == NutritionStatus.AtRisk),
                malnourishedCount = data.Analyses.Count(
                    a => a.Status == NutritionStatus.Malnourished),
                averageDeficitPct = data.Analyses.Count > 0
                    ? data.Analyses.Average(a => a.WeightedDeficitPercentage)
                    : 0.0
            },
            students = data.Students,
            mealLogs = data.MealLogs.Select(l => new
            {
                l.Id,
                l.StudentId,
                l.StudentName,
                logDate = l.LogDate.ToString("yyyy-MM-dd"),
                l.MealType,
                l.CaloriesKcal,
                l.ProteinG,
                l.CarbohydratesG,
                l.FatsG,
                l.FiberG,
                l.VitaminAMcg,
                l.VitaminCMg,
                l.VitaminDMcg,
                l.CalciumMg,
                l.IronMg,
                l.ZincMg,
                l.Notes
            }),
            analyses = data.Analyses.Select(a => new
            {
                a.StudentId,
                a.StudentName,
                a.Age,
                gender = a.Gender.ToString(),
                status = a.Status.ToString(),
                a.WeightedDeficitPercentage,
                averages = new
                {
                    a.AvgCalories,
                    a.AvgProtein,
                    a.AvgCarbohydrates,
                    a.AvgFats,
                    a.AvgFiber,
                    a.AvgVitaminA,
                    a.AvgVitaminC,
                    a.AvgVitaminD,
                    a.AvgCalcium,
                    a.AvgIron,
                    a.AvgZinc
                },
                deficits = a.Deficits.Select(d => new
                {
                    d.NutrientName,
                    d.ActualValue,
                    d.RecommendedValue,
                    d.DeficitPercentage,
                    d.Unit
                })
            })
        };

        return JsonSerializer.Serialize(payload, options);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Preview Renderer
    // ─────────────────────────────────────────────────────────────────────────

    private void RenderPreview(ReportData data)
    {
        _rtbPreview.Clear();

        // Header
        WritePreviewLine(new string('═', 68), PreviewMuted);
        WritePreviewLine(
            $"  {data.ReportType.ToUpperInvariant()}",
            TealAccent);
        WritePreviewLine(new string('═', 68), PreviewMuted);
        WritePreviewLine(
            $"  Generated : {data.GeneratedAt:MMM dd, yyyy  HH:mm:ss}  " +
            $"by {data.GeneratedBy}", PreviewMuted);
        WritePreviewLine(
            $"  Period    : {data.PeriodFrom:MMM dd} – {data.PeriodTo:MMM dd, yyyy}  " +
            $"·  {data.Students.Count} students  ·  {data.MealLogs.Count} logs",
            PreviewMuted);
        WritePreviewLine(new string('─', 68), PreviewMuted);
        WritePreviewLine(string.Empty, PreviewMuted);

        // Status summary
        if (data.Analyses.Count > 0)
        {
            int normal = data.Analyses.Count(a => a.Status == NutritionStatus.Normal);
            int atRisk = data.Analyses.Count(a => a.Status == NutritionStatus.AtRisk);
            int mal = data.Analyses.Count(a => a.Status == NutritionStatus.Malnourished);

            WritePreviewLine("  STATUS SUMMARY", PreviewMuted);
            WritePreviewLine(
                $"  ✅  Normal       : {normal}", Color.FromArgb(0, 200, 150));
            WritePreviewLine(
                $"  ⚠   At-Risk      : {atRisk}", AmberColor);
            WritePreviewLine(
                $"  🔴  Malnourished : {mal}", DangerRed);
            WritePreviewLine(string.Empty, PreviewMuted);
        }

        // Top at-risk
        var flagged = data.Analyses
            .Where(a => a.Status != NutritionStatus.Normal)
            .OrderByDescending(a => a.WeightedDeficitPercentage)
            .Take(5)
            .ToList();

        if (flagged.Count > 0)
        {
            WritePreviewLine("  TOP FLAGGED STUDENTS", PreviewMuted);
            WritePreviewLine(new string('─', 68), PreviewMuted);
            foreach (var a in flagged)
            {
                Color c = a.Status == NutritionStatus.Malnourished
                    ? DangerRed : AmberColor;
                WritePreviewLine(
                    $"  {a.StudentName,-28} " +
                    $"{a.Status,-16} " +
                    $"Deficit: {a.WeightedDeficitPercentage:F1}%", c);
            }
            WritePreviewLine(string.Empty, PreviewMuted);
        }

        WritePreviewLine(new string('─', 68), PreviewMuted);
        WritePreviewLine(
            $"  Full report contains {data.MealLogs.Count} meal log entries " +
            $"across {data.Students.Count} students.",
            PreviewMuted);
        WritePreviewLine(
            "  Use ▶ Generate Report to save complete TXT / JSON files.",
            PreviewMuted);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void WritePreviewLine(string text, Color color)
    {
        _rtbPreview.SuspendLayout();
        _rtbPreview.SelectionStart = _rtbPreview.TextLength;
        _rtbPreview.SelectionLength = 0;
        _rtbPreview.SelectionColor = color;
        _rtbPreview.AppendText(text + "\n");
        _rtbPreview.ResumeLayout();
        _rtbPreview.ScrollToCaret();
    }

    private async Task SetLoadingAsync(bool loading, string progressText)
    {
        _btnGenerate.Enabled = !loading;
        _btnPreview.Enabled = !loading;
        _progressBar.Visible = loading;
        _lblProgress.Text = progressText;
        _btnGenerate.Text = loading ? "Generating…" : "▶  Generate Report";
        _btnPreview.Text = loading ? "Working…" : "👁  Preview";
        await Task.Yield();
    }

    private async Task UpdateProgressAsync(string message)
    {
        _lblProgress.Text = message;
        await Task.Yield();
    }

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
    }

    private void BrowseOutputDir(object? sender, EventArgs e)
    {
        using var dlg = new FolderBrowserDialog
        {
            Description = "Select output folder for report files",
            SelectedPath = _txtOutputDir.Text,
            UseDescriptionForTitle = true
        };

        if (dlg.ShowDialog() == DialogResult.OK)
            _txtOutputDir.Text = dlg.SelectedPath;
    }

    private static void PaintCard(Graphics g, Panel card, Color accent)
    {
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        using var bar = new SolidBrush(accent);
        g.FillRectangle(bar, 0, 0, card.Width, 4);
        using var pen = new Pen(Color.FromArgb(225, 232, 242), 1);
        g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
    }

    private static Button MakeButton(
        string text, Color bg, Color fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 36),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
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

    private static string SanitizeFileName(string name)
        => string.Concat(name.Split(Path.GetInvalidFileNameChars()))
            .Replace(" ", "_");

    private static string ShortenPath(string path, int maxLen)
    {
        if (path.Length <= maxLen) return path;
        var parts = path.Split(Path.DirectorySeparatorChar);
        return parts.Length <= 2
            ? path
            : $"{parts[0]}{Path.DirectorySeparatorChar}…" +
              $"{Path.DirectorySeparatorChar}{parts[^1]}";
    }
}

// ─────────────────────────────────────────────────────────────────────────────
//  Report Data Transfer Object (internal to this form)
// ─────────────────────────────────────────────────────────────────────────────

internal sealed class ReportData
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedAt { get; set; }
    public string GeneratedBy { get; set; } = string.Empty;
    public DateTime PeriodFrom { get; set; }
    public DateTime PeriodTo { get; set; }
    public List<StudentDto> Students { get; set; } = new();
    public List<MealLogDto> MealLogs { get; set; } = new();
    public List<NutritionAnalysisDto> Analyses { get; set; } = new();
}