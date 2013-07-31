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
namespace Histograms
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //-------------VARIABLES-------------
        #region Member Variables

        private KinectSensor _Kinect;
        private short[] _DepthPixelData;


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

        private void Kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame frame = e.OpenDepthImageFrame())
            {
                if (frame != null)
                {
                    frame.CopyPixelDataTo(this._DepthPixelData);
                    //CreateBetterShadesOfGray(frame, this._DepthPixelData);  //See Listing 3-8 

                    DepthImage.Source = BitmapSource.Create(frame.Width, frame.Height,
                                                                96, 96, PixelFormats.Gray16, null,
                                                                _DepthPixelData,
                                                                frame.Width* frame.BytesPerPixel);
                    CreateDepthHistogram(frame, this._DepthPixelData);
                }
            }
        }

        private void CreateDepthHistogram(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth=1;
            int[] depths = new int[4096];
            int maxValue = 0;
            double chartBarWidth = DepthHistogram.ActualWidth / depths.Length;

            DepthHistogram.Children.Clear();

            //First pass - Count the depths. 
            for (int i = 0; i < pixelData.Length; i += depthFrame.BytesPerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth > 0)
                {
                    depths[depth]++;
                }
            }

            //Second pass - Find the max depth count to scale the histogram to the space available. 
            //              This is only to make the UI look nice. 
            for (int i = 0; i < depths.Length; i++)
            {
                maxValue = Math.Max(maxValue, depths[i]);
            }

            //Third pass - Build the histogram. 
            for (int i = 0; i < depths.Length; i++)
            {
                if (depths[i] > 0)
                {
                    Rectangle r = new Rectangle();
                    r.Fill = Brushes.Black;
                    r.Width = chartBarWidth;
                    r.Height = DepthHistogram.ActualHeight *
                                          (depths[i] / (double)maxValue);
                    r.Margin = new Thickness(1, 0, 1, 0);
                    r.VerticalAlignment = System.Windows.VerticalAlignment.Bottom;
                    DepthHistogram.Children.Add(r);
                }
            }
        }

       private void CreateBetterShadesOfGray(DepthImageFrame depthFrame , short[] pixelData) 
        {   
            int depth;          
            int gray; 
            int loThreshold         = 1220; 
            int hiThreshold         = 3048; 
            int bytesPerPixel       = 4; 
            byte[] enhPixelData     = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel]; 
 
            for(int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel) 
            { 
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth; 
 
                if(depth < loThreshold || depth > hiThreshold) 
                { 
                    gray = 0xFF; 
                } 
                else 
                {     
                    gray = (255 * depth / 0xFFF); 
                } 
 
                enhPixelData[j]        = (byte) gray; 
                enhPixelData[j + 1]    = (byte) gray; 
                enhPixelData[j + 2]    = (byte) gray;  
                } 
 
                DepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,  
                                                                96, 96, PixelFormats.Bgr32, null,  
                                                                enhPixelData,  
                                                                depthFrame.Width * bytesPerPixel); 
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
                DepthImageStream depthStream = sensor.DepthStream;
                sensor.DepthStream.Enable(); //Enable depth camara

                _DepthPixelData = new short[depthStream.FramePixelDataLength];
                sensor.DepthFrameReady += Kinect_DepthFrameReady; //create event when there is a camera stream (30fps)
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

    }
}