using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;

namespace ShoopDoup
{
    /// <summary>
    /// Interaction logic for SceneController.xaml
    /// </summary>
    public partial class SceneController : Page
    {
        public MainWindow parentController;

        public SceneController()
        {
            InitializeComponent();
        }

        public virtual void updateSkeleton(SkeletonData skeleton) { }

        public virtual void updateWithoutSkeleton() { }

        public BitmapImage toBitmapImage(System.Drawing.Bitmap bitmap)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }
    }
}
