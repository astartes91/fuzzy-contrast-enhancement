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

            Bitmap grayScaleBitmap = null;
            BitmapImage grayBitmapImage = GetGrayscaleImage(sourceBitmapImage, out grayScaleBitmap);
            GrayImage.Source = grayBitmapImage;

            EnhancedImage.Source = GetEnhancedImage(grayScaleBitmap);
        }

        private BitmapImage GetEnhancedImage(Bitmap grayscaleBitmap)
        {
            int max = int.MinValue, min = int.MaxValue;
            int width = grayscaleBitmap.Width, height = grayscaleBitmap.Height;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color originalColor = grayscaleBitmap.GetPixel(i, j);

                    int redValue = originalColor.R;

                    if (redValue > max)
                    {
                        max = redValue;
                    }
                    if (redValue < min)
                    {
                        min = redValue;
                    }
                }
            }

            double fuzzyExponent = FuzzyExponentSlider.Value;
            double crossover = min + ((max-min+1)/2);
            double fuzzyDenominator = (max - crossover) / (Math.Pow(2, (1 / fuzzyExponent)) - 1);
            //double fuzzyDenominator = (max - crossover) / (Math.Pow(0.5, (1 / fuzzyExponent)));
            //double fuzzyDenominator = (max - crossover)/(Math.Pow(0.5, (-1/fuzzyExponent)) - 1);
            //double fuzzyDenominator = (max - crossover) / (Math.Exp(-1 * Math.Log(0.5) / fuzzyExponent) - 1);

            /********************************** Fuzzification **************************************/
            double[,] membershipValuesMatrix = new double[width, height];
            //double[,] fuzzyDenominatorValuesMatrix = new double[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Color originalColor = grayscaleBitmap.GetPixel(i, j);

                    int redValue = originalColor.R;

                    /*fuzzyDenominatorValuesMatrix[i, j] = (max - redValue) / (Math.Pow(2, (1 / fuzzyExponent)) - 1);
                    fuzzyDenominator = fuzzyDenominatorValuesMatrix[i, j];*/

                    /*membershipValuesMatrix[i, j] = 1/(Math.Pow((1 + ((max - redValue)/fuzzyDenominator)), 
                        fuzzyExponent));*/
                    membershipValuesMatrix[i, j] = (Math.Pow((1 + ((max - redValue) / fuzzyDenominator)),
                        -1 * fuzzyExponent));
                }
            }
            /********************************** End of Fuzzification *********************************/

            /*************************************** Intensification *****************************************/
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    if (membershipValuesMatrix[i, j] <= 0.5)
                    {
                        membershipValuesMatrix[i, j] = 2 * Math.Pow(membershipValuesMatrix[i, j], 2);
                    }
                    else
                    {
                        membershipValuesMatrix[i, j] = 1 - 2 * (Math.Pow(1 - membershipValuesMatrix[i, j], 2));
                    }
                }
            }
            /************************************* End of Intensification ************************************/

            /*********************************** Defuzzification **************************************/
            Bitmap enhancedBitmap = new Bitmap(width, height);
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    //fuzzyDenominator = fuzzyDenominatorValuesMatrix[i, j];
                    int color = (int) (max - (fuzzyDenominator * ((Math.Pow(membershipValuesMatrix[i, j], 
                        (-1/fuzzyExponent))) - 1)));

                    /*int color = (int)(fuzzyDenominator + max - (fuzzyDenominator * (Math.Pow(membershipValuesMatrix[i, j],
                        (-1 / fuzzyExponent)))));*/

                    //if (color < 0) color *= -1;

                    Color newColor = Color.FromArgb(color, color, color);

                    //set the new image's pixel
                    enhancedBitmap.SetPixel(i, j, newColor);
                }
            }
            /********************************** End of Defuzzification *********************************/

            IntPtr hBitmap = enhancedBitmap.GetHbitmap();
            BitmapImage enhancedBitmapImage = new BitmapImage();

            try
            {
                BitmapSource enhancedBitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                             hBitmap,
                             IntPtr.Zero,
                             Int32Rect.Empty,
                             BitmapSizeOptions.FromEmptyOptions());

                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                MemoryStream memoryStream = new MemoryStream();

                encoder.Frames.Add(BitmapFrame.Create(enhancedBitmapSource));
                encoder.Save(memoryStream);

                enhancedBitmapImage.BeginInit();
                enhancedBitmapImage.StreamSource = new MemoryStream(memoryStream.ToArray());
                enhancedBitmapImage.EndInit();

                memoryStream.Close();
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return enhancedBitmapImage;
        }

        private BitmapImage GetGrayscaleImage(BitmapImage source, out Bitmap grayScaleBitmap)
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
                    int grayScale = (int)((originalColor.R * 0.299) + (originalColor.G * 0.587)
                        + (originalColor.B * 0.114));

                    //create the color object
                    Color newColor = Color.FromArgb(grayScale, grayScale, grayScale);

                    //set the new image's pixel to the grayscale version
                    grayscaleBitmap.SetPixel(i, j, newColor);
                }
            }

            grayScaleBitmap = grayscaleBitmap;

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
