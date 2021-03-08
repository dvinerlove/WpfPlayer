using Microsoft.Win32;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.Entity;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using WpfPlayer.Models;
using WpfPlayer.Services;
using YouTubeSearch;

namespace WpfPlayer
{
    public class BoolToVisibilityConverter : System.Windows.Data.IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    public partial class MainWindow : Window
    {
        OpenFileDialog ofd = new OpenFileDialog();
        WaveOutEvent player;
        private MediaPlayer mediaPlayer = new MediaPlayer();
        bool isPlaying = false;
        WaveStream mainOutputStream;
        WaveChannel32 volumeStream;
        DispatcherTimer timerVideoTime = new DispatcherTimer();
        int selectedPlaylistId = 0;
        int volume = 50;
        string selectedPath;
        private int selectedAudioId;
        bool searchEnable = false;
        public object lb_item = null;
        string selectedUrl;
        ProgressBarOpen progressBar;
        string sort = "None";
        private string downloadFolder;
        public ObservableCollection<Playlist> pl1 { get; } = new ObservableCollection<Playlist>();
        private Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
        public MainWindow()
        {
            InitializeComponent();
            UpdateListbox();
            DataContext = this;
            Loaded += (sender, e) => MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
            this.DataContext = this;
            playlistList.PreviewMouseWheel += (sender, e) =>
            {
                var eventArg = new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta);
                eventArg.RoutedEvent = UIElement.MouseWheelEvent;
                eventArg.Source = sender;
                playlistList.RaiseEvent(eventArg);
            };
            SearchTextBox.Text = "Быстрый поиск";
            SearchTextBox.Foreground = new SolidColorBrush(Colors.LightGray);
            readTxt();
            selectPlaylist();
        }
        public void UpdateListbox(int playlistId = -1)
        {
            try
            {
                Playlist playlist;
                using (Context db = new Context())
                {
                    var playlists = from Playlist in db.Playlists
                                    select new
                                    {
                                        Name = Playlist.Name,
                                        Image = Playlist.Image,
                                        id = Playlist.id,
                                        AudioTime = Playlist.AudioTime,
                                        AudioLIst = Playlist.AudioList
                                    };
                    if (playlists.ToList().Count() == 0 || playlists == null)
                    {
                        playlist = new Playlist
                        {
                            Name = "Новый плейлист",
                            AudioList = GetAudiosList(),
                            Image = null
                        };
                        db.Playlists.Add(playlist);
                        db.SaveChanges();
                    } 
                    playlistList.ItemsSource = playlists.ToList();
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId();
                    dispatcher.Invoke(() => pl1.Clear());
                    foreach (var item in playlists)
                    {
                        playlist = new Playlist
                        {
                            Name = item.Name,
                            AudioList = null,
                            Image = item.Image,
                            AudioTime = item.AudioTime,
                            id = item.id
                        };
                        dispatcher.Invoke(() => pl1.Add(playlist));
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
            playlistList.SelectedIndex = 0;
        }
        private Playlist GetPlaylist()
        {
            using (Context db = new Context())
            {
                var r = from Playlist in db.Playlists
                        select new
                        {
                            Name = Playlist.Name,
                            Image = Playlist.Image,
                            id = Playlist.id,
                            AudioTime = Playlist.AudioTime,
                            AudioLIst = Playlist.AudioList
                        };
                return (Playlist)r;
            }
        }
        System.Windows.Media.Imaging.BitmapImage currentImage;
        string GetCorrectTime(long ms)
        {
            TimeSpan t = TimeSpan.FromMilliseconds(ms);
            return string.Format("{0:D2}:{1:D2}",
                                    t.Minutes,
                                    t.Seconds);
        }
        private void PlayMp3(string path)
        {
            if (isPlaying)
            {
                player.Stop();
                player.Dispose();
            } 
            using (var FD = TagLib.File.Create(path))
            {
                if (mainOutputStream!=null)
                {
                    mainOutputStream.Dispose();

                }
                player = new WaveOutEvent();
                if (!File.Exists(path))
                {
                }
                else
                {
                    mainOutputStream = WaveFormatConversionStream.CreatePcmStream(new Mp3FileReader(path)); //new Mp3FileReader(path);
                    volumeStream = new WaveChannel32(mainOutputStream);
                    player.Init(volumeStream);
                    trackSlider.Maximum = volumeStream.Length;
                    string curTimeString = mainOutputStream.TotalTime.ToString("mm\\:ss");
                    time2.Text = curTimeString;
                    {
                        using (var db = new Context())
                        {
                            var a = db.Audios.Where(a => a.id == selectedAudioId).FirstOrDefault();
                            a.lastPlay = DateTime.Now;
                            a.countPlay = a.countPlay + 1;
                            db.SaveChanges();
                            trackPerformer.Text = a.Singer;
                            trackName.Text = a.Title;
                            try
                            {
                                trackCover.Source = PlaylistNameDialog.LoadImage(FD.Tag.Pictures[0].Data.Data);
                            }
                            catch
                            {
                                string directoryName = Path.GetDirectoryName(path);
                                DirectoryInfo di = new DirectoryInfo(directoryName);
                                string firstFileName = di.GetFiles().Select(fi => fi.Name).Where(x => x.Substring(x.Length - 3) == "jpg" || x.Substring(x.Length - 3) == "png" || x.Substring(x.Length - 3) == "jpeg").FirstOrDefault();
                                var img = new System.Windows.Media.Imaging.BitmapImage(new Uri(directoryName + @"\" + firstFileName, UriKind.Relative));
                                if (img != null && !string.IsNullOrEmpty(firstFileName))
                                {
                                    directoryName = directoryName + @"\" + firstFileName;
                                    currentImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@directoryName, UriKind.RelativeOrAbsolute));
                                    trackCover.Source = currentImage;
                                }
                                else
                                {
                                    currentImage = new System.Windows.Media.Imaging.BitmapImage(new Uri(@"404.png", UriKind.RelativeOrAbsolute));
                                    trackCover.Source = currentImage;
                                }
                            }
                        }
                    }
                    player.Play();
                    isPlaying = true;
                    trackSlider.Value = 0;
                    volumeSlider.Value = volume;
                }
                FD.Dispose(); 
            }
        }
        public void OpenMp3(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                TagLib.File FD = TagLib.File.Create(path);
                string title;
                if (!string.IsNullOrEmpty(FD.Tag.Title))
                {
                    title = FD.Tag.Title;
                }
                else
                {
                    title = Path.GetFileName(path).Substring(0, Path.GetFileName(path).Length - 4);
                }
                using (Context db = new Context())
                {
                    using (Mp3FileReader reader = new Mp3FileReader(path))
                    {
                        Audio audio = new Audio
                        {
                            Title = title,
                            DirectoryName = path,
                            Album = FD.Tag.Album,
                            Duration = reader.TotalTime,
                            countPlay = 0,
                            isFavorite = false,
                            lastPlay = DateTime.Now,
                            Singer = FD.Tag.FirstPerformer,
                        };
                        trackName.Text = audio.Title;
                        trackPerformer.Text = audio.Singer;
                        db.Audios.Add(audio);
                        var playlist22 = db.Playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault();
                        audio.Playlist.Add(playlist22);
                        db.SaveChanges();
                        UpdateAudios();
                    }
                }
            }
        }
        public void OpenMp3(string[] path)
        {
            if (path.Length > 0)
            {
                for (int i = 0; i < path.Length; i++)
                {
                    TagLib.File FD = TagLib.File.Create(path[i]);
                    string title;
                    if (!string.IsNullOrEmpty(FD.Tag.Title))
                    {
                        title = FD.Tag.Title;
                    }
                    else
                    {
                        title = Path.GetFileName(path[i]).Substring(0, Path.GetFileName(path[i]).Length - 4);
                    }
                    using (Context db = new Context())
                    {
                        using (Mp3FileReader reader = new Mp3FileReader(path[i]))
                        {
                            Audio audio = new Audio
                            {
                                Title = title,
                                DirectoryName = path[i],
                                Album = FD.Tag.Album,
                                Duration = reader.TotalTime,
                                countPlay = 0,
                                isFavorite = false,
                                lastPlay = DateTime.Now,
                                Singer = FD.Tag.FirstPerformer,
                            };
                            trackName.Text = audio.Title;
                            trackPerformer.Text = audio.Singer;
                            db.Audios.Add(audio);
                            var playlist22 = db.Playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault();
                            audio.Playlist.Add(playlist22);
                            db.SaveChanges();
                            UpdateAudios();
                        }
                    }
                }
            }
        }
        public void UpdateAudios()
        {
            audiosListBox.ItemsSource = null;
            switch (sort)
            {
                case "NameAsc":
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId().OrderBy(p => p.Title);
                    break;
                case "NameDesc":
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId().OrderByDescending(p => p.Title);
                    break;
                case "CountAsc":
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId().OrderBy(p => p.countPlay);
                    break;
                case "CountDesc":
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId().OrderByDescending(p => p.countPlay);
                    break;
                default:
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId();
                    break;
            }
        }
        public List<Audio> GetAudiosList()
        {
            List<Audio> audios = new List<Audio>();
            foreach (var item in audiosListBox.Items)
            {
                audios.Add(item as Audio);
            }
            return audios;
        }
        private bool RepoveDuplicate(ListBox listBox)
        {
            for (int Row = 0; Row <= listBox.Items.Count - 2; Row++)
            {
                for (int RowAgain = listBox.Items.Count - 1; RowAgain >= Row + 1;
                    RowAgain += -1)
                {
                    if (((listBox as ListBox).Items[Row] as Audio).DirectoryName == ((listBox as ListBox).Items[RowAgain] as Audio).DirectoryName)
                    {
                        listBox.Items.RemoveAt(RowAgain);
                        return true;
                    }
                }
            }
            return false;
        }
        private void OpenFile(object sender, RoutedEventArgs e)
        {
            ofd.Multiselect = true;
            ofd.AddExtension = true;
            ofd.DefaultExt = "*.*";
            ofd.Filter = "All files (*.mp3; ) | *.mp3; ";
            ofd.ShowDialog();
            string[] fn = ofd.FileNames;
            OpenMp3(fn);
        }
        public void ReadTextFile(string link)
        {
            WebClient client = new WebClient();
            Stream stream = client.OpenRead(link);
            StreamReader reader = new StreamReader(stream);
            String content = reader.ReadToEnd();
            MessageBox.Show(content);
        }
        private void StartStop(object sender, RoutedEventArgs e)
        {
            if (player != null)
            {
                isPlaying = !isPlaying;
                if (!isPlaying)
                    player.Pause();
                else player.Play();
            }
        }
        private void CommandBinding_CanExecute_1(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }
        private void CommandBinding_Executed_1(object sender, ExecutedRoutedEventArgs e)
        {
        }
        private void CommandBinding_Executed_2(object sender, ExecutedRoutedEventArgs e)
        {
        }
        private void CommandBinding_Executed_3(object sender, ExecutedRoutedEventArgs e)
        {
        }
        private void zalupa_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectAudio();
        }
        private int SelectAudio(/*bool i = false*/)
        {
            try
            {
                Audio lbi = (audiosListBox.SelectedItem as Audio);
                if (lbi != null)
                {
                    int id = (Int32)lbi.id;
                    using (var db = new Context())
                    {
                        selectedPath = db.Audios.Where(c => c.id == id).ToList<Audio>().FirstOrDefault().DirectoryName;
                        selectedAudioId = db.Audios.Where(c => c.id == id).ToList<Audio>().FirstOrDefault().id;
                        return id;
                    }
                }
            }
            catch
            {
                return 0;
            }
            return 0;
        }
        private static bool IsMouseOverTarget(Visual target, Point point)
        {
            var bounds = VisualTreeHelper.GetDescendantBounds(target);
            return bounds.Contains(point);
        }
        private void zalupa_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ListBox lst = sender as ListBox;
            int index = -1;
            for (int i = 0; i < audiosListBox.Items.Count; i++)
            {
                var lbi = audiosListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (lbi == null) continue;
                if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                {
                    index = i;
                    break;
                }
            }
            lst.SelectedIndex = index;
            if (index < 0) return;
            DragItem drag_item = new DragItem(lst, index, lst.Items[index]);
            DragDrop.DoDragDrop(lst, drag_item, DragDropEffects.Move);
        }
        private void zalupa_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (selectedPath != "")
            {
                PlayMp3(selectedPath);
            }
        }
        private void searchBtn_Click(object sender, RoutedEventArgs e)
        {
            YouTubeSearch(searchString.Text);
        }
        async void YouTubeSearch(string querystring)
        {
            AudioS audioSearch;
            int querypages = 1;
            VideoSearch videos = new VideoSearch();
            var items = await videos.GetVideos(querystring, querypages);
            searchList.Items.Clear();
            foreach (var item in items)
            {
                audioSearch = new AudioS(item.getAuthor(), item.getTitle().Trim(), item.getDescription(), item.getDuration(), item.getUrl(), item.getThumbnail());
                searchList.Items.Add(audioSearch);
            }
        }
        private void Download_Click(object sender, RoutedEventArgs e)
        {
            selectedUrl = (sender as System.Windows.Controls.Button).Tag.ToString();
            progressBar = new ProgressBarOpen(selectedUrl, downloadFolder);
            progressBar.Owner = null;
            progressBar.Show();
            progressBar.Closing += ProgressBar_Closed;
        }
        private void ProgressBar_Closed(object sender, EventArgs e)
        {
            OpenMp3(progressBar.OutputFileName);
        }
        private void searchList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AudioS lbi = ((sender as ListBox).SelectedItem as AudioS);
            selectedUrl = lbi.Url;
        }
        private void CloseSearch_Click(object sender, RoutedEventArgs e)
        {
            gridSearch.Visibility = Visibility.Collapsed;
            grid1.Visibility = Visibility.Visible;
        }
        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volumeStream != null)
            {
                if (volumeSlider.Value >= 00) { volumeStream.Volume = 0.0f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 10) { volumeStream.Volume = 0.1f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 20) { volumeStream.Volume = 0.2f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 30) { volumeStream.Volume = 0.3f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 40) { volumeStream.Volume = 0.4f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 50) { volumeStream.Volume = 0.5f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 60) { volumeStream.Volume = 0.6f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 70) { volumeStream.Volume = 0.7f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 80) { volumeStream.Volume = 0.8f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 90) { volumeStream.Volume = 0.9f; volume = (int)volumeSlider.Value; }
                if (volumeSlider.Value >= 95) { volumeStream.Volume = 1.0f; volume = (int)volumeSlider.Value; }
            }
        }
        private void trackSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (volumeStream != null)
            {
                volumeStream.Position = Convert.ToInt32(trackSlider.Value);
            }
        }
        void timer_Tick(object sender, EventArgs e)
        {
            if (volumeStream != null && mainOutputStream != null)
            {
                if (volumeStream.Length > 0)
                {
                    trackSlider.Value = volumeStream.Position;
                    string curTimeString = mainOutputStream.CurrentTime.ToString("mm\\:ss");
                    time1.Text = curTimeString;
                }
                if (volumeStream.Position == volumeStream.Length && isPlaying)
                {
                    NextTrack();
                }
            }
            else
            {
                trackSlider.Value = 0;
            }
        }
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            timerVideoTime.Interval = TimeSpan.FromSeconds(0.1);
            timerVideoTime.Tick += new EventHandler(timer_Tick);
            timerVideoTime.Start();
            selectPlaylist();
        }
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            grid1.Visibility = Visibility.Collapsed;
            gridSearch.Visibility = Visibility.Visible;
        }
        private void NewPlaylist_Click_1(object sender, RoutedEventArgs e)
        {
            Playlist playlist;
            string playlistName;
            var dialog = new PlaylistNameDialog();
            if (dialog.ShowDialog() == true)
            {
                playlistName = dialog.ResponseText;
                var image = dialog.GetImageByteArray;
                playlist = new Playlist { Name = playlistName, AudioList = null, Image = image };//(playlistName, audios1, image);
                using (Context db = new Context())
                {
                    db.Playlists.Add(playlist);
                    db.SaveChanges();
                    UpdateListbox();
                }
            }
        }
        private void playlistList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectPlaylist();
        }
        private void selectPlaylist()
        {
            //dynamic b = playlistList.Items[0];
            //selectedPlaylistId = b.id;
            try
            {
                object selectedItem = playlistList.SelectedItem;
                ListBoxItem selectedListBoxItem = playlistList.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListBoxItem;
                if (selectedListBoxItem != null)
                {
                    System.Reflection.PropertyInfo pi = selectedListBoxItem.Content.GetType().GetProperty("id");
                    if (pi != null)
                    {
                        Int32 id = (Int32)(pi.GetValue(selectedListBoxItem.Content, null));
                        selectedPlaylistId = id;
                        UpdateAudios();
                    }
                }
            }
            catch
            {
              //  UpdateAudios();
            }
        }
        public ICollection<Audio> GetFavAudios()
        {
            using (Context db = new Context())
            {
                return db.Audios.Where(x => x.isFavorite == true).Distinct().ToList();
            }
        }
        public ICollection<Audio> GetLastAudios()
        {
            using (Context db = new Context())
            {
                return db.Audios.Where(x => DbFunctions.TruncateTime(x.lastPlay).Value.Month == DateTime.Now.Month && DateTime.Now.Day - DbFunctions.TruncateTime(x.lastPlay).Value.Day < 7 || DbFunctions.TruncateTime(x.lastPlay).Value.Month == DateTime.Now.Month - 1 && DateTime.Now.Day + 30 - DbFunctions.TruncateTime(x.lastPlay).Value.Day < 7).Distinct().ToList();
            }
        }
        public ICollection<Audio> GetAudiosByPlaylistId()
        {
            using (Context db = new Context())
            {
                var playlists = from Playlist in db.Playlists
                                select new
                                {
                                    id = Playlist.id,
                                    list = Playlist.AudioList,
                                    name = Playlist.Name,
                                    img = Playlist.Image
                                };
                if (playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault() != null)
                {
                    playlistName.Text = playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().name;
                    playlistImage.Source = PlaylistNameDialog.LoadImage(playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().img);
                    TimeSpan tmp = TimeSpan.Zero;
                    foreach (var i in playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list)
                    {
                        tmp += i.Duration;
                    }
                    playlistInfo.Text = tmp.ToString(@"mm\:ss") + " | " + playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count().ToString();
                    if (Math.Abs(playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count()) % 10 == 1)
                    {
                        playlistInfo.Text += " трек";
                    }
                    else if
                       (Math.Abs(playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count()) % 10 >= 2 && Math.Abs(playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count()) % 10 < 5)
                    {
                        playlistInfo.Text += " трекa";
                    }
                    else if
                       (Math.Abs(playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count()) % 10 >= 5)
                    {
                        playlistInfo.Text += " треков";
                    }
                    if (playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list.Count() == 0)
                    {
                        playlistInfo.Text += " треков";
                    }
                    return playlists.Where(c => c.id == selectedPlaylistId).FirstOrDefault().list;
                }
                else
                {
                    ICollection<Audio> audios = null;
                    return audios;
                }
            }
        }
        void s()
        {
            using (Context db = new Context())
            {
                var a = db.Audios.Where(x => x.Title == "gavno").FirstOrDefault();
            }
        }
        private void DeleteEvent_Click(object sender, RoutedEventArgs e)
        {
            using (Context db = new Context())
            {
                int id = SelectAudio();
                if (id != 0)
                {
                    Audio p = db.Audios.Where(a => a.id == id).FirstOrDefault();
                    db.Audios.Remove(p);
                    db.SaveChanges();
                    UpdateAudios();
                }
            }
        }
        private void Replace_Click(object sender, RoutedEventArgs e)
        {
            int s = int.Parse((sender as MenuItem).Tag.ToString());
            using (Context db = new Context())
            {
                var aaa = db.Audios.Where(x => x.id == selectedAudioId).FirstOrDefault();
                var playlist22 = db.Playlists.Where(c => c.id == s).FirstOrDefault();
                aaa.Playlist.Add(playlist22);
                db.SaveChanges();
                UpdateAudios();
            }
        }
        private void Prev_Click(object sender, RoutedEventArgs e)
        {
            if (audiosListBox.SelectedIndex > 0)
            {
                audiosListBox.SelectedIndex = audiosListBox.SelectedIndex - 1;
                SelectAudio();
                if (selectedPath != "")
                {
                    PlayMp3(selectedPath);
                }
                selectedPath = "";
            }
        }
        private void Next_Click(object sender, RoutedEventArgs e)
        {
            NextTrack();
        }
        void NextTrack()
        {
            if (audiosListBox.SelectedIndex < audiosListBox.Items.Count - 1)
            {
                audiosListBox.SelectedIndex = audiosListBox.SelectedIndex + 1;
                SelectAudio();
                if (selectedPath != "")
                {
                    PlayMp3(selectedPath);
                }
                selectedPath = "";
            }
        }
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
        }
        private void Panel_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                OpenMp3(files);
            }
        }
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new TagEditor();
            using (TagLib.File FD = TagLib.File.Create(selectedPath))
            {
                using (Context db = new Context())
                {
                    dialog.Id = SelectAudio();
                    var a = db.Audios.Where(c => c.id == selectedAudioId).ToList<Audio>().FirstOrDefault();
                    dialog.TrackTitle = a.Title;
                    dialog.Author = a.Singer;
                    dialog.Album = a.Album;
                    dialog.Genre = FD.Tag.FirstGenre;
                    dialog.Year = FD.Tag.Year.ToString();
                    dialog.counter.Text = a.countPlay.ToString();
                    try
                    {
                        dialog.Image = PlaylistNameDialog.LoadImage(FD.Tag.Pictures[0].Data.Data);
                    }
                    catch
                    {
                        dialog.Image = currentImage;
                    }
                    dialog.Path = selectedPath;
                    FD.Dispose();
                }
            }
            
            dialog.Show();
            dialog.Closed += Dialog_Closed;
        }
        private void Dialog_Closed(object sender, EventArgs e)
        {
            UpdateListbox();
            UpdateAudios();
        }
        private void Btn1_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FullScreenImage();
            dialog.ImgSource = this.trackCover.Source;
            dialog.ShowDialog();
        }
        private void DeletePlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            using (Context db = new Context())
            {
                int id = selectedPlaylistId;
                if (id != 0)
                {
                    var playlists = db.Playlists;
                    try
                    {
                        Playlist p = db.Playlists.Where(a => a.id == selectedPlaylistId).FirstOrDefault();
                        db.Playlists.Remove(p);
                        db.SaveChanges();
                    }
                    catch
                    {
                        if (selectedPlaylistId == 1)
                        {
                            dynamic b = playlistList.SelectedItem;
                            string str = b.Name;
                            var a = db.Playlists.Where(a => a.Name == str).FirstOrDefault();
                            db.Playlists.Remove(a);
                            db.SaveChanges();
                        }
                    }
                    UpdateListbox();
                    UpdateAudios();
                }
            }
        }
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "Быстрый поиск")
            {
                SearchTextBox.Text = "";
                SearchTextBox.Foreground = new SolidColorBrush(Colors.Black);
                searchEnable = true;
            }
        }
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (SearchTextBox.Text == "")
            {
                SearchTextBox.Text = "Быстрый поиск";
                SearchTextBox.Foreground = new SolidColorBrush(Colors.LightGray);
                searchEnable = false;
            }
        }
        public void Ses(string query)
        {
            query = query.ToLower();
            try
            {
                if (searchEnable && query != "")
                {
                    Int32 id;
                    object selectedItem = playlistList.SelectedItem;
                    ListBoxItem selectedListBoxItem = playlistList.ItemContainerGenerator.ContainerFromItem(selectedItem) as ListBoxItem;
                    if (selectedListBoxItem != null)
                    {
                    }
                    else
                    {
                        using (var db = new Context())
                        {
                            id = db.Playlists.FirstOrDefault().id;
                        }
                    }
                    if (selectedListBoxItem != null)
                    {
                        System.Reflection.PropertyInfo pi = selectedListBoxItem.Content.GetType().GetProperty("Id");
                        if (pi != null)
                        {
                            id = (Int32)(pi.GetValue(selectedListBoxItem.Content, null));
                        }
                    }
                    audiosListBox.ItemsSource = null;
                    audiosListBox.ItemsSource = GetAudiosByPlaylistId().Where(s => s.Title.ToLower().Contains(query.ToLower())).ToList();
                    if (audiosListBox.Items.Count == 0)
                    {
                        UpdateAudios();
                    }
                }
                else UpdateAudios();
            }
            catch
            {
            }
        }
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            Ses(SearchTextBox.Text);
        }
        private void LineLeftButton_Click(object sender, RoutedEventArgs e)
        {
            if (playlistList.SelectedIndex - 1 >= 0)
            {
                playlistList.SelectedIndex = playlistList.SelectedIndex - 1;
            }
            selectPlaylist();

            //dynamic b = playlistList.SelectedItem;
            //selectedPlaylistId = b.id;
            //if (true)
            //{
            //    UpdateAudios();
            //}
        }
        private void LineRightButton_Click(object sender, RoutedEventArgs e)
        {
            if (playlistList.SelectedIndex + 1 < playlistList.Items.Count)
            {
                playlistList.SelectedIndex = playlistList.SelectedIndex + 1;
            }
            selectPlaylist();
            //dynamic b = playlistList.SelectedItem;
            //selectedPlaylistId = b.id;
            //if (true)
            //{
            //    UpdateAudios();
            //}
        }
        private void ItemsPresenter_MouseDown(object sender, MouseButtonEventArgs e)
        {
        }
        private void MenuItem_Click_1(object sender, RoutedEventArgs e)
        {
        }
        private void MenuItem_Click_2(object sender, RoutedEventArgs e)
        {
        }
        private void Play_Click(object sender, RoutedEventArgs e)
        {
            if (selectedPath != "")
            {
                PlayMp3(selectedPath);
            }
        }
        private void Like_Click(object sender, RoutedEventArgs e)
        {
            using (Context db = new Context())
            {
                var a = db.Audios.Where(a => a.id == selectedAudioId).FirstOrDefault();
                if (a.isFavorite)
                {
                    a.isFavorite = false;
                }
                else
                {
                    a.isFavorite = true;
                }
                db.SaveChanges();
                UpdateAudios();
            }
        }
        private void OpenPath_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Explorer.exe", "/select," + selectedPath);
        }
        public class DragItem
        {
            public ListBox Client;
            public int Index;
            public object Item;
            public DragItem(ListBox client, int index, object item)
            {
                Client = client;
                Index = index;
                Item = item;
            }
        }
        private void audiosListBox_DragLeave(object sender, DragEventArgs e)
        {
            ListBox lb = sender as ListBox;
            lb_item = lb.SelectedItem;
            lb.Items.Remove(lb.SelectedItem);
        }
        private void audiosListBox_DragEnter(object sender, DragEventArgs e)
        {
            ListBox lst = sender as ListBox;
            if (!e.Data.GetDataPresent(typeof(DragItem)))
            {
                e.Effects = DragDropEffects.None;
            }
            else if ((e.AllowedEffects & DragDropEffects.Move) == 0)
            {
                e.Effects = DragDropEffects.None;
            }
            else
            {
                DragItem drag_item = (DragItem)e.Data.GetData(typeof(DragItem));
                if (drag_item.Client != lst)
                {
                    e.Effects = DragDropEffects.None;
                }
                else
                {
                    e.Effects = DragDropEffects.Move;
                }
            }
        }
        private void audiosListBox_DragOver(object sender, DragEventArgs e)
        {
            if (e.Effects != DragDropEffects.Move) return;
            ListBox lst = sender as ListBox;
            int index = -1;
            for (int i = 0; i < audiosListBox.Items.Count; i++)
            {
                var lbi = audiosListBox.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                if (lbi == null) continue;
                if (IsMouseOverTarget(lbi, e.GetPosition((IInputElement)lbi)))
                {
                    index = i;
                    break;
                }
            }
            lst.SelectedIndex = index;
        }
        private void favBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistList.SelectedIndex = -1;
            audiosListBox.ItemsSource = null;
            audiosListBox.ItemsSource = GetFavAudios();
        }
        private void lastPlayBtn_Click(object sender, RoutedEventArgs e)
        {
            playlistList.SelectedIndex = -1;
            audiosListBox.ItemsSource = null;
            audiosListBox.ItemsSource = GetLastAudios();
        }

        private void MenuItem_Click_3(object sender, RoutedEventArgs e)
        {
            if (sort != "NameAsc")
            {
                sort = "NameAsc";
            }
            else
            {
                sort = "NameDesc";
            }
            UpdateAudios();
        }
        private void MenuItem_Click_4(object sender, RoutedEventArgs e)
        {
            if (sort == "CountDesc" || sort.Contains("Name"))
            {
                sort = "CountAsc";
            }
            else
            {
                sort = "CountDesc";
            }
            UpdateAudios();
        }
        void readTxt()
        {
            if (File.Exists("folder.txt"))
            {
                downloadFolder = File.ReadAllText("folder.txt");
            }
        }
        private void chooseBtn_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                var result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    System.Windows.Forms.MessageBox.Show(dialog.SelectedPath);
                    File.Delete("folder.txt");
                    File.WriteAllText("folder.txt", dialog.SelectedPath);
                    downloadFolder = File.ReadAllText("folder.txt");
                }
            }
        }
        private void AddMenu_Click_5(object sender, RoutedEventArgs e)
        {
        }
        private void favSecBtn_Click(object sender, RoutedEventArgs e)
        {
            if (favStack.Visibility == Visibility.Visible)
            {
                favStack.Visibility = Visibility.Hidden;
            }
            else
            {
                favStack.Visibility = Visibility.Visible;
            }
        }
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            var dialog = new FullScreenImage();
            dialog.ImgSource = this.playlistImage.Source;
            dialog.ShowDialog();
        }
        private void EditPlaylistBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new PlaylistNameDialog();
            dialog.ResponseTextBox.Text = playlistName.Text;
            dialog.ImageBox.Source = playlistImage.Source;
            if (dialog.ShowDialog() == true)
            {
                using (Context db = new Context())
                {
                    var a = db.Playlists.Where(x => x.id == selectedPlaylistId).FirstOrDefault();
                    a.Image = dialog.GetImageByteArray;
                    a.Name = dialog.ResponseText;
                    db.SaveChanges();
                    UpdateListbox();
                }
            }
        }
        private void Btnclose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private void Btnmax_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
            }
            else
            {
                this.WindowState = WindowState.Maximized;
            }
        }
        private void Btnmin_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        private void grid1_MouseDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                DragMove();
            }
            catch
            {
            }
        }
    }
}
