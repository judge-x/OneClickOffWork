using System.Drawing;
using System.IO;
using System.Windows;
using Forms = System.Windows.Forms;

namespace OneClickOffWork.Services;

public sealed class TrayService : IDisposable
{
    private readonly LogService _log;
    private readonly NotificationService _notifications;
    private Forms.NotifyIcon? _icon;

    public event EventHandler? OpenRequested;
    public event EventHandler? OffWorkRequested;
    public event EventHandler? RemindersRequested;
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;

    public TrayService(LogService log, NotificationService notifications)
    {
        _log = log;
        _notifications = notifications;
    }

    public void Initialize(Window owner)
    {
        try
        {
            var menu = CreateMenu();
            menu.Items.Add("打开主界面", null, (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("一键下班", null, (_, _) => OffWorkRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("注意事项管理", null, (_, _) => RemindersRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add("设置", null, (_, _) => SettingsRequested?.Invoke(this, EventArgs.Empty));
            menu.Items.Add(new Forms.ToolStripSeparator());
            menu.Items.Add("退出", null, (_, _) => ExitRequested?.Invoke(this, EventArgs.Empty));

            _icon = new Forms.NotifyIcon
            {
                Text = "一键下班",
                Icon = LoadTrayIcon(),
                ContextMenuStrip = menu,
                Visible = true
            };
            _icon.DoubleClick += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _log.Error("托盘图标加载失败", ex);
            _notifications.Toast("托盘图标加载失败，但软件仍可继续运行");
        }
    }

    private static Forms.ContextMenuStrip CreateMenu()
    {
        var menu = new Forms.ContextMenuStrip
        {
            ShowImageMargin = false,
            Font = new Font(new FontFamily("Microsoft YaHei UI"), 10F, System.Drawing.FontStyle.Regular),
            BackColor = Color.FromArgb(247, 249, 253),
            ForeColor = Color.FromArgb(23, 32, 51),
            Padding = new Forms.Padding(8),
            Renderer = new TrayMenuRenderer()
        };

        menu.Opening += (_, _) =>
        {
            foreach (Forms.ToolStripItem item in menu.Items)
            {
                item.AutoSize = false;
                item.Height = item is Forms.ToolStripSeparator ? 8 : 34;
                item.Padding = new Forms.Padding(12, 0, 12, 0);
            }
        };

        return menu;
    }

    private static Icon LoadTrayIcon()
    {
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(iconPath))
        {
            return new Icon(iconPath);
        }

        var resource = System.Windows.Application.GetResourceStream(new Uri("Assets/AppIcon.ico", UriKind.Relative));
        if (resource?.Stream is null)
        {
            return SystemIcons.Application;
        }

        using var stream = resource.Stream;
        using var icon = new Icon(stream);
        return (Icon)icon.Clone();
    }

    public void ShowBalloon(string title, string message)
    {
        try
        {
            _icon?.ShowBalloonTip(3500, title, message, Forms.ToolTipIcon.Info);
        }
        catch { }
    }

    public void Dispose()
    {
        if (_icon is null) return;
        _icon.Visible = false;
        _icon.Icon?.Dispose();
        _icon.Dispose();
    }

    private sealed class TrayMenuRenderer : Forms.ToolStripProfessionalRenderer
    {
        private static readonly Color MenuBack = Color.FromArgb(247, 249, 253);
        private static readonly Color HoverBack = Color.FromArgb(232, 238, 248);
        private static readonly Color TextColor = Color.FromArgb(23, 32, 51);
        private static readonly Color SeparatorColor = Color.FromArgb(220, 228, 240);

        protected override void OnRenderToolStripBackground(Forms.ToolStripRenderEventArgs e)
        {
            using var brush = new SolidBrush(MenuBack);
            e.Graphics.FillRectangle(brush, e.AffectedBounds);
        }

        protected override void OnRenderMenuItemBackground(Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Selected)
            {
                return;
            }

            using var brush = new SolidBrush(HoverBack);
            var rect = new Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
            e.Graphics.FillRoundedRectangle(brush, rect, 8);
        }

        protected override void OnRenderItemText(Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = TextColor;
            e.TextRectangle = new Rectangle(12, e.TextRectangle.Y, e.TextRectangle.Width, e.TextRectangle.Height);
            base.OnRenderItemText(e);
        }

        protected override void OnRenderSeparator(Forms.ToolStripSeparatorRenderEventArgs e)
        {
            using var pen = new Pen(SeparatorColor);
            e.Graphics.DrawLine(pen, 10, e.Item.Height / 2, e.Item.Width - 10, e.Item.Height / 2);
        }
    }
}

internal static class GraphicsExtensions
{
    public static void FillRoundedRectangle(this Graphics graphics, Brush brush, Rectangle bounds, int radius)
    {
        using var path = new System.Drawing.Drawing2D.GraphicsPath();
        var diameter = radius * 2;
        path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
        path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
        path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
        path.AddArc(bounds.Left, bounds.Bottom - diameter, diameter, diameter, 90, 90);
        path.CloseFigure();
        graphics.FillPath(brush, path);
    }
}
