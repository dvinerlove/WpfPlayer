using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace WpfPlayer
{
    /// <summary>
    /// Логика взаимодействия для ProgressBarOpen.xaml
    /// </summary>
    public partial class ProgressBarOpen : Window
    {
        public ProgressBarOpen(string selectedUrl,string downloadFolder)
        {
            SelectedUrl = selectedUrl;
            DownloadFolder = downloadFolder;
            //this.Owner = this;
            
            //Form1.Instance = this;
            InitializeComponent();
            StartAsync();
        }
        public string SelectedUrl { get; private set; }
        public string DownloadFolder { get; private set; }
        public string OutputFileName { get; private set; }

        async void StartAsync()
        {

            await Task.Run(() => DownloadAudioAsync());

        }

        public async Task DownloadAudioAsync()
        { 

            var source = @DownloadFolder + @"\";
            var youtube = YouTube.Default;
            var vid = await youtube.GetVideoAsync(SelectedUrl);


            System.IO.File.WriteAllBytes(source + vid.FullName, vid.GetBytes());
            var inputFile = new MediaFile { Filename = source + vid.FullName };

            var outputFile = new MediaFile { Filename = $"{source + vid.FullName.Substring(0, vid.FullName.Length - 4)}.mp3" };

            using (var engine = new Engine())
            {
                engine.GetMetadata(inputFile);
                engine.Convert(inputFile, outputFile);
                System.IO.File.Delete(source + vid.FullName);
                 
               // MessageBox.Show("Сохранено");
                OutputFileName = outputFile.Filename;
                //Environment.Exit(0);
                //this.Close();
                Dispatcher.Invoke(() => CloseThis());


            }


        }
        void CloseThis()
        {
            this.Close();
        }

    }
}
