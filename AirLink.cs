using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;


class AirLink : Form
{
    readonly TextBox name = new TextBox();
    readonly Label status = new Label();
    readonly Label network = new Label();
    readonly Button start = new Button();
    readonly Button stop = new Button();
    readonly ComboBox quality = new ComboBox();
    readonly Label qualityHint = new Label();
    readonly CheckBox stable = new CheckBox();
    readonly Button restart = new Button();
    readonly Timer restartTimer = new Timer();
    bool restarting;
    static readonly string[] QualityArgs = { " -s 1920x1080@60 -fps 30", " -s 1920x1080@60 -fps 60", " -s 2560x1440@30 -fps 30", " -h265 -s 2560x1440@30 -fps 30", " -h265 -s 2732x2048@30 -fps 30", " -h265 -s 3840x2160@30 -fps 30" };
    readonly Timer timer = new Timer();
    Process receiver;
    string config;
    byte[] previous;
    bool configOwned;
    readonly string root = AppDomain.CurrentDomain.BaseDirectory;
    readonly string preferencesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AirLink", "preferences.xml");
    bool preferenceWarning;
    public AirLink(bool remember = true)
    {
        Text = "AirLink"; ClientSize = new Size(760, 610);
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        MinimumSize = new Size(776, 649); MaximumSize = new Size(776, 649);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Microsoft YaHei UI", 10); BackColor = Color.FromArgb(244, 247, 252);
        AddLabel("AirLink", 32, 24, 600, 48, 28, Color.FromArgb(26, 42, 69));
        AddLabel("接收设备名称", 36, 137, 650, 25, 10, Color.DimGray);
        name.SetBounds(36, 172, 440, 34); name.Text = "AirLink-" + Environment.MachineName;
        name.MaxLength = 48; Controls.Add(name);
        start.Text = "开始接收"; start.SetBounds(494, 169, 112, 38); start.BackColor = Color.FromArgb(44, 100, 230);
        start.ForeColor = Color.White; start.FlatStyle = FlatStyle.Flat; start.Click += delegate { StartReceiver(); }; Controls.Add(start);
        stop.Text = "停止"; stop.SetBounds(617, 169, 103, 38); stop.Enabled = false;
        stop.Click += delegate { StopReceiver(); }; Controls.Add(stop);
        status.SetBounds(36, 226, 685, 48); status.Text = "● 尚未启动"; status.ForeColor = Color.FromArgb(43, 91, 177); Controls.Add(status);
        network.SetBounds(36, 460, 682, 43); network.Font = new Font(Font.FontFamily, 9); Controls.Add(network); RefreshNetwork();
        var help = new Button { Text = "连接排查", Left = 36, Top = 523, Width = 110 }; help.Click += delegate {
            RefreshNetwork(); MessageBox.Show("1. 两台设备使用同一局域网，避免访客 Wi-Fi 和 AP 隔离。\n2. Windows 防火墙提示时允许接收组件访问专用网络。\n3. 如果启用了 VPN，请检查是否阻断本地网络。\n4. 首次运行可能提示安装 Bonjour 服务，需要 Windows 管理员权限。\n5. 画面冻结时，在 Apple设备 停止镜像后重新连接。\n\n接收进程启动不代表 Apple设备 已连接。受 DRM 保护的视频可能无法显示。", "连接排查"); }; Controls.Add(help);
        AddLabel("AirPlay 接收引擎：UxPlay  ·  Windows x64", 285, 530, 440, 24, 9, Color.Gray);
        foreach(Control control in Controls) if(control.Top >= 226) control.Top += 100;
        AddLabel("画质模式", 36, 223, 100, 26, 10, Color.DimGray);
        quality.SetBounds(142, 220, 578, 30); quality.DropDownStyle = ComboBoxStyle.DropDownList;
        quality.Items.AddRange(new object[] { "兼容 · 1080p / 30 帧", "流畅 · 1080p / 60 帧", "文字优先 · 2K / 30 帧", "高效编码 · 2K / 30 帧", "平板高清 · 2732×2048 / 30 帧", "超清 · 4K / 30 帧（实验）" });
        qualityHint.SetBounds(36, 266, 684, 46); qualityHint.Font = new Font(Font.FontFamily, 9); qualityHint.ForeColor = Color.DimGray;
        quality.SelectedIndexChanged += delegate {
            qualityHint.Text = quality.SelectedIndex >= 3 ? "HEVC 需设备支持；黑屏请切回 H.264。4:3 适合相同比例的 Apple设备。\n分辨率与编码由 Apple设备 协商，码率由发送端决定；不保证提升。" : "文字模式降低帧率请求，优先尝试改善静态细节；游戏可选流畅模式。\n停止接收后更换模式，再从 Apple设备 重新连接。";
        };
        Controls.Add(quality); quality.SelectedIndex = 2;
        qualityHint.Height = 38;
        stable.Text = "稳定优先"; stable.Checked = true;
        stable.SetBounds(36, 302, 430, 24); Controls.Add(stable);
        restart.Text = "重连接收"; restart.SetBounds(158, 623, 110, 30); restart.Enabled = false;
        restart.Click += delegate {
            if(restarting) return;
            if(!StopReceiver()) return;
            restarting = true; start.Enabled = stop.Enabled = restart.Enabled = false;
            name.Enabled = quality.Enabled = stable.Enabled = false;
            status.Text = "● 正在释放旧连接，请稍候…"; restartTimer.Start();
        }; Controls.Add(restart);
        restartTimer.Interval = 1500; restartTimer.Tick += delegate {
            restartTimer.Stop(); restarting = false; StartReceiver();
        };
        foreach(Control control in Controls) {
            if(control.Top >= 560) control.Top -= 250;
            else if(control.Top >= 137) control.Top -= 50;
        }
        foreach(Control control in Controls) if(control.Top >= 310) control.Top += 180;
        AddLabel("操作说明", 36, 330, 680, 30, 15, Color.FromArgb(26, 42, 69));
        AddLabel("01   电脑与 Apple设备连接同一个局域网 / Wi-Fi", 36, 374, 680, 28, 11, Color.FromArgb(60, 70, 90));
        AddLabel("02   点击“开始接收”，在 Apple设备打开控制中心 → 屏幕镜像", 36, 412, 680, 28, 11, Color.FromArgb(60, 70, 90));
        AddLabel("03   选择上方设备名称，画面将在独立窗口显示", 36, 450, 680, 28, 11, Color.FromArgb(60, 70, 90));
        quality.Width = 490;
        stable.SetBounds(142, 216, 160, 26);
        var details = new Button { Text = "说明", Left = 646, Top = 169, Width = 74, Height = 30, FlatStyle = FlatStyle.Flat };
        details.FlatAppearance.BorderColor = Color.FromArgb(210, 220, 235);
        details.Click += delegate {
            string codec = quality.SelectedIndex >= 3 ? "HEVC" : "H.264";
            MessageBox.Show("当前画质：" + quality.SelectedItem + "\n视频编码：" + codec + "\n\n" + qualityHint.Text + "\n\n稳定优先\n尝试减少初始同步等待，可能影响音画同步。观看视频时如声音与画面不同步，可停止接收后关闭此选项。\n\n修改画质后需重新连接屏幕镜像。", "画质与稳定设置", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }; Controls.Add(details);
        status.SetBounds(36, 262, 684, 48);
        status.BackColor = Color.FromArgb(233, 240, 250);
        status.Padding = new Padding(12, 7, 8, 0);
        foreach(Button button in Controls.OfType<Button>()) {
            bool primary = button == start;
            button.FlatStyle = FlatStyle.Flat;
            button.UseVisualStyleBackColor = false;
            button.Height = 38;
            button.FlatAppearance.BorderSize = 1;
            button.BackColor = primary ? Color.FromArgb(44, 100, 230) : BackColor;
            button.ForeColor = primary ? Color.White : Color.FromArgb(43, 91, 177);
            button.FlatAppearance.BorderColor = primary ? Color.FromArgb(44, 100, 230) : Color.FromArgb(210, 220, 235);
            button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(36, 86, 210) : Color.FromArgb(231, 239, 251);
            button.FlatAppearance.MouseDownBackColor = primary ? Color.FromArgb(29, 71, 181) : Color.FromArgb(216, 229, 247);
        }
        timer.Interval = 1000; timer.Tick += delegate {
            if (receiver != null && receiver.HasExited) { var code = receiver.ExitCode; Cleanup(); status.Text = "● 接收组件已退出（代码 " + code + "），可重新启动或查看连接排查。"; }
            if(receiver != null) WindowFit.Apply(receiver);

        }; timer.Start();
        if(remember) {
            var saved = Preferences.Load(preferencesPath);
            name.Text = saved.Name; quality.SelectedIndex = saved.Quality; stable.Checked = saved.Stable;
            quality.SelectedIndexChanged += delegate { SavePreferences(); };
            stable.CheckedChanged += delegate { SavePreferences(); };
            name.TextChanged += delegate { SavePreferences(); };
        }
        FormClosing += delegate(object sender, FormClosingEventArgs e) { restartTimer.Stop(); if(!StopReceiver()) e.Cancel = true; if(remember) SavePreferences(); };
    }
    void SavePreferences() {
        if(!Preferences.ValidName(name.Text) || quality.SelectedIndex < 0) return;
        try { new Preferences { Name = name.Text, Quality = quality.SelectedIndex, Stable = stable.Checked }.Save(preferencesPath); }
        catch(Exception ex) {
            if(!(ex is IOException) && !(ex is UnauthorizedAccessException)) throw;
            if(!preferenceWarning) { preferenceWarning = true; MessageBox.Show("本次设置未能保存，下次启动可能恢复旧设置。\n" + ex.Message, "设置保存失败"); }
        }
    }
    void AddLabel(string text, int x, int y, int w, int h, float size, Color color) {
        Controls.Add(new Label { Text = text, Left = x, Top = y, Width = w, Height = h, Font = new Font("Microsoft YaHei UI", size), ForeColor = color });
    }
    void RefreshNetwork() {
        try { network.Text = "本机 IPv4：" + string.Join("   ", NetworkInterface.GetAllNetworkInterfaces().Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback).SelectMany(n => n.GetIPProperties().UnicastAddresses).Where(a => a.Address.AddressFamily == AddressFamily.InterNetwork).Select(a => a.Address.ToString())); }
        catch { network.Text = "无法读取网络地址，请检查网络连接。"; }
    }
    void StartReceiver() {
        try {
            if(receiver != null) return;
            if (!Regex.IsMatch(name.Text, @"^[\p{L}\p{N}_.][\p{L}\p{N}_.-]{0,47}$")) throw new Exception("名称请使用 1–48 个汉字、字母、数字、点、下划线或短横线，不含空格且不以短横线开头。");
            var exe = Path.Combine(root, "runtime", "uxplay-windows.exe");
            if (!File.Exists(exe)) throw new Exception("缺少 runtime 接收组件，请保持软件目录完整。");
            var bundledCompiler = Path.Combine(root, "runtime", "D3Dcompiler_47.dll");
            if(File.Exists(bundledCompiler)) throw new Exception("检测到旧版附带的 D3Dcompiler_47.dll。请将修复版解压到新文件夹，不要覆盖旧版目录。");
            if (Process.GetProcessesByName("uxplay-windows").Length != 0) throw new Exception("已有 UxPlay 接收组件运行，请从它的托盘菜单退出后重试。");
            var machine = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "uxplay-windows", "arguments.txt");
            if (File.Exists(machine)) throw new Exception("检测到管理员统一配置：" + machine + "。请先移除或调整该配置，以免设备名称被覆盖。");
            config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "leapbtw", "uxplay-windows", "arguments.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(config));
            previous = File.Exists(config) ? File.ReadAllBytes(config) : null;
            File.WriteAllText(config, "-n " + name.Text.Trim() + " -nh" + QualityArgs[quality.SelectedIndex] + StabilityArgs(stable.Checked), new UTF8Encoding(false)); configOwned = true;
            var info = new ProcessStartInfo(exe) { WorkingDirectory = Path.GetDirectoryName(exe), UseShellExecute = false, CreateNoWindow = true };
            var logs = Path.Combine(root, "logs"); Directory.CreateDirectory(logs);
            // Rotate only our own old diagnostic files; no video or audio is recorded.
            foreach(var old in new DirectoryInfo(logs).GetFiles("receiver-*.log").OrderByDescending(f => f.LastWriteTimeUtc).Skip(4)) { try { old.Delete(); } catch(IOException) {} }
            info.EnvironmentVariables["GST_DEBUG"] = "2";
            info.EnvironmentVariables["GST_DEBUG_NO_COLOR"] = "1";
            info.EnvironmentVariables["GST_DEBUG_FILE"] = Path.Combine(logs, "receiver-" + DateTime.Now.ToString("yyyyMMdd-HHmmss-fff") + ".log");
            receiver = Process.Start(info);
            start.Enabled = name.Enabled = quality.Enabled = stable.Enabled = false; stop.Enabled = restart.Enabled = true;
            status.Text = "● 接收组件已启动，请在 Apple设备 选择「" + name.Text.Trim() + "」\n首次启动请完成系统提示；连接状态请以 Apple设备 和画面窗口为准。";
        } catch (Exception ex) { Cleanup(); MessageBox.Show(ex.Message, "无法启动接收", MessageBoxButtons.OK, MessageBoxIcon.Information); }
    }
    static string StabilityArgs(bool enabled) { return enabled ? " -vsync no -reset 10 -nofreeze" : " -reset 10 -nofreeze"; }
    bool StopReceiver() {
        try { if(receiver != null && !receiver.HasExited) {
            using(var kill = Process.Start(new ProcessStartInfo("taskkill.exe", "/PID " + receiver.Id + " /T /F") { UseShellExecute = false, CreateNoWindow = true })) { kill.WaitForExit(5000); }
            if(!receiver.WaitForExit(3000)) throw new Exception("接收进程仍在运行。");
        } }
        catch(Exception ex) { MessageBox.Show("接收组件未能停止：" + ex.Message); return false; }
        Cleanup(); status.Text = "● 已停止接收";
        return true;
    }
    void Cleanup() {
        WindowFit.Reset();

        if (receiver != null) { receiver.Dispose(); receiver = null; }
        if (configOwned) {
            try { if(previous == null) File.Delete(config); else File.WriteAllBytes(config, previous); }
            catch(Exception ex) { MessageBox.Show("配置恢复失败：" + ex.Message); }
            configOwned = false;
        }
        start.Enabled = name.Enabled = quality.Enabled = stable.Enabled = true; stop.Enabled = restart.Enabled = false;
    }
    [STAThread] static int Main(string[] args) {
        Application.EnableVisualStyles(); Application.SetCompatibleTextRenderingDefault(false);
        using(var app = new AirLink(!args.Contains("--smoke-test"))) {
            if(args.Contains("--smoke-test")) {
                if(!WindowFit.SmokeTest()) return 5;
                if(app.quality.Items.Count != 6 || app.quality.SelectedIndex != 2 || QualityArgs[app.quality.SelectedIndex] != " -s 2560x1440@30 -fps 30") return 2;
                if(!app.stable.Checked || !StabilityArgs(true).Contains("-vsync no") || StabilityArgs(false).Contains("-vsync no") || app.restart.Enabled) return 3;
                app.Show(); Application.DoEvents(); using(var bitmap = new Bitmap(app.Width, app.Height)) { app.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size)); bitmap.Save(Path.Combine(app.root, "ui-preview.png")); } app.Hide();
                return 0;
            }
            if(args.Contains("--start")) app.Shown += delegate { app.StartReceiver(); };
            Application.Run(app);
        } return 0;
    }
}


