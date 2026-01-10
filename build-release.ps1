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

# Build React UI (Contextualizer.UI) for packaging into Assets\Ui\dist
Write-Host "Building React UI..." -ForegroundColor Yellow
$uiProject = ".\Contextualizer.UI"
if (Test-Path "$uiProject\package.json") {
    Push-Location $uiProject
    try {
        if (Test-Path ".\package-lock.json") {
            npm ci
        } else {
            npm install
        }
        npm run build
        Write-Host "React UI built successfully" -ForegroundColor Green
    } catch {
        Write-Host "Warning: React UI build failed. The app will show a 'UI build not found' screen at startup." -ForegroundColor Yellow
        Write-Host $_ -ForegroundColor Yellow
    } finally {
        Pop-Location
    }
} else {
    Write-Host "Warning: Contextualizer.UI not found at $uiProject" -ForegroundColor Yellow
}

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

# Copy Assets folder (required for WebView2 controls like PlSqlEditor)
Write-Host "Copying Assets folder..." -ForegroundColor Yellow
$assetsSource = ".\WpfInteractionApp\Assets"
$assetsTarget = "$distPath\Assets"
if (Test-Path $assetsSource) {
    Copy-Item -Path $assetsSource -Destination $assetsTarget -Recurse -Force
    Write-Host "Assets folder copied successfully" -ForegroundColor Green
} else {
    Write-Host "Warning: Assets folder not found at $assetsSource" -ForegroundColor Yellow
}

# Copy React UI build output into Assets\Ui\dist (preferred for ReactShellWindow)
Write-Host "Copying React UI build (dist)..." -ForegroundColor Yellow
$uiDistSource = ".\Contextualizer.UI\dist"
$uiDistTarget = "$assetsTarget\Ui\dist"
if (Test-Path $uiDistSource) {
    New-Item -ItemType Directory -Path "$assetsTarget\Ui" -Force | Out-Null
    Remove-Item -Path $uiDistTarget -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -Path $uiDistSource -Destination $uiDistTarget -Recurse -Force
    Write-Host "React UI copied to $uiDistTarget" -ForegroundColor Green

    # Also copy into the raw publish output folder so running publish\win-x64\WpfInteractionApp.exe works too.
    $publishAssetsTarget = ".\publish\win-x64\Assets\Ui\dist"
    New-Item -ItemType Directory -Path ".\publish\win-x64\Assets\Ui" -Force | Out-Null
    Remove-Item -Path $publishAssetsTarget -Recurse -Force -ErrorAction SilentlyContinue
    Copy-Item -Path $uiDistSource -Destination $publishAssetsTarget -Recurse -Force
    Write-Host "React UI also copied to $publishAssetsTarget" -ForegroundColor Green
} else {
    Write-Host "Warning: React UI dist folder not found at $uiDistSource" -ForegroundColor Yellow
    Write-Host "Tip: run 'npm run build' inside Contextualizer.UI to generate dist." -ForegroundColor Yellow
}

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
