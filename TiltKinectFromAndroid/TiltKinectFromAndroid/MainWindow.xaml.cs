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
using Microsoft.Kinect;
using System.Threading;
using System.IO;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace TiltKinectFromAndroid
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        //-------------VARIABLES-------------
        #region MemberVariables

        private KinectSensor _Kinect;


        //Depth
        private DepthImageFrame _LastDepthFrame;

        //ColorImage 
        private WriteableBitmap _ColorImageBitmap; //bitmap (DepthImageBitmap if depth)
        private Int32Rect _ColorImageBitmapRect; //image values (DepthImageRect if depth)
        private int _ColorImageStride; // image size (DepthImageStride if depth)

  
        //Server
        private TcpListener tcpListener;
        private Thread listenThread;
        private int action;

        #endregion MemberVariables



        //-----------Connection Methods--------------
        #region ConnectionMethods
        public MainWindow()
        {
            InitializeComponent();
            //Server TCPServer = new Server();

            this.tcpListener = new TcpListener(IPAddress.Any, 3200);
            this.listenThread = new Thread(new ThreadStart(ListenForClients));
            this.listenThread.Start();

            this.Loaded += (s, e) => { DiscoverKinectSensor(); };
            this.Unloaded += (s, e) => { this.Kinect = null; };
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

                    //Debug.Log("hand is over by the right"); 
                    //updates the bitmap
                    this._ColorImageBitmap.WritePixels(this._ColorImageBitmapRect, pixelData,
                                                this._ColorImageStride, 0);
                }
            }
        }


        //starts the tcp listener and accept connections
        private void ListenForClients()
        {
            this.tcpListener.Start();

            while (true)
            {
                //blocks until a client has connected to the server
                System.Diagnostics.Debug.WriteLine("Listening...");
                TcpClient client = this.tcpListener.AcceptTcpClient();
                System.Diagnostics.Debug.WriteLine("Client connected");


                //create a thread to handle communication 
                //with connected client
                Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                clientThread.Start(client);
                //clientThread.Join();
                System.Diagnostics.Debug.WriteLine("client finished");
            }
        }


        //Read the data from the client
        private void HandleClientComm(object client)
        {

            TcpClient tcpClient = (TcpClient)client; //start the client
            NetworkStream clientStream = tcpClient.GetStream(); //get the stream of data for network access

            byte[] message = new byte[4096];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    //blocks until a client sends a message
                    bytesRead = clientStream.Read(message, 0, 4096);
                }
                catch
                {
                    //a socket error has occured
                    break;
                }

                if (bytesRead == 0) //if we receive 0 bytes
                {
                    //the client has disconnected from the server 
                    break;
                }

                //message has successfully been received
                ASCIIEncoding encoder = new ASCIIEncoding();
                String mes = encoder.GetString(message, 0, bytesRead);

                if (mes.CompareTo("U") == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Up");
                    if (_Kinect.ElevationAngle < 27 - 5) //range -27 to 27
                    {
                        _Kinect.ElevationAngle = _Kinect.ElevationAngle + 5;
                    }
                }

                if (mes.CompareTo("D") == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Down");
                    if (_Kinect.ElevationAngle > -27 + 5) //range -27 to 27
                    {
                        _Kinect.ElevationAngle = _Kinect.ElevationAngle - 5;
                    }
                }

                //Reply
                byte[] buffer = encoder.GetBytes("ACK");
                clientStream.Write(buffer, 0, buffer.Length);
                System.Diagnostics.Debug.WriteLine("ACK");
                clientStream.Flush();
            } //while

            tcpClient.Close();
            System.Diagnostics.Debug.WriteLine("Client disconnected");
        }//handle


        #endregion Methods


    }
}



