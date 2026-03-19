using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using System.Globalization;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Intrinsics.X86;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.Analysis;

public class NutritionAnalysisForm : UserControl
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly Color BgColor = Color.FromArgb(243, 246, 250);
    private static readonly Color CardBg = Color.White;
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 150);
    private static readonly Color TealLight = Color.FromArgb(225, 248, 245);
    private static readonly Color TealHover = Color.FromArgb(0, 148, 132);
    private static readonly Color TextDark = Color.FromArgb(22, 32, 50);
    private static readonly Color TextMid = Color.FromArgb(80, 100, 130);
    private static readonly Color TextMuted = Color.FromArgb(140, 160, 185);
    private static readonly Color BorderLight = Color.FromArgb(225, 232, 242);
    private static readonly Color DangerRed = Color.FromArgb(220, 60, 60);
    private static readonly Color DangerLight = Color.FromArgb(254, 226, 226);
    private static readonly Color AmberColor = Color.FromArgb(245, 158, 11);
    private static readonly Color AmberLight = Color.FromArgb(254, 243, 199);
    private static readonly Color NormalGreen = Color.FromArgb(0, 168, 150);
    private static readonly Color NormalLight = Color.FromArgb(209, 250, 229);
    private static readonly Color GridHeader = Color.FromArgb(245, 248, 252);
    private static readonly Color GridAlt = Color.FromArgb(250, 252, 255);
    private static readonly Color GridSelect = Color.FromArgb(209, 246, 241);

    // ── Layout panels ─────────────────────────────────────────────────────────
    private Panel _filterPanel = null!;
    private Panel _summaryStrip = null!;
    private SplitContainer _splitMain = null!;
    private Panel _statusBar = null!;

    // ── Filter controls ───────────────────────────────────────────────────────
    private System.Windows.Forms.ComboBox _cmbStudent = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private System.Windows.Forms.Button _btnAnalyze = null!;
    private System.Windows.Forms.Button _btnAnalyzeAll = null!;
    private System.Windows.Forms.Button _btnClear = null!;

    // ── Left pane — student list ──────────────────────────────────────────────
    private DataGridView _gridStudents = null!;

    // ── Right pane — detail ───────────────────────────────────────────────────
    private Panel _detailPanel = null!;

    // ── Summary strip labels ──────────────────────────────────────────────────
    private Label _lblTotalAnalyzed = null!;
    private Label _lblNormalCount = null!;
    private Label _lblAtRiskCount = null!;
    private Label _lblMalCount = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Label _lblStatus = null!;
    private Label _lblCount = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<StudentDto> _students = new();
    private List<NutritionAnalysisDto> _results = new();
    private readonly Panel _parentContentArea;

    // ─────────────────────────────────────────────────────────────────────────
    public NutritionAnalysisForm(Panel parentContentArea)
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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            BackColor = BgColor,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));  // filter
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 54f));  // summary strip
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // split pane
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // status bar

        BuildFilterPanel();
        BuildSummaryStrip();
        BuildSplitPane();
        BuildStatusBar();

        layout.Controls.Add(_filterPanel, 0, 0);
        layout.Controls.Add(_summaryStrip, 0, 1);
        layout.Controls.Add(_splitMain, 0, 2);
        layout.Controls.Add(_statusBar, 0, 3);

        Controls.Add(layout);
    }

    // ── Filter Panel ──────────────────────────────────────────────────────────

    private void BuildFilterPanel()
    {
        _filterPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg
        };

        _filterPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _filterPanel.Height - 1,
                _filterPanel.Width, _filterPanel.Height - 1);
        };

        // Use FlowLayoutPanel so items wrap gracefully
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(12, 10, 12, 8)
        };

        // ── Student combo ──────────────────────────────────────────────────────
        var studentBlock = MakeFlowBlock("STUDENT", out var studentInner);
        _cmbStudent = new System.Windows.Forms.ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(200, 30),
            BackColor = CardBg,
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            DisplayMember = "FullName"
        };
        studentInner.Controls.Add(_cmbStudent);

        // ── From date ──────────────────────────────────────────────────────────
        var fromBlock = MakeFlowBlock("FROM", out var fromInner);
        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddDays(-30),
            Size = new Size(120, 30)
        };
        fromInner.Controls.Add(_dtpFrom);

        // ── To date ────────────────────────────────────────────────────────────
        var toBlock = MakeFlowBlock("TO", out var toInner);
        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today,
            Size = new Size(120, 30)
        };
        toInner.Controls.Add(_dtpTo);

        // ── Spacer ─────────────────────────────────────────────────────────────
        var spacer = new Panel
        {
            Size = new Size(12, 40),
            BackColor = Color.Transparent
        };

        // ── Buttons ────────────────────────────────────────────────────────────
        var btnBlock = new Panel
        {
            Size = new Size(420, 44),
            BackColor = Color.Transparent
        };

        _btnAnalyze = MakeButton("▶  Analyze Student",
            TealAccent, Color.White, new Point(0, 6), 160);

        _btnAnalyzeAll = MakeButton("▶▶  Analyze All",
            Color.FromArgb(40, 60, 100), Color.White, new Point(168, 6), 140);

        _btnClear = MakeButton("✕  Clear",
            Color.FromArgb(240, 244, 248), TextMid, new Point(316, 6), 80);
        _btnClear.FlatAppearance.BorderSize = 1;
        _btnClear.FlatAppearance.BorderColor = BorderLight;

        btnBlock.Controls.AddRange(new Control[]
        {
        _btnAnalyze, _btnAnalyzeAll, _btnClear
        });

        flow.Controls.AddRange(new Control[]
        {
        studentBlock, fromBlock, toBlock, spacer, btnBlock
        });

        _filterPanel.Controls.Add(flow);

        _btnAnalyze.Click += async (_, _) => await AnalyzeStudentAsync();
        _btnAnalyzeAll.Click += async (_, _) => await AnalyzeAllAsync();
        _btnClear.Click += ClearResults;
    }

    // ── Helper: labeled flow block ─────────────────────────────────────────────
    private static Panel MakeFlowBlock(string labelText, out Panel innerPanel)
    {
        var outer = new Panel
        {
            Size = new Size(0, 44),  // width auto from content
            BackColor = Color.Transparent,
            Padding = new Padding(0, 0, 14, 0),
            AutoSize = true
        };

        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(0, 0),
            Height = 16
        };

        innerPanel = new Panel
        {
            Location = new Point(0, 18),
            AutoSize = true,
            BackColor = Color.Transparent
        };

        outer.Controls.Add(lbl);
        outer.Controls.Add(innerPanel);
        return outer;
    }

    // ── Summary Strip ─────────────────────────────────────────────────────────

    private void BuildSummaryStrip()
    {
        _summaryStrip = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(22, 32, 50),
            Padding = new Padding(16, 0, 16, 0)
        };

        _summaryStrip.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(40, 60, 90), 1);
            e.Graphics.DrawLine(pen, 0, _summaryStrip.Height - 1,
                _summaryStrip.Width, _summaryStrip.Height - 1);
        };

        // Items: icon, label, value label ref, color
        _lblTotalAnalyzed = MakeSummaryValue("—");
        _lblNormalCount = MakeSummaryValue("—");
        _lblAtRiskCount = MakeSummaryValue("—");
        _lblMalCount = MakeSummaryValue("—");

        var items = new[]
        {
            ("📋", "Total Analyzed",  _lblTotalAnalyzed, Color.FromArgb(150, 180, 220)),
            ("✅", "Normal",          _lblNormalCount,   NormalGreen),
            ("⚠",  "At-Risk",         _lblAtRiskCount,   AmberColor),
            ("🔴", "Malnourished",    _lblMalCount,      DangerRed),
        };

        int x = 0;
        foreach (var (icon, title, valLbl, color) in items)
        {
            var iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 13f),
                ForeColor = color,
                AutoSize = false,
                Size = new Size(28, 54),
                Location = new Point(x, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            var titleLbl = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 130, 170),
                AutoSize = false,
                Size = new Size(130, 18),
                Location = new Point(x + 32, 8),
                TextAlign = ContentAlignment.BottomLeft
            };

            valLbl.Location = new Point(x + 32, 28);
            valLbl.ForeColor = color;

            _summaryStrip.Controls.Add(iconLbl);
            _summaryStrip.Controls.Add(titleLbl);
            _summaryStrip.Controls.Add(valLbl);

            x += 200;
        }
    }

    // ── Split Pane ────────────────────────────────────────────────────────────

    private void BuildSplitPane()
    {
        _splitMain = new SplitContainer
        {
            Dock = DockStyle.Fill,
            Orientation = Orientation.Vertical,
            SplitterWidth = 6,
            BackColor = BorderLight
            // ── NOTHING size-related here ──────────────────────────────────────
            // Never set Panel1MinSize, Panel2MinSize, or SplitterDistance
            // in the constructor. WinForms calls ApplyPanel2MinSize internally
            // when Panel2MinSize is assigned, which triggers set_SplitterDistance
            // while the control still has Width = 0, causing the crash.
        };

        _splitMain.Panel1.BackColor = BgColor;
        _splitMain.Panel2.BackColor = BgColor;

        // HandleCreated fires after the HWND exists and layout has run.
        // SizeChanged keeps the ratio correct when the user resizes the window.
        _splitMain.HandleCreated += (_, _) => SetSplitterSafe();
        _splitMain.SizeChanged += (_, _) => SetSplitterSafe();

        BuildStudentGrid();
        BuildDetailPanel();
    }

    private void SetSplitterSafe()
    {
        // Guard: handle must exist and control must have real width
        if (!_splitMain.IsHandleCreated) return;
        if (_splitMain.Width <= 1) return;

        try
        {
            // Set min sizes first — but only now that Width > 0
            _splitMain.Panel1MinSize = 240;
            _splitMain.Panel2MinSize = 300;

            int desired = (int)(_splitMain.Width * 0.38);
            int min = _splitMain.Panel1MinSize;
            int max = _splitMain.Width
                          - _splitMain.Panel2MinSize
                          - _splitMain.SplitterWidth;

            if (max > min)
                _splitMain.SplitterDistance = Math.Clamp(desired, min, max);
        }
        catch (InvalidOperationException)
        {
            // Swallow any residual layout-transition edge cases silently
        }
    }


    // ── Student Results Grid (left pane) ──────────────────────────────────────

    private void BuildStudentGrid()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = CardBg,
            Padding = new Padding(12, 0, 12, 0)
        };

        header.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, header.Height - 1,
                header.Width, header.Height - 1);
        };

        var lblHeader = new Label
        {
            Text = "ANALYSIS RESULTS",
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            ForeColor = TextMid,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        };
        header.Controls.Add(lblHeader);

        _gridStudents = new DataGridView
        {
            Dock = DockStyle.Fill,
            BackgroundColor = CardBg,
            BorderStyle = BorderStyle.None,
            CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
            ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            RowHeadersVisible = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            Font = new Font("Segoe UI", 9.5f),
            GridColor = BorderLight,
            RowTemplate = { Height = 40 }
        };

        _gridStudents.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridHeader,
            ForeColor = TextMid,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _gridStudents.ColumnHeadersHeight = 36;
        _gridStudents.EnableHeadersVisualStyles = false;

        _gridStudents.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardBg,
            ForeColor = TextDark,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0)
        };

        _gridStudents.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridAlt,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0)
        };

        _gridStudents.Columns.AddRange(
            MakeCol("StudentName", "Student", 120),
            MakeCol("Status", "Status", 90),
            MakeCol("WeightedDeficit", "Deficit %", 80),
            MakeCol("Age", "Age", 45)
        );

        _gridStudents.Columns["StudentName"]!.FillWeight = 140;
        _gridStudents.Columns["Status"]!.FillWeight = 90;
        _gridStudents.Columns["WeightedDeficit"]!.FillWeight = 80;
        _gridStudents.Columns["Age"]!.FillWeight = 45;

        _gridStudents.SelectionChanged += GridStudents_SelectionChanged;
        _gridStudents.CellFormatting += GridStudents_CellFormatting;

        var gridContainer = new Panel { Dock = DockStyle.Fill };
        gridContainer.Controls.Add(_gridStudents);
        gridContainer.Controls.Add(header);

        _splitMain.Panel1.Controls.Add(gridContainer);
    }

    // ── Detail Panel (right pane) ─────────────────────────────────────────────

    private void BuildDetailPanel()
    {
        _detailPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            AutoScroll = true
        };

        ShowDetailPlaceholder();
        _splitMain.Panel2.Controls.Add(_detailPanel);
    }

    private void ShowDetailPlaceholder()
    {
        _detailPanel.Controls.Clear();

        var card = new Panel
        {
            Size = new Size(380, 160),
            BackColor = CardBg
        };

        card.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, card.Width, 3);
        };

        var lbl = new Label
        {
            Text = "📊\n\nSelect a student from the results list\nor run an analysis to see detailed\nnutrient deficit information here.",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter
        };

        card.Controls.Add(lbl);

        _detailPanel.Controls.Add(card);
        _detailPanel.Resize += (_, _) =>
        {
            card.Location = new Point(
                Math.Max(0, (_detailPanel.Width - card.Width) / 2),
                Math.Max(0, (_detailPanel.Height - card.Height) / 2 - 20));
        };
        card.Location = new Point(
            Math.Max(0, (_detailPanel.Width - card.Width) / 2),
            Math.Max(0, (_detailPanel.Height - card.Height) / 2 - 20));
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

        _lblCount = new Label
        {
            Text = "No analysis run yet.",
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

        _statusBar.Controls.AddRange(new Control[] { _lblCount, _lblStatus });
        _statusBar.Resize += (_, _) =>
            _lblStatus.Location = new Point(
                _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Data Operations
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadStudentsAsync()
    {
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IStudentService>();
            _students = (await svc.GetAllStudentsAsync()).ToList();

            _cmbStudent.Items.Clear();
            _cmbStudent.Items.Add("— Select a student —");
            foreach (var s in _students)
                _cmbStudent.Items.Add(s);
            _cmbStudent.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load students for analysis.");
        }
    }

    private async Task AnalyzeStudentAsync()
    {
        if (_cmbStudent.SelectedItem is not StudentDto student)
        {
            SetStatus("Please select a student first.", AmberColor);
            return;
        }

        SetStatus("Analyzing…", TextMuted);
        SetLoading(true);

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider
                .GetRequiredService<INutritionAnalysisService>();

            var result = await svc.AnalyzeStudentAsync(
                student.Id,
                _dtpFrom.Value.Date,
                _dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

            if (result == null)
            {
                SetStatus(
                    $"No meal logs found for {student.FullName} in the selected period.",
                    AmberColor);
                return;
            }

            _results = new List<NutritionAnalysisDto> { result };
            BindResultsGrid(_results);
            UpdateSummaryStrip(_results);
            ShowDetailView(result);
            SetStatus($"Analysis complete for {student.FullName}.", TealAccent);
            _lblCount.Text = "1 student analyzed";

            SerilogLog.Information(
                "Analysis complete: {Name} — Status: {Status}, Deficit: {Deficit:F1}%",
                result.StudentName, result.Status, result.WeightedDeficitPercentage);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Analysis failed.");
            SetStatus("Analysis failed. Check logs for details.", DangerRed);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private async Task AnalyzeAllAsync()
    {
        SetStatus("Analyzing all students…", TextMuted);
        SetLoading(true);

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider
                .GetRequiredService<INutritionAnalysisService>();

            var results = await svc.AnalyzeAllStudentsAsync(
                _dtpFrom.Value.Date,
                _dtpTo.Value.Date.AddDays(1).AddSeconds(-1));

            _results = results.ToList();

            if (_results.Count == 0)
            {
                SetStatus("No meal logs found in the selected period.", AmberColor);
                _lblCount.Text = "0 students analyzed";
                return;
            }

            BindResultsGrid(_results);
            UpdateSummaryStrip(_results);
            ShowDetailPlaceholder();
            SetStatus($"Analysis complete. {_results.Count} students analyzed.", TealAccent);
            _lblCount.Text = $"{_results.Count} students analyzed";

            SerilogLog.Information(
                "Batch analysis complete. {Count} students.", _results.Count);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Batch analysis failed.");
            SetStatus("Batch analysis failed. Check logs for details.", DangerRed);
        }
        finally
        {
            SetLoading(false);
        }
    }

    private void ClearResults(object? sender, EventArgs e)
    {
        _results = new List<NutritionAnalysisDto>();
        _gridStudents.Rows.Clear();
        ShowDetailPlaceholder();
        UpdateSummaryStrip(_results);
        SetStatus(string.Empty, TealAccent);
        _lblCount.Text = "No analysis run yet.";
        _cmbStudent.SelectedIndex = 0;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Grid Binding
    // ─────────────────────────────────────────────────────────────────────────

    private void BindResultsGrid(List<NutritionAnalysisDto> results)
    {
        _gridStudents.Rows.Clear();

        // Sort: Malnourished first, then At-Risk, then Normal
        var sorted = results
            .OrderByDescending(r => (int)r.Status)
            .ThenByDescending(r => r.WeightedDeficitPercentage)
            .ToList();

        foreach (var r in sorted)
        {
            int row = _gridStudents.Rows.Add(
                r.StudentName,
                StatusLabel(r.Status),
                $"{r.WeightedDeficitPercentage:F1}%",
                r.Age.ToString()
            );
            _gridStudents.Rows[row].Tag = r;
        }
    }

    private void GridStudents_SelectionChanged(object? sender, EventArgs e)
    {
        if (_gridStudents.SelectedRows.Count == 0) return;
        if (_gridStudents.SelectedRows[0].Tag is NutritionAnalysisDto result)
            ShowDetailView(result);
    }

    private void GridStudents_CellFormatting(
        object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (_gridStudents.Columns[e.ColumnIndex].Name != "Status") return;
        if (e.Value == null) return;

        var text = e.Value.ToString();
        e.CellStyle.ForeColor = text switch
        {
            "🔴 Malnourished" => DangerRed,
            "⚠  At-Risk" => AmberColor,
            "✅ Normal" => NormalGreen,
            _ => TextDark
        };
        e.CellStyle.Font = new Font("Segoe UI", 9f, FontStyle.Bold);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Detail View
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowDetailView(NutritionAnalysisDto result)
    {
        _detailPanel.Controls.Clear();

        int y = 16;
        int padX = 16;
        int width = _detailPanel.Width - padX * 2 - 20;
        width = Math.Max(300, width);

        // ── Student header card ───────────────────────────────────────────────
        var headerCard = BuildDetailCard(padX, y, width, 90);
        headerCard.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var statusColor = result.Status switch
            {
                NutritionStatus.Malnourished => DangerRed,
                NutritionStatus.AtRisk => AmberColor,
                _ => NormalGreen
            };

            // Left status bar
            using var bar = new SolidBrush(statusColor);
            g.FillRectangle(bar, 0, 0, 5, headerCard.Height);

            using var pen = new Pen(BorderLight, 1);
            g.DrawRectangle(pen, 0, 0, headerCard.Width - 1, headerCard.Height - 1);
        };

        var lblName = new Label
        {
            Text = result.StudentName,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(width - 130, 28),
            Location = new Point(16, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblMeta = new Label
        {
            Text = $"Age {result.Age}  ·  {result.Gender}  ·  " +
                        $"{result.Period.From:MMM dd} – {result.Period.To:MMM dd, yyyy}",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(width - 130, 20),
            Location = new Point(16, 40),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Status badge
        var statusColor2 = result.Status switch
        {
            NutritionStatus.Malnourished => DangerRed,
            NutritionStatus.AtRisk => AmberColor,
            _ => NormalGreen
        };
        var statusBg = result.Status switch
        {
            NutritionStatus.Malnourished => DangerLight,
            NutritionStatus.AtRisk => AmberLight,
            _ => NormalLight
        };

        var lblStatusBadge = new Label
        {
            Text = StatusLabel(result.Status),
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = statusColor2,
            BackColor = statusBg,
            AutoSize = false,
            Size = new Size(130, 30),
            Location = new Point(width - 140, 20),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblDeficit = new Label
        {
            Text = $"Weighted Deficit: {result.WeightedDeficitPercentage:F1}%",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = statusColor2,
            AutoSize = false,
            Size = new Size(width - 130, 18),
            Location = new Point(16, 64),
            TextAlign = ContentAlignment.MiddleLeft
        };

        headerCard.Controls.AddRange(new Control[]
        {
            lblName, lblMeta, lblStatusBadge, lblDeficit
        });
        _detailPanel.Controls.Add(headerCard);
        y += 100;

        // ── Averages card ─────────────────────────────────────────────────────
        var avgCard = BuildDetailCard(padX, y, width, 120);
        avgCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, avgCard.Width - 1, avgCard.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, avgCard.Width, 3);
        };

        var lblAvgTitle = new Label
        {
            Text = "PERIOD AVERAGES",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(width - 20, 20),
            Location = new Point(12, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };
        avgCard.Controls.Add(lblAvgTitle);

        var avgItems = new[]
        {
            ($"🔥 {result.AvgCalories:F0} kcal",   "Calories"),
            ($"💪 {result.AvgProtein:F1}g",         "Protein"),
            ($"🌾 {result.AvgCarbohydrates:F1}g",   "Carbs"),
            ($"🧈 {result.AvgFats:F1}g",            "Fats"),
            ($"🥦 {result.AvgFiber:F1}g",           "Fiber"),
        };

        int ax = 12;
        int colW = (width - 24) / 5;
        foreach (var (val, lbl) in avgItems)
        {
            avgCard.Controls.Add(new Label
            {
                Text = val,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = TealAccent,
                AutoSize = false,
                Size = new Size(colW, 22),
                Location = new Point(ax, 36),
                TextAlign = ContentAlignment.MiddleLeft
            });
            avgCard.Controls.Add(new Label
            {
                Text = lbl,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                AutoSize = false,
                Size = new Size(colW, 16),
                Location = new Point(ax, 60),
                TextAlign = ContentAlignment.MiddleLeft
            });
            ax += colW;
        }

        _detailPanel.Controls.Add(avgCard);
        y += 130;

        // ── Deficit breakdown card ────────────────────────────────────────────
        if (result.Deficits.Count > 0)
        {
            var defTitle = new Label
            {
                Text = "NUTRIENT DEFICIT BREAKDOWN",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMid,
                AutoSize = false,
                Size = new Size(width, 20),
                Location = new Point(padX, y),
                TextAlign = ContentAlignment.BottomLeft
            };
            _detailPanel.Controls.Add(defTitle);
            y += 24;

            foreach (var deficit in result.Deficits
                .OrderByDescending(d => d.DeficitPercentage))
            {
                var row = BuildDeficitRow(
                    padX, y, width, deficit);
                _detailPanel.Controls.Add(row);
                y += row.Height + 6;
            }
        }

        // ── Reni reference note ───────────────────────────────────────────────
        y += 8;
        var refNote = new Label
        {
            Text = "※  Reference values based on DOH Philippine RENI 2015 standards.",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(width, 18),
            Location = new Point(padX, y),
            TextAlign = ContentAlignment.MiddleLeft
        };
        _detailPanel.Controls.Add(refNote);

        // Resize handler to reflow detail width
        _detailPanel.Resize += (_, _) => ReflowDetail(result);
    }

    private Panel BuildDeficitRow(
        int x, int y, int width, NutrientDeficitDetail deficit)
    {
        var row = new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, 52),
            BackColor = CardBg
        };

        row.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, row.Width - 1, row.Height - 1);
        };

        // Status color by deficit amount
        Color defColor = deficit.DeficitPercentage switch
        {
            >= 30 => DangerRed,
            >= 15 => AmberColor,
            _ => NormalGreen
        };

        // Left color bar
        row.Paint += (s, e) =>
        {
            using var bar = new SolidBrush(defColor);
            e.Graphics.FillRectangle(bar, 0, 0, 4, row.Height);
        };

        var lblName = new Label
        {
            Text = deficit.NutrientName,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(120, 18),
            Location = new Point(12, 8),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblActual = new Label
        {
            Text = $"Actual: {deficit.ActualValue:F1} {deficit.Unit}",
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextMid,
            AutoSize = false,
            Size = new Size(160, 16),
            Location = new Point(12, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblRecommended = new Label
        {
            Text = $"RENI: {deficit.RecommendedValue:F1} {deficit.Unit}",
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(160, 16),
            Location = new Point(160, 28),
            TextAlign = ContentAlignment.MiddleLeft
        };

        // Deficit percentage badge
        var lblPct = new Label
        {
            Text = $"{deficit.DeficitPercentage:F1}%",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = defColor,
            AutoSize = false,
            Size = new Size(70, 52),
            Location = new Point(row.Width - 80, 0),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Progress bar track
        var trackW = row.Width - 290;
        var barW = Math.Max(0, (int)(trackW * (deficit.DeficitPercentage / 100.0)));
        barW = Math.Min(barW, trackW);

        var track = new Panel
        {
            Location = new Point(330, 20),
            Size = new Size(trackW, 8),
            BackColor = Color.FromArgb(230, 235, 242)
        };

        var fill = new Panel
        {
            Location = new Point(0, 0),
            Size = new Size(barW, 8),
            BackColor = defColor
        };
        track.Controls.Add(fill);

        row.Controls.AddRange(new Control[]
        {
            lblName, lblActual, lblRecommended, track, lblPct
        });

        return row;
    }

    private void ReflowDetail(NutritionAnalysisDto result)
    {
        // Re-render detail on resize for correct widths
        ShowDetailView(result);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Summary Strip
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateSummaryStrip(List<NutritionAnalysisDto> results)
    {
        _lblTotalAnalyzed.Text = results.Count.ToString();
        _lblNormalCount.Text = results.Count(r => r.Status == NutritionStatus.Normal).ToString();
        _lblAtRiskCount.Text = results.Count(r => r.Status == NutritionStatus.AtRisk).ToString();
        _lblMalCount.Text = results.Count(r => r.Status == NutritionStatus.Malnourished).ToString();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static string StatusLabel(NutritionStatus status) => status switch
    {
        NutritionStatus.Malnourished => "🔴 Malnourished",
        NutritionStatus.AtRisk => "⚠  At-Risk",
        _ => "✅ Normal"
    };

    private void SetLoading(bool loading)
    {
        _btnAnalyze.Enabled = !loading;
        _btnAnalyzeAll.Enabled = !loading;
        _btnAnalyze.Text = loading ? "Analyzing…" : "▶  Analyze Student";
        _btnAnalyzeAll.Text = loading ? "Please wait…" : "▶▶  Analyze All";
        Application.DoEvents();
    }

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    private static Panel BuildDetailCard(int x, int y, int width, int height)
    {
        return new Panel
        {
            Location = new Point(x, y),
            Size = new Size(width, height),
            BackColor = CardBg
        };
    }

    private static Label MakeSummaryValue(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 14f, FontStyle.Bold),
        ForeColor = Color.White,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Label MakeFilterLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Height = 32
    };

    private static System.Windows.Forms.Button MakeButton(
        string text, Color bg, Color fg, Point loc, int width)
    {
        var btn = new System.Windows.Forms.Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Location = loc,
            Size = new Size(width, 36),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor =
            Color.FromArgb(
                Math.Max(0, bg.R - 15),
                Math.Max(0, bg.G - 15),
                Math.Max(0, bg.B - 15));
        btn.MouseLeave += (_, _) => btn.BackColor = bg;
        return btn;
    }

    private static DataGridViewTextBoxColumn MakeCol(
        string name, string header, int minWidth)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = name,
            MinimumWidth = minWidth,
            ReadOnly = true,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }
}

