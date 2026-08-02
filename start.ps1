# Unreal Engine AI Expert — Windows launcher
# Run from the injectioncgpt55 folder:
#   .\start.ps1

$ErrorActionPreference = "Stop"

# Load .env if present
$envFile = Join-Path $PSScriptRoot ".env"
if (Test-Path $envFile) {
    Get-Content $envFile | ForEach-Object {
        $line = $_.Trim()
        if ($line -and -not $line.StartsWith("#") -and $line -match "=") {
            $parts = $line -split "=", 2
            $key = $parts[0].Trim()
            $val = $parts[1].Trim()
            [System.Environment]::SetEnvironmentVariable($key, $val, "Process")
            Write-Host "  Loaded: $key"
        }
    }
}

if (-not $env:ANTHROPIC_API_KEY) {
    Write-Error "ANTHROPIC_API_KEY is not set. Add it to .env or run: `$env:ANTHROPIC_API_KEY='sk-ant-...'"
    exit 1
}

Write-Host ""
Write-Host "Installing dependencies..."
pip install -r (Join-Path $PSScriptRoot "backend\requirements.txt") -q

Write-Host ""
Write-Host "Starting Unreal Engine AI Expert at http://localhost:8000"
Write-Host "Press Ctrl+C to stop."
Write-Host ""

$env:PYTHONPATH = $PSScriptRoot
uvicorn backend.main:app --host 0.0.0.0 --port 8000 --reload
