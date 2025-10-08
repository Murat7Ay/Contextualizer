# IT Ekibi İçin - Network Update Kurulum Rehberi

## 🎯 Gerekli İzinler

### 1. **AppLocker/Whitelist İzinleri**

Lütfen aşağıdaki dosyalara çalıştırma izni verilmesini talep ediyorum:

```
C:\PortableApps\Contextualizer\Contextualizer.exe
\\fileserver\share\Contextualizer\Updates\install_update.bat
```

**Not:** Sadece bu iki dosyanın çalıştırılmasına izin verilmesi yeterli olacaktır.

---

## 📁 Network Share Yapısı

Network share'de şu klasör yapısı oluşturulmalı:

```
\\fileserver\share\Contextualizer\Updates\
├── Contextualizer.exe           (Yeni versiyon EXE)
├── install_update.bat           (Update script)
├── version.json                 (Version bilgileri)
└── changelog.txt                (Release notları)
```

---

## 📝 Dosya Detayları

### 1. **install_update.bat**

Bu dosya update işlemini gerçekleştirir. İçeriği:
- Application'ı kapatır (`taskkill`)
- Mevcut EXE'yi yedekler
- Yeni versiyonu kopyalar
- Application'ı yeniden başlatır

**Dosya:** `install_update.bat` (proje root'unda mevcut)

### 2. **version.json**

Örnek içerik:
```json
{
  "Version": "1.1.0",
  "ExecutableFileName": "Contextualizer.exe",
  "ReleaseDate": "2025-01-15T10:00:00Z",
  "IsMandatory": false,
  "MinimumRequiredVersion": "1.0.0",
  "Description": "Contextualizer v1.1.0 Update",
  "Features": [
    "Performance improvements",
    "Bug fixes"
  ],
  "BugFixes": [
    "Fixed encoding issue",
    "Improved update mechanism"
  ]
}
```

### 3. **changelog.txt**

Kullanıcıya gösterilecek release notları (Türkçe olabilir).

---

## 🔒 Güvenlik Notları

1. **BAT Script:**
   - Network share'de merkezi olarak yönetilir
   - Tek bir yer, tüm kullanıcılar için
   - IT ekibi dilediği zaman güncelleyebilir

2. **EXE Whitelist:**
   - Sadece `Contextualizer.exe` adına izin verilmeli
   - `update.bat`, `install.bat` gibi isimler çalışmayacak
   - Değişiklik gerekirse IT ile koordinasyon

3. **Erişim İzinleri:**
   - Network share: **Read-Only** yeterli
   - BAT script: **Execute** izni gerekli
   - Local klasör: **Write** izni gerekli (`C:\PortableApps\Contextualizer\`)

---

## 🧪 Test Ortamı

Test için lokal bir ortam hazırlanmıştır:

```powershell
# Test ortamını hazırla
.\test-network-update.ps1
```

Bu script:
- `C:\Temp\Contextualizer\Updates\` klasörü oluşturur
- Test dosyalarını kopyalar
- Konfigurasyon ayarlarını gösterir

---

## 📞 İletişim

Sorularınız için lütfen benimle iletişime geçin.

**Test Sonuçları:**
- ✅ EXE ismi: Contextualizer.exe
- ✅ Network BAT: install_update.bat
- ✅ Update mekanizması çalışıyor
- ⏳ AppLocker izinleri bekleniyor

---

## 🚀 Deployment Sonrası

İzinler verildikten sonra:

1. Network share'i hazırlayın
2. İlk versiyonu (v1.0.0) deploy edin
3. Test update'i (v1.1.0) yerleştirin
4. Bir kullanıcı ile test edin
5. Production'a geçin

**Tahmini süre:** 30 dakika

