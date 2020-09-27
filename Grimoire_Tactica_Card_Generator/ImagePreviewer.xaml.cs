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
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Grimoire_Tactica_Card_Generator
{
    /// <summary>
    /// Interaction logic for ImagePreviewer.xaml
    /// </summary>
    public partial class ImagePreviewer : Window
    {
        public ImagePreviewer(string Image_Path)
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));
            //make an image from the path that was passed
            if (File.Exists(Image_Path))
            {
                BitmapImage i;
                using(Bitmap b = new Bitmap(Image_Path))
                {
                    using (MemoryStream mem = new MemoryStream())
                    {
                        //save the bitmap to the stream for use of the image
                        b.Save(mem, System.Drawing.Imaging.ImageFormat.Bmp);
                        mem.Position = 0;
                        i = new BitmapImage();
                        i.BeginInit();
                        i.StreamSource = mem;
                        i.CacheOption = BitmapCacheOption.OnLoad;
                        i.EndInit();                        
                    }
                }
                IMG_Whole_Image.Source = i;
            }
        }
    }
}
