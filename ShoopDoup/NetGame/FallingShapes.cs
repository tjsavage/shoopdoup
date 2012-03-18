/////////////////////////////////////////////////////////////////////////
//
// This module contains code to do display falling shapes, and do
// hit testing against a set of segments provided by the Kinect NUI, and
// have shapes react accordingly.
//
// Copyright © Microsoft Corporation.  All rights reserved.  
// This code is licensed under the terms of the 
// Microsoft Kinect for Windows SDK (Beta) 
// License Agreement: http://kinectforwindows.org/KinectSDK-ToU
//
/////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using ShoopDoup.Models;
using Microsoft.Research.Kinect.Nui;

/// <summary>
/// Falling shapes, and intersection hit testing with body segments
/// </summary>
/// 
namespace NetGame.Utils
{

    public enum PolyType
    {
        None        = 0x00,
        Triangle    = 0x01,
        Square      = 0x02,
        Star        = 0x04,
        Pentagon    = 0x08,
        Hex         = 0x10,
        Star7       = 0x20,
        Circle      = 0x40,
        Bubble      = 0x80,
        All         = 0x7f
    }

    public enum HitType
    {
        None = 0x00,
        Hand = 0x01,
        Arm = 0x02,
        Squeezed = 0x04,
        Popped = 0x08
    }

    // BannerText generates a scrolling or still banner of text (along the bottom of the screen).
    // Only one banner exists at a time.  Calling NewBanner() will erase the old one and start the new one.

    public class BannerText
    {
        private static BannerText bannerText = null;
        private Brush brush;
        private Color color;
        private Label label;
        private Rect boundsRect;
        private Rect renderedRect;
        private bool doScroll;
        private double offset = 0;
        private string text;

        public BannerText(string s, Rect rect, bool scroll, Color col)
        {
            text = s;
            boundsRect = rect;
            doScroll = scroll;
            brush = null;
            label = null;
            color = col;
            offset = (doScroll) ? 1.0 : 0.0;
        }

        public static void NewBanner(string s, Rect rect, bool scroll, Color col)
        {
            bannerText = (s != null) ? new BannerText(s, rect, scroll, col) : null;
        }

        private Label GetLabel()
        {
            if (brush == null)
                brush = new SolidColorBrush(color);

            if (label == null)
            {
                label = FallingThings.MakeSimpleLabel(text, boundsRect, brush);
                if (doScroll)
                {
                    label.FontSize = Math.Max(20, boundsRect.Height / 30);
                    label.Width = 10000;
                }
                else
                    label.FontSize = Math.Min(Math.Max(10, boundsRect.Width * 2 / text.Length),
                                              Math.Max(10, boundsRect.Height / 20));
                label.VerticalContentAlignment= VerticalAlignment.Bottom;
                label.HorizontalContentAlignment = (doScroll) ? HorizontalAlignment.Left : HorizontalAlignment.Center;
                label.SetValue(Canvas.LeftProperty, offset * boundsRect.Width);
            }

            renderedRect = new Rect(label.RenderSize);

            if (doScroll)
            {
                offset -= 0.0015;
                if (offset * boundsRect.Width < boundsRect.Left - 10000)
                    return null;
                label.SetValue(Canvas.LeftProperty, offset * boundsRect.Width + boundsRect.Left);
            }
            return label;
        }

        public static void UpdateBounds(Rect rect)
        {
            if (bannerText == null)
                return;
            bannerText.boundsRect = rect;
            bannerText.label = null;
        }

        public static void Draw(UIElementCollection children)
        {
            if (bannerText == null)
                return;

            Label text = bannerText.GetLabel();
            if (text == null)
            {
                bannerText = null;
                return;
            }
            children.Add(text);
        }
    }

    // FlyingText creates text that flys out from a given point, and fades as it gets bigger.
    // NewFlyingText() can be called as often as necessary, and there can be many texts flying out at once.

    public class FlyingText
    {
        Point center;
        string text;
        Brush brush;
        double fontSize;
        double fontGrow;
        double alpha;
        Label label;

        public FlyingText(string s, double size, Point ptCenter)
        {
            text = s;
            fontSize = Math.Max(1, size);
            fontGrow = Math.Sqrt(size) * 0.4;
            center = ptCenter;
            alpha = 1.0;
            label = null;
            brush = null;
        }

        public static void NewFlyingText(double size, Point center, string s)
        {
            flyingTexts.Add(new FlyingText(s, size, center));
        }

        void Advance()
        {
            alpha -= 0.01;
            if (alpha < 0)
                alpha = 0;

            if (brush == null)
                brush = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            if (label == null)
                label = FallingThings.MakeSimpleLabel(text, new Rect(0, 0, 0, 0), brush);

            brush.Opacity = Math.Pow(alpha, 1.5);
            label.Foreground = brush;
            fontSize += fontGrow;
            label.FontSize = Math.Max(1, fontSize);
            Rect rRendered = new Rect(label.RenderSize);
            label.SetValue(Canvas.LeftProperty, center.X - rRendered.Width / 2);
            label.SetValue(Canvas.TopProperty, center.Y - rRendered.Height / 2);
        }

        public static void Draw(UIElementCollection children)
        {
            for (int i = 0; i < flyingTexts.Count; i++)
            {
                FlyingText flyout = flyingTexts[i];
                if (flyout.alpha <= 0)
                {
                    flyingTexts.Remove(flyout);
                    i--;
                }
            }

            foreach (var flyout in flyingTexts)
            {
                flyout.Advance();
                children.Add(flyout.label);
            }
        }

        static List<FlyingText> flyingTexts = new List<FlyingText>();
    }

    // FallingThings is the main class to draw and maintain positions of falling shapes.  It also does hit testing
    // and appropriate bouncing.

    public class FallingThings
    {
        struct PolyDef
        {
            public int numSides;
            public int skip;
        }

        Dictionary<PolyType, PolyDef> PolyDefs = new Dictionary<PolyType, PolyDef>()
        {
            {PolyType.Triangle, new PolyDef()   {numSides = 3, skip = 1}},
            {PolyType.Star, new PolyDef()       {numSides = 5, skip = 2}},
            {PolyType.Pentagon, new PolyDef()   {numSides = 5, skip = 1}},
            {PolyType.Square, new PolyDef()     {numSides = 4, skip = 1}},
            {PolyType.Hex, new PolyDef()        {numSides = 6, skip = 1}},
            {PolyType.Star7, new PolyDef()      {numSides = 7, skip = 3}},
            {PolyType.Circle, new PolyDef()     {numSides = 1, skip = 1}},
            {PolyType.Bubble, new PolyDef()     {numSides = 0, skip = 1}}
        };

        public enum ThingState
        {
            Falling = 0,
            Bouncing = 1,
            Dissolving = 2,
            Remove = 3
        }

        public enum GameMode
        {
            Off = 0,
            Solo = 1,
            TwoPlayer = 2
        }

        // The Thing struct represents a single object that is flying through the air, and
        // all of its properties.

        private struct Thing
        {
            public Point center;
            public double size;
            public double theta;
            public double spinRate;
            public double yVelocity;
            public double xVelocity;
            public PolyType shape;
            public Color color;
            public Brush brush;
            public Brush brush2;
            public Brush brushPulse;
            public double dissolve;
            public ThingState state;
            public DateTime timeLastHit;
            public double avgTimeBetweenHits;
            public int touchedBy;               // Last player to touch this thing
            public int hotness;                 // Score level
            public int flashCount;

            // Hit testing between this thing and a single segment.  If hit, the center point on
            // the segment being hit is returned, along with the spot on the line from 0 to 1 if
            // a line segment was hit.

            public bool Hit(Line myNet, ref Point ptHitCenter, ref double lineHitLocation)
            {
                double minDxSquared = size;
                minDxSquared *= minDxSquared;

                double sqrLineSize = SquaredDistance(myNet.X1, myNet.Y1, myNet.X2, myNet.Y2);
                if (sqrLineSize < 0.5)  // if less than 1/2 pixel apart, just check dx to an endpoint
                {
                    return (SquaredDistance(center.X, center.Y, myNet.X1, myNet.X1) < minDxSquared) ? true : false;
                }
                else
                {   // Find dx from center to line
                    double u = ((center.X - myNet.X1) * (myNet.X2 - myNet.X1) + (center.Y - myNet.Y1) * (myNet.Y2 - myNet.Y1)) / sqrLineSize;
                    if ((u >= 0) && (u <= 1.0))
                    {   // Tangent within line endpoints, see if we're close enough
                        double xIntersect = myNet.X1 + (myNet.X2 - myNet.X1) * u;
                        double yIntersect = myNet.Y1 + (myNet.Y2 - myNet.Y1) * u;

                        if (SquaredDistance(center.X, center.Y,
                            xIntersect, yIntersect) < minDxSquared)
                        {
                            lineHitLocation = u;
                            ptHitCenter.X = xIntersect;
                            ptHitCenter.Y = yIntersect; ;
                            return true;
                        }
                    }
                    else
                    {
                        // See how close we are to an endpoint
                        if (u < 0)
                        {
                            if (SquaredDistance(center.X, center.Y, myNet.X1, myNet.Y1) < minDxSquared)
                            {
                                lineHitLocation = 0;
                                ptHitCenter.X = myNet.X1;
                                ptHitCenter.Y = myNet.Y1;
                                return true;
                            }
                        }
                        else
                        {
                            if (SquaredDistance(center.X, center.Y, myNet.X2, myNet.Y2) < minDxSquared)
                            {
                                lineHitLocation = 1;
                                ptHitCenter.X = myNet.X2;
                                ptHitCenter.Y = myNet.Y2;
                                return true;
                            }
                        }
                    }
                }
                    return false;
                
            }
        }

        private List<Thing> things = new List<Thing>();
        private List<Image> apples = new List<Image>();
        private List<Label> appleLabels = new List<Label>();
        private List<String> labelText = new List<String>();
        private List<String> usedLabels = new List<String>();
        private List<double> appleLocations = new List<double>();
        private const double DissolveTime = 0.4;
        private int maxThings = 0;
        private Rect sceneRect;
        private Random rnd = new Random();
        private double targetFrameRate = 60;
        private double dropRate = 2.0;
        private double shapeSize = 1.0;
        private double baseShapeSize = 20;
        private GameMode gameMode = GameMode.Off;
        private const double BaseGravity = 0.017;
        private double gravity = BaseGravity;
        private double gravityFactor = 1.0;
        private const double baseAirFriction = 0.994;
        private double airFriction = baseAirFriction;
        private int intraFrames = 1;
        private int frameCount = 0;
        private bool doRandomColors = true;
        private double expandingRate = 1.0;
        private Color baseColor = Color.FromRgb(0, 0, 0);
        private PolyType polyTypes = PolyType.All;
        private Dictionary<int, int> scores = new Dictionary<int, int>();
        private DateTime gameStartTime;

        public FallingThings(int maxThings, double framerate, int intraFrames, double sceneWidth, double sceneHeight)
        {
            this.maxThings = maxThings;
            this.intraFrames = intraFrames;
            this.targetFrameRate = framerate * intraFrames;
            SetGravity(gravityFactor);
            sceneRect.X = sceneRect.Y = 0;
            sceneRect.Width=sceneWidth;
            sceneRect.Height = sceneHeight;
            shapeSize = sceneRect.Height * baseShapeSize / 1000.0;
            expandingRate = Math.Exp(Math.Log(6.0) / (targetFrameRate * DissolveTime));
            labelText.Add("Lebanon");
            labelText.Add("China");
            labelText.Add("Israel");
            labelText.Add("Serbia");
            labelText.Add("France");
            labelText.Add("Germany");
            labelText.Add("South Africa");
            labelText.Add("Madagascar");
            labelText.Add("Vietnam");
            labelText.Add("Russia");
            labelText.Add("Iraq");
            labelText.Add("Brazil");
            labelText.Add("Somalia");
            labelText.Add("Saudi Arabia");
            labelText.Add("Italy");
            labelText.Add("USA");
            labelText.Add("Egypt");
            labelText.Add("Kenya");
            labelText.Add("Uganda");
            labelText.Add("Vatican");
            labelText.Add("Mongolia");
            labelText.Add("Philippines");
            labelText.Add("South Korea");
            labelText.Add("North Korea");
            labelText.Add("Poland");
            labelText.Add("Ukraine");
            labelText.Add("Australia");
            labelText.Add("New Zealand");
            labelText.Add("Japan");
            labelText.Add("Mexico");
            labelText.Add("Iran");
            labelText.Add("Spain");

        }

        public void SetFramerate(double actualFramerate)
        {
            targetFrameRate = actualFramerate * intraFrames;
            expandingRate = Math.Exp(Math.Log(6.0) / (targetFrameRate * DissolveTime));
            if (gravityFactor != 0)
                SetGravity(gravityFactor);
        }

        public void SetBoundaries(Rect r)
        {
            sceneRect = r;
            shapeSize = r.Height * baseShapeSize / 1000.0;
        }

        public void SetDropRate(double f)
        {
            dropRate = f;
        }

        public void SetSize(double f)
        {
            baseShapeSize = f;
            shapeSize = sceneRect.Height * baseShapeSize / 1000.0;
        }

        public void SetShapesColor(Color color, bool doRandom)
        {
            doRandomColors = doRandom;
            baseColor = color;
        }

        public void Reset()
        {
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                if ((thing.state == ThingState.Bouncing) || (thing.state == ThingState.Falling))
                {
                    thing.state = ThingState.Dissolving;
                    thing.dissolve = 0;
                    things[i] = thing;
                }
            }
            gameStartTime = DateTime.Now;
            scores.Clear();
        }
        
        public void SetGameMode(GameMode mode)
        {
            gameMode = mode;
            gameStartTime = DateTime.Now;
            scores.Clear();
        }

        public void SetGravity(double f)
        {
            gravityFactor = f;
            gravity = f * BaseGravity / targetFrameRate / Math.Sqrt(targetFrameRate) / Math.Sqrt((double)intraFrames);
            airFriction = (f == 0) ? 0.997 : Math.Exp(Math.Log(1.0 - (1.0 - baseAirFriction) / f) / intraFrames);
            
            if (f == 0)  // Stop all movement as well!
            {
                for (int i = 0; i < things.Count; i++)
                {
                    Thing thing = things[i];
                    thing.xVelocity = thing.yVelocity = 0;
                    things[i] = thing;
                }
            }
        }

        public void SetPolies(PolyType polies)
        {
            polyTypes = polies;
        }

        private void AddToScore(int player, int points, Point center)
        {
            if (scores.ContainsKey(player))
            {
                scores[player] = scores[player] + 1;
                Console.WriteLine("Same User: " + scores[player]);
            }
            else
            {
                scores.Add(player, points);
                Console.WriteLine("New User: " + points);
            }
           
            FlyingText.NewFlyingText(sceneRect.Width / 300, center, "+" + points);
        }

        private static double SquaredDistance(double x1, double y1, double x2, double y2)
        {
            return ((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }

        public HitType LookForHits(Line myNet,int playerId)
        {
            DateTime cur = DateTime.Now;
            HitType allHits = HitType.None;

            // Zero out score if necessary
            if (!scores.ContainsKey(playerId))
                scores.Add(playerId, 0);

                for (int i = 0; i < things.Count; i++)
                {
                    HitType hit = HitType.None;
                    Thing thing = things[i];
                    switch (thing.state)
                    {
                        case ThingState.Bouncing:
                        case ThingState.Falling:
                            {
                                var ptHitCenter = new Point(0, 0);
                                double lineHitLocation = 0;
                                if (thing.Hit(myNet, ref ptHitCenter, ref lineHitLocation))
                                {
                                    double fMs = 1000;
                                    if (thing.timeLastHit != DateTime.MinValue)
                                    {
                                        fMs = cur.Subtract(thing.timeLastHit).TotalMilliseconds;
                                        thing.avgTimeBetweenHits = thing.avgTimeBetweenHits * 0.8 + 0.2 * fMs;
                                    }
                                    thing.timeLastHit = cur;

                                    if (gameMode == GameMode.TwoPlayer)
                                    {
                                        if (thing.state == ThingState.Falling)
                                        {
                                            thing.state = ThingState.Bouncing;
                                            thing.touchedBy = playerId;
                                            thing.hotness = 1;
                                            thing.flashCount = 0;
                                        }
                                        else if (thing.state == ThingState.Bouncing)
                                        {
                                            if (thing.touchedBy != playerId)
                                            {
                                                hit |= HitType.Popped;
                                                AddToScore(thing.touchedBy, 5 << (thing.hotness - 1), thing.center);
                                            }
                                        }
                                    }
                                    else if (gameMode == GameMode.Solo)
                                    {

                                    }

                                    things[i] = thing;

                                    if (thing.avgTimeBetweenHits < 8)
                                    {
                                        hit |= HitType.Popped | HitType.Squeezed;
                                        if (gameMode != GameMode.Off)
                                            AddToScore(1, 1, thing.center);
                                    }
                                }
                            }
                            break;
                    }

                    if ((hit & HitType.Popped) != 0)
                    {
                        thing.state = ThingState.Dissolving;
                        thing.dissolve = 0;
                        thing.xVelocity = thing.yVelocity = 0;
                        thing.spinRate = thing.spinRate * 6 + 0.2;
                        things[i] = thing;
                    }
                    allHits |= hit;
                }
            return allHits;
        }

        public void DropNewThing(PolyType newShape, double newSize, double appleLocation ,Color newColor, UIElementCollection children, String word /*, List<ShoopDoup.Models.DataObject> data*/)
        {
            // Only drop within the center "square" area 
            double dropX;
            double fDropWidth = (sceneRect.Bottom - sceneRect.Top);
            if (fDropWidth > sceneRect.Right - sceneRect.Left)
                fDropWidth = sceneRect.Right - sceneRect.Left;
            if (appleLocation == 0)
            {
                dropX = rnd.NextDouble() * fDropWidth + (sceneRect.Left + sceneRect.Right - fDropWidth) / 2;
            }
            else
            {
                dropX = appleLocation;
            }

            var newThing = new Thing()
            {
                size = 125,
                yVelocity = (0.5 * rnd.NextDouble() - 0.25) / targetFrameRate,
                xVelocity = 0,
                shape = newShape,
                center = new Point(dropX, sceneRect.Top - newSize),
                spinRate = (rnd.NextDouble() * 12.0 - 6.0) * 2.0 * Math.PI / targetFrameRate / 4.0,
                theta = 0,
                timeLastHit = DateTime.MinValue,
                avgTimeBetweenHits = 100,
                color = newColor,
                brush = null,
                brush2 = null,
                brushPulse = null,
                dissolve = 0,
                state = ThingState.Falling,
                touchedBy = 0,
                hotness = 0,
                flashCount = 0,
            };
            things.Add(newThing);

            String text = word;

            if (word == null)
            {
                int randomLabel = rnd.Next(0, labelText.Count - 1);
                 text = labelText[randomLabel];

                while (usedLabels.Contains(text))
                {
                    if (usedLabels.Count > labelText.Count - 5)
                    {
                        usedLabels.Clear();
                    }
                    randomLabel = rnd.Next(0, labelText.Count - 1);
                    text = labelText[randomLabel];
                }
                usedLabels.Add(text);
            }
            

            Label label = new Label();
            TextBlock bubbleTextBlock = new TextBlock();
            bubbleTextBlock.Text = text;
            SolidColorBrush appleColor = new SolidColorBrush();
            appleColor.Color = Color.FromArgb(255, 227, 50, 50);
            bubbleTextBlock.Foreground=appleColor;
            Viewbox bubbleViewBox = new Viewbox();
            bubbleViewBox.Stretch = Stretch.Uniform;
            bubbleViewBox.Height = 80;
            bubbleViewBox.Width = 100;
            bubbleViewBox.Child = bubbleTextBlock;
            label.Content = bubbleViewBox;
            appleLabels.Add(label);

            children.Add(label);

            children.Add(makeApple(PolyDefs[newThing.shape].numSides, PolyDefs[newThing.shape].skip,
        newThing.size, newThing.theta, newThing.center, newThing.brush,
        newThing.brushPulse, newThing.size * 0.1, 0));
        }

        public BitmapImage toBitmapImage(System.Drawing.Bitmap bitmap)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            ms.Position = 0;
            BitmapImage bi = new BitmapImage();
            bi.BeginInit();
            bi.StreamSource = ms;
            bi.EndInit();
            return bi;
        }

        private Image makeApple(int numSides, int skip, double size, double spin, Point center, Brush brush,
            Brush brushStroke, double strokeThickness, double opacity)
        {

            System.Windows.Controls.Image apple = new System.Windows.Controls.Image();
            apple.Source = this.toBitmapImage(ShoopDoup.Properties.Resources.apple);
            apple.SetValue(Canvas.LeftProperty, center.X - size);
            apple.SetValue(Canvas.TopProperty, center.Y - size);
            apple.Height = 150;
            apples.Add(apple);

            return apple;
        }

        public static Label MakeSimpleLabel(string text, Rect bounds, Brush brush)
        {
            Label label = new Label();
            label.Content = text;
            if (bounds.Width != 0)
            {
                label.SetValue(Canvas.LeftProperty, bounds.Left);
                label.SetValue(Canvas.TopProperty, bounds.Top);
                label.Width = bounds.Width;
                label.Height = bounds.Height;
            }
            label.Foreground = brush;
            label.FontFamily = new FontFamily("Arial");
            label.FontWeight = FontWeight.FromOpenTypeWeight(600);
            label.FontStyle = FontStyles.Normal;
            label.HorizontalAlignment = HorizontalAlignment.Center;
            label.VerticalAlignment = VerticalAlignment.Center;
            return label;
        }

        public void AdvanceFrame(UIElementCollection children, bool instructing/*, List<ShoopDoup.Models.DataObject> data*/)
        {
            // Move all things by one step, accounting for gravity
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                Image apple = apples[i];
                Label label = appleLabels[i];

                thing.center.Offset(thing.xVelocity, thing.yVelocity);
                thing.yVelocity += gravity * sceneRect.Height;
                thing.yVelocity *= airFriction;
                thing.xVelocity *= airFriction;
                thing.theta += thing.spinRate;

                if (thing.state != ThingState.Dissolving)
                {
                    apple.SetValue(Canvas.LeftProperty, thing.center.X - thing.size);
                    apple.SetValue(Canvas.TopProperty, thing.center.Y - thing.size);
                    label.SetValue(Canvas.LeftProperty, thing.center.X - thing.size*.84);
                    label.SetValue(Canvas.TopProperty, thing.center.Y - thing.size*.84);
                    label.SetValue(Canvas.ZIndexProperty, 100);
                }

                // bounce off walls
                if ((thing.center.X - thing.size < 0) || (thing.center.X + thing.size > sceneRect.Width))
                {
                    thing.xVelocity = -thing.xVelocity;
                    thing.center.X += thing.xVelocity;
                }

                // Then get rid of one if any that fall off the bottom
                if (thing.center.Y - thing.size > sceneRect.Bottom)
                {
                    thing.state = ThingState.Remove;
                }

                // Get rid of after dissolving.
                if (thing.state == ThingState.Dissolving)
                {
                        thing.state = ThingState.Remove;
                }
                things[i] = thing;
                apples[i] = apple;
                appleLabels[i] = label;
            }

            // Then remove any that should go away now
            for (int i = 0; i < things.Count; i++)
            {
                Thing thing = things[i];
                Image apple = apples[i];
                Label label = appleLabels[i];
                //double appleLocation = appleLocations[i];
                //String text = labelText[i];
                if (thing.state == ThingState.Remove)
                {
                    things.Remove(thing);
                    apples.Remove(apple);
                    appleLabels.Remove(label);
                    //appleLocations.Remove(appleLocation);
                    //labelText.Remove(text);
                    children.Remove(apple);
                    children.Remove(label);
                    i--;
                }
            }

            // Create any new things to drop based on dropRate
            if (!instructing && (things.Count < maxThings) && (rnd.NextDouble() < dropRate / targetFrameRate) && (polyTypes != PolyType.None))
            {
                PolyType[] alltypes = {PolyType.Square};
                byte r = baseColor.R;
                byte g = baseColor.G;
                byte b = baseColor.B;

                if (doRandomColors)
                {
                    r = (byte)(rnd.Next(215) + 40);
                    g = (byte)(rnd.Next(215) + 40);
                    b = (byte)(rnd.Next(215) + 40);
                }
                else
                {
                    r = (byte)(Math.Min(255.0, (double)baseColor.R * (0.7 + rnd.NextDouble() * 0.7)));
                    g = (byte)(Math.Min(255.0, (double)baseColor.G * (0.7 + rnd.NextDouble() * 0.7)));
                    b = (byte)(Math.Min(255.0, (double)baseColor.B * (0.7 + rnd.NextDouble() * 0.7)));
                }
                
                PolyType tryType = PolyType.None;
                do
                {
                    tryType = alltypes[rnd.Next(alltypes.Length)];
                } while ((polyTypes & tryType) == 0);

                DropNewThing(tryType, shapeSize, 0,Color.FromRgb(r, g, b),children, null /*,data*/);
            }
        }
    }
}