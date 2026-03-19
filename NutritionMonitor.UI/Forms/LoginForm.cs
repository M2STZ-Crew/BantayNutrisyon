// File Path: NutritionMonitor.UI/Forms/LoginForm.cs
using NutritionMonitor.Models.DTOs;
using NutritionMonitor.Models.Interfaces;
using NutritionMonitor.UI.Session;
using Microsoft.Extensions.DependencyInjection;
using SerilogLog = Serilog.Log;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Threading.Tasks;

namespace NutritionMonitor.UI.Forms;

public class LoginForm : Form
{
    // ── Controls ────────────────────────────────────────────────────────────
    private Panel _leftPanel = null!;
    private Panel _rightPanel = null!;
    private TableLayoutPanel _formLayout = null!;

    private Label _brandTitle = null!;
    private Label _brandSubtitle = null!;
    private Label _brandTagline = null!;

    private Label _welcomeLabel = null!;
    private Label _instructionLabel = null!;

    private Label _emailLabel = null!;
    private TextBox _emailBox = null!;

    private Label _passwordLabel = null!;
    private TextBox _passwordBox = null!;
    private Button _togglePasswordBtn = null!;
    private Panel _passwordRow = null!;

    private Label _errorLabel = null!;
    private Button _loginButton = null!;
    private Label _versionLabel = null!;

    // ── Colors ───────────────────────────────────────────────────────────────
    private static readonly Color NavyDark = Color.FromArgb(15, 40, 70);
    private static readonly Color NavyMid = Color.FromArgb(20, 55, 95);
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 150);
    private static readonly Color White = Color.White;
    private static readonly Color OffWhite = Color.FromArgb(248, 250, 252);
    private static readonly Color TextDark = Color.FromArgb(30, 40, 55);
    private static readonly Color TextMuted = Color.FromArgb(110, 125, 145);
    private static readonly Color ErrorRed = Color.FromArgb(200, 50, 50);
    private static readonly Color InputBorder = Color.FromArgb(210, 220, 230);
    private static readonly Color InputFocus = Color.FromArgb(0, 168, 150);

    // ── State ────────────────────────────────────────────────────────────────
    private bool _passwordVisible = false;
    private bool _isLoading = false;

    public LoginForm()
    {
        InitializeComponent();
        WireEvents();
    }

    // ────────────────────────────────────────────────────────────────────────
    //  UI Construction
    // ────────────────────────────────────────────────────────────────────────

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = "NutritionMonitor — Login";
        Size = new Size(860, 540);
        MinimumSize = new Size(700, 460); // Adjusted per Rule 8
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = NavyDark;
        Font = new Font("Segoe UI", 9.5f);

        // ROOT LAYOUT - Prevents any overlap between left and right panels
        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 320f));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildLeftPanel(rootLayout);
        BuildRightPanel(rootLayout);

        Controls.Add(rootLayout);

        ResumeLayout(false);
        PerformLayout();
    }

    private void BuildLeftPanel(TableLayoutPanel root)
    {
        _leftPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = NavyDark,
            Margin = new Padding(0)
        };

        var leftLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 7,
            Padding = new Padding(36, 48, 28, 32),
            Margin = new Padding(0)
        };
        leftLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        leftLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f)); // Pushes tagline to the bottom
        leftLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // Decorative top accent bar
        var accentBar = new Panel
        {
            Height = 4,
            Width = 48,
            BackColor = TealAccent,
            Margin = new Padding(0, 0, 0, 16)
        };

        _brandTitle = new Label
        {
            Text = "Nutrition\nMonitor",
            Font = new Font("Segoe UI", 26f, FontStyle.Bold),
            ForeColor = White,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8),
            TextAlign = ContentAlignment.TopLeft
        };

        _brandSubtitle = new Label
        {
            Text = "Student Health Tracking System",
            Font = new Font("Segoe UI", 10f, FontStyle.Regular),
            ForeColor = Color.FromArgb(160, 200, 220),
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 24)
        };

        // Divider
        var divider = new Panel
        {
            Height = 1,
            BackColor = Color.FromArgb(50, 100, 140),
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 24)
        };

        // Feature bullets
        var features = new[]
        {
            ("◆", "Malnutrition Risk Classification"),
            ("◆", "DOH RENI Nutrient Standards"),
            ("◆", "Meal Log & Deficit Analysis"),
            ("◆", "Visual Nutrition Reports")
        };

        var featuresLayout = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = features.Length,
            AutoSize = true,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Left | AnchorStyles.Right
        };
        featuresLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 20f));
        featuresLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        for (int i = 0; i < features.Length; i++)
        {
            featuresLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));

            var iconLbl = new Label
            {
                Text = features[i].Item1,
                Font = new Font("Segoe UI", 7f),
                ForeColor = TealAccent,
                AutoSize = false,
                Size = new Size(16, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };
            var textLbl = new Label
            {
                Text = features[i].Item2,
                Font = new Font("Segoe UI", 9f),
                ForeColor = Color.FromArgb(180, 210, 230),
                AutoSize = false,
                Size = new Size(228, 20),
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0)
            };

            featuresLayout.Controls.Add(iconLbl, 0, i);
            featuresLayout.Controls.Add(textLbl, 1, i);
        }

        _brandTagline = new Label
        {
            Text = "Department of Health · RENI 2015",
            Font = new Font("Segoe UI", 8f, FontStyle.Italic),
            ForeColor = Color.FromArgb(90, 130, 160),
            AutoSize = true,
            Margin = new Padding(0),
            Anchor = AnchorStyles.Bottom | AnchorStyles.Left
        };

        leftLayout.Controls.Add(accentBar, 0, 0);
        leftLayout.Controls.Add(_brandTitle, 0, 1);
        leftLayout.Controls.Add(_brandSubtitle, 0, 2);
        leftLayout.Controls.Add(divider, 0, 3);
        leftLayout.Controls.Add(featuresLayout, 0, 4);
        leftLayout.Controls.Add(_brandTagline, 0, 6);

        _leftPanel.Controls.Add(leftLayout);
        root.Controls.Add(_leftPanel, 0, 0); // Placed safely in column 0
    }

    private void BuildRightPanel(TableLayoutPanel root)
    {
        _rightPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = OffWhite,
            Margin = new Padding(0)
        };

        // Outer layout to vertically and horizontally center the form inside column 1
        var rightLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Margin = new Padding(0)
        };
        rightLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rightLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _formLayout = new TableLayoutPanel
        {
            ColumnCount = 1,
            RowCount = 9,
            AutoSize = true,
            Width = 360,
            Anchor = AnchorStyles.None, // Centers perfectly inside rightLayout
            Margin = new Padding(0)
        };
        _formLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        for (int i = 0; i < 9; i++) _formLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // ── Welcome text ──────────────────────────────────────────────────
        _welcomeLabel = new Label
        {
            Text = "Welcome back",
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };

        _instructionLabel = new Label
        {
            Text = "Sign in your account to continue",
            Font = new Font("Segoe UI", 10f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 32)
        };

        // ── Email field ───────────────────────────────────────────────────
        _emailLabel = MakeFieldLabel("Email address");
        _emailBox = MakeTextBox();
        _emailBox.Name = "emailBox";
        _emailBox.TabIndex = 0;

        // ── Password field ────────────────────────────────────────────────
        _passwordLabel = MakeFieldLabel("Password");

        _passwordRow = new Panel
        {
            Height = 40,
            BackColor = White,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 8)
        };
        StyleInputPanel(_passwordRow);

        var pwdLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        pwdLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        pwdLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 50f));
        pwdLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        _passwordBox = new TextBox
        {
            PasswordChar = '●',
            BorderStyle = BorderStyle.None,
            Font = new Font("Segoe UI", 10.5f),
            ForeColor = TextDark,
            BackColor = White,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(8, 10, 0, 0),
            TabIndex = 1
        };

        _togglePasswordBtn = new Button
        {
            Text = "Show",
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextMuted,
            BackColor = White,
            Dock = DockStyle.Fill,
            Margin = new Padding(0),
            TabStop = false,
            Cursor = Cursors.Hand
        };
        _togglePasswordBtn.FlatAppearance.BorderSize = 0;

        pwdLayout.Controls.Add(_passwordBox, 0, 0);
        pwdLayout.Controls.Add(_togglePasswordBtn, 1, 0);
        _passwordRow.Controls.Add(pwdLayout);

        // ── Error label ───────────────────────────────────────────────────
        _errorLabel = new Label
        {
            Text = string.Empty,
            Font = new Font("Segoe UI", 9f),
            ForeColor = ErrorRed,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 16),
            Visible = false
        };

        // ── Login button ──────────────────────────────────────────────────
        _loginButton = new Button
        {
            Text = "Sign In",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = White,
            BackColor = TealAccent,
            FlatStyle = FlatStyle.Flat,
            Height = 44,
            Anchor = AnchorStyles.Left | AnchorStyles.Right,
            Margin = new Padding(0, 0, 0, 32),
            TabIndex = 2,
            Cursor = Cursors.Hand
        };
        _loginButton.FlatAppearance.BorderSize = 0;

        // ── Version label ─────────────────────────────────────────────────
        _versionLabel = new Label
        {
            Text = "NutritionMonitor v1.0  ·  .NET 10  ·  SQLite",
            Font = new Font("Segoe UI", 8f),
            ForeColor = Color.FromArgb(190, 200, 210),
            AutoSize = true,
            Margin = new Padding(0)
        };

        _formLayout.Controls.Add(_welcomeLabel, 0, 0);
        _formLayout.Controls.Add(_instructionLabel, 0, 1);
        _formLayout.Controls.Add(_emailLabel, 0, 2);
        _formLayout.Controls.Add(_emailBox, 0, 3);
        _formLayout.Controls.Add(_passwordLabel, 0, 4);
        _formLayout.Controls.Add(_passwordRow, 0, 5);
        _formLayout.Controls.Add(_errorLabel, 0, 6);
        _formLayout.Controls.Add(_loginButton, 0, 7);
        _formLayout.Controls.Add(_versionLabel, 0, 8);

        rightLayout.Controls.Add(_formLayout, 0, 0);
        _rightPanel.Controls.Add(rightLayout);

        root.Controls.Add(_rightPanel, 1, 0); // Placed safely in column 1
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Event Wiring
    // ────────────────────────────────────────────────────────────────────────

    private void WireEvents()
    {
        _loginButton.Click += async (_, _) => await HandleLoginAsync();
        _togglePasswordBtn.Click += TogglePasswordVisibility;
        _passwordBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) await HandleLoginAsync();
        };
        _emailBox.KeyDown += async (_, e) =>
        {
            if (e.KeyCode == Keys.Enter) _passwordBox.Focus();
        };

        // Button hover effects
        _loginButton.MouseEnter += (_, _) =>
            _loginButton.BackColor = Color.FromArgb(0, 148, 132);
        _loginButton.MouseLeave += (_, _) =>
            _loginButton.BackColor = TealAccent;
    }

    private void WireInputFocusEffects()
    {
        StyleInputFocus(_emailBox, null);
        StyleInputFocus(_passwordBox, _passwordRow);
    }

    private void StyleInputFocus(TextBox box, Panel? parentPanel)
    {
        Control target = (Control?)parentPanel ?? (Control)box;

        box.Enter += (_, _) =>
        {
            target.BackColor = White;
            if (parentPanel != null)
                StyleInputPanelFocused(parentPanel);
            else
                box.BackColor = White;
        };

        box.Leave += (_, _) =>
        {
            if (parentPanel != null)
                StyleInputPanel(parentPanel);
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    //  Login Logic
    // ────────────────────────────────────────────────────────────────────────

    private async Task HandleLoginAsync()
    {
        if (_isLoading) return;

        ClearError();

        var email = _emailBox.Text.Trim();
        var password = _passwordBox.Text;

        if (string.IsNullOrWhiteSpace(email))
        {
            ShowError("Please enter your email address.");
            _emailBox.Focus();
            return;
        }

        if (string.IsNullOrWhiteSpace(password))
        {
            ShowError("Please enter your password.");
            _passwordBox.Focus();
            return;
        }

        await SetLoadingStateAsync(true);

        try
        {
            using var scope = ServiceLocator.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();

            var loginDto = new LoginDto
            {
                Email = email,
                Password = password
            };

            var user = await authService.LoginAsync(loginDto);

            if (user == null)
            {
                SerilogLog.Warning("Failed login attempt for email: {Email}", email);
                ShowError("Invalid email or password. Please try again.");
                _passwordBox.Clear();
                _passwordBox.Focus();
                return;
            }

            SerilogLog.Information(
                "User logged in: {FullName} ({Email}) — Role: {Role}",
                user.FullName, user.Email, user.Role);

            SessionManager.SetUser(user);

            var dashboard = new DashboardForm();
            dashboard.Show();
            Hide();

            dashboard.FormClosed += (_, _) =>
            {
                SessionManager.Clear();
                _emailBox.Clear();
                _passwordBox.Clear();
                ClearError();
                Show();
            };
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Unexpected error during login for email: {Email}", email);
            ShowError("An unexpected error occurred. Please try again.");
        }
        finally
        {
            await SetLoadingStateAsync(false);
        }
    }

    private void TogglePasswordVisibility(object? sender, EventArgs e)
    {
        _passwordVisible = !_passwordVisible;
        _passwordBox.PasswordChar = _passwordVisible ? '\0' : '●';
        _togglePasswordBtn.Text = _passwordVisible ? "Hide" : "Show";
        _togglePasswordBtn.ForeColor = _passwordVisible ? TealAccent : TextMuted;
    }

    // ────────────────────────────────────────────────────────────────────────
    //  UI Helpers
    // ────────────────────────────────────────────────────────────────────────

    private void ShowError(string message)
    {
        _errorLabel.Text = "⚠  " + message;
        _errorLabel.Visible = true;
    }

    private void ClearError()
    {
        _errorLabel.Text = string.Empty;
        _errorLabel.Visible = false;
    }

    private async Task SetLoadingStateAsync(bool loading)
    {
        _isLoading = loading;
        _loginButton.Enabled = !loading;
        _emailBox.Enabled = !loading;
        _passwordBox.Enabled = !loading;
        _loginButton.Text = loading ? "Signing in…" : "Sign In";
        _loginButton.BackColor = loading
            ? Color.FromArgb(100, 170, 160)
            : TealAccent;

        await Task.Yield();
    }

    private static Label MakeFieldLabel(string text) => new()
    {
        Text = text.ToUpperInvariant(),
        Font = new Font("Segoe UI", 8f, FontStyle.Bold),
        ForeColor = TextMuted,
        AutoSize = true,
        Margin = new Padding(0, 0, 0, 4),
        TextAlign = ContentAlignment.MiddleLeft
    };

    private static TextBox MakeTextBox() => new()
    {
        Font = new Font("Segoe UI", 10.5f),
        ForeColor = TextDark,
        BackColor = White,
        BorderStyle = BorderStyle.FixedSingle,
        Anchor = AnchorStyles.Left | AnchorStyles.Right,
        AutoSize = false,
        Height = 40,
        Margin = new Padding(0, 0, 0, 16)
    };

    private static void StyleInputPanel(Panel p)
    {
        p.BackColor = White;
        p.Paint -= PaintFocusedBorder;
        p.Paint += PaintNormalBorder;
        p.Invalidate();
    }

    private static void StyleInputPanelFocused(Panel p)
    {
        p.BackColor = White;
        p.Paint -= PaintNormalBorder;
        p.Paint += PaintFocusedBorder;
        p.Invalidate();
    }

    private static void PaintNormalBorder(object? sender, PaintEventArgs e)
    {
        if (sender is Panel p)
            ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle,
                InputBorder, ButtonBorderStyle.Solid);
    }

    private static void PaintFocusedBorder(object? sender, PaintEventArgs e)
    {
        if (sender is Panel p)
            ControlPaint.DrawBorder(e.Graphics, p.ClientRectangle,
                InputFocus, ButtonBorderStyle.Solid);
    }
}