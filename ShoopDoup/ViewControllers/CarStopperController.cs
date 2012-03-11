﻿using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using ShoopDoup.Models;
using System.Drawing.Drawing2D;


namespace ShoopDoup.ViewControllers
{
    class CarStopperController : SceneController
    {
        private enum GAME_STATE {Intro, Instructions, Playing, Exit};

        struct Car
        {
            public Label carLabel;
            public System.Windows.Controls.Image carImage;

            public Car(Label label, System.Windows.Controls.Image image)
            {
                carLabel = label;
                carImage = image;
            }
        };

        private List<Bitmap> carBitmaps;

        private GAME_STATE state;

        private Minigame minigame;
        private String introTitle;
        private String introDescription;

        private String instructions = "Stop cars if they are associated with the item at the top of the screen.\nTo stop, hover over an item and push your hand forward.";

        private Label introTitleLabel;
        private Label introDescriptionLabel;
        private Label instructionLabel;
        private Label exitLabel;
        private Label scoreLabel;

        private Dictionary<String, Car> carDictionary;

        private System.Windows.Threading.DispatcherTimer transitionTimer;
        private System.Windows.Threading.DispatcherTimer fadeTimer;
        private System.Windows.Threading.DispatcherTimer carTimer;
        private System.Windows.Threading.DispatcherTimer exitTimer;
        private System.Windows.Threading.DispatcherTimer userExitedTimer;
        private System.Windows.Threading.DispatcherTimer introCarAnimatorTimer;

        private System.Windows.Threading.DispatcherTimer introCarTimer;
        private int numIntroCarTimerTicks = 0;

        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;

        private int numFaderTicks = 0;

        private int carsStopped = 0;

        private double highlightedHandBaseDepth;
        private double depthDeltaForSelection = .3;

        private Label carLabel;
        private BitmapImage carBitmapImage;
        private System.Windows.Controls.Image carImage;
        private System.Windows.Controls.Image trafficBackgroundImage;
        private int currentcarDataIndex = 0;

        private Random randomGen = new Random();

        private String baseItem;
        private Label baseLabel;

        private System.Windows.Shapes.Rectangle[,] grid = new System.Windows.Shapes.Rectangle[3,3];

        private int[] laneHeights = {125, 250, 375, 500, 625};


        public CarStopperController(Minigame game)
        {
            state = GAME_STATE.Intro;
            minigame = game;
            introTitle = minigame.getTitle();
            introDescription = minigame.getDescription();

            carDictionary = new Dictionary<String, Car>();

            Console.WriteLine(minigame.getData().Count);
            int randomElementIndex = randomGen.Next(minigame.getData().Count);
            baseItem = (String)minigame.getData().ElementAt(randomElementIndex).getElementValue();
            minigame.getData().RemoveAt(randomElementIndex);

            setupLabels();

            mainCanvas.Children.Add(introTitleLabel);
            mainCanvas.Children.Add(introDescriptionLabel);
            mainCanvas.Children.Add(instructionLabel);
            mainCanvas.Children.Add(exitLabel);
            mainCanvas.Children.Add(scoreLabel);
            mainCanvas.Children.Add(baseLabel);

            this.transitionTimer = new System.Windows.Threading.DispatcherTimer();
            this.transitionTimer.Tick += moveToNextState;
            this.transitionTimer.Interval = TimeSpan.FromMilliseconds(2000);
            this.transitionTimer.Start();

            this.fadeTimer = new System.Windows.Threading.DispatcherTimer();
            this.fadeTimer.Tick += fadeToNextState;
            this.fadeTimer.Interval = TimeSpan.FromMilliseconds(40);
            this.fadeTimer.IsEnabled = false;

            this.carTimer = new System.Windows.Threading.DispatcherTimer();
            this.carTimer.Tick += moveCars;
            this.carTimer.Interval = TimeSpan.FromMilliseconds(20);

            this.exitTimer = new System.Windows.Threading.DispatcherTimer();
            this.exitTimer.Tick += controllerFinished;
            this.exitTimer.Interval = TimeSpan.FromMilliseconds(6000);

            this.userExitedTimer = new System.Windows.Threading.DispatcherTimer();
            this.userExitedTimer.Tick += controllerFinished;
            this.userExitedTimer.Interval = TimeSpan.FromMilliseconds(2000);

            this.introCarTimer = new System.Windows.Threading.DispatcherTimer();
            this.introCarTimer.Tick += addNewInstructionCar;
            this.introCarTimer.Interval = TimeSpan.FromMilliseconds(2600);

            this.introCarAnimatorTimer = new System.Windows.Threading.DispatcherTimer();
            this.introCarAnimatorTimer.Tick += moveCars;
            this.introCarAnimatorTimer.Interval = TimeSpan.FromMilliseconds(20);

            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            trafficBackgroundImage = new System.Windows.Controls.Image();
            trafficBackgroundImage.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.TrafficLaneBackGround);
            trafficBackgroundImage.Width = 1280;
            trafficBackgroundImage.Height = 800;
            trafficBackgroundImage.Opacity = 0;

            rightHandCursor.Opacity = 0;
            leftHandCursor.Opacity = 0;

            carBitmaps = new List<Bitmap>();
            carBitmaps.Add(ShoopDoup.Properties.Resources.CarStopperBus);
            carBitmaps.Add(ShoopDoup.Properties.Resources.CarStopperCar);
            carBitmaps.Add(ShoopDoup.Properties.Resources.CarStopperTruck);
            carBitmaps.Add(ShoopDoup.Properties.Resources.CarStopperMotorcycle);
            carBitmaps.Add(ShoopDoup.Properties.Resources.CarStopperSUV);

            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
            mainCanvas.Children.Add(trafficBackgroundImage);

            Canvas.SetZIndex(rightHandCursor, 4);
            Canvas.SetZIndex(leftHandCursor, 4);
            Canvas.SetLeft(trafficBackgroundImage, 0);
            Canvas.SetTop(trafficBackgroundImage, 0);
            Canvas.SetZIndex(trafficBackgroundImage, 0);
        }

        private void setupLabels()
        {
            introTitleLabel = new Label();
            introDescriptionLabel = new Label();
            instructionLabel = new Label();
            exitLabel = new Label();
            scoreLabel = new Label();
            baseLabel = new Label();

            introTitleLabel.Content = introTitle;
            introTitleLabel.FontSize = 40;
            Canvas.SetLeft(introTitleLabel, 300);
            Canvas.SetTop(introTitleLabel, 100);

            introDescriptionLabel.Content = new TextBlock();
            ((TextBlock)(introDescriptionLabel.Content)).Text = introDescription;
            ((TextBlock)(introDescriptionLabel.Content)).TextWrapping = 0;
            introDescriptionLabel.MaxWidth = 500;
            introDescriptionLabel.FontSize = 40;
            introDescriptionLabel.VerticalAlignment = VerticalAlignment.Center;
            introDescriptionLabel.HorizontalAlignment = HorizontalAlignment.Center;
            Canvas.SetLeft(introDescriptionLabel, 200);
            Canvas.SetTop(introDescriptionLabel, 300);

            instructionLabel.Content = new TextBlock();
            ((TextBlock)(instructionLabel.Content)).Text = instructions;
            ((TextBlock)(instructionLabel.Content)).TextWrapping = 0;
            instructionLabel.MaxWidth = 500;
            instructionLabel.FontSize = 40;
            Canvas.SetLeft(instructionLabel, 200);
            Canvas.SetTop(instructionLabel, 100);
            ((TextBlock)instructionLabel.Content).Opacity = 0;

            exitLabel.Content = new TextBlock();
            ((TextBlock)(exitLabel.Content)).Text = "Thanks for playing! \n Your input is invaluable in creating accurate machine learning algorithms";
            ((TextBlock)(exitLabel.Content)).TextWrapping = 0;
            exitLabel.MaxWidth = 500;
            exitLabel.FontSize = 40;
            exitLabel.VerticalAlignment = VerticalAlignment.Center;
            exitLabel.HorizontalAlignment = HorizontalAlignment.Center;
            Canvas.SetLeft(exitLabel, 200);
            Canvas.SetTop(exitLabel, 200);
            ((TextBlock)exitLabel.Content).Opacity = 0;

            scoreLabel.Content = new TextBlock();
            ((TextBlock)(scoreLabel.Content)).Text = "Cars Stopped: 0";
            ((TextBlock)(scoreLabel.Content)).TextWrapping = 0;
            scoreLabel.MaxWidth = 500;
            scoreLabel.FontSize = 30;
            Canvas.SetLeft(scoreLabel, 500);
            Canvas.SetTop(scoreLabel, 500);
            Canvas.SetZIndex(scoreLabel, 10);
            ((TextBlock)scoreLabel.Content).Opacity = 0;

            baseLabel.Content = new TextBlock();
            ((TextBlock)(baseLabel.Content)).Text = baseItem;
            ((TextBlock)(baseLabel.Content)).TextWrapping = 0;
            baseLabel.MaxWidth = 500;
            baseLabel.FontSize = 50;
            Canvas.SetLeft(baseLabel, 300);
            Canvas.SetTop(baseLabel, 0);
            Canvas.SetZIndex(baseLabel, 10);
            ((TextBlock)baseLabel.Content).Opacity = 0;

            //runIntro();
        }


        public override void updateSkeleton(SkeletonData skeleton)
        {
            highlightedHandBaseDepth = skeleton.Joints[JointID.Spine].ScaleTo(1200, 800, .5f, .5f).Position.Z;

            if (userExitedTimer.IsEnabled)
            {
                userExitedTimer.Stop();
            }

            if (state == GAME_STATE.Playing)
            {
                Joint rightHand = skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f);
                Joint leftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f);

                double rightHandScale = skeleton.Joints[JointID.HandRight].Position.Z / highlightedHandBaseDepth;
                double leftHandScale = skeleton.Joints[JointID.HandLeft].Position.Z / highlightedHandBaseDepth;

                rightHandCursor.Height = 100 * rightHandScale;
                rightHandCursor.Width = 100 * rightHandScale;

                leftHandCursor.Height = 100 * leftHandScale;
                leftHandCursor.Width = 100 * leftHandScale;

                Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.Y);
                Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.X);
                Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.Y);
                Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.X);


                List<String> toRemove = new List<String>();

                foreach(String key in carDictionary.Keys)
                {
                    Car curCar = carDictionary[key];
                    Label curLabel = curCar.carLabel;
                    System.Windows.Controls.Image curImage = curCar.carImage;
                    TextBlock curTextBlock = (TextBlock)((Viewbox)curLabel.Content).Child;

                    double deltaX_right = Math.Abs(Canvas.GetLeft(rightHandCursor) - Canvas.GetLeft(curImage));
                    double deltaY_right = Math.Abs(Canvas.GetTop(rightHandCursor) - Canvas.GetTop(curImage));

                    double deltaX_left = Math.Abs(Canvas.GetLeft(leftHandCursor) - Canvas.GetLeft(curImage));
                    double deltaY_left = Math.Abs(Canvas.GetTop(leftHandCursor) - Canvas.GetTop(curImage));

                    //If we have a hit in a reasonable range, highlight the target
                    if (deltaX_right < 200 && deltaY_right < 200)
                    {
                        //Console.WriteLine("Right hand: " + rightHand.Position.Z + " \t Chest: " + highlightedHandBaseDepth);
                        if (Math.Abs(rightHand.Position.Z - highlightedHandBaseDepth) > depthDeltaForSelection)
                        {
                            if (curTextBlock.Foreground == System.Windows.Media.Brushes.Yellow)
                            {
                                toRemove.Add(key);
                            }
                            else
                            {
                                curTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                            }

                        }
                        else
                        {
                            curTextBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        }
                    }
                    else if (deltaX_left < 200 && deltaY_left < 200)
                    {
                        //Console.WriteLine("Right hand: " + rightHand.Position.Z + " \t Chest: " + highlightedHandBaseDepth);
                        if (Math.Abs(leftHand.Position.Z - highlightedHandBaseDepth) > depthDeltaForSelection)
                        {
                            if (curTextBlock.Foreground == System.Windows.Media.Brushes.Yellow)
                            {
                                toRemove.Add(key);
                            }
                            else
                            {
                                curTextBlock.Foreground = System.Windows.Media.Brushes.Red;
                            }

                        }
                        else
                        {
                            curTextBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        }
                    }
                    else
                    {
                        curTextBlock.Foreground = System.Windows.Media.Brushes.Black;
                    }
                }

                for (int i = 0; i < toRemove.Count; i++)
                {
                    removeCar(toRemove[i]);
                    carDictionary.Remove(toRemove[i]);
                    carsStopped++;
                    currentcarDataIndex++;
                    ((TextBlock)scoreLabel.Content).Text = "Cars Stopped: " + carsStopped;
                    addNewCar();

                }
                /*else
                {
                    rightHandCursor.Opacity = .68;
                    leftHandCursor.Opacity = .68;
                }*/
            }
        }

        public override void updateWithoutSkeleton()
        {
            userExitedTimer.Start();
        }

        private void moveToNextState(object sender, EventArgs e)
        {
            Console.WriteLine("Moving to next state");
            fadeTimer.Start();
            transitionTimer.Stop();
            state = state + 1;

            if (state == GAME_STATE.Playing)
            {
                //drawGrid();
            }
            if (state == GAME_STATE.Exit)
            {
                this.exitTimer.Start();
            }
        }

        private void fadeToNextState(object sender, EventArgs e)
        {
            numFaderTicks++;

            if (state == GAME_STATE.Instructions)
            {
                introTitleLabel.Opacity -= .04;
                introDescriptionLabel.Opacity -= .04;

                ((TextBlock)baseLabel.Content).Opacity += .04;
                trafficBackgroundImage.Opacity += .03;
            }
            else if (state == GAME_STATE.Playing)
            {
                //introTitleLabel.Opacity -= .04;
                //introDescriptionLabel.Opacity -= .04;

                leftHandCursor.Opacity += .02;
                rightHandCursor.Opacity += .02;
                ((TextBlock)scoreLabel.Content).Opacity += .04;
                // ((TextBlock)baseLabel.Content).Opacity += .04;
                //trafficBackgroundImage.Opacity += .03;
            }
            else if (state == GAME_STATE.Exit)
            {
                leftHandCursor.Opacity -= .02;
                rightHandCursor.Opacity -= .02;
                trafficBackgroundImage.Opacity -= .03;
                ((TextBlock)exitLabel.Content).Opacity += .04;
                ((TextBlock)scoreLabel.Content).Opacity -= .04;
                ((TextBlock)baseLabel.Content).Opacity -= .04;
            }

            if (numFaderTicks >= 34)
            {
                numFaderTicks = 0;
                fadeTimer.Stop();

                if (state == GAME_STATE.Instructions)
                {
                    runIntro();
                }
                else if (state == GAME_STATE.Playing)
                {
                    addNewCar();
                    carTimer.Start();
                }
                else if (state == GAME_STATE.Exit)
                {
                    Console.WriteLine("hi mom!");
                }
                else
                {
                    transitionTimer.Start();
                }
            }
        }

        private void addNewCar()
        {

            double randomY = laneHeights[randomGen.Next(4)];


            addCarAt(-300, randomY, minigame.getData().ElementAt(currentcarDataIndex).getElementValue(), carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), true);
            /*carBitmapImage = this.toBitmapImage(carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)));
            carImage = new System.Windows.Controls.Image();
            carImage.Source = carBitmapImage;
            carImage.MaxHeight = 100;
            carImage.MaxWidth = 200;

            mainCanvas.Children.Add(carImage);

            carLabel = new Label();
            TextBlock carTextBlock = new TextBlock();
            Viewbox carViewbox = new Viewbox();
            carViewbox.Stretch = Stretch.Uniform;
            carTextBlock.Text = minigame.getData().ElementAt(currentcarDataIndex).getElementValue();
            carViewbox.Height = 100;
            carViewbox.Width = 200;
            carViewbox.Child = carTextBlock;
            carLabel.Content = carViewbox;


            int randomY = laneHeights[randomGen.Next(4)];
            Console.WriteLine(randomY);

            Canvas.SetLeft(carLabel, 0);
            Canvas.SetTop(carLabel, randomY);
            Canvas.SetLeft(carImage, 0);
            Canvas.SetTop(carImage, randomY + 10);
            mainCanvas.Children.Add(carLabel);*/

            //Canvas.SetZIndex(carLabel, 2);
            //Canvas.SetZIndex(carImage, Canvas.GetZIndex(carLabel) - 1);
        }

        private void removeCar(String key)
        {
            Car toRemove = carDictionary[key];

            mainCanvas.Children.Remove(toRemove.carImage);
            mainCanvas.Children.Remove(toRemove.carLabel);
        }

        private void controllerFinished(object o, EventArgs e)
        {
            exitTimer.Stop();
            introCarTimer.Stop();
            introCarAnimatorTimer.Stop();
            transitionTimer.Stop();
            fadeTimer.Stop();
            userExitedTimer.Stop();
            ReturnToStandbyController();
        }

        private void runIntro()
        {
            runInstructions();
        }

        private void runInstructions()
        {

            this.introCarTimer.Start();
            this.introCarAnimatorTimer.Start();
        }

        private void addNewInstructionCar(object o, EventArgs e)
        {
            numIntroCarTimerTicks++;

            switch(numIntroCarTimerTicks)
            {
                case 1:
                    addCarAt(-300, laneHeights[1], "vehicles", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[2], "similar", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[3], "word", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    break;
                case 2:
                    addCarAt(-300, laneHeights[1], "over", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[2], "aren't", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[3], "above", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    break;
                case 3:
                    addCarAt(-300, laneHeights[1], "pull ", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[2], "that", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    addCarAt(-300, laneHeights[3], "to the", carBitmaps.ElementAt(randomGen.Next(0, carBitmaps.Count)), false);
                    break;
            }

            if (carDictionary.Count == 0)
            {
                introCarTimer.Stop();
                introCarAnimatorTimer.Stop();
                moveToNextState(null, null);
            }
        }

        private void addCarAt(double x, double y, String label, Bitmap carBitmap,bool shouldStrechText)
        {
            BitmapImage bitmapImage = this.toBitmapImage(carBitmap);
            System.Windows.Controls.Image image = new System.Windows.Controls.Image();
            image.Source = bitmapImage;
            image.Height = 150;
            image.Width = 300;

            mainCanvas.Children.Add(image);

            Label carLabel = new Label();
            TextBlock carTextBlock = new TextBlock();
            Viewbox carViewbox = new Viewbox();


            carTextBlock.Text = label;
            carViewbox.Height = 100;
            carViewbox.Width = 200;
            carViewbox.Child = carTextBlock;
            carLabel.Content = carViewbox;

            if (shouldStrechText)
            {
                carViewbox.Stretch = Stretch.Uniform;
            }

            mainCanvas.Children.Add(carLabel);

            Canvas.SetLeft(carLabel, x + 30);
            Canvas.SetTop(carLabel, y + 15);
            Canvas.SetLeft(image, x);
            Canvas.SetTop(image, y);

            carDictionary.Add(label, new Car(carLabel,image));
        }

        private void moveCars(object o, EventArgs e)
        {
            List<String> toRemove = new List<String>();

            foreach(String key in carDictionary.Keys)
            {
                Label curLabel = carDictionary[key].carLabel;
                System.Windows.Controls.Image curImage = carDictionary[key].carImage;

                double curLabelLeft = Canvas.GetLeft(curLabel);
                double curImageLeft = Canvas.GetLeft(curImage);

                Canvas.SetLeft(curLabel, curLabelLeft + 3);
                Canvas.SetLeft(curImage, curImageLeft + 3);

                if (Canvas.GetLeft(curLabel) > mainCanvas.ActualWidth)
                {
                    toRemove.Add(key);
                    removeCar(key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                carDictionary.Remove(toRemove[i]);

                if (state == GAME_STATE.Playing)
                {
                    currentcarDataIndex++;
                    addNewCar();
                }
            }
        }




    }
}
