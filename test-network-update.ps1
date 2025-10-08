# Test Network Update Setup
Write-Host "Network Update Test Ortami Hazirlaniyor..." -ForegroundColor Cyan

# Test network update path
$networkUpdatePath = "C:\Temp\Contextualizer\Updates"

# Clean old test files
if (Test-Path $networkUpdatePath) {
    Write-Host "   Eski test dosyalari temizleniyor..." -ForegroundColor Yellow
    Remove-Item $networkUpdatePath -Recurse -Force -ErrorAction SilentlyContinue
}

# Create network update directory
New-Item -ItemType Directory -Path $networkUpdatePath -Force | Out-Null
Write-Host "Network update dizini olusturuldu: $networkUpdatePath" -ForegroundColor Green

# Copy exe
$sourceExe = ".\publish\Contextualizer-Portable\Contextualizer.exe"
if (!(Test-Path $sourceExe)) {
    Write-Error "Exe bulunamadi: $sourceExe"
    Write-Host "Once build-release.ps1 calistirin!" -ForegroundColor Red
    exit 1
}

Copy-Item $sourceExe -Destination "$networkUpdatePath\Contextualizer.exe" -Force
Write-Host "Exe kopyalandi" -ForegroundColor Green

# Copy update BAT script
$sourceBat = ".\install_update.bat"
if (!(Test-Path $sourceBat)) {
    Write-Error "install_update.bat bulunamadi!"
    exit 1
}
Copy-Item $sourceBat -Destination "$networkUpdatePath\install_update.bat" -Force
Write-Host "install_update.bat kopyalandi" -ForegroundColor Green

# Create version.json (v1.1.0 - newer version!)
$versionInfo = @{
    Version = "1.1.0"
    ExecutableFileName = "Contextualizer.exe"
    ReleaseDate = (Get-Date).ToString("yyyy-MM-ddTHH:mm:ssZ")
    IsMandatory = $false
    MinimumRequiredVersion = "1.0.0"
    Description = "Test Update - Network Update System Test"
    Features = @(
        "Network update working",
        "Progress bar with download",
        "Auto restart",
        "Mandatory/Optional support"
    )
    BugFixes = @(
        "Test fake update",
        "Will work in production"
    )
}

$versionJsonPath = Join-Path $networkUpdatePath "version.json"
$versionInfo | ConvertTo-Json -Depth 10 | Out-File $versionJsonPath -Encoding UTF8 -NoNewline

Write-Host "version.json olusturuldu (v1.1.0)" -ForegroundColor Green

# Create changelog.txt
$changelog = @"
Contextualizer v1.1.0 - Test Update
====================================

Release Date: $(Get-Date -Format 'yyyy-MM-dd HH:mm')

BU BIR TEST UPDATE'IDIR!

Yeni Ozellikler:
- Network update sistemi calisiyor
- Otomatik guncelleme kontrolu (3 saniye sonra)
- Progress bar ile file copy
- Mandatory/Optional update destegi
- Auto-restart after update

Test Notlari:
Bu update gercekte ayni exe'nin kopyasidir.
Sadece version numarasi farklidir (1.1.0 > 1.0.0).
Update mekanizmasini test etmek icindir.

Test Edilen Ozellikler:
- Network path erisimi
- version.json parse
- File size validation
- Progress bar
- Update installation
- Application restart
"@

$changelogPath = Join-Path $networkUpdatePath "changelog.txt"
$changelog | Out-File $changelogPath -Encoding UTF8

Write-Host "changelog.txt olusturuldu" -ForegroundColor Green

# Summary
Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "NETWORK UPDATE TEST ORTAMI HAZIR!" -ForegroundColor Green
Write-Host "==========================================================" -ForegroundColor Cyan

Write-Host "`nTest Dosyalari:" -ForegroundColor Yellow
Write-Host "   Path: $networkUpdatePath" -ForegroundColor White
$exeSize = (Get-Item "$networkUpdatePath\Contextualizer.exe").Length / 1MB
Write-Host "   - Contextualizer.exe ($([math]::Round($exeSize, 2)) MB)" -ForegroundColor Gray
Write-Host "   - install_update.bat (Update script)" -ForegroundColor Gray
Write-Host "   - version.json (v1.1.0)" -ForegroundColor Gray
Write-Host "   - changelog.txt" -ForegroundColor Gray

Write-Host "`nConfig Ayari:" -ForegroundColor Yellow
Write-Host "   Uygulama appsettings.json'da bu ayar olmali:" -ForegroundColor White
Write-Host "   network_update_settings:" -ForegroundColor Gray
Write-Host "     enable_network_updates: true" -ForegroundColor Gray
Write-Host "     network_update_path: `"$networkUpdatePath`"" -ForegroundColor Gray
Write-Host "     update_script_path: `"$networkUpdatePath\install_update.bat`"" -ForegroundColor Gray

Write-Host "`nTEST ADIMLARI:" -ForegroundColor Yellow
Write-Host "----------------------------------------------------------" -ForegroundColor Gray
Write-Host "1. Uygulamayi calistir:" -ForegroundColor Cyan
Write-Host "     .\publish\Contextualizer-Portable\Contextualizer.exe" -ForegroundColor White

Write-Host "`n2. 3 saniye bekle!" -ForegroundColor Cyan
Write-Host "     - Update window otomatik acilacak" -ForegroundColor Gray
Write-Host "     - Version: 1.0.0 -> 1.1.0 gosterecek" -ForegroundColor Gray
Write-Host "     - Release notes gorunecek" -ForegroundColor Gray

Write-Host "`n3. Test secenekleri:" -ForegroundColor Cyan
Write-Host "     [Test Network] -> Network baglantisini test et" -ForegroundColor White
Write-Host "     [Install Update] -> Update'i yukle ve restart" -ForegroundColor White
Write-Host "     [Remind Later] -> Sonra hatirlat" -ForegroundColor White

Write-Host "`n4. Mandatory test:" -ForegroundColor Cyan
Write-Host "     $versionJsonPath dosyasinda" -ForegroundColor White
Write-Host "     `"IsMandatory`": false -> true yap" -ForegroundColor White
Write-Host "     Kullanici update yapmadan kapatamayacak!" -ForegroundColor White

Write-Host "`n==========================================================" -ForegroundColor Cyan
Write-Host "Hazir! Uygulamayi baslat!" -ForegroundColor Green
Write-Host ""

# Open in Explorer
Start-Process explorer.exe $networkUpdatePath
