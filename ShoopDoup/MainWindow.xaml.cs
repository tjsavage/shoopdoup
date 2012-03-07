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
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using ShoopDoup.ViewControllers;
using NetGame;
using NetGame.Utils;
using NetGame.Speech;
using ShoopDoup;
using ShoopDoup.Models;

namespace ShoopDoup
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
        public SceneController currentController;
        MinigameFactory minigameFactory;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            SetupKinect();
            minigameFactory = new MinigameFactory();
            minigameFactory.mainController = this;
            currentController = new CarStopperController(minigameFactory.getMinigameOfType(MINIGAME_TYPE.Association));//new StandbyController(); // new WhackAMoleController(minigameFactory.getMinigameOfType(Models.MINIGAME_TYPE.Association)); 
            currentController.ControllerFinished += switchMinigame;
            currentController.parentController = this;
            this.Content = currentController;


        }

        public void controllerFinished()
        {
            Console.WriteLine("A controller finished.");

            //Minigame newGame = minigameFactory.getDefaultMinigame();
            //currentController = newGame.getController();
            //currentController.parentController = this;
            //this.Content = newGame.getController();
            //newGame.start();
        }

        public void switchMinigame(object o, EventArgs e)
        {
            Console.WriteLine("Switching now!");
            currentController = new StandbyController();
            this.Content = currentController;
        }

        private void SetupKinect()
        {
            minigameFactory = new MinigameFactory();

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
                //nui.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(nui_SkeletonFrameReady);
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

            SkeletonFrame allSkeletons= e.SkeletonFrame;

            //get the first tracked skeleton
            SkeletonData skeleton = (from s in allSkeletons.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();


            if (skeleton != null)
            {
                currentController.updateSkeleton(skeleton);
            }
            else
            {
                currentController.updateWithoutSkeleton();
            }
        }



        private void Window_Closed(object sender, EventArgs e)
        {
            //Cleanup
            nui.Uninitialize();
        }
    }
}
