// File Path: NutritionMonitor.UI/Forms/DashboardForm.cs
using NutritionMonitor.Models.Enums;
using NutritionMonitor.Models.Interfaces;
using NutritionMonitor.UI.Session;
using NutritionMonitor.UI.Forms.Students;
using NutritionMonitor.UI.Forms.MealLogs;
using NutritionMonitor.UI.Forms.Analysis;
using NutritionMonitor.UI.Forms.Charts;
using NutritionMonitor.UI.Forms.Backup;
using NutritionMonitor.UI.Forms.Reports;
using NutritionMonitor.UI.Forms.Logs;
using Microsoft.Extensions.DependencyInjection;
using SerilogLog = Serilog.Log;

namespace NutritionMonitor.UI.Forms;

public class DashboardForm : Form
{
    // ── Palette ───────────────────────────────────────────────────────────────
    private static readonly Color Sidebar = Color.FromArgb(18, 28, 45);
    private static readonly Color SidebarHover = Color.FromArgb(28, 42, 64);
    private static readonly Color SidebarActive = Color.FromArgb(0, 168, 150);
    private static readonly Color SidebarText = Color.FromArgb(185, 200, 220);
    private static readonly Color SidebarMuted = Color.FromArgb(90, 110, 140);
    private static readonly Color TopBar = Color.FromArgb(255, 255, 255);
    private static readonly Color ContentBg = Color.FromArgb(243, 246, 250);
    private static readonly Color CardBg = Color.FromArgb(255, 255, 255);
    private static readonly Color TealAccent = Color.FromArgb(0, 168, 150);
    private static readonly Color TealLight = Color.FromArgb(225, 248, 245);
    private static readonly Color TextDark = Color.FromArgb(22, 32, 50);
    private static readonly Color TextMid = Color.FromArgb(80, 100, 130);
    private static readonly Color TextMuted = Color.FromArgb(140, 160, 185);
    private static readonly Color BorderLight = Color.FromArgb(225, 232, 242);
    private static readonly Color StatBlue = Color.FromArgb(59, 130, 246);
    private static readonly Color StatBlueLight = Color.FromArgb(219, 234, 254);
    private static readonly Color StatAmber = Color.FromArgb(245, 158, 11);
    private static readonly Color StatAmberLight = Color.FromArgb(254, 243, 199);
    private static readonly Color StatRed = Color.FromArgb(220, 60, 60);
    private static readonly Color StatRedLight = Color.FromArgb(254, 226, 226);
    private static readonly Color StatGreen = Color.FromArgb(0, 168, 150);
    private static readonly Color StatGreenLight = Color.FromArgb(209, 250, 229);

    // ── Layout panels ─────────────────────────────────────────────────────────
    private Panel _sidebarPanel = null!;
    private Panel _mainPanel = null!;
    private Panel _topBar = null!;
    private Panel _contentArea = null!;

    // ── Sidebar controls ──────────────────────────────────────────────────────
    private Label _lblAppName = null!;
    private Label _lblAppSub = null!;
    private FlowLayoutPanel _navContainer = null!;
    private Panel _userChip = null!;

    // ── Top bar controls ──────────────────────────────────────────────────────
    private Label _lblPageTitle = null!;
    private Label _lblBreadcrumb = null!;
    private Label _lblUserBadge = null!;
    private Label _lblRoleBadge = null!;

    // ── Stat card value labels (populated after data loads) ───────────────────
    private Label _statStudents = null!;
    private Label _statLogs = null!;
    private Label _statAtRisk = null!;
    private Label _statMal = null!;

    // ── Nav state ─────────────────────────────────────────────────────────────
    private Button? _activeNavBtn = null;
    private string _currentPage = "Dashboard";

    // ─────────────────────────────────────────────────────────────────────────
    private record NavItem(string Icon, string Label, string Page, bool AdminOnly = false);

    private readonly NavItem[] _navItems =
    {
        new("⊞",  "Dashboard",         "Dashboard"),
        new("👤", "Students",          "Students"),
        new("🍽", "Meal Logs",         "MealLogs"),
        new("📊", "Nutrition Analysis","Analysis"),
        new("📈", "Visualizations",    "Charts"),
        new("📄", "Reports",           "Reports"),
        new("💾", "Backup & Restore",  "Backup",  AdminOnly: true),
        new("📋", "Application Logs",  "Logs"),
    };

    // ─────────────────────────────────────────────────────────────────────────
    public DashboardForm()
    {
        BuildForm();
        LoadDashboardContent();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Form Shell
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildForm()
    {
        SuspendLayout();

        Text = "NutritionMonth";
        Size = new Size(1280, 760);
        MinimumSize = new Size(1024, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = ContentBg;
        Font = new Font("Segoe UI", 9.5f);
        FormBorderStyle = FormBorderStyle.Sizable;

        var rootLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230f));
        rootLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildSidebar(rootLayout);
        BuildMainPanel(rootLayout);

        Controls.Add(rootLayout);

        ResumeLayout(false);
        PerformLayout();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Sidebar
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildSidebar(TableLayoutPanel root)
    {
        _sidebarPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Sidebar,
            Margin = new Padding(0)
        };

        var sidebarLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        sidebarLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80f));   // Logo
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 32f));   // Section Label
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));        // Nav Items
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));   // Spacer
        sidebarLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));   // User Chip

        // ── Logo block ────────────────────────────────────────────────────────
        var logoBlock = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 20, 35),
            Margin = new Padding(0),
            Padding = new Padding(20, 20, 12, 0)
        };

        var logoLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Margin = new Padding(0)
        };
        logoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14f));
        logoLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        var accentDot = new Panel
        {
            Size = new Size(6, 32),
            BackColor = TealAccent,
            Margin = new Padding(0, 4, 8, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        logoLayout.SetRowSpan(accentDot, 2);

        _lblAppName = new Label
        {
            Text = "NutritionMonitor",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0)
        };

        _lblAppSub = new Label
        {
            Text = "Student Health System",
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = SidebarMuted,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };

        logoLayout.Controls.Add(accentDot, 0, 0);
        logoLayout.Controls.Add(_lblAppName, 1, 0);
        logoLayout.Controls.Add(_lblAppSub, 1, 1);
        logoBlock.Controls.Add(logoLayout);

        // ── Section label ─────────────────────────────────────────────────────
        var sectionLabel = new Label
        {
            Text = "NAVIGATION",
            Font = new Font("Segoe UI", 7f, FontStyle.Bold),
            ForeColor = SidebarMuted,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.BottomLeft,
            Padding = new Padding(20, 0, 0, 4),
            Margin = new Padding(0)
        };

        // ── Nav items ─────────────────────────────────────────────────────────
        _navContainer = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            BackColor = Sidebar,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0)
        };

        Button? firstBtn = null;

        foreach (var item in _navItems)
        {
            if (item.AdminOnly && !SessionManager.IsAdmin) continue;
            var btn = BuildNavButton(item);
            if (firstBtn == null) firstBtn = btn;
            _navContainer.Controls.Add(btn);
        }

        // ── User chip at bottom ───────────────────────────────────────────────
        _userChip = BuildUserChip();

        sidebarLayout.Controls.Add(logoBlock, 0, 0);
        sidebarLayout.Controls.Add(sectionLabel, 0, 1);
        sidebarLayout.Controls.Add(_navContainer, 0, 2);
        // Row 3 is empty spacer
        sidebarLayout.Controls.Add(_userChip, 0, 4);

        _sidebarPanel.Controls.Add(sidebarLayout);
        root.Controls.Add(_sidebarPanel, 0, 0);

        if (firstBtn != null) ActivateNavButton(firstBtn);
    }

    private Button BuildNavButton(NavItem item)
    {
        var btn = new Button
        {
            Text = $"  {item.Icon}   {item.Label}",
            Tag = item,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = SidebarText,
            BackColor = Sidebar,
            TextAlign = ContentAlignment.MiddleLeft,
            Size = new Size(210, 40),
            Margin = new Padding(0, 0, 0, 6),
            Cursor = Cursors.Hand,
            Padding = new Padding(8, 0, 0, 0)
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = SidebarHover;
        btn.FlatAppearance.MouseDownBackColor = SidebarHover;
        btn.Click += NavButton_Click;
        return btn;
    }

    private Panel BuildUserChip()
    {
        var chip = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.FromArgb(12, 20, 35),
            Margin = new Padding(0),
            Padding = new Padding(16, 10, 16, 10)
        };

        var chipLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 2,
            Margin = new Padding(0)
        };
        chipLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46f));
        chipLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        chipLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 32f));

        var avatar = new Panel
        {
            Size = new Size(38, 38),
            BackColor = TealAccent,
            Margin = new Padding(0, 3, 8, 0),
            Anchor = AnchorStyles.Top | AnchorStyles.Left
        };
        chipLayout.SetRowSpan(avatar, 2);
        avatar.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(TealAccent);
            e.Graphics.FillEllipse(brush, 0, 0, 37, 37);
            string initials = GetInitials(SessionManager.IsLoggedIn
                ? SessionManager.Current.FullName : "?");
            using var font = new Font("Segoe UI", 11f, FontStyle.Bold);
            using var fBrush = new SolidBrush(Color.White);
            var sz = e.Graphics.MeasureString(initials, font);
            e.Graphics.DrawString(initials, font, fBrush,
                (38 - sz.Width) / 2f, (38 - sz.Height) / 2f);
        };

        var lblName = new Label
        {
            Text = SessionManager.IsLoggedIn
                ? SessionManager.Current.FullName : "Unknown",
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 0)
        };

        var lblRole = new Label
        {
            Text = SessionManager.IsLoggedIn
                ? SessionManager.Current.Role.ToString() : string.Empty,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = TealAccent,
            AutoSize = true,
            Margin = new Padding(0, 2, 0, 0)
        };

        var btnLogout = new Button
        {
            Text = "⏻",
            Font = new Font("Segoe UI", 11f),
            ForeColor = SidebarMuted,
            BackColor = Color.Transparent,
            FlatStyle = FlatStyle.Flat,
            Size = new Size(28, 28),
            Margin = new Padding(0, 8, 0, 0),
            Cursor = Cursors.Hand,
            TabStop = false,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        chipLayout.SetRowSpan(btnLogout, 2);
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(30, 255, 255, 255);
        btnLogout.Click += (_, _) =>
        {
            SerilogLog.Information("Logout: {Email}", SessionManager.Current.Email);
            Close();
        };

        chipLayout.Controls.Add(avatar, 0, 0);
        chipLayout.Controls.Add(lblName, 1, 0);
        chipLayout.Controls.Add(lblRole, 1, 1);
        chipLayout.Controls.Add(btnLogout, 2, 0);

        chip.Controls.Add(chipLayout);
        return chip;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Main Panel
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildMainPanel(TableLayoutPanel root)
    {
        _mainPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ContentBg,
            Margin = new Padding(0)
        };

        var mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0)
        };
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 64f));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

        BuildTopBar(mainLayout);
        BuildContentArea(mainLayout);

        _mainPanel.Controls.Add(mainLayout);
        root.Controls.Add(_mainPanel, 1, 0);
    }

    private void BuildTopBar(TableLayoutPanel mainLayout)
    {
        _topBar = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = TopBar,
            Margin = new Padding(0),
            Padding = new Padding(28, 0, 24, 0)
        };

        _topBar.Paint += (s, e) =>
        {
            using var pen = new Pen(BorderLight, 1);
            e.Graphics.DrawLine(pen, 0, _topBar.Height - 1,
                _topBar.Width, _topBar.Height - 1);
        };

        var topLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
        topLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

        // Left: Titles
        var titleLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0),
            Padding = new Padding(0, 10, 0, 0)
        };

        _lblPageTitle = new Label
        {
            Text = "Dashboard",
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 2)
        };

        _lblBreadcrumb = new Label
        {
            Text = "Home  ›  Dashboard",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Margin = new Padding(1, 0, 0, 0)
        };

        titleLayout.Controls.Add(_lblPageTitle, 0, 0);
        titleLayout.Controls.Add(_lblBreadcrumb, 0, 1);

        // Right: Badges
        var badgeLayout = new TableLayoutPanel
        {
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Dock = DockStyle.Fill,
            Margin = new Padding(0)
        };

        _lblUserBadge = new Label
        {
            Text = SessionManager.IsLoggedIn
                ? SessionManager.Current.FullName : string.Empty,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Anchor = AnchorStyles.Right | AnchorStyles.Top | AnchorStyles.Bottom,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 0, 12, 0)
        };

        _lblRoleBadge = new Label
        {
            Text = SessionManager.IsLoggedIn
                ? $"  {SessionManager.Current.Role}  " : string.Empty,
            Font = new Font("Segoe UI", 8f, FontStyle.Bold),
            ForeColor = TealAccent,
            BackColor = TealLight,
            AutoSize = true,
            Anchor = AnchorStyles.Right,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(6, 4, 6, 4),
            Margin = new Padding(0)
        };

        badgeLayout.Controls.Add(_lblUserBadge, 0, 0);
        badgeLayout.Controls.Add(_lblRoleBadge, 1, 0);

        topLayout.Controls.Add(titleLayout, 0, 0);
        topLayout.Controls.Add(badgeLayout, 1, 0);

        _topBar.Controls.Add(topLayout);
        mainLayout.Controls.Add(_topBar, 0, 0);
    }

    private void BuildContentArea(TableLayoutPanel mainLayout)
    {
        _contentArea = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = ContentBg,
            Padding = new Padding(28, 24, 28, 24),
            AutoScroll = true,
            Margin = new Padding(0)
        };

        mainLayout.Controls.Add(_contentArea, 0, 1);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Navigation
    // ─────────────────────────────────────────────────────────────────────────

    private void NavButton_Click(object? sender, EventArgs e)
    {
        if (sender is not Button btn) return;
        if (btn.Tag is not NavItem item) return;
        ActivateNavButton(btn);
        NavigateTo(item.Page, item.Label);
    }

    private void ActivateNavButton(Button btn)
    {
        if (_activeNavBtn != null)
        {
            _activeNavBtn.BackColor = Sidebar;
            _activeNavBtn.ForeColor = SidebarText;
            _activeNavBtn.Font = new Font("Segoe UI", 9.5f);
            _activeNavBtn.Paint -= PaintNavActiveBar;
            _activeNavBtn.Invalidate();
        }

        btn.BackColor = SidebarHover;
        btn.ForeColor = Color.White;
        btn.Font = new Font("Segoe UI", 9.5f, FontStyle.Bold);
        _activeNavBtn = btn;
        btn.Paint += PaintNavActiveBar;
        btn.Invalidate();
    }

    private static void PaintNavActiveBar(object? sender, PaintEventArgs e)
    {
        if (sender is not Button btn) return;
        using var brush = new SolidBrush(TealAccent);
        e.Graphics.FillRectangle(brush, 0, 6, 3, btn.Height - 12);
    }

    private void NavigateTo(string page, string label)
    {
        _currentPage = page;
        _lblPageTitle.Text = label;
        _lblBreadcrumb.Text = $"Home  ›  {label}";
        Text = $"NutritionMonitor — {label}";

        _contentArea.Controls.Clear();

        switch (page)
        {
            case "Dashboard":
                LoadDashboardContent();
                break;

            case "Students":
                var studentPanel = new StudentListForm(_contentArea);
                studentPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(studentPanel);
                break;

            case "MealLogs":
                var mealLogPanel = new MealLogListForm(_contentArea);
                mealLogPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(mealLogPanel);
                break;

            case "Analysis":
                var analysisPanel = new NutritionAnalysisForm(_contentArea);
                analysisPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(analysisPanel);
                break;

            case "Charts":
                var chartsPanel = new ChartsForm(_contentArea);
                chartsPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(chartsPanel);
                break;

            case "Backup":
                var backupPanel = new BackupForm(_contentArea);
                backupPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(backupPanel);
                break;

            case "Reports":
                var reportsPanel = new ReportsForm(_contentArea);
                reportsPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(reportsPanel);
                break;

            case "Logs":
                var logsPanel = new ErrorLogViewerForm(_contentArea);
                logsPanel.Dock = DockStyle.Fill;
                _contentArea.Controls.Add(logsPanel);
                break;

            default:
                LoadComingSoonContent(label);
                break;
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Dashboard Content
    // ─────────────────────────────────────────────────────────────────────────

    private void LoadDashboardContent()
    {
        var dashboardLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 1,
            RowCount = 5,
            Margin = new Padding(0)
        };
        dashboardLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));

        // ── Welcome banner ────────────────────────────────────────────────────
        var banner = BuildWelcomeBanner();
        dashboardLayout.Controls.Add(banner, 0, 0);

        // ── Stat cards row ────────────────────────────────────────────────────
        var statDefs = new[]
        {
            ("Total Students",    "—", "Enrolled students",          StatBlue,  StatBlueLight,  "👤"),
            ("Meal Logs Today",   "—", "Entries logged today",        StatGreen, StatGreenLight, "🍽"),
            ("At-Risk Students",  "—", "Require attention",           StatAmber, StatAmberLight, "⚠"),
            ("Malnourished",      "—", "Critical nutritional deficit",StatRed,   StatRedLight,   "🔴"),
        };

        var cardRow = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 110,
            BackColor = ContentBg,
            Margin = new Padding(0, 20, 0, 0)
        };

        var cards = new List<Panel>();
        var valueLabels = new List<Label>();

        foreach (var (title, value, sub, accent, light, icon) in statDefs)
        {
            var (card, valLbl) = BuildStatCard(title, value, sub, accent, light, icon);
            cards.Add(card);
            valueLabels.Add(valLbl);
            cardRow.Controls.Add(card);
        }

        // Store references so LoadDashboardStatsAsync can update them
        _statStudents = valueLabels[0];
        _statLogs = valueLabels[1];
        _statAtRisk = valueLabels[2];
        _statMal = valueLabels[3];

        void LayoutCards()
        {
            if (cards.Count == 0 || cardRow.Width == 0) return;
            int gap = 16;
            int w = (cardRow.Width - gap * (cards.Count - 1)) / cards.Count;
            if (w < 10) return;
            for (int i = 0; i < cards.Count; i++)
                cards[i].SetBounds(i * (w + gap), 0, w, cardRow.Height);
        }

        cardRow.Resize += (_, _) => LayoutCards();
        dashboardLayout.Controls.Add(cardRow, 0, 1);

        // ── Section: Quick Access ─────────────────────────────────────────────
        var sectionTitle = new Label
        {
            Text = "Quick Access",
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Margin = new Padding(0, 32, 0, 16)
        };
        dashboardLayout.Controls.Add(sectionTitle, 0, 2);

        // ── Quick access tiles ────────────────────────────────────────────────
        // Each tile now carries a Page target for navigation
        var tileDefs = new[]
        {
            ("👤", "Manage Students", "Add, edit, search\nstudent records",  StatBlue,  "Students"),
            ("🍽", "Log Meals",       "Record daily meal\nand nutrient data", StatGreen, "MealLogs"),
            ("📊", "Run Analysis",    "Compute nutritional\ndeficit reports",  StatAmber, "Analysis"),
            ("💾", "Backup Data",     "Export or restore\nsystem data",        StatRed,   "Backup"),
        };

        var tileRow = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 130,
            BackColor = ContentBg,
            Margin = new Padding(0)
        };

        var tileList = new List<Panel>();
        foreach (var (icon, title, desc, color, page) in tileDefs)
        {
            var tile = BuildQuickTile(icon, title, desc, color, page);
            tileList.Add(tile);
            tileRow.Controls.Add(tile);
        }

        void LayoutTiles()
        {
            if (tileList.Count == 0 || tileRow.Width == 0) return;
            int gap = 16;
            int w = (tileRow.Width - gap * (tileList.Count - 1)) / tileList.Count;
            if (w < 10) return;
            for (int i = 0; i < tileList.Count; i++)
                tileList[i].SetBounds(i * (w + gap), 0, w, tileRow.Height);
        }

        tileRow.Resize += (_, _) => LayoutTiles();
        dashboardLayout.Controls.Add(tileRow, 0, 3);

        // ── System info strip ─────────────────────────────────────────────────
        var infoStrip = BuildInfoStrip();
        dashboardLayout.Controls.Add(infoStrip, 0, 4);

        _contentArea.Controls.Add(dashboardLayout);

        // Force initial layout
        LayoutCards();
        LayoutTiles();

        // Load real stat numbers asynchronously — UI stays responsive
        _ = LoadDashboardStatsAsync();
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Stat Card Data Loader
    // ─────────────────────────────────────────────────────────────────────────

    private async Task LoadDashboardStatsAsync()
    {
        try
        {
            using var scope = ServiceLocator.CreateScope();
            var studentSvc = scope.ServiceProvider
                .GetRequiredService<IStudentService>();
            var mealSvc = scope.ServiceProvider
                .GetRequiredService<IMealLogService>();
            var analysisSvc = scope.ServiceProvider
                .GetRequiredService<INutritionAnalysisService>();

            // Total students
            var students = (await studentSvc.GetAllStudentsAsync()).ToList();
            SafeSetText(_statStudents, students.Count.ToString());

            // Meal logs today
            var today = DateTime.Today;
            var todayEnd = today.AddDays(1).AddSeconds(-1);
            var todayLogs = (await mealSvc
                .GetLogsByDateRangeAsync(today, todayEnd)).ToList();
            SafeSetText(_statLogs, todayLogs.Count.ToString());

            // At-risk and malnourished (last 30 days)
            var from = today.AddDays(-30);
            var analyses = (await analysisSvc
                .AnalyzeAllStudentsAsync(from, todayEnd)).ToList();

            int atRisk = analyses.Count(a => a.Status == NutritionStatus.AtRisk);
            int mal = analyses.Count(a => a.Status == NutritionStatus.Malnourished);

            SafeSetText(_statAtRisk, atRisk.ToString());
            SafeSetText(_statMal, mal.ToString());

            SerilogLog.Information(
                "Dashboard stats — Students:{S} LogsToday:{L} AtRisk:{AR} Mal:{M}",
                students.Count, todayLogs.Count, atRisk, mal);
        }
        catch (Exception ex)
        {
            SerilogLog.Error(ex, "Failed to load dashboard stats.");
            // Gracefully show dashes — do not crash the dashboard
            foreach (var lbl in new[] { _statStudents, _statLogs, _statAtRisk, _statMal })
                SafeSetText(lbl, "—");
        }
    }

    /// <summary>
    /// Thread-safe label text setter — guards against disposed labels
    /// when the user navigates away before the async call completes.
    /// </summary>
    private static void SafeSetText(Label? lbl, string text)
    {
        if (lbl == null || lbl.IsDisposed) return;
        if (lbl.InvokeRequired)
            lbl.Invoke(() => lbl.Text = text);
        else
            lbl.Text = text;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Welcome Banner
    // ─────────────────────────────────────────────────────────────────────────

    private Panel BuildWelcomeBanner()
    {
        var banner = new Panel
        {
            Dock = DockStyle.Fill,
            Height = 88,
            BackColor = CardBg,
            Margin = new Padding(0)
        };

        banner.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var stripeBrush = new SolidBrush(TealAccent);
            g.FillRectangle(stripeBrush, 0, 0, 5, banner.Height);
            var rect = new Rectangle(banner.Width - 180, 0, 180, banner.Height);
            using var gradBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                rect, Color.Transparent, TealLight,
                System.Drawing.Drawing2D.LinearGradientMode.Horizontal);
            g.FillRectangle(gradBrush, rect);
            using var pen = new Pen(BorderLight, 1);
            g.DrawRectangle(pen, 0, 0, banner.Width - 1, banner.Height - 1);
        };

        var hour = DateTime.Now.Hour;
        var greeting = hour < 12 ? "Good morning" : hour < 17 ? "Good afternoon" : "Good evening";
        var name = SessionManager.IsLoggedIn
            ? SessionManager.Current.FullName.Split(' ')[0] : "User";

        var lblGreeting = new Label
        {
            Text = $"{greeting}, {name}!",
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Location = new Point(22, 16)
        };

        var lblSub = new Label
        {
            Text = $"Today is {DateTime.Now:dddd, MMMM dd, yyyy}  ·  NutritionMonitor v1.0",
            Font = new Font("Segoe UI", 9f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(22, 52)
        };

        banner.Controls.AddRange(new Control[] { lblGreeting, lblSub });
        return banner;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Stat Card — returns card Panel AND value Label so we can update it later
    // ─────────────────────────────────────────────────────────────────────────

    private static (Panel card, Label valueLabel) BuildStatCard(
        string title, string value, string subtitle,
        Color accent, Color light, string icon)
    {
        var card = new Panel
        {
            BackColor = CardBg
        };

        card.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderLight, 1);
            g.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
            using var bar = new SolidBrush(accent);
            g.FillRectangle(bar, 0, 0, card.Width, 3);
        };

        var iconPanel = new Panel
        {
            Size = new Size(42, 42),
            Location = new Point(16, 28),
            BackColor = light
        };
        iconPanel.Paint += (s, e) =>
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var brush = new SolidBrush(light);
            e.Graphics.FillEllipse(brush, 0, 0, 41, 41);
            using var font = new Font("Segoe UI", 14f);
            var sz = e.Graphics.MeasureString(icon, font);
            e.Graphics.DrawString(icon, font, Brushes.Black,
                (42 - sz.Width) / 2f, (42 - sz.Height) / 2f);
        };

        var lblValue = new Label
        {
            Text = value,
            Font = new Font("Segoe UI", 20f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Location = new Point(70, 16)
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9f, FontStyle.Bold),
            ForeColor = TextMid,
            AutoSize = true,
            Location = new Point(70, 50)
        };

        var lblSub = new Label
        {
            Text = subtitle,
            Font = new Font("Segoe UI", 7.5f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(70, 70)
        };

        card.Controls.AddRange(new Control[] { iconPanel, lblValue, lblTitle, lblSub });

        // Return both the card and the value label so the caller can update it
        return (card, lblValue);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Quick Tile — now accepts a page target and wires click navigation
    // ─────────────────────────────────────────────────────────────────────────

    private Panel BuildQuickTile(
        string icon, string title, string desc,
        Color accent, string page)
    {
        var tile = new Panel
        {
            BackColor = CardBg,
            Cursor = Cursors.Hand
        };

        tile.Paint += (s, e) =>
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            using var pen = new Pen(BorderLight, 1);
            g.DrawRectangle(pen, 0, 0, tile.Width - 1, tile.Height - 1);
        };

        tile.MouseEnter += (_, _) =>
        {
            tile.BackColor = Color.FromArgb(248, 252, 252);
            tile.Invalidate();
        };
        tile.MouseLeave += (_, _) =>
        {
            tile.BackColor = CardBg;
            tile.Invalidate();
        };

        // ── Wire tile click to navigation ─────────────────────────────────────
        tile.Click += (_, _) =>
        {
            NavigateTo(page, title);
            // Also highlight the matching nav button in the sidebar
            ActivateMatchingNavButton(page);
        };

        var lblIcon = new Label
        {
            Text = icon,
            Font = new Font("Segoe UI", 22f),
            ForeColor = accent,
            AutoSize = false,
            Size = new Size(44, 44),
            Location = new Point(16, 16),
            TextAlign = ContentAlignment.MiddleCenter,
            Cursor = Cursors.Hand
        };

        var lblTitle = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
            ForeColor = TextDark,
            AutoSize = true,
            Location = new Point(16, 64),
            Cursor = Cursors.Hand
        };

        var lblDesc = new Label
        {
            Text = desc,
            Font = new Font("Segoe UI", 8f),
            ForeColor = TextMuted,
            AutoSize = true,
            Location = new Point(16, 84),
            Cursor = Cursors.Hand
        };

        // Propagate hover and click from child labels to tile
        foreach (Control child in new Control[] { lblIcon, lblTitle, lblDesc })
        {
            child.MouseEnter += (_, _) =>
            {
                tile.BackColor = Color.FromArgb(248, 252, 252);
                tile.Invalidate();
            };
            child.MouseLeave += (_, _) =>
            {
                tile.BackColor = CardBg;
                tile.Invalidate();
            };
            child.Click += (_, _) =>
            {
                NavigateTo(page, title);
                ActivateMatchingNavButton(page);
            };
        }

        tile.Controls.AddRange(new Control[] { lblIcon, lblTitle, lblDesc });
        return tile;
    }

    /// <summary>
    /// When a quick tile is clicked, also highlight the matching
    /// sidebar nav button so the active state stays in sync.
    /// </summary>
    private void ActivateMatchingNavButton(string page)
    {
        foreach (Control ctrl in _navContainer.Controls)
        {
            if (ctrl is Button btn && btn.Tag is NavItem item && item.Page == page)
            {
                ActivateNavButton(btn);
                return;
            }
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Info Strip
    // ─────────────────────────────────────────────────────────────────────────

    private Panel BuildInfoStrip()
    {
        var strip = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            Height = 44,
            BackColor = Color.FromArgb(235, 245, 243),
            Margin = new Padding(0, 32, 0, 0),
            Padding = new Padding(16, 0, 16, 0),
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false
        };

        strip.Paint += (s, e) =>
        {
            using var pen = new Pen(Color.FromArgb(190, 230, 225), 1);
            e.Graphics.DrawRectangle(pen, 0, 0, strip.Width - 1, strip.Height - 1);
        };

        var items = new[]
        {
            $"🗄  Database: SQLite",
            $"⚙  .NET 10 Windows",
            $"🔐  Session: {(SessionManager.IsLoggedIn ? SessionManager.Current.Role.ToString() : "—")}",
            $"🕐  {DateTime.Now:HH:mm  dd/MM/yyyy}"
        };

        foreach (var item in items)
        {
            var lbl = new Label
            {
                Text = item,
                Font = new Font("Segoe UI", 8.5f),
                ForeColor = Color.FromArgb(60, 130, 115),
                AutoSize = true,
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom,
                TextAlign = ContentAlignment.MiddleLeft,
                Height = 44,
                Margin = new Padding(0, 0, 48, 0)
            };
            strip.Controls.Add(lbl);
        }

        return strip;
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Coming Soon Placeholder
    // ─────────────────────────────────────────────────────────────────────────

    private void LoadComingSoonContent(string label)
    {
        var placeholderLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 1,
            Margin = new Padding(0)
        };

        var card = new Panel
        {
            Size = new Size(500, 200),
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
            Text = $"🔧  {label}\n\nThis module will be implemented in an upcoming phase.\nNavigation is wired and ready.",
            Font = new Font("Segoe UI", 11f),
            ForeColor = TextMid,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Padding = new Padding(24)
        };

        card.Controls.Add(lbl);
        placeholderLayout.Controls.Add(card, 0, 0);
        _contentArea.Controls.Add(placeholderLayout);
    }

    // ─────────────────────────────────────────────────────────────────────────
    //  Utilities
    // ─────────────────────────────────────────────────────────────────────────

    private static string GetInitials(string fullName)
    {
        var parts = fullName.Trim().Split(' ',
            StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0][0]}{parts[^1][0]}".ToUpper()
            : fullName.Length > 0
                ? fullName[0].ToString().ToUpper()
                : "?";
    }
}