using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace WinFixPro
{
    public static class AppMeta
    {
        public const string AppName = "WinFixPro";
        public const string Version = "v2.7";
        public const string Developer = "Soumya Ranjan Sethy";
        public const string Tagline = "Portable Multi-System Windows Repair Toolkit";
    }

    public static class Theme
    {
        // Dark Theme Color Palette
        public static readonly Color BgDark = Color.FromArgb(14, 16, 25);            // #0E1019 Deep Slate
        public static readonly Color BgSidebar = Color.FromArgb(19, 22, 35);         // #131623
        public static readonly Color BgHeader = Color.FromArgb(24, 28, 44);          // #181C2C
        public static readonly Color BgCard = Color.FromArgb(26, 30, 48);            // #1A1E30
        public static readonly Color BgCardHover = Color.FromArgb(33, 38, 62);       // #21263E
        public static readonly Color BorderCard = Color.FromArgb(43, 50, 77);        // #2B324D
        public static readonly Color ConsoleBg = Color.FromArgb(10, 12, 19);         // #0A0C13
        public static readonly Color ConsoleFg = Color.FromArgb(226, 232, 240);      // #E2E8F0

        // Accents
        public static readonly Color AccentBlue = Color.FromArgb(59, 130, 246);      // #3B82F6
        public static readonly Color AccentBlueHover = Color.FromArgb(37, 99, 235);  // #2563EB
        public static readonly Color AccentIndigo = Color.FromArgb(99, 102, 241);    // #6366F1
        public static readonly Color AccentGreen = Color.FromArgb(16, 185, 129);     // #10B981
        public static readonly Color AccentGreenHover = Color.FromArgb(5, 150, 105); // #059669
        public static readonly Color AccentAmber = Color.FromArgb(245, 158, 11);     // #F59E0B
        public static readonly Color AccentRed = Color.FromArgb(239, 68, 68);        // #EF4444
        public static readonly Color AccentRedHover = Color.FromArgb(220, 38, 38);   // #DC2626
        public static readonly Color AccentPurple = Color.FromArgb(168, 85, 247);    // #A855F7
        public static readonly Color AccentCyan = Color.FromArgb(56, 189, 248);      // #38BDF8

        // Text Colors
        public static readonly Color TextMain = Color.FromArgb(248, 250, 252);       // #F8FAFC
        public static readonly Color TextSecondary = Color.FromArgb(148, 163, 184);  // #94A3B8
        public static readonly Color TextMuted = Color.FromArgb(100, 116, 139);      // #64748B

        // Fonts
        public static Font FontHeader = new Font("Segoe UI", 13.0f, FontStyle.Bold);
        public static Font FontTitle = new Font("Segoe UI", 10.5f, FontStyle.Bold);
        public static Font FontRegular = new Font("Segoe UI", 9.25f, FontStyle.Regular);
        public static Font FontSemibold = new Font("Segoe UI", 9.25f, FontStyle.Bold);
        public static Font FontSmall = new Font("Segoe UI", 8.25f, FontStyle.Regular);
        public static Font FontSmallBold = new Font("Segoe UI", 8.25f, FontStyle.Bold);
        public static Font FontConsole = new Font("Consolas", 9.0f, FontStyle.Regular);

        public static bool IsAdministrator()
        {
            try
            {
                WindowsIdentity id = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(id);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }

    public enum ButtonStyle
    {
        Primary,
        Success,
        Warning,
        Danger,
        Secondary,
        Sidebar,
        Outline
    }

    public class ModernButton : Button
    {
        private bool isHovered = false;
        private bool isPressed = false;
        private ButtonStyle style = ButtonStyle.Primary;
        private bool isActiveSidebar = false;

        public ButtonStyle Style
        {
            get { return style; }
            set { style = value; Invalidate(); }
        }

        public bool IsActiveSidebar
        {
            get { return isActiveSidebar; }
            set { isActiveSidebar = value; Invalidate(); }
        }

        public ModernButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            SetStyle(ControlStyles.Opaque, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = Theme.FontSemibold;
            Cursor = Cursors.Hand;
            Size = new Size(130, 36);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            isHovered = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            isPressed = true;
            Invalidate();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            Color bgColor;
            Color fgColor = Theme.TextMain;
            Color borderColor = Color.Transparent;

            switch (style)
            {
                case ButtonStyle.Primary:
                    bgColor = isPressed ? Theme.AccentBlueHover : (isHovered ? Theme.AccentBlue : Color.FromArgb(37, 99, 235));
                    break;
                case ButtonStyle.Success:
                    bgColor = isPressed ? Theme.AccentGreenHover : (isHovered ? Theme.AccentGreen : Color.FromArgb(14, 159, 110));
                    break;
                case ButtonStyle.Warning:
                    bgColor = isPressed ? Color.FromArgb(217, 119, 6) : (isHovered ? Theme.AccentAmber : Color.FromArgb(202, 138, 4));
                    fgColor = Color.FromArgb(20, 20, 20);
                    break;
                case ButtonStyle.Danger:
                    bgColor = isPressed ? Theme.AccentRedHover : (isHovered ? Theme.AccentRed : Color.FromArgb(220, 38, 38));
                    break;
                case ButtonStyle.Secondary:
                    bgColor = isPressed ? Color.FromArgb(28, 32, 50) : (isHovered ? Theme.BgCardHover : Theme.BgCard);
                    borderColor = Theme.BorderCard;
                    fgColor = Theme.TextMain;
                    break;
                case ButtonStyle.Outline:
                    bgColor = isHovered ? Color.FromArgb(35, 45, 75) : (Parent != null ? Parent.BackColor : Theme.BgDark);
                    borderColor = Theme.AccentBlue;
                    fgColor = Theme.AccentBlue;
                    break;
                case ButtonStyle.Sidebar:
                    if (isActiveSidebar)
                    {
                        bgColor = Color.FromArgb(34, 40, 64);
                        fgColor = Theme.AccentCyan;
                    }
                    else if (isHovered)
                    {
                        bgColor = Color.FromArgb(28, 33, 52);
                        fgColor = Theme.TextMain;
                    }
                    else
                    {
                        bgColor = Theme.BgSidebar; // Solid opaque sidebar background fixes ghosting/blurriness!
                        fgColor = Theme.TextSecondary;
                    }
                    break;
                default:
                    bgColor = Theme.AccentBlue;
                    break;
            }

            if (!Enabled)
            {
                bgColor = Color.FromArgb(36, 40, 56);
                fgColor = Color.FromArgb(90, 100, 120);
                borderColor = Color.Transparent;
            }

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Draw Background
            using (SolidBrush brush = new SolidBrush(bgColor))
            {
                g.FillRectangle(brush, rect);
            }

            // Draw Border if any
            if (borderColor != Color.Transparent)
            {
                using (Pen pen = new Pen(borderColor, 1))
                {
                    g.DrawRectangle(pen, rect);
                }
            }

            // Draw Active indicator for sidebar
            if (style == ButtonStyle.Sidebar && isActiveSidebar)
            {
                using (SolidBrush barBrush = new SolidBrush(Theme.AccentCyan))
                {
                    g.FillRectangle(barBrush, 0, 0, 4, Height);
                }
            }

            // Draw Text with NoPrefix to avoid converting '&' into underscores!
            TextFormatFlags flags = TextFormatFlags.VerticalCenter | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix;
            if (style == ButtonStyle.Sidebar)
            {
                flags |= TextFormatFlags.Left;
                Rectangle textRect = new Rectangle(18, 0, Width - 24, Height);
                TextRenderer.DrawText(g, Text, Font, textRect, fgColor, flags);
            }
            else
            {
                flags |= TextFormatFlags.HorizontalCenter;
                TextRenderer.DrawText(g, Text, Font, rect, fgColor, flags);
            }
        }
    }

    public class ModernCard : Panel
    {
        private string title = "Card Title";
        private string description = "Description text...";
        private string badge = "";
        private Color badgeColor = Theme.AccentBlue;
        private ModernButton actionButton;
        private CheckBox checkBox;
        private bool isHovered = false;

        public event EventHandler ActionClicked;

        public string CardTitle
        {
            get { return title; }
            set { title = value; Invalidate(); }
        }

        public string CardDescription
        {
            get { return description; }
            set { description = value; Invalidate(); }
        }

        public string Badge
        {
            get { return badge; }
            set { badge = value; Invalidate(); }
        }

        public Color BadgeColor
        {
            get { return badgeColor; }
            set { badgeColor = value; Invalidate(); }
        }

        public string ButtonText
        {
            get { return actionButton != null ? actionButton.Text : ""; }
            set { if (actionButton != null) actionButton.Text = value; }
        }

        public ButtonStyle ButtonColorStyle
        {
            get { return actionButton != null ? actionButton.Style : ButtonStyle.Primary; }
            set { if (actionButton != null) actionButton.Style = value; }
        }

        public bool IsChecked
        {
            get { return checkBox != null && checkBox.Checked; }
            set { if (checkBox != null) checkBox.Checked = value; }
        }

        public bool ShowCheckbox
        {
            get { return checkBox != null && checkBox.Visible; }
            set { if (checkBox != null) checkBox.Visible = value; }
        }

        public ModernCard()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);

            BackColor = Theme.BgCard;
            Size = new Size(600, 78);
            Padding = new Padding(12);

            checkBox = new CheckBox();
            checkBox.AutoSize = true;
            checkBox.Location = new Point(12, 28);
            checkBox.ForeColor = Theme.TextMain;
            checkBox.Cursor = Cursors.Hand;
            Controls.Add(checkBox);

            actionButton = new ModernButton();
            actionButton.Text = "Run Fix";
            actionButton.Size = new Size(100, 32);
            actionButton.Style = ButtonStyle.Primary;
            actionButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            actionButton.Click += delegate (object s, EventArgs e)
            {
                if (ActionClicked != null) ActionClicked(this, EventArgs.Empty);
            };
            Controls.Add(actionButton);

            MouseEnter += delegate { isHovered = true; Invalidate(); };
            MouseLeave += delegate { isHovered = false; Invalidate(); };
        }

        protected override void OnResize(EventArgs eventargs)
        {
            base.OnResize(eventargs);
            if (actionButton != null)
            {
                actionButton.Location = new Point(Width - actionButton.Width - 14, (Height - actionButton.Height) / 2);
            }
            if (checkBox != null)
            {
                checkBox.Location = new Point(14, (Height - checkBox.Height) / 2);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);
            Color currentBg = isHovered ? Theme.BgCardHover : Theme.BgCard;

            using (SolidBrush brush = new SolidBrush(currentBg))
            {
                g.FillRectangle(brush, rect);
            }

            using (Pen pen = new Pen(isHovered ? Theme.AccentBlue : Theme.BorderCard, 1))
            {
                g.DrawRectangle(pen, rect);
            }

            int contentLeft = (checkBox != null && checkBox.Visible) ? 42 : 16;
            int rightBound = (actionButton != null && actionButton.Visible) ? actionButton.Left - 10 : Width - 16;
            int availWidth = Math.Max(50, rightBound - contentLeft);

            TextFormatFlags flags = TextFormatFlags.Left | TextFormatFlags.WordEllipsis | TextFormatFlags.NoPrefix;

            // Title
            Size titleSize = TextRenderer.MeasureText(title, Theme.FontTitle);
            Rectangle titleRect = new Rectangle(contentLeft, 12, Math.Min(titleSize.Width, availWidth), 22);
            TextRenderer.DrawText(g, title, Theme.FontTitle, titleRect, Theme.TextMain, flags);

            // Badge next to title
            if (!string.IsNullOrEmpty(badge))
            {
                int badgeX = titleRect.Right + 8;
                if (badgeX + 65 < rightBound)
                {
                    Size bSize = TextRenderer.MeasureText(badge, Theme.FontSmallBold);
                    Rectangle bRect = new Rectangle(badgeX, 13, bSize.Width + 10, 18);

                    using (SolidBrush bBg = new SolidBrush(Color.FromArgb(40, badgeColor)))
                    {
                        g.FillRectangle(bBg, bRect);
                    }
                    using (Pen bPen = new Pen(badgeColor, 1))
                    {
                        g.DrawRectangle(bPen, bRect);
                    }
                    TextRenderer.DrawText(g, badge, Theme.FontSmallBold, bRect, badgeColor,
                        TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
                }
            }

            // Description
            Rectangle descRect = new Rectangle(contentLeft, 38, availWidth, 32);
            TextRenderer.DrawText(g, description, Theme.FontSmall, descRect, Theme.TextSecondary, flags);
        }
    }

    public class ModernProgressBar : Control
    {
        private int val = 0;
        private int max = 100;
        private string customText = "";

        public int Value
        {
            get { return val; }
            set
            {
                val = Math.Max(0, Math.Min(max, value));
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return max; }
            set { max = Math.Max(1, value); Invalidate(); }
        }

        public string CustomText
        {
            get { return customText; }
            set { customText = value; Invalidate(); }
        }

        public ModernProgressBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.UserPaint, true);
            Size = new Size(200, 18);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            Rectangle rect = new Rectangle(0, 0, Width - 1, Height - 1);

            // Background
            using (SolidBrush bgBrush = new SolidBrush(Color.FromArgb(20, 24, 38)))
            {
                g.FillRectangle(bgBrush, rect);
            }
            using (Pen borderPen = new Pen(Theme.BorderCard, 1))
            {
                g.DrawRectangle(borderPen, rect);
            }

            // Fill
            if (val > 0)
            {
                int fillWidth = (int)(((double)val / max) * (Width - 2));
                Rectangle fillRect = new Rectangle(1, 1, fillWidth, Height - 2);

                using (LinearGradientBrush fillBrush = new LinearGradientBrush(
                    fillRect, Theme.AccentBlue, Theme.AccentIndigo, LinearGradientMode.Horizontal))
                {
                    g.FillRectangle(fillBrush, fillRect);
                }
            }

            // Text
            string disp = string.IsNullOrEmpty(customText) ? (val + "%") : customText;
            TextRenderer.DrawText(g, disp, Theme.FontSmallBold, rect, Theme.TextMain,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }
}
