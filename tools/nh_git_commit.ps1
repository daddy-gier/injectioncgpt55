# NyghtshadeHollow — commit all untracked Phase 18-20 + 7/16 expansion files
# Run from: F:\Unreal\NyghtshadeHollow_UE58\
# Usage: cd F:\Unreal\NyghtshadeHollow_UE58 && .\nh_git_commit.ps1

$ErrorActionPreference = "Stop"
$ProjectRoot = "F:\Unreal\NyghtshadeHollow_UE58"

if (-not (Test-Path "$ProjectRoot\.git")) {
    Write-Error "Not a git repo. Run from F:\Unreal\NyghtshadeHollow_UE58"
    exit 1
}

Set-Location $ProjectRoot

Write-Host "=== NyghtshadeHollow Git Commit ===" -ForegroundColor Cyan
Write-Host ""

# Show current status
git status --short

Write-Host ""
Write-Host "Staging all changes..." -ForegroundColor Yellow

# Stage everything
git add Source/NyghtshadeHollow/PrisonDisciplineSubsystem.h
git add Source/NyghtshadeHollow/PrisonDisciplineSubsystem.cpp
git add Source/NyghtshadeHollow/PrisonInformationSubsystem.h
git add Source/NyghtshadeHollow/PrisonInformationSubsystem.cpp
git add Source/NyghtshadeHollow/PrisonJournalSubsystem.h
git add Source/NyghtshadeHollow/PrisonJournalSubsystem.cpp
git add Source/NyghtshadeHollow/nh_ghostcharacter.h
git add Source/NyghtshadeHollow/nh_ghostcharacter.cpp
git add Source/NyghtshadeHollow/nh_npccharacter.h
git add Source/NyghtshadeHollow/nh_npccharacter.cpp
git add Source/NyghtshadeHollow/nhcanonregistrysubsystem.h
git add Source/NyghtshadeHollow/nhcanonregistrysubsystem.cpp
git add Source/NyghtshadeHollow/nhgameplaytypes.h
git add Source/NyghtshadeHollow/nhinventorycomponent.h
git add Source/NyghtshadeHollow/nhinventorycomponent.cpp
git add Source/NyghtshadeHollow/nhquestcomponent.h
git add Source/NyghtshadeHollow/nhquestcomponent.cpp
git add Source/NyghtshadeHollow/nhrelationshipcomponent.h
git add Source/NyghtshadeHollow/nhrelationshipcomponent.cpp
git add Source/NyghtshadeHollow/nhreputationcomponent.h
git add Source/NyghtshadeHollow/nhreputationcomponent.cpp
git add Source/NyghtshadeHollow/nhsavebridgecomponent.h
git add Source/NyghtshadeHollow/nhsavebridgecomponent.cpp
git add NyghtshadeHollow.uproject
git add Source/NyghtshadeHollow/NyghtshadeHollow.Build.cs
git add Content/Dialogue/PRISON_DIALOGUE.json 2>$null
git add Docs/ 2>$null
git add .vsconfig 2>$null
git add AGENTS.md 2>$null
git add run_validate_assets.bat 2>$null

Write-Host ""
git status --short
Write-Host ""

$msg = @"
Phase 18-20 subsystems + 7/16 expansions (49 source files total)

Phase 18 - Prison Discipline (PrisonDisciplineSubsystem):
  20 UFUNCTIONs: contraband catalog, incident tracking, severity scoring,
  8 punishment types, 6 privilege flags, door gating, auto-escalation

Phase 19 - Information/Secrets (PrisonInformationSubsystem):
  19 UFUNCTIONs: rumor propagation, knowledge state, trade/coerce/expose,
  blackmail system, snitching with fear thresholds, eavesdropping

Phase 20 - Player Journal (PrisonJournalSubsystem):
  26 UFUNCTIONs: quest log, relationship log, faction standings,
  freeform entries, full-text search, unread tracking, UMG-ready API

7/16 expansions (9 files, never compiled):
  nh_ghostcharacter, nh_npccharacter, nhcanonregistrysubsystem,
  nhgameplaytypes, nhinventorycomponent, nhquestcomponent,
  nhrelationshipcomponent, nhreputationcomponent, nhsavebridgecomponent

Also: uproject + Build.cs dependency updates, PRISON_DIALOGUE.json
"@

git commit -m $msg

Write-Host ""
Write-Host "Done! Check 'git log --oneline -5' to verify." -ForegroundColor Green
Write-Host "Next: open project in VS/Rider and Build Solution (Win64 Development Editor)" -ForegroundColor Yellow
