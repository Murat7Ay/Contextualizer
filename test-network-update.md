# 🧪 Network Update System Test Guide

## 🎯 Test Senaryosu

Network update sistemini test etmek için aşağıdaki adımları izleyin:

## 📋 Adım 1: Test Environment Hazırlama

### 1.1 Release Build
```powershell
# Release build yap
.\build-release.ps1
```

### 1.2 Test Network Path Oluştur
```powershell
# Local test için klasör oluştur
$testPath = "C:\temp\Contextualizer\Updates"
New-Item -ItemType Directory -Path $testPath -Force
```

### 1.3 Update Package Hazırla
```powershell
# Test update package oluştur
.\prepare-network-update.ps1 -Version "1.1.0" -NetworkUpdatePath $testPath -Description "Test Network Update"
```

## 📋 Adım 2: Mevcut App'i Çalıştır

### 2.1 Settings Değiştir
```json
// AppSettings.json içinde network update settings:
{
  "network_update_settings": {
    "enable_network_updates": true,
    "network_update_path": "C:\\temp\\Contextualizer\\Updates",
    "check_interval_hours": 24,
    "auto_install_non_mandatory": false,
    "auto_install_mandatory": true
  }
}
```

### 2.2 App'i Başlat
- `.\publish\Contextualizer-Portable\Contextualizer.exe` çalıştır
- 3 saniye sonra update check olacak

## 📋 Adım 3: Test Senaryoları

### 3.1 Normal Update Test
```powershell
# Version 1.1.0 ile test package oluştur
.\prepare-network-update.ps1 -Version "1.1.0" -NetworkUpdatePath "C:\temp\Contextualizer\Updates"
```

### 3.2 Mandatory Update Test
```powershell
# Zorunlu update test
.\prepare-network-update.ps1 -Version "1.2.0" -NetworkUpdatePath "C:\temp\Contextualizer\Updates" -IsMandatory $true
```

### 3.3 Network Error Test
```powershell
# Network path'i mevcut olmayan path yaparak test et
# Settings'te: "\\nonexistent\share\Updates"
```

## 📋 Adım 4: UI Test Points

### 4.1 Update Dialog Kontrolü
- Corporate branding (🏢 icon)
- Version bilgileri doğru
- Release notes görünüyor
- Progress bar çalışıyor
- Test Network button çalışıyor

### 4.2 Mandatory Update Kontrolü
- "Remind Later" button gizli
- Pencere kapatılamıyor
- Uyarı mesajı görünüyor

### 4.3 Error Handling
- Network path erişilemez
- Version file yok
- Executable yok
- Permission denied

## 📋 Adım 5: End-to-End Test

### 5.1 Tam Update Süreci
1. App çalıştır (v1.0.0)
2. Network'te v1.1.0 var
3. Update dialog görünür
4. "Install Update" tıkla
5. Progress bar dolacak
6. App restart olacak
7. Yeni version çalışacak

### 5.2 Doğrulama
- Assembly version check: `Contextualizer.exe` → Properties → Details
- About dialog version kontrolü
- Log dosyalarında update kayıtları

## 🛠️ Troubleshooting

### Network Path Issues
```powershell
# Path accessibility test
Test-Path "C:\temp\Contextualizer\Updates"
Get-ChildItem "C:\temp\Contextualizer\Updates"
```

### Version File Issues
```powershell
# version.json kontrolü
Get-Content "C:\temp\Contextualizer\Updates\version.json" | ConvertFrom-Json
```

### Log Monitoring
```
# Log dosyalarını izle
Get-Content "Data\Logs\debug_*.log" -Wait
```

## 🎯 Expected Results

### Başarılı Test:
- ✅ Update check 3 saniye içinde
- ✅ Corporate dialog açılır
- ✅ Network test button çalışır
- ✅ Download progress görünür
- ✅ App restart olur
- ✅ Yeni version çalışır

### Error Cases:
- ❌ Network path yok → Error dialog
- ❌ Version file yok → Silent fail (debug log)
- ❌ Permission denied → Error message
- ❌ User cancel → App devam eder

## 🔧 Advanced Testing

### Performance Test
```powershell
# Büyük dosya ile test (100MB+)
$largeDummy = "C:\temp\Contextualizer\Updates\Contextualizer.exe"
fsutil file createnew $largeDummy 104857600  # 100MB
```

### Network Latency Test
```powershell
# Yavaş network simülasyonu
# Network throttling tools kullan
```

### Multi-User Test
```powershell
# Farklı kullanıcı hesapları ile test
# Domain/Local hesap farklılıkları
```

Bu test guide ile network update sisteminin tüm fonksiyonlarını test edebilirsin! 🎯
