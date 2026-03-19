using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Interfaces;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.Students;

public class StudentListForm : UserControl
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
    private static readonly Color GridHeader = Color.FromArgb(245, 248, 252);
    private static readonly Color GridAlt = Color.FromArgb(250, 252, 255);
    private static readonly Color GridSelect = Color.FromArgb(209, 246, 241);

    // ── Controls ──────────────────────────────────────────────────────────────
    private TextBox _txtSearch = null!;
    private Button _btnSearch = null!;
    private Button _btnClear = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnRefresh = null!;
    private DataGridView _grid = null!;
    private Label _lblStatus = null!;
    private Label _lblCount = null!;
    private Panel _toolbarPanel = null!;
    private Panel _gridPanel = null!;
    private Panel _statusBar = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private List<StudentDto> _students = new();
    private readonly Panel _parentContentArea;

    // ─────────────────────────────────────────────────────────────────────────
    public StudentListForm(Panel parentContentArea)
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
        Padding = new Padding(0);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            BackColor = BgColor,
            Padding = new Padding(0)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 68f));  // toolbar
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // grid
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36f));  // status bar

        BuildToolbar();
        BuildGrid();
        BuildStatusBar();

        layout.Controls.Add(_toolbarPanel, 0, 0);
        layout.Controls.Add(_gridPanel, 0, 1);
        layout.Controls.Add(_statusBar, 0, 2);

        Controls.Add(layout);
    }

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

        // ── Search row ────────────────────────────────────────────────────────
        var searchIcon = new Label
        {
            Text = "🔍",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(28, 36),
            Location = new Point(16, 16),
            TextAlign = ContentAlignment.MiddleCenter
        };

        _txtSearch = new TextBox
        {
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextDark,
            BackColor = Color.FromArgb(246, 248, 252),
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = "Search by name, student number, grade or section…",
            Location = new Point(44, 16),
            Size = new Size(340, 36),
            TabIndex = 0
        };

        _btnSearch = MakeButton("Search", TealAccent, Color.White, new Point(392, 16), 88);
        _btnClear = MakeButton("Clear", Color.FromArgb(240, 244, 248), TextMid, new Point(488, 16), 70);
        _btnClear.FlatAppearance.BorderColor = BorderLight;
        _btnClear.FlatAppearance.BorderSize = 1;

        // ── Action buttons (right-aligned) ────────────────────────────────────
        _btnAdd = MakeButton("＋  Add Student", TealAccent, Color.White, Point.Empty, 130);
        _btnEdit = MakeButton("✎  Edit", Color.FromArgb(240, 244, 248), TextMid, Point.Empty, 90);
        _btnDelete = MakeButton("🗑  Delete", Color.FromArgb(240, 244, 248), DangerRed, Point.Empty, 90);
        _btnRefresh = MakeButton("↺  Refresh", Color.FromArgb(240, 244, 248), TextMid, Point.Empty, 90);

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
            searchIcon, _txtSearch, _btnSearch, _btnClear,
            _btnAdd, _btnEdit, _btnDelete, _btnRefresh
        });

        // Position right-side buttons on resize
        _toolbarPanel.Resize += (_, _) => PositionToolbarRight();
        PositionToolbarRight();

        // Events
        _btnSearch.Click += async (_, _) => await SearchAsync();
        _btnClear.Click += async (_, _) =>
        {
            _txtSearch.Clear();
            await LoadStudentsAsync();
        };
        _btnAdd.Click += OpenAddDialog;
        _btnEdit.Click += OpenEditDialog;
        _btnDelete.Click += async (_, _) => await DeleteSelectedAsync();
        _btnRefresh.Click += async (_, _) => await LoadStudentsAsync();
        _txtSearch.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await SearchAsync();
        };
    }

    private void PositionToolbarRight()
    {
        int right = _toolbarPanel.Width - 16;
        int y = 16;
        int h = 36;

        _btnRefresh.Location = new Point(right - _btnRefresh.Width, y);
        right -= _btnRefresh.Width + 8;

        _btnDelete.Location = new Point(right - _btnDelete.Width, y);
        right -= _btnDelete.Width + 8;

        _btnEdit.Location = new Point(right - _btnEdit.Width, y);
        right -= _btnEdit.Width + 8;

        _btnAdd.Location = new Point(right - _btnAdd.Width, y);
    }

    private void BuildGrid()
    {
        _gridPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(16, 12, 16, 0)
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
            RowTemplate = { Height = 40 },
            ShowCellToolTips = true
        };

        // Header style
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

        // Default cell style
        _grid.DefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = CardBg,
            ForeColor = TextDark,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0),
            Alignment = DataGridViewContentAlignment.MiddleLeft
        };

        // Alternating row style
        _grid.AlternatingRowsDefaultCellStyle = new DataGridViewCellStyle
        {
            BackColor = GridAlt,
            ForeColor = TextDark,
            SelectionBackColor = GridSelect,
            SelectionForeColor = TextDark,
            Padding = new Padding(8, 0, 0, 0)
        };

        // Columns
        _grid.Columns.AddRange(
            MakeColumn("StudentNumber", "Student No.", 80, false),
            MakeColumn("FullName", "Full Name", 160, false),
            MakeColumn("GradeLevel", "Grade", 70, false),
            MakeColumn("Section", "Section", 80, false),
            MakeColumn("Gender", "Gender", 70, false),
            MakeColumn("Age", "Age", 50, false),
            MakeColumn("DateOfBirth", "Date of Birth", 110, false)
        );

        // Set fill weights
        _grid.Columns["StudentNumber"]!.FillWeight = 80;
        _grid.Columns["FullName"]!.FillWeight = 180;
        _grid.Columns["GradeLevel"]!.FillWeight = 70;
        _grid.Columns["Section"]!.FillWeight = 80;
        _grid.Columns["Gender"]!.FillWeight = 70;
        _grid.Columns["Age"]!.FillWeight = 50;
        _grid.Columns["DateOfBirth"]!.FillWeight = 110;

        _grid.SelectionChanged += Grid_SelectionChanged;
        _grid.CellDoubleClick += Grid_CellDoubleClick;

        // Paint bottom border on header
        _grid.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _grid.ColumnHeadersHeight,
                _grid.Width, _grid.ColumnHeadersHeight);
        };

        _gridPanel.Controls.Add(_grid);
    }

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
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 36
        };

        _lblStatus = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TealAccent,
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Height = 36
        };

        _statusBar.Controls.Add(_lblCount);
        _statusBar.Controls.Add(_lblStatus);

        _statusBar.Resize += (_, _) =>
        {
            _lblStatus.Location = new Point(_statusBar.Width - _lblStatus.Width - 16, 0);
        };
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Data Operations
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadStudentsAsync()
    {
        SetStatus("Loading…", TextMuted);
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IStudentService>();
            _students = (await service.GetAllStudentsAsync()).ToList();
            BindGrid(_students);
            SetCount(_students.Count);
            SetStatus("Ready", TealAccent);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load students.");
            SetStatus("Error loading students.", DangerRed);
        }
    }

    private async Task SearchAsync()
    {
        var keyword = _txtSearch.Text.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            await LoadStudentsAsync();
            return;
        }

        SetStatus("Searching…", TextMuted);
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IStudentService>();
            _students = (await service.SearchStudentsAsync(keyword)).ToList();
            BindGrid(_students);
            SetCount(_students.Count, $"results for \"{keyword}\"");
            SetStatus($"{_students.Count} found", TealAccent);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Search failed.");
            SetStatus("Search error.", DangerRed);
        }
    }

    private async Task DeleteSelectedAsync()
    {
        var student = GetSelectedStudent();
        if (student == null) return;

        var confirm = MessageBox.Show(
            $"Delete student {student.FullName} ({student.StudentNumber})?\n\nThis will soft-delete the record. Meal logs are preserved.",
            "Confirm Delete",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IStudentService>();
            var (success, message) = await service.DeleteStudentAsync(student.Id);

            if (success)
            {
                SerilogLog.Information("Student deleted: {Name} ({Id})",
                    student.FullName, student.Id);
                SetStatus($"Deleted: {student.FullName}", TealAccent);
                await LoadStudentsAsync();
            }
            else
            {
                MessageBox.Show(message, "Delete Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Delete failed for student {Id}", student.Id);
            MessageBox.Show("An error occurred while deleting the student.",
                "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dialog Launchers
    // ─────────────────────────────────────────────────────────────────────────

    private void OpenAddDialog(object? sender, EventArgs e)
    {
        using var dialog = new StudentFormDialog(null);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetStatus("Student added successfully.", TealAccent);
            _ = LoadStudentsAsync();
        }
    }

    private void OpenEditDialog(object? sender, EventArgs e)
    {
        var student = GetSelectedStudent();
        if (student == null) return;

        using var dialog = new StudentFormDialog(student);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            SetStatus($"Updated: {student.FullName}", TealAccent);
            _ = LoadStudentsAsync();
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Grid Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void BindGrid(List<StudentDto> students)
    {
        _grid.Rows.Clear();

        foreach (var s in students)
        {
            int row = _grid.Rows.Add(
                s.StudentNumber,
                s.FullName,
                s.GradeLevel,
                s.Section,
                s.Gender.ToString(),
                s.Age,
                s.DateOfBirth.ToString("MMM dd, yyyy")
            );
            _grid.Rows[row].Tag = s;
        }

        UpdateActionButtons();
    }

    private void Grid_SelectionChanged(object? sender, EventArgs e)
        => UpdateActionButtons();

    private void Grid_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0) return;
        OpenEditDialog(sender, e);
    }

    private void UpdateActionButtons()
    {
        bool hasSelection = _grid.SelectedRows.Count > 0;
        _btnEdit.Enabled = hasSelection;
        _btnDelete.Enabled = hasSelection;
    }

    private StudentDto? GetSelectedStudent()
    {
        if (_grid.SelectedRows.Count == 0) return null;
        return _grid.SelectedRows[0].Tag as StudentDto;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Status Bar
    // ─────────────────────────────────────────────────────────────────────────

    private void SetCount(int count, string suffix = "students")
    {
        _lblCount.Text = $"Showing {count} {suffix}";
    }

    private void SetStatus(string message, Color color)
    {
        _lblStatus.ForeColor = color;
        _lblStatus.Text = message;
        _statusBar.Resize += (_, _) =>
            _lblStatus.Location = new Point(
                _statusBar.Width - _lblStatus.Width - 16, 0);
        _lblStatus.Location = new Point(
            _statusBar.Width - _lblStatus.Width - 16, 0);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Factory Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static DataGridViewTextBoxColumn MakeColumn(
        string name, string header, int minWidth, bool readOnly)
    {
        return new DataGridViewTextBoxColumn
        {
            Name = name,
            HeaderText = header,
            DataPropertyName = name,
            MinimumWidth = minWidth,
            ReadOnly = readOnly,
            SortMode = DataGridViewColumnSortMode.Automatic
        };
    }

    private static Button MakeButton(
        string text, Color bg, Color fg, Point location, int width)
    {
        var btn = new Button
        {
            Text = text,
            Font = new Font("Segoe UI", 9f, FontStyle.Regular),
            BackColor = bg,
            ForeColor = fg,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(width, 36),
            Location = location,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderSize = 0;
        return btn;
    }
}