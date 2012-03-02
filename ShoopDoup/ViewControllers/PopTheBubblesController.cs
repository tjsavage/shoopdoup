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
    public struct Bubble
    {
        public int x, y;
        public System.Windows.Controls.Image bubble;
        public Label bubbleLabel;

        public Bubble(int p1, int p2, System.Windows.Controls.Image theImage, Label theLabel)
        {
            x = p1;
            y = p2;
            bubble = theImage;
            bubbleLabel = theLabel;
        }
    }

    class PopTheBubblesController : SceneController
    {

        private enum GAME_STATE { Instruction, GamePlay, GameOver };
        private System.Windows.Controls.Image background;
        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;
        private List<Bubble> bubbles;
        private int MAX_BUBBLES = 10;
        private Random randomGen = new Random();
        private System.Windows.Threading.DispatcherTimer bubbleTimer;
        private System.Windows.Threading.DispatcherTimer gameTimer;
        private System.Windows.Threading.DispatcherTimer removeTimer;
        private int timeLeft;
        private Label timerLabel;
        private int score;
        private Label scoreLabel;
        private static int REMOVE_INTERVAL = 2000;
        private static int ADD_INTERVAL = 1000;

        public PopTheBubblesController()
        {
            bubbles = new List<Bubble>();
            LoadScore();
            LoadBackground();
            LoadCursors();
            SetTimerLabel();
            StartGameTimer();
            StartBubbleTimer();
            StartRemoveTimer();
        }

        private void LoadScore()
        {
            score = 0;
            scoreLabel = new Label();
            scoreLabel.Content = "SCORE: " + score;
            scoreLabel.FontSize = 40;
            scoreLabel.Foreground = System.Windows.Media.Brushes.White;
            mainCanvas.Children.Add(scoreLabel);
            Canvas.SetTop(scoreLabel, 500);
            Canvas.SetLeft(scoreLabel, 600);
            Canvas.SetZIndex(scoreLabel, 4);
        }

        private void LoadCursors()
        {
            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.HandCursor);
            rightHandCursor.Width = 100;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.HandCursor;
            leftHandBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;

            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
            Canvas.SetZIndex(leftHandCursor, 4);
            Canvas.SetZIndex(rightHandCursor, 4);
        }

        private void countdown(object sender, EventArgs e)
        {
            timeLeft--;
            timerLabel.Content = "TIME: " + timeLeft;
            if (timeLeft == 0)
            {
                bubbleTimer.Stop();
                gameTimer.Stop();
                removeTimer.Stop();
            }
        }

        private void LoadBackground()
        {
            background = new System.Windows.Controls.Image();
            background.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.ocean2);
            background.Width = 800;
            mainCanvas.Children.Add(background);
            Canvas.SetTop(background, 0);
            Canvas.SetLeft(background, 0);
            Canvas.SetZIndex(background, 1);
        }

        private void StartBubbleTimer()
        {
            this.bubbleTimer = new System.Windows.Threading.DispatcherTimer();
            this.bubbleTimer.Tick += bubblePopup;
            this.bubbleTimer.Interval = TimeSpan.FromMilliseconds(ADD_INTERVAL);
            this.bubbleTimer.Start();
        }

        private void StartRemoveTimer()
        {
            this.removeTimer = new System.Windows.Threading.DispatcherTimer();
            this.removeTimer.Tick += bubbleRandomRemove;
            this.removeTimer.Interval = TimeSpan.FromMilliseconds(REMOVE_INTERVAL);
            this.removeTimer.Start();
        }

        private void bubbleRandomRemove(object sender, EventArgs e)
        {
            if (bubbles.Count >= 6)
            {
                mainCanvas.Children.Remove(bubbles[0].bubble);
                mainCanvas.Children.Remove(bubbles[0].bubbleLabel);
                bubbles.RemoveAt(0);
                removeTimer.Start();
            }
        }

        private void SetTimerLabel()
        {
            timeLeft = 60;
            this.timerLabel = new Label();
            timerLabel.FontSize = 40;
            timerLabel.Foreground = System.Windows.Media.Brushes.White;
            timerLabel.Content = "TIME: " + timeLeft;
            mainCanvas.Children.Add(timerLabel);
            Canvas.SetTop(timerLabel, 500);
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


        private int hitBubble(int x, int y)
        {
            //Console.WriteLine("TRYING: " + x);
            for (int i = 0; i < bubbles.Count; i++)
            {
                //Console.WriteLine("HERE: " + bubbles[i].x);
                if (x >= bubbles[i].x - 100 && x <= bubbles[i].x + 100 && y >= bubbles[i].y - 100 && y <= bubbles[i].y + 100)
                {
                    //Console.WriteLine("SUCCESS");
                    return i;
                }
                //Console.WriteLine("FAIL");
            }
            return (-1);

        }

        private void bubblePopup(object sender, EventArgs e)
        {
            if (bubbles.Count <= MAX_BUBBLES)
            {
                System.Windows.Controls.Image bubble = new System.Windows.Controls.Image();
                bubble.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.bubble);
                bubble.Width = 100;
                mainCanvas.Children.Add(bubble);
                int x = randomGen.Next(0, 680);
                int y = randomGen.Next(0, 410);
                //int x = bubbles.Count * 50;
                //int y = 0;

                while (hitBubble(x, y) != (-1))
                {
                    //Console.WriteLine("X:" + x);
                    //x = x + 150;
                    x = randomGen.Next(0, 680);
                    y = randomGen.Next(0, 410);
                }

                Label label = new Label();
                label.Content = new TextBlock();
                label.Height = 100;
                label.Width = 100;
                ((TextBlock)label.Content).Text = "South Africaaaaaaaa";
                ((TextBlock)label.Content).TextWrapping = 0;
                ((TextBlock)label.Content).FontSize = 20;
                label.HorizontalAlignment = HorizontalAlignment.Center;
                ((TextBlock)label.Content).VerticalAlignment = VerticalAlignment.Center;
                //label.FontSize = 20;

                
                mainCanvas.Children.Add(label);
                
                Canvas.SetTop(bubble, y);
                Canvas.SetLeft(bubble, x);
                Canvas.SetZIndex(bubble, 2);
                Canvas.SetTop(label, y);
                Canvas.SetLeft(label, x);
                Canvas.SetZIndex(label, 3);
                Bubble newBubble = new Bubble(x, y, bubble,label);
                bubbles.Add(newBubble);
                bubbleTimer.Start();
            }
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {
            /*
            if (rightHandCursor.Visibility == System.Windows.Visibility.Hidden)
            {
                rightHandCursor.Visibility = System.Windows.Visibility.Visible;
                leftHandCursor.Visibility = System.Windows.Visibility.Visible;
            }*/
            Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y);
            Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X);
            Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y);
            Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.X);
        }

        public override void updateWithoutSkeleton()
        {
        }
    }
}
