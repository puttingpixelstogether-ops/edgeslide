using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using static EdgeSlide.NativeMethods;

namespace EdgeSlide;

/// <summary>
/// Slim chromeless HUD shown during an active gesture. Top-most, never activates,
/// hidden from Alt+Tab and the taskbar. Auto-hides after a short idle period.
/// </summary>
public sealed class OverlayForm : Form
{
    private const int AutoHideMs = 1500;
    private const int HudWidth = 96;
    private const int HudHeight = 240;
    private const int Margin = 48;

    private readonly System.Windows.Forms.Timer _hideTimer;
    private double _value;          // 0..1
    private StripAction _action = StripAction.Volume;
    private StripSide _side = StripSide.Right;

    public OverlayForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(HudWidth, HudHeight);
        BackColor = Color.FromArgb(20, 20, 24);
        Opacity = 0.0;
        DoubleBuffered = true;
        ControlBox = false;
        Text = "EdgeSlide HUD";

        _hideTimer = new System.Windows.Forms.Timer { Interval = AutoHideMs };
        _hideTimer.Tick += (_, _) => Hide();
    }

    // Never take focus when shown.
    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ExStyle |= WS_EX_TOOLWINDOW | WS_EX_NOACTIVATE | WS_EX_LAYERED | WS_EX_TRANSPARENT;
            return cp;
        }
    }

    /// <summary>Show/refresh the HUD with a new value. Safe to call from any thread.</summary>
    public void ShowValue(StripSide side, StripAction action, double value)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(() => ShowValue(side, action, value)));
            return;
        }

        _side = side;
        _action = action;
        _value = Math.Clamp(value, 0.0, 1.0);

        PositionForSide(side);

        if (!Visible)
        {
            Opacity = 0.92;
            Show();
        }
        Invalidate();

        _hideTimer.Stop();
        _hideTimer.Start();
    }

    /// <summary>Hide the HUD immediately (e.g. on finger lift). Safe from any thread.</summary>
    public void HideNow()
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            BeginInvoke(new Action(HideNow));
            return;
        }
        _hideTimer.Stop();
        if (Visible) Hide();
    }

    private void PositionForSide(StripSide side)
    {
        Rectangle wa = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1280, 720);
        int y = wa.Bottom - Height - Margin;
        int x = side == StripSide.Left
            ? wa.Left + Margin
            : wa.Right - Width - Margin;
        Location = new Point(x, y);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

        var bg = new Rectangle(0, 0, Width - 1, Height - 1);
        using (var path = RoundedRect(bg, 16))
        using (var fill = new SolidBrush(Color.FromArgb(235, 28, 28, 34)))
            g.FillPath(fill, path);

        // Icon area (top)
        var iconRect = new Rectangle(0, 14, Width, 34);
        DrawIcon(g, iconRect);

        // Vertical track
        int trackW = 14;
        int trackTop = 58;
        int trackBottom = Height - 52;
        int trackX = (Width - trackW) / 2;
        var track = new Rectangle(trackX, trackTop, trackW, trackBottom - trackTop);

        using (var trackPath = RoundedRect(track, trackW / 2))
        using (var trackBrush = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
            g.FillPath(trackBrush, trackPath);

        // Filled portion (from bottom up)
        int fillH = (int)Math.Round(track.Height * _value);
        var fillRect = new Rectangle(track.X, track.Bottom - fillH, track.Width, fillH);
        Color accent = _action == StripAction.Brightness
            ? Color.FromArgb(255, 224, 130)  // warm (sun)
            : Color.FromArgb(120, 200, 255);  // cool (volume)
        if (fillH > 0)
        {
            using var fillPath = RoundedRect(fillRect, Math.Min(track.Width / 2, fillH / 2));
            using var fillBrush = new SolidBrush(accent);
            g.FillPath(fillBrush, fillPath);
        }

        // Percentage label (bottom)
        int pct = (int)Math.Round(_value * 100);
        using var labelFont = new Font("Segoe UI", 12f, FontStyle.Bold);
        using var labelBrush = new SolidBrush(Color.White);
        var labelRect = new Rectangle(0, Height - 40, Width, 28);
        var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString($"{pct}%", labelFont, labelBrush, labelRect, fmt);
    }

    private void DrawIcon(Graphics g, Rectangle area)
    {
        Color c = _action == StripAction.Brightness
            ? Color.FromArgb(255, 224, 130)
            : Color.FromArgb(160, 215, 255);
        using var pen = new Pen(c, 2.2f);
        using var brush = new SolidBrush(c);

        int cx = area.X + area.Width / 2;
        int cy = area.Y + area.Height / 2;

        if (_action == StripAction.Brightness)
        {
            // Sun: circle + rays
            int r = 7;
            g.FillEllipse(brush, cx - r, cy - r, r * 2, r * 2);
            for (int i = 0; i < 8; i++)
            {
                double ang = i * Math.PI / 4;
                int x1 = cx + (int)(Math.Cos(ang) * (r + 3));
                int y1 = cy + (int)(Math.Sin(ang) * (r + 3));
                int x2 = cx + (int)(Math.Cos(ang) * (r + 8));
                int y2 = cy + (int)(Math.Sin(ang) * (r + 8));
                g.DrawLine(pen, x1, y1, x2, y2);
            }
        }
        else
        {
            // Speaker: trapezoid body + sound arcs
            var body = new[]
            {
                new Point(cx - 10, cy - 4),
                new Point(cx - 4, cy - 4),
                new Point(cx + 2, cy - 10),
                new Point(cx + 2, cy + 10),
                new Point(cx - 4, cy + 4),
                new Point(cx - 10, cy + 4)
            };
            g.FillPolygon(brush, body);
            g.DrawArc(pen, cx + 2, cy - 9, 12, 18, -50, 100);
            g.DrawArc(pen, cx + 2, cy - 13, 18, 26, -50, 100);
        }
    }

    private static GraphicsPath RoundedRect(Rectangle r, int radius)
    {
        int d = Math.Max(1, radius * 2);
        var path = new GraphicsPath();
        if (d >= r.Width) d = Math.Max(1, r.Width - 1);
        if (d >= r.Height) d = Math.Max(1, r.Height - 1);
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _hideTimer.Dispose();
        base.Dispose(disposing);
    }
}
