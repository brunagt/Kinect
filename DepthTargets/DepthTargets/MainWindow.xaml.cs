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

namespace DepthTargets
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //-------------VARIABLES-------------
        #region Member Variables
        private const float FeetPerMeters = 3.2808399f;

        private KinectSensor _Kinect;
        private Skeleton[] _FrameSkeletons;

        #endregion Member Variables

        //-----------CONSTRUCTOR--------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };
        }


        #endregion Constructor

        //-------------METHODS----------------
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
                case KinectStatus.Connected:
                    if (this.Kinect == null)
                    {
                        this.Kinect = e.Sensor;
                    }
                    break;

                case KinectStatus.Disconnected:
                    if (this.Kinect == e.Sensor)
                    {
                        this.Kinect = null;
                        this.Kinect = KinectSensor.KinectSensors
                                      .FirstOrDefault(x => x.Status == KinectStatus.Connected);

                        if (this.Kinect == null)
                        {
                            //Notify the user that the sensor is disconnected 
                        }
                    }
                    break;

                //Handle all other statuses according to needs 
            }
        }

        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonFrame.CopySkeletonDataTo(this._FrameSkeletons);
                    Skeleton skeleton = GetPrimarySkeleton(this._FrameSkeletons);

                    if (skeleton != null)
                    {
                        TrackHand(skeleton.Joints[JointType.HandLeft], LeftHandElement,
                                  LeftHandScaleTransform, LayoutRoot, true);
                        TrackHand(skeleton.Joints[JointType.HandRight], RightHandElement,
                                  RightHandScaleTransform, LayoutRoot, false);
                    }
                }
            }
        }

        //GET JOINT POINT
        private static Point GetJointPoint(KinectSensor kinectDevice, Joint joint, Size containerSize, Point offset)
        {
            DepthImagePoint point = kinectDevice.MapSkeletonPointToDepth(joint.Position, kinectDevice.DepthStream.Format);
            point.X = (int)((point.X * containerSize.Width / kinectDevice.DepthStream.FrameWidth) - offset.X);
            point.Y = (int)((point.Y * containerSize.Height / kinectDevice.DepthStream.FrameHeight) - offset.Y);

            return new Point(point.X, point.Y);
        }

        //GET PRIMARY SKELETON
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

        private void TrackHand(Joint hand, FrameworkElement cursorElement,
                               ScaleTransform cursorScale, FrameworkElement container, bool isLeft)
        {
            if(hand.TrackingState != JointTrackingState.NotTracked)
            {
                double z = hand.Position.Z * FeetPerMeters;
                cursorElement.Visibility = System.Windows.Visibility.Visible;
                Point cursorCenter = new Point(cursorElement.ActualWidth / 2.0,
                                               cursorElement.ActualHeight / 2.0);
                Point jointPoint = GetJointPoint(this.Kinect, hand,
                                                 container.RenderSize, cursorCenter);
                Canvas.SetLeft(cursorElement, jointPoint.X);
                Canvas.SetTop(cursorElement, jointPoint.Y);
                Canvas.SetZIndex(cursorElement, (int) (1340 - (z * 100)));

                cursorScale.ScaleX = 1340 / z * ((isLeft) ? -1 : 1);
                cursorScale.ScaleY = 1340 / z;

                if(hand.JointType == JointType.HandLeft)
                {
                    DebugLeftHand.Text = string.Format("Left Hand: {0:0.00}", z * 10);
                }
                else
                {
                     DebugRightHand.Text = string.Format("Right Hand: {0:0.00}", z * 10);
                }
            }
            else
            {
                DebugLeftHand.Text  = string.Empty;
                DebugRightHand.Text = string.Empty;
            }
        }
        #endregion Methods

        //------------PROPERTIES--------------
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

                sensor.DepthStream.Enable(); //Enable depth camara
                this._FrameSkeletons = new Skeleton[this._Kinect.SkeletonStream.FrameSkeletonArrayLength];
                sensor.SkeletonFrameReady += Kinect_SkeletonFrameReady; //create event when there is a camera stream (30fps)
                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                this._FrameSkeletons = null;
            }
        }
        #endregion Properties

    }
}