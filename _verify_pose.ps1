param(
    [string]$Pose = "0,0,0,0",
    [string]$Out = "_pose.png",
    [int]$WaitMs = 9000
)

Add-Type -AssemblyName System.Drawing
Add-Type -ReferencedAssemblies @([System.Drawing.Bitmap].Assembly.Location) -TypeDefinition @'
using System;
using System.Drawing;
using System.Runtime.InteropServices;
public class VP
{
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int L, T, R, B; }
    [DllImport("user32.dll")] public static extern bool SetProcessDPIAware();
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, ref RECT r);
    [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr h, int c);
}
'@
[VP]::SetProcessDPIAware() | Out-Null

$dir = Split-Path -Parent $MyInvocation.MyCommand.Path
$exe = Join-Path $dir "OpenStepViewer\bin\Debug\net8.0\OpenStepViewer.dll"

$env:OSV_POSE = $Pose
$p = Start-Process dotnet -ArgumentList "`"$exe`"", '"C:\osvbench\abb.step"' -PassThru
"pid=$($p.Id) pose=$Pose"

$h = [IntPtr]::Zero
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 250
    $p.Refresh()
    if ($p.HasExited) { "EXITED"; exit 1 }
    if ($p.MainWindowHandle -ne [IntPtr]::Zero) { $h = $p.MainWindowHandle; break }
}
if ($h -eq [IntPtr]::Zero) { "NO_HWND"; $p.Kill(); exit 1 }
Start-Sleep -Milliseconds $WaitMs

[VP]::ShowWindow($h, 9) | Out-Null
[VP]::SetForegroundWindow($h) | Out-Null
Start-Sleep -Milliseconds 500

$r = [VP+RECT]::new()
[VP]::GetWindowRect($h, [ref]$r) | Out-Null
$bmp = New-Object System.Drawing.Bitmap ($r.R - $r.L), ($r.B - $r.T)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.CopyFromScreen($r.L, $r.T, 0, 0, $bmp.Size)
$g.Dispose()
$bmp.Save((Join-Path $dir $Out), [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()
"saved $Out  rect=$($r.L),$($r.T) $($r.R - $r.L)x$($r.B - $r.T)"

Start-Sleep -Milliseconds 300
$p.Kill()
"closed"
