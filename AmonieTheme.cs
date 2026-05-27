using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AmonieNomnomPublisher;

public static class AmonieTheme
{
    public static readonly Color Background = Color.FromArgb(8, 12, 18);
    public static readonly Color Surface = Color.FromArgb(14, 20, 29);
    public static readonly Color Surface2 = Color.FromArgb(20, 29, 40);
    public static readonly Color Surface3 = Color.FromArgb(26, 38, 52);
    public static readonly Color Border = Color.FromArgb(50, 76, 92);
    public static readonly Color BorderSoft = Color.FromArgb(35, 53, 66);
    public static readonly Color Accent = Color.FromArgb(103, 236, 181);
    public static readonly Color Accent2 = Color.FromArgb(94, 184, 255);
    public static readonly Color Warning = Color.FromArgb(255, 191, 105);
    public static readonly Color Danger = Color.FromArgb(255, 99, 118);
    public static readonly Color Text = Color.FromArgb(235, 246, 248);
    public static readonly Color Muted = Color.FromArgb(145, 164, 176);
    public static readonly Color Deep = Color.FromArgb(5, 8, 12);

    public static Font TitleFont(float size = 20f) => new("Segoe UI Variable Display", size, FontStyle.Bold, GraphicsUnit.Point);
    public static Font TextFont(float size = 9.5f, FontStyle style = FontStyle.Regular) => new("Segoe UI", size, style, GraphicsUnit.Point);
    public static Font MonoFont(float size = 9.2f, FontStyle style = FontStyle.Regular) => new("Cascadia Mono", size, style, GraphicsUnit.Point);

    public static GraphicsPath RoundedRect(Rectangle bounds, int radius)
    {
        var path = new GraphicsPath();
        var diameter = radius * 2;
        var arc = new Rectangle(bounds.Location, new Size(diameter, diameter));
        path.AddArc(arc, 180, 90);
        arc.X = bounds.Right - diameter;
        path.AddArc(arc, 270, 90);
        arc.Y = bounds.Bottom - diameter;
        path.AddArc(arc, 0, 90);
        arc.X = bounds.Left;
        path.AddArc(arc, 90, 90);
        path.CloseFigure();
        return path;
    }
}

public sealed class AmoniePanel : Panel
{
    public int Radius { get; set; } = 18;
    public Color Fill { get; set; } = AmonieTheme.Surface;
    public Color Stroke { get; set; } = AmonieTheme.BorderSoft;
    public bool Glow { get; set; }

    public AmoniePanel()
    {
        DoubleBuffered = true;
        Padding = new Padding(18);
        BackColor = Color.Transparent;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        using var path = AmonieTheme.RoundedRect(rect, Radius);
        using var brush = new SolidBrush(Fill);
        e.Graphics.FillPath(brush, path);
        if (Glow)
        {
            using var glowPen = new Pen(Color.FromArgb(70, AmonieTheme.Accent), 3f);
            e.Graphics.DrawPath(glowPen, path);
        }
        using var pen = new Pen(Stroke, 1f);
        e.Graphics.DrawPath(pen, path);
        base.OnPaint(e);
    }
}

public sealed class AmonieButton : Button
{
    public bool Primary { get; set; }
    public bool Danger { get; set; }

    public AmonieButton()
    {
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        Cursor = Cursors.Hand;
        Height = 42;
        Font = AmonieTheme.TextFont(10f, FontStyle.Bold);
        BackColor = Color.Transparent;
        ForeColor = AmonieTheme.Text;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        rect.Width -= 1;
        rect.Height -= 1;
        var baseColor = Primary ? AmonieTheme.Accent : Danger ? AmonieTheme.Danger : AmonieTheme.Surface3;
        var textColor = Primary ? Color.FromArgb(3, 15, 11) : AmonieTheme.Text;
        if (!Enabled)
        {
            baseColor = Color.FromArgb(42, 50, 58);
            textColor = AmonieTheme.Muted;
        }
        using var path = AmonieTheme.RoundedRect(rect, 13);
        using var brush = new SolidBrush(baseColor);
        pevent.Graphics.FillPath(brush, path);
        using var pen = new Pen(Primary ? AmonieTheme.Accent : AmonieTheme.Border, 1f);
        pevent.Graphics.DrawPath(pen, path);
        TextRenderer.DrawText(pevent.Graphics, Text, Font, rect, textColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}

public sealed class AmonieTextBox : TextBox
{
    public AmonieTextBox()
    {
        BorderStyle = BorderStyle.FixedSingle;
        BackColor = AmonieTheme.Surface3;
        ForeColor = AmonieTheme.Text;
        Font = AmonieTheme.TextFont();
    }
}

public sealed class AmonieHeader : Panel
{
    public string HeaderText { get; set; } = "";
    public string SubText { get; set; } = "";

    public AmonieHeader()
    {
        DoubleBuffered = true;
        Height = 104;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using var bgBrush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(16, 24, 34), Color.FromArgb(8, 12, 18), 15f);
        e.Graphics.FillRectangle(bgBrush, ClientRectangle);
        using var accentPen = new Pen(Color.FromArgb(120, AmonieTheme.Accent), 2f);
        e.Graphics.DrawLine(accentPen, 22, Height - 16, 180, Height - 16);
        TextRenderer.DrawText(e.Graphics, HeaderText, AmonieTheme.TitleFont(20f), new Rectangle(22, 18, Width - 44, 36), AmonieTheme.Text, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        TextRenderer.DrawText(e.Graphics, SubText, AmonieTheme.TextFont(9.5f), new Rectangle(24, 55, Width - 44, 30), AmonieTheme.Muted, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
    }
}
