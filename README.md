# Müzik USB Yöneticisi V4 🎵🚗

<div align="center">
  <img src="https://img.shields.io/badge/Versiyon-V4.0%20(Premium)-blue?style=for-the-badge&logo=appveyor" alt="Version"/>
  <img src="https://img.shields.io/badge/Platform-Windows%20(WPF)-blueviolet?style=for-the-badge&logo=windows" alt="Platform"/>
  <img src="https://img.shields.io/badge/.NET-8.0-512BD4?style=for-the-badge&logo=dotnet" alt=".NET 8.0"/>
  <img src="https://img.shields.io/badge/License-MIT-green?style=for-the-badge" alt="License"/>
</div>

<br/>

Müzik USB Yöneticisi V4, araç teypleri ve harici oynatıcılar için özel olarak geliştirilmiş **akıllı müzik indirme ve USB senkronizasyon** aracıdır. YouTube üzerinden tekil şarkı, oynatma listesi veya direkt kanal linki vererek müzikleri saniyeler içinde en yüksek kalitede bilgisayarınıza indirebilir ve tek tıkla USB belleğinize aktarabilirsiniz.

## 🌟 Öne Çıkan Üstün Özellikler

* **🚀 Çoklu İndirme Desteği (Multi-Threading):** Limitleri ortadan kaldırın! Şarkıları aynı anda indirerek maksimum indirme hızına ulaşır (Varsayılan olarak 3 concurrent download destekler).
* **🎛️ FFmpeg ile Gerçek Ses Normalizasyonu:** YouTube üzerindeki bazı şarkıların ses seviyeleri düşük veya orantısız olabilir; araçta volume ile oynamak çok rahatsız edicidir. Uygulama, her inen şarkıyı otomatik olarak FFmpeg stüdyo editöründen geçirip ses seviyelerini pürüzsüz ve standart bir yüksekliğe sabitler (*Loudnorm filtresi*).
* **🖼️ ID3 Tag & Albüm Kapak Gömmesi (Metadata):** Şarkılar inerken sadece sesi almakla kalmaz; YouTube kapak fotoğraflarını ve sanatçı bilgilerini direkt müzik dosyasının meta verisine (ID3) kodlar. Bu sayede gelişmiş araç teyplerinde şarkının kapağı ve doğru sanatçı ismi ekranda belirecektir.
* **📂 Akıllı Sanatçı Klasörlemesi:** İnen binlerce şarkıyı tek bir klasöre yığmak yerine, okunan YouTube Kanal / Sanatçı isimlerine göre şarkıları arkaplanda kendi alt klasörlerine ayırır.
* **🗑️ Akıllı USB Temizleyicisi (Auto-Clean):** USB'de yer kaplayan ve eski teyplerin şarkı geçerken okuyamayıp takılmasına veya teybin kapanmasına neden olan gizli dosyaları ve uyumsuz formatları (`.webm`, `.mp4`, `.ini`, `.txt` vb.) tek tıkla tespit eder ve siler.
* **🔍 Remix / Mix Orijinal Filtrelemesi:** Arama kısmına girdiğiniz sanatçıların "club, mix, remix" tarzındaki çöp versiyonlarını otomatik filtrelemek için yeni filtre motoru eklendi. Listede sadece orijinal parçaları ayırabilirsiniz.
* **🔗 Üstün Bağlantı & API Desteği:** 
  * Şarkı veya Sanatçı adı ile jenerik metin araması (Limit Yok)
  * Direkt YouTube Video linki desteği
  * YouTube Oynatma Listesi (Playlist) indirme desteği
  * YouTube Kanal (`@kanaladı`) ve User profilindeki tüm yüklemeleri çekebilme

## 📸 Arayüz Önizlemesi
_(Yakında projenizin yeni arayüzüne ait muhteşem ekran görüntüsünü buraya ekleyebilirsiniz)_

## 🛠️ Nasıl Kullanılır?

1. **Hedef USB'yi Seçin:** Uygulama açıldığında bilgisayarınıza takılı olan USB belleği (varsa) otomatik algılar. Gerekirse açılır menüden kopyalama yapılacak hedef diski seçin.
2. **Arama Yapın / Link Yapıştırın:** Arama kutusuna sanatçı ismini veya doğrudan YouTube liste/video linkini yapıştırıp **"Eksik Şarkıları Çek"** butonuna basın.
3. **Filtreleyin ve Seçin:** Çıkan listeden Remix/Mix filtresiyle sonuçları daraltabilir ve indirmek istediklerinizi tikleyerek seçebilirsiniz.
4. **Listeyi Arşive İndir (PC):** "İndir" butonuna tıklayın. Müzikler eşzamanlı çekilecek, kalitesi ayarlanacak ve kapakları gömülmüş şekilde bilgisayarınızdaki arşivinize inecek. Süreç, dinamik ilerleme barları ile gösterilir.
5. **USB'ye Kopyala:** İndirme işlemleri tamamlandığında yeşil renkli "USB'ye Kopyala" butonuna tıklayarak klasörünüzdeki parçaları eksiksiz ve hızlıca cihazınıza senkronize edin.

## 💻 Sistem Gereksinimleri & Altyapı
Uygulama modern ve güncel .NET mimarisi üzerine inşa edilmiştir.

* **Platform:** Windows 10 / Windows 11 (WPF Desktop App)
* **Framework:** .NET 8.0 (C#)
* **Arkaplan Teknolojileri:** 
   * `YoutubeExplode`: Hızlı YouTube veri çekimi ve bypass indirme işlemleri.
   * `Xabe.FFmpeg` & `Xabe.FFmpeg.Downloader`: Gelişmiş "Loudnorm" ses dengesi filtresi (gerekli bileşenleri kendi indirir).
   * `TagLibSharp`: Ses dosyalarının ID3 resim ve sanatçı meta verilerinin yönetimi.

## 📄 Lisans
Bu proje açık kaynak olup [MIT Lisansı](LICENSE) altında sunulmaktadır. Geliştirmek veya kopyalamak serbesttir.

---
*🎶 Yüklediğiniz şarkılarla yollara hükmedin! Güvenli sürüşler! 🚗*
