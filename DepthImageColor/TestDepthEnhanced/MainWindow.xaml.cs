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

namespace TestDepthEnhanced
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor _Kinect;
        private DepthImageFrame _LastDepthFrame;

        private WriteableBitmap _RawDepthImage;
        private Int32Rect _RawDepthImageRect;
        private int _RawDepthImageStride;

        private short[] _DepthImagePixelData;

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
                sensor.DepthStream.Enable(); //Enable depth camara

                //WRITEABLE BITMAP
                this._RawDepthImage = new WriteableBitmap(depthStream.FrameWidth,
                                                     depthStream.FrameHeight, 96, 96,
                                                     PixelFormats.Gray16, null);
                this._RawDepthImageRect = new Int32Rect(0, 0, depthStream.FrameWidth,
                                                            depthStream.FrameHeight);

                this._RawDepthImageStride = depthStream.FrameWidth * depthStream.FrameBytesPerPixel;
                DepthImage.Source = this._RawDepthImage;

                _DepthImagePixelData = new short[depthStream.FramePixelDataLength];

                //start sensor

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

        #region Events
        //------------EVENTS-----------------

        //COLOR IMAGE FRAME READY
        private void Kinect_DepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            if (this._LastDepthFrame != null)
            {
                this._LastDepthFrame.Dispose();
                this._LastDepthFrame = null;
            }

            this._LastDepthFrame = e.OpenDepthImageFrame();

            if (this._LastDepthFrame != null)
            {
                this._LastDepthFrame.CopyPixelDataTo(this._DepthImagePixelData);
                this._RawDepthImage.WritePixels(this._RawDepthImageRect, this._DepthImagePixelData,
                                                this._RawDepthImageStride, 0);
                CreateLighterShadesOfGray(_LastDepthFrame, _DepthImagePixelData);
                CreateColorDepthImage(_LastDepthFrame, _DepthImagePixelData);
            }
        }

        #endregion Events

        #region Method

        //CREATE A LIGHTER IMAGE (DARK=FAR, LIGH=NEAR)
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

            /*EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                                                            96, 96, PixelFormats.Gray16, null,
                                                            enhPixelData,
                                                            depthFrame.Width *
                                                            depthFrame.BytesPerPixel); */

            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                                                  96, 96, PixelFormats.Bgr32, null,
                                                  enhPixelData, depthFrame.Width * bytesPerPixel); //bgr32 instead gray16

        }

        //CREATE A COLOR DEPTH IMAGE
        private void CreateColorDepthImage(DepthImageFrame depthFrame, short[] pixelData)
        {
            int depth;
            double hue;
            int loThreshold = 1220;
            int hiThreshold = 3048;
            int bytesPerPixel = 4;
            byte[] rgb = new byte[3];
            byte[] enhPixelData = new byte[depthFrame.Width * depthFrame.Height * bytesPerPixel];


            for (int i = 0, j = 0; i < pixelData.Length; i++, j += bytesPerPixel)
            {
                depth = pixelData[i] >> DepthImageFrame.PlayerIndexBitmaskWidth;

                if (depth < loThreshold || depth > hiThreshold)
                {
                    enhPixelData[j] = 0x00;
                    enhPixelData[j + 1] = 0x00;
                    enhPixelData[j + 2] = 0x00;
                }
                else
                {
                    hue = ((360 * depth / 0xFFF) + loThreshold);
                    ConvertHslToRgb(hue, 100, 100, rgb);

                    enhPixelData[j] = rgb[2]; //Blue
                    enhPixelData[j + 1] = rgb[1]; //Green
                    enhPixelData[j + 2] = rgb[0]; //Red
                }
            }

            EnhancedDepthImage.Source = BitmapSource.Create(depthFrame.Width, depthFrame.Height,
                                                            96, 96, PixelFormats.Bgr32, null,
                                                            enhPixelData,
                                                            depthFrame.Width * bytesPerPixel);
        }


        public void ConvertHslToRgb(Double hue, Double saturation, Double lightness, byte[] rgb)
        {
            Double red = 0.0;
            Double green = 0.0;
            Double blue = 0.0;
            hue = hue % 360.0;
            saturation = saturation / 100.0;
            lightness = lightness / 100.0;

            if (saturation == 0.0)
            {
                red = lightness;
                green = lightness;
                blue = lightness;
            }
            else
            {
                Double huePrime = hue / 60.0;
                Int32 x = (Int32)huePrime;
                Double xPrime = huePrime - (Double)x;
                Double L0 = lightness * (1.0 - saturation);
                Double L1 = lightness * (1.0 - (saturation * xPrime));
                Double L2 = lightness * (1.0 - (saturation * (1.0 - xPrime)));

                switch (x)
                {
                    case 0:
                        red = lightness;
                        green = L2;
                        blue = L0;
                        break;
                    case 1:
                        red = L1;
                        green = lightness;
                        blue = L0;
                        break;
                    case 2:
                        red = L0;
                        green = lightness;
                        blue = L2;
                        break;
                    case 3:
                        red = L0;
                        green = L1;
                        blue = lightness;
                        break;
                    case 4:
                        red = L2;
                        green = L0;
                        blue = lightness;
                        break;
                    case 5:
                        red = lightness;
                        green = L0;
                        blue = L1;
                        break;
                }
            }

            rgb[0] = (byte)(255.0 * red);
            rgb[1] = (byte)(255.0 * green);
            rgb[2] = (byte)(255.0 * blue);
        }
    }
        #endregion Methods
}

