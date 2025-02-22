using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_DataCompare
{
    internal class TaskOperation_DataCompare : TaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        private List<CompareData> _compareDatas = new List<CompareData>();

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_DataCompare();
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
             int compareCount = (int)(inputParams[0].ActualParam);

            _compareDatas.Clear();
            for (int i = 0; i < compareCount; i++) {
                CompareData data = new CompareData();

                if (inputParams.Count < 2 + 4 * i + 0 || inputParams[1 + 4 * i + 0].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.DataType = inputParams[1 + 4 * i + 0].ActualParam as string;

                if (data.DataType == "Int") {
                    if (inputParams.Count < 2 + 4 * i + 1 || inputParams[1 + 4 * i + 1].ActualParam.GetType() != typeof(int)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.LeftActualValue = ((int)(inputParams[1 + 4 * i + 1].ActualParam)).ToString();

                    if (inputParams.Count < 2 + 4 * i + 2 || inputParams[1 + 4 * i + 2].ActualParam.GetType() != typeof(string)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.Compare = inputParams[1 + 4 * i + 2].ActualParam as string;

                    if (inputParams.Count < 2 + 4 * i + 3 || inputParams[1 + 4 * i + 3].ActualParam.GetType() != typeof(int)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.RightActualValue = ((int)(inputParams[1 + 4 * i + 3].ActualParam)).ToString();
                }
                else if (data.DataType == "Float") {
                    if (inputParams.Count < 2 + 4 * i + 1 || inputParams[1 + 4 * i + 1].ActualParam.GetType() != typeof(float)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.LeftActualValue = ((float)(inputParams[1 + 4 * i + 1].ActualParam)).ToString();

                    if (inputParams.Count < 2 + 4 * i + 2 || inputParams[1 + 4 * i + 2].ActualParam.GetType() != typeof(string)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.Compare = inputParams[1 + 4 * i + 2].ActualParam as string;

                    if (inputParams.Count < 2 + 4 * i + 3 || inputParams[1 + 4 * i + 3].ActualParam.GetType() != typeof(float)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.RightActualValue = ((float)(inputParams[1 + 4 * i + 3].ActualParam)).ToString();
                }
                else if (data.DataType == "Double") {
                    if (inputParams.Count < 2 + 4 * i + 1 || inputParams[1 + 4 * i + 1].ActualParam.GetType() != typeof(double)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.LeftActualValue = ((double)(inputParams[1 + 4 * i + 1].ActualParam)).ToString();

                    if (inputParams.Count < 2 + 4 * i + 2 || inputParams[1 + 4 * i + 2].ActualParam.GetType() != typeof(string)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.Compare = inputParams[1 + 4 * i + 2].ActualParam as string;

                    if (inputParams.Count < 2 + 4 * i + 3 || inputParams[1 + 4 * i + 3].ActualParam.GetType() != typeof(double)) {
                        return TaskNodeStatus.kFailure;
                    }
                    data.RightActualValue = ((double)(inputParams[1 + 4 * i + 3].ActualParam)).ToString();
                }
                else {
                    return TaskNodeStatus.kFailure;
                }

                _compareDatas.Add(data);
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            foreach (var data in _compareDatas) {
                if (data.DataType == "Int") {
                    int leftValue = int.Parse(data.LeftActualValue);
                    int rightValue = int.Parse(data.RightActualValue);
                    if (data.Compare == "=" && leftValue == rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<" && leftValue < rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">" && leftValue > rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<=" && leftValue <= rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">=" && leftValue >= rightValue) {
                        data.Result = true.ToString();
                    }
                    else {
                        data.Result = false.ToString();
                    }
                }
                else if (data.DataType == "Float") {
                    float leftValue = float.Parse(data.LeftActualValue);
                    float rightValue = float.Parse(data.RightActualValue);
                    if (data.Compare == "=" && leftValue == rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<" && leftValue < rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">" && leftValue > rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<=" && leftValue <= rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">=" && leftValue >= rightValue) {
                        data.Result = true.ToString();
                    }
                    else {
                        data.Result = false.ToString();
                    }
                }
                else if (data.DataType == "Double") {
                    double leftValue = double.Parse(data.LeftActualValue);
                    double rightValue = double.Parse(data.RightActualValue);
                    if (data.Compare == "=" && leftValue == rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<" && leftValue < rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">" && leftValue > rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == "<=" && leftValue <= rightValue) {
                        data.Result = true.ToString();
                    }
                    else if (data.Compare == ">=" && leftValue >= rightValue) {
                        data.Result = true.ToString();
                    }
                    else {
                        data.Result = false.ToString();
                    }
                }
                else {
                    return TaskNodeStatus.kFailure;
                }
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "比较数量",
                ActualParam = _compareDatas.Count,
                Description = "一共比较了多少组数据",
            });
            foreach (var data in _compareDatas) {
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "左实值",
                    ActualParam = data.LeftActualValue,
                    Description = "左实值",
                });
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "右实值",
                    ActualParam = data.RightActualValue,
                    Description = "右实值",
                });
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "比较结果",
                    ActualParam = data.Result,
                    Description = "比较结果",
                });
            }
            return TaskNodeStatus.kSuccess;
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "数据比较";
                case LangaugeType.kEnglish:
                    return "DataCompare";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
