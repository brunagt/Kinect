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

namespace SimonSays
{
    /// <summary>
    /// Interaction logic for SkeletonViewer.xaml
    /// </summary>
    public partial class SkeletonViewer : UserControl
    {
        //--------------VARIABLES---------------
        #region Member Variables
        private const float FeetPerMeters = 3.2808399f;
        private readonly Brush[] _SkeletonBrushes = new Brush[] { Brushes.Black, Brushes.Crimson, Brushes.Indigo, Brushes.DodgerBlue, Brushes.Purple, Brushes.Pink };
        private Skeleton[] _FrameSkeletons;
        #endregion Member Variables

        //-------------CONSTRUCTOR---------------
        #region Constructor
        public SkeletonViewer()
        {
            InitializeComponent();
            
        }
        #endregion Constructor

        //---------------------METHODS-------------
        #region Methods
        private void KinectDevice_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            SkeletonsPanel.Children.Clear();
            JointInfoPanel.Children.Clear();

            if (this.IsEnabled)
            {
                using (SkeletonFrame frame = e.OpenSkeletonFrame())
                {
                    if (frame != null)
                    {
                        if (this.IsEnabled)
                        {
                            Brush brush;
                            Skeleton skeleton;
                            frame.CopySkeletonDataTo(this._FrameSkeletons);

                            for (int i = 0; i < this._FrameSkeletons.Length; i++)
                            {

                                skeleton = this._FrameSkeletons[i];
                                brush = this._SkeletonBrushes[i];
                                DrawSkeleton(skeleton, brush);

                                TrackJoint(skeleton.Joints[JointType.HandLeft], brush);
                                TrackJoint(skeleton.Joints[JointType.HandRight], brush);
                                //You can track all the joints if you want
                            }
                        }
                    }
                }
            }
        }
        private void DrawSkeleton(Skeleton skeleton, Brush brush)
        {
            if (skeleton != null && skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                //Draw head and torso
                Polyline figure = CreateFigure(skeleton, brush, new[] { JointType.Head, JointType.ShoulderCenter, JointType.ShoulderLeft, JointType.Spine,
                                                                             JointType.ShoulderRight, JointType.ShoulderCenter, JointType.HipCenter});
                SkeletonsPanel.Children.Add(figure);

                figure = CreateFigure(skeleton, brush, new[] { JointType.HipLeft, JointType.HipRight });
                SkeletonsPanel.Children.Add(figure);

                //Draw left leg
                figure = CreateFigure(skeleton, brush, new[] { JointType.HipCenter, JointType.HipLeft, JointType.KneeLeft, JointType.AnkleLeft, JointType.FootLeft });
                SkeletonsPanel.Children.Add(figure);

                //Draw right leg
                figure = CreateFigure(skeleton, brush, new[] { JointType.HipCenter, JointType.HipRight, JointType.KneeRight, JointType.AnkleRight, JointType.FootRight });
                SkeletonsPanel.Children.Add(figure);

                //Draw left arm
                figure = CreateFigure(skeleton, brush, new[] { JointType.ShoulderLeft, JointType.ElbowLeft, JointType.WristLeft, JointType.HandLeft });
                SkeletonsPanel.Children.Add(figure);

                //Draw right arm
                figure = CreateFigure(skeleton, brush, new[] { JointType.ShoulderRight, JointType.ElbowRight, JointType.WristRight, JointType.HandRight });
                SkeletonsPanel.Children.Add(figure);
            }
        }

        private void TrackJoint(Joint joint, Brush brush)
        {
            if(joint.TrackingState != JointTrackingState.NotTracked)
            {
                Canvas container = new Canvas();
                Point jointPoint = GetJointPoint(joint);

                //FeetPerMeters is a class constant of 3.2808399f;
                double z = joint.Position.Z * FeetPerMeters;

                Ellipse element = new Ellipse();
                element.Height  = 10;
                element.Width   = 10;
                element.Fill    = brush;
                Canvas.SetLeft(element, 0 - (element.Width / 2));
                Canvas.SetTop(element, 0 - (element.Height / 2));
                container.Children.Add(element);

                TextBlock positionText  = new TextBlock();
                positionText.Text = string.Format("<{0:0.00}, {1:0.00}, {2:0.00}>",
                                                   jointPoint.X, jointPoint.Y, z);
                positionText.Foreground = brush;
                positionText.FontSize   = 24;
                Canvas.SetLeft(positionText, 0 - (positionText.Width / 2));
                Canvas.SetTop(positionText, 25);
                container.Children.Add(positionText);

                Canvas.SetLeft(container, jointPoint.X);
                Canvas.SetTop(container, jointPoint.Y);

                JointInfoPanel.Children.Add(container);
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
            DepthImagePoint point = this.KinectDevice.MapSkeletonPointToDepth(joint.Position,
                                                                this.KinectDevice.DepthStream.Format);
            point.X *= (int)this.JointInfoPanel.ActualWidth / this.KinectDevice.DepthStream.FrameWidth;
            point.Y *= (int)this.JointInfoPanel.ActualHeight / this.KinectDevice.DepthStream.FrameHeight;

            return new Point(point.X, point.Y);
        }

        #endregion Methods

        //--------------PROPERTIES------------
        #region Properties
        #region KinectDevice
        protected const string KinectDevicePropertyName = "KinectDevice";

        public static readonly DependencyProperty KinectDeviceProperty =
                               DependencyProperty.Register(KinectDevicePropertyName,
                                                     typeof(KinectSensor),
                                                     typeof(SkeletonViewer),
                                                     new PropertyMetadata(null, KinectDeviceChanged));

        private static void KinectDeviceChanged(DependencyObject owner,
                                                 DependencyPropertyChangedEventArgs e)
        {
            SkeletonViewer viewer = (SkeletonViewer)owner;

            if (e.OldValue != null)
            {
                KinectSensor sensor;
                sensor = (KinectSensor)e.OldValue;
                sensor.SkeletonFrameReady -= viewer.KinectDevice_SkeletonFrameReady;
            }

            if (e.NewValue != null)
            {
                viewer.KinectDevice = (KinectSensor)e.NewValue;
                viewer.KinectDevice.SkeletonFrameReady += viewer.KinectDevice_SkeletonFrameReady;
                viewer._FrameSkeletons = new Skeleton[viewer.KinectDevice.SkeletonStream.FrameSkeletonArrayLength];

            }
        }

        public KinectSensor KinectDevice
        {
            get { return (KinectSensor)GetValue(KinectDeviceProperty); }
            set { SetValue(KinectDeviceProperty, value); }
        }
        #endregion KinectDevice
        #endregion Properties
    }
}

