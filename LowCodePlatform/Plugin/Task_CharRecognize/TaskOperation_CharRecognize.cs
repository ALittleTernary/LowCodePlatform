using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Delay;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Tesseract;

namespace LowCodePlatform.Plugin.Task_CharRecognize
{
    internal class TaskOperation_CharRecognize : TaskOperationPluginBase
    {
        private Mat _inputImage = null;
        private string _regionType = null;
        private double _regionLeftTopX = -1;
        private double _regionLeftTopY = -1;
        private double _regionWidth = -1;
        private double _regionHeight = -1;
        private Mat[] _inputRegion = null;
        private Mat _outputImage = null;
        private string _charRecognize = string.Empty;

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_CharRecognize();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "字符识别";
                case LangaugeType.kEnglish:
                    return "CharRecognize";
                default:
                    break;
            }
            return string.Empty;
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(Mat)) {
                return TaskNodeStatus.kFailure;
            }
            _inputImage = inputParams[0].ActualParam as Mat;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _regionType = Convert.ToString(inputParams[1].ActualParam);

            if (_regionType == "全图区域") {
                _inputRegion = new Mat[1]{
                    new Mat(_inputImage.Size(), MatType.CV_8UC1, new Scalar(255)) 
                };
            }
            else if (_regionType == "绘制区域") {
                if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(double)) {
                    return TaskNodeStatus.kFailure;
                }
                _regionLeftTopX = Convert.ToDouble(inputParams[2].ActualParam);
                if (inputParams.Count < 4 || inputParams[3].ActualParam.GetType() != typeof(double)) {
                    return TaskNodeStatus.kFailure;
                }
                _regionLeftTopY = Convert.ToDouble(inputParams[3].ActualParam);
                if (inputParams.Count < 5 || inputParams[4].ActualParam.GetType() != typeof(double)) {
                    return TaskNodeStatus.kFailure;
                }
                _regionWidth = Convert.ToDouble(inputParams[4].ActualParam);
                if (inputParams.Count < 6 || inputParams[5].ActualParam.GetType() != typeof(double)) {
                    return TaskNodeStatus.kFailure;
                }
                _regionHeight = Convert.ToDouble(inputParams[5].ActualParam);


                Mat mask = new Mat(_inputImage.Size(), MatType.CV_8UC1, new Scalar(0)); // 0 表示黑色掩膜

                // 4. 在掩膜上绘制一个白色的矩形，代表我们关注的区域
                OpenCvSharp.Rect rect = new OpenCvSharp.Rect((int)_regionLeftTopX, (int)_regionLeftTopY, (int)_regionWidth, (int)_regionHeight);
                Cv2.Rectangle(mask, rect, new Scalar(255), -1); // -1 表示填充矩形区域
                _inputRegion = new Mat[1]{
                    mask
                };
            }
            else if (_regionType == "链接区域") {
                if (inputParams.Count < 7 || inputParams[6].ActualParam.GetType() != typeof(Mat[])) {
                    return TaskNodeStatus.kFailure;
                }
                Mat[] region = inputParams[6].ActualParam as Mat[];

                Mat mergedMask = new Mat(_inputImage.Size(), MatType.CV_8UC1, new Scalar(0));
                // 遍历所有掩膜，按位“或”操作合并它们
                foreach (var mask in region) {
                    Cv2.BitwiseOr(mergedMask, mask, mergedMask);
                }
                _inputRegion = new Mat[1]{
                    mergedMask
                };
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            Func<Mat, Pix> func_Mat2Pix = (Mat mat) => {
                Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat);
                MemoryStream ms = new MemoryStream();
                // 将 Bitmap 对象保存到 MemoryStream 对象中
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Tiff);
                // 从 MemoryStream 对象中获取byte[] 数组
                byte[] bitmapBytes = ms.GetBuffer();
                //将bitmap转化为pix
                Pix pix = Pix.LoadTiffFromMemory(bitmapBytes);
                //Pix pix = PixConverter.ToPix(newbitmap);       
                return pix;
            };

            Func<Mat, Mat, Mat> func_CropImageByMask = (Mat mat, Mat mask) => {
                // Step 1: 获取掩膜的非零区域（即前景区域）
                // 这里使用findContours来获取掩膜图像的轮廓
                OpenCvSharp.Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                // Step 2: 计算最小外接矩形
                // 选择第一个轮廓来计算最小外接矩形
                OpenCvSharp.Rect boundingRect = Cv2.BoundingRect(contours[0]);

                // Step 3: 根据最小外接矩形裁剪原图像
                Mat croppedMat = new Mat(mat, boundingRect);

                return croppedMat;
            };

            _charRecognize = "ocr未能检出字符";

            Mat regionImage = func_CropImageByMask(_inputImage, _inputRegion[0]);

            string tessdataPath = Directory.GetCurrentDirectory() + "\\Resource\\tessdata";
            using (var engine = new TesseractEngine(tessdataPath, "chi_sim", EngineMode.Default)) {
                // 加载图像
                using (var img = func_Mat2Pix(regionImage))  // 将你的图片路径传递进去
                {
                    // 进行 OCR 识别
                    using (var page = engine.Process(img)) {
                        // 获取识别结果
                        _charRecognize = page.GetText();
                    }
                }
            }

            _outputImage = regionImage;
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "图像",
                ActualParam = _outputImage,
                Description = "实际检测图像",
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "字符识别结果",
                ActualParam = _charRecognize,
                Description = "ocr算法的字符识别结果",
            });
            return TaskNodeStatus.kSuccess;
        }
    }
}
