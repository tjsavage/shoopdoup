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
        private Bubble instructionBubble;
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
            state = GAME_STATE.Instruction;
            StartInstructionTimer();
            bubbles = new List<Bubble>();
            LoadScore();
            LoadBackground();
            LoadCursors();
            SetAssociateLabel();
            SetTimerLabel();
            //StartGameTimer();
            //StartBubbleTimer();
            //StartRemoveTimer();
        }

        private void StartInstructionTimer()
        {
            secondsLeft = 15;
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
            instructionBubble = new Bubble(x, y, bubble, label);
        }

        private void instructUser(Object sender, EventArgs e)
        {
            mainCanvas.Children.Remove(instructionBubble.bubble);
            mainCanvas.Children.Remove(instructionBubble.bubbleLabel);
            switch (secondsLeft)
            {
                case 15: createInstructionBubble("Pop", 50, 150); break;
                case 14: createInstructionBubble("The", 415, 200); break;
                case 13: createInstructionBubble("Bubbles", 600, 250); break;
                case 12: createInstructionBubble("That", 500, 100); break;
                case 11: createInstructionBubble("Relate", 300, 300); break;
                case 10: createInstructionBubble("To", 50, 250); break;
                case 9: createInstructionBubble("The", 650, 100); break;
                case 8: createInstructionBubble("Word", 300, 300); break;
                case 7: createInstructionBubble("At", 150, 150); break;
                case 6: createInstructionBubble("The", 100, 275); break;
                case 5: createInstructionBubble("Top", 300, 250); break;
                case 4: createInstructionBubble("Ready", 50, 175); break;
                case 3: createInstructionBubble("GO!!", 350, 250); break;
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
            associateWithLabel.FontSize = 40;
            associateWithLabel.Foreground = System.Windows.Media.Brushes.White;
            associateWithLabel.Content = "Germany";
            mainCanvas.Children.Add(associateWithLabel);
            Canvas.SetTop(associateWithLabel, 0);
            Canvas.SetLeft(associateWithLabel, 300);
            Canvas.SetZIndex(associateWithLabel, 4);
        }


        private void LoadScore()
        {
            score = 0;
            scoreLabel = new Label();
            scoreLabel.Content = "SCORE: " + score;
            scoreLabel.FontSize = 40;
            scoreLabel.Foreground = System.Windows.Media.Brushes.Green;
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
                ReturnToStandbyController();
            }
        }

        private void LoadBackground()
        {
            background = new System.Windows.Controls.Image();
            background.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.OceanBackground);
            background.Width = 785;
            System.Windows.Controls.Image fish1 = new System.Windows.Controls.Image();
            fish1.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish1.Width = 50;
            mainCanvas.Children.Add(fish1);
            Canvas.SetTop(fish1, 150);
            Canvas.SetLeft(fish1, 150);
            Canvas.SetZIndex(fish1, 2);
            System.Windows.Controls.Image fish2 = new System.Windows.Controls.Image();
            fish2.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish2.Width = 75;
            mainCanvas.Children.Add(fish2);
            Canvas.SetTop(fish2, 275);
            Canvas.SetLeft(fish2, 400);
            Canvas.SetZIndex(fish2, 2);
            System.Windows.Controls.Image fish3 = new System.Windows.Controls.Image();
            fish3.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish3.Width = 60;
            mainCanvas.Children.Add(fish3);
            Canvas.SetTop(fish3, 125);
            Canvas.SetLeft(fish3, 600);
            Canvas.SetZIndex(fish3, 2);
            System.Windows.Controls.Image fish4 = new System.Windows.Controls.Image();
            fish4.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish4.Width = 80;
            mainCanvas.Children.Add(fish4);
            Canvas.SetTop(fish4, 350);
            Canvas.SetLeft(fish4, 100);
            Canvas.SetZIndex(fish4, 2);
            System.Windows.Controls.Image fish5 = new System.Windows.Controls.Image();
            fish5.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.fish);
            fish5.Width = 80;
            mainCanvas.Children.Add(fish5);
            Canvas.SetTop(fish5, 100);
            Canvas.SetLeft(fish5, 300);
            Canvas.SetZIndex(fish5, 2);
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
            timerLabel.Foreground = System.Windows.Media.Brushes.Green;
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
            for (int i = 0; i < bubbles.Count; i++)
            {
                if (x >= bubbles[i].x - 100 && x <= bubbles[i].x + 100 && y >= bubbles[i].y - 100 && y <= bubbles[i].y + 100)
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
                if (x > bubbles[i].x && x < bubbles[i].x + 100 && y > bubbles[i].y && y < bubbles[i].y + 100)
                {
                    return i;
                }
            }
            return (-1);
        }

        private Boolean hitWords(String text)
        {
            if (text.Equals("Germany")) 
            {
                return true;
            }
            for (int i = 0; i < bubbles.Count; i++)
            {
                TextBlock textBlock = ((TextBlock)((Viewbox)bubbles[i].bubbleLabel.Content).Child);
                if (textBlock.Text.Equals(text)) return true;
            }
            return false;
        }

        private void bubblePopup(object sender, EventArgs e)
        {
            if (bubbles.Count <= MAX_BUBBLES)
            {
                int minigameRandom = randomGen.Next(0,minigame.getData().Count-1);
                String text = minigame.getData()[minigameRandom].getElementValue();

                while (hitWords(text))
                {
                    minigameRandom = randomGen.Next(0, minigame.getData().Count - 1);
                    text = minigame.getData()[minigameRandom].getElementValue();
                }

                System.Windows.Controls.Image bubble = new System.Windows.Controls.Image();
                bubble.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.bubble);
                bubble.Width = 100;
                mainCanvas.Children.Add(bubble);
                int x = randomGen.Next(20, 660);
                int y = randomGen.Next(80, 380);

                while (hitBubble(x, y) != (-1))
                {
                    x = randomGen.Next(20, 660);
                    y = randomGen.Next(80, 380);
                }

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
                Canvas.SetTop(label, y+5);
                Canvas.SetLeft(label, x+5);
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
                highlightedHandBaseDepth = skeleton.Joints[JointID.Spine].ScaleTo(640, 480, .5f, .5f).Position.Z;

                Joint rightHand = skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f);
                Joint leftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f);

                Canvas.SetTop(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.Y);
                Canvas.SetLeft(rightHandCursor, skeleton.Joints[JointID.HandRight].ScaleTo(640, 480, .5f, .5f).Position.X);
                Canvas.SetTop(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.Y);
                Canvas.SetLeft(leftHandCursor, skeleton.Joints[JointID.HandLeft].ScaleTo(640, 480, .5f, .5f).Position.X);

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
                        bubbles[hit1].setSelected(true);
                    }
                }
                for (int i = 0; i < bubbles.Count; i++)
                {
                    if (i != hit1 && i != hit2)
                    {
                        TextBlock tb = ((TextBlock)((Viewbox)bubbles[i].bubbleLabel.Content).Child);
                        tb.Foreground = System.Windows.Media.Brushes.Black;
                        bubbles[hit1].setSelected(false);
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
