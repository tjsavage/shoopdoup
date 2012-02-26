
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading;
using System.Media;
using System.Text;
using Microsoft.Research.Kinect.Nui;
using Coding4Fun.Kinect.Wpf;
using NetGame;
using NetGame.Speech;
using NetGame.Utils;

namespace ShoopDoup.ViewControllers
{
    class NetGameController : SceneController
    {
        #region Private State
        const int TimerResolution = 2;  // ms
        const int NumIntraFrames = 3;
        const int MaxShapes = 80;
        const double MaxFramerate = 70;
        const double MinFramerate = 15;
        const double MinShapeSize = 12;
        const double MaxShapeSize = 90;
        const double DefaultDropRate = 2.5;
        const double DefaultDropSize = 32.0;
        const double DefaultDropGravity = 1.0;

        Dictionary<int, Player> players = new Dictionary<int, Player>();

        double dropRate = DefaultDropRate;
        double dropSize = DefaultDropSize;
        double dropGravity = DefaultDropGravity;
        DateTime lastFrameDrawn = DateTime.MinValue;
        DateTime predNextFrame = DateTime.MinValue;
        double actualFrameTime = 0;

        // Player(s) placement in scene (z collapsed):
        Rect playerBounds;
        Rect screenRect;

        double targetFramerate = MaxFramerate;
        int frameCount = 0;
        bool runningGameThread = false;
        FallingThings fallingThings = null;
        int playersAlive = 0;
        SoundPlayer popSound = new SoundPlayer();
        SoundPlayer hitSound = new SoundPlayer();
        SoundPlayer squeezeSound = new SoundPlayer();

        RuntimeOptions runtimeOptions;
        SpeechRecognizer speechRecognizer = null;
        private System.Windows.Controls.Canvas playfield;
        System.Windows.Shapes.Line myNet = null;


        #endregion Private State

        public NetGameController()
        {
            //InitState();
            playfield = new Canvas();
            playfield.ClipToBounds = true;
            playfield.Background = Brushes.Azure;
            mainCanvas.Children.Add(playfield);

            fallingThings = new FallingThings(MaxShapes, targetFramerate, NumIntraFrames);
            fallingThings.DrawFrame(playfield.Children);

            //UpdatePlayfieldSize();

            fallingThings.SetGravity(dropGravity);
            fallingThings.SetDropRate(dropRate);
            fallingThings.SetSize(dropSize);
            fallingThings.SetPolies(PolyType.All);
            fallingThings.SetGameMode(FallingThings.GameMode.Off);

            myNet.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            myNet.StrokeThickness = 2;
            playfield.Children.Add(myNet);

            popSound.Stream = Properties.Resources.Pop_5;
            hitSound.Stream = null;
            squeezeSound.Stream = Properties.Resources.Squeeze;

            popSound.Play();

            Win32Timer.timeBeginPeriod(TimerResolution);
            var gameThread = new Thread(GameThread);
            gameThread.SetApartmentState(ApartmentState.STA);
            gameThread.Start();

            FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2), "Shapes!");
        }


        #region ctor + Window Events

        private void RestoreWindowState()
        {
            // Restore window state to that last used
            Rect bounds = Properties.Settings.Default.PrevWinPosition;
            if (bounds.Right != bounds.Left)
            {
                this.Top = bounds.Top;
                this.Left = bounds.Left;
                this.Height = bounds.Height;
                this.Width = bounds.Width;
            }
            this.WindowState = (WindowState)Properties.Settings.Default.WindowState;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            runningGameThread = false;
            Properties.Settings.Default.PrevWinPosition = this.RestoreBounds;
            Properties.Settings.Default.WindowState = (int)this.WindowState;
            Properties.Settings.Default.Save();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            KinectStop();
        }
        #endregion ctor + Window Events*/

        
        #region Kinect Skeleton processing
        public override void updateSkeleton(SkeletonData skeleton)
        {
            //KinectSDK TODO: This nullcheck shouldn't be required. 
            //Unfortunately, this version of the Kinect Runtime will continue to fire some skeletonFrameReady events after the Kinect USB is unplugged.
            myNet.X1 = skeleton.Joints[JointID.HandLeft].Position.X;
            myNet.X2 = skeleton.Joints[JointID.HandRight].Position.X;
            myNet.Y1 = skeleton.Joints[JointID.HandLeft].Position.X;
            myNet.Y2 = skeleton.Joints[JointID.HandRight].Position.X;
          

            int iSkeletonSlot = 0;

            foreach (SkeletonData data in allSkeletons.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    Player player;
                    if (players.ContainsKey(iSkeletonSlot))
                    {
                        player = players[iSkeletonSlot];
                    }
                    else
                    {
                        player = new Player(iSkeletonSlot);
                        player.setBounds(playerBounds);
                        players.Add(iSkeletonSlot, player);
                    }

                    player.lastUpdated = DateTime.Now;

                    // Update player's bone and joint positions
                    if (data.Joints.Count > 0)
                    {
                        player.isAlive = true;

                        // Head, hands, feet (hit testing happens in order here)
                        player.UpdateJointPosition(data.Joints, JointID.HandLeft);
                        player.UpdateJointPosition(data.Joints, JointID.HandRight);

                        //Update the net position
                        player.UpdateBonePosition(data.Joints, JointID.HandLeft, JointID.HandRight);
                       
                    }
                }
                iSkeletonSlot++;
            }
        }

        void SkeletonsReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonFrame skeletonFrame = e.SkeletonFrame;

            //KinectSDK TODO: This nullcheck shouldn't be required. 
            //Unfortunately, this version of the Kinect Runtime will continue to fire some skeletonFrameReady events after the Kinect USB is unplugged.
            if (skeletonFrame == null)
            {
                return;
            }

            SkeletonData skeleton = (from s in skeletonFrame.Skeletons
                                     where s.TrackingState == SkeletonTrackingState.Tracked
                                     select s).FirstOrDefault();
            myNet.X1 = skeleton.Joints[JointID.HandLeft].Position.X;
            myNet.X2 = skeleton.Joints[JointID.HandRight].Position.X;
            myNet.Y1 = skeleton.Joints[JointID.HandLeft].Position.X;
            myNet.Y2 = skeleton.Joints[JointID.HandRight].Position.X;

         

            int iSkeletonSlot = 0;

            foreach (SkeletonData data in skeletonFrame.Skeletons)
            {
                if (SkeletonTrackingState.Tracked == data.TrackingState)
                {
                    Player player;
                    if (players.ContainsKey(iSkeletonSlot))
                    {
                        player = players[iSkeletonSlot];
                    }
                    else
                    {
                        player = new Player(iSkeletonSlot);
                        player.setBounds(playerBounds);
                        players.Add(iSkeletonSlot, player);
                    }

                    player.lastUpdated = DateTime.Now;

                    // Update player's bone and joint positions
                    if (data.Joints.Count > 0)
                    {
                        player.isAlive = true;

                        // Head, hands, feet (hit testing happens in order here)
                        //player.UpdateJointPosition(data.Joints, JointID.Head);
                        player.UpdateJointPosition(data.Joints, JointID.HandLeft);
                        player.UpdateJointPosition(data.Joints, JointID.HandRight);

                        //Update net position
                        player.UpdateBonePosition(data.Joints, JointID.HandLeft, JointID.HandRight);
                    }
                }
                iSkeletonSlot++;
            }
        }

        void CheckPlayers()
        {
            foreach (var player in players)
            {
                if (!player.Value.isAlive)
                {
                    // Player left scene since we aren't tracking it anymore, so remove from dictionary
                    players.Remove(player.Value.getId());
                    break;
                }
            }

            // Count alive players
            int alive = 0;
            foreach (var player in players)
            {
                if (player.Value.isAlive)
                    alive++;
            }
            if (alive != playersAlive)
            {
                if (alive == 2)
                    fallingThings.SetGameMode(FallingThings.GameMode.TwoPlayer);
                else if (alive == 1)
                    fallingThings.SetGameMode(FallingThings.GameMode.Solo);
                else if (alive == 0)
                    fallingThings.SetGameMode(FallingThings.GameMode.Off);

                if ((playersAlive == 0) && (speechRecognizer != null))
                    BannerText.NewBanner(Properties.Resources.Vocabulary, screenRect, true, Color.FromArgb(200, 255, 255, 255));

                playersAlive = alive;
            }
        }

        private void Playfield_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePlayfieldSize();
        }

        private void UpdatePlayfieldSize()
        {
            // Size of player wrt size of playfield, putting ourselves low on the screen.
            screenRect.X = 0;
            screenRect.Y = 0;
            screenRect.Width = playfield.ActualWidth;
            screenRect.Height = playfield.ActualHeight;

            BannerText.UpdateBounds(screenRect);

            playerBounds.X = 0;
            playerBounds.Width = playfield.ActualWidth;
            playerBounds.Y = playfield.ActualHeight * 0.2;
            playerBounds.Height = playfield.ActualHeight * 0.75;

            foreach (var player in players)
                player.Value.setBounds(playerBounds);

            Rect rFallingBounds = playerBounds;
            rFallingBounds.Y = 0;
            rFallingBounds.Height = playfield.ActualHeight;
            if (fallingThings != null)
            {
                fallingThings.SetBoundaries(rFallingBounds);
            }
        }
        #endregion Kinect Skeleton processing

        #region GameTimer/Thread
        private void GameThread()
        {
            runningGameThread = true;
            predNextFrame = DateTime.Now;
            actualFrameTime = 1000.0 / targetFramerate;

            // Try to dispatch at as constant of a framerate as possible by sleeping just enough since
            // the last time we dispatched.
            while (runningGameThread)
            {
                // Calculate average framerate.  
                DateTime now = DateTime.Now;
                if (lastFrameDrawn == DateTime.MinValue)
                    lastFrameDrawn = now;
                double ms = now.Subtract(lastFrameDrawn).TotalMilliseconds;
                actualFrameTime = actualFrameTime * 0.95 + 0.05 * ms;
                lastFrameDrawn = now;

                // Adjust target framerate down if we're not achieving that rate
                frameCount++;
                if (((frameCount % 100) == 0) && (1000.0 / actualFrameTime < targetFramerate * 0.92))
                    targetFramerate = Math.Max(MinFramerate, (targetFramerate + 1000.0 / actualFrameTime) / 2);

                if (now > predNextFrame)
                    predNextFrame = now;
                else
                {
                    double msSleep = predNextFrame.Subtract(now).TotalMilliseconds;
                    if (msSleep >= TimerResolution)
                        Thread.Sleep((int)(msSleep + 0.5));
                }
                predNextFrame += TimeSpan.FromMilliseconds(1000.0 / targetFramerate);

                Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Send,
                    new Action<int>(HandleGameTimer), 0);
            }
        }

        private void HandleGameTimer(int param)
        {
            // Every so often, notify what our actual framerate is
            if ((frameCount % 100) == 0)
                fallingThings.SetFramerate(1000.0 / actualFrameTime);

            // Advance animations, and do hit testing.
            for (int i = 0; i < NumIntraFrames; ++i)
            {
                foreach (var pair in players)
                {
                    HitType hit = fallingThings.LookForHits(pair.Value.segments, pair.Value.getId());
                    if ((hit & HitType.Squeezed) != 0)
                        squeezeSound.Play();
                    else if ((hit & HitType.Popped) != 0)
                        popSound.Play();
                    else if ((hit & HitType.Hand) != 0)
                        hitSound.Play();
                }
                fallingThings.AdvanceFrame();
            }

            // Draw new Wpf scene by adding all objects to canvas
            playfield.Children.Clear();
            fallingThings.DrawFrame(playfield.Children);
            foreach (var player in players)
                player.Value.Draw(playfield.Children);
            BannerText.Draw(playfield.Children);
            FlyingText.Draw(playfield.Children);

            CheckPlayers();
        }
        #endregion GameTimer/Thread*/

        #region Kinect Speech processing
        void recognizer_SaidSomething(object sender, SpeechRecognizer.SaidSomethingEventArgs e)
        {
            FlyingText.NewFlyingText(screenRect.Width / 30, new Point(screenRect.Width / 2, screenRect.Height / 2), e.Matched);
            switch (e.Verb)
            {
                case SpeechRecognizer.Verbs.Pause:
                    fallingThings.SetDropRate(0);
                    fallingThings.SetGravity(0);
                    break;
                case SpeechRecognizer.Verbs.Resume:
                    fallingThings.SetDropRate(dropRate);
                    fallingThings.SetGravity(dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Reset:
                    dropRate = DefaultDropRate;
                    dropSize = DefaultDropSize;
                    dropGravity = DefaultDropGravity;
                    fallingThings.SetPolies(PolyType.All);
                    fallingThings.SetDropRate(dropRate);
                    fallingThings.SetGravity(dropGravity);
                    fallingThings.SetSize(dropSize);
                    fallingThings.SetShapesColor(Color.FromRgb(0, 0, 0), true);
                    fallingThings.Reset();
                    break;
                case SpeechRecognizer.Verbs.DoShapes:
                    fallingThings.SetPolies(e.Shape);
                    break;
                case SpeechRecognizer.Verbs.RandomColors:
                    fallingThings.SetShapesColor(Color.FromRgb(0, 0, 0), true);
                    break;
                case SpeechRecognizer.Verbs.Colorize:
                    fallingThings.SetShapesColor(e.RGBColor, false);
                    break;
                case SpeechRecognizer.Verbs.ShapesAndColors:
                    fallingThings.SetPolies(e.Shape);
                    fallingThings.SetShapesColor(e.RGBColor, false);
                    break;
                case SpeechRecognizer.Verbs.More:
                    dropRate *= 1.5;
                    fallingThings.SetDropRate(dropRate);
                    break;
                case SpeechRecognizer.Verbs.Fewer:
                    dropRate /= 1.5;
                    fallingThings.SetDropRate(dropRate);
                    break;
                case SpeechRecognizer.Verbs.Bigger:
                    dropSize *= 1.5;
                    if (dropSize > MaxShapeSize)
                        dropSize = MaxShapeSize;
                    fallingThings.SetSize(dropSize);
                    break;
                case SpeechRecognizer.Verbs.Biggest:
                    dropSize = MaxShapeSize;
                    fallingThings.SetSize(dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smaller:
                    dropSize /= 1.5;
                    if (dropSize < MinShapeSize)
                        dropSize = MinShapeSize;
                    fallingThings.SetSize(dropSize);
                    break;
                case SpeechRecognizer.Verbs.Smallest:
                    dropSize = MinShapeSize;
                    fallingThings.SetSize(dropSize);
                    break;
                case SpeechRecognizer.Verbs.Faster:
                    dropGravity *= 1.25;
                    if (dropGravity > 4.0)
                        dropGravity = 4.0;
                    fallingThings.SetGravity(dropGravity);
                    break;
                case SpeechRecognizer.Verbs.Slower:
                    dropGravity /= 1.25;
                    if (dropGravity < 0.25)
                        dropGravity = 0.25;
                    fallingThings.SetGravity(dropGravity);
                    break;
            }
        }
        #endregion Kinect Speech processing
       
    }
}

public class Win32Timer
{
    [DllImport("Winmm.dll")]
    public static extern int timeBeginPeriod(UInt32 uPeriod);
}