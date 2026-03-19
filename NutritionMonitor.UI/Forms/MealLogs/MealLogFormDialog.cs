// PHASE 2 FIX — MealLogFormDialog.cs
// Changes made:
//   [FIX #1] Removed: using HarfBuzzSharp;
//            → HarfBuzz is a font shaping engine library. Nothing to do with this form.
//              If the package isn't installed it causes a compile error.
//
//   [FIX #2] Removed: using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
//            → This is an EF Core internal logging class. Not used anywhere in this file.
//
//   [FIX #3] Removed: using Microsoft.EntityFrameworkCore;
//            → Not used in a UI dialog. Repositories handle EF Core, not forms.
//
//   [FIX #4] Removed: using NutritionMonitor.Models.Entities;
//            → This form works with DTOs only (MealLogDto, StudentDto).
//              It never touches Entity classes directly.
//
//   [FIX #5] Removed: using System.ComponentModel.DataAnnotations;
//            → DataAnnotations (like [Required], [Range]) are not used anywhere
//              in this file. Validation is done manually via TryParseDouble().

using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Interfaces;
using System.Drawing;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        MinimumSize = new Size(480, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        AutoSize = false;
        MinimizeBox = false;
        BackColor = BgColor;
        Font = new System.Drawing.Font("Segoe UI", 9.5f);

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72f));   // Header
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Body
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 60f));   // Footer

        BuildHeader(rootLayout);
        BuildBody(rootLayout);
        BuildFooter(rootLayout);

        Controls.Add(rootLayout);

        ResumeLayout(false);
        PerformLayout();
    }

    // ── Header ────────────────────────────────────────────────────────────────

    private void BuildHeader(TableLayoutPanel root)
    {
        var headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(0)
        };

        headerPanel.Paint += (s, e) =>
        {
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, 4, headerPanel.Height);
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, headerPanel.Height - 1,
                headerPanel.Width, headerPanel.Height - 1);
        };

        var headerLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(20, 12, 20, 12)
        };
        headerLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        headerLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lblTitle = new Label
        {
            Text = _isEdit ? "Edit Meal Log Entry" : "New Meal Log Entry",
            Font = new System.Drawing.Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft
        };

        var lblSub = new Label
        {
            Text = "Record macronutrient and micronutrient intake for a student.",
            Font = new System.Drawing.Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        headerLayout.Controls.Add(lblTitle, 0, 0);
        headerLayout.Controls.Add(lblSub, 0, 1);
        headerPanel.Controls.Add(headerLayout);

        root.Controls.Add(headerPanel, 0, 0);
    }

    // ── Body ──────────────────────────────────────────────────────────────────

    private void BuildBody(TableLayoutPanel root)
    {
        var scrollBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            AutoScroll = true,
            Margin = new Padding(0)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };

        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        int row = 0;
        void AddRow() => layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ── STUDENT ──────────────────────────────────────────────────────────
        AddRow();
        var lblStudent = MakeLabel("STUDENT");
        lblStudent.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblStudent, 0, row);
        layout.SetColumnSpan(lblStudent, 2);
        row++;

        AddRow();
        _cmbStudent = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Font = new System.Drawing.Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 0, 16),
            DisplayMember = "FullName"
        };
        foreach (var s in _students) _cmbStudent.Items.Add(s);
        if (_cmbStudent.Items.Count > 0) _cmbStudent.SelectedIndex = 0;
        layout.Controls.Add(_cmbStudent, 0, row);
        layout.SetColumnSpan(_cmbStudent, 2);
        row++;

        // ── DATE + MEAL TYPE ─────────────────────────────────────────────────
        AddRow();
        var lblDate = MakeLabel("LOG DATE");
        lblDate.Margin = new Padding(0, 0, 8, 4);
        var lblMeal = MakeLabel("MEAL TYPE");
        lblMeal.Margin = new Padding(8, 0, 0, 4);
        layout.Controls.Add(lblDate, 0, row);
        layout.Controls.Add(lblMeal, 1, row);
        row++;

        AddRow();
        _dtpDate = new DateTimePicker
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Format = DateTimePickerFormat.Short,
            Margin = new Padding(0, 0, 8, 16)
        };
        _cmbMealType = new ComboBox
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(8, 0, 0, 16)
        };
        _cmbMealType.Items.AddRange(new object[]
        {
            "Breakfast", "Lunch", "Dinner", "Snack", "Other"
        });
        _cmbMealType.SelectedIndex = 0;
        layout.Controls.Add(_dtpDate, 0, row);
        layout.Controls.Add(_cmbMealType, 1, row);
        row++;

        // ── NUTRIENT TABS ─────────────────────────────────────────────────────
        AddRow();
        var lblNutrients = MakeLabel("NUTRIENTS");
        lblNutrients.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblNutrients, 0, row);
        layout.SetColumnSpan(lblNutrients, 2);
        row++;

        AddRow();
        _tabNutrients = new TabControl
        {
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 280,
            Margin = new Padding(0, 0, 0, 16)
        };
        _tabNutrients.TabPages.Add(BuildMacroTab());
        _tabNutrients.TabPages.Add(BuildMicroTab());
        layout.Controls.Add(_tabNutrients, 0, row);
        layout.SetColumnSpan(_tabNutrients, 2);
        row++;

        // ── NOTES ─────────────────────────────────────────────────────────────
        AddRow();
        var lblNotes = MakeLabel("NOTES");
        lblNotes.Margin = new Padding(0, 0, 0, 4);
        layout.Controls.Add(lblNotes, 0, row);
        layout.SetColumnSpan(lblNotes, 2);
        row++;

        AddRow();
        _txtNotes = new TextBox
        {
            Multiline = true,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 80,
            Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(_txtNotes, 0, row);
        layout.SetColumnSpan(_txtNotes, 2);
        row++;

        // ── ERROR ─────────────────────────────────────────────────────────────
        AddRow();
        _lblError = new Label
        {
            ForeColor = ErrorRed,
            BackColor = ErrorLight,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 30,
            Visible = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 16)
        };
        layout.Controls.Add(_lblError, 0, row);
        layout.SetColumnSpan(_lblError, 2);

        scrollBody.Controls.Add(layout);
        root.Controls.Add(scrollBody, 0, 1);
    }

    private TabPage BuildMacroTab()
    {
        var page = new TabPage("Macronutrients")
        {
            BackColor = TabBg,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(MakeFieldBlock("CALORIES", "e.g. 500", out _txtCalories), 0, 0);
        layout.Controls.Add(MakeFieldBlock("PROTEIN (g)", "e.g. 30", out _txtProtein), 1, 0);
        layout.Controls.Add(MakeFieldBlock("CARBS (g)", "e.g. 50", out _txtCarbs), 0, 1);
        layout.Controls.Add(MakeFieldBlock("FATS (g)", "e.g. 15", out _txtFats), 1, 1);
        layout.Controls.Add(MakeFieldBlock("FIBER (g)", "e.g. 8", out _txtFiber), 0, 2);

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

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 3,
            AutoSize = true
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        layout.Controls.Add(MakeFieldBlock("VITAMIN A (mcg)", "e.g. 400", out _txtVitA), 0, 0);
        layout.Controls.Add(MakeFieldBlock("VITAMIN C (mg)", "e.g. 35", out _txtVitC), 1, 0);
        layout.Controls.Add(MakeFieldBlock("VITAMIN D (mcg)", "e.g. 10", out _txtVitD), 0, 1);
        layout.Controls.Add(MakeFieldBlock("CALCIUM (mg)", "e.g. 700", out _txtCalcium), 1, 1);
        layout.Controls.Add(MakeFieldBlock("IRON (mg)", "e.g. 10", out _txtIron), 0, 2);
        layout.Controls.Add(MakeFieldBlock("ZINC (mg)", "e.g. 7", out _txtZinc), 1, 2);

        page.Controls.Add(layout);
        return page;
    }

    private TableLayoutPanel MakeFieldBlock(
        string labelText, string placeholder, out TextBox txt)
    {
        var block = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            AutoSize = true,
            Margin = new Padding(4, 4, 4, 8)
        };
        block.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        block.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        block.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var lbl = MakeLabel(labelText);
        lbl.Margin = new Padding(0, 0, 0, 4);

        txt = new TextBox
        {
            Font = new System.Drawing.Font("Segoe UI", 10f),
            ForeColor = Color.FromArgb(22, 32, 50),
            BackColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Height = 34,
            TextAlign = HorizontalAlignment.Right,
            Margin = new Padding(0)
        };

        block.Controls.Add(lbl, 0, 0);
        block.Controls.Add(txt, 0, 1);
        return block;
    }

    private Label MakeLabel(string text) => new()
    {
        Text = text,
        Font = new System.Drawing.Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = LabelColor,
        AutoSize = true,
        TextAlign = ContentAlignment.BottomLeft
    };

    // ── Footer ────────────────────────────────────────────────────────────────

    private void BuildFooter(TableLayoutPanel root)
    {
        var footerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Padding = new Padding(20, 10, 20, 10),
            Margin = new Padding(0)
        };

        footerPanel.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, 0, footerPanel.Width, 0);
        };

        var btnFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            Font = new System.Drawing.Font("Segoe UI", 9.5f),
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = TextMid,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 38),
            Margin = new Padding(0, 0, 12, 0),
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
            Margin = new Padding(0),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;
        _btnSave.MouseEnter += (_, _) => _btnSave.BackColor = TealHover;
        _btnSave.MouseLeave += (_, _) => _btnSave.BackColor = TealAccent;
        _btnSave.Click += async (_, _) => await SaveAsync();

        btnFlow.Controls.Add(_btnCancel);
        btnFlow.Controls.Add(_btnSave);
        footerPanel.Controls.Add(btnFlow);

        root.Controls.Add(footerPanel, 0, 2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Populate (Edit Mode)
    // ─────────────────────────────────────────────────────────────────────────

    private void PopulateFields()
    {
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

        // Lock student selector in edit mode — student should not change on an existing log
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

        await Task.Yield(); // Let UI render the button state change before DB call

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
        return double.TryParse(
            text,
            System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture,
            out value) && value >= 0;
    }
}