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


namespace TestDepthPlayer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
	public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _Kinect;

        //writable RawDepthImage bitmap variables
        private WriteableBitmap _RawDepthImage;
        private Int32Rect _RawDepthImageRect;
        private int _RawDepthImageStride;

        //Writeable EnhDepthImage bitmap variables
        private WriteableBitmap _EnhDepthImage;
        private Int32Rect _EnhDepthImageRect;
        private int _EnhDepthImageStride;

        private short[] _RawDepthPixelData;
        #endregion Member Variables

        //-----------MAIN WINDOW-----------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

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
                DepthImageStream depthStream = sensor.DepthStream;
                SkeletonStream skeletonStrem = sensor.SkeletonStream;

                sensor.DepthStream.Enable(); //Enable camara
                sensor.SkeletonStream.Enable(); //Enable skeleton stream

                //writeable bitmap RawDepthImage
                this._RawDepthImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight,
                                                            96, 96, PixelFormats.Gray16, null);
                this._RawDepthImageRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this._RawDepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                RawDepthImage.Source = this._RawDepthImage;

                //Writeable bitmap EnhDepthImage
                this._EnhDepthImage = new WriteableBitmap(depthStream.FrameWidth, depthStream.FrameHeight,
                                                            96, 96, PixelFormats.Gray16, null);
                this._EnhDepthImageRect = new Int32Rect(0, 0, depthStream.FrameWidth, depthStream.FrameHeight);
                this._EnhDepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                EnhDepthImage.Source = this._EnhDepthImage;
                
                _RawDepthPixelData = new short[depthStream.FramePixelDataLength];

                sensor.DepthFrameReady += Kinect_DepthFrameReady; //subscribe to depthframe ready
                sensor.Start();
            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.DepthFrameReady -= Kinect_DepthFrameReady;
            }
        }
        #endregion Properties

        #region Events
        //------------EVENTS-----------------

        //COLOR IMAGE FRAME READY
        private void Kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
               if(frame != null)
                {
                    frame.CopyPixelDataTo(this._RawDepthPixelData);
                    this._RawDepthImage.WritePixels(this._RawDepthImageRect, this._RawDepthPixelData,
                                                    this._RawDepthImageStride, 0);
                   GeneratePlayerDepthImage(frame, this._RawDepthPixelData);
               }
           }
        }
        #endregion Events


        #region OtherMethods
        //-------------Other METHODS--------------------
        private void GeneratePlayerDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int playerIndex;
            int depthBytePerPixel = 4;
            byte[] enhPixelData = new byte[depthFrame.Height * depthFrame.Width * depthBytePerPixel];

            for (int i = 0, j = 0; i < pixelData.Length; i++, j += depthBytePerPixel)
            {
                playerIndex = pixelData[i] & DepthImageFrame.PlayerIndexBitmask;

                if (playerIndex == 0)
                {
                    enhPixelData[j] = 0xFF;
                    enhPixelData[j + 1] = 0xFF;
                    enhPixelData[j + 2] = 0xFF;
                }
                else
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
            }

            this._EnhDepthImage.WritePixels(this._EnhDepthImageRect, enhPixelData,
                                            this._EnhDepthImageStride, 0);
        }

        #endregion OtherMethods
    }
}
