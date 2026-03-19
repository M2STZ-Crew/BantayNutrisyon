using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using OpenTK.Graphics;
using ScottPlot;
using ScottPlot.TickGenerators.TimeUnits;
using System.Diagnostics.Metrics;
using System.Globalization;
using static System.Windows.Forms.LinkLabel;
using SerilogLog = Serilog.Log;
using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;


// Alias to avoid conflict between System.Drawing.Color and ScottPlot.Color
using SPColor = ScottPlot.Color;
using WinColor = System.Drawing.Color;

namespace NutritionMonitor.UI.Forms.Charts;

public class ChartsForm : UserControl
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly WinColor BgColor = WinColor.FromArgb(243, 246, 250);
    private static readonly WinColor CardBg = WinColor.White;
    private static readonly WinColor TealAccent = WinColor.FromArgb(0, 168, 150);
    private static readonly WinColor TealHover = WinColor.FromArgb(0, 148, 132);
    private static readonly WinColor TextDark = WinColor.FromArgb(22, 32, 50);
    private static readonly WinColor TextMid = WinColor.FromArgb(80, 100, 130);
    private static readonly WinColor TextMuted = WinColor.FromArgb(140, 160, 185);
    private static readonly WinColor BorderLight = WinColor.FromArgb(225, 232, 242);
    private static readonly WinColor DangerRed = WinColor.FromArgb(220, 60, 60);
    private static readonly WinColor AmberColor = WinColor.FromArgb(245, 158, 11);
    private static readonly WinColor NormalGreen = WinColor.FromArgb(0, 168, 150);
    private static readonly WinColor StatBlue = WinColor.FromArgb(59, 130, 246);

    // ScottPlot colors
    private static readonly SPColor SP_Teal = new SPColor(0, 168, 150);
    private static readonly SPColor SP_Blue = new SPColor(59, 130, 246);
    private static readonly SPColor SP_Amber = new SPColor(245, 158, 11);
    private static readonly SPColor SP_Red = new SPColor(220, 60, 60);
    private static readonly SPColor SP_Green = new SPColor(40, 200, 120);
    private static readonly SPColor SP_Navy = new SPColor(30, 50, 100);
    private static readonly SPColor SP_BgFig = new SPColor(255, 255, 255);
    private static readonly SPColor SP_BgData = new SPColor(248, 250, 252);
    private static readonly SPColor SP_Grid = new SPColor(225, 232, 242);

    // ── Filter controls ───────────────────────────────────────────────────────
    private Panel _filterPanel = null!;
    private ComboBox _cmbStudent = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private Button _btnLoad = null!;

    // ── Tab control ───────────────────────────────────────────────────────────
    private TabControl _tabs = null!;

    // ── ScottPlot controls ────────────────────────────────────────────────────
    private ScottPlot.WinForms.FormsPlot _trendPlot = null!;
    private ScottPlot.WinForms.FormsPlot _statusPlot = null!;
    private ScottPlot.WinForms.FormsPlot _deficitPlot = null!;

    // ── Status bar ────────────────────────────────────────────────────────────
    private Panel _statusBar = null!;
    private WinForms.Label _lblStatus = null!;
    private WinForms.Label _lblCount = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<StudentDto> _students = new();
    private readonly Panel _parentContentArea;

    // ─────────────────────────────────────────────────────────────────────────
    public ChartsForm(Panel parentContentArea)
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
            RowCount = 3,
            ColumnCount = 1,
            BackColor = BgColor,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));  // filter
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // charts
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // status

        BuildFilterPanel();
        BuildChartTabs();
        BuildStatusBar();

        layout.Controls.Add(_filterPanel, 0, 0);
        layout.Controls.Add(_tabs, 0, 1);
        layout.Controls.Add(_statusBar, 0, 2);

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
            using var pen = new System.Drawing.Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _filterPanel.Height - 1,
                _filterPanel.Width, _filterPanel.Height - 1);
        };

        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = false,
            Padding = new Padding(14, 8, 14, 8)
        };

        // Student
        flow.Controls.Add(MakeFlowLabel("STUDENT"));
        _cmbStudent = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Size = new Size(210, 30),
            BackColor = CardBg,
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat,
            DisplayMember = "FullName",
            Margin = new Padding(4, 2, 16, 0)
        };
        flow.Controls.Add(_cmbStudent);

        // From
        flow.Controls.Add(MakeFlowLabel("FROM"));
        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddDays(-30),
            Size = new Size(118, 30),
            Margin = new Padding(4, 2, 16, 0)
        };
        flow.Controls.Add(_dtpFrom);

        // To
        flow.Controls.Add(MakeFlowLabel("TO"));
        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today,
            Size = new Size(118, 30),
            Margin = new Padding(4, 2, 20, 0)
        };
        flow.Controls.Add(_dtpTo);

        // Load button
        _btnLoad = MakeButton("📊  Load Charts", TealAccent, WinColor.White, 140);
        _btnLoad.Margin = new Padding(0, 2, 0, 0);
        flow.Controls.Add(_btnLoad);

        _filterPanel.Controls.Add(flow);
        _btnLoad.Click += async (_, _) => await LoadAllChartsAsync();
    }

    // ── Chart Tabs ────────────────────────────────────────────────────────────

    private void BuildChartTabs()
    {
        _tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5f),
            Appearance = TabAppearance.FlatButtons,
            ItemSize = new Size(180, 32),
            SizeMode = TabSizeMode.Fixed,
            BackColor = BgColor
        };

        _trendPlot = MakeFormsPlot();
        _statusPlot = MakeFormsPlot();
        _deficitPlot = MakeFormsPlot();

        var tabTrend = new TabPage("  📈  Nutrient Trend  ")
        {
            BackColor = CardBg,
            Padding = new Padding(12)
        };

        var tabStatus = new TabPage("  📊  Status Overview  ")
        {
            BackColor = CardBg,
            Padding = new Padding(12)
        };

        var tabDeficit = new TabPage("  🔬  Deficit vs RENI  ")
        {
            BackColor = CardBg,
            Padding = new Padding(12)
        };

        tabTrend.Controls.Add(_trendPlot);
        tabStatus.Controls.Add(_statusPlot);
        tabDeficit.Controls.Add(_deficitPlot);

        _tabs.TabPages.AddRange(new[] { tabTrend, tabStatus, tabDeficit });

        DrawTrendPlaceholder();
        DrawStatusPlaceholder();
        DrawDeficitPlaceholder();
    }

    // ── Status Bar ────────────────────────────────────────────────────────────

    private void BuildStatusBar()
    {
        _statusBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = WinColor.FromArgb(245, 248, 252),
            Padding = new Padding(16, 0, 16, 0)
        };

        _statusBar.Paint += (s, e) =>
        {
            using var pen = new System.Drawing.Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, _statusBar.Width, 0);
        };

        _lblCount = new WinForms.Label
        {
            Text = "Select a student and date range, then click Load Charts.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(0, 0),
            Height = 36,
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblStatus = new WinForms.Label
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
            SerilogLog.Error(ex, "Failed to load students for charts.");
        }
    }

    private async Task LoadAllChartsAsync()
    {
        SetStatus("Loading charts…", TextMuted);
        SetLoading(true);

        try
        {
            var from = _dtpFrom.Value.Date;
            var to = _dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

            var selectedStudent =
                _cmbStudent.SelectedItem as StudentDto;

            using var scope = ServiceLocator.CreateScope();
            var mealSvc = scope.ServiceProvider
                .GetRequiredService<IMealLogService>();
            var analysisSvc = scope.ServiceProvider
                .GetRequiredService<INutritionAnalysisService>();

            // ── Chart 1: Nutrient Trend ───────────────────────────────────────
            if (selectedStudent != null)
            {
                var logs = (await mealSvc
                    .GetLogsByStudentAndDateRangeAsync(
                        selectedStudent.Id, from, to))
                    .OrderBy(l => l.LogDate)
                    .ToList();

                DrawNutrientTrend(logs, selectedStudent.FullName);
            }
            else
            {
                var logs = (await mealSvc.GetLogsByDateRangeAsync(from, to))
                    .OrderBy(l => l.LogDate)
                    .ToList();
                DrawNutrientTrend(logs, "All Students");
            }

            // ── Chart 2: Status Overview ──────────────────────────────────────
            var allResults = (await analysisSvc
                .AnalyzeAllStudentsAsync(from, to))
                .ToList();

            DrawStatusOverview(allResults);

            // ── Chart 3: Deficit vs RENI ──────────────────────────────────────
            if (selectedStudent != null)
            {
                var result = await analysisSvc
                    .AnalyzeStudentAsync(selectedStudent.Id, from, to);

                if (result != null)
                    DrawDeficitVsReni(result);
                else
                    DrawDeficitPlaceholder(
                        $"No data for {selectedStudent.FullName} in this period.");
            }
            else if (allResults.Count > 0)
            {
                // Show the most-at-risk student
                var worst = allResults
                    .OrderByDescending(r => r.WeightedDeficitPercentage)
                    .First();
                DrawDeficitVsReni(worst);
                SetStatus(
                    $"Showing deficit chart for most at-risk: {worst.StudentName}",
                    AmberColor);
            }
            else
            {
                DrawDeficitPlaceholder("No analysis data available.");
            }

            int logCount = 0;
            if (selectedStudent != null)
            {
                var l = await mealSvc.GetLogsByStudentAndDateRangeAsync(
                    selectedStudent.Id, from, to);
                logCount = l.Count();
            }

            _lblCount.Text =
                $"Period: {from:MMM dd} – {to:MMM dd, yyyy}  ·  " +
                $"{allResults.Count} students analyzed";

            if (_lblStatus.Text == "Loading charts…" ||
                string.IsNullOrEmpty(_lblStatus.Text))
                SetStatus("Charts updated.", TealAccent);

            SerilogLog.Information("Charts loaded. {Count} results.", allResults.Count);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load charts.");
            SetStatus("Error loading charts. Check logs.", DangerRed);
        }
        finally
        {
            SetLoading(false);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chart 1 — Nutrient Trend (Line Chart)
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawNutrientTrend(List<MealLogDto> logs, string title)
    {
        var plt = _trendPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);

        if (logs.Count == 0)
        {
            plt.Title($"Nutrient Trend — No data for {title}");
            _trendPlot.Refresh();
            return;
        }

        // Group by date and compute daily averages
        var grouped = logs
            .GroupBy(l => l.LogDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => new
            {
                Date = g.Key,
                Calories = g.Average(l => l.CaloriesKcal),
                Protein = g.Average(l => l.ProteinG),
                Carbs = g.Average(l => l.CarbohydratesG),
                Fats = g.Average(l => l.FatsG),
            })
            .ToList();

        double[] dates = grouped.Select(g => g.Date.ToOADate()).ToArray();
        double[] calories = grouped.Select(g => g.Calories).ToArray();
        double[] protein = grouped.Select(g => g.Protein * 4).ToArray(); // kcal equiv
        double[] carbs = grouped.Select(g => g.Carbs * 4).ToArray();
        double[] fats = grouped.Select(g => g.Fats * 9).ToArray();

        // Calories line
        var calLine = plt.Add.Scatter(dates, calories);
        calLine.LegendText = "Calories (kcal)";
        calLine.Color = SP_Teal;
        calLine.LineWidth = 2.5f;
        calLine.MarkerSize = 6;

        // Protein line (kcal equivalent)
        var proLine = plt.Add.Scatter(dates, protein);
        proLine.LegendText = "Protein (kcal equiv.)";
        proLine.Color = SP_Blue;
        proLine.LineWidth = 2f;
        proLine.MarkerSize = 5;

        // Carbs line
        var carbLine = plt.Add.Scatter(dates, carbs);
        carbLine.LegendText = "Carbs (kcal equiv.)";
        carbLine.Color = SP_Amber;
        carbLine.LineWidth = 2f;
        carbLine.MarkerSize = 5;

        // Fats line
        var fatLine = plt.Add.Scatter(dates, fats);
        fatLine.LegendText = "Fats (kcal equiv.)";
        fatLine.Color = SP_Red;
        fatLine.LineWidth = 2f;
        fatLine.MarkerSize = 5;

        plt.Axes.DateTimeTicksBottom();
        plt.Title($"Daily Nutrient Trend — {title}");
        plt.XLabel("Date");
        plt.YLabel("Energy (kcal)");
        plt.ShowLegend();
        plt.Axes.AutoScale();

        _trendPlot.Refresh();
    }

    private void DrawTrendPlaceholder(string msg = "Load charts to see nutrient trend over time.")
    {
        var plt = _trendPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);
        plt.Title(msg);
        _trendPlot.Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chart 2 — Status Overview (Bar Chart)
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawStatusOverview(List<NutritionAnalysisDto> results)
    {
        var plt = _statusPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);

        if (results.Count == 0)
        {
            plt.Title("Status Overview — No analysis data available.");
            _statusPlot.Refresh();
            return;
        }

        int normal = results.Count(r => r.Status == NutritionStatus.Normal);
        int atRisk = results.Count(r => r.Status == NutritionStatus.AtRisk);
        int malnou = results.Count(r => r.Status == NutritionStatus.Malnourished);

        var bars = new ScottPlot.Bar[]
        {
            new ScottPlot.Bar
            {
                Position  = 0,
                Value     = normal,
                FillColor = SP_Green,
                Label     = $"Normal ({normal})"
            },
            new ScottPlot.Bar
            {
                Position  = 1,
                Value     = atRisk,
                FillColor = SP_Amber,
                Label     = $"At-Risk ({atRisk})"
            },
            new ScottPlot.Bar
            {
                Position  = 2,
                Value     = malnou,
                FillColor = SP_Red,
                Label     = $"Malnourished ({malnou})"
            }
        };

        plt.Add.Bars(bars);

        // Custom tick labels
        var tickGen = new ScottPlot.TickGenerators.NumericManual();
        tickGen.AddMajor(0, "Normal");
        tickGen.AddMajor(1, "At-Risk");
        tickGen.AddMajor(2, "Malnourished");
        plt.Axes.Bottom.TickGenerator = tickGen;

        plt.Title($"Nutrition Status Distribution — {results.Count} Students");
        plt.XLabel("Status");
        plt.YLabel("Number of Students");
        plt.Axes.AutoScale();
        plt.Axes.SetLimitsY(0,
            Math.Max(normal, Math.Max(atRisk, malnou)) * 1.25 + 1);

        _statusPlot.Refresh();
    }

    private void DrawStatusPlaceholder(string msg = "Load charts to see status distribution.")
    {
        var plt = _statusPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);
        plt.Title(msg);
        _statusPlot.Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Chart 3 — Deficit vs RENI (Horizontal-style bar chart)
    // ─────────────────────────────────────────────────────────────────────────

    private void DrawDeficitVsReni(NutritionAnalysisDto result)
    {
        var plt = _deficitPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);

        if (result.Deficits.Count == 0)
        {
            plt.Title($"No deficit data for {result.StudentName}.");
            _deficitPlot.Refresh();
            return;
        }

        var sorted = result.Deficits
            .OrderByDescending(d => d.DeficitPercentage)
            .ToList();

        var bars = new List<ScottPlot.Bar>();

        for (int i = 0; i < sorted.Count; i++)
        {
            var d = sorted[i];

            SPColor fillColor = d.DeficitPercentage switch
            {
                >= 30 => SP_Red,
                >= 15 => SP_Amber,
                _ => SP_Green
            };

            bars.Add(new ScottPlot.Bar
            {
                Position = i,
                Value = Math.Round(d.DeficitPercentage, 1),
                FillColor = fillColor,
                Label = $"{d.DeficitPercentage:F1}%"
            });
        }

        plt.Add.Bars(bars.ToArray());

        // Nutrient name tick labels
        var tickGen = new ScottPlot.TickGenerators.NumericManual();
        for (int i = 0; i < sorted.Count; i++)
            tickGen.AddMajor(i, sorted[i].NutrientName);
        plt.Axes.Bottom.TickGenerator = tickGen;

        // Reference lines
        var atRiskLine = plt.Add.HorizontalLine(15);
        atRiskLine.Color = SP_Amber;
        atRiskLine.LineWidth = 1.5f;
        atRiskLine.LinePattern = ScottPlot.LinePattern.Dashed;
        atRiskLine.LegendText = "At-Risk threshold (15%)";

        var malLine = plt.Add.HorizontalLine(30);
        malLine.Color = SP_Red;
        malLine.LineWidth = 1.5f;
        malLine.LinePattern = ScottPlot.LinePattern.Dashed;
        malLine.LegendText = "Malnourished threshold (30%)";

        var statusLabel = result.Status switch
        {
            NutritionStatus.Malnourished => "🔴 Malnourished",
            NutritionStatus.AtRisk => "⚠ At-Risk",
            _ => "✅ Normal"
        };

        plt.Title(
            $"Nutrient Deficit vs RENI — {result.StudentName}  " +
            $"[{statusLabel}  {result.WeightedDeficitPercentage:F1}% avg deficit]");
        plt.XLabel("Nutrient");
        plt.YLabel("Deficit (%)");
        plt.ShowLegend();
        plt.Axes.AutoScale();
        plt.Axes.SetLimitsY(0,
            Math.Max(sorted.Max(d => d.DeficitPercentage) * 1.2, 35));

        _deficitPlot.Refresh();
    }

    private void DrawDeficitPlaceholder(
        string msg = "Select a student and load charts to see deficit breakdown.")
    {
        var plt = _deficitPlot.Plot;
        plt.Clear();
        ApplyPlotStyle(plt);
        plt.Title(msg);
        _deficitPlot.Refresh();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  ScottPlot Style Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static ScottPlot.WinForms.FormsPlot MakeFormsPlot()
    {
        return new ScottPlot.WinForms.FormsPlot
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            BorderStyle = BorderStyle.None
        };
    }

    private static void ApplyPlotStyle(Plot plt)
    {
        plt.FigureBackground.Color = SP_BgFig;
        plt.DataBackground.Color = SP_BgData;

        plt.Grid.MajorLineColor = SP_Grid;
        plt.Grid.MajorLineWidth = 1f;

        plt.Axes.Color(new SPColor(80, 100, 130));
        plt.Axes.Bottom.FrameLineStyle.Color = new SPColor(210, 220, 232);
        plt.Axes.Left.FrameLineStyle.Color = new SPColor(210, 220, 232);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void SetLoading(bool loading)
    {
        _btnLoad.Enabled = !loading;
        _btnLoad.Text = loading ? "Loading…" : "📊  Load Charts";
        Application.DoEvents();
    }

    private void SetStatus(string msg, WinColor color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    private static WinForms.Label MakeFlowLabel(string text) => new()
    {
        Text = text,
        Font = new Drawing.Font("Segoe UI", 7.5f, Drawing.FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
        Margin = new Padding(0, 6, 4, 0),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static Button MakeButton(
        string text, WinColor bg, WinColor fg, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Drawing.Font("Segoe UI", 9.5f, Drawing.FontStyle.Bold),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 32),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.MouseEnter += (_, _) => btn.BackColor = TealHover;
        btn.MouseLeave += (_, _) => btn.BackColor = bg;
        return btn;
    }
}