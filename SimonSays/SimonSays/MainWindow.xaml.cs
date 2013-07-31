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
using System.Windows.Media.Animation;


namespace SimonSays
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public enum GamePhase
        {
            GameOver = 0,
            SimonInstructing = 1,
            PlayerPerforming = 2
        }

        //-------------VARIABLES-------------
        #region Member Variables
        private KinectSensor _Kinect;
        private Skeleton[] _FrameSkeletons;
        private GamePhase _CurrentPhase;
        private int _CurrentLevel;

        private int _InstructionPosition;
        private UIElement[] _InstructionSequence;
        private Random rnd = new Random();

        private IInputElement _LeftHandTarget;
        private IInputElement _RightHandTarget;

        #endregion Member Variables

        //-----------CONSTRUCTOR--------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            this._CurrentPhase = GamePhase.GameOver;
            this._CurrentLevel = 0;
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
                        LeftHandElement.Visibility = Visibility.Collapsed;
                        RightHandElement.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        TrackHand(skeleton.Joints[JointType.HandLeft], LeftHandElement, LayoutRoot);
                        TrackHand(skeleton.Joints[JointType.HandRight], RightHandElement, LayoutRoot);
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
        
        //TRACK HAND
        private void TrackHand(Joint hand, FrameworkElement cursorElement, FrameworkElement container)
        {
            if (hand.TrackingState == JointTrackingState.NotTracked)
            {
                cursorElement.Visibility = Visibility.Collapsed;
            }
            else
            {
                cursorElement.Visibility = Visibility.Visible;
                Point jointPoint = GetJointPoint(this.Kinect, hand, container.RenderSize, new Point(cursorElement.ActualWidth / 2.0, cursorElement.ActualHeight / 2.0));
                Canvas.SetLeft(cursorElement, jointPoint.X);
                Canvas.SetTop(cursorElement, jointPoint.Y);
            }
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

        //PROCESS GAME OVER
        private void ProcessGameOver(Skeleton skeleton)
        {
            //Determine if the user triggers to start of a new game
            if (GetHitTarget(skeleton.Joints[JointType.HandLeft], LeftHandStartElement) != null &&
               GetHitTarget(skeleton.Joints[JointType.HandRight], RightHandStartElement) != null)
            {
                ChangePhase(GamePhase.SimonInstructing);
            }
        }

        //GET HIT TARGET!!
        private IInputElement GetHitTarget(Joint joint, UIElement target)
        {
            //GetjoinPoint: get the coordinates of the join within the coordinate space ofthe LayoutRoot
            Point targetPoint = GetJointPoint(this.Kinect, joint,LayoutRoot.RenderSize, new Point());

            //Translates the joint point in the LayoutRoot space to the target space
            targetPoint = LayoutRoot.TranslatePoint(targetPoint, target);

            //If joint in the target returns the UI element in the target's visual tree
            //No-null value = ok
            return target.InputHitTest(targetPoint);
        }

        //CHANGE PHASE OF THE GAME
        private void ChangePhase(GamePhase newPhase)
        {
            if(newPhase != this._CurrentPhase)
            {
                this._CurrentPhase = newPhase;

                switch(this._CurrentPhase)
                {
                    case GamePhase.GameOver:
                        this._CurrentLevel          = 0;
                        RedBlock.Opacity            = 0.2;
                        BlueBlock.Opacity           = 0.2;
                        GreenBlock.Opacity          = 0.2;
                        YellowBlock.Opacity         = 0.2;

                        GameStateElement.Text           = "GAME OVER!";
                        ControlCanvas.Visibility        = System.Windows.Visibility.Visible;
                        GameInstructionsElement.Text    = "Place hands over the targets to start a new game.";
                        break;

                    case GamePhase.SimonInstructing:
                        this._CurrentLevel++;
                        GameStateElement.Text = string.Format("Level {0}", this._CurrentLevel);
                        ControlCanvas.Visibility        = System.Windows.Visibility.Collapsed;
                        GameInstructionsElement.Text    = "Watch for Simon's instructions";
                        GenerateInstructions();
                        DisplayInstructions();
                        break;

                    case GamePhase.PlayerPerforming:
                        this._InstructionPosition       = 0;
                        GameInstructionsElement.Text    = "Repeat Simon's instructions";
                        break;
                }
            }
        }

        //GENERATE INSTRUCTIONS
        private void GenerateInstructions()
        {
            this._InstructionSequence = new UIElement[this._CurrentLevel];

            for (int i = 0; i < this._CurrentLevel; i++)
            {
                switch (rnd.Next(1, 4))
                {
                    case 1:
                        this._InstructionSequence[i] = RedBlock;
                        break;

                    case 2:
                        this._InstructionSequence[i] = BlueBlock;
                        break;

                    case 3:
                        this._InstructionSequence[i] = GreenBlock;
                        break;

                    case 4:
                        this._InstructionSequence[i] = YellowBlock;
                        break;
                }
            }
        }

        //DISPLAY INSTRUCTIONS
        private void DisplayInstructions()
        {
            Storyboard instructionsSequence = new Storyboard();
            DoubleAnimationUsingKeyFrames animation;

            for (int i = 0; i < this._InstructionSequence.Length; i++)
            {
                animation = new DoubleAnimationUsingKeyFrames();
                animation.FillBehavior = FillBehavior.Stop;
                animation.BeginTime = TimeSpan.FromMilliseconds(i * 1500);
                Storyboard.SetTarget(animation, this._InstructionSequence[i]);
                Storyboard.SetTargetProperty(animation, new PropertyPath("Opacity"));
                instructionsSequence.Children.Add(animation);

                animation.KeyFrames.Add(new EasingDoubleKeyFrame(0.3,
                                            KeyTime.FromTimeSpan(TimeSpan.Zero)));
                animation.KeyFrames.Add(new EasingDoubleKeyFrame(1,
                                            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(500))));
                animation.KeyFrames.Add(new EasingDoubleKeyFrame(1,
                                            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1000))));
                animation.KeyFrames.Add(new EasingDoubleKeyFrame(0.3,
                                            KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1300))));
            }


            instructionsSequence.Completed += (s, e) =>
            {
                ChangePhase(GamePhase.PlayerPerforming);
            };
            instructionsSequence.Begin(LayoutRoot);
        }

        //PROCESS PLAYER PERFORMING
        private void ProcessPlayerPerforming(Skeleton skeleton)
        {
            IInputElement leftTarget;
            IInputElement rightTarget;
            UIElement correctTarget;

            correctTarget = this._InstructionSequence[this._InstructionPosition];
            leftTarget    = GetHitTarget(skeleton.Joints[JointType.HandLeft], GameCanvas);
            rightTarget   = GetHitTarget(skeleton.Joints[JointType.HandRight], GameCanvas);

            if ((leftTarget != this._LeftHandTarget) || (rightTarget != this._RightHandTarget))
            {

                if (leftTarget != null && rightTarget != null)
                {
                    ChangePhase(GamePhase.GameOver);
                }
                else if (leftTarget == null && rightTarget == null)
                {
                    //Do nothing - target found
                }

                else if ((_LeftHandTarget == correctTarget && _RightHandTarget == null) ||
                 (_RightHandTarget == correctTarget && _LeftHandTarget == null))
                {
                    this._InstructionPosition++;

                    if (this._InstructionPosition >= this._InstructionSequence.Length)
                    {
                        ChangePhase(GamePhase.SimonInstructing);
                    }
                }
                else
                {
                    ChangePhase(GamePhase.GameOver);
                }
                if(leftTarget != this._LeftHandTarget)
                 {
                     //AnimateHandLeave(this._LeftHandTarget);
                    // AnimateHandEnter(leftTarget);
                     this._LeftHandTarget = leftTarget;
                 }

                 if(rightTarget != this._RightHandTarget)
                 {
                     //AnimateHandLeave(this._RightHandTarget);
                     //AnimateHandEnter(rightTarget)
                     this._RightHandTarget = rightTarget;
                 }
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

                sensor.SkeletonStream.Enable(); //Enable skeleton
                this._FrameSkeletons = new Skeleton[this._Kinect.SkeletonStream.FrameSkeletonArrayLength];
                sensor.SkeletonFrameReady += Kinect_SkeletonFrameReady; //create event when there is a camera stream (30fps)
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
                SkeletonViewerElement.KinectDevice = null; //TEST WITH skeleton viewer
                this._FrameSkeletons = null;
                this._FrameSkeletons = null;

            }
        }
        #endregion Properties

    }
}