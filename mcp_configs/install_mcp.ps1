# install_mcp.ps1 — Install and configure MCP bridges for Nyghtshade Hollow
# Run as Administrator from PowerShell on NYGHTSHADE machine
# Usage: cd C:\Users\leegi\injectioncgpt55\mcp_configs && .\install_mcp.ps1

$ErrorActionPreference = "Stop"

Write-Host "=== Nyghtshade Hollow MCP Setup ===" -ForegroundColor Cyan

# ── 1. Install Node.js MCP servers ───────────────────────────────────────────
Write-Host "`n[1/5] Installing MCP npm packages..." -ForegroundColor Yellow
npm install -g @modelcontextprotocol/server-filesystem
npm install -g @modelcontextprotocol/server-git
Write-Host "  MCP npm packages installed." -ForegroundColor Green

# ── 2. Install Python MCP server dependencies ────────────────────────────────
Write-Host "`n[2/5] Installing Python MCP dependencies..." -ForegroundColor Yellow
pip install httpx mcp
Write-Host "  Python deps installed." -ForegroundColor Green

# ── 3. Copy unreal_mcp_server.py to Scripts folder ───────────────────────────
Write-Host "`n[3/5] Installing Unreal MCP server..." -ForegroundColor Yellow
$ScriptDest = "$env:LOCALAPPDATA\Programs\Python\Python312\Scripts\unreal_mcp_server.py"
Copy-Item -Path ".\unreal_mcp_server.py" -Destination $ScriptDest -Force
Write-Host "  Copied to: $ScriptDest" -ForegroundColor Green

# ── 4. Install OpenClaw config ────────────────────────────────────────────────
Write-Host "`n[4/5] Installing OpenClaw config..." -ForegroundColor Yellow
$OpenClawDir = "$env:USERPROFILE\.openclaw-autoclaw"
if (-not (Test-Path $OpenClawDir)) { New-Item -ItemType Directory -Path $OpenClawDir }
Copy-Item -Path ".\openclaw_config.json" -Destination "$OpenClawDir\openclaw.json" -Force
Write-Host "  OpenClaw config installed at: $OpenClawDir\openclaw.json" -ForegroundColor Green

# ── 5. Install Hermes config ──────────────────────────────────────────────────
Write-Host "`n[5/5] Installing Hermes config..." -ForegroundColor Yellow
$HermesDir = "$env:LOCALAPPDATA\hermes"
if (-not (Test-Path $HermesDir)) { New-Item -ItemType Directory -Path $HermesDir }
Copy-Item -Path ".\hermes_config.yaml" -Destination "$HermesDir\config.yaml" -Force
Write-Host "  Hermes config installed at: $HermesDir\config.yaml" -ForegroundColor Green

# ── Fix Hermes-3 Ollama model ─────────────────────────────────────────────────
Write-Host "`nChecking Hermes-3 Ollama model..." -ForegroundColor Yellow
$models = ollama list 2>$null
if ($models -match "hermes3:8b-llama3.1") {
    Write-Host "  hermes3:8b-llama3.1 already installed." -ForegroundColor Green
} else {
    Write-Host "  Pulling hermes3:8b-llama3.1..." -ForegroundColor Yellow
    ollama pull hermes3:8b-llama3.1
    if ($LASTEXITCODE -ne 0) {
        Write-Host "  Fallback: pulling nous-hermes2..." -ForegroundColor Yellow
        ollama pull nous-hermes2
    }
}

# ── Enable UE Remote Control ──────────────────────────────────────────────────
Write-Host "`n[Manual step required] Enable Remote Control in UE Editor:" -ForegroundColor Cyan
Write-Host "  1. Open NyghtshadeHollow_UE58 in UE 5.8"
Write-Host "  2. Edit > Plugins > search 'Remote Control API' > Enable > Restart"
Write-Host "  3. Project Settings > Plugins > Remote Control API > HTTP Server Port: 30010"
Write-Host "  4. The MCP bridge will then be able to control the editor remotely."

Write-Host "`n=== MCP Setup Complete ===" -ForegroundColor Green
Write-Host "Restart OpenClaw and Hermes to load the new configs." -ForegroundColor Yellow
