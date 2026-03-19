// PHASE 6 FIX — NutritionAnalysisForm.cs
// Changes made — junk using directives removed:
//
//   [FIX #1] Removed: using System.Runtime.ConstrainedExecution;
//            → This namespace contains SafeHandle and CriticalFinalizerObject,
//              which are used in low-level memory-safe unmanaged code.
//              A nutrition monitoring form has absolutely no use for this.
//
//   [FIX #2] Removed: using System.Runtime.Intrinsics.X86;
//            → This is for CPU-level SIMD vector instructions (SSE, AVX).
//              Used in high-performance numerical computing. Not for WinForms.
//
//   [FIX #3] Removed: using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
//            → EF Core internal logging categories. The UI layer never
//              interacts with EF Core directly. This belongs in DAL only.
//
//   [FIX #4] Removed: using static System.Windows.Forms.VisualStyles.VisualStyleElement;
//            → Provides static access to Windows visual theme element identifiers
//              like VisualStyleElement.Button.PushButton. Not used anywhere
//              in this file — all styling is done manually via Paint events.
//
//   [FIX #5] Removed: using System.Globalization;
//            → CultureInfo, DateTimeFormatInfo etc. Not used in this file.
//
// Zero logic changes. All form behaviour is identical.

using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Linq;
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
            Padding = new Padding(0),
            Margin = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));  // filter
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

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(12, 10, 12, 8)
        };

        // Student combo
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

        // From date
        var fromBlock = MakeFlowBlock("FROM", out var fromInner);
        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddDays(-30),
            Size = new Size(120, 30)
        };
        fromInner.Controls.Add(_dtpFrom);

        // To date
        var toBlock = MakeFlowBlock("TO", out var toInner);
        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today,
            Size = new Size(120, 30)
        };
        toInner.Controls.Add(_dtpTo);

        // Spacer
        var spacer = new Panel
        {
            Size = new Size(12, 40),
            BackColor = Color.Transparent
        };

        // Buttons
        var btnBlock = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0, 18, 0, 0),
            Margin = new Padding(0)
        };

        _btnAnalyze = MakeButton("▶  Analyze Student",
            TealAccent, Color.White, 160);

        _btnAnalyzeAll = MakeButton("▶▶  Analyze All",
            Color.FromArgb(40, 60, 100), Color.White, 140);

        _btnClear = MakeButton("✕  Clear",
            Color.FromArgb(240, 244, 248), TextMid, 80);
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

    private static Panel MakeFlowBlock(string labelText, out Panel innerPanel)
    {
        var outer = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 14, 0)
        };

        var lbl = new Label
        {
            Text = labelText,
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        innerPanel = new Panel
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Margin = new Padding(0)
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

        var summaryFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Margin = new Padding(0)
        };

        _lblTotalAnalyzed = MakeSummaryValue("—");
        _lblNormalCount = MakeSummaryValue("—");
        _lblAtRiskCount = MakeSummaryValue("—");
        _lblMalCount = MakeSummaryValue("—");

        var items = new[]
        {
            ("📋", "Total Analyzed", _lblTotalAnalyzed, Color.FromArgb(150, 180, 220)),
            ("✅", "Normal",         _lblNormalCount,   NormalGreen),
            ("⚠",  "At-Risk",        _lblAtRiskCount,   AmberColor),
            ("🔴", "Malnourished",   _lblMalCount,      DangerRed),
        };

        foreach (var (icon, title, valLbl, color) in items)
        {
            var block = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 2,
                AutoSize = true,
                Margin = new Padding(0, 0, 64, 0),
                Padding = new Padding(0, 8, 0, 8)
            };
            block.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 36f));
            block.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var iconLbl = new Label
            {
                Text = icon,
                Font = new Font("Segoe UI", 13f),
                ForeColor = color,
                AutoSize = false,
                Size = new Size(28, 38),
                TextAlign = ContentAlignment.MiddleCenter,
                Margin = new Padding(0)
            };
            block.SetRowSpan(iconLbl, 2);

            var titleLbl = new Label
            {
                Text = title.ToUpperInvariant(),
                Font = new Font("Segoe UI", 7f, FontStyle.Bold),
                ForeColor = Color.FromArgb(100, 130, 170),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left
            };

            valLbl.ForeColor = color;
            valLbl.Margin = new Padding(0);

            block.Controls.Add(iconLbl, 0, 0);
            block.Controls.Add(titleLbl, 1, 0);
            block.Controls.Add(valLbl, 1, 1);

            summaryFlow.Controls.Add(block);
        }

        _summaryStrip.Controls.Add(summaryFlow);
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
        };

        _splitMain.Panel1.BackColor = BgColor;
        _splitMain.Panel2.BackColor = BgColor;

        _splitMain.HandleCreated += (_, _) => SetSplitterSafe(_splitMain);
        _splitMain.SizeChanged += (_, _) => SetSplitterSafe(_splitMain);

        BuildStudentGrid();
        BuildDetailPanel();
    }

    private void SetSplitterSafe(SplitContainer s)
    {
        if (!s.IsHandleCreated || s.Width <= 1) return;

        try
        {
            s.Panel1MinSize = 240;
            s.Panel2MinSize = 300;

            int desired = (int)(s.Width * 0.38);
            int min = s.Panel1MinSize;
            int max = s.Width - s.Panel2MinSize - s.SplitterWidth;

            if (max > min)
                s.SplitterDistance = Math.Clamp(desired, min, max);
        }
        catch (InvalidOperationException) { }
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

        var placeholderLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Margin = new Padding(0)
        };

        var card = new Panel
        {
            Size = new Size(380, 160),
            BackColor = CardBg,
            Anchor = AnchorStyles.None
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
        placeholderLayout.Controls.Add(card, 0, 0);

        _detailPanel.Controls.Add(placeholderLayout);
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
        statusLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _lblCount = new Label
        {
            Text = "No analysis run yet.",
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

        statusLayout.Controls.Add(_lblCount, 0, 0);
        statusLayout.Controls.Add(_lblStatus, 1, 0);

        _statusBar.Controls.Add(statusLayout);
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
        await SetLoadingAsync(true);

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
            await SetLoadingAsync(false);
        }
    }

    private async Task AnalyzeAllAsync()
    {
        SetStatus("Analyzing all students…", TextMuted);
        await SetLoadingAsync(true);

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
            await SetLoadingAsync(false);
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

        var rootTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(16, 16, 16, 32),
            Margin = new Padding(0)
        };
        rootTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        int row = 0;
        void AddRow() => rootTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ── Student header card ───────────────────────────────────────────────
        AddRow();
        var headerCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 94,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 0, 16)
        };

        var statusColor = result.Status switch
        {
            NutritionStatus.Malnourished => DangerRed,
            NutritionStatus.AtRisk => AmberColor,
            _ => NormalGreen
        };

        headerCard.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var bar = new SolidBrush(statusColor);
            g.FillRectangle(bar, 0, 0, 5, headerCard.Height);
            using var pen = new Pen(BorderLight, 1);
            g.DrawRectangle(pen, 0, 0, headerCard.Width - 1, headerCard.Height - 1);
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(16, 12, 16, 12)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblName = new Label
        {
            Text = result.StudentName,
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4)
        };

        var lblMeta = new Label
        {
            Text = $"Age {result.Age}  ·  {result.Gender}  ·  " +
                        $"{result.Period.From:MMM dd} – {result.Period.To:MMM dd, yyyy}",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0)
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
            ForeColor = statusColor,
            BackColor = statusBg,
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Anchor = AnchorStyles.Right | AnchorStyles.Bottom,
            Margin = new Padding(0, 0, 0, 4)
        };

        var lblDeficit = new Label
        {
            Text = $"Weighted Deficit: {result.WeightedDeficitPercentage:F1}%",
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = statusColor,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0)
        };

        headerLayout.Controls.Add(lblName, 0, 0);
        headerLayout.Controls.Add(lblMeta, 0, 1);
        headerLayout.Controls.Add(lblStatusBadge, 1, 0);
        headerLayout.Controls.Add(lblDeficit, 1, 1);

        headerCard.Controls.Add(headerLayout);
        rootTable.Controls.Add(headerCard, 0, row++);

        // ── Averages card ─────────────────────────────────────────────────────
        AddRow();
        var avgCard = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 0, 24)
        };

        avgCard.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, avgCard.Width - 1, avgCard.Height - 1);
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, avgCard.Width, 3);
        };

        var avgLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            RowCount = 3,
            Padding = new Padding(12, 10, 12, 10)
        };
        for (int i = 0; i < 5; i++)
            avgLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20f));

        avgLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        avgLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
        avgLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblAvgTitle = new Label
        {
            Text = "PERIOD AVERAGES",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        avgLayout.Controls.Add(lblAvgTitle, 0, 0);
        avgLayout.SetColumnSpan(lblAvgTitle, 5);

        var avgItems = new[]
        {
            ($"🔥 {result.AvgCalories:F0} kcal", "Calories"),
            ($"💪 {result.AvgProtein:F1}g",       "Protein"),
            ($"🌾 {result.AvgCarbohydrates:F1}g",  "Carbs"),
            ($"🧈 {result.AvgFats:F1}g",           "Fats"),
            ($"🥦 {result.AvgFiber:F1}g",          "Fiber"),
        };

        for (int i = 0; i < avgItems.Length; i++)
        {
            var valLbl = new Label
            {
                Text = avgItems[i].Item1,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                ForeColor = TealAccent,
                AutoSize = true,
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                Margin = new Padding(0, 0, 0, 2)
            };
            var titleLbl = new Label
            {
                Text = avgItems[i].Item2,
                Font = new Font("Segoe UI", 7.5f),
                ForeColor = TextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Top | AnchorStyles.Left,
                Margin = new Padding(0)
            };
            avgLayout.Controls.Add(valLbl, i, 1);
            avgLayout.Controls.Add(titleLbl, i, 2);
        }

        avgCard.Controls.Add(avgLayout);
        rootTable.Controls.Add(avgCard, 0, row++);

        // ── Deficit breakdown ─────────────────────────────────────────────────
        if (result.Deficits.Count > 0)
        {
            AddRow();
            var defTitle = new Label
            {
                Text = "NUTRIENT DEFICIT BREAKDOWN",
                Font = new Font("Segoe UI", 8f, FontStyle.Bold),
                ForeColor = TextMid,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 8)
            };
            rootTable.Controls.Add(defTitle, 0, row++);

            foreach (var deficit in result.Deficits.OrderByDescending(d => d.DeficitPercentage))
            {
                AddRow();
                var defRow = BuildDeficitRow(deficit);
                rootTable.Controls.Add(defRow, 0, row++);
            }
        }

        // ── RENI reference note ───────────────────────────────────────────────
        AddRow();
        var refNote = new Label
        {
            Text = "※  Reference values based on DOH Philippine RENI 2015 standards.",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 16, 0, 0)
        };
        rootTable.Controls.Add(refNote, 0, row++);

        _detailPanel.Controls.Add(rootTable);
    }

    private Panel BuildDeficitRow(NutrientDeficitDetail deficit)
    {
        var rowPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 52,
            BackColor = CardBg,
            Margin = new Padding(0, 0, 0, 8)
        };

        Color defColor = deficit.DeficitPercentage switch
        {
            >= 30 => DangerRed,
            >= 15 => AmberColor,
            _ => NormalGreen
        };

        rowPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawRectangle(pen, 0, 0, rowPanel.Width - 1, rowPanel.Height - 1);
            using var bar = new SolidBrush(defColor);
            e.Graphics.FillRectangle(bar, 0, 0, 4, rowPanel.Height);
        };

        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Padding = new Padding(12, 6, 12, 6)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80f));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        var lblName = new Label
        {
            Text = deficit.NutrientName,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Bottom,
            Margin = new Padding(0, 0, 0, 2)
        };

        var lblDetails = new Label
        {
            Text = $"Actual: {deficit.ActualValue:F1}   RENI: {deficit.RecommendedValue:F1} {deficit.Unit}",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = TextMid,
            AutoSize = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Top,
            Margin = new Padding(0)
        };

        var track = new Panel
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Height = 8,
            BackColor = Color.FromArgb(230, 235, 242),
            Margin = new Padding(16, 0, 16, 0)
        };

        var fill = new Panel
        {
            Height = 8,
            BackColor = defColor,
            Location = new Point(0, 0)
        };

        track.Controls.Add(fill);
        track.Resize += (_, _) =>
        {
            fill.Width = (int)Math.Min(
                track.Width, track.Width * (deficit.DeficitPercentage / 100.0));
        };

        table.SetRowSpan(track, 2);

        var lblPct = new Label
        {
            Text = $"{deficit.DeficitPercentage:F1}%",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = defColor,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleCenter,
            Margin = new Padding(0)
        };
        table.SetRowSpan(lblPct, 2);

        table.Controls.Add(lblName, 0, 0);
        table.Controls.Add(lblDetails, 0, 1);
        table.Controls.Add(track, 1, 0);
        table.Controls.Add(lblPct, 2, 0);

        rowPanel.Controls.Add(table);
        return rowPanel;
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

    private async Task SetLoadingAsync(bool loading)
    {
        _btnAnalyze.Enabled = !loading;
        _btnAnalyzeAll.Enabled = !loading;
        _btnAnalyze.Text = loading ? "Analyzing…" : "▶  Analyze Student";
        _btnAnalyzeAll.Text = loading ? "Please wait…" : "▶▶  Analyze All";
        await Task.Yield();
    }

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
    }

    private static Label MakeSummaryValue(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 14f, FontStyle.Bold),
        ForeColor = Color.White,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Margin = new Padding(0)
    };

    private static System.Windows.Forms.Button MakeButton(
        string text, Color bg, Color fg, int width)
    {
        var btn = new System.Windows.Forms.Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9f),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 36),
            Margin = new Padding(0, 0, 8, 0),
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