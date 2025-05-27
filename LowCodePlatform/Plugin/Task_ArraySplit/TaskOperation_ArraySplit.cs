using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using OpenCvSharp;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_ArraySplit
{
    internal class TaskOperation_ArraySplit : TaskOperationPluginBase
    {
        private string _arrayType = string.Empty;
        private int _arrayIndex = 0;

        private List<string> _inputListString = new List<string>();
        private Mat[] _inputListMask = null;

        private List<TaskOperationOutputParams> _outputParams = new List<TaskOperationOutputParams>();

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_ArraySplit();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "数组拆分";
                case LangaugeType.kEnglish:
                    return "ArraySplit";
                default:
                    break;
            }
            return string.Empty;
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _arrayType = Convert.ToString(inputParams[0].ActualParam);

            _inputListString.Clear();
            _inputListMask = null ;
            if (inputParams.Count < 2) {
                return TaskNodeStatus.kFailure;
            }
            else if (_arrayType == "kListString" && inputParams[1].ActualParam.GetType() == typeof(List<string>)) {
                _inputListString = inputParams[1].ActualParam as List<string>;
            }
            else if (_arrayType == "kRegion" && inputParams[1].ActualParam.GetType() == typeof(Mat[])) {
                _inputListMask = inputParams[1].ActualParam as Mat[];
            }
            else {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _arrayIndex = Convert.ToInt32(inputParams[2].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _outputParams.Clear();
            if (_arrayType == "kListString") {
                if (_arrayIndex < 0 || _arrayIndex >= _inputListString.Count) {
                    Log.Error("kListString数组越界，数组长度为" + _inputListString.Count + "，数组索引为" + _arrayIndex);
                    return TaskNodeStatus.kFailure;
                }

                _outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = $"kListString中的元素",
                    ActualParam = _inputListString[_arrayIndex],
                    Description = $"kListString中的第{_arrayIndex}元素",
                });
            }
            else if (_arrayType == "kRegion") {
                if (_arrayIndex < 0 || _arrayIndex >= _inputListMask.Length) {
                    Log.Error("kRegion数组越界，数组长度为" + _inputListMask.Length + "，数组索引为" + _arrayIndex);
                    return TaskNodeStatus.kFailure;
                }
                Mat[] region = new Mat[1] {
                    _inputListMask[_arrayIndex],
                };

                // 查找所有非零像素的位置
                Mat nonZeroPoints = new Mat();
                Cv2.FindNonZero(_inputListMask[_arrayIndex], nonZeroPoints);  // 查找所有非零像素

                int topLeftX = -1;
                int topLeftY = -1;
                if (nonZeroPoints.Rows > 0) {
                    // 获取第一个非零像素的坐标
                    Point topLeft = nonZeroPoints.At<Point>(0);  // 左上角的坐标
                    topLeftX = topLeft.X;
                    topLeftY = topLeft.Y;
                }

                _outputParams.Add(
                    new TaskOperationOutputParams() {
                    ParamName = $"输出区域",
                    ActualParam = region,
                    Description = $"输入区域中的第{_arrayIndex}元素",
                });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                    ParamName = $"输出区域左上角intX",
                    ActualParam = topLeftX,
                    Description = $"输入区域中的第{_arrayIndex}元素,左上角X",
                });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                    ParamName = $"输出区域左上角intY",
                    ActualParam = topLeftY,
                    Description = $"输入区域中的第{_arrayIndex}元素,左上角Y",
                });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                    ParamName = $"输出区域左上角floatX",
                    ActualParam = (float)topLeftX,
                    Description = $"输入区域中的第{_arrayIndex}元素,左上角X",
                });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                        ParamName = $"输出区域左上角floatY",
                        ActualParam = (float)topLeftY,
                        Description = $"输入区域中的第{_arrayIndex}元素,左上角Y",
                    });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                    ParamName = $"输出区域左上角doubleX",
                    ActualParam = (double)topLeftX,
                    Description = $"输入区域中的第{_arrayIndex}元素,左上角X",
                });
                _outputParams.Add(
                    new TaskOperationOutputParams() {
                        ParamName = $"输出区域左上角doubleY",
                        ActualParam = (double)topLeftY,
                        Description = $"输入区域中的第{_arrayIndex}元素,左上角Y",
                });
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = _outputParams;
            return TaskNodeStatus.kSuccess;
        }
    }
}
