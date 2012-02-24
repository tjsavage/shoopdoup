using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;


namespace ShoopDoup.ViewControllers
{
    class StandbyController : SceneController
    {
        private Line myLine;

        public StandbyController()
        {
            // Create a StackPanel to contain the shape.
            myLine = new Line();
            myLine.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            myLine.X1 = 1;
            myLine.X2 = 50;
            myLine.Y1 = 1;
            myLine.Y2 = 50;
            myLine.StrokeThickness = 5;
            mainGrid.Children.Add(myLine);
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {
            Console.WriteLine("Here!");

            myLine.X1 = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.X;
            myLine.X2 = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X;
            myLine.Y1 = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y;
            myLine.Y2 = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y;
        }
    }
}
