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
        private System.Windows.Controls.Image timerBox;
        private System.Windows.Controls.Image scoreBox;
        private System.Windows.Controls.Image wordBox;
        private List<Bubble> bubbles;
        private List<Boolean> selectedBubbles;
        private List<Bubble> instructionBubbles;
        private int MAX_BUBBLES = 6;
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
        private static int REMOVE_INTERVAL = 2500;
        private static int ADD_INTERVAL = 1000;
        private double highlightedHandBaseDepth;
        private double depthDeltaForSelection = .3;
        private int secondsLeft;
        private System.Media.SoundPlayer sp;

        public PopTheBubblesController(Minigame game)
        {
            this.minigame = game;
            this.instructionBubbles = new List<Bubble>();
            this.selectedBubbles = new List<Boolean>();
            this.sp = new System.Media.SoundPlayer(ShoopDoup.Properties.Resources.bobblepopwav);
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
                rightHandCursor.Visibility = System.Windows.Visibility.Visible;
                leftHandCursor.Visibility = System.Windows.Visibility.Visible;
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
            bubbleTextBlock.Foreground = System.Windows.Media.Brushes.DarkGreen;
            bubbleTextBlock.FontFamily = new System.Windows.Media.FontFamily("Arial");
            Viewbox bubbleViewBox = new Viewbox();
            bubbleViewBox.Stretch = Stretch.Uniform;
            bubbleViewBox.Height = 70;
            bubbleViewBox.Width = 150;
            bubbleViewBox.Child = bubbleTextBlock;
            this.associateWithLabel.Content = bubbleViewBox;
            mainCanvas.Children.Add(associateWithLabel);
            Canvas.SetTop(associateWithLabel, 14);
            Canvas.SetLeft(associateWithLabel, 545);
            Canvas.SetZIndex(associateWithLabel, 5);

            this.wordBox = new System.Windows.Controls.Image();
            wordBox.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.greenText);
            wordBox.Height = 70;
            mainCanvas.Children.Add(wordBox);

            Canvas.SetTop(wordBox, 18);
            Canvas.SetLeft(wordBox, 400);
            Canvas.SetZIndex(wordBox, 4);
        }


        private void LoadScore()
        {
            score = 0;
            scoreLabel = new Label();
            scoreLabel.Content = "000" + score;
            scoreLabel.FontSize = 40;
            scoreLabel.Foreground = System.Windows.Media.Brushes.DarkGreen;
            scoreLabel.FontFamily = new System.Windows.Media.FontFamily("Arial");

            this.scoreBox = new System.Windows.Controls.Image();
            scoreBox.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.ThreeDigitGreen);
            scoreBox.Height = 70;
            mainCanvas.Children.Add(scoreBox);

            mainCanvas.Children.Add(scoreLabel);
            Canvas.SetTop(scoreLabel, 25);
            Canvas.SetLeft(scoreLabel, 91);
            Canvas.SetZIndex(scoreLabel, 5);

            Canvas.SetTop(scoreBox, 18);
            Canvas.SetLeft(scoreBox, 70);
            Canvas.SetZIndex(scoreBox, 4);
        }

        private void LoadCursors()
        {
            rightHandCursor = new System.Windows.Controls.Image();
            rightHandCursor.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.GreenHandCursor);
            rightHandCursor.Width = 100;
            rightHandCursor.Visibility = System.Windows.Visibility.Hidden;

            leftHandCursor = new System.Windows.Controls.Image();
            System.Drawing.Bitmap leftHandBitmap = ShoopDoup.Properties.Resources.GreenHandCursor;
            leftHandBitmap.RotateFlip(RotateFlipType.RotateNoneFlipX);
            leftHandCursor.Source = this.toBitmapImage(leftHandBitmap);
            leftHandCursor.Width = 100;
            leftHandCursor.Visibility = System.Windows.Visibility.Hidden;

            mainCanvas.Children.Add(rightHandCursor);
            mainCanvas.Children.Add(leftHandCursor);
            Canvas.SetZIndex(leftHandCursor, 4);
            Canvas.SetZIndex(rightHandCursor, 4);
        }

        private void countdown(object sender, EventArgs e)
        {
            timeLeft--;
            if (timeLeft < 10)
            {
                timerLabel.Content = ":0" + timeLeft;
            }
            else
            {
                timerLabel.Content = ":" + timeLeft;
            }
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
            int ra = randomGen.Next(0, bubbles.Count - 1);
            if (bubbles.Count > 3)
            {
                if (selectedBubbles[ra] != true)
                {
                    mainCanvas.Children.Remove(bubbles[ra].bubble);
                    mainCanvas.Children.Remove(bubbles[ra].bubbleLabel);
                    bubbles.RemoveAt(ra);
                    selectedBubbles.RemoveAt(ra);
                }
            }
        }

        private void SetTimerLabel()
        {
            timeLeft = 60;
            this.timerLabel = new Label();
            timerLabel.FontSize = 40;
            timerLabel.Foreground = System.Windows.Media.Brushes.DarkGreen;
            timerLabel.Content = ":" + timeLeft;
            timerLabel.FontFamily = new System.Windows.Media.FontFamily("Arial");
            
            this.timerBox = new System.Windows.Controls.Image();
            timerBox.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.TwoDigitGreen);
            timerBox.Height = 70;
            mainCanvas.Children.Add(timerBox);

            mainCanvas.Children.Add(timerLabel);
            Canvas.SetTop(timerLabel, 25);
            Canvas.SetLeft(timerLabel, 240);
            Canvas.SetZIndex(timerLabel, 5);
            Canvas.SetTop(timerBox, 18);
            Canvas.SetLeft(timerBox, 220);
            Canvas.SetZIndex(timerBox, 4);
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
                if (x >= bubbles[i].x - 160 && x <= bubbles[i].x + 160 && y >= bubbles[i].y - 160 && y <= bubbles[i].y + 160)
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
                if (x > bubbles[i].x - 30 && x < bubbles[i].x + 170 && y > bubbles[i].y - 30 && y < bubbles[i].y + 170)
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
            /*for (int i = 0; i < bubbles.Count; i++)
            {
                Console.WriteLine("BUBBLE: " + i + " - " + selectedBubbles[i]);
            }*/

            if (bubbles.Count < MAX_BUBBLES)
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
                selectedBubbles.Add(false);
                //bubbleTimer.Start();
            }
        }

        public override void updateSkeleton(SkeletonData skeleton)
        {
            if (state == GAME_STATE.GamePlay)
            {
                highlightedHandBaseDepth = skeleton.Joints[JointID.Spine].ScaleTo(1280, 800, .5f, .5f).Position.Z;

                Joint rightHand = skeleton.Joints[JointID.HandRight].ScaleTo(1280, 800, .5f, .5f);
                Joint leftHand = skeleton.Joints[JointID.HandLeft].ScaleTo(1280, 800, .5f, .5f);

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
                            selectedBubbles.RemoveAt(hit1);
                            score++;
                            if (score < 10)
                            {
                                scoreLabel.Content = "000" + score;
                            }
                            else if (score < 100)
                            {
                                scoreLabel.Content = "00" + score;
                            }
                            else if (score < 1000)
                            {
                                scoreLabel.Content = "0" + score;
                            }
                            else
                            {
                                scoreLabel.Content = "" + score;
                            }
                            sp.Play();
                        }
                        else
                        {
                            textBlock.Foreground = System.Windows.Media.Brushes.Red;
                            selectedBubbles[hit1] = true;
                        }
                    }
                    else
                    {
                        textBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        selectedBubbles[hit1] = true;
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
                            selectedBubbles.RemoveAt(hit2);
                            score++;
                            if (score < 10)
                            {
                                scoreLabel.Content = "000" + score;
                            }
                            else if (score < 100)
                            {
                                scoreLabel.Content = "00" + score;
                            }
                            else if (score < 1000)
                            {
                                scoreLabel.Content = "0" + score;
                            }
                            else
                            {
                                scoreLabel.Content = "" + score;
                            }
                            sp.Play();
                        }
                        else
                        {
                            textBlock.Foreground = System.Windows.Media.Brushes.Red;
                            selectedBubbles[hit2] = true;
                        }
                    }
                    else
                    {
                        textBlock.Foreground = System.Windows.Media.Brushes.Yellow;
                        selectedBubbles[hit2] = true;
                    }
                }
                for (int i = 0; i < bubbles.Count; i++)
                {
                    if (i != hit1 && i != hit2)
                    {
                        TextBlock tb = ((TextBlock)((Viewbox)bubbles[i].bubbleLabel.Content).Child);
                        tb.Foreground = System.Windows.Media.Brushes.Black;
                        selectedBubbles[i] = false;
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
