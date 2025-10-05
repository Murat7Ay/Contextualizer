# Tab ve Pencere Odaklama Kılavuzu

Bu kılavuz, Contextualizer uygulamasında tab ve pencere odaklama özelliklerinin nasıl kullanılacağını açıklar.

## Özellikler

### 1. Tab Odaklama Kontrolü (`auto_focus_tab`)
- **Varsayılan**: `false` ⚡ (Performans ve UX için güncellendi)
- **Açıklama**: Yeni açılan tab'ın otomatik olarak aktif hale gelip gelmeyeceğini kontrol eder.
- **`true`**: Tab açıldığında otomatik olarak aktif hale gelir
- **`false`**: Tab arka planda açılır, kullanıcı manuel olarak geçmeli (önerilen)

### 2. Pencere Öne Getirme (`bring_window_to_front`)
- **Varsayılan**: `false`
- **Açıklama**: Tüm uygulamanın öne getirilip getirilmeyeceğini kontrol eder.
- **`true`**: Uygulama penceresi öne gelir ve aktif hale gelir (akıllı state kontrolü ile korunmuş)
- **`false`**: Uygulama penceresi mevcut durumunda kalır

### 3. Akıllı Performans Optimizasyonu ⚡
- **Smart State Checking**: Pencere zaten öndeyse hiçbir işlem yapmaz
- **Gereksiz İşlem Önleme**: Sadece gerektiğinde activate eder
- **Çoklu Handler Koruması**: 10+ handler aynı anda çalışsa bile performans sorunu yaşatmaz
- **Debug Logging**: Tüm durumlar log'a kaydedilir

## Kullanım Senaryoları

### Senaryo 1: Sessiz Arka Plan İşlemi (Yeni Varsayılan) ⭐
```json
{
  "name": "Silent Background Handler",
  "auto_focus_tab": false,
  "bring_window_to_front": false,
  "actions": [{"name": "show_window", "key": "_formatted_output"}]
}
```
- Tab arka planda açılır (varsayılan davranış)
- Uygulama öne gelmez
- Kullanıcı dikkatini dağıtmaz
- En performanslı seçenek

### Senaryo 2: Aktif Tab Açma
```json
{
  "name": "Active Tab Handler",
  "auto_focus_tab": true,
  "bring_window_to_front": false,
  "actions": [{"name": "show_window", "key": "_formatted_output"}]
}
```
- Tab otomatik olarak aktif hale gelir (açık şekilde belirtilmiş)
- Uygulama mevcut durumunda kalır
- Kullanıcı yeni içeriği hemen görür

### Senaryo 3: Acil Durum/Önemli Bildirim
```json
{
  "name": "Urgent Notification Handler",
  "auto_focus_tab": true,
  "bring_window_to_front": true,
  "actions": [{"name": "show_window", "key": "_formatted_output"}]
}
```
- Tab otomatik olarak aktif hale gelir
- Tüm uygulama öne gelir ve aktif hale gelir
- Minimize edilmişse restore olur
- Kullanıcının tam dikkatini çeker

## Teknik Detaylar

### BringToFront Metodu Özellikleri
- Minimize edilmiş pencereyi restore eder
- Pencereyi aktif hale getirir
- Geçici olarak `Topmost` yapar, sonra kaldırır
- Hata durumlarında log kaydı tutar

### Örnek Handler Konfigürasyonu
```json
[
  {
    "name": "Data Processing Result",
    "description": "Veri işleme sonuçlarını göster",
    "type": "regex",
    "regex": "data-process-complete",
    "screen_id": "markdown",
    "title": "Processing Results",
    "auto_focus_tab": true,
    "bring_window_to_front": false,
    "actions": [
      {
        "name": "show_window",
        "key": "_formatted_output"
      }
    ],
    "output_format": "# İşlem Tamamlandı\n\nVeri işleme başarıyla tamamlandı."
  }
]
```

## Test Etme

Örnek handler'ları test etmek için:

1. `example-auto-focus-handler.json` dosyasını handler konfigürasyonunuza ekleyin
2. Clipboard'a aşağıdaki metinleri kopyalayın:
   - `silent-tab-test` → Sessiz tab açar
   - `auto-focus-test` → Normal tab açar
   - `bring-to-front-test` → Uygulamayı öne getirir

## Önemli Notlar

- `bring_window_to_front: true` kullanırken dikkatli olun, kullanıcı deneyimini olumsuz etkileyebilir
- Sessiz işlemler için her iki özelliği de `false` yapın
- Acil durumlar dışında `bring_window_to_front` kullanmaktan kaçının
- Bu özellikler sadece `show_window` action'ı ile çalışır

## Varsayılan Değerler (Güncellendi) ⚡

```json
{
  "auto_focus_tab": false,        // Tab arka planda açılır (performans için)
  "bring_window_to_front": false  // Uygulama öne gelmez (güvenli)
}
```

## Performans İpuçları

1. **Çoklu Handler Senaryoları**: 10+ handler aynı anda çalışsa bile sorun yok - akıllı kontrol sayesinde
2. **Smart State Checking**: Pencere zaten öndeyse hiç işlem yapılmaz (çok hızlı)
3. **Varsayılan Davranış**: Artık sessiz arka plan işlemi varsayılandır
4. **Debug Logging**: Tüm durumlar debug log'unda görülebilir

Bu ayarlar sayesinde kullanıcı deneyimini optimize edebilir ve farklı senaryolar için uygun davranışları tanımlayabilirsiniz.
