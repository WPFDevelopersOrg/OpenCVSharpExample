using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OpenCVSharpExample
{
    public class WriteableBitmapHelper
    {
        //将Bitmap 转换成WriteableBitmap 
        public static WriteableBitmap BitmapToWriteableBitmap(System.Drawing.Bitmap src)
        {
            var wb = CreateCompatibleWriteableBitmap(src);
            System.Drawing.Imaging.PixelFormat format = src.PixelFormat;
            if (wb == null)
            {
                wb = new WriteableBitmap(src.Width, src.Height, 0, 0, System.Windows.Media.PixelFormats.Bgra32, null);
                format = System.Drawing.Imaging.PixelFormat.Format32bppArgb;
            }
            BitmapCopyToWriteableBitmap(src, wb, new System.Drawing.Rectangle(0, 0, src.Width, src.Height), 0, 0, format);
            return wb;
        }
        //创建尺寸和格式与Bitmap兼容的WriteableBitmap
        public static WriteableBitmap CreateCompatibleWriteableBitmap(System.Drawing.Bitmap src)
        {
            System.Windows.Media.PixelFormat format;
            switch (src.PixelFormat)
            {
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb555:
                    format = System.Windows.Media.PixelFormats.Bgr555;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format16bppRgb565:
                    format = System.Windows.Media.PixelFormats.Bgr565;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format24bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr24;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppRgb:
                    format = System.Windows.Media.PixelFormats.Bgr32;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppPArgb:
                    format = System.Windows.Media.PixelFormats.Pbgra32;
                    break;
                case System.Drawing.Imaging.PixelFormat.Format32bppArgb:
                    format = System.Windows.Media.PixelFormats.Bgra32;
                    break;
                default:
                    return null;
            }
            return new WriteableBitmap(src.Width, src.Height, 0, 0, format, null);
        }
        //将Bitmap数据写入WriteableBitmap中
        public static void BitmapCopyToWriteableBitmap(System.Drawing.Bitmap src, WriteableBitmap dst, System.Drawing.Rectangle srcRect, int destinationX, int destinationY, System.Drawing.Imaging.PixelFormat srcPixelFormat)
        {
            var data = src.LockBits(new System.Drawing.Rectangle(new System.Drawing.Point(0, 0), src.Size), System.Drawing.Imaging.ImageLockMode.ReadOnly, srcPixelFormat);
            dst.WritePixels(new Int32Rect(srcRect.X, srcRect.Y, srcRect.Width, srcRect.Height), data.Scan0, data.Height * data.Stride, data.Stride, destinationX, destinationY);
            src.UnlockBits(data);
        }

        public static BitmapImage ConvertWriteableBitmapToBitmapImage(WriteableBitmap wbm)
        {
            BitmapImage bmImage = new BitmapImage();
            using (MemoryStream stream = new MemoryStream())
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(wbm));
                encoder.Save(stream);
                bmImage.BeginInit();
                bmImage.CacheOption = BitmapCacheOption.OnLoad;
                bmImage.StreamSource = stream;
                bmImage.EndInit();
                bmImage.Freeze();
            }
            return bmImage;
        }
    }
}
