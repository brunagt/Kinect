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

namespace TrackJoints
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private KinectSensor _Kinect;
        StreamWriter writer;

        byte[] colorBytes;
        Skeleton[] skeletons;
        StreamWriter coordinatesStream;

        bool isCirclesVisible = true;

        public MainWindow()
        {
            InitializeComponent();
            
            
            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };
           
        }

        #region KinectStatus
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
                            MessageBox.Show("This application requires a Kinect sensor");
                            this.Close();
                        }
                    }
                    break;

                //Handle all other statuses according to needs 
            }
        }


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

        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {

                sensor.DepthStream.Enable(); //Enable depth camara
                sensor.ColorStream.Enable();
                sensor.SkeletonStream.Enable();

                //sensor.DepthFrameReady += Kinect_DepthFrameReady; //create event when there is a camera stream (30fps)
                sensor.ColorFrameReady += Kinect_ColorFrameReady;
                sensor.SkeletonFrameReady += Kinect_SkeletonFrameReady;

                SkeletonViewerElement.KinectDevice = this._Kinect;//TEST WITH skeleton viewer
               

                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                //sensor.DepthFrameReady -= Kinect_DepthFrameReady;
                sensor.ColorFrameReady -= Kinect_ColorFrameReady;
                sensor.SkeletonFrameReady -= Kinect_SkeletonFrameReady;
                sensor.Dispose();
                sensor = null;

                SkeletonViewerElement.KinectDevice = null; //TEST WITH skeleton viewer
            }
        }
        #endregion KinectStatus


        #region Events

        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame == null)
                    return;

                if (colorBytes == null ||
                    colorBytes.Length != imageFrame.PixelDataLength)
                {
                    colorBytes = new byte[imageFrame.PixelDataLength];
                }

                imageFrame.CopyPixelDataTo(colorBytes);

                //You could use PixelFormats.Bgr32 below to ignore the alpha,
                //or if you need to set the alpha you would loop through the bytes 
                //as in this loop below
                int length = colorBytes.Length;
                for (int i = 0; i < length; i += 4)
                {
                    colorBytes[i + 3] = 255;
                }

                BitmapSource source = BitmapSource.Create(imageFrame.Width, imageFrame.Height,
                    96, 96, PixelFormats.Bgra32, null, colorBytes, imageFrame.Width * imageFrame.BytesPerPixel);
                videoImage.Source = source;
            }
        }


        private void Kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame == null)
                    return;

                if (skeletons == null ||
                    skeletons.Length != skeletonFrame.SkeletonArrayLength)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                }

                skeletonFrame.CopySkeletonDataTo(skeletons);

                Skeleton closestSkeleton = (from s in skeletons
                                            where s.TrackingState == SkeletonTrackingState.Tracked &&
                                                  s.Joints[JointType.Head].TrackingState == JointTrackingState.Tracked
                                            select s).OrderBy(s => s.Joints[JointType.Head].Position.Z)
                                                    .FirstOrDefault();
                //saveCoordinates(closestSkeleton, "nothing");

                if (closestSkeleton == null)
                    return;

                var hipCenter = closestSkeleton.Joints[JointType.HipCenter];
                var rightKnee = closestSkeleton.Joints[JointType.KneeRight];
                var leftKnee = closestSkeleton.Joints[JointType.KneeLeft];
                var rightFoot = closestSkeleton.Joints[JointType.FootLeft];
                var leftFoot = closestSkeleton.Joints[JointType.FootRight];


                if (hipCenter.TrackingState != JointTrackingState.Tracked ||
                    rightKnee.TrackingState != JointTrackingState.Tracked ||
                    leftKnee.TrackingState != JointTrackingState.Tracked ||
                    leftFoot.TrackingState != JointTrackingState.Tracked ||
                    rightFoot.TrackingState != JointTrackingState.Tracked )

                {
                    //Don't have a good read on the joints so we cannot process gestures
                    return;
                }

                SetEllipsePosition(ellipseHip, hipCenter, false);
                SetEllipsePosition(ellipseLeftKnee, leftKnee, false);
                SetEllipsePosition(ellipseRightKnee, rightKnee, false);
                SetEllipsePosition(ellipseRightFoot, rightFoot, false);
                SetEllipsePosition(ellipseLeftFoot, leftFoot, false);

                saveCoordinates(closestSkeleton, "nothing");
            }
        }

        

        //This method is used to position the ellipses on the canvas
        //according to correct movements of the tracked joints.
        private void SetEllipsePosition(Ellipse ellipse, Joint joint, bool isHighlighted)
        {
            var point = this.Kinect.MapSkeletonPointToColor(joint.Position, this.Kinect.ColorStream.Format);

            Canvas.SetLeft(ellipse, point.X - ellipse.ActualWidth / 2);
            Canvas.SetTop(ellipse, point.Y - ellipse.ActualHeight / 2);
            //writer.WriteLine(point.X + " " + point.Y );
        }

        void ToggleCircles()
        {
            if (isCirclesVisible)
                HideCircles();
            else
                ShowCircles();
        }

        void HideCircles()
        {
            isCirclesVisible = false;
            ellipseHip.Visibility = System.Windows.Visibility.Collapsed;
            ellipseLeftKnee.Visibility = System.Windows.Visibility.Collapsed;
            ellipseRightKnee.Visibility = System.Windows.Visibility.Collapsed;
        }

        void ShowCircles()
        {
            isCirclesVisible = true;
            ellipseHip.Visibility = System.Windows.Visibility.Visible;
            ellipseLeftKnee.Visibility = System.Windows.Visibility.Visible;
            ellipseRightKnee.Visibility = System.Windows.Visibility.Visible;
        }

        private void saveCoordinates(Skeleton skeleton, string textFile)
        {
            if (coordinatesStream == null)
                coordinatesStream = new StreamWriter("joints.txt", false);
            else
                coordinatesStream = new StreamWriter("joints.txt", true);

            foreach (Joint joint in skeleton.Joints)
            {
                if (joint.JointType == JointType.HipCenter  ||
                    joint.JointType == JointType.KneeLeft  ||
                    joint.JointType == JointType.KneeRight ||
                    joint.JointType == JointType.FootLeft ||
                    joint.JointType == JointType.FootRight)        
                {
                    //coordinatesStream.WriteLine(joint.JointType + ", " + joint.TrackingState + ", " + joint.Position.X + ", " + joint.Position.Y + ", " + joint.Position.Z);
                    coordinatesStream.WriteLine(joint.JointType + ", "  + joint.Position.X + ", " + joint.Position.Y + ", " + joint.Position.Z);
                }
            }
            coordinatesStream.Close();

        }

        /*
        void WriteJointsOnFile()
        {
            writer.WriteLine("X Y Z");
            for (int i = 0; i < Image.Bits.Length; i += 2)
            {
                int depthPixel = (Image.Bits[i + 1] << 8) | Image.Bits[i];
                int x = (i / 2) % 640; // Because image width is 320 pixels
                int y = (i / 2) / 640;

                Vector realWorldPos = DepthImageToSkeleton(((float)x) / 640.0f, ((float)y) / 480.0f, (short)depthPixel);

                // Store value of realWorldPos
                writer.WriteLine(realWorldPos.X + " " + realWorldPos.Y + " " + realWorldPos.Z);
            }
            writer.Close();
        }
       */
        #endregion Events
    }
}

