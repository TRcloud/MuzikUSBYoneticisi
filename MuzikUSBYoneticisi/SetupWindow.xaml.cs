using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Xabe.FFmpeg.Downloader;
using System.Windows.Media;

namespace MuzikUSBYoneticisi
{
    public partial class SetupWindow : Window
    {
        public SetupWindow()
        {
            InitializeComponent();
            this.Loaded += SetupWindow_Loaded;
        }

        private async void SetupWindow_Loaded(object sender, RoutedEventArgs e)
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;

            try
            {
                // FFmpeg indiricisinin Xabe parametresi -> İlerleme çubuğuna aktar
                var progress = new Progress<Xabe.FFmpeg.ProgressInfo>(p =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        long total = p.TotalBytes;
                        long downloaded = p.DownloadedBytes;

                        if (total > 0)
                        {
                            double percentage = (double)downloaded / total * 100;
                            DownloadProgressBar.Value = percentage;

                            double dlMb = Math.Round((double)downloaded / 1048576, 1);
                            double totalMb = Math.Round((double)total / 1048576, 1);

                            ProgressDetailsText.Text = $"İndiriliyor: {dlMb} MB / {totalMb} MB";
                            PercentageText.Text = $"%{Math.Round(percentage, 0)}";
                        }
                        else
                        {
                            double dlMb = Math.Round((double)downloaded / 1048576, 1);
                            ProgressDetailsText.Text = $"İndiriliyor: {dlMb} MB (Boyut Hesaplanıyor...)";
                        }
                    });
                });

                // Asıl indirme metodunu tetikler
                await FFmpegDownloader.GetLatestVersion(FFmpegVersion.Official, currentDir, progress);

                // Başarılı olursa tetiklenecek görsel güncellemeler
                Dispatcher.Invoke(() => 
                {
                    TitleText.Text = "Kurulum Tamamlandı";
                    StatusText.Text = "Uygulama başarıyla başlatılıyor...";
                    StatusText.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129));
                    DownloadProgressBar.Value = 100;
                    PercentageText.Text = "%100";
                });

                await Task.Delay(1500); // Kullanıcının başarıyı 1 sn görmesi için

                // Kurulum bittiyse Ana Uygulama Formunu Başlat
                MainWindow main = new MainWindow();
                Application.Current.MainWindow = main;
                main.Show();

                // Setup ekranını arka planda öldür
                this.Close();
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => 
                {
                    MessageBox.Show($"Kurulum sırasında bir hata oluştu:\n{ex.Message}\n\nLütfen uygulamanın kurulu olduğu klasör izinlerini kontrol edin veya yazılımı yönetici olarak başlatın.", "Kurulum Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                    Application.Current.Shutdown();
                });
            }
        }
    }
}