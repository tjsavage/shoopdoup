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
        private Page parentController;

        public SceneController()
        {
            InitializeComponent();
        }

        public virtual void updateSkeleton(SkeletonData skeleton)
        {

        }
    }
}
