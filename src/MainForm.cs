using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace WinFixPro
{
    public class MainForm : Form
    {
        // Core Layout Panels
        private Panel headerPanel;
        private Panel adminWarningPanel;
        private Panel sidebarPanel;
        private Panel contentPanel;
        private Panel consolePanel;
        private Panel consoleHeaderPanel;
        private Panel catHeaderPanel;
        private Panel cardsContainer;

        // Header controls
        private Label lblAppTitle;
        private Label lblDeveloperTag;
        private Label lblSysInfo;
        private Label lblAdminBadge;

        // Sidebar Navigation
        private List<ModernButton> navButtons = new List<ModernButton>();
        private string activeCategory = "Wizard";

        // Content Area controls
        private Label lblCategoryTitle;
        private Label lblCategoryDesc;
        private ModernButton btnRunAllCategory;
        private ModernButton btnCopySpecs;
        private ModernButton btnSaveSpecs;
        private ModernButton btnRefreshSpecs;

        // Console Controls
        private RichTextBox rtbConsole;
        private Label lblStatus;
        private ModernProgressBar progressBar;
        private ModernButton btnClearConsole;
        private ModernButton btnCopyConsole;
        private ModernButton btnExportLog;
        private ModernButton btnCancel;

        // Active Cards
        private List<ModernCard> currentCards = new List<ModernCard>();
        private SystemSpecs cachedSpecs = null;

        public MainForm()
        {
            InitializeComponent();
            ApplySystemInfo();
            SwitchCategory("Wizard");

            // Wire FixEngine Events
            FixEngine.Instance.OnLog += HandleLog;
            FixEngine.Instance.OnStatus += HandleStatus;
            FixEngine.Instance.OnTaskStateChanged += HandleTaskState;
        }

        private void InitializeComponent()
        {
            Text = string.Format("{0} {1} - {2} | Developed by {3}",
                AppMeta.AppName, AppMeta.Version, AppMeta.Tagline, AppMeta.Developer);

            Size = new Size(1120, 790);
            MinimumSize = new Size(980, 680);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Theme.BgDark;
            ForeColor = Theme.TextMain;
            Font = Theme.FontRegular;

            try
            {
                if (File.Exists("app.ico"))
                {
                    Icon = new Icon("app.ico");
                }
                else
                {
                    using (Bitmap bmp = new Bitmap(32, 32))
                    {
                        using (Graphics g = Graphics.FromImage(bmp))
                        {
                            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                            using (Brush b = new SolidBrush(Theme.AccentBlue))
                                g.FillEllipse(b, 2, 2, 28, 28);
                            using (Pen p = new Pen(Color.White, 3f))
                            {
                                p.StartCap = System.Drawing.Drawing2D.LineCap.Round;
                                p.EndCap = System.Drawing.Drawing2D.LineCap.Round;
                                g.DrawLine(p, 9, 16, 14, 21);
                                g.DrawLine(p, 14, 21, 23, 10);
                            }
                        }
                        Icon = Icon.FromHandle(bmp.GetHicon());
                    }
                }
            }
            catch { }

            // ==========================================
            // 1. TOP HEADER PANEL (Dock: Top)
            // ==========================================
            headerPanel = new Panel();
            headerPanel.Dock = DockStyle.Top;
            headerPanel.Height = 66;
            headerPanel.BackColor = Theme.BgHeader;
            headerPanel.Padding = new Padding(16, 8, 16, 8);

            lblAppTitle = new Label();
            lblAppTitle.Text = string.Format("{0}  {1}", AppMeta.AppName, AppMeta.Version);
            lblAppTitle.Font = Theme.FontHeader;
            lblAppTitle.ForeColor = Theme.TextMain;
            lblAppTitle.AutoSize = true;
            lblAppTitle.UseMnemonic = false;
            lblAppTitle.Location = new Point(16, 12);
            headerPanel.Controls.Add(lblAppTitle);

            lblDeveloperTag = new Label();
            lblDeveloperTag.Text = string.Format("Developed by {0}", AppMeta.Developer);
            lblDeveloperTag.Font = Theme.FontSmallBold;
            lblDeveloperTag.ForeColor = Theme.AccentCyan;
            lblDeveloperTag.AutoSize = true;
            lblDeveloperTag.UseMnemonic = false;
            lblDeveloperTag.Location = new Point(18, 40);
            headerPanel.Controls.Add(lblDeveloperTag);

            lblAdminBadge = new Label();
            lblAdminBadge.Font = Theme.FontSmallBold;
            lblAdminBadge.AutoSize = true;
            lblAdminBadge.UseMnemonic = false;
            lblAdminBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            lblAdminBadge.Padding = new Padding(8, 4, 8, 4);
            headerPanel.Controls.Add(lblAdminBadge);

            lblSysInfo = new Label();
            lblSysInfo.Font = Theme.FontSmall;
            lblSysInfo.ForeColor = Theme.TextSecondary;
            lblSysInfo.AutoSize = true;
            lblSysInfo.UseMnemonic = false;
            lblSysInfo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            headerPanel.Controls.Add(lblSysInfo);

            // ==========================================
            // 2. ADMIN WARNING BANNER (Dock: Top)
            // ==========================================
            bool isAdmin = Theme.IsAdministrator();
            if (!isAdmin)
            {
                adminWarningPanel = new Panel();
                adminWarningPanel.Dock = DockStyle.Top;
                adminWarningPanel.Height = 38;
                adminWarningPanel.BackColor = Color.FromArgb(55, 38, 15);
                adminWarningPanel.Padding = new Padding(16, 4, 16, 4);

                Label lblAdminWarning = new Label();
                lblAdminWarning.Text = "[!] Notice: Running as Standard User. Deep repairs (SFC, DISM, Update Reset) require Administrator rights.";
                lblAdminWarning.ForeColor = Theme.AccentAmber;
                lblAdminWarning.Font = Theme.FontSemibold;
                lblAdminWarning.AutoSize = true;
                lblAdminWarning.UseMnemonic = false;
                lblAdminWarning.Location = new Point(16, 10);
                adminWarningPanel.Controls.Add(lblAdminWarning);

                ModernButton btnElevate = new ModernButton();
                btnElevate.Text = "⚡ Relaunch as Admin";
                btnElevate.Size = new Size(160, 26);
                btnElevate.Style = ButtonStyle.Warning;
                btnElevate.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                btnElevate.Location = new Point(adminWarningPanel.Width - 180, 6);
                btnElevate.Click += delegate { FixEngine.RelaunchAsAdmin(); };
                adminWarningPanel.Controls.Add(btnElevate);
            }

            // ==========================================
            // 3. BOTTOM CONSOLE PANEL (Dock: Bottom)
            // ==========================================
            consolePanel = new Panel();
            consolePanel.Dock = DockStyle.Bottom;
            consolePanel.Height = 190;
            consolePanel.BackColor = Theme.ConsoleBg;

            consoleHeaderPanel = new Panel();
            consoleHeaderPanel.Dock = DockStyle.Top;
            consoleHeaderPanel.Height = 34;
            consoleHeaderPanel.BackColor = Color.FromArgb(20, 24, 38);
            consoleHeaderPanel.Padding = new Padding(12, 4, 12, 4);
            consolePanel.Controls.Add(consoleHeaderPanel);

            lblStatus = new Label();
            lblStatus.Text = string.Format("Status: Idle  •  {0} {1} by {2}", AppMeta.AppName, AppMeta.Version, AppMeta.Developer);
            lblStatus.Font = Theme.FontSemibold;
            lblStatus.ForeColor = Theme.TextSecondary;
            lblStatus.AutoSize = true;
            lblStatus.UseMnemonic = false;
            lblStatus.Location = new Point(12, 8);
            consoleHeaderPanel.Controls.Add(lblStatus);

            progressBar = new ModernProgressBar();
            progressBar.Size = new Size(180, 18);
            progressBar.Location = new Point(360, 8);
            progressBar.Visible = false;
            consoleHeaderPanel.Controls.Add(progressBar);

            btnCancel = new ModernButton();
            btnCancel.Text = "🛑 Cancel";
            btnCancel.Size = new Size(86, 25);
            btnCancel.Style = ButtonStyle.Danger;
            btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCancel.Enabled = false;
            btnCancel.Click += delegate { FixEngine.Instance.CancelCurrentTask(); };
            consoleHeaderPanel.Controls.Add(btnCancel);

            btnExportLog = new ModernButton();
            btnExportLog.Text = "💾 Save Log";
            btnExportLog.Size = new Size(88, 25);
            btnExportLog.Style = ButtonStyle.Secondary;
            btnExportLog.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnExportLog.Click += ExportLogClicked;
            consoleHeaderPanel.Controls.Add(btnExportLog);

            btnCopyConsole = new ModernButton();
            btnCopyConsole.Text = "📋 Copy";
            btnCopyConsole.Size = new Size(72, 25);
            btnCopyConsole.Style = ButtonStyle.Secondary;
            btnCopyConsole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopyConsole.Click += delegate
            {
                if (!string.IsNullOrEmpty(rtbConsole.Text))
                {
                    Clipboard.SetText(rtbConsole.Text);
                    lblStatus.Text = "Log copied to clipboard!";
                }
            };
            consoleHeaderPanel.Controls.Add(btnCopyConsole);

            btnClearConsole = new ModernButton();
            btnClearConsole.Text = "🧹 Clear";
            btnClearConsole.Size = new Size(72, 25);
            btnClearConsole.Style = ButtonStyle.Secondary;
            btnClearConsole.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnClearConsole.Click += delegate
            {
                rtbConsole.Clear();
                FixEngine.Instance.ClearLogs();
            };
            consoleHeaderPanel.Controls.Add(btnClearConsole);

            rtbConsole = new RichTextBox();
            rtbConsole.Dock = DockStyle.Fill;
            rtbConsole.BackColor = Theme.ConsoleBg;
            rtbConsole.ForeColor = Theme.ConsoleFg;
            rtbConsole.Font = Theme.FontConsole;
            rtbConsole.BorderStyle = BorderStyle.None;
            rtbConsole.ReadOnly = true;
            rtbConsole.ScrollBars = RichTextBoxScrollBars.Vertical;
            consolePanel.Controls.Add(rtbConsole);
            rtbConsole.BringToFront();

            // ==========================================
            // 4. LEFT SIDEBAR PANEL (Dock: Left)
            // ==========================================
            sidebarPanel = new Panel();
            sidebarPanel.Dock = DockStyle.Left;
            sidebarPanel.Width = 230;
            sidebarPanel.BackColor = Theme.BgSidebar;
            sidebarPanel.Padding = new Padding(0, 8, 0, 8);

            AddNavButton("⚡ 1-Click Repair", "Wizard");
            AddNavButton("💻 Specs & Activation", "Specs");
            AddNavButton("🛡 System Health", "System");
            AddNavButton("🌐 Network & Internet", "Network");
            AddNavButton("🔄 Windows Updates", "Updates");
            AddNavButton("⚙ Services & Spooler", "Services");
            AddNavButton("🧹 Disk & Cleanup", "Cleanup");
            AddNavButton("📊 Diagnostics & Info", "Diagnostics");

            // Sidebar footer with credits
            Panel sidebarFooter = new Panel();
            sidebarFooter.Dock = DockStyle.Bottom;
            sidebarFooter.Height = 72;
            sidebarFooter.BackColor = Color.FromArgb(16, 18, 30);
            sidebarFooter.Padding = new Padding(14, 10, 14, 10);

            Label lblFoot1 = new Label();
            lblFoot1.Text = string.Format("{0} {1} Portable", AppMeta.AppName, AppMeta.Version);
            lblFoot1.Font = Theme.FontSmallBold;
            lblFoot1.ForeColor = Theme.TextMain;
            lblFoot1.AutoSize = true;
            lblFoot1.UseMnemonic = false;
            lblFoot1.Location = new Point(14, 8);
            sidebarFooter.Controls.Add(lblFoot1);

            Label lblFoot2 = new Label();
            lblFoot2.Text = string.Format("Dev: {0}", AppMeta.Developer);
            lblFoot2.Font = Theme.FontSmall;
            lblFoot2.ForeColor = Theme.AccentCyan;
            lblFoot2.AutoSize = true;
            lblFoot2.UseMnemonic = false;
            lblFoot2.Location = new Point(14, 26);
            sidebarFooter.Controls.Add(lblFoot2);

            Label lblFoot3 = new Label();
            lblFoot3.Text = "Zero-install universal toolkit";
            lblFoot3.Font = Theme.FontSmall;
            lblFoot3.ForeColor = Theme.TextMuted;
            lblFoot3.AutoSize = true;
            lblFoot3.UseMnemonic = false;
            lblFoot3.Location = new Point(14, 44);
            sidebarFooter.Controls.Add(lblFoot3);

            sidebarPanel.Controls.Add(sidebarFooter);

            // ==========================================
            // 5. MAIN CONTENT PANEL (Dock: Fill)
            // ==========================================
            contentPanel = new Panel();
            contentPanel.Dock = DockStyle.Fill;
            contentPanel.BackColor = Theme.BgDark;
            contentPanel.Padding = new Padding(20, 14, 20, 8);

            catHeaderPanel = new Panel();
            catHeaderPanel.Dock = DockStyle.Top;
            catHeaderPanel.Height = 56;
            catHeaderPanel.BackColor = Color.Transparent;
            contentPanel.Controls.Add(catHeaderPanel);

            lblCategoryTitle = new Label();
            lblCategoryTitle.Text = "1-Click System Repair";
            lblCategoryTitle.Font = Theme.FontHeader;
            lblCategoryTitle.ForeColor = Theme.TextMain;
            lblCategoryTitle.AutoSize = true;
            lblCategoryTitle.UseMnemonic = false;
            lblCategoryTitle.Location = new Point(0, 2);
            catHeaderPanel.Controls.Add(lblCategoryTitle);

            lblCategoryDesc = new Label();
            lblCategoryDesc.Text = "Automated multi-step repair wizard that diagnoses and fixes common Windows issues.";
            lblCategoryDesc.Font = Theme.FontSmall;
            lblCategoryDesc.ForeColor = Theme.TextSecondary;
            lblCategoryDesc.AutoSize = true;
            lblCategoryDesc.UseMnemonic = false;
            lblCategoryDesc.Location = new Point(2, 28);
            catHeaderPanel.Controls.Add(lblCategoryDesc);

            // Action Buttons in Header
            btnRunAllCategory = new ModernButton();
            btnRunAllCategory.Text = "▶ Run Selected in Category";
            btnRunAllCategory.Size = new Size(190, 32);
            btnRunAllCategory.Style = ButtonStyle.Success;
            btnRunAllCategory.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRunAllCategory.Click += RunAllInCurrentCategoryClicked;
            catHeaderPanel.Controls.Add(btnRunAllCategory);

            btnSaveSpecs = new ModernButton();
            btnSaveSpecs.Text = "💾 Save Report";
            btnSaveSpecs.Size = new Size(115, 32);
            btnSaveSpecs.Style = ButtonStyle.Success;
            btnSaveSpecs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSaveSpecs.Visible = false;
            btnSaveSpecs.Click += SaveSpecsReportClicked;
            catHeaderPanel.Controls.Add(btnSaveSpecs);

            btnCopySpecs = new ModernButton();
            btnCopySpecs.Text = "📋 Copy Specs";
            btnCopySpecs.Size = new Size(110, 32);
            btnCopySpecs.Style = ButtonStyle.Secondary;
            btnCopySpecs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCopySpecs.Visible = false;
            btnCopySpecs.Click += CopySpecsReportClicked;
            catHeaderPanel.Controls.Add(btnCopySpecs);

            btnRefreshSpecs = new ModernButton();
            btnRefreshSpecs.Text = "🔄 Refresh";
            btnRefreshSpecs.Size = new Size(95, 32);
            btnRefreshSpecs.Style = ButtonStyle.Primary;
            btnRefreshSpecs.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnRefreshSpecs.Visible = false;
            btnRefreshSpecs.Click += delegate { SetupSpecsView(); };
            catHeaderPanel.Controls.Add(btnRefreshSpecs);

            // Cards scrollable container
            cardsContainer = new Panel();
            cardsContainer.Dock = DockStyle.Fill;
            cardsContainer.AutoScroll = true;
            cardsContainer.BackColor = Color.Transparent;
            cardsContainer.Padding = new Padding(0, 6, 12, 6);
            cardsContainer.Resize += delegate { ResizeCurrentCards(); };
            contentPanel.Controls.Add(cardsContainer);
            cardsContainer.BringToFront();

            // ==========================================
            // 6. ATTACH TO FORM IN EXACT MATHEMATICAL DOCK ORDER
            // Content (Fill) must be added first, then Sidebar (Left),
            // Console (Bottom), Warning (Top), Header (Top)
            // ==========================================
            Controls.Add(contentPanel);
            Controls.Add(sidebarPanel);
            Controls.Add(consolePanel);
            if (adminWarningPanel != null) Controls.Add(adminWarningPanel);
            Controls.Add(headerPanel);

            Resize += delegate { PositionControls(); };
            PositionControls();
        }

        private void PositionControls()
        {
            if (consoleHeaderPanel != null && btnCancel != null)
            {
                int r = consoleHeaderPanel.Width - 12;
                btnCancel.Location = new Point(r - btnCancel.Width, 5);
                btnExportLog.Location = new Point(btnCancel.Left - btnExportLog.Width - 6, 5);
                btnCopyConsole.Location = new Point(btnExportLog.Left - btnCopyConsole.Width - 6, 5);
                btnClearConsole.Location = new Point(btnCopyConsole.Left - btnClearConsole.Width - 6, 5);
            }

            if (headerPanel != null && lblAdminBadge != null && lblSysInfo != null)
            {
                lblAdminBadge.Location = new Point(headerPanel.Width - lblAdminBadge.Width - 20, 18);
                lblSysInfo.Location = new Point(lblAdminBadge.Left - lblSysInfo.Width - 18, 22);
            }

            if (catHeaderPanel != null)
            {
                int r = catHeaderPanel.Width - 8;
                if (btnRunAllCategory != null) btnRunAllCategory.Location = new Point(r - btnRunAllCategory.Width, 10);
                if (btnSaveSpecs != null) btnSaveSpecs.Location = new Point(r - btnSaveSpecs.Width, 10);
                if (btnCopySpecs != null && btnSaveSpecs != null) btnCopySpecs.Location = new Point(btnSaveSpecs.Left - btnCopySpecs.Width - 8, 10);
                if (btnRefreshSpecs != null && btnCopySpecs != null) btnRefreshSpecs.Location = new Point(btnCopySpecs.Left - btnRefreshSpecs.Width - 8, 10);
            }

            ResizeCurrentCards();
        }

        private void ResizeCurrentCards()
        {
            if (cardsContainer == null) return;
            int targetWidth = Math.Max(300, cardsContainer.ClientSize.Width - 28);
            for (int i = 0; i < cardsContainer.Controls.Count; i++)
            {
                Control c = cardsContainer.Controls[i];
                c.Width = targetWidth;
                c.Left = 14;
            }
        }

        private void AddNavButton(string label, string categoryTag)
        {
            ModernButton btn = new ModernButton();
            btn.Text = label;
            btn.Style = ButtonStyle.Sidebar;
            btn.Size = new Size(230, 42);
            btn.Location = new Point(0, 6 + navButtons.Count * 44);
            btn.Tag = categoryTag;
            btn.Click += delegate (object s, EventArgs e)
            {
                SwitchCategory(categoryTag);
            };
            sidebarPanel.Controls.Add(btn);
            navButtons.Add(btn);
        }

        private void ApplySystemInfo()
        {
            bool isAdmin = Theme.IsAdministrator();
            if (isAdmin)
            {
                lblAdminBadge.Text = "🛡 ADMINISTRATOR";
                lblAdminBadge.BackColor = Color.FromArgb(40, Theme.AccentGreen);
                lblAdminBadge.ForeColor = Theme.AccentGreen;
            }
            else
            {
                lblAdminBadge.Text = "[!] STANDARD USER";
                lblAdminBadge.BackColor = Color.FromArgb(40, Theme.AccentAmber);
                lblAdminBadge.ForeColor = Theme.AccentAmber;
            }

            string os = Environment.OSVersion.Version.Major >= 10 ?
                (Environment.OSVersion.Version.Build >= 22000 ? "Windows 11" : "Windows 10") :
                Environment.OSVersion.ToString();

            string arch = Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit";
            lblSysInfo.Text = string.Format("{0} ({1}) | {2} Cores | {3}", os, arch, Environment.ProcessorCount, Environment.MachineName);
        }

        private void SwitchCategory(string category)
        {
            activeCategory = category;

            // Update sidebar active buttons
            for (int i = 0; i < navButtons.Count; i++)
            {
                navButtons[i].IsActiveSidebar = (navButtons[i].Tag.ToString() == category);
            }

            cardsContainer.Controls.Clear();
            currentCards.Clear();

            btnRunAllCategory.Visible = false;
            btnCopySpecs.Visible = false;
            btnSaveSpecs.Visible = false;
            btnRefreshSpecs.Visible = false;

            if (category == "Wizard")
            {
                SetupWizardView();
            }
            else if (category == "Specs")
            {
                SetupSpecsView();
            }
            else
            {
                SetupCategoryView(category);
            }

            PositionControls();
        }

        private void SetupWizardView()
        {
            lblCategoryTitle.Text = "⚡ 1-Click Automated Repair Wizard";
            lblCategoryDesc.Text = "Runs the recommended sequence of safe diagnostic & repair tools in a single click.";
            btnRunAllCategory.Visible = false;

            int availWidth = Math.Max(300, cardsContainer.ClientSize.Width - 28);

            // Hero Card
            Panel hero = new Panel();
            hero.Size = new Size(availWidth, 224);
            hero.BackColor = Color.FromArgb(24, 28, 44);
            hero.Location = new Point(14, 8);
            hero.Padding = new Padding(18);

            Label heroTitle = new Label();
            heroTitle.Text = "Complete Automated Windows Health Repair";
            heroTitle.Font = Theme.FontHeader;
            heroTitle.ForeColor = Theme.TextMain;
            heroTitle.AutoSize = true;
            heroTitle.UseMnemonic = false;
            heroTitle.Location = new Point(18, 14);
            hero.Controls.Add(heroTitle);

            Label heroAuthor = new Label();
            heroAuthor.Text = string.Format("Developed by {0}  •  Version {1}", AppMeta.Developer, AppMeta.Version);
            heroAuthor.Font = Theme.FontSmallBold;
            heroAuthor.ForeColor = Theme.AccentCyan;
            heroAuthor.AutoSize = true;
            heroAuthor.UseMnemonic = false;
            heroAuthor.Location = new Point(19, 38);
            hero.Controls.Add(heroAuthor);

            Label heroDesc = new Label();
            heroDesc.Text = "This master maintenance routine automatically executes:\n" +
                            "  1. Network Stack & DNS Flush (Restores broken internet connectivity)\n" +
                            "  2. Temporary & Cache Cleanup (Frees gigabytes of system disk space)\n" +
                            "  3. Stuck Print Spooler Fix (Clears jammed print jobs)\n" +
                            "  4. Windows Update & BITS Reset (Fixes failed update downloads)\n" +
                            "  5. Unblock System Tools (Re-enables Task Manager, Regedit, CMD)\n" +
                            "  6. Windows Clock & Time Synchronization (Fixes HTTPS certificate errors)\n" +
                            "  7. DISM Component Store Health Check & Restore\n" +
                            "  8. SFC System File Scan & Automatic File Replacement\n" +
                            "  9. SSD TRIM & Drive Performance Optimization";
            heroDesc.Font = Theme.FontRegular;
            heroDesc.ForeColor = Theme.TextSecondary;
            heroDesc.UseMnemonic = false;
            heroDesc.Size = new Size(Math.Max(200, hero.Width - 230), 140);
            heroDesc.Location = new Point(20, 62);
            hero.Controls.Add(heroDesc);

            ModernButton btnStartWizard = new ModernButton();
            btnStartWizard.Text = "⚡ Start 1-Click Repair";
            btnStartWizard.Size = new Size(190, 48);
            btnStartWizard.Style = ButtonStyle.Success;
            btnStartWizard.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            btnStartWizard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnStartWizard.Location = new Point(hero.Width - 210, 50);
            btnStartWizard.Click += delegate { StartWizardRepair(); };
            hero.Controls.Add(btnStartWizard);

            cardsContainer.Controls.Add(hero);

            // Pro Tip Card
            Panel tip = new Panel();
            tip.Size = new Size(availWidth, 80);
            tip.BackColor = Theme.BgCard;
            tip.Location = new Point(14, 240);
            tip.Padding = new Padding(16);

            Label tipText = new Label();
            tipText.Text = string.Format("Tip: WinFixPro is 100% portable! Copy WinFixPro.exe to any USB flash drive and use it on\n" +
                           "client PCs, family computers, or office workstations without installing anything.\n" +
                           "Developed by {0}.", AppMeta.Developer);
            tipText.Font = Theme.FontRegular;
            tipText.ForeColor = Theme.AccentBlue;
            tipText.UseMnemonic = false;
            tipText.Dock = DockStyle.Fill;
            tip.Controls.Add(tipText);

            cardsContainer.Controls.Add(tip);
        }

        private void SetupSpecsView()
        {
            lblCategoryTitle.Text = "System Hardware & Windows Information";
            lblCategoryDesc.Text = "Live overview of Windows activation, processor, RAM, graphics card, and storage space.";
            btnRunAllCategory.Visible = false;
            btnSaveSpecs.Visible = true;
            btnCopySpecs.Visible = true;
            btnRefreshSpecs.Visible = true;

            cardsContainer.Controls.Clear();
            currentCards.Clear();

            cachedSpecs = SystemSpecs.FetchLiveSpecs();
            int availWidth = Math.Max(300, cardsContainer.ClientSize.Width - 28);
            int topOffset = 8;

            // 1. OS & Activation Card
            string osDetails = string.Format(
                "Edition:          {0} ({1})\n" +
                "Build & Version:  {2} (Kernel {3})\n" +
                "License State:    {4}\n" +
                "License Channel:  {5}\n" +
                "Partial Key:      {6}\n" +
                "Computer & User:  {7} \\ {8}",
                cachedSpecs.OsName, cachedSpecs.OsArch,
                cachedSpecs.OsBuild, cachedSpecs.OsVersion,
                cachedSpecs.ActivationStatus,
                string.IsNullOrEmpty(cachedSpecs.ActivationChannel) ? "Standard OEM/Retail" : cachedSpecs.ActivationChannel,
                string.IsNullOrEmpty(cachedSpecs.PartialKey) ? "N/A" : "*****-" + cachedSpecs.PartialKey,
                cachedSpecs.ComputerName, cachedSpecs.UserName);

            Panel pnlOs = CreateSpecsDisplayCard(
                "Windows Operating System & Activation",
                cachedSpecs.IsActivated ? "✔ ACTIVATED" : "[!] UNLICENSED",
                cachedSpecs.IsActivated ? Theme.AccentGreen : Theme.AccentAmber,
                osDetails,
                "🔑 Check SLMGR",
                delegate
                {
                    FixEngine.Instance.StartTask("Check Windows Activation (SLMGR)", delegate ()
                    {
                        FixEngine.Instance.Log("Checking Windows Activation Expiration...", LogLevel.Info);
                        FixEngine.Instance.RunCommand("cscript.exe", "//nologo C:\\Windows\\System32\\slmgr.vbs /xpr");
                        FixEngine.Instance.Log("Querying Detailed License Channel & Key Info...", LogLevel.Info);
                        FixEngine.Instance.RunCommand("cscript.exe", "//nologo C:\\Windows\\System32\\slmgr.vbs /dli");
                    });
                },
                availWidth, ref topOffset
            );
            cardsContainer.Controls.Add(pnlOs);

            // 2. Processor (CPU) Card
            string cpuDetails = string.Format(
                "Processor Model:  {0}\n" +
                "Core Count:       {1} Physical Cores / {2} Logical Threads\n" +
                "Architecture:     {3} processor architecture",
                cachedSpecs.CpuName, cachedSpecs.CpuCores, cachedSpecs.CpuThreads, cachedSpecs.CpuArch);

            Panel pnlCpu = CreateSpecsDisplayCard(
                "Processor (CPU)",
                "PROCESSOR",
                Theme.AccentBlue,
                cpuDetails,
                null, null,
                availWidth, ref topOffset
            );
            cardsContainer.Controls.Add(pnlCpu);

            // 3. Memory (RAM) Card
            string ramDetails = string.Format(
                "Installed RAM:    {0:F1} GB Total Physical Memory\n" +
                "Active Usage:     {1:F1} GB Used ({2}%) | {3:F1} GB Available Free",
                cachedSpecs.TotalRamGB, cachedSpecs.UsedRamGB, cachedSpecs.RamPctUsed, cachedSpecs.FreeRamGB);

            Panel pnlRam = CreateSpecsDisplayCard(
                "Installed Memory (RAM)",
                "MEMORY",
                Theme.AccentIndigo,
                ramDetails,
                null, null,
                availWidth, ref topOffset
            );
            // Append visual progress bar for RAM
            ModernProgressBar ramBar = new ModernProgressBar();
            ramBar.Size = new Size(availWidth - 40, 16);
            ramBar.Location = new Point(18, pnlRam.Height - 14);
            ramBar.Value = cachedSpecs.RamPctUsed;
            ramBar.CustomText = string.Format("RAM in Use: {0}% ({1:F1} GB / {2:F1} GB)", cachedSpecs.RamPctUsed, cachedSpecs.UsedRamGB, cachedSpecs.TotalRamGB);
            pnlRam.Height += 24;
            pnlRam.Controls.Add(ramBar);
            topOffset += 24;
            cardsContainer.Controls.Add(pnlRam);

            // 4. Graphics Card (GPU) Card
            StringBuilder gpuSb = new StringBuilder();
            if (cachedSpecs.Gpus.Count == 0)
            {
                gpuSb.AppendLine("No discrete or integrated video controller detected.");
            }
            else
            {
                for (int i = 0; i < cachedSpecs.Gpus.Count; i++)
                {
                    GpuInfo g = cachedSpecs.Gpus[i];
                    gpuSb.AppendLine(string.Format("GPU #{0}: {1}  |  VRAM: {2} MB  |  Driver: {3}",
                        i + 1, g.Name, g.VramMB, g.DriverVersion));
                }
            }

            Panel pnlGpu = CreateSpecsDisplayCard(
                "Graphics Cards & Display (GPU)",
                "GRAPHICS",
                Theme.AccentCyan,
                gpuSb.ToString().TrimEnd(),
                null, null,
                availWidth, ref topOffset
            );
            cardsContainer.Controls.Add(pnlGpu);

            // 5. Storage Drives & Disk Space Card
            StringBuilder diskSb = new StringBuilder();
            if (cachedSpecs.PhysicalDisks.Count > 0)
            {
                diskSb.AppendLine("Physical Disks:  " + string.Join("  •  ", cachedSpecs.PhysicalDisks.ToArray()));
            }
            for (int i = 0; i < cachedSpecs.Volumes.Count; i++)
            {
                VolumeInfo v = cachedSpecs.Volumes[i];
                diskSb.AppendLine(string.Format("Drive {0} [{1}]:  {2:F1} GB Free of {3:F1} GB  ({4}% Used, {5})",
                    v.DriveLetter, v.VolumeLabel, v.FreeGB, v.TotalGB, v.PctUsed, v.FileSystem));
            }

            Panel pnlDisk = CreateSpecsDisplayCard(
                "Storage Drives & Disk Space",
                "STORAGE",
                Theme.AccentAmber,
                diskSb.ToString().TrimEnd(),
                "🧹 Disk Cleanup",
                delegate
                {
                    FixEngine.Instance.StartTask("Launch Windows Disk Cleanup", delegate ()
                    {
                        FixEngine.Instance.Log("Launching native Windows cleanmgr...", LogLevel.Info);
                        Process.Start("cleanmgr.exe", "/d C:");
                    });
                },
                availWidth, ref topOffset
            );
            cardsContainer.Controls.Add(pnlDisk);

            // 6. Motherboard & BIOS Card
            string boardDetails = string.Format(
                "Motherboard:      {0}\n" +
                "BIOS Version:     {1}",
                string.IsNullOrEmpty(cachedSpecs.Motherboard) ? "Standard Motherboard / OEM" : cachedSpecs.Motherboard,
                string.IsNullOrEmpty(cachedSpecs.BiosVersion) ? "Standard BIOS" : cachedSpecs.BiosVersion);

            Panel pnlBoard = CreateSpecsDisplayCard(
                "Motherboard & BIOS Information",
                "HARDWARE",
                Theme.AccentPurple,
                boardDetails,
                null, null,
                availWidth, ref topOffset
            );
            cardsContainer.Controls.Add(pnlBoard);
        }

        private Panel CreateSpecsDisplayCard(string title, string badge, Color badgeColor, string content, string buttonText, Action buttonAction, int width, ref int topOffset)
        {
            Panel card = new Panel();
            card.BackColor = Theme.BgCard;
            card.Location = new Point(14, topOffset);
            card.Padding = new Padding(14);

            Label lblTitle = new Label();
            lblTitle.Text = title;
            lblTitle.Font = Theme.FontTitle;
            lblTitle.ForeColor = Theme.TextMain;
            lblTitle.AutoSize = true;
            lblTitle.UseMnemonic = false;
            lblTitle.Location = new Point(14, 12);
            card.Controls.Add(lblTitle);

            if (!string.IsNullOrEmpty(badge))
            {
                Label lblBadge = new Label();
                lblBadge.Text = badge;
                lblBadge.Font = Theme.FontSmallBold;
                lblBadge.ForeColor = badgeColor;
                lblBadge.BackColor = Color.FromArgb(40, badgeColor);
                lblBadge.AutoSize = true;
                lblBadge.UseMnemonic = false;
                lblBadge.Padding = new Padding(6, 2, 6, 2);
                lblBadge.Location = new Point(lblTitle.Right + 12, 11);
                card.Controls.Add(lblBadge);
            }

            if (!string.IsNullOrEmpty(buttonText) && buttonAction != null)
            {
                ModernButton btn = new ModernButton();
                btn.Text = buttonText;
                btn.Size = new Size(130, 30);
                btn.Style = ButtonStyle.Primary;
                btn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                btn.Location = new Point(width - 145, 12);
                btn.Click += delegate { buttonAction(); };
                card.Controls.Add(btn);
            }

            Label lblBody = new Label();
            lblBody.Text = content;
            lblBody.Font = Theme.FontRegular;
            lblBody.ForeColor = Theme.TextSecondary;
            lblBody.AutoSize = true;
            lblBody.UseMnemonic = false;
            lblBody.Location = new Point(16, 38);
            card.Controls.Add(lblBody);

            int cardHeight = Math.Max(75, lblBody.Bottom + 14);
            card.Size = new Size(width, cardHeight);
            topOffset += cardHeight + 10;

            return card;
        }

        private void CopySpecsReportClicked(object sender, EventArgs e)
        {
            if (cachedSpecs == null) cachedSpecs = SystemSpecs.FetchLiveSpecs();
            string rpt = cachedSpecs.ToFormattedReport();
            Clipboard.SetText(rpt);
            lblStatus.Text = "System hardware & Windows specifications copied to clipboard!";
        }

        private void SaveSpecsReportClicked(object sender, EventArgs e)
        {
            if (cachedSpecs == null) cachedSpecs = SystemSpecs.FetchLiveSpecs();
            string rpt = cachedSpecs.ToFormattedReport();
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            string dest = Path.Combine(desktop, string.Format("WinFixPro_Specs_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now));
            try
            {
                File.WriteAllText(dest, rpt, Encoding.UTF8);
                MessageBox.Show("Hardware and Windows specification report saved to:\n" + dest,
                    "Report Saved", MessageBoxButtons.OK, MessageBoxIcon.Information);
                Process.Start("notepad.exe", dest);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not save report: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SetupCategoryView(string category)
        {
            btnRunAllCategory.Visible = true;
            btnCopySpecs.Visible = false;
            btnSaveSpecs.Visible = false;
            btnRefreshSpecs.Visible = false;

            List<FixItem> items = FixModules.GetItemsByCategory(category);

            switch (category)
            {
                case "System":
                    lblCategoryTitle.Text = "System Health & Integrity";
                    lblCategoryDesc.Text = "Scans and repairs corrupted core Windows files, component store, and file systems.";
                    break;
                case "Network":
                    lblCategoryTitle.Text = "Network & Internet Repair";
                    lblCategoryDesc.Text = "Fixes DNS resolution errors, socket drops, corrupt TCP/IP stacks, and firewall blocks.";
                    break;
                case "Updates":
                    lblCategoryTitle.Text = "Windows Update & Store Fixes";
                    lblCategoryDesc.Text = "Resolves stuck updates (error codes 0x800...), re-registers apps, and restarts explorer.";
                    break;
                case "Services":
                    lblCategoryTitle.Text = "Windows Services & Spooler";
                    lblCategoryDesc.Text = "Repairs frozen print queues, resyncs system clock, fixes audio engines, and unblocks tools.";
                    break;
                case "Cleanup":
                    lblCategoryTitle.Text = "Disk Cleanup & Drive Optimization";
                    lblCategoryDesc.Text = "Purges gigabytes of temporary junk, error reporting dumps, and optimizes SSDs with TRIM.";
                    break;
                case "Diagnostics":
                    lblCategoryTitle.Text = "Diagnostics & System Reports";
                    lblCategoryDesc.Text = "Generates laptop battery health reports, exports hardware configurations, and scans event logs.";
                    btnRunAllCategory.Visible = false;
                    break;
            }

            int topOffset = 8;
            int cardWidth = Math.Max(300, cardsContainer.ClientSize.Width - 28);

            for (int i = 0; i < items.Count; i++)
            {
                FixItem item = items[i];
                ModernCard card = new ModernCard();
                card.CardTitle = item.Title;
                card.CardDescription = item.Description;
                card.Badge = item.Badge;
                card.BadgeColor = item.BadgeColor;
                card.ShowCheckbox = true;
                card.IsChecked = true;
                card.Size = new Size(cardWidth, 80);
                card.Location = new Point(14, topOffset);

                // Wire click
                FixItem closureItem = item;
                card.ActionClicked += delegate
                {
                    if (closureItem.RequiresAdmin && !Theme.IsAdministrator())
                    {
                        DialogResult res = MessageBox.Show(
                            "This repair requires Administrator privileges to execute properly.\nWould you like to relaunch WinFixPro as Administrator?",
                            "Administrator Required", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                        if (res == DialogResult.Yes)
                        {
                            FixEngine.RelaunchAsAdmin();
                            return;
                        }
                    }
                    FixEngine.Instance.StartTask(closureItem.Title, closureItem.ActionToRun);
                };

                cardsContainer.Controls.Add(card);
                currentCards.Add(card);
                topOffset += 88;
            }
        }

        private void StartWizardRepair()
        {
            if (!Theme.IsAdministrator())
            {
                DialogResult res = MessageBox.Show(
                    "The 1-Click Repair Wizard requires Administrator privileges to perform deep repairs (SFC, DISM, Update reset).\n\nWould you like to elevate to Administrator now?",
                    "Administrator Elevation Needed", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (res == DialogResult.Yes)
                {
                    FixEngine.RelaunchAsAdmin();
                    return;
                }
            }

            FixEngine.Instance.StartTask("1-Click Master Repair Wizard", delegate ()
            {
                List<FixItem> all = FixModules.GetAllItems();

                string[] wizardIds = new string[] {
                    "flush_dns",
                    "reset_winsock",
                    "clean_temp",
                    "fix_spooler",
                    "unblock_tools",
                    "reset_wuauserv",
                    "sync_time",
                    "dism_restore",
                    "sfc_scan",
                    "trim_drives"
                };

                int total = wizardIds.Length;
                for (int i = 0; i < total; i++)
                {
                    string id = wizardIds[i];
                    FixItem target = null;
                    for (int j = 0; j < all.Count; j++)
                    {
                        if (all[j].Id == id) { target = all[j]; break; }
                    }

                    if (target != null)
                    {
                        int pct = (int)(((double)i / total) * 100);
                        FixEngine.Instance.UpdateStatus(string.Format("Step {0}/{1}: {2}", i + 1, total, target.Title), pct);
                        FixEngine.Instance.Log(string.Format("[Wizard Step {0}/{1}] Starting: {2}", i + 1, total, target.Title), LogLevel.Header);

                        try
                        {
                            target.ActionToRun();
                        }
                        catch (Exception ex)
                        {
                            FixEngine.Instance.Log("Error in " + target.Title + ": " + ex.Message, LogLevel.Error);
                        }

                        Thread.Sleep(500);
                    }
                }

                FixEngine.Instance.UpdateStatus(string.Format("1-Click Repair Complete!  •  {0} by {1}", AppMeta.AppName, AppMeta.Developer), 100);
                FixEngine.Instance.Log("==================================================", LogLevel.Success);
                FixEngine.Instance.Log("ALL WIZARD REPAIRS COMPLETED SUCCESSFULLY!", LogLevel.Success);
                FixEngine.Instance.Log("Developed by " + AppMeta.Developer + " | Version " + AppMeta.Version, LogLevel.Header);
                FixEngine.Instance.Log("A system restart is recommended to finalize all changes.", LogLevel.Header);
                FixEngine.Instance.Log("==================================================", LogLevel.Success);
            });
        }

        private void RunAllInCurrentCategoryClicked(object sender, EventArgs e)
        {
            List<FixItem> items = FixModules.GetItemsByCategory(activeCategory);
            List<FixItem> toRun = new List<FixItem>();

            for (int i = 0; i < items.Count && i < currentCards.Count; i++)
            {
                if (currentCards[i].IsChecked)
                {
                    toRun.Add(items[i]);
                }
            }

            if (toRun.Count == 0)
            {
                MessageBox.Show("Please select at least one fix to run.", "No Fixes Selected", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            FixEngine.Instance.StartTask("Batch: " + lblCategoryTitle.Text, delegate ()
            {
                int total = toRun.Count;
                for (int i = 0; i < total; i++)
                {
                    int pct = (int)(((double)i / total) * 100);
                    FixEngine.Instance.UpdateStatus(string.Format("Running ({0}/{1}): {2}", i + 1, total, toRun[i].Title), pct);
                    try
                    {
                        toRun[i].ActionToRun();
                    }
                    catch (Exception ex)
                    {
                        FixEngine.Instance.Log("Error in " + toRun[i].Title + ": " + ex.Message, LogLevel.Error);
                    }
                    Thread.Sleep(400);
                }
            });
        }

        private void HandleLog(string message, LogLevel level)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, LogLevel>(HandleLog), message, level);
                return;
            }

            Color col = Theme.ConsoleFg;
            switch (level)
            {
                case LogLevel.Success:
                    col = Color.FromArgb(74, 222, 128); // Bright Green
                    break;
                case LogLevel.Warning:
                    col = Color.FromArgb(251, 191, 36); // Amber
                    break;
                case LogLevel.Error:
                    col = Color.FromArgb(248, 113, 113); // Red
                    break;
                case LogLevel.Header:
                    col = Color.FromArgb(129, 140, 248); // Indigo
                    break;
                case LogLevel.Info:
                default:
                    col = Theme.ConsoleFg;
                    break;
            }

            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            rtbConsole.SelectionStart = rtbConsole.TextLength;
            rtbConsole.SelectionLength = 0;

            rtbConsole.SelectionColor = Color.FromArgb(100, 116, 139);
            rtbConsole.AppendText("[" + timestamp + "] ");

            rtbConsole.SelectionColor = col;
            rtbConsole.AppendText(message + "\n");
            rtbConsole.ScrollToCaret();
        }

        private void HandleStatus(string status, int progress)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, int>(HandleStatus), status, progress);
                return;
            }

            lblStatus.Text = status;
            if (progress > 0)
            {
                progressBar.Visible = true;
                progressBar.Value = progress;
            }
            else
            {
                progressBar.Visible = false;
            }
        }

        private void HandleTaskState(bool isRunning, string taskName)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool, string>(HandleTaskState), isRunning, taskName);
                return;
            }

            btnCancel.Enabled = isRunning;
            if (!isRunning)
            {
                progressBar.Visible = false;
                lblStatus.Text = string.Format("Status: Idle  •  {0} {1} by {2}", AppMeta.AppName, AppMeta.Version, AppMeta.Developer);
            }
        }

        private void ExportLogClicked(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Filter = "Text Log (*.txt)|*.txt|All Files (*.*)|*.*";
            sfd.FileName = string.Format("WinFixPro_Log_{0:yyyyMMdd_HHmmss}.txt", DateTime.Now);
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                if (FixEngine.Instance.ExportLog(sfd.FileName))
                {
                    MessageBox.Show("Session log saved successfully!", "Log Exported", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("Failed to save log to the specified path.", "Export Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
