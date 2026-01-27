# Build & Release

## Build Script
- PowerShell: [build-release.ps1](build-release.ps1)

## Release Pipeline (Script Steps)
1. Clean and restore .NET packages.
2. Build React UI (Contextualizer.UI) and generate dist.
3. Build solution in Release mode.
4. Publish single-file WPF executable (win-x64, self-contained).
5. Copy Assets and React UI dist to the portable package.
6. Create ZIP archive for distribution.

## UI Packaging
- UI build output is copied into Assets\Ui\dist.
- A fallback warning screen appears if UI dist is missing.

## Output Artifacts
- Portable folder: publish\Contextualizer-Portable
- Executable: Contextualizer.exe
- ZIP package: publish\Contextualizer-Portable-v1.0.zip

## Publish Configuration (WPF)
- Runtime: win-x64
- Self-contained: true
- PublishSingleFile: true
- PublishReadyToRun: true
- PublishTrimmed: false
- IncludeNativeLibrariesForSelfExtract: true
- EnableCompressionInSingleFile: true

## Solution
- Solution file: [Contextualizer.sln](Contextualizer.sln)

## Projects
- Core: [Contextualizer.Core/Contextualizer.Core.csproj](Contextualizer.Core/Contextualizer.Core.csproj)
- Plugin contracts: [Contextualizer.PluginContracts/Contextualizer.PluginContracts.csproj](Contextualizer.PluginContracts/Contextualizer.PluginContracts.csproj)
- Plugins: [Contextualizer.Plugins/Contextualizer.Plugins.csproj](Contextualizer.Plugins/Contextualizer.Plugins.csproj)
- WPF app: [WpfInteractionApp/WpfInteractionApp.csproj](WpfInteractionApp/WpfInteractionApp.csproj)

## UI Build
- Web UI: [Contextualizer.UI/package.json](Contextualizer.UI/package.json)
