# setup-hermes-local.ps1
# One shot: restart Ollama at 65536 ctx, point Hermes at local qwen2.5-coder:7b, launch.
# Run from any PowerShell window (no admin needed):
#   Set-ExecutionPolicy -Scope CurrentUser RemoteSigned -Force
#   .\setup-hermes-local.ps1

param([string]$Model = "qwen2.5-coder:7b")

$ErrorActionPreference = "Stop"

function Say($msg, $color = "Cyan") { Write-Host $msg -ForegroundColor $color }

# ── 1. Persist 65536 context length ───────────────────────────────────────────
Say "Setting OLLAMA_CONTEXT_LENGTH=65536 (user env)..."
[System.Environment]::SetEnvironmentVariable("OLLAMA_CONTEXT_LENGTH", "65536", "User")
$env:OLLAMA_CONTEXT_LENGTH = "65536"

# ── 2. Restart Ollama so the new context length takes effect ──────────────────
Say "Stopping existing Ollama processes..."
Get-Process -Name "ollama*" -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
Start-Sleep 3

$candidates = @(
    "$env:LOCALAPPDATA\Programs\Ollama\ollama app.exe",
    "$env:ProgramFiles\Ollama\ollama app.exe",
    "$env:LOCALAPPDATA\Programs\Ollama\ollama.exe"
)
$ollamaExe = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $ollamaExe) {
    $fromPath = (Get-Command ollama -ErrorAction SilentlyContinue)?.Source
    if ($fromPath) { $ollamaExe = $fromPath } else {
        Write-Error "Ollama executable not found. Is Ollama installed? (winget install Ollama.Ollama)"
    }
}

Say "Starting Ollama: $ollamaExe"
Start-Process $ollamaExe
Say "Waiting for Ollama API on :11434..."
$up = $false
for ($i = 0; $i -lt 20; $i++) {
    Start-Sleep 2
    try { $null = Invoke-RestMethod "http://127.0.0.1:11434/api/tags" -TimeoutSec 3; $up = $true; break } catch {}
    Write-Host "  ...($($i*2+2)s)" -NoNewline
}
if (-not $up) { Write-Error "Ollama didn't respond in 40s — check the system tray icon." }
Say "`nOllama is up." "Green"

# ── 3. Make sure the model exists locally ─────────────────────────────────────
$tags   = Invoke-RestMethod "http://127.0.0.1:11434/api/tags"
$names  = @($tags.models | Select-Object -ExpandProperty name)
$target = $names | Where-Object { $_ -like "$Model*" } | Select-Object -First 1
if (-not $target) {
    Say "Model '$Model' not on disk — pulling now (~5 GB, grab a coffee)..." "Yellow"
    & ollama pull $Model
    if ($LASTEXITCODE -ne 0) { Write-Error "ollama pull failed." }
    $target = $Model
}
Say "Model confirmed: $target" "Green"

# ── 4. Patch Hermes config.yaml ───────────────────────────────────────────────
$cfgPath = "$env:LOCALAPPDATA\hermes\config.yaml"
if (-not (Test-Path $cfgPath)) {
    Write-Error "Hermes config not found at $cfgPath`nRun 'hermes setup' first, then re-run this script."
}

Say "Patching $cfgPath..."
$lines   = Get-Content $cfgPath
$changed = $false

# Walk line by line; replace provider/model/base_url values at the top level.
# Preserves indentation and comments; only changes value portion after ": ".
$out = for ($i = 0; $i -lt $lines.Count; $i++) {
    $line = $lines[$i]
    if ($line -match '^(\s*provider\s*:\s*)(.+)$') {
        $new = $Matches[1] + "ollama"
        if ($line -ne $new) { Say "  provider: $($Matches[2].Trim()) → ollama"; $changed = $true }
        $new
    } elseif ($line -match '^(\s*model\s*:\s*)(.+)$') {
        $new = $Matches[1] + $target
        if ($line -ne $new) { Say "  model: $($Matches[2].Trim()) → $target"; $changed = $true }
        $new
    } elseif ($line -match '^(\s*(?:base_url|api_base)\s*:\s*)(.+)$') {
        $new = $Matches[1] + "http://127.0.0.1:11434"
        if ($line -ne $new) { Say "  base_url: $($Matches[2].Trim()) → http://127.0.0.1:11434"; $changed = $true }
        $new
    } else {
        $line
    }
}

if ($changed) {
    Copy-Item $cfgPath "$cfgPath.bak" -Force
    Say "  (backup saved to $cfgPath.bak)"
    $out | Set-Content $cfgPath -Encoding UTF8
    Say "Config updated." "Green"
} else {
    Say "No matching keys found in config — trying hermes setup model instead..." "Yellow"
    # Fallback: run hermes setup model and let user interact manually
    Say "MANUAL STEP NEEDED:" "Yellow"
    Say "  Run: hermes setup model" "Yellow"
    Say "  Provider: 38 (ollama 127.0.0.1:11434)" "Yellow"
    Say "  Model: pick $target from the list" "Yellow"
    Say "  Then re-run this script or just run: hermes" "Yellow"
    exit 1
}

# ── 5. Verify ─────────────────────────────────────────────────────────────────
Say "`nRunning hermes doctor..."
hermes doctor

# ── 6. Launch ─────────────────────────────────────────────────────────────────
Say "`nLaunching Hermes. Type 'hey' at the ❯ prompt to test." "Green"
Say "(First response takes ~30s while the model loads into RAM. That's normal.)" "DarkGray"
hermes
