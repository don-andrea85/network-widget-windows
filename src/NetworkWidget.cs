using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NetworkWidget
{
    public class WidgetForm : Form
    {
        private bool wifiEnabled = true;
        private bool ethernetEnabled = true;
        private bool dragging;
        private bool resizing;
        private Point dragStart;
        private Point resizeStart;
        private Size resizeStartSize;
        private Theme currentTheme;
        private readonly NotifyIcon trayIcon;
        private readonly Icon trayIconImage;
        private int themeIndex;

        private RectangleF themeRect;
        private RectangleF minimizeRect;
        private RectangleF closeRect;
        private RectangleF wifiToggleRect;
        private RectangleF ethernetToggleRect;
        private RectangleF gripRect;

        private static readonly Size DefaultWidgetSize = new Size(380, 300);
        private static readonly Size MinWidgetSize = new Size(340, 268);
        private static readonly Size MaxWidgetSize = new Size(560, 500);
        private static readonly Color OffColor = Color.FromArgb(210, 50, 45);

        private static readonly Theme[] Themes =
        {
            new Theme("Windows", Color.FromArgb(246, 248, 252), Color.White, Color.FromArgb(0, 103, 192), Color.FromArgb(29, 33, 41), Color.FromArgb(99, 105, 113), Color.FromArgb(30, 164, 92)),
            new Theme("Smeraldo", Color.FromArgb(241, 249, 246), Color.White, Color.FromArgb(0, 137, 123), Color.FromArgb(20, 43, 41), Color.FromArgb(85, 103, 101), Color.FromArgb(28, 166, 94)),
            new Theme("Viola", Color.FromArgb(248, 246, 252), Color.White, Color.FromArgb(103, 80, 164), Color.FromArgb(38, 32, 54), Color.FromArgb(95, 87, 112), Color.FromArgb(42, 166, 99)),
            new Theme("Grafite", Color.FromArgb(239, 242, 246), Color.FromArgb(250, 251, 253), Color.FromArgb(72, 86, 104), Color.FromArgb(25, 31, 39), Color.FromArgb(92, 101, 113), Color.FromArgb(34, 158, 97))
        };

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("User32.dll")]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public WidgetForm()
        {
            Text = "Network Widget";
            Size = LoadWidgetSize();
            MinimumSize = MinWidgetSize;
            MaximumSize = MaxWidgetSize;
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            ShowInTaskbar = false;
            Opacity = 0.98;
            DoubleBuffered = true;
            ResizeRedraw = true;

            Rectangle screen = Screen.PrimaryScreen.WorkingArea;
            Location = new Point(screen.Right - Width - 24, screen.Top + 120);

            themeIndex = LoadThemeIndex();
            currentTheme = Themes[themeIndex];
            trayIconImage = CreateTrayIcon();
            trayIcon = CreateNotifyIcon(trayIconImage);
            UpdateWindowRegion();

            Shown += async (s, e) => await RefreshStates();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.CompositingQuality = CompositingQuality.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            g.Clear(currentTheme.Background);

            LayoutRects();
            float scale = ScaleFactor;

            using (Font titleFont = new Font("Segoe UI", 12.2f * scale, FontStyle.Bold))
            using (Font smallFont = new Font("Segoe UI", 8.5f * scale, FontStyle.Regular))
            using (Font buttonFont = new Font("Segoe UI", 8.4f * scale, FontStyle.Bold))
            using (Font nameFont = new Font("Segoe UI", 13.8f * scale, FontStyle.Bold))
            using (Font stateFont = new Font("Segoe UI", 11.6f * scale, FontStyle.Regular))
            using (SolidBrush textBrush = new SolidBrush(currentTheme.Text))
            using (SolidBrush mutedBrush = new SolidBrush(currentTheme.MutedText))
            {
                g.DrawString("Rete", titleFont, textBrush, 16f * scale, 17f * scale);
                g.DrawString("Widget desktop", smallFont, mutedBrush, 76f * scale, 22f * scale);

                DrawButton(g, themeRect, currentTheme.Name, buttonFont, currentTheme.Card, currentTheme.Text, true);
                DrawButton(g, minimizeRect, "_", buttonFont, Color.FromArgb(235, 239, 245), currentTheme.Text, false);
                DrawButton(g, closeRect, "X", buttonFont, currentTheme.Accent, Color.White, false);

                DrawNetworkCard(g, "Wi-Fi", wifiEnabled, wifiToggleRect, 74f * scale, nameFont, stateFont);
                DrawNetworkCard(g, "Ethernet", ethernetEnabled, ethernetToggleRect, 179f * scale, nameFont, stateFont);
                DrawGrip(g);
            }
        }

        private void LayoutRects()
        {
            float scale = ScaleFactor;
            themeRect = new RectangleF(226f * scale, 13f * scale, 84f * scale, 32f * scale);
            minimizeRect = new RectangleF(318f * scale, 13f * scale, 28f * scale, 32f * scale);
            closeRect = new RectangleF(352f * scale, 13f * scale, 28f * scale, 32f * scale);
            wifiToggleRect = new RectangleF(28f * scale, 96f * scale, 86f * scale, 48f * scale);
            ethernetToggleRect = new RectangleF(28f * scale, 201f * scale, 86f * scale, 48f * scale);
            gripRect = new RectangleF(ClientSize.Width - 22f * scale, ClientSize.Height - 22f * scale, 14f * scale, 14f * scale);
        }

        private float ScaleFactor
        {
            get
            {
                float sx = ClientSize.Width / (float)DefaultWidgetSize.Width;
                float sy = ClientSize.Height / (float)DefaultWidgetSize.Height;
                return Math.Max(0.68f, Math.Min(sx, sy));
            }
        }

        private void DrawNetworkCard(Graphics g, string name, bool enabled, RectangleF toggleRect, float top, Font nameFont, Font stateFont)
        {
            float scale = ScaleFactor;
            RectangleF card = new RectangleF(14f * scale, top, 352f * scale, 92f * scale);
            DrawRoundRect(g, card, 14f * scale, currentTheme.Card);

            DrawToggle(g, toggleRect, enabled);

            using (SolidBrush textBrush = new SolidBrush(currentTheme.Text))
            using (SolidBrush stateBrush = new SolidBrush(enabled ? currentTheme.SwitchOn : OffColor))
            {
                RectangleF nameRect = new RectangleF(132f * scale, top + 20f * scale, 138f * scale, 28f * scale);
                RectangleF stateRect = new RectangleF(133f * scale, top + 51f * scale, 125f * scale, 26f * scale);
                DrawTextFit(g, name, nameFont, textBrush, nameRect, FontStyle.Bold);
                DrawTextFit(g, enabled ? "Attivo" : "Spento", stateFont, stateBrush, stateRect, FontStyle.Regular);
            }

            RectangleF icon = new RectangleF(292f * scale, top + 24f * scale, 42f * scale, 44f * scale);
            if (name == "Wi-Fi")
                DrawWifiIcon(g, icon, enabled ? currentTheme.Accent : OffColor);
            else
                DrawEthernetIcon(g, icon, enabled ? currentTheme.Accent : OffColor);
        }

        private void DrawButton(Graphics g, RectangleF rect, string text, Font font, Color background, Color foreground, bool border)
        {
            DrawRoundRect(g, rect, 3f * ScaleFactor, background);

            if (border)
            {
                using (Pen pen = new Pen(Color.FromArgb(130, currentTheme.MutedText), Math.Max(1f, ScaleFactor)))
                    g.DrawRectangle(pen, Rectangle.Round(rect));
            }

            TextRenderer.DrawText(g, text, font, Rectangle.Round(rect), foreground, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);
        }

        private void DrawTextFit(Graphics g, string text, Font baseFont, Brush brush, RectangleF rect, FontStyle style)
        {
            float size = baseFont.Size;
            SizeF measured;

            do
            {
                using (Font testFont = new Font(baseFont.FontFamily, size, style, GraphicsUnit.Point))
                    measured = g.MeasureString(text, testFont);

                if (measured.Width <= rect.Width || size <= 7f)
                    break;

                size -= 0.5f;
            }
            while (true);

            using (Font drawFont = new Font(baseFont.FontFamily, size, style, GraphicsUnit.Point))
            using (StringFormat format = new StringFormat { Alignment = StringAlignment.Near, LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter, FormatFlags = StringFormatFlags.NoWrap })
            {
                g.DrawString(text, drawFont, brush, rect, format);
            }
        }

        private void DrawToggle(Graphics g, RectangleF rect, bool enabled)
        {
            Color track = enabled ? currentTheme.SwitchOn : OffColor;
            DrawRoundRect(g, rect, rect.Height / 2f, track);

            float knob = rect.Height - 16f * ScaleFactor;
            float x = enabled ? rect.Right - knob - 8f * ScaleFactor : rect.X + 8f * ScaleFactor;
            RectangleF knobRect = new RectangleF(x, rect.Y + 8f * ScaleFactor, knob, knob);

            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(30, 0, 0, 0)))
                g.FillEllipse(shadow, knobRect.X, knobRect.Y + 2f * ScaleFactor, knobRect.Width, knobRect.Height);

            using (SolidBrush brush = new SolidBrush(Color.White))
                g.FillEllipse(brush, knobRect);
        }

        private void DrawWifiIcon(Graphics g, RectangleF r, Color color)
        {
            float stroke = Math.Max(2f, r.Width * 0.075f);
            using (Pen pen = new Pen(color, stroke))
            using (SolidBrush brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;

                float cx = r.X + r.Width / 2f;
                g.DrawArc(pen, r.X + r.Width * 0.08f, r.Y + r.Height * 0.16f, r.Width * 0.84f, r.Height * 0.44f, 205, 130);
                g.DrawArc(pen, r.X + r.Width * 0.24f, r.Y + r.Height * 0.34f, r.Width * 0.52f, r.Height * 0.28f, 205, 130);
                g.DrawArc(pen, r.X + r.Width * 0.40f, r.Y + r.Height * 0.51f, r.Width * 0.20f, r.Height * 0.12f, 205, 130);

                float dot = Math.Max(4f, r.Width * 0.12f);
                g.FillEllipse(brush, cx - dot / 2f, r.Y + r.Height * 0.74f, dot, dot);
            }
        }

        private void DrawEthernetIcon(Graphics g, RectangleF r, Color color)
        {
            float stroke = Math.Max(2f, r.Width * 0.07f);
            using (Pen pen = new Pen(color, stroke))
            using (SolidBrush brush = new SolidBrush(color))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                pen.LineJoin = LineJoin.Round;

                RectangleF screen = new RectangleF(r.X + r.Width * 0.12f, r.Y + r.Height * 0.15f, r.Width * 0.48f, r.Height * 0.48f);
                using (GraphicsPath screenPath = RoundedRect(screen, r.Width * 0.06f))
                    g.DrawPath(pen, screenPath);

                g.DrawLine(pen, r.X + r.Width * 0.36f, r.Y + r.Height * 0.65f, r.X + r.Width * 0.36f, r.Y + r.Height * 0.81f);
                g.DrawLine(pen, r.X + r.Width * 0.23f, r.Y + r.Height * 0.84f, r.X + r.Width * 0.49f, r.Y + r.Height * 0.84f);

                RectangleF plug = new RectangleF(r.X + r.Width * 0.62f, r.Y + r.Height * 0.14f, r.Width * 0.25f, r.Height * 0.34f);
                using (GraphicsPath plugPath = RoundedRect(plug, r.Width * 0.045f))
                    g.DrawPath(pen, plugPath);

                g.DrawLine(pen, r.X + r.Width * 0.75f, r.Y + r.Height * 0.50f, r.X + r.Width * 0.75f, r.Y + r.Height * 0.86f);
                g.FillRectangle(brush, r.X + r.Width * 0.67f, r.Y + r.Height * 0.06f, r.Width * 0.045f, r.Height * 0.08f);
                g.FillRectangle(brush, r.X + r.Width * 0.78f, r.Y + r.Height * 0.06f, r.Width * 0.045f, r.Height * 0.08f);
            }
        }

        private void DrawGrip(Graphics g)
        {
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, currentTheme.MutedText)))
            {
                float s = Math.Max(2.2f, 3f * ScaleFactor);
                g.FillEllipse(brush, gripRect.Right - s, gripRect.Bottom - s, s, s);
                g.FillEllipse(brush, gripRect.Right - s * 3f, gripRect.Bottom - s, s, s);
                g.FillEllipse(brush, gripRect.Right - s, gripRect.Bottom - s * 3f, s, s);
            }
        }

        private static void DrawRoundRect(Graphics g, RectangleF rect, float radius, Color color)
        {
            using (GraphicsPath path = RoundedRect(rect, radius))
            using (SolidBrush brush = new SolidBrush(color))
                g.FillPath(brush, path);
        }

        private static GraphicsPath RoundedRect(RectangleF rect, float radius)
        {
            float d = radius * 2f;
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            LayoutRects();
            PointF p = e.Location;

            if (closeRect.Contains(p))
            {
                Close();
                return;
            }

            if (themeRect.Contains(p))
            {
                NextTheme();
                return;
            }

            if (minimizeRect.Contains(p))
            {
                HideToTray();
                return;
            }

            if (wifiToggleRect.Contains(p))
            {
                BeginInvoke(new Action(async () => await ToggleAdapter("Wi-Fi")));
                return;
            }

            if (ethernetToggleRect.Contains(p))
            {
                BeginInvoke(new Action(async () => await ToggleAdapter("Ethernet")));
                return;
            }

            if (gripRect.Contains(p))
            {
                resizing = true;
                resizeStart = Cursor.Position;
                resizeStartSize = Size;
                Cursor = Cursors.SizeNWSE;
                return;
            }

            dragging = true;
            dragStart = e.Location;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            LayoutRects();

            if (resizing)
            {
                int dx = Cursor.Position.X - resizeStart.X;
                int dy = Cursor.Position.Y - resizeStart.Y;
                int delta = Math.Max(dx, dy);
                int width = Math.Max(MinWidgetSize.Width, Math.Min(MaxWidgetSize.Width, resizeStartSize.Width + delta));
                int height = Math.Max(MinWidgetSize.Height, Math.Min(MaxWidgetSize.Height, (int)(width * (DefaultWidgetSize.Height / (float)DefaultWidgetSize.Width))));
                Size = new Size(width, height);
                UpdateWindowRegion();
                Invalidate();
                return;
            }

            if (dragging)
            {
                Point current = PointToScreen(e.Location);
                Location = new Point(current.X - dragStart.X, current.Y - dragStart.Y);
                return;
            }

            Cursor = gripRect.Contains(e.Location) ? Cursors.SizeNWSE : Cursors.Default;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            dragging = false;

            if (resizing)
            {
                resizing = false;
                SaveWidgetSize(Size);
                Cursor = Cursors.Default;
            }
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateWindowRegion();
            Invalidate();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (trayIcon != null)
                    trayIcon.Dispose();
                if (trayIconImage != null)
                    trayIconImage.Dispose();
            }

            base.Dispose(disposing);
        }

        private void UpdateWindowRegion()
        {
            if (Width > 0 && Height > 0)
                Region = Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, Math.Max(14, (int)(22 * ScaleFactor)), Math.Max(14, (int)(22 * ScaleFactor))));
        }

        private void NextTheme()
        {
            themeIndex = (themeIndex + 1) % Themes.Length;
            currentTheme = Themes[themeIndex];
            SaveThemeIndex(themeIndex);
            Invalidate();
        }

        private void HideToTray()
        {
            Hide();
            trayIcon.Visible = true;
            trayIcon.ShowBalloonTip(1200, "Network Widget", "Il widget resta attivo nell'area notifiche.", ToolTipIcon.Info);
        }

        private void ShowFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
        }

        private void ExitFromTray()
        {
            trayIcon.Visible = false;
            Close();
        }

        private NotifyIcon CreateNotifyIcon(Icon icon)
        {
            NotifyIcon notify = new NotifyIcon();
            notify.Icon = icon;
            notify.Text = "Network Widget";
            notify.Visible = true;
            notify.DoubleClick += (s, e) => ShowFromTray();
            notify.ContextMenu = new ContextMenu(new[]
            {
                new MenuItem("Apri", (s, e) => ShowFromTray()),
                new MenuItem("Esci", (s, e) => ExitFromTray())
            });
            return notify;
        }

        private Icon CreateTrayIcon()
        {
            using (Bitmap bitmap = new Bitmap(32, 32, System.Drawing.Imaging.PixelFormat.Format32bppArgb))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);

                using (SolidBrush background = new SolidBrush(Color.FromArgb(0, 103, 192)))
                    g.FillEllipse(background, 2, 2, 28, 28);

                using (Pen pen = new Pen(Color.White, 2.4f))
                using (SolidBrush brush = new SolidBrush(Color.White))
                {
                    pen.StartCap = LineCap.Round;
                    pen.EndCap = LineCap.Round;
                    g.DrawArc(pen, 8, 9, 16, 12, 210, 120);
                    g.DrawArc(pen, 12, 14, 8, 6, 210, 120);
                    g.FillEllipse(brush, 14, 21, 4, 4);
                }

                IntPtr handle = bitmap.GetHicon();
                try
                {
                    return (Icon)Icon.FromHandle(handle).Clone();
                }
                finally
                {
                    DestroyIcon(handle);
                }
            }
        }

        private async Task ToggleAdapter(string adapterName)
        {
            try
            {
                bool enabled = await IsAdapterEnabled(adapterName);
                string action = enabled ? "disable" : "enable";

                if (!RunElevatedTask(adapterName, action))
                {
                    string exePath = Application.ExecutablePath;
                    string args = "--toggle \"" + adapterName + "\" " + action;

                    var startInfo = new ProcessStartInfo(exePath, args);
                    startInfo.UseShellExecute = true;
                    startInfo.Verb = "runas";
                    using (Process process = Process.Start(startInfo))
                        process.WaitForExit();
                }

                await Task.Delay(700);
                await RefreshStates();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Network Widget", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }

            await RefreshStates();
        }

        private async Task RefreshStates()
        {
            wifiEnabled = await IsAdapterEnabled("Wi-Fi");
            ethernetEnabled = await IsAdapterEnabled("Ethernet");
            Invalidate();
        }

        private async Task<bool> IsAdapterEnabled(string adapterName)
        {
            string output = await RunNetsh("interface show interface name=\"" + adapterName + "\"");
            string lower = output.ToLowerInvariant();

            if (lower.Contains("disabilitato") || lower.Contains("disabled"))
                return false;

            if (lower.Contains("abilitato") || lower.Contains("enabled"))
                return true;

            throw new InvalidOperationException("Non riesco a leggere lo stato di " + adapterName + ".");
        }

        private static Task<string> RunNetsh(string arguments)
        {
            return Task.Run(() =>
            {
                var startInfo = new ProcessStartInfo("netsh.exe", arguments);
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;
                startInfo.StandardOutputEncoding = Encoding.Default;
                startInfo.StandardErrorEncoding = Encoding.Default;

                using (var process = Process.Start(startInfo))
                {
                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0)
                        throw new InvalidOperationException(string.IsNullOrWhiteSpace(error) ? output : error);

                    return output;
                }
            });
        }

        private static bool RunElevatedTask(string adapterName, string action)
        {
            string taskName = GetTaskName(adapterName, action);

            try
            {
                var startInfo = new ProcessStartInfo("schtasks.exe", "/Run /TN \"" + taskName + "\"");
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardOutput = true;
                startInfo.RedirectStandardError = true;

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();
                    return process.ExitCode == 0;
                }
            }
            catch
            {
                return false;
            }
        }

        private static string GetTaskName(string adapterName, string action)
        {
            string adapter = adapterName.Equals("Wi-Fi", StringComparison.OrdinalIgnoreCase) ? "WiFi" : "Ethernet";
            string suffix = action == "enable" ? "Enable" : "Disable";
            return "\\NetworkWidget\\" + adapter + suffix;
        }

        private static string SettingsPath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetworkWidget", "theme.txt"); }
        }

        private static string SizePath
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NetworkWidget", "size.txt"); }
        }

        private static int LoadThemeIndex()
        {
            try
            {
                int value;
                if (File.Exists(SettingsPath) && int.TryParse(File.ReadAllText(SettingsPath), out value))
                    return Math.Max(0, Math.Min(value, Themes.Length - 1));
            }
            catch
            {
            }

            return 0;
        }

        private static void SaveThemeIndex(int value)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath));
                File.WriteAllText(SettingsPath, value.ToString());
            }
            catch
            {
            }
        }

        private static Size LoadWidgetSize()
        {
            try
            {
                if (File.Exists(SizePath))
                {
                    string[] parts = File.ReadAllText(SizePath).Split(',');
                    if (parts.Length == 2)
                    {
                        int w;
                        int h;
                        if (int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out w) &&
                            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out h))
                        {
                            w = Math.Max(MinWidgetSize.Width, Math.Min(MaxWidgetSize.Width, w));
                            h = Math.Max(MinWidgetSize.Height, Math.Min(MaxWidgetSize.Height, h));
                            return new Size(w, h);
                        }
                    }
                }
            }
            catch
            {
            }

            return new Size((int)(DefaultWidgetSize.Width * 0.92f), (int)(DefaultWidgetSize.Height * 0.92f));
        }

        private static void SaveWidgetSize(Size size)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SizePath));
                File.WriteAllText(SizePath, size.Width.ToString(CultureInfo.InvariantCulture) + "," + size.Height.ToString(CultureInfo.InvariantCulture));
            }
            catch
            {
            }
        }

        [STAThread]
        public static void Main(string[] args)
        {
            if (args.Length >= 3 && args[0] == "--toggle")
            {
                string adapterName = args[1];
                string action = args[2] == "enable" ? "enabled" : "disabled";
                RunNetsh("interface set interface name=\"" + adapterName + "\" admin=" + action).Wait();
                return;
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool created;
            using (var mutex = new Mutex(true, "AndreNetworkWidgetSingleInstance", out created))
            {
                if (!created)
                    return;

                Application.Run(new WidgetForm());
            }
        }

        private struct Theme
        {
            public readonly string Name;
            public readonly Color Background;
            public readonly Color Card;
            public readonly Color Accent;
            public readonly Color Text;
            public readonly Color MutedText;
            public readonly Color SwitchOn;

            public Theme(string name, Color background, Color card, Color accent, Color text, Color mutedText, Color switchOn)
            {
                Name = name;
                Background = background;
                Card = card;
                Accent = accent;
                Text = text;
                MutedText = mutedText;
                SwitchOn = switchOn;
            }
        }
    }
}
