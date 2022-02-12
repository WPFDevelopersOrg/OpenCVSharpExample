using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Management;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace OpenCVSharpExample
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        private VideoCapture capCamera;
        private Mat matImage = new Mat();
        private Thread cameraThread;

        public List<string> CameraArray
        {
            get { return (List<string>)GetValue(CameraArrayProperty); }
            set { SetValue(CameraArrayProperty, value); }
        }

        public static readonly DependencyProperty CameraArrayProperty =
            DependencyProperty.Register("CameraArray", typeof(List<string>), typeof(MainWindow), new PropertyMetadata(null));



        public int CameraIndex
        {
            get { return (int)GetValue(CameraIndexProperty); }
            set { SetValue(CameraIndexProperty, value); }
        }

        public static readonly DependencyProperty CameraIndexProperty =
            DependencyProperty.Register("CameraIndex", typeof(int), typeof(MainWindow), new PropertyMetadata(0));



        

        public MainWindow()
        {
            InitializeComponent();
            Width = SystemParameters.WorkArea.Width / 1.5;
            Height = SystemParameters.WorkArea.Height / 1.5;
            this.Loaded += MainWindow_Loaded;

        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeCamera();
        }
        private void ComboBoxCamera_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraArray.Count - 1 < CameraIndex)
                return;

            if (capCamera != null && cameraThread != null)
            {
                cameraThread.Abort();
                StopDispose();
            }

            capCamera = new VideoCapture(CameraIndex);
            capCamera.Fps = 30;
            CreateCamera();
            
        }

        private void InitializeCamera()
        {
            CameraArray = GetAllConnectedCameras();
        }
        List<string> GetAllConnectedCameras()
        {
            var cameraNames = new List<string>();
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE (PNPClass = 'Image' OR PNPClass = 'Camera')"))
            {
                foreach (var device in searcher.Get())
                {
                    cameraNames.Add(device["Caption"].ToString());
                }
            }

            return cameraNames;
        }

        void CreateCamera()
        {
            cameraThread = new Thread(PlayCamera);
            cameraThread.Start();
        }

        private void PlayCamera()
        {
            while (capCamera != null && !capCamera.IsDisposed)
            {
                capCamera.Read(matImage);
                if (matImage.Empty()) break;
                Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    var converted = Convert(BitmapConverter.ToBitmap(matImage));
                    imgViewport.Source = converted;
                }));
            }
        }
       
        private void btStop_Click(object sender, RoutedEventArgs e)
        {
            StopDispose();
            btStop.IsEnabled = false;
        }

        void StopDispose()
        {
            if (capCamera != null && capCamera.IsOpened())
            {
                capCamera.Dispose();
                capCamera = null;
            }
        }

        void CreateRecord()
        {
            cameraThread = new Thread(PlayCamera);
            cameraThread.Start();
        }

        BitmapImage Convert(Bitmap src)
        {
            System.Drawing.Image img = src;
            var now = DateTime.Now;
            var g = Graphics.FromImage(img);
            var brush = new SolidBrush(System.Drawing.Color.Red);
            g.DrawString($"北京时间：{ now.ToString("yyyy年MM月dd日 HH:mm:ss")}", new System.Drawing.Font("Arial", 18), brush, new PointF(5, 5));
            brush.Dispose();
            g.Dispose();
            MemoryStream ms = new MemoryStream();
            img.Save(ms, ImageFormat.Bmp);
            ms.Seek(0, SeekOrigin.Begin);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = ms;
            image.EndInit();
            image.Freeze();
            return image;
        }

        protected override void OnClosed(EventArgs e)
        {
            StopDispose();
        }


    }
}
