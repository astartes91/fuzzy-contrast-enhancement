using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Color = System.Drawing.Color;

namespace FuzzyContrastEnhancement
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ChooseFileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog();
            bool? result = dialog.ShowDialog();

            BitmapImage sourceBitmapImage = new BitmapImage(new Uri(dialog.FileName));
            SourceImage.Source = sourceBitmapImage;

            BitmapImage grayBitmapImage = GetGrayscaleImage(sourceBitmapImage);
            GrayImage.Source = grayBitmapImage;
        }

        private BitmapImage GetGrayscaleImage(BitmapImage source)
        {
            Bitmap sourceBitmap = null;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(source));
                enc.Save(outStream);
                sourceBitmap = new Bitmap(outStream);
            }

            Bitmap grayscaleBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            for (int i = 0; i < sourceBitmap.Width; i++)
            {
                for (int j = 0; j < sourceBitmap.Height; j++)
                {
                    //get the pixel from the original image
                    Color originalColor = sourceBitmap.GetPixel(i, j);

                    //create the grayscale version of the pixel
                    int grayScale = (int)((originalColor.R * .3) + (originalColor.G * .59)
                        + (originalColor.B * .11));

                    //create the color object
                    Color newColor = Color.FromArgb(grayScale, grayScale, grayScale);

                    //set the new image's pixel to the grayscale version
                    grayscaleBitmap.SetPixel(i, j, newColor);
                }
            }

            IntPtr hBitmap = grayscaleBitmap.GetHbitmap();
            BitmapImage grayScaleBitmapImage = new BitmapImage();

            try
            {
                BitmapSource grayScaleBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());

                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                MemoryStream memoryStream = new MemoryStream();

                encoder.Frames.Add(BitmapFrame.Create(grayScaleBitmapSource));
                encoder.Save(memoryStream);

                grayScaleBitmapImage.BeginInit();
                grayScaleBitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                grayScaleBitmapImage.EndInit();

                memoryStream.Close();
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return grayScaleBitmapImage;
        }
    }
}
