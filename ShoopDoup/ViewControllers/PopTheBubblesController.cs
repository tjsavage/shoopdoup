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
    public struct Bubble
    {
        public int x, y;
        public System.Windows.Controls.Image bubble;
        public Label bubbleLabel;
        public Boolean isSelected;

        public Bubble(int p1, int p2, System.Windows.Controls.Image theImage, Label theLabel)
        {
            x = p1;
            y = p2;
            bubble = theImage;
            bubbleLabel = theLabel;
            isSelected = false;
        }

        public void setSelected(Boolean selected)
        {
            isSelected = selected;
        }
    }

    class PopTheBubblesController : SceneController
    {

        private Minigame minigame;
        private enum GAME_STATE { Instruction, GamePlay, GameOver };
        private GAME_STATE state;
        private System.Windows.Controls.Image background;
        private System.Windows.Controls.Image leftHandCursor;
        private System.Windows.Controls.Image rightHandCursor;
        private List<Bubble> bubbles;
        private List<Bubble> instructionBubbles;
        private int MAX_BUBBLES = 10;
        private Random randomGen = new Random();
        private System.Windows.Threading.DispatcherTimer bubbleTimer;
        private System.Windows.Threading.DispatcherTimer gameTimer;
        private System.Windows.Threading.DispatcherTimer removeTimer;
        private System.Windows.Threading.DispatcherTimer instructionTimer;
        private int timeLeft;
        private Label timerLabel;
        private int score;
        private Label scoreLabel;
        private Label associateWithLabel;
        private static int REMOVE_INTERVAL = 2000;
        private static int ADD_INTERVAL = 1000;
        private double highlightedHandBaseDepth;
        private double depthDeltaForSelection = .4;
        private int secondsLeft;

        public PopTheBubblesController(Minigame game)
        {
            this.minigame = game;
            this.instructionBubbles = new List<Bubble>();
            state = GAME_STATE.Instruction;
            StartInstructionTimer();
            bubbles = new List<Bubble>();
            LoadScore();
            LoadBackground();
            LoadCursors();
            SetAssociateLabel();
            SetTimerLabel();
        }

        private void StartInstructionTimer()
        {
            secondsLeft = 11;
            this.instructionTimer = new System.Windows.Threading.DispatcherTimer();
            this.instructionTimer.Tick += instructUser;
            this.instructionTimer.Interval = TimeSpan.FromMilliseconds(1000);
            this.instructionTimer.Start();
        }

        private void createInstructionBubble(String text, int x, int y)
        {
            System.Windows.Controls.Image bubble = new System.Windows.Controls.Image();
            bubble.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.bubble);
            bubble.Width = 100;
            mainCanvas.Children.Add(bubble);

            Label label = new Label();
            TextBlock bubbleTextBlock = new TextBlock();
            bubbleTextBlock.Text = text;
            Viewbox bubbleViewBox = new Viewbox();
            bubbleViewBox.Stretch = Stretch.Uniform;
            bubbleViewBox.Height = 80;
            bubbleViewBox.Width = 80;
            bubbleViewBox.Child = bubbleTextBlock;
            label.Content = bubbleViewBox;

            mainCanvas.Children.Add(label);

            Canvas.SetTop(bubble, y);
            Canvas.SetLeft(bubble, x);
            Canvas.SetZIndex(bubble, 2);
            Canvas.SetTop(label, y + 5);
            Canvas.SetLeft(label, x + 5);
            Canvas.SetZIndex(label, 3);
            instructionBubbles.Add(new Bubble(x, y, bubble, label));
        }

        private void removeBubble(int i)
        {
            mainCanvas.Children.Remove(instructionBubbles[i].bubble);
            mainCanvas.Children.Remove(instructionBubbles[i].bubbleLabel);
            //instructionBubbles.RemoveAt(i);
        }

        private void instructUser(Object sender, EventArgs e)
        {
            //mainCanvas.Children.Remove(instructionBubble.bubble);
            //mainCanvas.Children.Remove(instructionBubble.bubbleLabel);
            switch (secondsLeft)
            {
                case 11: createInstructionBubble("Pop The", 500, 150); break;
                case 10: createInstructionBubble("Bubbles", 600, 200); break;
                case 9: createInstructionBubble("That", 700, 150); break;
                case 8: createInstructionBubble("Relate", 800, 200); break;
                case 7: createInstructionBubble("To The", 700, 270); break;
                case 6: createInstructionBubble("Word", 800, 320); break;
                case 5: createInstructionBubble("At The Top!", 900, 270); break;
                case 4: createInstructionBubble("Ready...", 1050, 400); break;
                case 2: for (int i = 0; i < instructionBubbles.Count; i++) removeBubble(i); break;
            }
            secondsLeft--;
            if (secondsLeft == 0)
            {
                state = GAME_STATE.GamePlay;
                StartGameTimer();
                StartBubbleTimer();
                StartRemoveTimer();
                this.instructionTimer.Stop();
            }
        }

        private void SetAssociateLabel()
        {
            this.associateWithLabel = new Label();
            int minigameRandom = randomGen.Next(0, minigame.getData().Count - 1);
            String text = minigame.getData()[minigameRandom].getElementValue().ToLower();

            TextBlock bubbleTextBlock = new TextBlock();
            bubbleTextBlock.Text = text;
            bubbleTextBlock.Foreground = System.Windows.Media.Brushes.White;
            Viewbox bubbleViewBox = new Viewbox();
            bubbleViewBox.Stretch = Stretch.Uniform;
            bubbleViewBox.Height = 100;
            bubbleViewBox.Width = 300;
            bubbleViewBox.Child = bubbleTextBlock;
            this.associateWithLabel.Content = bubbleViewBox;
            mainCanvas.Children.Add(associateWithLabel);
            Canvas.SetTop(associateWithLabel, 30);
            Canvas.SetLeft(associateWithLabel, 450);
            Canvas.SetZIndex(associateWithLabel, 4);
        }


        private void LoadScore()
        {
            score = 0;
            scoreLabel = new Label();
            scoreLabel.Content = "SCORE: " + score;
            scoreLabel.FontSize = 40;
            scoreLabel.Foreground = System.Windows.Media.Brushes.White;
            mainCanvas.Children.Add(scoreLabel);
            Canvas.SetTop(scoreLabel, 700);
            Canvas.SetLeft(scoreLabel, 1000);
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
                ReturnToStandbyController();
            }
        }

        private void LoadBackground()
        {
            background = new System.Windows.Controls.Image();
            background.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.OceanBackground);
            background.Width = 1270;
            background.Height = 760;
            System.Windows.Controls.Image fish1 = new System.Windows.Controls.Image();
            fish1.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish1.Width = 150;
            mainCanvas.Children.Add(fish1);
            Canvas.SetTop(fish1, 300);
            Canvas.SetLeft(fish1, 300);
            Canvas.SetZIndex(fish1, 2);
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
            Canvas.SetTop(timerLabel, 700);
            Canvas.SetLeft(timerLabel, 70);
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
            for (int i = 0; i < bubbles.Count; i++)
            {
                if (x >= bubbles[i].x - 140 && x <= bubbles[i].x + 140 && y >= bubbles[i].y - 140 && y <= bubbles[i].y + 140)
                {
                    return i;
                }
            }
            return (-1);

        }

        private int getBubble(int x, int y)
        {
            for (int i = 0; i < bubbles.Count; i++)
            {
                if (x > bubbles[i].x && x < bubbles[i].x + 140 && y > bubbles[i].y && y < bubbles[i].y + 140)
                {
                    return i;
                }
            }
            return (-1);
        }

        private Boolean hitWords(String text)
        {
            TextBlock textBlock1 = ((TextBlock)((Viewbox)associateWithLabel.Content).Child);
            if (textBlock1.Text.Equals(text)) return true;

            for (int i = 0; i < bubbles.Count; i++)
            {
                TextBlock textBlock2 = ((TextBlock)((Viewbox)bubbles[i].bubbleLabel.Content).Child);
               
                if (textBlock2.Text.Equals(text)) return true;
            }
            return false;
        }

        private void bubblePopup(object sender, EventArgs e)
        {
            if (bubbles.Count <= MAX_BUBBLES)
            {
                int minigameRandom = randomGen.Next(0,minigame.getData().Count-1);
                String text = minigame.getData()[minigameRandom].getElementValue().ToLower();

                while (hitWords(text))
                {
                    minigameRandom = randomGen.Next(0, minigame.getData().Count - 1);
                    text = minigame.getData()[minigameRandom].getElementValue().ToLower();
                }

                System.Windows.Controls.Image bubble = new System.Windows.Controls.Image();
                bubble.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.bubble);
                bubble.Width = randomGen.Next(60,140);
                mainCanvas.Children.Add(bubble);
                int x = randomGen.Next(60, 1050);
                int y = randomGen.Next(115, 560);

                while (hitBubble(x, y) != (-1) || ((y > 160) && (y < 450) && (x > 160) && (x < 450)) || ((y > 350) && (x > 725) && (x < 1200)))
                {
                    x = randomGen.Next(60, 1050);
                    y = randomGen.Next(115, 560);
                }

                Label label = new Label();
                TextBlock bubbleTextBlock = new TextBlock();
                bubbleTextBlock.Text = text;
                bubbleTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
                Viewbox bubbleViewBox = new Viewbox();
                bubbleViewBox.Stretch = Stretch.Uniform;
                bubbleViewBox.Height = 100;
                bubbleViewBox.Width = 100;
                bubbleViewBox.Child = bubbleTextBlock;
                label.Content = bubbleViewBox;

                mainCanvas.Children.Add(label);
                
                Canvas.SetTop(bubble, y + ((140-bubble.Width)/2));
                Canvas.SetLeft(bubble, x + ((140-bubble.Width)/2));
                Canvas.SetZIndex(bubble, 2);
                Canvas.SetTop(label, y+((140-112)/2));
                Canvas.SetLeft(label, x+((140-112)/2));
                Canvas.SetZIndex(label, 3);
                Bubble newBubble = new Bubble(x, y, bubble,label);
                bubbles.Add(newBubble);
                //bubbleTimer.Start();
            }
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {
            if (state == GAME_STATE.GamePlay)
            {
                highlightedHandBaseDepth = skeleton.Joints[JointID.Spine].ScaleTo(1280, 800, .5f, .5f).Position.Z;

                Joint rightHand = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f);
                Joint leftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f);

                Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.Y);
                Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f).Position.X);
                Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.Y);
                Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f).Position.X);

                int x1 = Convert.ToInt32(Canvas.GetLeft(rightHandCursor));
                int y1 = Convert.ToInt32(Canvas.GetTop(rightHandCursor));
                int x2 = Convert.ToInt32(Canvas.GetLeft(leftHandCursor));
                int y2 = Convert.ToInt32(Canvas.GetTop(leftHandCursor));
                int hit1 = getBubble(x1, y1);
                int hit2 = getBubble(x2, y2);

                if (hit1 != (-1))
                {
                    TextBlock textBlock = ((TextBlock)((Viewbox)bubbles[hit1].bubbleLabel.Content).Child);
                    if (Math.Abs(rightHand.Position.Z - highlightedHandBaseDepth) > depthDeltaForSelection)
                    {
                        if (textBlock.Foreground == System.Windows.Media.Brushes.Yellow)
                        {
                            mainCanvas.Children.Remove(bubbles[hit1].bubble);
                            mainCanvas.Children.Remove(bubbles[hit1].bubbleLabel);
                            bubbles.RemoveAt(hit1);
                            score++;
                            scoreLabel.Content = "SCORE: " + score;
                        }
                        else
                        {
                            textBlock.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    else
                    {
                        textBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        bubbles[hit1].setSelected(true);
                    }
                }
                if (hit2 != (-1))
                {
                    TextBlock textBlock = ((TextBlock)((Viewbox)bubbles[hit2].bubbleLabel.Content).Child);
                    if (Math.Abs(leftHand.Position.Z - highlightedHandBaseDepth) > depthDeltaForSelection)
                    {
                        if (textBlock.Foreground == System.Windows.Media.Brushes.Yellow)
                        {
                            mainCanvas.Children.Remove(bubbles[hit2].bubble);
                            mainCanvas.Children.Remove(bubbles[hit2].bubbleLabel);
                            bubbles.RemoveAt(hit2);
                            score++;
                            scoreLabel.Content = "SCORE: " + score;
                        }
                        else
                        {
                            textBlock.Foreground = System.Windows.Media.Brushes.Red;
                        }
                    }
                    else
                    {
                        textBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        bubbles[hit2].setSelected(true);
                    }
                }
                for (int i = 0; i < bubbles.Count; i++)
                {
                    if (i != hit1 && i != hit2)
                    {
                        TextBlock tb = ((TextBlock)((Viewbox)bubbles[i].bubbleLabel.Content).Child);
                        tb.Foreground = System.Windows.Media.Brushes.Black;
                        bubbles[i].setSelected(false);
                    }
                }
            }
        }

        public override void updateWithoutSkeleton()
        {
            ReturnToStandbyController();
        }
    }
}
