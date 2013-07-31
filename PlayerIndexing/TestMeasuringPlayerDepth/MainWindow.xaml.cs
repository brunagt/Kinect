
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


namespace TestMeasuringPlayerDepth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
	public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _Kinect;
        short[] _DepthPixelData;
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

                sensor.DepthStream.Enable(); //enable depth stream
                sensor.SkeletonStream.Enable(); //enable skeleton stream

                _DepthPixelData = new short[depthStream.FramePixelDataLength];
                sensor.DepthFrameReady += Kinect_DepthFrameReady; //subscribe to depthframeready event
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


        //----------------DEPTH FRAME READY-------------------
        private void Kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    frame.CopyPixelDataTo(this._DepthPixelData);
                    CreateLighterShadesOfGray(frame, this._DepthPixelData);
                    CalculatePlayerSize(frame, this._DepthPixelData);
                }
            }
        }



        private void CalculatePlayerSize(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            int playerIndex;
            int pixelIndex;
            int bytesPerPixel = depthFrame.BytesPerPixel;
            PlayerDepthData[] players = new PlayerDepthData[6];


            //First pass - Calculate stats from the pixel data
            for (int row = 0; row < depthFrame.Height; row++)
            {
                for (int col = 0; col < depthFrame.Width; col++)
                {
                    pixelIndex = col + (row * depthFrame.Width);
                    depth = pixelData[pixelIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                    if (depth != 0)
                    {
                        playerIndex = (pixelData[pixelIndex] & DepthImageFrame.PlayerIndexBitmask);
                        playerIndex -= 1;

                        if (playerIndex > -1)
                        {
                            if (players[playerIndex] == null)
                            {
                                players[playerIndex] = new PlayerDepthData(playerIndex + 1,
                                                        depthFrame.Width, depthFrame.Height);
                            }

                            players[playerIndex].UpdateData(col, row, depth);
                        }
                    }
                }
            }


            PlayerDepthData.ItemsSource = players;
        }

        //CREATE A LIGHTER IMAGE (better)
        private void CreateLighterShadesOfGray(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            int gray; //
            int loThreshold = 1220;
            int hiThreshold = 3048;
            int bytesPerPixel = 4; //
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel]; //byte instead of short


            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)//agregated j
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth < loThreshold || depth > hiThreshold)
                {
                    //enhPixelData[i] = 0xFF;
                    gray = 0xFF;//
                }
                else
                {
                    //enhPixelData[i] = (short)~pixelData[i];
                    gray = (255 * depth / 0xFFF); //
                }

                enhPixelData[j] = (byte)gray;
                enhPixelData[j + 1] = (byte)gray;
                enhPixelData[j + 2] = (byte)gray;
            }

            DepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                                                  96, 96, PixelFormats.Bgr32, null,
                                                  enhPixelData, depthFrame.Width * bytesPerPixel); //bgr32 instead gray16

        }


        #endregion Events
    }
}

