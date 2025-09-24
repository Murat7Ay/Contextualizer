# Corporate Network Update Preparation Script
# Prepares Contextualizer update package for network deployment

param(
    [Parameter(Mandatory=$true)]
    [string]$Version,
    
    [Parameter(Mandatory=$true)]
    [string]$NetworkUpdatePath,
    
    [Parameter(Mandatory=$false)]
    [bool]$IsMandatory = $false,
    
    [Parameter(Mandatory=$false)]
    [string]$MinimumRequiredVersion = "",
    
    [Parameter(Mandatory=$false)]
    [string]$Description = "Contextualizer Update",
    
    [Parameter(Mandatory=$false)]
    [string]$ChangelogFile = ""
)

Write-Host "🏢 Preparing Corporate Network Update..." -ForegroundColor Cyan

# Validate inputs
if (![System.Version]::TryParse($Version, [ref]$null)) {
    Write-Error "Invalid version format: $Version"
    exit 1
}

if (!(Test-Path $NetworkUpdatePath)) {
    Write-Host "Creating network update directory: $NetworkUpdatePath" -ForegroundColor Yellow
    New-Item -ItemType Directory -Path $NetworkUpdatePath -Force | Out-Null
}

# Check if executable exists
$executablePath = ".\publish\Contextualizer-Portable\Contextualizer.exe"
if (!(Test-Path $executablePath)) {
    Write-Error "Executable not found: $executablePath"
    Write-Host "Please run build-release.ps1 first to create the executable." -ForegroundColor Yellow
    exit 1
}

# Copy executable to network share
$networkExecutablePath = Join-Path $NetworkUpdatePath "Contextualizer.exe"
Write-Host "📦 Copying executable to network share..." -ForegroundColor Yellow
Copy-Item $executablePath $networkExecutablePath -Force

# Create version.json
$versionInfo = @{
    Version = $Version
    ExecutableFileName = "Contextualizer.exe"
    ReleaseDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    IsMandatory = $IsMandatory
    MinimumRequiredVersion = $MinimumRequiredVersion
    Description = $Description
    Features = @()
    BugFixes = @()
}

$versionJsonPath = Join-Path $NetworkUpdatePath "version.json"
Write-Host "📄 Creating version.json..." -ForegroundColor Yellow
$versionInfo | ConvertTo-Json -Depth 10 | Out-File $versionJsonPath -Encoding UTF8

# Handle changelog
$changelogPath = Join-Path $NetworkUpdatePath "changelog.txt"
if ($ChangelogFile -and (Test-Path $ChangelogFile)) {
    Write-Host "📝 Copying changelog..." -ForegroundColor Yellow
    Copy-Item $ChangelogFile $changelogPath -Force
} else {
    # Create default changelog
    $defaultChangelog = @"
Contextualizer Update v$Version
Released: $(Get-Date -Format 'MMMM dd, yyyy')

$Description

For detailed information about this update, contact your IT administrator.
"@
    $defaultChangelog | Out-File $changelogPath -Encoding UTF8
}

# Create deployment instructions
$deploymentInstructions = @"
# Contextualizer Corporate Update Deployment

## Version Information
- Version: $Version
- Release Date: $(Get-Date -Format 'MMMM dd, yyyy')
- Mandatory: $(if($IsMandatory) {"Yes"} else {"No"})
- Minimum Required Version: $(if($MinimumRequiredVersion) {$MinimumRequiredVersion} else {"None"})

## Files in this package:
- Contextualizer.exe ($('{0:N2}' -f ((Get-Item $executablePath).Length / 1MB)) MB)
- version.json (Version metadata)
- changelog.txt (Release notes)
- deployment-instructions.txt (This file)

## Network Path Setup:
This update is deployed from: $NetworkUpdatePath

## Client Configuration:
Contextualizer clients should be configured to check for updates at this network path.
This can be configured in the application settings or via configuration file.

## Permissions:
Ensure all users have READ access to this directory.
The directory should be accessible via UNC path: \\server\share\path

## Verification:
1. Verify all users can access: $NetworkUpdatePath
2. Check version.json is valid JSON
3. Ensure Contextualizer.exe is not corrupted
4. Test with a pilot user before company-wide deployment

## Rollback:
To rollback, replace the files with the previous version or remove version.json
to prevent clients from detecting the update.

Generated: $(Get-Date)
"@

$instructionsPath = Join-Path $NetworkUpdatePath "deployment-instructions.txt"
$deploymentInstructions | Out-File $instructionsPath -Encoding UTF8

# Create client configuration sample
$clientConfigSample = @"
{
  "update_settings": {
    "enable_network_updates": true,
    "network_update_path": "$NetworkUpdatePath",
    "check_interval_hours": 24,
    "auto_install_non_mandatory": false,
    "auto_install_mandatory": true
  }
}
"@

$clientConfigPath = Join-Path $NetworkUpdatePath "client-config-sample.json"
$clientConfigSample | Out-File $clientConfigPath -Encoding UTF8

# Set appropriate permissions (Windows)
try {
    $acl = Get-Acl $NetworkUpdatePath
    $accessRule = New-Object System.Security.AccessControl.FileSystemAccessRule("Everyone", "ReadAndExecute", "ContainerInherit,ObjectInherit", "None", "Allow")
    $acl.SetAccessRule($accessRule)
    Set-Acl -Path $NetworkUpdatePath -AclObject $acl
    Write-Host "✅ Set read permissions for Everyone" -ForegroundColor Green
} catch {
    Write-Warning "Could not set permissions automatically. Please ensure users have read access to: $NetworkUpdatePath"
}

# Display summary
Write-Host "`n🎉 Corporate Update Package Created Successfully!" -ForegroundColor Green
Write-Host "📁 Network Path: $NetworkUpdatePath" -ForegroundColor Cyan
Write-Host "📄 Files created:" -ForegroundColor Cyan
$fileSize = '{0:N2}' -f ((Get-Item $networkExecutablePath).Length / 1MB)
Write-Host "   - Contextualizer.exe ($fileSize MB)" -ForegroundColor White
Write-Host "   - version.json" -ForegroundColor White
Write-Host "   - changelog.txt" -ForegroundColor White
Write-Host "   - deployment-instructions.txt" -ForegroundColor White
Write-Host "   - client-config-sample.json" -ForegroundColor White

if ($IsMandatory) {
    Write-Host "`n⚠️  MANDATORY UPDATE - All clients will be required to install this update" -ForegroundColor Yellow
}

Write-Host "`n📋 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Test the update with a pilot group" -ForegroundColor White
Write-Host "2. Verify network accessibility from client machines" -ForegroundColor White
Write-Host "3. Configure client applications to use this network path" -ForegroundColor White
Write-Host "4. Monitor deployment across your organization" -ForegroundColor White

Write-Host "`n🚀 Update package ready for enterprise deployment!" -ForegroundColor Green
