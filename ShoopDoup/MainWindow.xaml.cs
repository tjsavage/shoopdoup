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
        List<Type> minigameControllers;
        Random randomGenerator = new Random();

        System.Windows.Threading.DispatcherTimer instructionDisplayTimer;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            instructionDisplayTimer = new System.Windows.Threading.DispatcherTimer();
            instructionDisplayTimer.Interval = TimeSpan.FromMilliseconds(3000);
            instructionDisplayTimer.Tick += SwitchController;
            instructionDisplayTimer.IsEnabled = false;

            minigameFactory = new MinigameFactory();
            minigameFactory.mainController = this;

            currentController = new NetGameController(null);
            currentController.ControllerFinished += switchMinigame;
            currentController.parentController = this;
            this.Content = currentController;

            minigameControllers = new List<Type>();
            minigameControllers.Add(typeof(CarStopperController));
            minigameControllers.Add(typeof(PopTheBubblesController));
            minigameControllers.Add(typeof(NetGameController));
            SetupKinect();


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
            if (currentController is StandbyController)
            {
                int randomControllerIndex = randomGenerator.Next(minigameControllers.Count);
                Minigame minigameToSwitchTo = minigameFactory.getMinigameOfType(MINIGAME_TYPE.Association);

                ((StandbyController)currentController).setInstructionText(minigameToSwitchTo.getDescription());
                instructionDisplayTimer.Start();
                
                switch (randomControllerIndex)
                {
                    case 0:
                        currentController = new CarStopperController(minigameToSwitchTo);
                        break;
                    case 1:
                        currentController = new PopTheBubblesController(minigameToSwitchTo);
                        break;
                    case 2:
                        currentController = new NetGameController(minigameToSwitchTo);
                        break;
                }

                //currentController = //new CarStopperController(minigameFactory.getMinigameOfType(MINIGAME_TYPE.Association));

            }
            else
            {
                currentController = new StandbyController();
                this.Content = currentController;
            }

            currentController.ControllerFinished += switchMinigame;

        }

        private void SwitchController(object o, EventArgs e)
        {
            instructionDisplayTimer.Stop();
            this.Content = currentController;
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


            if (currentController != null)
            {
                if (skeleton != null)
                {
                    currentController.updateSkeleton(skeleton);
                }
                else
                {
                    currentController.updateWithoutSkeleton();
                }
            }
        }



        private void Window_Closed(object sender, EventArgs e)
        {
            //Cleanup
            nui.Uninitialize();
        }
    }
}
