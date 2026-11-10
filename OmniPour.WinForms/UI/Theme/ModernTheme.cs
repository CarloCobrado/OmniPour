using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace OmniPour.WinForms.UI.Theme
{
    public static class ModernTheme
    {
        // Color Palette
        public static readonly Color ObsidianDark     = Color.FromArgb(15, 23, 42);     // #0F172A (Sidebar)
        public static readonly Color ObsidianDarker   = Color.FromArgb(10, 15, 29);     // #0A0F1D (Sidebar header)
        public static readonly Color SidebarHover     = Color.FromArgb(30, 41, 59);     // #1E293B
        public static readonly Color AmberAccent      = Color.FromArgb(245, 158, 11);   // #F59E0B (Brand Gold/Amber)
        public static readonly Color AmberAccentDark  = Color.FromArgb(217, 119, 6);    // #D97706
        public static readonly Color CanvasBackground = Color.FromArgb(248, 250, 252);  // #F8FAFC (Workspace)
        public static readonly Color CardSurface      = Color.White;
        public static readonly Color CardBorder       = Color.FromArgb(226, 232, 240);  // #E2E8F0
        public static readonly Color TextPrimary      = Color.FromArgb(15, 23, 42);     // #0F172A
        public static readonly Color TextSecondary    = Color.FromArgb(100, 116, 139);  // #64748B
        public static readonly Color TextMuted        = Color.FromArgb(148, 163, 184);  // #94A3B8
        public static readonly Color EmeraldSuccess   = Color.FromArgb(16, 185, 129);   // #10B981
        public static readonly Color RoseDanger       = Color.FromArgb(239, 68, 68);    // #EF4444
        public static readonly Color IndigoInfo       = Color.FromArgb(99, 102, 241);   // #6366F1

        // Standard Fonts
        public static readonly Font FontDisplay     = new Font("Segoe UI", 16F, FontStyle.Bold);
        public static readonly Font FontTitle       = new Font("Segoe UI", 13F, FontStyle.Bold);
        public static readonly Font FontSubTitle    = new Font("Segoe UI", 10.5F, FontStyle.Bold);
        public static readonly Font FontBodyBold    = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        public static readonly Font FontBody        = new Font("Segoe UI", 9.5F, FontStyle.Regular);
        public static readonly Font FontSmall       = new Font("Segoe UI", 8.5F, FontStyle.Regular);
        public static readonly Font FontSmallBold   = new Font("Segoe UI", 8.5F, FontStyle.Bold);

        public static void StyleDataGridView(DataGridView dgv)
        {
            dgv.AutoGenerateColumns = false;
            dgv.DataError += (s, e) => { e.ThrowException = false; };
            dgv.BackgroundColor = Color.White;
            dgv.BorderStyle = BorderStyle.None;
            dgv.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dgv.GridColor = Color.FromArgb(241, 245, 249);
            dgv.EnableHeadersVisualStyles = false;
            dgv.RowHeadersVisible = false;
            dgv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgv.MultiSelect = false;
            dgv.AllowUserToResizeRows = false;
            dgv.RowTemplate.Height = 38;
            dgv.Font = FontBody;

            // Column Header Style
            dgv.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 41, 59);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = FontSubTitle;
            dgv.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 8, 0);
            dgv.ColumnHeadersHeight = 42;
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;

            // Default Cell Style
            dgv.DefaultCellStyle.BackColor = Color.White;
            dgv.DefaultCellStyle.ForeColor = TextPrimary;
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 243, 199); // Soft Amber tint
            dgv.DefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
            dgv.DefaultCellStyle.Padding = new Padding(8, 2, 8, 2);

            // Alternating Row Style
            dgv.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(248, 250, 252);
            dgv.AlternatingRowsDefaultCellStyle.SelectionBackColor = Color.FromArgb(254, 243, 199);
            dgv.AlternatingRowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(15, 23, 42);
        }

        public static GraphicsPath GetRoundedPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            float r2 = radius / 2f;
            if (radius <= 0)
            {
                path.AddRectangle(rect);
                return path;
            }

            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class ModernButton : Button
    {
        public int BorderRadius { get; set; } = 8;
        public Color NormalColor { get; set; } = ModernTheme.AmberAccentDark;
        public Color HoverColor { get; set; } = ModernTheme.AmberAccent;
        public Color PressedColor { get; set; } = Color.FromArgb(180, 83, 9);
        public Color BorderOutlineColor { get; set; } = Color.Transparent;
        public int BorderThickness { get; set; } = 0;

        private bool _isHovered;
        private bool _isPressed;

        public ModernButton()
        {
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            Font = ModernTheme.FontBodyBold;
            ForeColor = Color.White;
            Cursor = Cursors.Hand;
            Size = new Size(120, 38);
            UseMnemonic = false;
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true; Invalidate(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _isPressed = false; Invalidate(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs mevent) { _isPressed = true; Invalidate(); base.OnMouseDown(mevent); }
        protected override void OnMouseUp(MouseEventArgs mevent) { _isPressed = false; Invalidate(); base.OnMouseUp(mevent); }

        protected override void OnPaint(PaintEventArgs pevent)
        {
            var g = pevent.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // Fill background with parent color to avoid dark/black corners around rounded edges
            Color parentBg = Parent != null && Parent.BackColor != Color.Transparent ? Parent.BackColor : SystemColors.Control;
            using (var bgBrush = new SolidBrush(parentBg))
            {
                g.FillRectangle(bgBrush, pevent.ClipRectangle);
            }

            var bg = !Enabled ? Color.FromArgb(203, 213, 225) : (_isPressed ? PressedColor : (_isHovered ? HoverColor : NormalColor));
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);

            if (BorderRadius > 0)
            {
                using var path = ModernTheme.GetRoundedPath(rect, BorderRadius);
                using var brush = new SolidBrush(bg);
                g.FillPath(brush, path);

                if (BorderThickness > 0 && BorderOutlineColor != Color.Transparent)
                {
                    using var pen = new Pen(BorderOutlineColor, BorderThickness);
                    g.DrawPath(pen, path);
                }
            }
            else
            {
                using var brush = new SolidBrush(bg);
                g.FillRectangle(brush, rect);
            }

            // Draw Text & Image (NoPrefix prevents '&' from turning into underscores)
            TextRenderer.DrawText(g, Text, Font, rect, Enabled ? ForeColor : Color.FromArgb(148, 163, 184),
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    public class ModernCard : Panel
    {
        public int BorderRadius { get; set; } = 10;
        public Color BorderLineColor { get; set; } = ModernTheme.CardBorder;
        public Color SurfaceColor { get; set; } = ModernTheme.CardSurface;

        public ModernCard()
        {
            DoubleBuffered = true;
            BackColor = Color.Transparent;
            Padding = new Padding(16);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            Color parentBg = Parent != null && Parent.BackColor != Color.Transparent ? Parent.BackColor : ModernTheme.CanvasBackground;
            using (var bgBrush = new SolidBrush(parentBg))
            {
                e.Graphics.FillRectangle(bgBrush, e.ClipRectangle);
            }

            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            using var path = ModernTheme.GetRoundedPath(rect, BorderRadius);
            using var brush = new SolidBrush(SurfaceColor);
            g.FillPath(brush, path);

            using var pen = new Pen(BorderLineColor, 1);
            g.DrawPath(pen, path);
        }
    }
}
