 
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


namespace TestChromaDepth
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
	public partial class MainWindow : Window
    {
        #region Member Variables
        private KinectSensor KinectDevice;

        //writablebitmap
        private WriteableBitmap _GreenScreenImage;
        private Int32Rect _GreenScreenImageRect;
        private int _GreenScreenImageStride;

        //Data buffers
        private short[] _DepthPixelData;
        private byte[] _ColorPixelData;

        #endregion Member Variables

        //-----------MAIN WINDOW-----------------
        #region Constructor
        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += (s, e) => { DiscoverKinect(); };
            this.Unloaded += (s, e) => { this.KinectDevice = null; };
        }

       
        #endregion Constructor
    private void CompositionTarget_Rendering(object sender, EventArgs e)
    {
        DiscoverKinect();  


        if(this.KinectDevice != null)
        {
            try
            {
                ColorImageStream colorStream = this.KinectDevice.ColorStream;
                DepthImageStream depthStream = this.KinectDevice.DepthStream;

                using(ColorImageFrame colorFrame = colorStream.OpenNextFrame(100))
                {
                    using(DepthImageFrame depthFrame = depthStream.OpenNextFrame(100))
                    {
                       RenderGreenScreen(this.KinectDevice, colorFrame, depthFrame);
                    }
                }
            }
            catch(Exception)
            {
                //Handle exception as needed
            }
        }
    }


    private void DiscoverKinect()
    {
        if(this._KinectDevice != null && this._KinectDevice.Status != KinectStatus.Connected)
        {
            this._KinectDevice.ColorStream.Disable();
            this._KinectDevice.DepthStream.Disable();
            this._KinectDevice.SkeletonStream.Disable();
            this._KinectDevice.Stop();
            this._KinectDevice = null;
        }


        if(this._KinectDevice == null)
        {
            this._KinectDevice = KinectSensor.KinectSensors.FirstOrDefault(x => x.Status ==
                                                                          KinectStatus.Connected);


            if(this._KinectDevice != null)
            {
               this._KinectDevice.SkeletonStream.Enable();
               this._KinectDevice.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
               this._KinectDevice.ColorStream.Enable(ColorImageFormat.RgbResolution1280x960Fps12);

               DepthImageStream depthStream = this._KinectDevice.DepthStream;
               this._GreenScreenImage       = new WriteableBitmap(depthStream.FrameWidth,
                                                                  depthStream.FrameHeight, 96, 96,
                                                                  PixelFormats.Bgra32, null);
               this._GreenScreenImageRect   = new Int32Rect(0, 0,
                                                          (int) Math.Ceiling(depthStream.Width),
                                                          (int) Math.Ceiling(depthStream.Height));
               this._GreenScreenImageStride = depthStream.FrameWidth * 4;              
               this.GreenScreenImage.Source = this._GreenScreenImage;

               this._DepthPixelData = new short[depthStream.FramePixelDataLength];
               int colorFramePixelDataLength =
               this._ColorPixelData = new
              byte[this._KinectDevice.ColorStream.FramePixelDataLength];

               this._KinectDevice.Start();
            }
        }
        }

private void RenderGreenScreen(KinectSensor kinectDevice, ColorImageFrame colorFrame,
                               DepthImageFrame depthFrame)
    {
    if(kinectDevice != null && depthFrame != null && colorFrame != null)
    {
        int depthPixelIndex;
        int playerIndex;
        int colorPixelIndex;
        ColorImagePoint colorPoint;
        int colorStride         = colorFrame.BytesPerPixel * colorFrame.Width;
        int bytesPerPixel       = 4;
        byte[] playerImage      = new byte[depthFrame.Height * this._GreenScreenImageStride];
        int playerImageIndex    = 0;

        depthFrame.CopyPixelDataTo(this._DepthPixelData);
        colorFrame.CopyPixelDataTo(this._ColorPixelData);


        for(int depthY = 0; depthY < depthFrame.Height; depthY++)
        {
            for(int depthX = 0; depthX < depthFrame.Width; depthX++, playerImageIndex += bytesPerPixel)
            {
                depthPixelIndex = depthX + (depthY * depthFrame.Width);
                playerIndex     = this._DepthPixelData[depthPixelIndex] DepthImageFrame.PlayerIndexBitmask;

                if(playerIndex != 0)
                {
                    colorPoint = kinectDevice.MapDepthToColorImagePoint(depthX, depthY,
                                              this._DepthPixelData[depthPixelIndex],
                                              colorFrame.Format, depthFrame.Format);
                    colorPixelIndex = (colorPoint.X * colorFrame.BytesPerPixel) +
                                      (colorPoint.Y * colorStride);
                    playerImage[playerImageIndex] =
                                          this._ColorPixelData[colorPixelIndex];  //Blue
                    playerImage[playerImageIndex + 1] =
                                          this._ColorPixelData[colorPixelIndex + 1];  //Green
                    playerImage[playerImageIndex + 2] =
                                          this._ColorPixelData[colorPixelIndex + 2];  //Red
                    playerImage[playerImageIndex + 3] = 0xFF;  //Alpha
                }
            }
        }

        this._GreenScreenImage.WritePixels(this._GreenScreenImageRect, playerImage,
                                           this._GreenScreenImageStride, 0);
    }
        }
    }