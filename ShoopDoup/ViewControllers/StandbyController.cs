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
        private enum STANDBY_STATE { Sleep, Attention, FollowingLeft, FollowingRight, Bored };

        private BitmapImage welcomeSleepBitmap;
        private BitmapImage welcomeAttentionBitmap;
        private BitmapImage welcomeFollowingRightBitmap;
        private BitmapImage welcomeFollowingLeftBitmap;
        private BitmapImage welcomeBoredBitmap;
        private System.Windows.Controls.Image currentImage;
        private STANDBY_STATE state;
        private DateTime lastPlayerTime;
        private DateTime playerActiveTime;
        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;
        private System.Windows.Threading.DispatcherTimer fadeTimer;

        public StandbyController() : base()
        {
            welcomeSleepBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeSleep);
            welcomeAttentionBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeAttention);
            welcomeFollowingLeftBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeFollowing);
            System.Drawing.Bitmap followingBitmap = ShoopDoup.Properties.Resources.WelcomeFollowing;
            followingBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            welcomeFollowingRightBitmap = this.toBitmapImage(followingBitmap);
            welcomeBoredBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeBored);

            currentImage = new System.Windows.Controls.Image();
            state = STANDBY_STATE.Sleep;
            currentImage.Source = welcomeSleepBitmap;
            currentImage.Width = 800;

            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            mainCanvas.Children.Add(currentImage);
            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);

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
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {

            if (rightHandCursor.Visibility == System.Windows.Visibility.Hidden)
            {
                rightHandCursor.Visibility = System.Windows.Visibility.Visible;
                leftHandCursor.Visibility = System.Windows.Visibility.Visible;
            }
            Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y);
            Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X);
            Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y);
            Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.X);


            if (skeleton.Joints[JointID.Spine].ScaleTo(640, 480, .5f, .5f).Position.X < this.WindowWidth / 3)
            {
                if (state != STANDBY_STATE.FollowingLeft)
                {
                    state = STANDBY_STATE.FollowingLeft;
                    currentImage.Source = welcomeFollowingLeftBitmap;
                }
            }
            else if (skeleton.Joints[JointID.Spine].ScaleTo(640, 480, .5f, .5f).Position.X > this.WindowWidth * 2 / 3)
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

            if (state == STANDBY_STATE.Attention && (DateTime.UtcNow - playerActiveTime).Seconds > 5)
            {
                fadeTimer.IsEnabled = true;
            }
        }

        private void FadeOut(object sender, EventArgs e)
        {
            currentImage.Opacity -= .03;
            leftHandCursor.Opacity -= .03;
            rightHandCursor.Opacity -= .03;
            fadeTimer.IsEnabled = true;
        
            if (currentImage.Opacity < .04)
            {
                fadeTimer.IsEnabled = false;
                parentController.controllerFinished();
            }
        }

        public override void updateWithoutSkeleton()
        {
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
