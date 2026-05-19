# OpenMythos setup + smoke test launcher
# Run from PowerShell as a normal user (no admin required for Python install via winget)
#
# Usage:
#   .\run_mythos.ps1                  # runs both gqa and mla variants
#   .\run_mythos.ps1 -AttnType gqa    # runs only gqa

param(
    [string]$AttnType = "",
    [string]$OpenMythosPath = "$env:USERPROFILE\Downloads\OpenMythos-main\OpenMythos-main"
)

$ErrorActionPreference = 'Stop'

# ── 1. Ensure Python is available ─────────────────────────────────────────────
$py = $null
foreach ($cmd in @('python', 'python3', 'py')) {
    try {
        $ver = & $cmd --version 2>&1
        if ($ver -match 'Python 3') { $py = $cmd; break }
    } catch { }
}

if (-not $py) {
    Write-Host "Python not found — installing via winget..." -ForegroundColor Yellow
    winget install Python.Python.3.12 --silent --accept-package-agreements --accept-source-agreements
    $env:PATH = [System.Environment]::GetEnvironmentVariable('PATH', 'Machine') + ';' +
                [System.Environment]::GetEnvironmentVariable('PATH', 'User')
    $py = 'python'
}

Write-Host "Using: $(& $py --version)" -ForegroundColor Cyan

# ── 2. Install / upgrade pip ───────────────────────────────────────────────────
& $py -m pip install --upgrade pip --quiet

# ── 3. Install OpenMythos from local source ───────────────────────────────────
if (-not (Test-Path $OpenMythosPath)) {
    Write-Error "OpenMythos not found at: $OpenMythosPath`nPass -OpenMythosPath <path> or download from GitHub."
}

Write-Host "Installing OpenMythos from $OpenMythosPath ..." -ForegroundColor Cyan
Push-Location $OpenMythosPath
try {
    # Try the [flash] extra first (includes FlashAttention); fall back to base install
    try {
        & $py -m pip install -e ".[flash]" --quiet
    } catch {
        Write-Host "  flash extra failed — installing base package instead." -ForegroundColor Yellow
        & $py -m pip install -e "." --quiet
    }
} finally {
    Pop-Location
}

# ── 4. Install torch if missing ───────────────────────────────────────────────
$torchCheck = & $py -c "import torch; print(torch.__version__)" 2>&1
if ($LASTEXITCODE -ne 0) {
    Write-Host "Installing PyTorch (CPU)..." -ForegroundColor Cyan
    & $py -m pip install torch --index-url https://download.pytorch.org/whl/cpu --quiet
}

# ── 5. Run the smoke test ─────────────────────────────────────────────────────
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$testScript = Join-Path $scriptDir "test_mythos.py"

Write-Host "`nRunning OpenMythos smoke test...`n" -ForegroundColor Green
if ($AttnType) {
    & $py $testScript $AttnType
} else {
    & $py $testScript
}

if ($LASTEXITCODE -eq 0) {
    Write-Host "`nSmoke test PASSED." -ForegroundColor Green
} else {
    Write-Error "Smoke test FAILED (exit $LASTEXITCODE)."
}
