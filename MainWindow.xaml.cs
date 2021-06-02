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
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


namespace SprayPaintImage
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ImageBrush brush;
        MainViewModel viewModel;
        public MainWindow()
        {
            InitializeComponent();

            //int width = (int)this.paintSurface.Width;
            //int height = (int)this.paintSurface.Height;
            Init();


        }

        private void Init()
        {
            viewModel = new MainViewModel(this.paintSurface);
            this.DataContext = viewModel;
            System.Drawing.Color color = new System.Drawing.Color();
            System.Windows.Media.Color selectedColor = System.Windows.Media.Color.FromArgb(color.A, color.R, color.G, color.B);
            this.colorPicker.SelectedColor = selectedColor;
        }





        private void Open_File(object sender, RoutedEventArgs e)
        {
           
           viewModel.Delete();
            this.paintSurface.Strokes.Clear();
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".png";
            dlg.Filter = "Image Files(*.BMP;*.JPG;*.GIF)|*.BMP;*.JPG;*.GIF|All files (*.*)|*.*";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                this.paintSurface.Strokes.Clear();
                string filename = dlg.FileName;
                Bitmap currentBitmp = new Bitmap(filename);
                this.paintSurface.Width = currentBitmp.Width;
                this.paintSurface.Height = currentBitmp.Height;
                brush = new ImageBrush();
                brush.ImageSource = new BitmapImage(new Uri(filename, UriKind.Relative));
                this.paintSurface.Background = brush;
            }
        }

       

        private void Save_File(object sender, RoutedEventArgs e)
        {
            FileInfo file = null;
            if (!ShowSaveFileDialog(out file))
                return;
            int width = (int)this.paintSurface.Width;
            int height = (int)this.paintSurface.Height;

            if(width < 0 || height < 0)
            {
                MessageBox.Show("Please load an image");
                return;
            }

            RenderTargetBitmap rtb = new RenderTargetBitmap(width, height, 96d, 96d, PixelFormats.Default);
            rtb.Render(this.paintSurface);            
            Save(file, rtb);
        }

       

        public static bool ShowSaveFileDialog(out FileInfo file)
        {
            // Initializing Dialog:
            Microsoft.Win32.SaveFileDialog dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "Bitmap|*.bmp|PNG Image|*.png|JPEG Image|*.jpg|GIF Image|*.gif|TIFF Image|*.tif";
            dialog.AddExtension = true;

            // Checking if a File was selected:
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                file = new FileInfo(dialog.FileName);
                return true;
            }
            else
            {
                file = null;
                return false;
            }
        }

        public static void Save(FileInfo file, BitmapSource image)
        {
            try
            {
                // Selecting a Way to save the Image depending on it's Format:
                switch (file.Extension.ToLower())
                {
                    case ".bmp": SaveBitmap(file, image, new BmpBitmapEncoder()); break;
                    case ".png": SaveBitmap(file, image, new PngBitmapEncoder()); break;
                    case ".jpg":
                    case ".jpeg": SaveBitmap(file, image, new JpegBitmapEncoder()); break;
                    case ".gif": SaveBitmap(file, image, new GifBitmapEncoder()); break;
                    case ".tif": SaveBitmap(file, image, new TiffBitmapEncoder()); break;
                    default:
                        throw new ArgumentException("");
                }
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }
        }

        private static void SaveBitmap(FileInfo file, BitmapSource image, BitmapEncoder encoder)
        {
            encoder.Frames.Add(BitmapFrame.Create(image));
            FileStream output = File.Open(file.FullName, FileMode.OpenOrCreate, FileAccess.Write);
            encoder.Save(output);
            output.Close();
        }

        private void Undo(object sender, RoutedEventArgs e)
        {
            viewModel.Undo();
        }

        private void Redo(object sender, RoutedEventArgs e)
        {
            viewModel.Redo();
        }
    }
}
