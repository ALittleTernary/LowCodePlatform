using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Res_Tcp;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Media3D;

namespace LowCodePlatform.Plugin.Res_Camera
{

    public delegate void USBCameraNotify(Mat mat);

    public class USBCamera
    {
        /// <summary>
        /// 线程锁
        /// </summary>
        private object _lockImage = new object();
        private object _lockCamera = new object();
        private object _lockNotify = new object();

        private Thread _cameraThread = null;
        private CancellationTokenSource _cancelTokenSource = new CancellationTokenSource();
        private bool _cameraThreadStatus = false;

        private USBCameraNotify _notify = null;

        private bool _refresh = false;
        private Mat _image = null;
        /// <summary>
        /// 内部使用，从相机取到图后，给这个
        /// </summary>
        private Mat RefreshImage { 
            set {
                lock (_lockImage) {
                    _image = value;
                    _refresh = true;
                }
            } 
        }
        /// <summary>
        /// 最后取到的图，重复取还有
        /// </summary>
        public Mat FinalImage
        {
            get {
                lock (_lockImage) {
                    return _image;
                }
            }
        }
        /// <summary>
        /// 最新取到的图，重复取没有，直到刷新
        /// </summary>
        public Mat LastestImage
        {
            get {
                lock (_lockImage) {
                    if (!_refresh) {
                        return null;
                    }
                    _refresh = false;
                    return _image;
                }
            }
        }
    
        /// <summary>
        /// 实例化的相机
        /// </summary>
        private VideoCapture _camera = null;

        /// <summary>
        /// 相机索引
        /// </summary>
        private int _cameraIndex = -1;
        /// <summary>
        /// 构造函数
        /// </summary>
        public USBCamera(int index) {
            _cameraIndex = index;
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~USBCamera() {
            Stop();
        }

        public bool Start() {
            lock (_lockCamera) {
                if (_camera != null) {
                    Log.Warning("相机" + _cameraIndex + "已经开启");
                    return false;
                }
                _camera = new VideoCapture(_cameraIndex);
                // 检查相机是否成功打开
                if (!_camera.IsOpened()) {
                    Log.Warning("无法打开USB相机" + _cameraIndex);
                    _camera = null;
                    return false;
                }

                _cameraThread = new Thread(() => {
                    // 创建一个 Mat 用于存储捕获的图像
                    using (Mat frame = new Mat()) {
                        Mat imgResized = new Mat();
                        while (!_cancelTokenSource.Token.IsCancellationRequested) {
                            // 从相机读取一帧图像
                            _camera.Read(frame);

                            // 如果帧为空，则继续
                            if (frame.Empty()) {
                                continue;
                            }
                            ////修改分辨率用于测试
                            Cv2.Resize(frame, imgResized, new OpenCvSharp.Size(2560, 1440), 0, 0, InterpolationFlags.Linear);
                            RefreshImage = imgResized;
                            lock (_lockNotify) {
                                _notify?.Invoke(imgResized);
                            }

                            //RefreshImage = frame;
                            //lock (_lockNotify) {
                            //    _notify?.Invoke(frame);
                            //}

                            //别取图太快就行，在差不多40ms以下一帧就行
                            Thread.Sleep(10);
                        }
                    }
                });
                _cameraThread.Start();
                return true;
            }
        }

        public void Stop() {
            lock (_lockCamera) {
                if (_camera == null) {
                    Log.Warning("相机" + _cameraIndex + "已经关闭");
                    return;
                }
                _cancelTokenSource.Cancel();
                _cameraThread.Join();
                _cameraThread = null;
                _camera.Release();
                _camera = null;
                Log.Warning("相机" + _cameraIndex + "已经关闭");
            }
        }

        public void SetCameraExposure(int expType, double exposure) {
            lock (_lockCamera) {
                if (_camera == null) {
                    return;
                }
                if (expType == 0) {
                    _camera.Set(VideoCaptureProperties.AutoExposure, 1);
                }
                else{
                    _camera.Set(VideoCaptureProperties.Exposure, exposure);
                }
            }
        }

        public void SetCameraGain(double gain) {
            lock (_lockCamera) {
                if (_camera == null) { 
                    return;
                }
                _camera.Set(VideoCaptureProperties.Gain, 0.75);
            }
        }

        public void SetUSBCameraNotifyCallback(USBCameraNotify cb) {
            lock (_lockNotify) {
                if (cb == null) {
                    return;
                }
                _notify = cb;
            }
        }
    }

    internal class ResOperation_USBCamera : ResOperationPluginBase {

        /// <summary>
        /// 发布信息函数
        /// </summary>
        private ResMessageLaunch _resMessageLaunch = null;

        /// <summary>
        /// 锁对象
        /// </summary>
        private static readonly object _lockObject = new object();

        /// <summary>
        /// 相机实例
        /// </summary>
        private USBCamera _camera = null;

        /// <summary>
        /// 最后的图像，可重复
        /// </summary>
        public Mat FinalImage { 
            get {
                lock (_lockObject) {
                    if (_camera == null) {
                        return null;
                    }
                    return _camera.FinalImage;
                }
            }
        }

        /// <summary>
        /// 最新的图像，再取就没有
        /// 可能创建一个线程去等待会出现大量资源消耗,这里先不管
        /// todo
        /// </summary>
        public Mat LastestImage {
            get {
                // 创建一个 1 秒的超时任务
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                Task timeoutTask = Task.Delay(1000, cancellationTokenSource.Token); // 超过 1 秒后返回 null

                // 通过异步任务尝试获取图像
                var getImageTask = Task.Run(() =>
                {
                    while (!cancellationTokenSource.Token.IsCancellationRequested) {
                        //正常来说不会有人跟你竞争
                        lock (_lockObject) {
                            if (_camera == null) { 
                                return null;
                            }
                            // 如果 LastestImage 不为 null，则返回该图像
                            Mat mat = _camera.LastestImage;
                            if (mat != null) {
                                return mat;
                            }
                        }
                    }

                    return null;
                });

                // 等待超时或任务完成，哪个先完成就返回
                var completedTask = Task.WhenAny(getImageTask, timeoutTask).Result;

                // 如果超时任务先完成，则取消获取图像的任务并返回 null
                if (completedTask == timeoutTask) {
                    cancellationTokenSource.Cancel();
                    return null;
                }

                // 否则返回获取的图像
                return getImageTask.Result;            
            }
        }

        public ResOperationPluginBase Clone() {
            return new ResOperation_USBCamera();
        }

        public void Dispose() {
            _camera?.Stop();
            _camera = null;
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "USB相机";
                case LangaugeType.kEnglish:
                    return "USBCamera";
                default:
                    break;
            }
            return string.Empty;
        }

        public bool ResStatus { get; set; }

        public void ResTemporaryEvent(string temporaryParams) {
            lock (_lockObject) {
                if (string.IsNullOrEmpty(temporaryParams) || _camera == null) {
                    return;
                }
                JObject json = JObject.Parse(temporaryParams);
                string action = json["Action"].ToString();
                if (action == "开始采图") {
                    _camera.SetUSBCameraNotifyCallback((Mat mat) => {
                        List<ResOperationOutputParams> datas = new List<ResOperationOutputParams>();
                        datas.Add(new ResOperationOutputParams() {
                            ParamName = "相机状态",
                            ActualParam = "相机正在显示取图",
                        });
                        datas.Add(new ResOperationOutputParams() {
                            ParamName = "相机图像",
                            ActualParam = mat,
                        });
                        _resMessageLaunch?.Invoke(datas);
                    });
                }
                else if (action == "关闭采图") {
                    _camera.SetUSBCameraNotifyCallback((Mat mat) => {

                    });
                    List<ResOperationOutputParams> datas = new List<ResOperationOutputParams>();
                    datas.Add(new ResOperationOutputParams() {
                        ParamName = "相机状态",
                        ActualParam = "相机处于连接中",
                    });
                    _resMessageLaunch?.Invoke(datas);
                }
                else {

                }
            }
        }

        public void SetResMessageLaunchCallback(ResMessageLaunch cb) {
            if (cb == null) {
                return;
            }
            _resMessageLaunch = cb;
        }

        public void TurnOffRes(List<ResViewInputParams> inputParams) {
            lock (_lockObject) {
                _camera.Stop();
                _camera = null ;
                ResStatus = false ;

                List<ResOperationOutputParams> datas = new List<ResOperationOutputParams>();
                datas.Add(new ResOperationOutputParams() {
                    ParamName = "相机状态",
                    ActualParam = "相机断开连接",
                });
                _resMessageLaunch?.Invoke(datas);
            }
        }

        public void TurnOnRes(List<ResViewInputParams> inputParams) {
            lock (_lockObject) {
                if (inputParams == null) {
                    return;
                }
                if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(int)) {
                    return;
                }
                int index = Convert.ToInt32(inputParams[0].ActualParam);
                if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(int)) {
                    return;
                }
                int expType = Convert.ToInt32(inputParams[1].ActualParam);
                if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(double)) {
                    return;
                }
                double exposure = Convert.ToDouble(inputParams[2].ActualParam);
                if (inputParams.Count < 4 || inputParams[3].ActualParam.GetType() != typeof(double)) {
                    return;
                }
                double gain = Convert.ToDouble(inputParams[3].ActualParam);

                List<ResOperationOutputParams> datas = new List<ResOperationOutputParams>();
                //启动相机
                _camera = new USBCamera(index);
                if (!_camera.Start()) {
                    ResStatus = false ;
                    datas.Add(new ResOperationOutputParams() {
                        ParamName = "相机状态",
                        ActualParam = "相机初始化失败",
                    });
                    _resMessageLaunch?.Invoke(datas);
                    return;
                }
                _camera.SetCameraExposure(expType, exposure);
                _camera.SetCameraGain(gain);
                ResStatus = true ;

                datas.Add(new ResOperationOutputParams() {
                    ParamName = "相机状态",
                    ActualParam = "相机处于连接中",
                });
                _resMessageLaunch?.Invoke(datas);
                return;
            }
        }
    }
}
