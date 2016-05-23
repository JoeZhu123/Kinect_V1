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
using System.Windows.Forms;

namespace Kinect_PPT_Control
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor kinectSensor;        
        private bool isForwardGestureActive;
        private bool isBackGestureActive;
        private byte[] pixelData;
        private Skeleton[] skeletonData;
        public MainWindow()
        {
            InitializeComponent();
        }       

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            kinectSensor = (from sensor in KinectSensor.KinectSensors
                            where sensor.Status == KinectStatus.Connected
                            select sensor).FirstOrDefault();
            kinectSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
            kinectSensor.SkeletonStream.Enable();

            kinectSensor.Start();
            kinectSensor.ColorFrameReady += kinectSensor_ColorFrameReady;
            kinectSensor.SkeletonFrameReady += new EventHandler<SkeletonFrameReadyEventArgs>(kinectSensor_SkeletonFrameReady);
        }
        private void Window_Closed(object sender, EventArgs e)
        {
            kinectSensor.Stop();
        }
        private void kinectSensor_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame imageFrame = e.OpenColorImageFrame())
            {
                if (imageFrame != null)
                {
                    this.pixelData = new byte[imageFrame.PixelDataLength];
                    imageFrame.CopyPixelDataTo(this.pixelData);
                    this.ColorImage.Source = BitmapSource.Create(imageFrame.Width, imageFrame.Height, 96, 96,
                                                PixelFormats.Bgr32, null, pixelData, imageFrame.Width * imageFrame.BytesPerPixel);
                }
            }
        }
        private void kinectSensor_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletonData = new Skeleton[kinectSensor.SkeletonStream.FrameSkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(this.skeletonData);
                    Skeleton skeleton = (from s in skeletonData where s.TrackingState == SkeletonTrackingState.Tracked select s).FirstOrDefault();
                    if (skeleton != null)
                    {
                        SkeletonCanvas.Visibility = Visibility.Visible;
                        ProcessGesture(skeleton);
                    }
                }
            }
        }

        private void ProcessGesture(Skeleton s)
        {
            Joint leftHand = (from j in s.Joints
                              where j.JointType == JointType.HandLeft
                              select j).FirstOrDefault();
            Joint rightHand = (from j in s.Joints
                              where j.JointType == JointType.HandRight
                              select j).FirstOrDefault();
            Joint head = (from j in s.Joints
                              where j.JointType == JointType.Head
                              select j).FirstOrDefault();
            if (rightHand.Position.X > head.Position.X + 0.45)
            {
                if (!isBackGestureActive && !isForwardGestureActive)
                {
                    isForwardGestureActive = true;
                    System.Windows.Forms.SendKeys.SendWait("{Right}");
                }
            }
            else
            {
                isForwardGestureActive = false;
            }
            if (leftHand.Position.X < head.Position.X - 0.45)
            {
                if (!isBackGestureActive && !isForwardGestureActive)
                {
                    isBackGestureActive = true;
                    System.Windows.Forms.SendKeys.SendWait("{Left}");
                }
            }
            else
            {
                isBackGestureActive = false;
            }
            //将头和左右手的骨骼点与界面上对应的原点相关联
            SetEllipsePosition(ellipseHead, head, false);
            SetEllipsePosition(ellipseLeftHand, leftHand, false);
            SetEllipsePosition(ellipseRightHand, rightHand, false);
        }
        private void SetEllipsePosition(Ellipse ellipse, Joint joint, bool isHighlighted)
        {
            ColorImagePoint colorImagePoint = kinectSensor.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, ColorImageFormat.RgbResolution640x480Fps30);
            if (isHighlighted)
            {
                ellipse.Width = 60;
                ellipse.Height = 60;
                ellipse.Fill = Brushes.Red;
            }
            else
            {
                ellipse.Width = 20;
                ellipse.Height = 20;
                ellipse.Fill = Brushes.Blue;
            }
            Canvas.SetLeft(ellipse, colorImagePoint.X - ellipse.ActualWidth / 2);
            Canvas.SetTop(ellipse, colorImagePoint.Y - ellipse.ActualHeight / 2);
        }
    }
}
