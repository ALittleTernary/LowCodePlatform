using Newtonsoft.Json.Linq;
using OpenCvSharp;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LowCodePlatform.Plugin.Base
{
    /// <summary>
    /// ImageShow.xaml 的交互逻辑
    /// </summary>
    public partial class ImageShow : UserControl
    {
        private DateTime _imageLastSetTime = DateTime.MinValue;
        private readonly TimeSpan _imageRestInterval = TimeSpan.FromMilliseconds(10);  // 设置休息时间为 30ms
        private DateTime _maskLastSetTime = DateTime.MinValue;
        private readonly TimeSpan _maskRestInterval = TimeSpan.FromMilliseconds(10);  // 设置休息时间为 30ms
        private readonly object _lockObject = new object();  // 锁对象，确保线程安全

        /// <summary>
        /// 区域绘制的颜色列表
        /// </summary>
        private List<Scalar> _colorList = new List<Scalar>{
            new Scalar(255, 0, 0),     // 蓝色
            new Scalar(0, 255, 0),     // 绿色
            new Scalar(0, 0, 255),     // 红色
            new Scalar(255, 255, 0),   // 黄色
            new Scalar(0, 255, 255),   // 青色
            new Scalar(255, 0, 255),   // 品红
            new Scalar(128, 0, 0),     // 深红
            new Scalar(0, 128, 0),     // 深绿
            new Scalar(0, 0, 128),     // 深蓝
            new Scalar(255, 165, 0),   // 橙色
            new Scalar(75, 0, 130),    // 靛蓝
            new Scalar(238, 130, 238), // 紫色
            new Scalar(255, 99, 71),   // 番茄红
            new Scalar(34, 139, 34),   // 森林绿
            new Scalar(255, 215, 0),   // 金色
            new Scalar(0, 0, 0),       // 黑色
            new Scalar( 205, 92, 92), // 白色
            new Scalar(169, 169, 169), // 暗灰色
            new Scalar(210, 105, 30),  // 巧克力色
            new Scalar(255, 20, 147)   // 深粉色
        };

        /// <summary>
        /// 渲染完成标志，这个实践得到没什么用，并不是渲染完成标志位，可以去掉但聊胜于无吧
        /// </summary>
        private bool _isMasksRendering = false; 
        public Mat[] Masks {
            set {
                lock (_lockObject) {
                    // 检查上次赋值与当前时间的差距，如果小于10ms，跳过赋值操作
                    TimeSpan span = DateTime.Now - _maskLastSetTime;
                    if (span < _maskRestInterval) {
                        return;  // 如果在10ms内，忽略新的赋值操作
                    }
                    if (value == null || Image == null) {
                        // 更新上次赋值的时间
                        _maskLastSetTime = DateTime.Now;
                        return;
                    }

                    // 只有在上次渲染完成之后才能开始渲染新图像
                    if (_isMasksRendering) {
                        return; // 如果正在渲染中，跳过更新
                    }

                    // 设置正在渲染标志
                    _isMasksRendering = true;

                    Task.Run(() => {
                        using (MemoryStream stream = new MemoryStream()) {
                            Mat image_MasksShow = new Mat();
                            // 确保 Image 是单通道的灰度图像，并将其转换为 BGR 图像
                            if (Image.Channels() == 1) {
                                Cv2.CvtColor(Image, image_MasksShow, ColorConversionCodes.GRAY2BGR);
                            }
                            else {
                                // 如果 Image 已经是三通道图像，直接拷贝
                                Image.CopyTo(image_MasksShow);
                            }
                            // 确保 image_MasksShow 是 8UC3 类型的图像（即 3 通道图像）
                            if (image_MasksShow.Depth() != MatType.CV_8U || image_MasksShow.Channels() != 3) {
                                return;
                            }
                            for (int i = 0; i < value.Length; i++) {
                                Scalar color = _colorList[i % _colorList.Count];
                                image_MasksShow.SetTo(color, value[i]);
                            }

                            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(image_MasksShow);
                            // 将 Bitmap 保存为 PNG 格式到 MemoryStream
                            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

                            Application.Current.Dispatcher.Invoke(() => {
                                // 重置流的位置到开始
                                stream.Seek(0, SeekOrigin.Begin);

                                // 创建一个新的 BitmapImage 来显示图像
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.StreamSource = stream; // 设置 MemoryStream 作为图像来源
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 缓存图像以便及时释放内存
                                bitmapImage.EndInit();

                                // 显示图像
                                Image_Masks.Source = bitmapImage;
                            });

                            _isMasksRendering = false;
                        }
                        // 更新上次赋值的时间
                        _maskLastSetTime = DateTime.Now;
                    });

                }
            }
        }

        private Mat _image = null;
        private bool _isImageRendering = false; // 渲染完成标志
        /// <summary>
        /// 显示的图片,最好使用队列进行控制
        /// </summary>
        public Mat Image
        {
            get {
                lock (_lockObject) {
                    return _image;
                }
            }
            set {
                lock (_lockObject) {
                    // 检查上次赋值与当前时间的差距，如果小于10ms，跳过赋值操作
                    TimeSpan span = DateTime.Now - _imageLastSetTime;
                    if (span < _imageRestInterval) {
                        return;  // 如果在10ms内，忽略新的赋值操作
                    }

                    // 更新图像
                    _image = value;
                    if (value == null || value.IsDisposed || value.Empty()) {
                        // 更新上次赋值的时间
                        _imageLastSetTime = DateTime.Now;
                        return;
                    }

                    // 只有在上次渲染完成之后才能开始渲染新图像
                    if (_isImageRendering) {
                        return; // 如果正在渲染中，跳过更新
                    }

                    // 设置正在渲染标志
                    _isImageRendering = true;

                    // 先异步把能处理的数据处理了
                    Task.Run(() => {
                        // 渲染图像的过程
                        using (MemoryStream stream = new MemoryStream()) {
                            Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(value);

                            // 将 Bitmap 保存为 PNG 格式到 MemoryStream
                            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                            // 在UI线程中更新Image_Show控件
                            Application.Current.Dispatcher.Invoke(() => {
                                // 创建一个新的 BitmapImage 来显示图像
                                BitmapImage bitmapImage = new BitmapImage();

                                bitmapImage.BeginInit();
                                // 重置流的位置到开始
                                stream.Seek(0, SeekOrigin.Begin);
                                bitmapImage.StreamSource = stream; // 设置 MemoryStream 作为图像来源
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad; // 缓存图像以便及时释放内存
                                bitmapImage.EndInit();

                                Image_Show.Source = bitmapImage;
                                TextBlock_ImageNameHeight.Text = value.Height.ToString();
                                TextBlock_ImageNameWidth.Text = value.Width.ToString();
                                // 渲染完成后，清除渲染标志
                            });
                            _isImageRendering = false;
                        }
                        // 更新上次赋值的时间
                        _imageLastSetTime = DateTime.Now;
                    });
                }
            }
        }



        /// <summary>
        /// 编辑状态
        /// </summary>
        public bool EditEnable {
            get {
                if (Grid_Edit.Visibility == Visibility.Visible) {
                    return true;
                }
                else { 
                    return false;
                }
            }
            set {
                if (value) {
                    Grid_Edit.Visibility = Visibility.Visible;
                }
                else {
                    Grid_Edit.Visibility =Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// 图像的宽
        /// </summary>
        public int ImageWidth
        {
            get {
                int.TryParse(TextBlock_ImageNameWidth.Text, out int width);
                return width;
            }
        }

        /// <summary>
        /// 图像的高
        /// </summary>
        public int ImageHeight
        {
            get {
                int.TryParse(TextBlock_ImageNameHeight.Text, out int height);
                return height;
            }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ImageShow() {
            InitializeComponent();
            InitEvent();
            ResetView();
        }

        private void InitEvent() {
            Image_Show.MouseMove += Event_Image_MouseMove;

        }

        public void ResetView() {
            _image = null;
            System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                Image_Show.Source = null;
                Image_Masks.Source = null;
                TextBlock_ImageNameWidth.Text = "-1";
                TextBlock_ImageNameHeight.Text = "-1";
                TextBlock_PixelRedValue.Text = "-1";
                TextBlock_PixelGreenValue.Text = "-1";
                TextBlock_PixelBlueValue.Text = "-1";
                TextBlock_CoordinateXValue.Text = "-1";
                TextBlock_CoordinateYValue.Text = "-1";
            }));
        }

        private void Event_Image_MouseMove(object sender, MouseEventArgs e) {
            var position = e.GetPosition(Image_Show);

            int x = (int)position.X;
            int y = (int)position.Y;

            BitmapSource bitmapImage = Image_Show.Source as BitmapSource;
            if (bitmapImage == null) {
                return;
            }

            // 获取 Image_Show 的实际缩放比例
            double scaleX = Image_Show.ActualWidth / bitmapImage.PixelWidth;
            double scaleY = Image_Show.ActualHeight / bitmapImage.PixelHeight;

            // 将鼠标坐标从缩放后的显示区域转换回原始图像坐标
            int originalX = (int)(x / scaleX);
            int originalY = (int)(y / scaleY);
            if (originalX >= 0 && originalX < bitmapImage.PixelWidth && originalY >= 0 && originalY < bitmapImage.PixelHeight) {
                
                int[] pixels = new int[1];
                bitmapImage.CopyPixels(new Int32Rect(originalX, originalY, 1, 1), pixels, bitmapImage.PixelWidth * 4, 0);

                byte r, g, b;
                if (bitmapImage.Format == PixelFormats.Bgra32 || bitmapImage.Format == PixelFormats.Bgr32 || bitmapImage.Format == PixelFormats.Rgb24) {
                    // 多通道图像（如彩色图像）
                    int pixel = pixels[0];
                    b = (byte)(pixel & 0xFF);
                    g = (byte)((pixel >> 8) & 0xFF);
                    r = (byte)((pixel >> 16) & 0xFF);
                }
                else if (bitmapImage.Format == PixelFormats.Gray8 || bitmapImage.Format == PixelFormats.Gray16 || bitmapImage.Format == PixelFormats.Indexed8) {
                    // 单通道图像（如灰度图像）
                    byte grayValue = (byte)pixels[0];
                    r = g = b = grayValue;  // 灰度图像的 R、G、B 通道值相同
                }
                else {
                    // 未处理的图像格式，可以根据需要添加更多处理逻辑
                    return;
                }
                // 设置显示的像素值
                System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(r, g, b);

                TextBlock_PixelRedValue.Text = color.R.ToString();
                TextBlock_PixelGreenValue.Text = color.G.ToString();
                TextBlock_PixelBlueValue.Text = color.B.ToString();
                TextBlock_CoordinateXValue.Text = originalX.ToString();
                TextBlock_CoordinateYValue.Text = originalY.ToString();
            }
        }

    }
}

