using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Net.Http; // Thumbnail indirme
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Search;
using YoutubeExplode.Videos.Streams;

namespace MuzikUSBYoneticisi
{
    // Arama sonuçlarında göstereceğimiz model sınıfı
    public class YoutubeVideoItem : INotifyPropertyChanged
    {
        private string _title = string.Empty;
        public string Title
        {
            get => _title;
            set { _title = value; OnPropertyChanged(); }
        }

        public string Author { get; set; } = string.Empty;
        public string Duration { get; set; } = string.Empty;
        public string VideoId { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;

        // Progress bar yönetimi için yeni property'ler
        private double _progressValue;
        public double ProgressValue
        {
            get => _progressValue;
            set { _progressValue = value; OnPropertyChanged(); }
        }

        private Visibility _isDownloading = Visibility.Collapsed;
        public Visibility IsDownloading
        {
            get => _isDownloading;
            set { _isDownloading = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public partial class MainWindow : Window
    {
        // Bilgisayardaki ana arşiv dizini (Örn: Masaüstü veya Belgelerim\MuzikUSB_Arsiv)
        private string baseArchiveFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "MuzikUSB_Arsiv");
        private readonly YoutubeClient youtube = new YoutubeClient();
        private ObservableCollection<YoutubeVideoItem> searchResults = new ObservableCollection<YoutubeVideoItem>();

        // USB içindeki analiz edilmiş mevcut müziklerin listesi (Mükerrer indirmenin önüne geçmek için)
        private System.Collections.Generic.HashSet<string> currentUsbMusicFiles = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public MainWindow()
        {
            InitializeComponent();
            ArchivePathTextBox.Text = baseArchiveFolder;
            SearchResultsListBox.ItemsSource = searchResults;

            LogMessage("Sistem Başlatıldı: Premium Arayüz Aktif.");
            RefreshUsb_Click(null, null);
        }

        // ===================================
        // İndirme Klasörünü Değiştir (FolderBrowserDialog alternatifi - WPF OpenFolderDialog)
        // ===================================
        private void ChangeFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog();
            dialog.Title = "İndirme Arşivi İçin Klasör Seçin";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if (dialog.ShowDialog() == true)
            {
                baseArchiveFolder = dialog.FolderName;
                ArchivePathTextBox.Text = baseArchiveFolder;
                LogMessage($"[BİLGİ] Yeni İndirme Klasörü: {baseArchiveFolder}");
            }
        }

        // ====== DURUM YÖNETİMİ ======
        private void UpdateStatus(string status, bool isError = false)
        {
            Dispatcher.Invoke(() =>
            {
                StatusTextBlock.Text = status;
                StatusTextBlock.Foreground = isError 
                    ? new SolidColorBrush(Color.FromRgb(239, 68, 68)) // Kırmızı hata rengi
                    : new SolidColorBrush(Color.FromRgb(16, 185, 129)); // Yeşil başarılı/hazır rengi
            });
        }

        // Enter tuşuyla arama
        private void UrlTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddToListBtn_Click(sender, new RoutedEventArgs());
            }
        }

        // 1. ARAMA VE LİSTEYE EKLEME BUTONU
        private async void AddToListBtn_Click(object sender, RoutedEventArgs e)
        {
            string query = UrlTextBox.Text.Trim();
            if (string.IsNullOrEmpty(query))
            {
                UpdateStatus("Lütfen arama kelimesi veya link giriniz!", true);
                LogMessage("[HATA] Arama kutusu boş.");
                return;
            }

            searchResults.Clear();
            UpdateStatus("Youtube'da Aranıyor ve Taranıyor...");
            LogMessage($"[İŞLEM] '{query}' taranıyor...");

            try
            {
                Directory.CreateDirectory(baseArchiveFolder);

                // Ortak liste havuzu oluşturuyoruz (Tüm metodların video sonuçlarını buraya atacağız)
                var rawVideoList = new System.Collections.Generic.List<YoutubeVideoItem>();

                // Hangi arama yönteminin kullanılacağını belirliyoruz
                if (query.Contains("list="))
                {
                    LogMessage("[BİLGİ] Oynatma Listesi algılandı. Tüm liste çekiliyor...");
                    var videos = await youtube.Playlists.GetVideosAsync(query);
                    foreach (var vid in videos)
                    {
                        rawVideoList.Add(new YoutubeVideoItem { 
                            Title = vid.Title, Author = vid.Author.ChannelTitle, 
                            Duration = vid.Duration?.ToString(@"mm\:ss") ?? "00:00", 
                            VideoId = vid.Url, ThumbnailUrl = vid.Thumbnails.FirstOrDefault()?.Url ?? ""
                        });
                    }
                }
                else if (query.Contains("@") || query.Contains("/channel/") || query.Contains("/c/") || query.Contains("/user/"))
                {
                    LogMessage("[BİLGİ] Kanal Linki algılandı. Kanalın tüm videoları çekiliyor...");
                    var videos = await youtube.Channels.GetUploadsAsync(query);
                    foreach (var vid in videos)
                    {
                        rawVideoList.Add(new YoutubeVideoItem { 
                            Title = vid.Title, Author = vid.Author.ChannelTitle, 
                            Duration = vid.Duration?.ToString(@"mm\:ss") ?? "00:00", 
                            VideoId = vid.Url, ThumbnailUrl = vid.Thumbnails.FirstOrDefault()?.Url ?? ""
                        });
                    }
                }
                else
                {
                    LogMessage("[BİLGİ] Genel arama yapılıyor...");
                    int limit = 50; // Varsayılan değer
                    if (int.TryParse(SearchLimitTextBox.Text, out int parsedLimit) && parsedLimit > 0)
                    {
                        limit = parsedLimit;
                    }

                    var searchRes = await youtube.Search.GetVideosAsync(query);
                    int count = 0;
                    foreach (var vid in searchRes)
                    {
                        if (count >= limit) break;
                        rawVideoList.Add(new YoutubeVideoItem { 
                            Title = vid.Title, Author = vid.Author.ChannelTitle, 
                            Duration = vid.Duration?.ToString(@"mm\:ss") ?? "00:00", 
                            VideoId = vid.Url, ThumbnailUrl = vid.Thumbnails.FirstOrDefault()?.Url ?? ""
                        });
                        count++;
                    }
                }

                // Filtreleme (Remix / Orijinal Ayrımı)
                int filterIndex = FilterComboBox.SelectedIndex;
                if (filterIndex == 1) // Orijinaller (Remix / Mix Hariç)
                {
                    LogMessage("[FİLTRE] 'Remix, Mix, Club' gibi kelimeleri içerenler kaldırılıyor...");
                    rawVideoList = rawVideoList.Where(v => 
                        !v.Title.Contains("remix", StringComparison.OrdinalIgnoreCase) && 
                        !v.Title.Contains("mix", StringComparison.OrdinalIgnoreCase) &&
                        !v.Title.Contains("club", StringComparison.OrdinalIgnoreCase)).ToList();
                }
                else if (filterIndex == 2) // Sadece Remixler
                {
                    LogMessage("[FİLTRE] Sadece içerisinde 'Remix, Mix veya Club' bulunanlar gösteriliyor...");
                    rawVideoList = rawVideoList.Where(v => 
                        v.Title.Contains("remix", StringComparison.OrdinalIgnoreCase) || 
                        v.Title.Contains("mix", StringComparison.OrdinalIgnoreCase) ||
                        v.Title.Contains("club", StringComparison.OrdinalIgnoreCase)).ToList();
                }

                // Ön belleğe alınmış listeyi işle ve duplicate engelle
                foreach (var video in rawVideoList)
                {
                    string safeTitle = string.Join("_", video.Title.Split(Path.GetInvalidFileNameChars()));

                    bool existInPc = Directory.GetFiles(baseArchiveFolder, $"{safeTitle}.*", SearchOption.AllDirectories).Any();
                    bool existInUsb = currentUsbMusicFiles.Contains(safeTitle);

                    if (existInPc || existInUsb)
                    {
                        continue;
                    }

                    searchResults.Add(video);
                }

                if(searchResults.Count > 0)
                {
                    UpdateStatus("Listeye eklendi, indirmeye hazır.");
                    LogMessage($"[BİLGİ] İndirilebilecek {searchResults.Count} adet yeni şarkı bulundu.");
                    SelectAllBtn_Click(null, null); // Bulunca kolaylık olsun diye tümünü seç
                }
                else
                {
                    UpdateStatus("Tüm sonuçlar zaten mevcut veya sonuç bulunamadı!", true);
                    LogMessage("[BİLGİ] Şarkıların tamamı zaten arşivinizde veya USB'de bulunuyor.");
                }
            }
            catch (Exception ex)
            {
                UpdateStatus("Arama başarısız!", true);
                LogMessage($"[HATA] Arama sırasında sorun oluştu: {ex.Message}");
            }
        }

        // ====== LİSTE ARAÇLARI ======
        private void SelectAllBtn_Click(object sender, RoutedEventArgs e)
        {
            if (SearchResultsListBox.SelectedItems.Count == SearchResultsListBox.Items.Count && SearchResultsListBox.Items.Count > 0)
            {
                SearchResultsListBox.UnselectAll();
                SelectAllBtn.Content = "Tümünü Seç";
            }
            else
            {
                SearchResultsListBox.SelectAll();
                SelectAllBtn.Content = "Seçimi Kaldır";
            }
        }

        private void SearchResultsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = SearchResultsListBox.SelectedItems.Cast<YoutubeVideoItem>().ToList();

            if (selectedItems.Count == 0)
            {
                SelectionInfoText.Text = "0 Seçili (~0 MB)";
                SelectAllBtn.Content = "Tümünü Seç";
                return;
            }

            double totalMbEstimate = 0;
            foreach (var item in selectedItems)
            {
                string[] parts = item.Duration.Split(':');
                double minutes = 0;
                if (parts.Length == 2)
                {
                    int.TryParse(parts[0], out int m);
                    int.TryParse(parts[1], out int s);
                    minutes = m + (s / 60.0);
                }
                else if (parts.Length == 3)
                {
                    int.TryParse(parts[0], out int h);
                    int.TryParse(parts[1], out int m);
                    int.TryParse(parts[2], out int s);
                    minutes = (h * 60) + m + (s / 60.0);
                }
                // AAC 128kbps ortalama dakikada 0.94 MB - 1 MB tutar
                totalMbEstimate += minutes * 1.0; 
            }

            SelectionInfoText.Text = $"{selectedItems.Count} Seçili (~{Math.Round(totalMbEstimate, 1)} MB)";

            if (selectedItems.Count == SearchResultsListBox.Items.Count && SearchResultsListBox.Items.Count > 0)
                SelectAllBtn.Content = "Seçimi Kaldır";
            else
                SelectAllBtn.Content = "Tümünü Seç";
        }

        private void ClearListBtn_Click(object sender, RoutedEventArgs e)
        {
            searchResults.Clear();
            UpdateStatus("Liste boşaltıldı.");
        }

        private void CleanUsbBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UsbComboBox.SelectedItem == null)
            {
                UpdateStatus("Lütfen hedef USB'yi seçin!", true);
                return;
            }

            string targetDriveLetter = UsbComboBox.SelectedItem.ToString().Substring(0, 3);

            Task.Run(() =>
            {
                int deletedCount = 0;
                try
                {
                    Dispatcher.Invoke(() => UpdateStatus("Uyumsuz dosyalar temizleniyor..."));
                    var files = Directory.EnumerateFiles(targetDriveLetter, "*.*", SearchOption.AllDirectories)
                        .Where(s => s.EndsWith(".webm", StringComparison.OrdinalIgnoreCase) ||
                                    s.EndsWith(".mp4", StringComparison.OrdinalIgnoreCase) || // Videolar teybe uygun değil
                                    s.EndsWith(".txt", StringComparison.OrdinalIgnoreCase) ||
                                    s.EndsWith(".ini", StringComparison.OrdinalIgnoreCase));

                    foreach (var f in files)
                    {
                        File.Delete(f);
                        deletedCount++;
                    }

                    LogMessage($"[BİLGİ] USB Temizlendi. {deletedCount} adet gereksiz dosya silindi.");
                    Dispatcher.Invoke(() => UpdateStatus($"Temizlik Tamamlandı ({deletedCount} dosya)"));
                }
                catch (Exception ex)
                {
                    LogMessage($"[HATA] Temizleme işlemi sırasında hata oluştu: {ex.Message}");
                    Dispatcher.Invoke(() => UpdateStatus("Temizlik Hatası", true));
                }
            });
        }

        private void RefreshUsb_Click(object sender, RoutedEventArgs e)
        {
            UsbComboBox.Items.Clear();
            currentUsbMusicFiles.Clear();

            try
            {
                var usbDrives = DriveInfo.GetDrives().Where(d => d.DriveType == DriveType.Removable && d.IsReady);
                foreach (var drive in usbDrives)
                {
                    try
                    {
                        UsbComboBox.Items.Add($"{drive.Name} ({drive.VolumeLabel})");
                    }
                    catch
                    {
                        UsbComboBox.Items.Add($"{drive.Name} (Bilinmeyen)");
                    }
                }
                if (UsbComboBox.Items.Count > 0) UsbComboBox.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                LogMessage($"[HATA] USB Listeleme hatası: {ex.Message}");
            }

            LogMessage("[BİLGİ] USB Sürücü listesi güncellendi.");
        }

        private async void UsbComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (UsbComboBox.SelectedItem != null)
            {
                string selectedUsbStr = UsbComboBox.SelectedItem.ToString();
                string targetDriveLetter = selectedUsbStr.Substring(0, 3); // "D:\", "E:\" gibi...

                LogMessage($"[BİLGİ] Hedef Cihaz Seçildi: {selectedUsbStr}. İçeriği Okunuyor...");
                UpdateStatus("USB Taranıyor...");

                currentUsbMusicFiles.Clear();

                await Task.Run(() =>
                {
                    try
                    {
                        // Popüler müzik formatlarını hızlıca listele
                        var files = Directory.EnumerateFiles(targetDriveLetter, "*.*", SearchOption.AllDirectories)
                                             .Where(s => s.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase) ||
                                                         s.EndsWith(".m4a", StringComparison.OrdinalIgnoreCase) ||
                                                         s.EndsWith(".wav", StringComparison.OrdinalIgnoreCase));

                        foreach (var f in files)
                        {
                            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(f);
                            currentUsbMusicFiles.Add(fileNameWithoutExt);
                        }
                    }
                    catch (UnauthorizedAccessException) { }
                    catch (Exception ex)
                    {
                        LogMessage($"[UYARI] USB taranırken okunamayan dosyalar oldu: {ex.Message}");
                    }
                });

                LogMessage($"[BİLGİ] Tarama Bitti. USB'de {currentUsbMusicFiles.Count} adet müzik bulundu.");
                UpdateStatus("Hazır (USB Tarandı)");
            }
        }


        // 2. PC'YE İNDİR BUTONU (Çoklu İş Parçacıklı & Klasörlü & FFmpeg Normalization)
        private async void DownloadBtn_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = SearchResultsListBox.SelectedItems.Cast<YoutubeVideoItem>().ToList();

            if (selectedItems.Count == 0)
            {
                UpdateStatus("Lütfen listeden en az bir şarkı seçiniz!", true);
                return;
            }

            UpdateStatus("Gerekli FFmpeg araçları kontrol ediliyor...");
            try
            {
                // Xabe.FFmpeg arkaplanda kendi indirsin, veya varolanı kullansın.
                await Xabe.FFmpeg.Downloader.FFmpegDownloader.GetLatestVersion(Xabe.FFmpeg.Downloader.FFmpegVersion.Official);
            }
            catch(Exception ex)
            {
                LogMessage($"[BİLGİ] FFmpeg halihazırda mevcut veya indirilemedi (varsayılana düşülecektir). {ex.Message}");
            }

            UpdateStatus("İndirme işlemi sürüyor (Çoklu Bağlantı Aktif)...");
            LogMessage($"[BİLGİ] Ana Arşive {selectedItems.Count} adet parça indiriliyor...");

            // Multi-threading Limiti (Aynı Anda Sadece 3 Tane İner)
            var semaphore = new System.Threading.SemaphoreSlim(3);

            await Task.Run(() =>
            {
                var tasks = selectedItems.Select(async item =>
                {
                    await semaphore.WaitAsync();
                    try
                    {
                        string originalTitle = item.Title;
                        Dispatcher.Invoke(() => item.Title = $"⏳ [İniyor] {originalTitle}");

                        LogMessage($"[>] İndirme Başladı: {originalTitle}");
                        var streamManifest = await youtube.Videos.Streams.GetManifestAsync(item.VideoId);

                        var audioStreamInfo = streamManifest.GetAudioOnlyStreams()
                                                            .Where(s => s.Container == YoutubeExplode.Videos.Streams.Container.Mp4)
                                                            .GetWithHighestBitrate();

                        if (audioStreamInfo == null) 
                        {
                            audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
                        }

                        if (audioStreamInfo != null)
                        {
                            string safeTitle = string.Join("_", originalTitle.Split(Path.GetInvalidFileNameChars()));
                            string rawAuthor = string.IsNullOrWhiteSpace(item.Author) ? "Bilinmeyen Sanatçı" : item.Author;
                            string safeAuthor = string.Join("_", rawAuthor.Split(Path.GetInvalidFileNameChars()));

                            // Akıllı Otomatik Klasörleme (Sanatçı Adına Göre)
                            string artistFolder = Path.Combine(baseArchiveFolder, safeAuthor);
                            Directory.CreateDirectory(artistFolder);

                            string extension = audioStreamInfo.Container == YoutubeExplode.Videos.Streams.Container.Mp4 ? "m4a" : audioStreamInfo.Container.Name; 
                            string rawFilePath = Path.Combine(artistFolder, $"raw_{safeTitle}.{extension}");
                            string finalFilePath = Path.Combine(artistFolder, $"{safeTitle}.{extension}");

                            // 1. YouTube'dan Raw İndirme
                            await youtube.Videos.Streams.DownloadAsync(audioStreamInfo, rawFilePath, new Progress<double>(p =>
                            {
                                Dispatcher.Invoke(() => 
                                {
                                    item.IsDownloading = Visibility.Visible;
                                    item.ProgressValue = p * 100;
                                });
                            }));

                            // 2. FFmpeg ile Ses Düzeyi Sabitleme (Loudnorm)
                            try
                            {
                                Dispatcher.Invoke(() => item.Title = $"⚙️ [Düzenleniyor] {originalTitle}");
                                LogMessage($"[FFmpeg] {safeTitle} Ses Seviyesi dengeleniyor (Normalizer)...");

                                var mediaInfo = await Xabe.FFmpeg.FFmpeg.GetMediaInfo(rawFilePath);
                                var audioStream = mediaInfo.AudioStreams.FirstOrDefault();

                                var conversion = Xabe.FFmpeg.FFmpeg.Conversions.New()
                                    .AddStream(audioStream)
                                    .AddParameter("-af loudnorm=I=-14:LRA=11:TP=-1.5") // Çoğu radyo ve teyp için yüksek ve pürüzsüz ayar
                                    .SetOutput(finalFilePath);

                                await conversion.Start();
                                File.Delete(rawFilePath); // Ham dosyayı siliyoruz
                            }
                            catch(Exception ffEx)
                            {
                                LogMessage($"[UYARI] FFmpeg çalıştırılamadı, orijinal ses kalacak ({ffEx.Message})");
                                // Başarısız olursa raw dosyayı final yap
                                if(File.Exists(rawFilePath))
                                    File.Move(rawFilePath, finalFilePath, true);
                            }

                            // 3. ID3 TAG GÖMME VE ALBÜM RESMİ EKLENTİSİ
                            try
                            {
                                LogMessage($"[ID3 Tag] {safeTitle} Kapak Resmi/Sanatçı İşleniyor...");

                                string tempThumbPath = Path.Combine(artistFolder, $"temp_{safeTitle}.jpg");
                                if (!string.IsNullOrEmpty(item.ThumbnailUrl))
                                {
                                    using (var httpClient = new HttpClient())
                                    {
                                        var imgBytes = await httpClient.GetByteArrayAsync(item.ThumbnailUrl);
                                        File.WriteAllBytes(tempThumbPath, imgBytes);
                                    }
                                }

                                using (var tagFile = TagLib.File.Create(finalFilePath))
                                {
                                    tagFile.Tag.Title = originalTitle;
                                    tagFile.Tag.Performers = new string[] { rawAuthor };

                                    if (File.Exists(tempThumbPath))
                                    {
                                        var picture = new TagLib.Picture(tempThumbPath)
                                        {
                                            Type = TagLib.PictureType.FrontCover,
                                            Description = "Kapak Resmi",
                                            MimeType = System.Net.Mime.MediaTypeNames.Image.Jpeg
                                        };
                                        tagFile.Tag.Pictures = new TagLib.IPicture[] { picture };
                                    }
                                    tagFile.Save();
                                }
                                if (File.Exists(tempThumbPath)) File.Delete(tempThumbPath);
                            }
                            catch (Exception tEx)
                            {
                                LogMessage($"[UYARI] {safeTitle} ID3 Tag yazılırken sorun oluştu: {tEx.Message}");
                            }

                            Dispatcher.Invoke(() => 
                            { 
                                item.Title = $"✅ [Tamamlandı] {originalTitle}";
                                item.IsDownloading = Visibility.Collapsed;
                            });
                            LogMessage($"[BİLGİ] BİTTİ: {safeTitle}.{extension} (Sanatçı: {safeAuthor})");

                            currentUsbMusicFiles.Add(safeTitle);
                        }
                    }
                    catch (Exception ex)
                    {
                        Dispatcher.Invoke(() => item.Title = $"❌ [HATA] Yüklenemedi");
                        LogMessage($"[HATA] İndirilemedi ({item.Title}): {ex.Message}");
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                });

                Task.WhenAll(tasks).Wait();
            });

            UpdateStatus("İndirme İşlemi Tamamlandı!");
            LogMessage($"[BAŞARILI] {selectedItems.Count} Müzik arşive indirildi, FFmpeg ile dengelendi, USB'ye aktarılmaya hazır.");

            await Task.Delay(2000);
            Dispatcher.Invoke(() => 
            {
                searchResults.Clear();
                UpdateStatus("Hazır (Liste Temizlendi)");
            });
            LogMessage("[BİLGİ] Liste otomatik olarak boşaltıldı.");
        }

        // 3. USB'YE EŞİTLE (SYNC) BUTONU
        private async void SyncBtn_Click(object sender, RoutedEventArgs e)
        {
            if (UsbComboBox.SelectedItem == null)
            {
                UpdateStatus("Lütfen bir hedef USB seçin!", true);
                return;
            }

            string selectedUsbStr = UsbComboBox.SelectedItem.ToString();
            string targetDriveLetter = selectedUsbStr.Substring(0, 3); // "D:\", "E:\" gibi...

            if (!Directory.Exists(baseArchiveFolder))
            {
                UpdateStatus("Kopyalanacak arşiv bulunamadı!", true);
                return;
            }

            UpdateStatus("🚀 USB'ye Kopyalanıyor, Lütfen Bekleyin...");
            LogMessage($"[BİLGİ] Veriler {targetDriveLetter} sürücüsüne senkronize ediliyor...");

            await Task.Run(() =>
            {
                try
                {
                    CopyDirectory(baseArchiveFolder, targetDriveLetter, true);
                }
                catch (Exception ex)
                {
                    LogMessage($"[HATA] Eşitleme başarısız: {ex.Message}");
                }
            });

            UpdateStatus("✅ Tüm Dosyalar Başarıyla USB Belleğe Aktarıldı!");
            LogMessage($"[BAŞARILI] İşlem tamamlandı. Teybe takıp dinleyebilirsiniz.");
        }

        // Senkronizasyon
        private void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
        {
            var dir = new DirectoryInfo(sourceDir);
            DirectoryInfo[] dirs = dir.GetDirectories();
            Directory.CreateDirectory(destinationDir);

            foreach (FileInfo file in dir.GetFiles())
            {
                try
                {
                    string targetFilePath = Path.Combine(destinationDir, file.Name);
                    file.CopyTo(targetFilePath, true);
                }
                catch (Exception fileEx)
                {
                    LogMessage($"[UYARI] '{file.Name}' kopyalanamadı: {fileEx.Message}");
                }
            }

            if (recursive)
            {
                foreach (DirectoryInfo subDir in dirs)
                {
                    string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
                    CopyDirectory(subDir.FullName, newDestinationDir, true);
                }
            }
        }

        // Canlı Log Sistemi
        private void LogMessage(string message)
        {
            Dispatcher.Invoke(() =>
            {
                string time = DateTime.Now.ToString("HH:mm:ss");
                LogListBox.Items.Add($"[{time}] {message}");
                LogListBox.SelectedIndex = LogListBox.Items.Count - 1;
                LogListBox.ScrollIntoView(LogListBox.SelectedItem);
            });
        }

        // Başlık Çubuğu ve Pencere İşlemleri
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed) DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}