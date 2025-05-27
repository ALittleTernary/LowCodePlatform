using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_TemplateMatch
{
    internal class TaskOperation_TemplateMatch : TaskOperationPluginBase
    {
        private Mat _targetImage = null;

        private Mat _templateImage = null;

        private double _matchingThreshold = -1;

        private Mat[] _outputMasks = null;

        private ObservableCollection<TemplateMatchData> _templateMatchDatas = new ObservableCollection<TemplateMatchData>();

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_TemplateMatch();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "模板匹配";
                case LangaugeType.kEnglish:
                    return "TemplateMatch";
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
            _targetImage = inputParams[0].ActualParam as Mat;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(Mat)) {
                return TaskNodeStatus.kFailure;
            }
            _templateImage = inputParams[1].ActualParam as Mat;
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(double)) {
                return TaskNodeStatus.kFailure;
            }
            _matchingThreshold = Convert.ToDouble(inputParams[2].ActualParam);

            if (_matchingThreshold < 0) {
                _matchingThreshold = 0;
            }
            else if (_matchingThreshold > 1) {
                _matchingThreshold = 1;
            }
            else { 
                //其他情况不处理
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            //聚类相近点
            Func<List<(Point, double)>, double, List<(Point, double)>> func_MergeClosePoints = (List<(Point, double)> points, double threshold) => {
                List<(Point, double)> mergedPoints = new List<(Point, double)>();

                foreach (var point in points) {
                    bool isMerged = false;

                    // 遍历已合并的点，检查是否与已有点接近
                    foreach (var mergedPoint in mergedPoints) {
                        double distance = Math.Sqrt(Math.Pow(point.Item1.X - mergedPoint.Item1.X, 2) + Math.Pow(point.Item1.Y - mergedPoint.Item1.Y, 2));
                        if (distance <= threshold) {
                            isMerged = true;
                            break;
                        }
                    }

                    // 如果没有接近的点，则将当前点添加到结果中
                    if (!isMerged) {
                        mergedPoints.Add(point);
                    }
                }

                return mergedPoints;
            };


            _templateMatchDatas.Clear();

            // 创建一个结果矩阵来存储匹配结果
            Mat result = new Mat();

            // 执行模板匹配
            Cv2.MatchTemplate(_targetImage, _templateImage, result, TemplateMatchModes.CCoeffNormed);

            // 查找所有匹配的点
            List<(Point, double)> matchingPoints = new List<(Point, double)>();
            for (int i = 0; i < result.Rows; i++) {
                for (int j = 0; j < result.Cols; j++) {
                    if (result.At<float>(i, j) > _matchingThreshold) {
                        matchingPoints.Add((new Point(j, i), result.At<float>(i, j))); // 添加符合阈值的匹配点
                    }
                }
            }
            double distanceThreshold = 50;
            // 聚类：将相近的点合并为一个点
            List<(Point, double)> clusterPoints = func_MergeClosePoints(matchingPoints, distanceThreshold);

            // 创建一个List来存储所有符合阈值条件的掩膜
            List<Mat> masks = new List<Mat>();
            foreach (var item in clusterPoints) {
                // 为每个匹配点创建一个新的掩膜
                Mat mask = Mat.Zeros(_targetImage.Size(), MatType.CV_8UC1);
                Rect matchRect = new Rect(item.Item1.X, item.Item1.Y, _templateImage.Width, _templateImage.Height);

                // 在新的掩膜上设置对应区域的像素值为255
                Cv2.Rectangle(mask, matchRect, new Scalar(255), -1); // -1 填充矩形

                // 将这个掩膜添加到List中
                masks.Add(mask);

                _templateMatchDatas.Add(new TemplateMatchData() { 
                    SerialNumber = masks.Count,
                    InitiationX = item.Item1.X,
                    InitiationY = item.Item1.Y,
                    MatchValue = item.Item2,
                });
            }

            _outputMasks = masks.ToArray();

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "匹配区域",
                ActualParam = _outputMasks,
                Description = "模板匹配的区域",
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "目标图像",
                ActualParam = _targetImage,
                Description = "待处理图像",
                LinkVisual = false,
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "模板图像",
                ActualParam = _templateImage,
                Description = "匹配模板",
                LinkVisual = false,
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "模板匹配数据",
                ActualParam = _templateMatchDatas,
                Description = string.Empty,
                LinkVisual = false,
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "模板匹配个数",
                ActualParam = _templateMatchDatas.Count,
                Description = "模板匹配出的结果个数",
            });

            return TaskNodeStatus.kSuccess;
        }

    }
}
