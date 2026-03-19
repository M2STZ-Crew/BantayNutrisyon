// PHASE 6 FIX — StudentFormDialog.cs
// Changes made — junk using directives removed:
//
//   [FIX #1] Removed: using Microsoft.EntityFrameworkCore;
//            → EF Core belongs in DAL only. UI dialogs never touch it.
//
//   [FIX #2] Removed: using ScottPlot.Hatches;
//            → ScottPlot is the charting library. StudentFormDialog is a
//              data-entry form with no charts whatsoever.
//
//   [FIX #3] Removed: using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
//            → EF Core internal logging. Not for UI forms.
//
//   [FIX #4] Removed: using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
//            → Same as above — a sub-namespace of EF Core's internal logger.
//
// Zero logic changes. All form behaviour is identical.

using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using SerilogLog = Serilog.Log;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;

namespace NutritionMonitor.UI.Forms.Students;

public class StudentFormDialog : Form
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

    // ── Controls ──────────────────────────────────────────────────────────────
    private Panel _headerPanel = null!;
    private Label _lblTitle = null!;
    private Label _lblSubtitle = null!;
    private TableLayoutPanel _formTable = null!;
    private TextBox _txtStudentNo = null!;
    private TextBox _txtFirstName = null!;
    private TextBox _txtLastName = null!;
    private DateTimePicker _dtpDob = null!;
    private ComboBox _cmbGender = null!;
    private TextBox _txtGrade = null!;
    private TextBox _txtSection = null!;
    private Label _lblError = null!;
    private Button _btnSave = null!;
    private Button _btnCancel = null!;

    // ── State ─────────────────────────────────────────────────────────────────
    private readonly StudentDto? _existing;
    private readonly bool _isEdit;

    // ─────────────────────────────────────────────────────────────────────────
    public StudentFormDialog(StudentDto? existing)
    {
        _existing = existing;
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

        Text = _isEdit ? "Edit Student" : "Add New Student";
        Size = new Size(520, 560);
        MinimumSize = new Size(480, 400);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BgColor;
        Font = new Font("Segoe UI", 9.5f);

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
        BuildFormBody(rootLayout);
        BuildFooter(rootLayout);

        Controls.Add(rootLayout);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildHeader(TableLayoutPanel root)
    {
        _headerPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = CardBg,
            Margin = new Padding(0)
        };

        _headerPanel.Paint += (s, e) =>
        {
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, 4, _headerPanel.Height);
            using var pen = new Pen(Color.FromArgb(225, 232, 242), 1);
            e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1,
                _headerPanel.Width, _headerPanel.Height - 1);
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

        _lblTitle = new Label
        {
            Text = _isEdit ? "Edit Student Record" : "New Student Record",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 4),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblSubtitle = new Label
        {
            Text = _isEdit
                ? $"Editing: {_existing!.FullName}"
                : "Fill in the student details below. All fields are required.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleLeft
        };

        headerLayout.Controls.Add(_lblTitle, 0, 0);
        headerLayout.Controls.Add(_lblSubtitle, 0, 1);

        _headerPanel.Controls.Add(headerLayout);
        root.Controls.Add(_headerPanel, 0, 0);
    }

    private void BuildFormBody(TableLayoutPanel root)
    {
        var scrollBody = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            AutoScroll = true,
            Margin = new Padding(0)
        };

        _formTable = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            Padding = new Padding(20, 16, 20, 16),
            Margin = new Padding(0)
        };
        _formTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var (lblNo, txtNo) = MakeField("Student Number", "e.g. 2024-00001");
        var (lblFirst, txtFirst) = MakeField("First Name", "e.g. Juan");
        var (lblLast, txtLast) = MakeField("Last Name", "e.g. Dela Cruz");
        var (lblGrade, txtGrade) = MakeField("Grade Level", "e.g. Grade 7, Kinder 1");
        var (lblSection, txtSection) = MakeField("Section", "e.g. Sampaguita");

        _txtStudentNo = txtNo;
        _txtFirstName = txtFirst;
        _txtLastName = txtLast;
        _txtGrade = txtGrade;
        _txtSection = txtSection;

        var lblDob = MakeLabel("Date of Birth");
        _dtpDob = new DateTimePicker
        {
            Format = DateTimePickerFormat.Long,
            Font = new Font("Segoe UI", 10f),
            MaxDate = DateTime.Today,
            MinDate = DateTime.Today.AddYears(-25),
            Value = DateTime.Today.AddYears(-12),
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 0, 0, 16),
            Height = 36
        };

        var lblGender = MakeLabel("Gender");
        _cmbGender = new ComboBox
        {
            Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            FlatStyle = FlatStyle.Flat,
            BackColor = CardBg,
            ForeColor = TextDark,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 0, 0, 16),
            Height = 36
        };
        _cmbGender.Items.AddRange(new object[] { "Male", "Female" });
        _cmbGender.SelectedIndex = 0;

        _lblError = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ErrorRed,
            BackColor = ErrorLight,
            AutoSize = false,
            Height = 28,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 0, 0, 16),
            Visible = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        var rows = new (Control lbl, Control input)[]
        {
            (lblNo,      txtNo),
            (lblFirst,   txtFirst),
            (lblLast,    txtLast),
            (lblDob,     _dtpDob),
            (lblGender,  _cmbGender),
            (lblGrade,   txtGrade),
            (lblSection, txtSection),
        };

        int rowCount = rows.Length * 2 + 1;
        _formTable.RowCount = rowCount;
        for (int i = 0; i < rowCount; i++)
            _formTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        foreach (var (lbl, input) in rows)
        {
            _formTable.Controls.Add(lbl);
            _formTable.Controls.Add(input);
        }

        _formTable.Controls.Add(_lblError);

        scrollBody.Controls.Add(_formTable);
        root.Controls.Add(scrollBody, 0, 1);
    }

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
            using var pen = new Pen(Color.FromArgb(225, 232, 242), 1);
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
            Font = new Font("Segoe UI", 9.5f),
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
            Text = _isEdit ? "Save Changes" : "Add Student",
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            BackColor = TealAccent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(130, 38),
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
        _txtStudentNo.Text = _existing!.StudentNumber;
        _txtFirstName.Text = _existing.FirstName;
        _txtLastName.Text = _existing.LastName;
        _dtpDob.Value = _existing.DateOfBirth;
        _cmbGender.SelectedItem = _existing.Gender.ToString();
        _txtGrade.Text = _existing.GradeLevel;
        _txtSection.Text = _existing.Section;
        _txtStudentNo.ReadOnly = true;
        _txtStudentNo.BackColor = Color.FromArgb(242, 245, 250);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Save Logic
    // ─────────────────────────────────────────────────────────────────────────

    private async Task SaveAsync()
    {
        ClearError();

        var dto = new StudentDto
        {
            Id = _isEdit ? _existing!.Id : 0,
            StudentNumber = _txtStudentNo.Text.Trim(),
            FirstName = _txtFirstName.Text.Trim(),
            LastName = _txtLastName.Text.Trim(),
            DateOfBirth = _dtpDob.Value.Date,
            Gender = _cmbGender.SelectedIndex == 0 ? Gender.Male : Gender.Female,
            GradeLevel = _txtGrade.Text.Trim(),
            Section = _txtSection.Text.Trim()
        };

        _btnSave.Enabled = false;
        _btnSave.Text = "Saving…";
        await Task.Yield();

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var service = scope.ServiceProvider.GetRequiredService<IStudentService>();

            var (success, message) = _isEdit
                ? await service.UpdateStudentAsync(dto)
                : await service.AddStudentAsync(dto);

            if (success)
            {
                SerilogLog.Information("{Action} student: {Name}",
                    _isEdit ? "Updated" : "Added", dto.FullName);
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
            SerilogLog.Error(ex, "Failed to save student.");
            ShowError("An unexpected error occurred. Please try again.");
        }
        finally
        {
            _btnSave.Enabled = true;
            _btnSave.Text = _isEdit ? "Save Changes" : "Add Student";
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Error Handling
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

    // ─────────────────────────────────────────────────────────────────────────
    //  Field Factory Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private static (Label label, TextBox input) MakeField(
        string labelText, string placeholder)
    {
        var lbl = MakeLabel(labelText);
        var txt = new TextBox
        {
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextDark,
            BackColor = CardBg,
            BorderStyle = BorderStyle.FixedSingle,
            PlaceholderText = placeholder,
            AutoSize = false,
            Height = 36,
            Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top,
            Margin = new Padding(0, 0, 0, 16)
        };
        return (lbl, txt);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = LabelColor,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 4)
    };
}