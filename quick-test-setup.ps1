# Quick Test Setup for Network Updates
param(
    [string]$TestPath = "C:\temp\Contextualizer\Updates"
)

# Create test directory
New-Item -ItemType Directory -Path $TestPath -Force | Out-Null

# Copy built executable if exists
$builtExe = ".\publish\Contextualizer-Portable\Contextualizer.exe"
if (Test-Path $builtExe) {
    Copy-Item $builtExe "$TestPath\Contextualizer.exe" -Force
    Write-Host "Copied executable to test path" -ForegroundColor Green
} else {
    Write-Host "Build executable first with: .\build-release.ps1" -ForegroundColor Red
    exit 1
}

# Create version.json
$version = @{
    Version = "1.1.0"
    ExecutableFileName = "Contextualizer.exe"
    ReleaseDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    IsMandatory = $false
    MinimumRequiredVersion = "1.0.0"
    Description = "Test Network Update"
    Features = @("Network update system test", "Corporate deployment test")
    BugFixes = @("Test bug fix 1", "Test bug fix 2")
}

$version | ConvertTo-Json -Depth 10 | Out-File "$TestPath\version.json" -Encoding UTF8

# Create changelog
$changelog = @"
Contextualizer Update v1.1.0
Released: $(Get-Date -Format 'MMMM dd, yyyy')

Test Network Update

NEW FEATURES:
- Network update system test
- Corporate deployment test

BUG FIXES:
- Test bug fix 1
- Test bug fix 2

For detailed information about this update, contact your IT administrator.
"@

$changelog | Out-File "$TestPath\changelog.txt" -Encoding UTF8

Write-Host "Test update package created successfully!" -ForegroundColor Green
Write-Host "Path: $TestPath" -ForegroundColor Cyan
Write-Host ""
Write-Host "Files created:" -ForegroundColor Yellow
Get-ChildItem $TestPath | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor White }

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Update your app settings to use: $TestPath" -ForegroundColor White
Write-Host "2. Run your current app (it should detect v1.1.0 available)" -ForegroundColor White
Write-Host "3. Test the update dialog and installation" -ForegroundColor White
