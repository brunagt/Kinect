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
using Coding4Fun.Kinect.Wpf;

namespace WpfApplication1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            sensor.ColorStream.Enable();
            sensor.DepthStream.Enable();

            //sensor.DepthFrameReady += new EventHandler<DepthImageFrameReadyEventArgs>(sensor_DepthFrameReady);
            //sensor.ColorFrameReady += new EventHandler<ColorImageFrameReadyEventArgs>(sensor_ColorFrameReady);
            sensor.AllFramesReady += new EventHandler<AllFramesReadyEventArgs>(sensor_AllFramesReady);

            sensor.Start();
        }

        public KinectSensor sensor = KinectSensor.KinectSensors[0]; 

        /*
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
          
        }
        */
        void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {
            using (ColorImageFrame cframe = e.OpenColorImageFrame())
            {

                if (cframe == null)
                    return;

                byte[] cbytes = new byte[cframe.PixelDataLength];
                cframe.CopyPixelDataTo(cbytes);

                int stride = cframe.Width *4;

                imgkinect.Source = BitmapImage.Create(640,480,96,96,PixelFormats.Bgr32,null,cbytes,stride);
            }
        }

     

        private void Window_Closed(object sender, EventArgs e)
        {
            sensor.Stop();
        }
    }
}
