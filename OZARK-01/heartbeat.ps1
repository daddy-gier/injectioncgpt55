# CLAWED Heartbeat — queue-driven Claude Code worker
# Checks every 5 minutes for tasks in queue.txt and runs one per tick.
# Place this script at Z:\Gierl\Projects\CLAWED\.heartbeat\heartbeat.ps1
#
# Setup (run once):
#   New-Item -ItemType Directory "Z:\Gierl\Projects\CLAWED\.heartbeat\reports" -Force
#   Register-ScheduledTask -TaskName "CLAWED-Heartbeat" `
#     -Action (New-ScheduledTaskAction -Execute "powershell.exe" `
#       -Argument "-NoProfile -ExecutionPolicy Bypass -File `"Z:\Gierl\Projects\CLAWED\.heartbeat\heartbeat.ps1`"") `
#     -Trigger (New-ScheduledTaskTrigger -Once -At (Get-Date).AddMinutes(2) `
#       -RepetitionInterval (New-TimeSpan -Minutes 5)) `
#     -Settings (New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Minutes 20) -MultipleInstances IgnoreNew) `
#     -Description "Queue-driven Claude Code worker for CLAWED"
#
# Kill switch:  New-Item "Z:\Gierl\Projects\CLAWED\.heartbeat\STOP" -ItemType File
# Resume:       Remove-Item "Z:\Gierl\Projects\CLAWED\.heartbeat\STOP"
# Unregister:   Unregister-ScheduledTask -TaskName "CLAWED-Heartbeat" -Confirm:$false
# Watch log:    Get-Content "Z:\Gierl\Projects\CLAWED\.heartbeat\heartbeat.log" -Wait

$ErrorActionPreference = 'Stop'

# ── Config ────────────────────────────────────────────────────────────────────
$projectRoot = 'Z:\Gierl\Projects\CLAWED\CLAWED'   # folder containing CLAWED.uproject
$hb          = 'Z:\Gierl\Projects\CLAWED\.heartbeat'
$queue       = "$hb\queue.txt"
$done        = "$hb\done.txt"
$log         = "$hb\heartbeat.log"
$lock        = "$hb\.lock"
$stop        = "$hb\STOP"

# ── Helpers ───────────────────────────────────────────────────────────────────
function Write-Log($msg) {
    "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $msg" | Out-File $log -Append -Encoding utf8
}

# Kill switch — drop a file named STOP into .heartbeat to halt without unregistering
if (Test-Path $stop) { Write-Log 'STOP present — skipping tick.'; exit 0 }

# Overlap guard — skip if another instance finished less than 20 min ago
if (Test-Path $lock) {
    $age = (Get-Date) - (Get-Item $lock).LastWriteTime
    if ($age.TotalMinutes -lt 20) { Write-Log "Lock $([int]$age.TotalMinutes)m old — skipping."; exit 0 }
    Write-Log 'Stale lock cleared.'
}
New-Item -ItemType File -Path $lock -Force | Out-Null

try {
    # Ensure directories exist
    @("$hb\reports") | ForEach-Object { if (-not (Test-Path $_)) { New-Item -ItemType Directory $_ -Force | Out-Null } }
    if (-not (Test-Path $queue)) { Set-Content $queue "# One task per line. Oldest runs first.`n# Lines starting with # are ignored.`n" -Encoding utf8 }
    if (-not (Test-Path $done))  { New-Item -ItemType File $done -Force | Out-Null }

    # Pull first non-comment, non-blank task
    $lines = @(Get-Content $queue -ErrorAction SilentlyContinue)
    $task  = $lines | Where-Object { $_.Trim() -and -not $_.TrimStart().StartsWith('#') } | Select-Object -First 1

    if (-not $task) { Write-Log 'Queue empty — nothing to do.'; exit 0 }

    Write-Log "TASK: $task"
    $stamp      = Get-Date -Format 'yyyy-MM-dd_HHmm'
    $reportFile = "$hb\reports\$stamp.md"

    Push-Location $projectRoot
    $output = & claude -p $task `
        --output-format text `
        --allowedTools 'Read,Glob,Grep,Bash(git status),Bash(git log *),Bash(git diff *),Bash(git show *)' `
        --max-turns 15 2>&1
    $exitCode = $LASTEXITCODE
    Pop-Location

    # Write report
    @(
        "# Heartbeat report — $stamp",
        "",
        "**Task:** $task",
        "**Exit code:** $exitCode",
        "",
        "---",
        "",
        ($output -join "`n")
    ) | Out-File $reportFile -Encoding utf8

    Write-Log "EXIT $exitCode  →  $reportFile"

    if ($exitCode -eq 0) {
        # Remove completed task from queue
        $remaining = $lines | Where-Object { $_ -ne $task }
        $remaining | Out-File $queue -Encoding utf8
        "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $task" | Out-File $done -Append -Encoding utf8
        Write-Log 'DONE.'
    } else {
        Write-Log 'FAILED — task left in queue. Review report before retrying.'
    }
}
finally {
    Remove-Item $lock -ErrorAction SilentlyContinue
}
