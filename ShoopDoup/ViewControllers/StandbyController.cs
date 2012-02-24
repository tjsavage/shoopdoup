using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;


namespace ShoopDoup.ViewControllers
{
    class StandbyController : SceneController
    {
        private System.Windows.Controls.Image welcomeSleepImage;
        private System.Windows.Controls.Image welcomeAttentionImage;
        private System.Windows.Controls.Image welcomeFollowingImageImage;

        public StandbyController()
        {
            welcomeSleepImage = new Image();
            welcomeSleepImage.Width = 200;

            // Create source
            BitmapImage myBitmapImage = new BitmapImage();

            // BitmapImage.UriSource must be in a BeginInit/EndInit block
            myBitmapImage.BeginInit();
            myBitmapImage.UriSource = new Uri(@"../Assets/WelcomeSleep.png");
            myBitmapImage.DecodePixelWidth = 200;
            myBitmapImage.EndInit();
            //set image source
            welcomeSleepImage.Source = myBitmapImage;

        }

        public override void updateSkeleton(SkeletonData skeleton)
        {

        }

        public override void updateWithoutSkeleton()
        {
            Grid.SetColumn(welcomeSleepImage, 1);
            mainGrid.Children.Add(welcomeSleepImage);
        }
    }
}
