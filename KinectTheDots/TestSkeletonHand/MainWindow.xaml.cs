using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;

namespace TestSkeletonHand
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _Kinect;

        private readonly Brush[] _SkeletonBrushes;
        private Skeleton[] _FrameSkeletons;

        private DotPuzzle _Puzzle;
        private int _PuzzleDotIndex;

        #endregion Member Variables

        //-----------MAIN WINDOW-----------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            //BRUSHES
            this._SkeletonBrushes = new[] { Brushes.Black, Brushes.Crimson, Brushes.Indigo, 
                                            Brushes.DodgerBlue, Brushes.Purple, Brushes.Pink };

            this._Puzzle = new DotPuzzle();
            this._Puzzle.Dots.Add(new Point(200, 300));
            this._Puzzle.Dots.Add(new Point(1600, 300));
            this._Puzzle.Dots.Add(new Point(1650, 400));
            this._Puzzle.Dots.Add(new Point(1600, 500));
            this._Puzzle.Dots.Add(new Point(1000, 500));
            this._Puzzle.Dots.Add(new Point(1000, 600));
            this._Puzzle.Dots.Add(new Point(1200, 700));
            this._Puzzle.Dots.Add(new Point(1150, 800));
            this._Puzzle.Dots.Add(new Point(750, 800));
            this._Puzzle.Dots.Add(new Point(700, 700));
            this._Puzzle.Dots.Add(new Point(900, 600));
            this._Puzzle.Dots.Add(new Point(900, 500));
            this._Puzzle.Dots.Add(new Point(200, 500));
            this._Puzzle.Dots.Add(new Point(150, 400));
            
            this._PuzzleDotIndex = -1;

            //this.Loaded += MainWindow_Loaded;
            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            DrawPuzzle(this._Puzzle);
            this.Unloaded += (s, e) => { this.Kinect = null; };
        }


        #endregion Constructor

        //-------------STATUS KINECT-------------
        #region Methods
        private void DiscoverKinectSensor()
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this.Kinect = KinectSensor.KinectSensors
                                      .FirstOrDefault(x => x.Status == KinectStatus.Connected);
        }


        private void KinectSensors_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case KinectStatus.Initializing:
                case KinectStatus.Connected:
                    this.Kinect = e.Sensor;
                    break;
                case KinectStatus.Disconnected:
                    //TODO: Give the user feedback to plug-in a Kinect device.    
                    this.Kinect = null;
                    break;
                default:
                    //TODO: Show an error state
                    break;
            }
        }
        #endregion Methods
        
        //------------EVENTS-----------------
        #region Events
        

        //SKELETON FRAME READY
        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {

            using (SkeletonFrame frame = e.OpenSkeletonFrame())
            {
                if (frame != null)
                {

                    frame.CopySkeletonDataTo(this._FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this._FrameSkeletons);


                    if (skeleton == null)
                    {
                        HandCursorElement.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        Joint primaryHand = GetPrimaryHand(skeleton);
                        TrackHand(primaryHand);
                        TrackPuzzle(primaryHand.Position);
                    }
                }
            }
        }
        
        //GETPRIMARY SKELETON
        private static Skeleton GetPrimarySkeleton(Skeleton[] skeletons)
        {
            Skeleton skeleton = null;

            if (skeletons != null)
            {
                //Find the closest skeleton
                for (int i = 0; i < skeletons.Length; i++)
                {
                    if (skeletons[i].TrackingState == SkeletonTrackingState.Tracked)
                    {
                        if (skeleton == null)
                        {
                            skeleton = skeletons[i];
                        }
                        else
                        {
                            if (skeleton.Position.Z > skeletons[i].Position.Z)
                            {
                                skeleton = skeletons[i];
                            }
                        }
                    }
                }
            }

            return skeleton;
        }

        //Get primary hand
        private static Joint GetPrimaryHand(Skeleton skeleton)
        {
            Joint primaryHand = new Joint();

            if (skeleton != null)
            {
                primaryHand = skeleton.Joints[JointType.HandLeft];
                Joint righHand = skeleton.Joints[JointType.HandRight];


                if (righHand.TrackingState != JointTrackingState.NotTracked)
                {
                    if (primaryHand.TrackingState == JointTrackingState.NotTracked)
                    {
                        primaryHand = righHand;
                    }
                    else
                    {
                        if (primaryHand.Position.Z > righHand.Position.Z)
                        {
                            primaryHand = righHand;
                        }
                    }
                }
            }

            return primaryHand;
        }

        private void TrackHand(Joint hand)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                HandCursorElement.Visibility = System.Windows.Visibility.Collapsed;
            }
            else
            {
                HandCursorElement.Visibility = System.Windows.Visibility.Visible;

                // float x;
                //float y;

                DepthImagePoint point = this.Kinect.MapSkeletonPointToDepth(hand.Position,
                                                             DepthImageFormat.Resolution640x480Fps30);
                point.X = (int)((point.X * LayoutRoot.ActualWidth /
                                   this.Kinect.DepthStream.FrameWidth) -
                                   (HandCursorElement.ActualWidth / 2.0));
                point.Y = (int)((point.Y * LayoutRoot.ActualHeight /
                                    this.Kinect.DepthStream.FrameHeight) -
                                    (HandCursorElement.ActualHeight / 2.0));

                Canvas.SetLeft(HandCursorElement, point.X);
                Canvas.SetTop(HandCursorElement, point.Y);

                if (hand.JointType == JointType.HandRight)
                {
                    HandCursorScale.ScaleX = 1;
                }
                else
                {
                    HandCursorScale.ScaleX = -1;
                }
            }
        }

         private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            KinectSensor.KinectSensors.StatusChanged += KinectSensors_StatusChanged;
            this._Kinect = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status ==
                                                                          KinectStatus.Connected);

            DrawPuzzle(this._Puzzle);
        }

        //DRAW PUZZLE
        private void DrawPuzzle(DotPuzzle puzzle)
        {

            PuzzleBoardElement.Children.Clear();

            if (puzzle != null)
            {
                for (int i = 0; i < puzzle.Dots.Count; i++)
                {
                    Grid dotContainer = new Grid();
                    dotContainer.Width = 50;
                    dotContainer.Height = 50;
                    dotContainer.Children.Add(new Ellipse() { Fill = Brushes.Gray });

                    TextBlock dotLabel = new TextBlock();
                    dotLabel.Text = (i + 1).ToString();
                    dotLabel.Foreground = Brushes.White;
                    dotLabel.FontSize = 24;
                    dotLabel.HorizontalAlignment = System.Windows.HorizontalAlignment.Center;
                    dotLabel.VerticalAlignment = System.Windows.VerticalAlignment.Center;
                    dotContainer.Children.Add(dotLabel);

                    //Position the UI element centered on the dot point
                    Canvas.SetTop(dotContainer, puzzle.Dots[i].Y - (dotContainer.Height / 2));
                    Canvas.SetLeft(dotContainer, puzzle.Dots[i].X - (dotContainer.Width / 2));
                    PuzzleBoardElement.Children.Add(dotContainer);
                }
            }
        }

        //TRACK PUZZLE (PLAYING)

        private void TrackPuzzle(SkeletonPoint position)
        {
            if (this._PuzzleDotIndex == this._Puzzle.Dots.Count)
            {
                //Do nothing - Game is over 
            }
            else
            {
                Point dot;

                if (this._PuzzleDotIndex + 1 < this._Puzzle.Dots.Count)
                {
                    dot = this._Puzzle.Dots[this._PuzzleDotIndex + 1];
                }
                else
                {
                    dot = this._Puzzle.Dots[0];
                }


                //float x;
                //float y;

                DepthImagePoint point = this._Kinect.MapSkeletonPointToDepth(position,
                                                             DepthImageFormat.Resolution640x480Fps30);
                point.X = (int)(point.X * LayoutRoot.ActualWidth /
                                 this._Kinect.DepthStream.FrameWidth);
                point.Y = (int)(point.Y * LayoutRoot.ActualHeight /
                                 this._Kinect.DepthStream.FrameHeight);
                Point handPoint = new Point(point.X, point.Y);

                //Calculate the length between the two points. This can be done manually 
                //as shown here or by using the System.Windows.Vector object to get the length. 
                //System.Windows.Media.Media3D.Vector3D is available for 3D vector math. 
                Point dotDiff = new Point(dot.X - handPoint.X, dot.Y - handPoint.Y);
                double length = Math.Sqrt(dotDiff.X * dotDiff.X + dotDiff.Y * dotDiff.Y);

                int lastPoint = this.CrayonElement.Points.Count;
                lastPoint = lastPoint - 1;
                if (length < 25)
                {
                    //Cursor is within the hit zone 

                    if (lastPoint > 0)
                    {
                        //Remove the working end point 
                        this.CrayonElement.Points.RemoveAt(lastPoint);
                    }

                    //Set line end point 
                    this.CrayonElement.Points.Add(new Point(dot.X, dot.Y));

                    //Set new line start point 
                    this.CrayonElement.Points.Add(new Point(dot.X, dot.Y));

                    //Move to the next dot 
                    this._PuzzleDotIndex++;

                    if (this._PuzzleDotIndex == this._Puzzle.Dots.Count)
                    {
                        //Notify the user that the game is over 
                    }
                }
                else
                {
                    if (lastPoint > 0)
                    {
                        //To refresh the Polyline visual you must remove the last point, 
                        //update and add it back. 
                        Point lineEndpoint = this.CrayonElement.Points[lastPoint];
                        this.CrayonElement.Points.RemoveAt(lastPoint);
                        lineEndpoint.X = handPoint.X;
                        lineEndpoint.Y = handPoint.Y;
                        this.CrayonElement.Points.Add(lineEndpoint);
                    }
                }
            }
        }


        #endregion Events

        //------------PROPERTIES---------------
        #region Properties
        public KinectSensor Kinect
        {
            get { return this._Kinect; }
            set
            {
                if (this._Kinect != value)
                {
                    if (this._Kinect != null)
                    {
                        UninitializeKinectSensor(this._Kinect); //Uninitialize kinect
                        this._Kinect = null;
                    }

                    if (value != null && value.Status == KinectStatus.Connected)
                    {
                        this._Kinect = value;
                        InitializeKinectSensor(this._Kinect); //Initialize Kinect
                    }
                }
            }
        }

        //------------INITIALIZE / UNINITIALIZE KINECT ---------
        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {

                sensor.SkeletonStream.Enable(); //Enable skeleton

                this._FrameSkeletons = new Skeleton[this._Kinect.SkeletonStream.FrameSkeletonArrayLength];
                
                sensor.SkeletonFrameReady += Kinect_SkeletonFrameReady; //create event when skeleton stream 
                SkeletonViewerElement.KinectDevice = this._Kinect;//TEST WITH skeleton viewer
                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                sensor.SkeletonStream.Disable();
                SkeletonViewerElement.KinectDevice = null; //TEST WITH skeleton viewer
                this._FrameSkeletons = null;
            }
        }
        #endregion Properties

       
    }
}
