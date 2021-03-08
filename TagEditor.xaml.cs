using Microsoft.Win32;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using WpfPlayer.Services;

namespace WpfPlayer
{
    /// <summary>
    /// Логика взаимодействия для TagEditor.xaml
    /// </summary>
    public partial class TagEditor : Window
    {
        public string TrackTitle
        {
            get { return TrackTitleTextbox.Text.Trim(); }
            set { if (value != null) TrackTitleTextbox.Text = value.Trim(); }
        }
        int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
        public string Author
        {
            get { return AuthorTextbox.Text.Trim(); }
            set { if (value != null) AuthorTextbox.Text = value.Trim(); }
        }
        public string Album
        {
            get { return AlbumTextbox.Text.Trim(); }
            set { if (value != null) AlbumTextbox.Text = value.Trim(); }
        }
        public string Genre
        {
            get { return GanreTextbox.Text.Trim(); }
            set
            {
                if (value != null)

                    GanreTextbox.Text = value.Trim();
            }
        }
        public string Year
        {
            get { return YearTextbox.Text.Trim(); }
            set
            {
                if (value != null)

                    YearTextbox.Text = value.Trim();

            }
        }
        public string Path
        {
            get { return PathTextbox.Text.Trim(); }
            set { if (value != null) PathTextbox.Text = value.Trim(); }
        }
        public ImageSource Image
        {
            get { return ImageBox.Source; }
            set { if (value != null) ImageBox.Source = value; }
        }
        bool isSaved = false;
        public bool IsSaved
        {
            get { return isSaved; }
        }
        public TagEditor()
        {
            InitializeComponent();
        }
        string ImagePath = "";
        public void Button_Click(object sender, RoutedEventArgs e)
        {
            using (Context db = new Context())
            {

                using (TagLib.File FD = TagLib.File.Create(Path))
                {

                    int audioId = Id;
                    var audio = db.Audios.Where(a => a.id == audioId).FirstOrDefault();
                    ///*audio.Title = */
                    FD.Tag.Title = TrackTitle;
                    audio.Title = TrackTitle;
                    FD.Tag.Performers = new string[] { Author };
                    audio.Singer = Author;
                    FD.Tag.Album = Album;
                    audio.Album = Album;
                    //FD.Tag.Genres = new string[] { Ganre };
                    // FD.Tag.Year = Year;
                    ///*audio.Singer = */FD.Tag.Performers[0] = Author;
                    ///*audio.Album = */FD.Tag.Album = Album;
                    FD.Tag.Genres = new string[] { Genre };

                    try
                    {
                        FD.Tag.Year = Convert.ToUInt32(Year);
                    }
                    catch
                    {
                    }
                    var i = ImageBox.Source as BitmapImage;
                    //(YourImage.Source as BitmapImage).UriSource
                    
                    try
                    {
                        if (ImagePath != "")
                        {

                            TagLib.Picture artwork = new TagLib.Picture(ImagePath);
                            artwork.Type = TagLib.PictureType.FrontCover;
                            FD.Tag.Pictures = new TagLib.IPicture[] { artwork };
                        }
                    }
                    catch
                    {

                    }


                    //FD.Tag.Pictures[0]  = PlaylistNameDialog.BufferFromImage(i);

                    try
                    {

                        FD.Save();
                        FD.Dispose();
                        db.SaveChanges();
                        MessageBox.Show("saved");
                        this.Close();

                    }
                    catch(Exception ex)
                    {

                        MessageBox.Show("Файл занят другим процессом\n"+ex.ToString());
                    }

                    ////playlistList.Items.Add(playlist.Name);
                    //  audio.
                    //db.Audios.Add(audio);

                }
            }


        }

        private void PicButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();

            ofd.Multiselect = true;
            ofd.AddExtension = true;
            ofd.DefaultExt = "*.*";
            ofd.Filter = "All files (*.jpg; *.jpeg; *.png;  ) | *.jpg; *.jpeg; *.png; ";
            ofd.ShowDialog();
            if (ofd.FileName != "")
            {
                ImagePath = ofd.FileName;
                BitmapImage bimage = new BitmapImage();
                bimage.BeginInit();
                bimage.UriSource = new Uri(ImagePath, UriKind.Relative);
                bimage.EndInit();
                ImageBox.Source = new BitmapImage(new Uri(
                            ImagePath));
            }
            //  this.Title = ImagePath;
            //MessageBox.Show("");
        }

        private void openFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start("Explorer.exe", "/select," + PathTextbox.Text);
        }
    }
}
