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

# Copy executable
Copy-Item ".\publish\win-x64\WpfInteractionApp.exe" "$distPath\Contextualizer.exe"

# Create portable directory structure
Write-Host "Creating portable directory structure..." -ForegroundColor Yellow
$configPath = "$distPath\Config"
$dataPath = "$distPath\Data"
$pluginsPath = "$distPath\Plugins"

New-Item -ItemType Directory -Path $configPath -Force | Out-Null
New-Item -ItemType Directory -Path "$dataPath\Exchange" -Force | Out-Null
New-Item -ItemType Directory -Path "$dataPath\Installed" -Force | Out-Null
New-Item -ItemType Directory -Path "$dataPath\Logs" -Force | Out-Null
New-Item -ItemType Directory -Path "$dataPath\Temp" -Force | Out-Null
New-Item -ItemType Directory -Path $pluginsPath -Force | Out-Null

# Create default configuration files
Write-Host "Creating default configuration..." -ForegroundColor Yellow

# Default appsettings.json
$appSettings = @{
    handlers_file_path = "Config\handlers.json"
    plugins_directory = "Plugins"
    exchange_directory = "Data\Exchange"
    keyboard_shortcut = @{
        modifier_keys = @("Ctrl")
        key = "W"
    }
    clipboard_wait_timeout = 5000
    window_activation_delay = 100
    clipboard_clear_delay = 800
    window_settings = @{
        width = 800
        height = 600
        left = 100
        top = 100
        window_state = "Normal"
    }
    ui_settings = @{
        theme = "Dark"
    }
    logging_settings = @{
        enable_local_logging = $true
        enable_usage_tracking = $true
        minimum_log_level = "Info"
        local_log_path = "Data\Logs"
    }
    config_system = @{
        enabled = $true
        config_file_path = "Config\config.ini"
        secrets_file_path = "Config\secrets.ini"
        auto_create_files = $true
        file_format = "ini"
    }
    network_update_settings = @{
        enable_network_updates = $true
        network_update_path = "\\server\share\Contextualizer\Updates"
        check_interval_hours = 24
        auto_install_non_mandatory = $false
        auto_install_mandatory = $true
    }
}

$appSettings | ConvertTo-Json -Depth 10 | Out-File "$configPath\appsettings.json" -Encoding UTF8

# Default handlers.json
$handlers = @{
    handlers = @(
        @{
            name = "Welcome to Contextualizer"
            type = "manual"
            screen_id = "welcome_screen"
            title = "Welcome!"
            description = "Get started with Contextualizer"
            actions = @(
                @{
                    name = "show_notification"
                    message = "🎉 Welcome to Contextualizer!`n`nYour portable installation is ready to use.`n`n✅ Press Ctrl+W to activate`n✅ Visit marketplace for more handlers`n✅ Check documentation for advanced features"
                    title = "Welcome"
                    duration = 10
                }
            )
        }
    )
}

$handlers | ConvertTo-Json -Depth 10 | Out-File "$configPath\handlers.json" -Encoding UTF8

# Copy sample config files
Write-Host "Creating sample configuration files..." -ForegroundColor Yellow
Copy-Item "sample-regex-handler-with-config.json" "$dataPath\Exchange\" -ErrorAction SilentlyContinue

# Create README
Write-Host "Creating documentation..." -ForegroundColor Yellow
$readme = @"
# 🚀 Contextualizer Portable

## Quick Start
1. Run Contextualizer.exe
2. Press Ctrl+W to activate clipboard monitoring
3. Copy any text and see the magic!

## First Steps
- **Test**: Right-click tray icon → Manual Handlers → "Welcome to Contextualizer"
- **Install Handlers**: Click marketplace icon in main window
- **Configure**: Settings → adjust keyboard shortcut and preferences

## Directory Structure
- **Config/**: Application settings and handler definitions
- **Data/Exchange/**: Marketplace and installable handlers
- **Data/Logs/**: Application logs and analytics
- **Plugins/**: Custom plugin assemblies

## Documentation
- System Architecture: contextualizer-system-flow.md
- User Guide: contextualizer-user-guide.md
- Source Code: https://github.com/Murat7Ay/Contextualizer

## Support
- Check logs in Data/Logs/ for troubleshooting
- Enable debug logging in Settings → Logging
- Visit GitHub repository for issues and updates

Built with ❤️ using .NET 9.0 and WPF
"@

$readme | Out-File "$distPath\README.txt" -Encoding UTF8

# Copy documentation if available
Copy-Item "contextualizer-system-flow.md" $distPath -ErrorAction SilentlyContinue
Copy-Item "contextualizer-user-guide.md" $distPath -ErrorAction SilentlyContinue

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
