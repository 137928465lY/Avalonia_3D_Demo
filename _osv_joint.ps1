<#
  OpenStepViewer 关节面板外部控制器（Win32 鼠标注入 + 像素校准）
  用法:
    powershell -File _osv_joint.ps1                    # 列出 J1..J5 当前角度
    powershell -File _osv_joint.ps1 -Set 2 -Angle -60   # 把 J2 拽到 -60°
    powershell -File _osv_joint.ps1 -Set 1 -Angle 30 -Shot
  原理: 定位右侧滑块把手(强调蓝圆点) → 在同一条扫描行上取轨道左右端点做量程校准
        (360°/轨道像素宽) → SetCursorPos 分步拖动 + mouse_event 按下/抬起
#>
param(
    [int]$Set = 0,
    [double]$Angle = 0,
    [switch]$Shot
)

Add-Type -AssemblyName System.Drawing
$refs = @([System.Drawing.Bitmap].Assembly.Location)

Add-Type -ReferencedAssemblies $refs -TypeDefinition @'
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Collections.Generic;

public class OSV
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, ref RECT r);
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);

    public class Thumb { public int Center; public int Y; public int H; }

    static byte[] Grab(Bitmap b, out int stride, out int w, out int h)
    {
        var rect = new Rectangle(0, 0, b.Width, b.Height);
        var d = b.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
        stride = d.Stride; w = b.Width; h = b.Height;
        byte[] buf = new byte[stride * h];
        Marshal.Copy(d.Scan0, buf, 0, buf.Length);
        b.UnlockBits(d);
        return buf;
    }

    static bool Accent(byte bb, byte gg, byte rr) { return bb > 150 && rr < 110 && gg > 70 && gg < 175; }

    // 右侧面板里高约 20px 的强调色圆形把手，按 y 自上而下排序
    public static Thumb[] Thumbs(Bitmap b)
    {
        int stride, w, h;
        byte[] buf = Grab(b, out stride, out w, out h);
        int[] maxx = new int[h], cnt = new int[h];
        for (int y = 0; y < h; y++)
        {
            maxx[y] = -1; int o = y * stride;
            for (int x = 0; x < w; x++)
                if (Accent(buf[o + x * 4], buf[o + x * 4 + 1], buf[o + x * 4 + 2])) { cnt[y]++; if (x > maxx[y]) maxx[y] = x; }
        }
        var list = new List<Thumb>();
        int st = -1;
        for (int y = 0; y <= h; y++)
        {
            bool on = y < h && cnt[y] > 2;
            if (on && st < 0) st = y;
            if (!on && st >= 0)
            {
                int e = y - 1, mx = -1;
                for (int k = st; k <= e; k++) if (maxx[k] > mx) mx = maxx[k];
                if (e - st + 1 >= 16 && e - st + 1 <= 26 && mx > 0.6 * w)
                    list.Add(new Thumb { Center = mx - (e - st) / 2, Y = (st + e) / 2, H = e - st + 1 });
                st = -1;
            }
        }
        return list.ToArray();
    }

    // 从把手中心向左右延伸，取滑块轨道的左右端点（遇到空白即停，容许 2px 间隙）
    public static int[] ExtentAround(Bitmap b, int y, int x0)
    {
        int stride, w, h;
        byte[] buf = Grab(b, out stride, out w, out h);
        int o = y * stride;
        int l = x0, r = x0, gap = 0;
        for (int x = x0; x >= 0; x--)
        {
            byte bb = buf[o + x * 4], gg = buf[o + x * 4 + 1], rr = buf[o + x * 4 + 2];
            if (rr < 235 || gg < 235 || bb < 235) { l = x; gap = 0; }
            else if (++gap > 2) break;
        }
        gap = 0;
        for (int x = x0; x < w; x++)
        {
            byte bb = buf[o + x * 4], gg = buf[o + x * 4 + 1], rr = buf[o + x * 4 + 2];
            if (rr < 235 || gg < 235 || bb < 235) { r = x; gap = 0; }
            else if (++gap > 2) break;
        }
        return new int[] { l, r };
    }
}
'@

[OSV]::SetProcessDPIAware() | Out-Null

$proc = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" |
        Where-Object { $_.CommandLine -like '*OpenStepViewer.dll*' } | Select-Object -First 1
if (-not $proc) { Write-Output "NO_PROCESS"; exit 1 }
$p = Get-Process -Id $proc.ProcessId
$p.Refresh()
$h = $p.MainWindowHandle
if ($h -eq [IntPtr]::Zero) { Write-Output "NO_HWND"; exit 1 }

[OSV]::ShowWindow($h, 9) | Out-Null
[OSV]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 400

$r = [OSV+RECT]::new()
[OSV]::GetWindowRect($h, [ref]$r) | Out-Null
if (($r.R - $r.L) -le 0) { Write-Output "BAD_RECT"; exit 1 }
$dir = Split-Path -Parent $MyInvocation.MyCommand.Path

function Shot([string]$name) {
    $bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
    $g.Dispose()
    if ($name) { $bmp.Save((Join-Path $dir $name), [System.Drawing.Imaging.ImageFormat]::Png) }
    return $bmp
}

# 读取 5 个关节的角度
function Read-Joints {
    $bmp = Shot $null
    $ts = [OSV]::Thumbs($bmp)
    $out = @()
    foreach ($t in $ts) {
        $ext = [OSV]::ExtentAround($bmp, $t.Y, $t.Center)
        # 轨道左右各被把手半径占住，把手中心实际行程 = 轨道宽 - 直径
        $span = ($ext[1] - $ext[0]) - ($t.H - 1)
        $degPerPx = 360.0 / $span
        $zero = ($ext[0] + $ext[1]) / 2.0
        $out += [pscustomobject]@{
            Thumb = $t
            TrackL = $ext[0]; TrackR = $ext[1]; Span = $span
            Angle = [math]::Round(($t.Center - $zero) * $degPerPx)
        }
    }
    $bmp.Dispose()
    return $out
}

function Drag-Joint($info, [double]$target) {
    $degPerPx = 360.0 / $info.Span
    $zero = ($info.TrackL + $info.TrackR) / 2.0
    $dx = [int][math]::Round(($target - $info.Angle) / $degPerPx)
    if ($dx -eq 0) { return 0 }
    $sx = $r.L + $info.Thumb.Center
    $sy = $r.T + $info.Thumb.Y
    [OSV]::SetCursorPos($sx, $sy) | Out-Null
    Start-Sleep -Milliseconds 120
    [OSV]::mouse_event(0x2, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 100
    $steps = [math]::Max(8, [math]::Min(24, [int]([math]::Abs($dx) / 6)))
    for ($s = 1; $s -le $steps; $s++) {
        [OSV]::SetCursorPos($sx + [int]($dx * $s / $steps), $sy) | Out-Null
        Start-Sleep -Milliseconds 30
    }
    Start-Sleep -Milliseconds 60
    [OSV]::mouse_event(0x4, 0, 0, 0, [IntPtr]::Zero)
    Start-Sleep -Milliseconds 200
    return $dx
}

$joints = Read-Joints
if ($joints.Count -eq 0) { Write-Output "NO_SLIDER_FOUND"; exit 2 }

if ($Set -le 0) {
    for ($i = 0; $i -lt $joints.Count; $i++) {
        Write-Output ("J{0} angle={1,5}deg  track=[{2}..{3}] ({4}px)  thumb=({5},{6})" -f `
            ($i + 1), $joints[$i].Angle, $joints[$i].TrackL, $joints[$i].TrackR, ($joints[$i].TrackR - $joints[$i].TrackL), $joints[$i].Thumb.Center, $joints[$i].Thumb.Y)
    }
    if ($Shot) { Shot "osv_joints_now.png" | ForEach-Object { $_.Dispose() } }
    exit 0
}

if ($Set -gt $joints.Count) { Write-Output "JOINT_OUT_OF_RANGE max=$($joints.Count)"; exit 3 }
$tgt = [math]::Max(-180, [math]::Min(180, $Angle))
$info = $joints[$Set - 1]
Write-Output ("J{0}: {1}° -> {2}°  (track {3}px, {4} px)" -f $Set, $info.Angle, $tgt, ($info.TrackR - $info.TrackL), $info.Thumb.Center)

for ($try = 1; $try -le 3; $try++) {
    $d = Drag-Joint $info $tgt
    $joints = Read-Joints
    $info = $joints[$Set - 1]
    Write-Output ("  尝试{0}: dx={1}px  实测={2}°" -f $try, $d, $info.Angle)
    if ([math]::Abs($info.Angle - $tgt) -le 2) { break }
}
Shot "osv_joints_now.png" | ForEach-Object { $_.Dispose() }
Write-Output "ok  J$Set = $($info.Angle)°"
