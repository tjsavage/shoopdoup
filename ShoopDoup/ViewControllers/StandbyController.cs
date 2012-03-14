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


namespace ShoopDoup.ViewControllers
{
    class StandbyController : SceneController
    {
        private enum STANDBY_STATE { Sleep, Attention, FollowingLeft, FollowingRight, Bored, Thinking };

        private BitmapImage welcomeSleepBitmap;
        private BitmapImage welcomeAttentionBitmap;
        private BitmapImage welcomeFollowingRightBitmap;
        private BitmapImage welcomeFollowingLeftBitmap;
        private BitmapImage welcomeBoredBitmap;
        private BitmapImage thinkingBitmap;
        private System.Windows.Controls.Image currentImage;
        private STANDBY_STATE state;
        private DateTime lastPlayerTime;
        private DateTime playerActiveTime;
        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;
        private System.Windows.Threading.DispatcherTimer fadeTimer;

        private Label genericIntroLabel;
        private Label instructionIntroLabel;
        private System.Windows.Threading.DispatcherTimer exitTimer;

        public StandbyController() : base()
        {
            welcomeSleepBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeSleep);
            welcomeAttentionBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeAttention);
            welcomeFollowingLeftBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeFollowing);
            System.Drawing.Bitmap followingBitmap = ShoopDoup.Properties.Resources.WelcomeFollowing;
            followingBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            welcomeFollowingRightBitmap = this.toBitmapImage(followingBitmap);
            welcomeBoredBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeBored);
            thinkingBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.ThinkingUpgame);

            currentImage = new System.Windows.Controls.Image();
            state = STANDBY_STATE.Sleep;
            currentImage.Source = welcomeSleepBitmap;
            currentImage.Width = 1280;

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

            TextBlock genericTextBlock = new TextBlock();
            Viewbox genericViewbox = new Viewbox();
            genericIntroLabel = new Label();

            genericTextBlock.Text = "Help researchers \nby providing your\nopinion through a game!";
            genericTextBlock.Foreground = System.Windows.Media.Brushes.Navy;
            genericViewbox.Height = 500;
            genericViewbox.Width = 800;
            genericViewbox.Child = genericTextBlock;
            genericIntroLabel.Content = genericViewbox;
            genericViewbox.Stretch = Stretch.Uniform;
            genericIntroLabel.Opacity = 0;

            TextBlock instructionTextBlock = new TextBlock();
            Viewbox instructionViewbox = new Viewbox();
            instructionIntroLabel = new Label();

            instructionIntroLabel.Height = 500;
            instructionIntroLabel.Width = 800;
            instructionTextBlock.Foreground = System.Windows.Media.Brushes.Navy;
            instructionViewbox.Height = 500;
            instructionViewbox.Width = 800;
            instructionTextBlock.FontSize = 50;
            instructionViewbox.Child = instructionTextBlock;
            instructionIntroLabel.Content = instructionViewbox;
            instructionViewbox.Stretch = Stretch.Uniform;
            instructionIntroLabel.Opacity = 0;

            Canvas.SetLeft(genericIntroLabel, 400);
            Canvas.SetTop(genericIntroLabel, 100);
            Canvas.SetZIndex(genericIntroLabel, 2);

            Canvas.SetLeft(instructionIntroLabel, 400);
            Canvas.SetTop(instructionIntroLabel, 100);
            Canvas.SetZIndex(instructionIntroLabel, 2);

            mainCanvas.Children.Add(currentImage);
            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
            mainCanvas.Children.Add(genericIntroLabel);
            mainCanvas.Children.Add(instructionIntroLabel);

            //Canvas.SetTop(currentImage, 20);
            //Canvas.SetLeft(currentImage, this.WindowWidth / 2);
            
            Canvas.SetZIndex(currentImage, 1);
            Canvas.SetZIndex(rightHandCursor, 2);
            Canvas.SetZIndex(leftHandCursor, 2);
            rightHandCursor.Visibility = System.Windows.Visibility.Hidden;
            leftHandCursor.Visibility = System.Windows.Visibility.Hidden;

            this.playerActiveTime = DateTime.UtcNow;
            this.fadeTimer = new System.Windows.Threading.DispatcherTimer();
            this.fadeTimer.Tick += FadeOut;
            this.fadeTimer.Interval = TimeSpan.FromMilliseconds(40);
            this.fadeTimer.IsEnabled = false;

            this.exitTimer = new System.Windows.Threading.DispatcherTimer();
            this.exitTimer.Tick += prepareToExit;
            this.exitTimer.Interval = TimeSpan.FromMilliseconds(3000);
            this.exitTimer.IsEnabled = false;

            state = STANDBY_STATE.Thinking;
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {

            if (rightHandCursor.Visibility == System.Windows.Visibility.Hidden)
            {
                rightHandCursor.Visibility = System.Windows.Visibility.Visible;
                leftHandCursor.Visibility = System.Windows.Visibility.Visible;
            }
            Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.Y);
            Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.X);
            Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.Y);
            Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.X);

            if (state == STANDBY_STATE.Thinking)
            {
                genericIntroLabel.Opacity = 1;
                currentImage.Source = thinkingBitmap;
                exitTimer.Start();
            }
            else
            {
                exitTimer.Stop();

                if (skeleton.Joints[JointID.Spine].ScaleTo(1280, 800, .5f, .5f).Position.X < this.WindowWidth / 3)
                {
                    if (state != STANDBY_STATE.FollowingLeft)
                    {
                        state = STANDBY_STATE.FollowingLeft;
                        currentImage.Source = welcomeFollowingLeftBitmap;
                    }
                }
                else if (skeleton.Joints[JointID.Spine].ScaleTo(1280, 800, .5f, .5f).Position.X > this.WindowWidth * 2 / 3)
                {
                    if (state != STANDBY_STATE.FollowingRight)
                    {
                        state = STANDBY_STATE.FollowingRight;
                        currentImage.Source = welcomeFollowingRightBitmap;
                    }
                }
                else if (state != STANDBY_STATE.Attention)
                {
                    state = STANDBY_STATE.Attention;
                    currentImage.Source = welcomeAttentionBitmap;
                    playerActiveTime = DateTime.UtcNow;
                }

                if (state == STANDBY_STATE.Attention && (DateTime.UtcNow - playerActiveTime).Seconds > 3)
                {
                    state = STANDBY_STATE.Thinking;
                    //fadeTimer.IsEnabled = true;
                }
            }
        }

        private void FadeOut(object sender, EventArgs e)
        {
            currentImage.Opacity -= .03;
            leftHandCursor.Opacity -= .03;
            rightHandCursor.Opacity -= .03;
            //fadeTimer.IsEnabled = true;
        
            if (currentImage.Opacity < .04)
            {
                fadeTimer.IsEnabled = false;
                ReturnToStandbyController();
                //parentController.controllerFinished();
            }
        }

        private void prepareToExit(object sender, EventArgs e)
        {
            genericIntroLabel.Opacity = 0;
            instructionIntroLabel.Opacity = 1;
            exitTimer.Stop();
            ReturnToStandbyController();
        }

        public void setInstructionText(String instructions)
        {
            TextBlock contentBlock = (TextBlock)((Viewbox)instructionIntroLabel.Content).Child;
            contentBlock.Text = instructions;
            //instructionIntroLabel.FontSize = 50;
        }

        public override void updateWithoutSkeleton()
        {
            exitTimer.Stop();

            if (rightHandCursor.Visibility == System.Windows.Visibility.Visible)
            {
                rightHandCursor.Visibility = System.Windows.Visibility.Hidden;
                leftHandCursor.Visibility = System.Windows.Visibility.Hidden;
            }
            if (state != STANDBY_STATE.Bored && state != STANDBY_STATE.Sleep)
            {
                lastPlayerTime = DateTime.UtcNow;
            }

            if (state != STANDBY_STATE.Sleep && (DateTime.UtcNow - lastPlayerTime).Seconds > 5)
            {
                currentImage.Source = welcomeSleepBitmap;
                state = STANDBY_STATE.Sleep;
            }
            if (state != STANDBY_STATE.Bored && (DateTime.UtcNow - lastPlayerTime).Seconds < 5)
            {
                currentImage.Source = welcomeBoredBitmap;
                state = STANDBY_STATE.Bored;
            }
        }
    }
}
