using System.Configuration;
using System.Data;
using System.Windows;
using System;
using System.IO;

namespace MuzikUSBYoneticisi
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegExe = Path.Combine(currentDir, "ffmpeg.exe");

            // EĞER SİSTEME ÖZEL FFMPEG MOTORU KURULMADIYSA, İLK SETUP AŞAMASINI AÇ:
            if (!File.Exists(ffmpegExe))
            {
                SetupWindow setup = new SetupWindow();
                setup.Show();
            }
            else // ZATEN KURULUYSA, GEÇİCİ SETUP EKRANINI ATLAMA VE DİREKT ANA UYGULAMAYA GİRİŞ YAP
            {
                MainWindow main = new MainWindow();
                main.Show();
            }
        }
    }
}
