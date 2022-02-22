using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private VideoWriter videoWriter;
        private Mat matImage = new Mat();
        private Thread cameraThread;
        private Thread writerThread;
        private CascadeClassifier haarCascade;
        private WriteableBitmap writeableBitmap;
        private Rectangle rectangle;


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



        public bool IsSave
        {
            get { return (bool)GetValue(IsSaveProperty); }
            set { SetValue(IsSaveProperty, value); }
        }

        public static readonly DependencyProperty IsSaveProperty =
            DependencyProperty.Register("IsSave", typeof(bool), typeof(MainWindow), new UIPropertyMetadata(IsSaveChanged));

        private static void IsSaveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var mainWindow = d as MainWindow;
            if (e.NewValue != null)
            {
                var save = (bool) e.NewValue;
                if (save)
                    mainWindow.StartRecording();
                else
                    mainWindow.StopRecording();
            }
        }
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

           
            CreateCamera();
            writeableBitmap = new WriteableBitmap(capCamera.FrameWidth, capCamera.FrameHeight, 0, 0, System.Windows.Media.PixelFormats.Bgra32, null);
            imgViewport.Source = writeableBitmap;
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
            capCamera = new VideoCapture(CameraIndex);
            capCamera.Fps = 30;
            cameraThread = new Thread(PlayCamera);
            cameraThread.Start();
        }

        private void PlayCamera()
        {
            while (capCamera != null && !capCamera.IsDisposed)
            {
                capCamera.Read(matImage);
                if (matImage.Empty()) break;
                //Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                //{
                //    var converted = Convert(BitmapConverter.ToBitmap(matImage));
                //    imgViewport.Source = converted;
                //}));
                
                using (var img = BitmapConverter.ToBitmap(matImage))
                {
                    var now = DateTime.Now;
                    var g = Graphics.FromImage(img);
                    var brush = new SolidBrush(System.Drawing.Color.Red);
                    g.DrawString($"北京时间：{ now.ToString("yyyy年MM月dd日 HH:mm:ss")}", new System.Drawing.Font("Arial", 18), brush, new PointF(5, 5));
                    rectangle = new Rectangle(0, 0, img.Width, img.Height);
                    brush.Dispose();
                    g.Dispose();
                    Dispatcher.Invoke(new Action(() =>
                    {
                        WriteableBitmapHelper.BitmapCopyToWriteableBitmap(img, writeableBitmap, rectangle, 0, 0, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                    }));
                };

                Thread.Sleep(100);
            }
        }
        private void StartRecording()
        {
            if (capCamera == null)
            {
                WPFDevelopers.Minimal.Controls.MessageBox.Show("未开启摄像机","提示",MessageBoxButton.OKCancel,MessageBoxImage.Error);
                return;
            }
            var videoFile = System.IO.Path.Combine(System.Environment.CurrentDirectory, "Video");
            if (!System.IO.Directory.Exists(videoFile))
                System.IO.Directory.CreateDirectory(videoFile);
            var currentTime = System.IO.Path.Combine(videoFile, $"{DateTime.Now.ToString("yyyyMMddHHmmsshh")}.avi");
            videoWriter = new VideoWriter(currentTime, FourCCValues.XVID, capCamera.Fps, new OpenCvSharp.Size(capCamera.FrameWidth, capCamera.FrameHeight));

            
            writerThread = new Thread(AddCameraFrameToRecording);
            writerThread.Start();
        }
        private void StopRecording()
        {
            if (videoWriter != null && !videoWriter.IsDisposed)
            {
                videoWriter.Release();
                videoWriter.Dispose();
                videoWriter = null;
            }
        }
        private void AddCameraFrameToRecording()
        {
            var waitTimeBetweenFrames = 1_000 / capCamera.Fps;
            var lastWrite = DateTime.Now;

            while (!videoWriter.IsDisposed)
            {
                if (DateTime.Now.Subtract(lastWrite).TotalMilliseconds < waitTimeBetweenFrames)
                    continue;
                lastWrite = DateTime.Now;
                videoWriter.Write(matImage);
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
           
            if (videoWriter != null && !videoWriter.IsDisposed)
            {
                videoWriter.Release();
                videoWriter.Dispose();
                videoWriter = null;
            }
            btPlay.IsEnabled = true;
            GC.Collect();
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
            var writeableBitmap = WriteableBitmapHelper.BitmapToWriteableBitmap(src);
            return WriteableBitmapHelper.ConvertWriteableBitmapToBitmapImage(writeableBitmap);
        }
        protected override void OnClosing(CancelEventArgs e)
        {
            if(WPFDevelopers.Minimal.Controls.MessageBox.Show("是否关闭系统?", "询问", MessageBoxButton.OKCancel, MessageBoxImage.Question) != MessageBoxResult.OK) 
            {
                e.Cancel = true;
                return;
            }
        }
        protected override void OnClosed(EventArgs e)
        {
            StopDispose();
        }

        private void btPlay_Click(object sender, RoutedEventArgs e)
        {
            btPlay.IsEnabled = false;
            btStop.IsEnabled = true;
            CreateCamera();
        }
    }
}
