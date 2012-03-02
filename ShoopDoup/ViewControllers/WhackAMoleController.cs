using System;
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


namespace ShoopDoup.ViewControllers
{
    class WhackAMoleController : SceneController
    {
        private enum GAME_STATE {Intro, Instructions, Playing, Exit};

        private GAME_STATE state;

        private Minigame minigame;
        private String introTitle;
        private String introDescription;

        private String instructions = "Whack items as they pop up if you want to associate them with your current item.\nTo whack, hover over an item and push your hand forward.";

        private Label introTitleLabel;
        private Label introDescriptionLabel;
        private Label instructionLabel;

        private System.Windows.Threading.DispatcherTimer transitionTimer;
        private System.Windows.Threading.DispatcherTimer fadeTimer;
        private System.Windows.Threading.DispatcherTimer popupTimer;

        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;

        private int numFaderTicks = 0;

        private Label popupLabel;
        private int currentPopupDataIndex = 0;

        private Random randomGen = new Random();

        private System.Windows.Shapes.Rectangle[,] grid = new System.Windows.Shapes.Rectangle[3,3];


        public WhackAMoleController(Minigame game)
        {
            state = GAME_STATE.Intro;
            minigame = game;
            introTitle = minigame.getTitle();
            introDescription = minigame.getDescription();

            setupLabels();

            mainCanvas.Children.Add(introTitleLabel);
            mainCanvas.Children.Add(introDescriptionLabel);
            mainCanvas.Children.Add(instructionLabel);

            this.transitionTimer = new System.Windows.Threading.DispatcherTimer();
            this.transitionTimer.Tick += moveToNextState;
            this.transitionTimer.Interval = TimeSpan.FromMilliseconds(2000);
            this.transitionTimer.Start();

            this.fadeTimer = new System.Windows.Threading.DispatcherTimer();
            this.fadeTimer.Tick += fadeToNextState;
            this.fadeTimer.Interval = TimeSpan.FromMilliseconds(40);
            this.fadeTimer.IsEnabled = false;

            this.popupTimer = new System.Windows.Threading.DispatcherTimer();
            this.popupTimer.Tick += changePopup;
            this.popupTimer.Interval = TimeSpan.FromMilliseconds(2500);

            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            rightHandCursor.Opacity = 0;
            leftHandCursor.Opacity = 0;

            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
        }

        private void setupLabels()
        {
            introTitleLabel = new Label();
            introDescriptionLabel = new Label();
            instructionLabel = new Label();

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
        }


        public override void updateSkeleton(SkeletonData skeleton)
        {
            if (state == GAME_STATE.Playing)
            {
                Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y);
                Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X);
                Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y);
                Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.X);
            }
        }

        public override void updateWithoutSkeleton()
        {

        }

        private void moveToNextState(object sender, EventArgs e)
        {
            Console.WriteLine("Moving to next state");
            fadeTimer.Start();
            transitionTimer.Stop();
            state = state + 1;

            if (state == GAME_STATE.Playing)
            {
                drawGrid();
            }
        }

        private void fadeToNextState(object sender, EventArgs e)
        {
            numFaderTicks++;

            if (state == GAME_STATE.Instructions)
            {
                introTitleLabel.Opacity -= .04;
                introDescriptionLabel.Opacity -= .04;
                ((TextBlock)instructionLabel.Content).Opacity += .04;
            }
            else if (state == GAME_STATE.Playing)
            {
                ((TextBlock)instructionLabel.Content).Opacity -= .04;
                leftHandCursor.Opacity += .01;
                rightHandCursor.Opacity += .01;

                for (int row = 0; row < 3; row++)
                {
                    for (int col = 0; col < 3; col++)
                    {
                        grid[row, col].Opacity += .02;
                    }
                }
            }

            if (numFaderTicks >= 34)
            {
                numFaderTicks = 0;
                fadeTimer.Stop();

                if (state != GAME_STATE.Playing)
                {
                    transitionTimer.Start();
                }
                else
                {
                    addNewPopup();
                    popupTimer.Start();
                }
            }
        }

        private void drawGrid()
        {
            for (int row = 0; row < 3; row++)
            {
                for (int col = 0; col < 3; col++)
                {
                    grid[row,col] = new System.Windows.Shapes.Rectangle();
                    grid[row, col].Stroke = System.Windows.Media.Brushes.OrangeRed;
                    grid[row,col].Height = 80;
                    grid[row,col].Width = 80;
                    Canvas.SetLeft(grid[row,col], 220 + 110*row);
                    Canvas.SetTop(grid[row,col], 150 + 110*col);
                    grid[row, col].Opacity = 0;
                    
                    mainCanvas.Children.Add(grid[row,col]);
                }
            }
        }

        private void addNewPopup()
        {
            popupLabel = new Label();
            popupLabel.Content = new TextBlock();
            //((TextBlock)(popupLabel.Content)).Text = minigame.getData().ElementAt(currentPopupDataIndex).getElementValue();
            ((TextBlock)(popupLabel.Content)).TextWrapping = 0;
            popupLabel.MaxWidth = 500;
            popupLabel.FontSize = 40;

            int randomRow = randomGen.Next(0,2);
            int randomCol = randomGen.Next(0,2);
        }

        private void changePopup(object sender, EventArgs e)
        {
            
        }
    }
}
