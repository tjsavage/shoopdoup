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
        const int MaxShapes = 4;
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
        private System.Windows.Controls.Image timerOutline;
        private System.Windows.Controls.Image scoreOutline;
        private System.Windows.Controls.Image associationOutline;

        private System.Windows.Controls.Label label;
        private System.Windows.Shapes.Line myNet;
        private System.Windows.Controls.Canvas playfield;
        private Random randomGen = new Random();
        private Minigame minigame;

        private enum STANDBY_STATE { Instructions, Playing, Exiting };

        private int timeLeft;
        private Label timerLabel;
        private int score;
        private Label scoreLabel;
        private Label associationLabel;
        private System.Windows.Threading.DispatcherTimer gameTimer;
        private System.Windows.Threading.DispatcherTimer instructionTimer;
        private int secondsLeft;
        private bool instructing;
        private int run;
        private List<String> textLabels=new List<String>();

        #endregion Private State


        public NetGameController(Minigame game) : base()
        {
            this.minigame = game;
            start();
        }
            
        public override void start() {
            currentImage = new System.Windows.Controls.Image();
            currentImage.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.ThinkingUpgame);
            currentImage.Width = 1280;
            currentImage.Height = 800;

            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(System.Drawing.RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            playfield = new System.Windows.Controls.Canvas();
            playfield.Width = 1280;
            playfield.Height =800;
            UpdatePlayfieldSize();


            label = new Label();
            TextBlock bubbleTextBlock = new TextBlock();
            bubbleTextBlock.Text = "Catch the European Countries!";
            SolidColorBrush appleColor = new SolidColorBrush();
            appleColor.Color = Color.FromArgb(255, 227, 50, 50);
            bubbleTextBlock.Foreground = appleColor;
            Viewbox bubbleViewBox = new Viewbox();
            bubbleViewBox.Stretch = Stretch.Uniform;
            bubbleViewBox.Height = 300;
            bubbleViewBox.Width = 700;
            bubbleViewBox.Child = bubbleTextBlock;
            label.Content = bubbleViewBox;
            Canvas.SetLeft(label, 450);
            Canvas.SetTop(label,500);
            

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
            mainCanvas.Children.Add(currentImage);
            mainCanvas.Children.Add(label);


            Canvas.SetZIndex(rightHandCursor, 2);
            Canvas.SetZIndex(leftHandCursor, 2);
            Canvas.SetZIndex(myNet, 1);
            Canvas.SetZIndex(playfield, 0);
            Canvas.SetZIndex(currentImage, 0);
            Canvas.SetZIndex(label, 1);

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
            label.Visibility = System.Windows.Visibility.Hidden;
            currentImage.Visibility = System.Windows.Visibility.Hidden;

            String text;

            foreach (ShoopDoup.Models.DataObject obj in this.minigame.getData())
            {
                if (obj.getElementValue() != null)
                {
                    textLabels.Add(obj.getElementValue().ToLower());
                }
            }

            fallingThings = new FallingThings(MaxShapes, targetFramerate, NumIntraFrames, 1440, 900,textLabels);

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
            instructing = true;
            run = 0;
            StartInstructionTimer(1);
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

            myNet.X1 = skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X+50;
            myNet.X2 = skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.X+50;
            myNet.Y1 = skeleton.Joints[JointID.HandRight].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y+50;
            myNet.Y2 = skeleton.Joints[JointID.HandLeft].ScaleTo(displayWidth, displayHeight, .5f, .5f).Position.Y+50;
        }
        
        public override void updateWithoutSkeleton()
        {
            //ShowInstructions(playfield.Children,11);
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
            timeLeft = 10;
            this.timerLabel = new Label();
            timerLabel.FontSize = 40;
            SolidColorBrush timerColor = new SolidColorBrush();
            timerColor.Color = Color.FromArgb(255, 207, 20, 20);
            timerLabel.Foreground = timerColor;
            timerLabel.Content = ":"+timeLeft;
            timerLabel.FontFamily = new FontFamily("Arial");
            timerLabel.FontWeight = FontWeights.Bold;
            mainCanvas.Children.Add(timerLabel);
            Canvas.SetTop(timerLabel, 22);
            Canvas.SetLeft(timerLabel, 175);
            Canvas.SetZIndex(timerLabel, 301);

            timerOutline = new System.Windows.Controls.Image();
            timerOutline.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.TwoDigitRed);
            //210x131
            timerOutline.Width = 100;
            timerOutline.Height = 65;
            mainCanvas.Children.Add(timerOutline);

            timerOutline.Visibility = System.Windows.Visibility.Visible;
            Canvas.SetZIndex(timerOutline, 300);
            Canvas.SetTop(timerOutline, 20);
            Canvas.SetLeft(timerOutline, 160);
        }

        private void StartGameTimer()
        {
            this.gameTimer = new System.Windows.Threading.DispatcherTimer();
            this.gameTimer.Tick += countdown;
            this.gameTimer.Interval = TimeSpan.FromMilliseconds(1000);
            this.gameTimer.Start();
        }

        private void StartInstructionTimer(int time)
        {
            secondsLeft = time;
            this.instructionTimer = new System.Windows.Threading.DispatcherTimer();
            this.instructionTimer.Tick += instructUser;
            this.instructionTimer.Interval = TimeSpan.FromMilliseconds(700);
            this.instructionTimer.Start();
        }

        private void instructUser(Object sender, EventArgs e)
        {
            secondsLeft--;
            if (secondsLeft == 0 && run<7)
            {
                run++;
                ShowInstructions(playfield.Children, run);
                this.instructionTimer.Stop();
                StartInstructionTimer(1);
            }
            else if(secondsLeft==0)
            {
                run++;
                ShowInstructions(playfield.Children, run);
                this.instructionTimer.Stop();
                StartInstructionTimer(8);
            }
        }

        private void countdown(object sender, EventArgs e)
        {
            timeLeft--;
            timerLabel.Content = ":" + timeLeft;
            if (timeLeft == 0)
            {
                gameTimer.Stop();
                EndGame();
            }
        }

        private void EndGame()
        {
            instructing = true;
            StartInstructionTimer(12);
        }

        private void LoadScore()
        {
            score = 8575;
            scoreLabel = new Label();
            scoreLabel.Content = score;
            scoreLabel.FontSize = 40;
            SolidColorBrush timerColor = new SolidColorBrush();
            timerColor.Color = Color.FromArgb(255, 207, 20, 20);
            scoreLabel.Foreground = timerColor;
            scoreLabel.FontFamily = new FontFamily("Arial");
            scoreLabel.FontWeight = FontWeights.Bold;
            mainCanvas.Children.Add(scoreLabel);
            Canvas.SetTop(scoreLabel, 22);
            Canvas.SetLeft(scoreLabel, 40);
            Canvas.SetZIndex(scoreLabel, 301);

            scoreOutline = new System.Windows.Controls.Image();
            scoreOutline.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.ThreeDigitRed);
            scoreOutline.Height = 65;
            scoreOutline.Width = 150;

            mainCanvas.Children.Add(scoreOutline);

            Canvas.SetZIndex(scoreOutline, 300);
            Canvas.SetTop(scoreOutline, 20);
            Canvas.SetLeft(scoreOutline, 15);
            scoreOutline.Visibility = System.Windows.Visibility.Visible;

        }

        private void LoadAssociation()
        {
            this.associationLabel = new Label();
            associationLabel.FontSize = 40;
            SolidColorBrush timerColor = new SolidColorBrush();
            timerColor.Color = Color.FromArgb(255, 207, 20, 20);
            associationLabel.Foreground = timerColor;
            int minigameRandom = randomGen.Next(0, minigame.getData().Count - 1);
            String text = minigame.getData()[minigameRandom].getElementValue().ToUpper();
            associationLabel.Content = text;
            int width = text.Length * 26+30;
            //double actualWidth = ;

            associationLabel.FontFamily = new FontFamily("Arial");
            associationLabel.FontWeight = FontWeights.Bold;
            associationLabel.Width = width;

            mainCanvas.Children.Add(associationLabel);
            Canvas.SetTop(associationLabel, 22);
            Canvas.SetLeft(associationLabel, 640-associationLabel.Width/2);
            Canvas.SetZIndex(associationLabel, 301);

            associationOutline = new System.Windows.Controls.Image();
            associationOutline.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.redText);
            //210x131
            associationOutline.Width = 300;
            associationOutline.Height = 65;
            mainCanvas.Children.Add(associationOutline);

            timerOutline.Visibility = System.Windows.Visibility.Visible;
            Canvas.SetZIndex(associationOutline, 300);
            Canvas.SetTop(associationOutline, 20);
            Canvas.SetLeft(associationOutline, 470);
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

            Image leaves = new System.Windows.Controls.Image();
            leaves.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.leaves);
            leaves.Width = 1280;
            leaves.Height = 300;
            mainCanvas.Children.Add(leaves);
            Canvas.SetTop(leaves, -145);
            Canvas.SetLeft(leaves, 0);
            Canvas.SetZIndex(leaves, 200);


        }

        private void ShowGoodbye()
        {
            Label endLabel = new Label();
            endLabel.FontSize = 50;
            SolidColorBrush timerColor = new SolidColorBrush();
            timerColor.Color = Color.FromArgb(255, 207, 20, 20);
            endLabel.Foreground = timerColor;
            endLabel.Content = "Thanks For Playing!";
            endLabel.FontFamily = new FontFamily("Arial");
            mainCanvas.Children.Add(endLabel);
            Canvas.SetTop(endLabel, 350);
            Canvas.SetLeft(endLabel, 450);
            Canvas.SetZIndex(endLabel, 301);

        }

        public void ShowInstructions(UIElementCollection children, int run)
        {
            switch (run)
            {

                case 1:
                    fallingThings.DropNewThing(PolyType.Square, 100, 440, Color.FromRgb(255, 255, 0), children, "Move");
                    break;
                case 2:
                    fallingThings.DropNewThing(PolyType.Square, 100, 640, Color.FromRgb(255, 255, 0), children, "your");
                    break;
                case 3:
                    fallingThings.DropNewThing(PolyType.Square, 100, 840, Color.FromRgb(255, 255, 0), children, "hands");
                    break;
                case 4:
                    fallingThings.DropNewThing(PolyType.Square, 100, 440, Color.FromRgb(255, 255, 0), children, "to");
                    break;
                case 5:
                    fallingThings.DropNewThing(PolyType.Square, 100, 640, Color.FromRgb(255, 255, 0), children, "catch");
                    break;
                case 6:
                    fallingThings.DropNewThing(PolyType.Square, 100, 840, Color.FromRgb(255, 255, 0), children, "the");
                    break;
                case 7:
                    fallingThings.DropNewThing(PolyType.Square, 100, 540, Color.FromRgb(255, 255, 0), children, "falling");
                    break;
                case 8:
                    fallingThings.DropNewThing(PolyType.Square, 100, 740, Color.FromRgb(255, 255, 0), children, "apples!");
                    break;

                case 9:
                    SetTimerLabel();
                    StartGameTimer();
                    LoadScore();
                    LoadAssociation();
                    instructing = false;
                    this.instructionTimer.Stop();
                    break;

                case 10:
                    break;

                case 11:
                    /*timerLabel.Visibility = System.Windows.Visibility.Hidden;
                    timerOutline.Visibility = System.Windows.Visibility.Hidden;
                    scoreLabel.Visibility = System.Windows.Visibility.Hidden;
                    scoreOutline.Visibility = System.Windows.Visibility.Hidden;
                    associationLabel.Visibility = System.Windows.Visibility.Hidden;
                    associationOutline.Visibility = System.Windows.Visibility.Hidden;
                    ShowGoodbye();*/
                    ReturnToStandbyController();
                    break;

                case 12:
                    ReturnToStandbyController();
                    break;

                default:
                    break;

            }

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
                fallingThings.AdvanceFrame(playfield.Children,instructing);

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
