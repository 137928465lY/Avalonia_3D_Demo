# Guardian loop: run AnyCADShowcaseRunner with --resume.
# On native crash, add the crashed case to the skip list and restart.
$ErrorActionPreference = "Stop"
$appDir = "D:\OpenSource\anycad.rapid.net.sample示例\bin\Release\net8.0-windows"
$logPath = "D:\OpenSource\anycad.rapid.net.sample示例\showcase-capture.log"
$skipFile = "D:\OpenSource\anycad.rapid.net.sample示例\scripts\capture_skip.txt"

function Get-SkipList {
    if (Test-Path $skipFile) { @(Get-Content $skipFile | Where-Object { $_ -and -not $_.StartsWith("#") } | Sort-Object -Unique) } else { @() }
}

function Get-LastState {
    if (-not (Test-Path $logPath)) { return $null }
    $state = $null
    foreach ($line in Get-Content $logPath -Encoding UTF8) {
        if ($line -match "\[STATE (\d+)/(\d+)\] (\S+)/(\S+)") { $state = "$($Matches[3])/$($Matches[4])" }
    }
    return $state
}

function Get-LastDone {
    if (-not (Test-Path $logPath)) { return $false }
    # 任意位置出现 "DONE ok=" 即视为已有一轮完整跑完（resume 空跑也会写 DONE ok=0）
    return (Get-Content $logPath -Encoding UTF8 | Where-Object { $_ -match "DONE ok=" }).Count -gt 0
}

$maxRounds = 60
for ($round = 1; $round -le $maxRounds; $round++) {
    if (Get-LastDone) { Write-Host "All done (round ${round})."; break }
    $skipArgs = @("--resume")
    foreach ($s in Get-SkipList) { $skipArgs += @("--skip", $s) }

    Write-Host "=== round ${round}: $($skipArgs -join ' ') ==="
    $p = Start-Process dotnet -ArgumentList (@("AnyCADShowcaseRunner.dll") + $skipArgs) `
        -WorkingDirectory $appDir -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(600000)) {
        $stuck = Get-LastState
        Write-Host "TIMEOUT: stuck at $stuck, skipping."
        Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue
        if ($stuck) { Add-Content $skipFile $stuck }
        continue
    }
    Write-Host "runner exit code: $($p.ExitCode)"
    if (Get-LastDone) { Write-Host "All done (round ${round})."; break }
    if ($p.ExitCode -ne 0) {
        $crashed = Get-LastState
        Write-Host "CRASH at $crashed, adding to skip list."
        if ($crashed -and -not ((Get-SkipList) -contains $crashed)) { Add-Content $skipFile $crashed }
    }
}

Write-Host "=== final retry of skipped cases (--only) ==="
$stillSkipped = @()
foreach ($s in Get-SkipList) {
    Write-Host "retry $s ..."
    $p = Start-Process dotnet -ArgumentList @("AnyCADShowcaseRunner.dll", "--only", $s) `
        -WorkingDirectory $appDir -WindowStyle Hidden -PassThru
    if (-not $p.WaitForExit(600000)) { Stop-Process -Id $p.Id -Force -ErrorAction SilentlyContinue }
    $ok = $false
    $pat = "\] " + [regex]::Escape($s) + " -> "
    if (Test-Path $logPath) {
        foreach ($line in Get-Content $logPath -Encoding UTF8) {
            if ($line -match "\[OK \d+/\d+\]" -and $line -match $pat) { $ok = $true }
        }
    }
    if ($ok) { Write-Host "  $s OK now." } else { Write-Host "  $s still failing."; $stillSkipped += $s }
}
Write-Host "=== final skipped (failed): $($stillSkipped -join ', ') ==="
