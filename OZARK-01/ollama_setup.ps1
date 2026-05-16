# OZARK-01 FRANKENSTINE Node — Ollama Model Setup
# Run as Administrator in PowerShell on the Windows host
# Sets up qwen2.5-coder:14b (CHARLIE), qwen3:14b (SCOUT), mistral:7b (SPEEDY)

$ErrorActionPreference = 'Stop'
$models = @(
    @{ name = 'qwen2.5-coder:14b'; role = 'CHARLIE (UE5 C++ coder)' },
    @{ name = 'qwen3:14b';         role = 'SCOUT (NPC researcher)'  },
    @{ name = 'mistral:7b';        role = 'SPEEDY (fast tasks)'     }
)

function Write-Step($msg) { Write-Host "`n[OZARK-01] $msg" -ForegroundColor Cyan }
function Write-OK($msg)   { Write-Host "  OK  $msg" -ForegroundColor Green }
function Write-ERR($msg)  { Write-Host "  ERR $msg" -ForegroundColor Red }

# 1. Check / start Ollama
Write-Step "Checking Ollama..."
$ollamaRunning = $false
try {
    $r = Invoke-RestMethod -Uri 'http://localhost:11434/api/version' -TimeoutSec 3
    Write-OK "Ollama $($r.version) is running"
    $ollamaRunning = $true
} catch {
    Write-Host "  Ollama not responding — attempting to start..." -ForegroundColor Yellow
    Start-Process 'ollama' -ArgumentList 'serve' -WindowStyle Hidden
    Start-Sleep -Seconds 5
    try {
        $r = Invoke-RestMethod -Uri 'http://localhost:11434/api/version' -TimeoutSec 5
        Write-OK "Ollama started: $($r.version)"
        $ollamaRunning = $true
    } catch {
        Write-ERR "Could not start Ollama. Install from https://ollama.com and re-run."
        exit 1
    }
}

# 2. Pull models
Write-Step "Pulling models..."
foreach ($m in $models) {
    Write-Host "  Pulling $($m.name) [$($m.role)]..." -ForegroundColor DarkCyan
    $proc = Start-Process 'ollama' -ArgumentList "pull $($m.name)" -PassThru -Wait -NoNewWindow
    if ($proc.ExitCode -eq 0) {
        Write-OK "$($m.name) ready"
    } else {
        Write-ERR "Failed to pull $($m.name) — check disk space and retry"
    }
}

# 3. Quick smoke test
Write-Step "Running smoke tests..."
foreach ($m in $models) {
    try {
        $body = @{
            model    = $m.name
            messages = @(@{ role = 'user'; content = 'Reply with exactly: OK' })
        } | ConvertTo-Json -Depth 3
        $resp = Invoke-RestMethod -Uri 'http://localhost:11434/v1/chat/completions' `
            -Method POST -ContentType 'application/json' -Body $body -TimeoutSec 60
        $text = $resp.choices[0].message.content.Trim()
        Write-OK "$($m.name): `"$text`""
    } catch {
        Write-ERR "$($m.name): $($_.Exception.Message)"
    }
}

# 4. Write config file for the Express server
Write-Step "Writing OpenClaw config..."
$config = @{
    brain   = @{ provider = 'anthropic'; model = 'claude-sonnet-4-6'; role = 'orchestrator' }
    muscles = @{
        CHARLIE = @{ model = 'qwen2.5-coder:14b'; role = 'coder';      ollamaUrl = 'http://localhost:11434/v1' }
        SCOUT   = @{ model = 'qwen3:14b';          role = 'researcher'; ollamaUrl = 'http://localhost:11434/v1' }
        SPEEDY  = @{ model = 'mistral:7b';          role = 'fast';       ollamaUrl = 'http://localhost:11434/v1' }
    }
    node = @{ id = 'OZARK-01'; gpu = 'RTX 4070'; vram_gb = 12 }
} | ConvertTo-Json -Depth 5

$configPath = Join-Path $PSScriptRoot 'ollama_openclaw_config.json'
$config | Out-File -FilePath $configPath -Encoding utf8
Write-OK "Config written to $configPath"

Write-Step "Setup complete. Copy server/.env.example to server/.env and add your ANTHROPIC_API_KEY."
