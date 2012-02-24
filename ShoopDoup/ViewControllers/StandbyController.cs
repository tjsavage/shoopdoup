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


namespace ShoopDoup.ViewControllers
{
    class StandbyController : SceneController
    {
        private enum STANDBY_STATE { Sleep, Attention, FollowingLeft, FollowingRight };

        private BitmapImage welcomeSleepBitmap;
        private BitmapImage welcomeAttentionBitmap;
        private BitmapImage welcomeFollowingRightBitmap;
        private BitmapImage welcomeFollowingLeftBitmap;
        private System.Windows.Controls.Image currentImage;
        private STANDBY_STATE state;

        public StandbyController()
        {
            welcomeSleepBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeSleep);
            welcomeAttentionBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeAttention);
            welcomeFollowingLeftBitmap = this.toBitmapImage(ShoopDoup.Properties.Resources.WelcomeFollowing);
            System.Drawing.Bitmap followingBitmap = ShoopDoup.Properties.Resources.WelcomeFollowing;
            followingBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            welcomeFollowingRightBitmap = this.toBitmapImage(followingBitmap);

            currentImage = new System.Windows.Controls.Image();
            state = STANDBY_STATE.Sleep;
            currentImage.Source = welcomeSleepBitmap;

            mainGrid.Children.Add(currentImage);
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {
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
            }

        }

        public override void updateWithoutSkeleton()
        {
            if (state != STANDBY_STATE.Sleep)
            {
                currentImage.Source = welcomeSleepBitmap;
                state = STANDBY_STATE.Sleep;
            }
        }
    }
}
