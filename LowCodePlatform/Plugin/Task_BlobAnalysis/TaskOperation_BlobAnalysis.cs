using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LowCodePlatform.Plugin.Task_BlobAnalysis
{
    internal class TaskOperation_BlobAnalysis : TaskOperationPluginBase
    {
        private Mat _image = null;
        private string _regionType = string.Empty;
        private int _binarizationLowerLimit = 0; 
        private int _binarizationUpperLimit = 0;
        private Mat[] _inputMasks = null;
        private int _regionAnalysisCount = 0;
        private List<RegionAnalysisOptionData> _regionAnalysisOptionDatas = new List<RegionAnalysisOptionData>();
        private Dictionary<string, Func<Mat, Mat[], string, Mat[]>> _regionAnalysisAlgorithmDic = new Dictionary<string, Func<Mat, Mat[], string, Mat[]>>();
        private Mat[] _outputMasks = null;
        private string _featuresData = string.Empty;

        public bool EngineIsRunning { get; set; }

        public TaskOperation_BlobAnalysis() {
            InitData();
        }

        private void InitData() {
            //区域连通
            Func<Mat, Mat[], string, Mat[]> func_RegionConnect = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                Mat labels = new Mat();
                Mat stats = new Mat();
                Mat centroids = new Mat();
                int numLabels = Cv2.ConnectedComponentsWithStats(inputImage, labels, stats, centroids);
                Mat[] outputMasks = new Mat[numLabels - 1];
                // 从1开始，因为0是背景
                for (int componentId = 1; componentId < numLabels; componentId++) {
                    Mat mask = new Mat(inputImage.Size(), MatType.CV_8UC1, Scalar.All(0));
                    Cv2.InRange(labels, new Scalar(componentId), new Scalar(componentId), mask);
                    outputMasks[componentId - 1] = mask;
                }
                labels.Dispose();
                stats.Dispose();
                centroids.Dispose();
                return outputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("区域连通", func_RegionConnect);
            //区域合并
            Func<Mat, Mat[], string, Mat[]> func_RegionMerge = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputMasks.Length == 0) {
                    return new Mat[] { new Mat() };
                }
                Mat mergedMask = new Mat(inputMasks[0].Size(), MatType.CV_8UC1, new Scalar(0));
                // 遍历所有掩膜，按位“或”操作合并它们
                foreach (var mask in inputMasks) {
                    Cv2.BitwiseOr(mergedMask, mask, mergedMask);
                }
                return new Mat[] { mergedMask };
            };
            _regionAnalysisAlgorithmDic.Add("区域合并", func_RegionMerge);
            //圆形膨胀
            Func<Mat, Mat[], string, Mat[]> func_CircleExpansion = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams ==string.Empty) { 
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//圆形膨胀的直径

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(param0, param0));
                    Cv2.Dilate(mask, mask, kernel, iterations: 1);//迭代一次的意思
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("圆形膨胀", func_CircleExpansion);
            //圆形腐蚀
            Func<Mat, Mat[], string, Mat[]> func_CircleCorrosion = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//圆形腐蚀的直径

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(param0, param0));
                    Cv2.Erode(mask, mask, kernel, iterations: 1);//迭代一次的意思
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("圆形腐蚀", func_CircleCorrosion);
            //圆形开运算
            Func<Mat, Mat[], string, Mat[]> func_CircleOpenOperator = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//圆形开运算的直径

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(param0, param0));
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("圆形开运算", func_CircleOpenOperator);
            //圆形闭运算
            Func<Mat, Mat[], string, Mat[]> func_CircleCloseOperator = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//圆形闭运算的直径

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Ellipse, new Size(param0, param0));
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("圆形闭运算", func_CircleCloseOperator);
            //矩形膨胀
            Func<Mat, Mat[], string, Mat[]> func_RectExpansion = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//矩形膨胀的宽
                double param1 = ((double)json["param1"]);//矩形膨胀的高

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(param0, param1));
                    Cv2.Dilate(mask, mask, kernel, iterations: 1);//迭代一次的意思
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("矩形膨胀", func_RectExpansion);
            //矩形腐蚀
            Func<Mat, Mat[], string, Mat[]> func_RectCorrosion = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//矩形腐蚀的宽
                double param1 = ((double)json["param1"]);//矩形腐蚀的高

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(param0, param1));
                    Cv2.Erode(mask, mask, kernel, iterations: 1);//迭代一次的意思
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("矩形腐蚀", func_RectCorrosion);
            //矩形开运算
            Func<Mat, Mat[], string, Mat[]> func_RectOpenOperator = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//矩形开运算的宽
                double param1 = ((double)json["param1"]);//矩形开运算的高

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(param0, param1));
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Open, kernel);
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("矩形开运算", func_RectOpenOperator);
            //矩形闭运算
            Func<Mat, Mat[], string, Mat[]> func_RectCloseOperator = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                if (inputParams == null || inputParams == string.Empty) {
                    return null;
                }
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//矩形开运算的宽
                double param1 = ((double)json["param1"]);//矩形开运算的高

                foreach (var mask in inputMasks) {
                    // 创建一个圆形结构元素（核），这里使用一个 5x5 的椭圆形结构元素
                    Mat kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(param0, param1));
                    Cv2.MorphologyEx(mask, mask, MorphTypes.Close, kernel);
                }
                return inputMasks;
            };
            _regionAnalysisAlgorithmDic.Add("矩形闭运算", func_RectCloseOperator);
            //获取最大区域
            Func<Mat, Mat[], string, Mat[]> func_SelectMaxRegion = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                Mat maxMask = null;
                double maxArea = 0; 
                foreach (var mask in inputMasks) {
                    int totalArea = Cv2.CountNonZero(mask);
                    if (maxMask == null) {
                        maxMask = mask;
                        maxArea = totalArea;
                    }
                    else if (maxArea <= totalArea) {
                        maxMask = mask;
                        maxArea = totalArea;
                    }
                    else {
                        //继续
                    }
                }
                if (maxMask == null) { 
                    return null;
                }
                return new Mat[] { maxMask };
            };
            _regionAnalysisAlgorithmDic.Add("获取最大区域", func_SelectMaxRegion);
            //筛选面积
            Func<Mat, Mat[], string, Mat[]> func_SelectRegionArea = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//面积最小值
                double param1 = ((double)json["param1"]);//面积最大值
                if (param0 > param1) {
                    return null;
                }

                List<Mat> rangeAreaMask = new List<Mat>();
                foreach (var mask in inputMasks) {
                    int totalArea = Cv2.CountNonZero(mask);
                    if (param0 <= totalArea && totalArea <= param1) {
                        rangeAreaMask.Add(mask);
                    }
                }
                return rangeAreaMask.ToArray();
            };
            _regionAnalysisAlgorithmDic.Add("筛选面积", func_SelectRegionArea);
            //筛选中心X
            Func<Mat, Mat[], string, Mat[]> func_SelectRegionCenterX = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//面积最小值
                double param1 = ((double)json["param1"]);//面积最大值
                if (param0 > param1) {
                    return null;
                }
                List<Mat> rangeAreaMask = new List<Mat>();
                foreach (var mask in inputMasks) {
                    // 计算掩膜的矩
                    Moments moments = Cv2.Moments(mask);
                    // 计算质心坐标
                    double cx = moments.M10 / moments.M00;  // X 方向的质心
                    double cy = moments.M01 / moments.M00;  // Y 方向的质心
                    if (param0 <= cx && cy <= param1) {
                        rangeAreaMask.Add(mask);
                    }
                }
                return rangeAreaMask.ToArray();
            };
            _regionAnalysisAlgorithmDic.Add("筛选中心X", func_SelectRegionCenterX);
            //筛选中心Y
            Func<Mat, Mat[], string, Mat[]> func_SelectRegionCenterY = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//面积最小值
                double param1 = ((double)json["param1"]);//面积最大值
                if (param0 > param1) {
                    return null;
                }
                List<Mat> rangeAreaMask = new List<Mat>();
                foreach (var mask in inputMasks) {
                    // 计算掩膜的矩
                    Moments moments = Cv2.Moments(mask);
                    // 计算质心坐标
                    double cx = moments.M10 / moments.M00;  // X 方向的质心
                    double cy = moments.M01 / moments.M00;  // Y 方向的质心
                    if (param0 <= cy && cx <= param1) {
                        rangeAreaMask.Add(mask);
                    }
                }
                return rangeAreaMask.ToArray();
            };
            _regionAnalysisAlgorithmDic.Add("筛选中心Y", func_SelectRegionCenterY);
            //筛选圆度
            Func<Mat, Mat[], string, Mat[]> func_SelectRegionRoundness = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//面积最小值
                double param1 = ((double)json["param1"]);//面积最大值
                if (param0 > param1) {
                    return null;
                }
                List<Mat> rangeAreaMask = new List<Mat>();
                foreach (var mask in inputMasks) {
                    // 找到掩膜中的所有轮廓
                    Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    double totalArea = 0;
                    // 合并所有轮廓
                    Point[] allContours = new Point[0];
                    foreach (var contour in contours) {
                        double contourArea = Cv2.ContourArea(contour);
                        totalArea += contourArea;
                        allContours = allContours.Concat(contour).ToArray();  // 将每个轮廓的点合并
                    }
                    // 计算合并后的轮廓的周长
                    double perimeter = Cv2.ArcLength(allContours, true);
                    // 计算圆度
                    double circularity = (4 * Math.PI * totalArea) / (perimeter * perimeter);
                    if (param0 <= circularity && circularity <= param1) {
                        rangeAreaMask.Add(mask);
                    }
                }
                return rangeAreaMask.ToArray();
            };
            _regionAnalysisAlgorithmDic.Add("筛选圆度", func_SelectRegionRoundness);
            //筛选矩形度
            Func<Mat, Mat[], string, Mat[]> func_SelectRegionRectangularity = (Mat inputImage, Mat[] inputMasks, string inputParams) => {
                JObject json = JObject.Parse(inputParams);
                if (!json.ContainsKey("param0") || ((double)json["param0"]) < 0) {
                    return null;
                }
                if (!json.ContainsKey("param1") || ((double)json["param1"]) < 0) {
                    return null;
                }
                double param0 = ((double)json["param0"]);//面积最小值
                double param1 = ((double)json["param1"]);//面积最大值
                if (param0 > param1) {
                    return null;
                }
                List<Mat> rangeAreaMask = new List<Mat>();
                foreach (var mask in inputMasks) {
                    // 找到掩膜中的所有轮廓
                    Point[][] contours;
                    HierarchyIndex[] hierarchy;
                    Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                    double totalArea = 0;
                    // 合并所有轮廓
                    Point[] allContours = new Point[0];
                    foreach (var contour in contours) {
                        double contourArea = Cv2.ContourArea(contour);
                        totalArea += contourArea;
                        allContours = allContours.Concat(contour).ToArray();  // 将每个轮廓的点合并
                    }
                    // 计算合并后的轮廓的最小外接矩形
                    RotatedRect minRect = Cv2.MinAreaRect(allContours);
                    // 计算矩形度
                    double rectangularity = totalArea / (minRect.Size.Width * minRect.Size.Height);
                    if (param0 <= rectangularity && rectangularity <= param1) {
                        rangeAreaMask.Add(mask);
                    }
                }
                return rangeAreaMask.ToArray();
            };
            _regionAnalysisAlgorithmDic.Add("筛选矩形度", func_SelectRegionRectangularity);
        }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_BlobAnalysis();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "斑点分析";
                case LangaugeType.kEnglish:
                    return "BlobAnalysis";
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
            _image = inputParams[0].ActualParam as Mat;

            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _regionType = inputParams[1].ActualParam as string;

            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _binarizationLowerLimit = Convert.ToInt32(inputParams[2].ActualParam);

            if (inputParams.Count < 4 || inputParams[3].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _binarizationUpperLimit = Convert.ToInt32(inputParams[3].ActualParam);
            //最小和最大阈值范围不对
            if (_binarizationLowerLimit < 0 || _binarizationLowerLimit >= _binarizationUpperLimit || _binarizationUpperLimit > 255) { 
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 5 || inputParams[4].ActualParam.GetType() != typeof(Mat[])) {
                return TaskNodeStatus.kFailure;
            }
            _inputMasks = inputParams[4].ActualParam as Mat[];
            if (inputParams.Count < 6 || inputParams[5].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _regionAnalysisCount = Convert.ToInt32(inputParams[5].ActualParam);
            int index = 6;
            _regionAnalysisOptionDatas.Clear();
            for (int i = 0; i < _regionAnalysisCount; i++) {
                RegionAnalysisOptionData data = new RegionAnalysisOptionData();
                //_Num
                index = index + 1;
                if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(int)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Num = Convert.ToInt32(inputParams[index - 1].ActualParam);
                //_IsSelected
                index = index + 1;
                if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(bool)) {
                    return TaskNodeStatus.kFailure;
                }
                data.IsSelected = Convert.ToBoolean(inputParams[index - 1].ActualParam);
                //_Region
                index = index + 1;
                if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Region = inputParams[index - 1].ActualParam as string;
                //_Name
                index = index + 1;
                if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Name = inputParams[index - 1].ActualParam as string;
                //_Description_Count
                index = index + 1;
                if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(int)) {
                    return TaskNodeStatus.kFailure;
                }
                int descriptionCount = Convert.ToInt32(inputParams[index - 1].ActualParam);
                //_Description
                JObject json = new JObject();
                for (int j = 0; j < descriptionCount; j++) {
                    index = index + 1;
                    if (inputParams.Count < index || inputParams[index - 1].ActualParam.GetType() != typeof(double)) {
                        return TaskNodeStatus.kFailure;
                    }
                    double paramValue = Convert.ToDouble(inputParams[index - 1].ActualParam);
                    json["param" + j] = paramValue;
                    data.Description = json.ToString();
                }
                _regionAnalysisOptionDatas.Add(data);
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            //如果传入的图像是彩色图像，则先转为灰度图像
            Mat image = new Mat();
            if (_image.Channels() != 1) {
                Cv2.CvtColor(_image, image, ColorConversionCodes.BGR2GRAY);
                _image = image;
            }
            else {
                image = _image;
            }

            //记录每一步产生的掩膜,每一步的掩膜是一个mat数组
            List<Mat[]> masksList = new List<Mat[]>();

            if (_regionType == "全图区域") {
                //全图二值化
                //输出的是掩膜
                Mat mask_InRange = new Mat();
                Cv2.InRange(image, new Scalar(_binarizationLowerLimit), new Scalar(_binarizationUpperLimit), mask_InRange);
                image = mask_InRange;
                Mat[] masks = new Mat[] { mask_InRange };
                //Cv2.ImWrite("C:\\Users\\Administrator\\Desktop\\圆环.png", mask_InRange);
                masksList.Add(masks);
            }
            else if (_regionType == "链接区域") {
                Mat mergedMask = new Mat(_inputMasks[0].Size(), MatType.CV_8UC1, new Scalar(0));
                // 遍历所有掩膜，按位“或”操作合并它们
                foreach (var mask in _inputMasks) {
                    Cv2.BitwiseOr(mergedMask, mask, mergedMask);
                }
                image = mergedMask;
                masksList.Add(_inputMasks);
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            for (int i = 0; i < _regionAnalysisOptionDatas.Count; i++) {
                RegionAnalysisOptionData data = _regionAnalysisOptionDatas[i];
                if (!_regionAnalysisAlgorithmDic.ContainsKey(data.Name)) {
                    return TaskNodeStatus.kFailure;
                }

                //当前不启用
                if (!data.IsSelected) {
                    continue;
                }

                Mat[] masks;
                if (int.TryParse(data.Region, out int regionIndex)) {
                    if (regionIndex < 0 || regionIndex >= masksList.Count) {
                        return TaskNodeStatus.kFailure;
                    }
                    masks = masksList[regionIndex];
                }
                else {
                    //转整数失败就是上一掩膜
                    masks = masksList.Last();
                }
                var func = _regionAnalysisAlgorithmDic[data.Name];
                Mat[] outputMasks = func(image, masks, data.Description);
                if (outputMasks == null) {
                    return TaskNodeStatus.kFailure;
                }
                masksList.Add(outputMasks);
            }

            _outputMasks = masksList.Last();

            //把特征计算输出出来
            JObject json_MasksFeatures = new JObject();
            json_MasksFeatures["MasksLength"] = _outputMasks.Length;
            for (int i = 0; i < _outputMasks.Length; i++){
                Mat mask = _outputMasks[i];
                // 计算掩膜的矩
                Moments moments = Cv2.Moments(mask);
                // 计算质心坐标
                double cx = moments.M10 / moments.M00;  // X 方向的质心
                double cy = moments.M01 / moments.M00;  // Y 方向的质心

                // 找到掩膜中的所有轮廓
                Point[][] contours;
                HierarchyIndex[] hierarchy;
                Cv2.FindContours(mask, out contours, out hierarchy, RetrievalModes.External, ContourApproximationModes.ApproxSimple);

                double totalArea = 0;
                // 合并所有轮廓
                Point[] allContours = new Point[0];
                foreach (var contour in contours) {
                    double contourArea = Cv2.ContourArea(contour);
                    totalArea += contourArea;
                    allContours = allContours.Concat(contour).ToArray();  // 将每个轮廓的点合并
                }

                // 计算合并后的轮廓的最小外接矩形
                RotatedRect minRect = Cv2.MinAreaRect(allContours);
                // 计算合并后的轮廓的周长
                double perimeter = Cv2.ArcLength(allContours, true);
                // 计算圆度
                double circularity = (4 * Math.PI * totalArea) / (perimeter * perimeter);
                // 计算矩形度
                double rectangularity = totalArea / (minRect.Size.Width * minRect.Size.Height);

                json_MasksFeatures[i + "_Area"] = Cv2.CountNonZero(mask).ToString() ;
                json_MasksFeatures[i + "_CenterX"] = cx.ToString();
                json_MasksFeatures[i + "_CenterY"] = cy.ToString();
                json_MasksFeatures[i + "_Circularity"] = circularity.ToString();//由轮廓计算得到
                json_MasksFeatures[i + "_Rectangularity"] = rectangularity.ToString();//由轮廓计算得到
            }
            _featuresData = json_MasksFeatures.ToString();


            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "图像",
                ActualParam = _image,
                Description = "当前模块获取到的图像",
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "输出掩膜",
                ActualParam = _outputMasks,
                Description = "斑点分析输出的掩膜，可能没有也可能有一个也可能有很多个,但不会为null,也不会是轮廓"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "掩膜特征数据",
                ActualParam = _featuresData,
                Description = "掩膜的特征数据，根据数据进行进一步筛选"
            });
            return TaskNodeStatus.kSuccess;
        }
    }
}
