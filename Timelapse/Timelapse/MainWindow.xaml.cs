using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Kinect;
using System.Drawing;
using System.IO;


namespace Timelapse
{
    /// <summary>
    /// Generates a timelapse taking a picture every 5 seconds
    /// </summary>

    public partial class MainWindow : Window
    {
        //-------------VARIABLES-------------
        #region MemberVariables

        private KinectSensor _Kinect;

        //ColorImage 
        private WriteableBitmap _ColorImageBitmap; //bitmap (DepthImageBitmap if depth)
        private Int32Rect _ColorImageBitmapRect; //image values (DepthImageRect if depth)
        private int _ColorImageStride; // image size (DepthImageStride if depth)

        private string pathFolder;
        private string pathFile = null;

        private int indexPicture = 0;

        #endregion MemberVariables


        //-----------Connection Methods--------------
        #region ConnectionMethods
        public MainWindow()
        {


            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };

            //Create the new directory
            pathFolder = System.IO.Path.Combine(Environment.CurrentDirectory, @"Timelapse");
            if (Directory.Exists(pathFolder))
                Directory.Delete(pathFolder, true); //remove old directories and pictures
            System.IO.Directory.CreateDirectory(pathFolder); //create the new directory


            // Set the timer to take a picutre every 10 second
            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            dispatcherTimer.Interval = new TimeSpan(0, 0, 0, 0, 5000); //take picture every 5 seconds
            dispatcherTimer.Start();

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
        #endregion ConnectionMethods


        //------------INITIALIZE / UNINITIALIZE KINECT---------
        #region InitializeKinect
        private void InitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                //COLORIMAGE
                sensor.ColorStream.Enable(); //Enable camera
                ColorImageStream colorStream = sensor.ColorStream; //for bitmap

                //create new bitmap
                this._ColorImageBitmap = new WriteableBitmap(colorStream.FrameWidth, colorStream.FrameHeight,
                                                            96, 96, PixelFormats.Bgr32, null);
                this._ColorImageBitmapRect = new Int32Rect(0, 0, colorStream.FrameWidth,
                                                            colorStream.FrameHeight);
                this._ColorImageStride = colorStream.FrameWidth * colorStream.FrameBytesPerPixel;
                //bitmap at the image (visual)
                ColorImageElement.Source = this._ColorImageBitmap; //in xamp  <Image x:Name="ColorImageElement" />

                sensor.ColorFrameReady += Kinect_ColorFrameReady; //create event when there is a camera stream (30fps)

                sensor.Start();

            }
        }

        private void UninitializeKinectSensor(KinectSensor sensor)
        {
            if (sensor != null)
            {
                sensor.Stop();
                sensor.ColorFrameReady -= Kinect_ColorFrameReady;
            }
        }



        #endregion InitializeKinect



        #region Methods



        private void Kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    byte[] pixelData = new byte[imageFrame.PixelDataLength]; //bytearray to hold the data
                    imageFrame.CopyPixelDataTo(pixelData); //copy the data to the array pixeldata

                    //updates the bitmap (we are going to use it for the snapshoot)
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData,
                                                this._ColorImageStride, 0);
                }
            }
        }


        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            Console.WriteLine("timer");
            indexPicture++;
            savePictureAsPNG();
            /*
            ContextBoundObject context;
            try
            {
               context.WaitAndUpdateAll();
            }
            catch (Exception)
            {
            }
            UpdateDepth();
              */
        }


        private void savePictureAsPNG()
        {
            // create a png bitmap encoder which knows how to save a .png file
            BitmapEncoder encoder = new PngBitmapEncoder();

            // create frame from the writable bitmap and add to encoder
            encoder.Frames.Add(BitmapFrame.Create(this._ColorImageBitmap));

            //string time = System.DateTime.Now.ToString("hh'-'mm'-'ss", CultureInfo.CurrentUICulture.DateTimeFormat);

            //string myPhotos = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            //string path = "C:\\Develop\\VisualStudioWorkspace\\Projects\\My programs\\Pictures\\";
            // string path = Path.Combine(myPhotos, "KinectSnapshot-" + time + ".png");
            // string path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Image1.png");
            //string path = @"c:\temp\MyTest.txt";

            pathFile = System.IO.Path.Combine(Environment.CurrentDirectory, @"Timelapse\Picture" + indexPicture + ".png");
            Console.WriteLine(pathFile);


            //Remove the older pictures taken
            if (File.Exists(pathFile))
            {
                Console.WriteLine("file exists");
                File.Delete(pathFile);
            }

            // write the new file to disk
            try
            {
                using (FileStream fs = new FileStream(pathFile, FileMode.Create))
                {
                    encoder.Save(fs);
                }
                Console.WriteLine("picture saved correctly");
                /// this.statusBarText.Text = string.Format("{0} {1}", Properties.Resources.ScreenshotWriteSuccess, path);
            }
            catch (IOException)
            {
                Console.WriteLine("error when saving the picture");
                //this.statusBarText.Text = string.Format("{0} {1}", Properties.Resources.ScreenshotWriteFailed, path);
            }
        }

        #endregion Methods


    }
}


