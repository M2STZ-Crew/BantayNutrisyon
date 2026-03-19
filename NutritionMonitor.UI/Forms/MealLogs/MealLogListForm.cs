using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Interfaces;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.MealLogs;

public class MealLogListForm : UserControl
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
    private static readonly Color GridHeader = Color.FromArgb(245, 248, 252);
    private static readonly Color GridAlt = Color.FromArgb(250, 252, 255);
    private static readonly Color GridSelect = Color.FromArgb(209, 246, 241);
    private static readonly Color SummaryBg = Color.FromArgb(240, 252, 250);
    private static readonly Color AmberColor = Color.FromArgb(245, 158, 11);

    // ── Controls ──────────────────────────────────────────────────────────────
    private Panel _toolbarPanel = null!;
    private Panel _filterPanel = null!;
    private Panel _summaryBar = null!;
    private Panel _gridPanel = null!;
    private Panel _statusBar = null!;
    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private DateTimePicker _dtpFrom = null!;
    private DateTimePicker _dtpTo = null!;
    private ComboBox _cmbStudent = null!;
    private Button _btnFilter = null!;
    private Button _btnClear = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;
    private Label _lblCount = null!;
    private Label _lblStatus = null!;

    // ── Summary labels ────────────────────────────────────────────────────────
    private Label _lblSumCalories = null!;
    private Label _lblSumProtein = null!;
    private Label _lblSumCarbs = null!;
    private Label _lblSumFats = null!;
    private Label _lblSumFiber = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<MealLogDto> _logs = new();
    private List<StudentDto> _students = new();
    private readonly Panel _parentContentArea;

    // ─────────────────────────────────────────────────────────────────────────
    public MealLogListForm(Panel parentContentArea)
    {
        _parentContentArea = parentContentArea;
        BuildControl();
        _ = InitializeAsync();
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
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));  // toolbar
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58f));  // filter bar
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 52f));  // summary bar
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // grid
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // status bar

        BuildToolbar();
        BuildFilterBar();
        BuildSummaryBar();
        BuildGrid();
        BuildStatusBar();

        layout.Controls.Add(_toolbarPanel, 0, 0);
        layout.Controls.Add(_filterPanel, 0, 1);
        layout.Controls.Add(_summaryBar, 0, 2);
        layout.Controls.Add(_gridPanel, 0, 3);
        layout.Controls.Add(_statusBar, 0, 4);

        Controls.Add(layout);
    }

    // ── Toolbar ───────────────────────────────────────────────────────────────

    private void BuildToolbar()
    {
        _toolbarPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(16, 0, 16, 0)
        };

        _toolbarPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _toolbarPanel.Height - 1,
                _toolbarPanel.Width, _toolbarPanel.Height - 1);
        };

        var searchIcon = new Label
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(28, 36),
            Location = new Point(16, 14),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextDark,
            BackColor = Color.FromArgb(246, 248, 252),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Search by student name…",
            Location = new Point(44, 14),
            Size = new Size(260, 36),
            TabIndex = 0
        };

        _btnAdd = MakeToolButton("＋  Add Log", TealAccent, Color.White, 130);
        _btnEdit = MakeToolButton("✎  Edit", Color.FromArgb(240, 244, 248), TextMid, 88);
        _btnDelete = MakeToolButton("🗑  Delete", Color.FromArgb(240, 244, 248), DangerRed, 88);
        _btnRefresh = MakeToolButton("↺  Refresh", Color.FromArgb(240, 244, 248), TextMid, 88);

        _btnEdit.FlatAppearance.BorderSize = 1;
        _btnEdit.FlatAppearance.BorderColor = BorderLight;
        _btnDelete.FlatAppearance.BorderSize = 1;
        _btnDelete.FlatAppearance.BorderColor = Color.FromArgb(250, 200, 200);
        _btnRefresh.FlatAppearance.BorderSize = 1;
        _btnRefresh.FlatAppearance.BorderColor = BorderLight;

        _btnEdit.Enabled = false;
        _btnDelete.Enabled = false;

        _toolbarPanel.Controls.AddRange(new Control[]
        {
            searchIcon, _txtSearch, _btnAdd, _btnEdit, _btnDelete, _btnRefresh
        });

        _toolbarPanel.Resize += (_, _) => PositionToolbarRight();
        PositionToolbarRight();

        _btnAdd.Click += OpenAddDialog;
        _btnEdit.Click += OpenEditDialog;
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        _btnRefresh.Click += async (_, _) => await LoadLogsAsync();
        _txtSearch.TextChanged += FilterBySearch;
    }

    private void PositionToolbarRight()
    {
        int right = _toolbarPanel.Width - 16;
        int y = 14;

        _btnRefresh.Location = new Point(right - _btnRefresh.Width, y);
        right -= _btnRefresh.Width + 8;
        _btnDelete.Location = new Point(right - _btnDelete.Width, y);
        right -= _btnDelete.Width + 8;
        _btnEdit.Location = new Point(right - _btnEdit.Width, y);
        right -= _btnEdit.Width + 8;
        _btnAdd.Location = new Point(right - _btnAdd.Width, y);
    }

    // ── Filter Bar ────────────────────────────────────────────────────────────

    private void BuildFilterBar()
    {
        _filterPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(248, 250, 253),
            Padding = new Padding(16, 8, 16, 8)
        };

        _filterPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _filterPanel.Height - 1,
                _filterPanel.Width, _filterPanel.Height - 1);
        };

        var lblFrom = MakeFilterLabel("FROM");
        lblFrom.Location = new Point(0, 6);

        _dtpFrom = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today.AddDays(-30),
            Location = new Point(42, 4),
            Size = new Size(120, 32)
        };

        var lblTo = MakeFilterLabel("TO");
        lblTo.Location = new Point(172, 6);

        _dtpTo = new DateTimePicker
        {
            Format = DateTimePickerFormat.Short,
            Font = new Font("Segoe UI", 9.5f),
            Value = DateTime.Today,
            Location = new Point(196, 4),
            Size = new Size(120, 32)
        };

        var lblStudent = MakeFilterLabel("STUDENT");
        lblStudent.Location = new Point(330, 6);

        _cmbStudent = new ComboBox
        {
            Font = new Font("Segoe UI", 9.5f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(390, 4),
            Size = new Size(200, 32),
            BackColor = CardBg,
            ForeColor = TextDark,
            FlatStyle = FlatStyle.Flat
        };

        _btnFilter = MakeToolButton("Apply Filter", TealAccent, Color.White, 110);
        _btnFilter.Location = new Point(604, 4);
        _btnFilter.Size = new Size(110, 32);

        _btnClear = MakeToolButton("Clear", Color.FromArgb(240, 244, 248), TextMid, 70);
        _btnClear.Location = new Point(722, 4);
        _btnClear.Size = new Size(70, 32);
        _btnClear.FlatAppearance.BorderSize = 1;
        _btnClear.FlatAppearance.BorderColor = BorderLight;

        _filterPanel.Controls.AddRange(new Control[]
        {
            lblFrom, _dtpFrom, lblTo, _dtpTo,
            lblStudent, _cmbStudent, _btnFilter, _btnClear
        });

        _btnFilter.Click += async (_, _) => await LoadLogsAsync();
        _btnClear.Click += async (_, _) =>
        {
            _dtpFrom.Value = DateTime.Today.AddDays(-30);
            _dtpTo.Value = DateTime.Today;
            _cmbStudent.SelectedIndex = 0;
            _txtSearch.Clear();
            await LoadLogsAsync();
        };
    }

    // ── Summary Bar ───────────────────────────────────────────────────────────

    private void BuildSummaryBar()
    {
        _summaryBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = SummaryBg,
            Padding = new Padding(16, 0, 16, 0)
        };

        _summaryBar.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(190, 230, 225), 1);
            e.Graphics.DrawLine(pen, 0, _summaryBar.Height - 1,
                _summaryBar.Width, _summaryBar.Height - 1);
        };

        var summaryTitle = new Label
        {
            Text = "PERIOD AVERAGES  ·",
            Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
            ForeColor = Color.FromArgb(60, 130, 115),
            AutoSize = true,
            Location = new Point(0, 16),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var summaryItems = new[]
        {
            ("🔥 Calories", "—", _lblSumCalories),
            ("💪 Protein",  "—", _lblSumProtein),
            ("🌾 Carbs",    "—", _lblSumCarbs),
            ("🧈 Fats",     "—", _lblSumFats),
            ("🥦 Fiber",    "—", _lblSumFiber),
        };

        _lblSumCalories = MakeSummaryLabel();
        _lblSumProtein = MakeSummaryLabel();
        _lblSumCarbs = MakeSummaryLabel();
        _lblSumFats = MakeSummaryLabel();
        _lblSumFiber = MakeSummaryLabel();

        Label[] summaryLabels = {
            _lblSumCalories, _lblSumProtein, _lblSumCarbs, _lblSumFats, _lblSumFiber
        };
        string[] summaryTitles = {
            "🔥 Avg Calories", "💪 Avg Protein", "🌾 Avg Carbs", "🧈 Avg Fats", "🥦 Avg Fiber"
        };

        _summaryBar.Controls.Add(summaryTitle);

        int sx = summaryTitle.Width + 20;
        for (int i = 0; i < summaryLabels.Length; i++)
        {
            var titleLbl = new Label
            {
                Text = summaryTitles[i],
                Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 130, 115),
                AutoSize = true,
                Location = new Point(sx, 8),
                TextAlign = ContentAlignment.MiddleLeft
            };
            summaryLabels[i].Location = new Point(sx, 26);
            _summaryBar.Controls.Add(titleLbl);
            _summaryBar.Controls.Add(summaryLabels[i]);
            sx += 140;
        }
    }

    // ── Grid ─────────────────────────────────────────────────────────────────

    private void BuildGrid()
    {
        _gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(16, 10, 16, 0)
        };

        _grid = new DataGridView
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
            RowTemplate = { Height = 38 },
            ShowCellToolTips = true
        };

        _grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridHeader,
            ForeColor = TextMid,
            Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };
        _grid.ColumnHeadersHeight = 38;
        _grid.EnableHeadersVisualStyles = false;

        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardBg,
            ForeColor = TextDark,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridAlt,
            ForeColor = TextDark,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0)
        };

        _grid.Columns.AddRange(
            MakeCol("LogDate", "Date", 100),
            MakeCol("StudentName", "Student", 160),
            MakeCol("MealType", "Meal", 80),
            MakeCol("CaloriesKcal", "Calories", 80),
            MakeCol("ProteinG", "Protein (g)", 80),
            MakeCol("CarbohydratesG", "Carbs (g)", 80),
            MakeCol("FatsG", "Fats (g)", 80),
            MakeCol("FiberG", "Fiber (g)", 80),
            MakeCol("VitaminAMcg", "Vit A (mcg)", 90),
            MakeCol("VitaminCMg", "Vit C (mg)", 90),
            MakeCol("CalciumMg", "Calcium (mg)", 95),
            MakeCol("IronMg", "Iron (mg)", 80),
            MakeCol("Notes", "Notes", 120)
        );

        // Fill weights
        var weights = new Dictionary<string, float>
        {
            ["LogDate"] = 90,
            ["StudentName"] = 160,
            ["MealType"] = 80,
            ["CaloriesKcal"] = 80,
            ["ProteinG"] = 75,
            ["CarbohydratesG"] = 75,
            ["FatsG"] = 75,
            ["FiberG"] = 75,
            ["VitaminAMcg"] = 85,
            ["VitaminCMg"] = 85,
            ["CalciumMg"] = 90,
            ["IronMg"] = 75,
            ["Notes"] = 120
        };
        foreach (var kv in weights)
            if (_grid.Columns.Contains(kv.Key))
                _grid.Columns[kv.Key]!.FillWeight = kv.Value;

        _grid.SelectionChanged += (_, _) => UpdateActionButtons();
        _grid.CellDoubleClick += Grid_CellDoubleClick;

        _gridPanel.Controls.Add(_grid);
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
            Text = "Loading…",
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
            _lblStatus.Location = new Point(_statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Data Operations
    // ─────────────────────────────────────────────────────────────────────────

    private async Task InitializeAsync()
    {
        await LoadStudentsIntoComboAsync();
        await LoadLogsAsync();
    }

    private async Task LoadStudentsIntoComboAsync()
    {
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IStudentService>();
            _students = (await svc.GetAllStudentsAsync()).ToList();

            _cmbStudent.Items.Clear();
            _cmbStudent.Items.Add("— All Students —");
            foreach (var s in _students)
                _cmbStudent.Items.Add(s);

            _cmbStudent.DisplayMember = "FullName";
            _cmbStudent.SelectedIndex = 0;
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load students into combo.");
        }
    }

    private async Task LoadLogsAsync()
    {
        SetStatus("Loading…", TextMuted);

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMealLogService>();

            var from = _dtpFrom.Value.Date;
            var to = _dtpTo.Value.Date.AddDays(1).AddSeconds(-1);

            IEnumerable<MealLogDto> logs;

            if (_cmbStudent.SelectedItem is StudentDto selectedStudent)
                logs = await svc.GetLogsByStudentAndDateRangeAsync(
                    selectedStudent.Id, from, to);
            else
                logs = await svc.GetLogsByDateRangeAsync(from, to);

            _logs = logs.ToList();

            // Apply name search filter if any
            var keyword = _txtSearch.Text.Trim().ToLower();
            var filtered = string.IsNullOrEmpty(keyword)
                ? _logs
                : _logs.Where(l =>
                    l.StudentName.ToLower().Contains(keyword) ||
                    l.MealType.ToLower().Contains(keyword)).ToList();

            BindGrid(filtered);
            UpdateSummaryBar(filtered);
            SetCount(filtered.Count);
            SetStatus("Ready", TealAccent);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load meal logs.");
            SetStatus("Error loading logs.", DangerRed);
        }
    }

    private void FilterBySearch(object? sender, EventArgs e)
    {
        var keyword = _txtSearch.Text.Trim().ToLower();
        var filtered = string.IsNullOrEmpty(keyword)
            ? _logs
            : _logs.Where(l =>
                l.StudentName.ToLower().Contains(keyword) ||
                l.MealType.ToLower().Contains(keyword)).ToList();

        BindGrid(filtered);
        UpdateSummaryBar(filtered);
        SetCount(filtered.Count);
    }

    private async Task DeleteSelectedAsync()
    {
        var log = GetSelectedLog();
        if (log == null) return;

        var confirm = MessageBox.Show(
            $"Delete meal log for {log.StudentName}\non {log.LogDate:MMM dd, yyyy} ({log.MealType})?",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMealLogService>();
            var (success, message) = await svc.DeleteLogAsync(log.Id);

            if (success)
            {
                SerilogLog.Information("Meal log deleted: {Id}", log.Id);
                SetStatus("Log deleted.", TealAccent);
                await LoadLogsAsync();
            }
            else
            {
                MessageBox.Show(message, "Delete Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Delete meal log failed.");
            MessageBox.Show("An error occurred while deleting.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Grid Binding & Summary
    // ─────────────────────────────────────────────────────────────────────────

    private void BindGrid(List<MealLogDto> logs)
    {
        _grid.Rows.Clear();

        foreach (var l in logs)
        {
            int row = _grid.Rows.Add(
                l.LogDate.ToString("MMM dd, yyyy"),
                l.StudentName,
                l.MealType,
                l.CaloriesKcal.ToString("F1"),
                l.ProteinG.ToString("F1"),
                l.CarbohydratesG.ToString("F1"),
                l.FatsG.ToString("F1"),
                l.FiberG.ToString("F1"),
                l.VitaminAMcg.ToString("F1"),
                l.VitaminCMg.ToString("F1"),
                l.CalciumMg.ToString("F1"),
                l.IronMg.ToString("F1"),
                l.Notes ?? string.Empty
            );
            _grid.Rows[row].Tag = l;
        }

        UpdateActionButtons();
    }

    private void UpdateSummaryBar(List<MealLogDto> logs)
    {
        if (logs.Count == 0)
        {
            _lblSumCalories.Text = "—";
            _lblSumProtein.Text = "—";
            _lblSumCarbs.Text = "—";
            _lblSumFats.Text = "—";
            _lblSumFiber.Text = "—";
            return;
        }

        _lblSumCalories.Text = $"{logs.Average(l => l.CaloriesKcal):F1} kcal";
        _lblSumProtein.Text = $"{logs.Average(l => l.ProteinG):F1} g";
        _lblSumCarbs.Text = $"{logs.Average(l => l.CarbohydratesG):F1} g";
        _lblSumFats.Text = $"{logs.Average(l => l.FatsG):F1} g";
        _lblSumFiber.Text = $"{logs.Average(l => l.FiberG):F1} g";
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dialog Launchers
    // ─────────────────────────────────────────────────────────────────────────

    private void OpenAddDialog(object? sender, EventArgs e)
    {
        using var dialog = new MealLogFormDialog(null, _students);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetStatus("Meal log added.", TealAccent);
            _ = LoadLogsAsync();
        }
    }

    private void OpenEditDialog(object? sender, EventArgs e)
    {
        var log = GetSelectedLog();
        if (log == null) return;

        using var dialog = new MealLogFormDialog(log, _students);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetStatus("Meal log updated.", TealAccent);
            _ = LoadLogsAsync();
        }
    }

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        OpenEditDialog(sender, e);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void UpdateActionButtons()
    {
        bool sel = _grid.SelectedRows.Count > 0;
        _btnEdit.Enabled = sel;
        _btnDelete.Enabled = sel;
    }

    private MealLogDto? GetSelectedLog()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        return _grid.SelectedRows[0].Tag as MealLogDto;
    }

    private void SetCount(int count)
        => _lblCount.Text = $"Showing {count} log{(count != 1 ? "s" : "")}";

    private void SetStatus(string msg, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = msg;
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    private static Label MakeFilterLabel(string text) => new()
    {
        Text = text,
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
        TextAlign = ContentAlignment.MiddleLeft,
        Height = 32
    };

    private static Label MakeSummaryLabel() => new()
    {
        Text = "—",
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        ForeColor = TealAccent,
        AutoSize = true,
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
            Size = new Size(width, 36),
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
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