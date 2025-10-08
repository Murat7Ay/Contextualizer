# Contextualizer Release Build Script
# Creates optimized single-file executable for distribution

Write-Host "Building Contextualizer Release..." -ForegroundColor Cyan

# Clean previous builds
Write-Host "Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release
Remove-Item -Path ".\publish" -Recurse -Force -ErrorAction SilentlyContinue

# Restore packages
Write-Host "Restoring packages..." -ForegroundColor Yellow
dotnet restore

# Build solution
Write-Host "Building solution..." -ForegroundColor Yellow
dotnet build --configuration Release --no-restore

# Publish single file executable
Write-Host "Publishing single-file executable..." -ForegroundColor Yellow
dotnet publish WpfInteractionApp/WpfInteractionApp.csproj `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output ".\publish\win-x64" `
    /p:PublishSingleFile=true `
    /p:PublishReadyToRun=true `
    /p:PublishTrimmed=false `
    /p:IncludeNativeLibrariesForSelfExtract=true `
    /p:EnableCompressionInSingleFile=true

# Create distribution package
Write-Host "Creating distribution package..." -ForegroundColor Yellow
$distPath = ".\publish\Contextualizer-Portable"
New-Item -ItemType Directory -Path $distPath -Force | Out-Null

# Copy and rename executable
Copy-Item ".\publish\win-x64\WpfInteractionApp.exe" "$distPath\Contextualizer.exe"

# Note: Application will auto-create Config, Data, and Plugins directories on first run

# Create ZIP package
Write-Host "Creating ZIP package..." -ForegroundColor Yellow
$zipPath = ".\publish\Contextualizer-Portable-v1.0.zip"
if (Get-Command Compress-Archive -ErrorAction SilentlyContinue) {
    Compress-Archive -Path "$distPath\*" -DestinationPath $zipPath -Force
    Write-Host "ZIP package created: $zipPath" -ForegroundColor Green
}

# Display results
Write-Host "`nBuild completed successfully!" -ForegroundColor Green
Write-Host "Executable: $distPath\Contextualizer.exe" -ForegroundColor Cyan
Write-Host "Package: $zipPath" -ForegroundColor Cyan

$exeSize = (Get-Item "$distPath\Contextualizer.exe").Length / 1MB
Write-Host "Executable size: $([math]::Round($exeSize, 2)) MB" -ForegroundColor Yellow

Write-Host "`nReady for distribution!" -ForegroundColor Green
