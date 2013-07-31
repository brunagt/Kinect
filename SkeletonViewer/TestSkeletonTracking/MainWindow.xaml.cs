
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

using System.IO;
using Microsoft.Kinect;
using Coding4Fun.Kinect.Wpf;


namespace TestSkeletonTracking {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _Kinect;

        private readonly Brush[] _SkeletonBrushes;
        private Skeleton[] _FrameSkeletons;

        #endregion Member Variables

        //-----------MAIN WINDOW-----------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            //BRUSHES
            this._SkeletonBrushes = new[] { Brushes.Black, Brushes.Crimson, Brushes.Indigo, 
                                            Brushes.DodgerBlue, Brushes.Purple, Brushes.Pink };

            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };
        }


        #endregion Constructor

        //-------------STATUS KINECT---------------
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


        //CREATE FIGURE: DRAWS THE STICK FIGURE FOR A SINGLE SKELETON OBJECT
        private Polyline CreateFigure(Skeleton skeleton, Brush brush, JointType[] joints)
        {
            Polyline figure = new Polyline();
            figure.StrokeThickness = 8;
            figure.Stroke = brush;

            for (int i = 0; i < joints.Length; i++)
            {
                figure.Points.Add(GetJointPoint(skeleton.Joints[joints[i]]));
            }

            return figure;
        }

        //GETJOINTPOINT: CPNVERTS SKELETON CORDINATES TO A DEPTH IMAGE COORDINATES
        private Point GetJointPoint(Joint joint)
        {
            DepthImagePoint point = this.Kinect.MapSkeletonPointToDepth(joint.Position,
                                                                this.Kinect.DepthStream.Format);
            point.X *= (int)this.LayoutRoot.ActualWidth / this.Kinect.DepthStream.FrameWidth;
            point.Y *= (int)this.LayoutRoot.ActualHeight / this.Kinect.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }
        #endregion Methods

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
                
                sensor.SkeletonFrameReady += Kinect_SkeletonFrameReady; //create event when there is a skeleton stream 
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
                this._FrameSkeletons = null;
            }
        }
        #endregion Properties

        #region Events
        //------------EVENTS-----------------

        //COLOR IMAGE FRAME READY
        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using(SkeletonFrame frame = e.OpenSkeletonFrame())
        {
            if(frame != null)
            {
                Polyline figure;
                Brush userBrush;
                Skeleton skeleton;

                JointType[] joints;

                LayoutRoot.Children.Clear();
                frame.CopySkeletonDataTo(this._FrameSkeletons);

                for(int i = 0; i < this._FrameSkeletons.Length; i++)
                {
                    skeleton = this._FrameSkeletons[i];

                    //determine if we have an actual skeleton (just users)
                    if(skeleton.TrackingState == SkeletonTrackingState.Tracked)
                    {
                        //select a brush color based on the position of the player 
                        userBrush = this._SkeletonBrushes[i % this._SkeletonBrushes.Length]; 

                        //Draws the skeleton's head and torso
                        joints = new [] { JointType.Head, JointType.ShoulderCenter,
                                          JointType.ShoulderLeft, JointType.Spine,
                                          JointType.ShoulderRight, JointType.ShoulderCenter,
                                          JointType.HipCenter, JointType.HipLeft,
                                          JointType.Spine, JointType.HipRight,
                                          JointType.HipCenter };
                        LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));

                        //Draws the skeleton's left leg
                        joints = new [] { JointType.HipLeft, JointType.KneeLeft,
                                          JointType.AnkleLeft, JointType.FootLeft };
                        LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));

                        //Draws the skeleton's right leg
                        joints = new [] { JointType.HipRight, JointType.KneeRight,
                                          JointType.AnkleRight, JointType.FootRight };
                        LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));

                        //Draws the skeleton's left arm
                        joints = new [] { JointType.ShoulderLeft, JointType.ElbowLeft,
                                          JointType.WristLeft, JointType.HandLeft };
                        LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));

                        //Draws the skeleton's right arm
                        joints = new [] { JointType.ShoulderRight, JointType.ElbowRight,
                                          JointType.WristRight, JointType.HandRight };
                        LayoutRoot.Children.Add(CreateFigure(skeleton, userBrush, joints));
                    }
                }
            }
        }
    }



        #endregion Events
    }
}
