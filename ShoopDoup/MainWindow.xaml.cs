/////////////////////////////////////////////////////////////////////////
//
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// This code is licensed under the terms of the Microsoft Kinect for
// Windows SDK (Beta) License Agreement:
// http://kinectforwindows.org/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////

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

namespace SkeletalTracking
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        //Kinect Runtime
        Runtime nui;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetupKinect();

        }

        private void SetupKinect()
        {
            if (Runtime.Kinects.Count == 0)
            {
                this.Title = "No Kinect connected";
            }
            else
            {
                //use first Kinect
                nui = Runtime.Kinects[0];

                //Initialize to do skeletal tracking
                nui.Initialize(RuntimeOptions.UseSkeletalTracking);

                //add event to receive skeleton data
                nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);

                //to experiment, toggle TransformSmooth between true & false
                // parameters used to smooth the skeleton data
                nui.SkeletonEngine.TransformSmooth = true;
                TransformSmoothParameters parameters = new TransformSmoothParameters();
                parameters.Smoothing = 0.2f;
                parameters.Correction = 0.3f;
                parameters.Prediction = 0.2f;
                parameters.JitterRadius = 1.0f;
                parameters.MaxDeviationRadius = 0.5f;
                nui.SkeletonEngine.SmoothParameters = parameters;

            }
        }

        void nui_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            SkeletonFrame allSkeletons = e.SkeletonFrame;

            //get the first tracked skeleton
            SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();


            if (skeleton != null)
            {
                //set position
                SetEllipsePosition(headEllipse, skeleton.Joints[JointID.Head]);
                SetEllipsePosition(leftEllipse, skeleton.Joints[JointID.HandLeft]);
                SetEllipsePosition(rightEllipse, skeleton.Joints[JointID.HandRight]);
            }
        }

        private void SetEllipsePosition(FrameworkElement ellipse, Joint joint)
        {
            var scaledJoint = joint.ScaleTo(640, 480, .5f, .5f);

            Canvas.SetLeft(ellipse, scaledJoint.Position.X);
            Canvas.SetTop(ellipse, scaledJoint.Position.Y);

        }



        private void Window_Closed(object sender, EventArgs e)
        {
            //Cleanup
            nui.Uninitialize();
        }
    }
}
