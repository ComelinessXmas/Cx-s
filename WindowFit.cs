using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

// Restores the previously selected screenshot-based window fit without settings UI.
static class WindowFit {
    [StructLayout(LayoutKind.Sequential)] struct Rect { public int Left, Top, Right, Bottom; }
    delegate bool EnumProc(IntPtr window, IntPtr data);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc callback, IntPtr data);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window, out uint pid);
    [DllImport("user32.dll", CharSet=CharSet.Unicode)] static extern int GetWindowText(IntPtr window, StringBuilder text, int count);
    [DllImport("user32.dll")] static extern bool IsWindowVisible(IntPtr window);
    [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window, int command);
    [DllImport("user32.dll")] static extern int GetWindowLong(IntPtr window, int index);
    [DllImport("user32.dll")] static extern bool AdjustWindowRectEx(ref Rect rect, uint style, bool menu, uint exStyle);
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int w, int h, uint flags);
    [DllImport("user32.dll")] static extern bool GetClientRect(IntPtr window, out Rect rect);
    static IntPtr fitted;
    public static void Reset() { fitted=IntPtr.Zero; }
    public static bool Apply(Process receiver) {
        if(receiver==null || receiver.HasExited) return false;
        IntPtr target=IntPtr.Zero;
        EnumWindows(delegate(IntPtr window, IntPtr data) {
            uint pid; GetWindowThreadProcessId(window,out pid);
            if(pid!=(uint)receiver.Id || !IsWindowVisible(window)) return true;
            var title=new StringBuilder(256); GetWindowText(window,title,title.Capacity);
            string t=title.ToString();
            if(t.Contains("AirPlay Video Stream") || (t.Contains("Direct") && t.Contains("enderer"))) { target=window; return false; }
            return true;
        },IntPtr.Zero);
        if(target==IntPtr.Zero) { Reset(); return false; }
        if(target==fitted) return true;
        ShowWindow(target,9);
        var area=Screen.FromHandle(target).WorkingArea;
        var border=new Rect();
        if(!AdjustWindowRectEx(ref border,(uint)GetWindowLong(target,-16),false,(uint)GetWindowLong(target,-20))) return false;
        int bw=border.Right-border.Left,bh=border.Bottom-border.Top;
        double ratio=1620.0/1020.0;
        int width=Math.Min(area.Width-40-bw,(int)Math.Round((area.Height-40-bh)*ratio));
        int height=(int)Math.Round(width/ratio);
        if(width<=0 || height<=0) return false;
        bool ok=SetWindowPos(target,IntPtr.Zero,area.Left+(area.Width-width-bw)/2,area.Top+(area.Height-height-bh)/2,width+bw,height+bh,0x14);
        if(ok) fitted=target;
        return ok;
    }
    public static bool SmokeTest() {
        using(var mock=new Form {Text="AirPlay Video Stream",WindowState=FormWindowState.Maximized}) {
            mock.Show(); Application.DoEvents();
            using(var process=Process.GetCurrentProcess()) {
                if(!Apply(process)) return false;
                Rect rect; if(!GetClientRect(mock.Handle,out rect)) return false;
                bool valid=Math.Abs((double)(rect.Right-rect.Left)/(rect.Bottom-rect.Top)-1620.0/1020)<0.004;
                Reset(); return valid;
            }
        }
    }
}
