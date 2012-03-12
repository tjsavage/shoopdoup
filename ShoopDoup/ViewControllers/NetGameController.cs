using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using Microsoft.Research.Kinect.Nui;
using Microsoft.Research.Kinect.Audio;
using Coding4Fun.Kinect.Wpf;
using System.Windows.Media.Imaging;
using System.Windows.Media.Animation;
using NetGame.Utils;
using NetGame.Speech;
using ShoopDoup.Models;


namespace ShoopDoup.ViewControllers
{
    class NetGameController : SceneController
    {

        #region Private State
        const int TimerResolution = 2;  // ms
        const int NumIntraFrames = 3;
        const int MaxShapes = 10;
        const double MaxFramerate = 70;
        const double MinFramerate = 15;
        const double MinShapeSize = 12;
        const double MaxShapeSize = 90;
        const double DefaultDropRate = 1;
        const double DefaultDropSize = 32.0;
        const double DefaultDropGravity = 1.0;

        int displayWidth = 1180;
        int displayHeight=700;

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

        RuntimeOptions runtimeOptions;
        SpeechRecognizer speechRecognizer = null;

        private STANDBY_STATE state;

        private System.Windows.Controls.Image currentImage;
        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;
        private System.Windows.Controls.Image background;
        private System.Windows.Shapes.Line myNet;
        private System.Windows.Shapes.Rectangle startGameRect;
        private System.Windows.Controls.Canvas playfield;

        private enum STANDBY_STATE { Instructions, Playing, Exiting };
        private BitmapImage instructionsBitmap;

        private Minigame minigame;
        private int timeLeft;
        private Label timerLabel;
        private int score;
        private Label scoreLabel;
        private System.Windows.Threading.DispatcherTimer gameTimer;

        #endregion Private State


        public NetGameController(Minigame game) : base()
        {
            this.minigame = game;
            start();
        }
            
        public override void start() {
            currentImage = new System.Windows.Controls.Image();
            state = STANDBY_STATE.Instructions;
            currentImage.Source = instructionsBitmap;
            currentImage.Width = 800;

            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            playfield = new System.Windows.Controls.Canvas();
            //playfield.Background = Brushes.CadetBlue;
            playfield.Width = 1440;
            playfield.Height =900;
            UpdatePlayfieldSize();

            startGameRect = new System.Windows.Shapes.Rectangle();
            startGameRect.Stroke = System.Windows.Media.Brushes.Black;
            startGameRect.Fill = System.Windows.Media.Brushes.Green;
            startGameRect.Height = 100;
            startGameRect.Width = 100;
            mainCanvas.Children.Add(startGameRect);
            Canvas.SetTop(startGameRect, 300);
            Canvas.SetLeft(startGameRect, 320);


            myNet = new System.Windows.Shapes.Line();
            myNet.Stroke = System.Windows.Media.Brushes.LightSteelBlue;
            myNet.X1 = 1;
            myNet.X2 = 50;
            myNet.Y1 = 1;
            myNet.Y2 = 50;
            myNet.StrokeThickness = 5;


            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
            mainCanvas.Children.Add(myNet);
            mainCanvas.Children.Add(playfield);


            Canvas.SetZIndex(rightHandCursor, 2);
            Canvas.SetZIndex(leftHandCursor, 2);
            Canvas.SetZIndex(myNet, 1);
            Canvas.SetZIndex(playfield, 0);

            rightHandCursor.Visibility = System.Windows.Visibility.Hidden;
            leftHandCursor.Visibility = System.Windows.Visibility.Hidden;
            myNet.Visibility = System.Windows.Visibility.Hidden;
            playfield.Visibility = System.Windows.Visibility.Hidden;

            initNetGame();

            /*speechRecognizer = SpeechRecognizer.Create();         //returns null if problem with speech prereqs or instantiation.
            if (speechRecognizer != null)
            {
                speechRecognizer.Start(new KinectAudioSource());  //KinectSDK TODO: expose Runtime.AudioSource to return correct audiosource.
                speechRecognizer.SaidSomething += new EventHandler<SpeechRecognizer.SaidSomethingEventArgs>(recognizer_SaidSomething);
            }*/
            
        }

        public void initNetGame()
        {
            double sceneWidth = mainCanvas.ActualWidth;
            myNet.Visibility = System.Windows.Visibility.Visible;
            playfield.Visibility = System.Windows.Visibility.Visible;
            startGameRect.Visibility = System.Windows.Visibility.Hidden;

            fallingThings = new FallingThings(MaxShapes, targetFramerate, NumIntraFrames, 1440, 900);

            fallingThings.SetGravity(dropGravity);
            fallingThings.SetDropRate(dropRate);
            fallingThings.SetSize(dropSize);
            fallingThings.SetPolies(PolyType.All);
            fallingThings.SetGameMode(FallingThings.GameMode.Off);

            Win32Timer.timeBeginPeriod(TimerResolution);
            var gameThread = new Thread(GameThread);
            gameThread.SetApartmentState(ApartmentState.STA);
            gameThread.Start();
            LoadBackground();
            SetTimerLabel();
            StartGameTimer();
            LoadScore();
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {

            if (rightHandCursor.Visibility == System.Windows.Visibility.Hidden)
            {
                rightHandCursor.Visibility = System.Windows.Visibility.Visible;
                leftHandCursor.Visibility = System.Windows.Visibility.Visible;
            }

            Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y);
            Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X);
            Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y);
            Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X);

            if (startGameRect.Visibility==System.Windows.Visibility.Visible && skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X < 400 && skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y < 300)
            {
                initNetGame();
            }

            myNet.X1 = skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X+50;
            myNet.X2 = skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X+50;
            myNet.Y1 = skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y+50;
            myNet.Y2 = skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y+50;
        }
        
        public override void updateWithoutSkeleton()
        {
            ReturnToStandbyController();
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
            playerBounds.Width = 1265;
            playerBounds.Y = playfield.ActualHeight;
            playerBounds.Height = playfield.ActualHeight ;

            Rect rFallingBounds = playerBounds;
            rFallingBounds.Y = 0;
            rFallingBounds.Height = playfield.ActualHeight;
            if (fallingThings != null)
            {
                //fallingThings.SetBoundaries(rFallingBounds);
            }
        }

        private void SetTimerLabel()
        {
            timeLeft = 60;
            this.timerLabel = new Label();
            timerLabel.FontSize = 40;
            timerLabel.Foreground = System.Windows.Media.Brushes.Red;
            timerLabel.Content = "TIME: " + timeLeft;
            mainCanvas.Children.Add(timerLabel);
            Canvas.SetTop(timerLabel, 700);
            Canvas.SetLeft(timerLabel, 20);
            Canvas.SetZIndex(timerLabel, 4);
        }

        private void StartGameTimer()
        {
            this.gameTimer = new System.Windows.Threading.DispatcherTimer();
            this.gameTimer.Tick += countdown;
            this.gameTimer.Interval = TimeSpan.FromMilliseconds(1000);
            this.gameTimer.Start();
        }

        private void countdown(object sender, EventArgs e)
        {
            timeLeft--;
            timerLabel.Content = "TIME: " + timeLeft;
            if (timeLeft == 0)
            {
                gameTimer.Stop();
                ReturnToStandbyController();
            }
        }

        private void LoadScore()
        {
            score = 0;
            scoreLabel = new Label();
            scoreLabel.Content = "SCORE: " + score;
            scoreLabel.FontSize = 40;
            scoreLabel.Foreground = System.Windows.Media.Brushes.Red;
            mainCanvas.Children.Add(scoreLabel);
            Canvas.SetTop(scoreLabel, 700);
            Canvas.SetLeft(scoreLabel, 1000);
            Canvas.SetZIndex(scoreLabel, 4);
        }

        private void LoadBackground()
        {
            background = new System.Windows.Controls.Image();
            background.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.backgroundTree);
            background.Width = 1280;
            background.Height = 800;
            mainCanvas.Children.Add(background);
            Canvas.SetTop(background, -2);
            Canvas.SetLeft(background, 0);
            Canvas.SetZIndex(background, -20);
        }

        void CheckPlayers()
        {

            fallingThings.SetGameMode(FallingThings.GameMode.Solo);
            /*
                if (alive == 2)
                    fallingThings.SetGameMode(FallingThings.GameMode.TwoPlayer);
                else if (alive == 1)
                    fallingThings.SetGameMode(FallingThings.GameMode.Solo);
                else if (alive == 0)
                    fallingThings.SetGameMode(FallingThings.GameMode.Off);

                if ((playersAlive == 0) && (speechRecognizer != null))
                    BannerText.NewBanner(Properties.Resources.Vocabulary, screenRect, true, Color.FromArgb(200, 255, 255, 255));
                playersAlive = alive;
            }*/
        }

        #region Gamer/Thread
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

                Dispatcher.Invoke(DispatcherPriority.Send,
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
                HitType hit = fallingThings.LookForHits(myNet,1);
                if (hit != null)
                {

                }
                fallingThings.AdvanceFrame(playfield.Children);
            }

            // Draw new Wpf scene by adding all objects to canvas
            //playfield.Children.Clear();
            //fallingThings.DrawFrame(playfield.Children);
            //BannerText.Draw(playfield.Children);
            //FlyingText.Draw(playfield.Children);

            CheckPlayers();
        }
        #endregion GameTimer/Thread

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

// Since the timer resolution defaults to about 10ms precisely, we need to
// increase the resolution to get framerates above between 50fps with any
// consistency.
public class Win32Timer
{
    [DllImport("Winmm.dll")]
    public static extern int timeBeginPeriod(UInt32 uPeriod);
}
