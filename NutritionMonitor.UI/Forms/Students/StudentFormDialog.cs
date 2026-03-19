using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using ScottPlot.Hatches;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;
using SerilogLog = Serilog.Log;

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
        MinimumSize = new Size(480, 520);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = BgColor;
        Font = new Font("Segoe UI", 9.5f);

        BuildHeader();
        BuildFormBody();
        BuildFooter();

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildHeader()
    {
        _headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = CardBg
        };

        _headerPanel.Paint += (s, e) =>
        {
            using var bar = new SolidBrush(TealAccent);
            e.Graphics.FillRectangle(bar, 0, 0, 4, _headerPanel.Height);
            using var pen = new Pen(Color.FromArgb(225, 232, 242), 1);
            e.Graphics.DrawLine(pen, 0, _headerPanel.Height - 1,
                _headerPanel.Width, _headerPanel.Height - 1);
        };

        _lblTitle = new Label
        {
            Text = _isEdit ? "Edit Student Record" : "New Student Record",
            Font = new Font("Segoe UI", 13f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = false,
            Size = new Size(460, 30),
            Location = new Point(20, 12),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _lblSubtitle = new Label
        {
            Text = _isEdit
                ? $"Editing: {_existing!.FullName}"
                : "Fill in the student details below. All fields are required.",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = false,
            Size = new Size(460, 18),
            Location = new Point(20, 46),
            TextAlign = ContentAlignment.MiddleLeft
        };

        _headerPanel.Controls.AddRange(new Control[] { _lblTitle, _lblSubtitle });
        Controls.Add(_headerPanel);
    }

    private void BuildFormBody()
    {
        var bodyPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = BgColor,
            Padding = new Padding(20, 16, 20, 0),
            AutoScroll = true
        };

        // Student Number
        var (lblNo, txtNo) = MakeField("Student Number", "e.g. 2024-00001");
        _txtStudentNo = txtNo;

        // First Name
        var (lblFirst, txtFirst) = MakeField("First Name", "e.g. Juan");
        _txtFirstName = txtFirst;

        // Last Name
        var (lblLast, txtLast) = MakeField("Last Name", "e.g. Dela Cruz");
        _txtLastName = txtLast;

        // Date of Birth
        var lblDob = MakeLabel("Date of Birth");
        _dtpDob = new DateTimePicker
        {
            Format = DateTimePickerFormat.Long,
            Font = new Font("Segoe UI", 10f),
            MaxDate = DateTime.Today,
            MinDate = DateTime.Today.AddYears(-25),
            Value = DateTime.Today.AddYears(-12),
            Width = 440,
            Height = 36
        };

        // Gender
        var lblGender = MakeLabel("Gender");
        _cmbGender = new ComboBox
        {
            Font = new Font("Segoe UI", 10f),
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 440,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = CardBg,
            ForeColor = TextDark
        };
        _cmbGender.Items.AddRange(new object[] { "Male", "Female" });
        _cmbGender.SelectedIndex = 0;

        // Grade Level
        var (lblGrade, txtGrade) = MakeField("Grade Level", "e.g. Grade 7, Kinder 1");
        _txtGrade = txtGrade;

        // Section
        var (lblSection, txtSection) = MakeField("Section", "e.g. Sampaguita");
        _txtSection = txtSection;

        // Error label
        _lblError = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ErrorRed,
            BackColor = ErrorLight,
            AutoSize = false,
            Size = new Size(440, 28),
            Visible = false,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0)
        };

        // Stack all fields vertically
        int y = 0;
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

        foreach (var (lbl, input) in rows)
        {
            lbl.Location = new Point(0, y);
            input.Location = new Point(0, y + 20);
            y += 64;
            bodyPanel.Controls.Add(lbl);
            bodyPanel.Controls.Add(input);
        }

        _lblError.Location = new Point(0, y + 4);
        bodyPanel.Controls.Add(_lblError);

        Controls.Add(bodyPanel);
    }

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
            using var pen = new Pen(Color.FromArgb(225, 232, 242), 1);
            e.Graphics.DrawLine(pen, 0, 0, footer.Width, 0);
        };

        _btnCancel = new Button
        {
            Text = "Cancel",
            Font = new Font("Segoe UI", 9.5f),
            BackColor = Color.FromArgb(240, 244, 248),
            ForeColor = TextMid,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(100, 38),
            Location = new Point(footer.Width - 220, 11),
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
            Location = new Point(footer.Width - 110, 11),
            Cursor = Cursors.Hand
        };
        _btnSave.FlatAppearance.BorderSize = 0;

        _btnSave.MouseEnter += (_, _) => _btnSave.BackColor = TealHover;
        _btnSave.MouseLeave += (_, _) => _btnSave.BackColor = TealAccent;

        footer.Controls.AddRange(new Control[] { _btnCancel, _btnSave });

        // Re-anchor buttons on resize
        footer.Resize += (_, _) =>
        {
            _btnSave.Location = new Point(footer.Width - _btnSave.Width - 20, 11);
            _btnCancel.Location = new Point(footer.Width - _btnSave.Width - _btnCancel.Width - 28, 11);
        };

        _btnSave.Click += async (_, _) => await SaveAsync();
        Controls.Add(footer);
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

        // Lock student number in edit mode
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
            Width = 440,
            Height = 36
        };
        return (lbl, txt);
    }

    private static Label MakeLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        Font = new Font("Segoe UI", 7.5f, FontStyle.Bold),
        ForeColor = LabelColor,
        AutoSize = false,
        Size = new Size(440, 18),
        TextAlign = ContentAlignment.BottomLeft
    };
}
