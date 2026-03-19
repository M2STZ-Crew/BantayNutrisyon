using HarfBuzzSharp;    
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Entities;
using NutritionMonitor.Models.Interfaces;
using System.ComponentModel.DataAnnotations;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using System.Drawing;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms.MealLogs;

public class MealLogFormDialog : Form
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly Color BgColor = Color.FromArgb(248, 250, 252);
    private static readonly Color CardBg = Color.White;
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 150);
    private static readonly Color TealHover = Color.FromArgb(0, 148, 132);
    private static readonly Color TextDark = Color.FromArgb(22, 32, 50);
    private static readonly Color TextMid = Color.FromArgb(80, 100, 130);
    private static readonly Color TextMuted = Color.FromArgb(140, 160, 185);
    private static readonly Color BorderLight = Color.FromArgb(210, 220, 232);
    private static readonly Color ErrorRed = Color.FromArgb(200, 50, 50);
    private static readonly Color ErrorLight = Color.FromArgb(254, 242, 242);
    private static readonly Color LabelColor = Color.FromArgb(70, 90, 120);
    private static readonly Color TabBg = Color.FromArgb(245, 248, 252);

    // ── Controls ──────────────────────────────────────────────────────────────
    private ComboBox _cmbStudent = null!;
    private DateTimePicker _dtpDate = null!;
    private ComboBox _cmbMealType = null!;
    private TabControl _tabNutrients = null!;

    // Macro tab inputs
    private TextBox _txtCalories = null!;
    private TextBox _txtProtein = null!;
    private TextBox _txtCarbs = null!;
    private TextBox _txtFats = null!;
    private TextBox _txtFiber = null!;

    // Micro tab inputs
    private TextBox _txtVitA = null!;
    private TextBox _txtVitC = null!;
    private TextBox _txtVitD = null!;
    private TextBox _txtCalcium = null!;
    private TextBox _txtIron = null!;
    private TextBox _txtZinc = null!;

    private TextBox _txtNotes = null!;
    private Label _lblError = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly MealLogDto? _existing;
    private readonly List<StudentDto> _students;
    private readonly bool _isEdit;

    // ─────────────────────────────────────────────────────────────────────────
    public MealLogFormDialog(MealLogDto? existing, List<StudentDto> students)
    {
        _existing = existing;
        _students = students;
        _isEdit = existing != null;
        BuildForm();
        if (_isEdit) PopulateFields();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Form Construction
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildForm()
    {
        SuspendLayout();

        Text = _isEdit ? "Edit Meal Log" : "Add Meal Log";
        Size = new Size(560, 640);
        MinimumSize = new Size(520, 600);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        AutoSize = false;
        MinimizeBox = false;
        BackColor = BgColor;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);

        BuildHeader();
        BuildBody();
        BuildFooter();

        ResumeLayout(false);
        PerformLayout();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void BuildHeader()
    {
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 68,
            BackColor = CardBg
        };

        header.Paint += (s, e) =>
        {
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, 4, header.Height);
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, header.Height - 1,
                header.Width, header.Height - 1);
        };

        var lblTitle = new Label
        {
            Text = _isEdit ? "Edit Meal Log Entry" : "New Meal Log Entry",
            Font = new System.Drawing.Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(480, 30),
            Location = new Point(20, 10),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Record macronutrient and micronutrient intake for a student.",
            Font = new System.Drawing.Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(480, 18),
            Location = new Point(20, 44),
            TextAlign = ContentAlignment.MiddleLeft
        };

        header.Controls.AddRange(new Control[] { lblTitle, lblSub });
        Controls.Add(header);
    }

    // ── Body ──────────────────────────────────────────────────────────────────

    private void BuildBody()
    {
        var body = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(20),
            AutoScroll = true
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        int row = 0;

        // ── STUDENT ─────────────────
        AddFullWidth(layout, MakeLabel("STUDENT"), ref row);

        _cmbStudent = new ComboBox
        {
            Dock = DockStyle.Top,
            Font = new System.Drawing.Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList
        };

        foreach (var s in _students)
            _cmbStudent.Items.Add(s);

        if (_cmbStudent.Items.Count > 0)
            _cmbStudent.SelectedIndex = 0;

        AddFullWidth(layout, _cmbStudent, ref row);

        // ── DATE + MEAL ─────────────
        layout.Controls.Add(MakeLabel("LOG DATE"), 0, row);
        layout.Controls.Add(MakeLabel("MEAL TYPE"), 1, row);
        row++;

        _dtpDate = new DateTimePicker { Dock = DockStyle.Top };
        _cmbMealType = new ComboBox { Dock = DockStyle.Top };

        _cmbMealType.Items.AddRange(new object[]
        {
        "Breakfast", "Lunch", "Dinner", "Snack", "Other"
        });

        _cmbMealType.SelectedIndex = 0;

        layout.Controls.Add(_dtpDate, 0, row);
        layout.Controls.Add(_cmbMealType, 1, row);
        row++;

        // ── TABS ────────────────────
        AddFullWidth(layout, MakeLabel("NUTRIENTS"), ref row);

        _tabNutrients = new TabControl
        {
            Dock = DockStyle.Fill,
            Height = 280
        };

        _tabNutrients.TabPages.Add(BuildMacroTab());
        _tabNutrients.TabPages.Add(BuildMicroTab());

        AddFullWidth(layout, _tabNutrients, ref row);

        // ── NOTES ───────────────────
        AddFullWidth(layout, MakeLabel("NOTES"), ref row);

        _txtNotes = new TextBox
        {
            Multiline = true,
            Dock = DockStyle.Fill,
            Height = 80
        };

        AddFullWidth(layout, _txtNotes, ref row);

        // ── ERROR ───────────────────
        _lblError = new Label
        {
            ForeColor = ErrorRed,
            Dock = DockStyle.Top,
            Height = 30,
            Visible = false
        };

        AddFullWidth(layout, _lblError, ref row);

        body.Controls.Add(layout);
        Controls.Add(body);
    }


    private void AddFullWidth(TableLayoutPanel layout, Control control, ref int row)
    {
        layout.Controls.Add(control, 0, row);
        layout.SetColumnSpan(control, 2);
        row++;
    }

    private Label MakeLabel(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Top,
            Font = new System.Drawing.Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = LabelColor,
            Height = 18
        };
    }

    private TabPage BuildMacroTab()
    {
        var page = new TabPage("Macronutrients");

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

        string[] labels = {
        "Calories", "Protein",
        "Carbs", "Fats",
        "Fiber"
    };

        TextBox[] inputs = new TextBox[5];

        for (int i = 0; i < labels.Length; i++)
        {
            var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(5) };

            var lbl = new Label
            {
                Text = labels[i],
                Dock = DockStyle.Top
            };

            var txt = new TextBox
            {
                Dock = DockStyle.Top
            };

            panel.Controls.Add(txt);
            panel.Controls.Add(lbl);

            layout.Controls.Add(panel, i % 2, i / 2);
            inputs[i] = txt;
        }

        _txtCalories = inputs[0];
        _txtProtein = inputs[1];
        _txtCarbs = inputs[2];
        _txtFats = inputs[3];
        _txtFiber = inputs[4];

        page.Controls.Add(layout);
        return page;
    }

    private TabPage BuildMicroTab()
    {
        var page = new TabPage("  Micronutrients  ")
        {
            BackColor = TabBg,
            Padding = new Padding(12)
        };

        var fields = new[]
        {
            ("VITAMIN A (mcg)", "e.g. 400"),
            ("VITAMIN C (mg)",  "e.g. 35"),
            ("VITAMIN D (mcg)", "e.g. 10"),
            ("CALCIUM (mg)",    "e.g. 700"),
            ("IRON (mg)",       "e.g. 10"),
            ("ZINC (mg)",       "e.g. 7"),
        };

        var inputs = new TextBox[6];
        int col = 0, row = 0;

        for (int i = 0; i < fields.Length; i++)
        {
            int x = 12 + col * 240;
            int y = 12 + row * 64;

            page.Controls.Add(MakeLabel(fields[i].Item1, new Point(x, y)));

            inputs[i] = MakeNumericBox(fields[i].Item2, new Point(x, y + 20));
            page.Controls.Add(inputs[i]);

            col++;
            if (col >= 2) { col = 0; row++; }
        }

        _txtVitA = inputs[0];
        _txtVitC = inputs[1];
        _txtVitD = inputs[2];
        _txtCalcium = inputs[3];
        _txtIron = inputs[4];
        _txtZinc = inputs[5];

        return page;
    }

    // ── Footer ────────────────────────────────────────────────────────────────

    private void BuildFooter()
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = CardBg,
            Padding = new Padding(20, 10, 20, 10)
        };

        footer.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            Font = new System.Drawing.Font("Segoe UI", 9.5f),
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = TextMid,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 38),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel
        };
        _btnCancel.FlatAppearance.BorderSize = 1;
        _btnCancel.FlatAppearance.BorderColor = BorderLight;

        _btnSave = new Button
        {
            Text = _isEdit ? "Save Changes" : "Add Log",
            Font = new System.Drawing.Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = TealAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(120, 38),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.MouseEnter += (_, _) => _btnSave.BackColor = TealHover;
        _btnSave.MouseLeave += (_, _) => _btnSave.BackColor = TealAccent;
        _btnSave.Click += async (_, _) => await SaveAsync();

        footer.Controls.AddRange(new Control[] { _btnCancel, _btnSave });

        footer.Resize += (_, _) =>
        {
            _btnSave.Location = new Point(footer.Width - _btnSave.Width - 20, 11);
            _btnCancel.Location = new Point(
                footer.Width - _btnSave.Width - _btnCancel.Width - 28, 11);
        };

        Controls.Add(footer);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Populate (Edit Mode)
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateFields()
    {
        // Select student
        for (int i = 0; i < _cmbStudent.Items.Count; i++)
        {
            if (_cmbStudent.Items[i] is StudentDto s &&
                s.Id == _existing!.StudentId)
            {
                _cmbStudent.SelectedIndex = i;
                break;
            }
        }

        _dtpDate.Value = _existing!.LogDate;

        // Meal type
        int mealIdx = _cmbMealType.Items.IndexOf(_existing.MealType);
        _cmbMealType.SelectedIndex = mealIdx >= 0 ? mealIdx : 0;

        // Macros
        _txtCalories.Text = _existing.CaloriesKcal.ToString("F1");
        _txtProtein.Text = _existing.ProteinG.ToString("F1");
        _txtCarbs.Text = _existing.CarbohydratesG.ToString("F1");
        _txtFats.Text = _existing.FatsG.ToString("F1");
        _txtFiber.Text = _existing.FiberG.ToString("F1");

        // Micros
        _txtVitA.Text = _existing.VitaminAMcg.ToString("F1");
        _txtVitC.Text = _existing.VitaminCMg.ToString("F1");
        _txtVitD.Text = _existing.VitaminDMcg.ToString("F1");
        _txtCalcium.Text = _existing.CalciumMg.ToString("F1");
        _txtIron.Text = _existing.IronMg.ToString("F1");
        _txtZinc.Text = _existing.ZincMg.ToString("F1");

        _txtNotes.Text = _existing.Notes ?? string.Empty;

        _cmbStudent.Enabled = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Save
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        ClearError();

        if (_cmbStudent.SelectedItem is not StudentDto student)
        {
            ShowError("Please select a student.");
            return;
        }

        if (!TryParseDouble(_txtCalories.Text, out double calories) ||
            !TryParseDouble(_txtProtein.Text, out double protein) ||
            !TryParseDouble(_txtCarbs.Text, out double carbs) ||
            !TryParseDouble(_txtFats.Text, out double fats) ||
            !TryParseDouble(_txtFiber.Text, out double fiber))
        {
            ShowError("Macronutrient fields must be valid numbers (e.g. 25.5).");
            _tabNutrients.SelectedIndex = 0;
            return;
        }

        if (!TryParseDouble(_txtVitA.Text, out double vitA) ||
            !TryParseDouble(_txtVitC.Text, out double vitC) ||
            !TryParseDouble(_txtVitD.Text, out double vitD) ||
            !TryParseDouble(_txtCalcium.Text, out double calcium) ||
            !TryParseDouble(_txtIron.Text, out double iron) ||
            !TryParseDouble(_txtZinc.Text, out double zinc))
        {
            ShowError("Micronutrient fields must be valid numbers (e.g. 10.0).");
            _tabNutrients.SelectedIndex = 1;
            return;
        }

        var dto = new MealLogDto
        {
            Id = _isEdit ? _existing!.Id : 0,
            StudentId = student.Id,
            LogDate = _dtpDate.Value.Date,
            MealType = _cmbMealType.SelectedItem?.ToString() ?? "Other",
            CaloriesKcal = calories,
            ProteinG = protein,
            CarbohydratesG = carbs,
            FatsG = fats,
            FiberG = fiber,
            VitaminAMcg = vitA,
            VitaminCMg = vitC,
            VitaminDMcg = vitD,
            CalciumMg = calcium,
            IronMg = iron,
            ZincMg = zinc,
            Notes = string.IsNullOrWhiteSpace(_txtNotes.Text)
                             ? null : _txtNotes.Text.Trim()
        };

        _btnSave.Enabled = false;
        _btnSave.Text = "Saving…";

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IMealLogService>();

            var (success, message) = _isEdit
                ? await svc.UpdateLogAsync(dto)
                : await svc.AddLogAsync(dto);

            if (success)
            {
                SerilogLog.Information("{Action} meal log for student {Id}",
                    _isEdit ? "Updated" : "Added", student.Id);
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                ShowError(message);
            }
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to save meal log.");
            ShowError("An unexpected error occurred. Please try again.");
        }
        finally
        {
            _btnSave.Enabled = true;
            _btnSave.Text = _isEdit ? "Save Changes" : "Add Log";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private void ShowError(string message)
    {
        _lblError.Text = "⚠  " + message;
        _lblError.Visible = true;
    }

    private void ClearError()
    {
        _lblError.Text = string.Empty;
        _lblError.Visible = false;
    }

    private static bool TryParseDouble(string text, out double value)
    {
        text = text.Trim();
        if (string.IsNullOrEmpty(text)) { value = 0; return true; }
        return double.TryParse(text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out value) && value >= 0;
    }

    private static TextBox MakeNumericBox(string placeholder, Point location)
        => new()
        {
            Font = new System.Drawing.Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(22, 32, 50),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder,
            Location = location,
            Size = new Size(210, 34),
            TextAlign = HorizontalAlignment.Right
        };

    private static Label MakeLabel(string text, Point location) => new()
    {
        Text = text,
        Font = new System.Drawing.Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = LabelColor,
        AutoSize = false,
        Size = new Size(220, 18),
        Location = location,
        TextAlign = ContentAlignment.BottomLeft
    };
}
