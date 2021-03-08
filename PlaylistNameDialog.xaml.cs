using Microsoft.Win32;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace WpfPlayer
{
    /// <summary>
    /// Логика взаимодействия для PlaylistNameDialog.xaml
    /// </summary>
    public partial class PlaylistNameDialog : Window
    {
        public PlaylistNameDialog()
        {
            InitializeComponent();
        }
        public string ResponseText
        {
            get { return ResponseTextBox.Text; }
            set { ResponseTextBox.Text = value; }
        }

        public byte[] GetImageByteArray
        {
            get { return BufferFromImage((ImageBox.Source as BitmapImage)); }
            set { ImageBox.Source = LoadImage(value); }
        }

        public static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0) return null;
            var image = new BitmapImage();
            using (var mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }


        public byte[] ImageToByteArray(System.Drawing.Image imageIn)
        {
            using (var ms = new MemoryStream())
            {
                imageIn.Save(ms, imageIn.RawFormat);
                return ms.ToArray();
            }
        }

        private System.Drawing.Image ImageWpfToGDI(System.Windows.Media.ImageSource image)
        {
            MemoryStream ms = new MemoryStream();
            var encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image as System.Windows.Media.Imaging.BitmapSource));
            encoder.Save(ms);
            ms.Flush();
            return System.Drawing.Image.FromStream(ms);
        }

        public byte[] BufferFromImage(BitmapImage imageSource)
        {
            return ImageToByteArray(compressImage(ImageWpfToGDI(ImageBox.Source), 500, 500, 50));
        }

        private System.Drawing.Image compressImage(System.Drawing.Image image, int newWidth, int newHeight, int newQuality)
        {
            using (System.Drawing.Image memImage = new Bitmap(image, newWidth, newHeight))
            {
                ImageCodecInfo myImageCodecInfo;
                System.Drawing.Imaging.Encoder myEncoder;
                EncoderParameter myEncoderParameter;
                EncoderParameters myEncoderParameters;
                myImageCodecInfo = GetEncoderInfo("image/jpeg");
                myEncoder = System.Drawing.Imaging.Encoder.Quality;
                myEncoderParameters = new EncoderParameters(1);
                myEncoderParameter = new EncoderParameter(myEncoder, newQuality);
                myEncoderParameters.Param[0] = myEncoderParameter;
                MemoryStream memStream = new MemoryStream();
                memImage.Save(memStream, myImageCodecInfo, myEncoderParameters);
                System.Drawing.Image newImage = System.Drawing.Image.FromStream(memStream);
                ImageAttributes imageAttributes = new ImageAttributes();
                using (Graphics g = Graphics.FromImage(newImage))
                {
                    g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                    g.DrawImage(newImage, new System.Drawing.Rectangle(System.Drawing.Point.Empty, newImage.Size), 0, 0,
                      newImage.Width, newImage.Height, GraphicsUnit.Pixel, imageAttributes);
                }
                return newImage;
            }
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo ici in encoders)
                if (ici.MimeType == mimeType) return ici;
            return null;
        }


        private void OKButton_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        string ImagePath = "";
        private void imgBtn_Click(object sender, RoutedEventArgs e)
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
        }
    }
}
