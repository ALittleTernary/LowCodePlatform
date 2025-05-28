using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace LowCodePlatform.Plugin.Task_Sort
{
    internal class TaskOperation_Sort : TaskOperationPluginBase
    {
        private string _dataType = string.Empty;
        private string _sortOrder = string.Empty;
        private List<int> _inputIntList = new List<int>();
        private List<float> _inputFloatList = new List<float>();
        private List<double> _inputDoubleList = new List<double>();
        private List<string> _inputStringList = new List<string>();
        private List<TimeSpan> _inputTimeSpanList = new List<TimeSpan>();

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Sort();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "排序";
                case LangaugeType.kEnglish:
                    return "Sort";
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
            _dataType = Convert.ToString(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _sortOrder = Convert.ToString(inputParams[1].ActualParam);

            _inputIntList.Clear();
            _inputFloatList.Clear();
            _inputDoubleList.Clear();
            _inputStringList.Clear();
            _inputTimeSpanList.Clear();
            if (inputParams.Count < 3) {
                return TaskNodeStatus.kFailure;
            }
            else if (_dataType == "kListInt" && inputParams[2].ActualParam.GetType() == typeof(List<int>)) {
                _inputIntList = inputParams[2].ActualParam as List<int>;
            }
            else if (_dataType == "kListFloat" && inputParams[2].ActualParam.GetType() == typeof(List<float>)) {
                _inputFloatList = inputParams[2].ActualParam as List<float>;
            }
            else if (_dataType == "kListDouble" && inputParams[2].ActualParam.GetType() == typeof(List<double>)) {
                _inputDoubleList = inputParams[2].ActualParam as List<double>;
            }
            else if (_dataType == "kListString" && inputParams[2].ActualParam.GetType() == typeof(List<string>)) {
                _inputStringList = inputParams[2].ActualParam as List<string>;
            }
            else if (_dataType == "kListTimeSpan" && inputParams[2].ActualParam.GetType() == typeof(List<string>)) {
                List<string> datas = inputParams[2].ActualParam as List<string>;
                foreach (string str in datas) {
                    try {
                        _inputTimeSpanList.Add(TimeSpan.Parse(str));
                    }
                    catch (Exception ex) {
                        Log.Error(str + "转为TimeSpan格式失败:" + ex);
                        return TaskNodeStatus.kFlowStop;
                    }
                }
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            if (_dataType == "kListInt" && _sortOrder == "升序") {
                _inputIntList.Sort();
            }
            else if (_dataType == "kListInt" && _sortOrder == "降序") {
                _inputIntList.Sort((a, b) => b.CompareTo(a));
            }
            else if (_dataType == "kListFloat" && _sortOrder == "升序") {
                _inputFloatList.Sort();
            }
            else if (_dataType == "kListFloat" && _sortOrder == "降序") {
                _inputFloatList.Sort((a, b) => b.CompareTo(a));
            }
            else if (_dataType == "kListDouble" && _sortOrder == "升序") {
                _inputDoubleList.Sort();
            }
            else if (_dataType == "kListDouble" && _sortOrder == "降序") {
                _inputDoubleList.Sort((a, b) => b.CompareTo(a));
            }
            else if (_dataType == "kListString" && _sortOrder == "升序") {
                _inputStringList.Sort();
            }
            else if (_dataType == "kListString" && _sortOrder == "降序") {
                _inputStringList.Sort((a, b) => b.CompareTo(a));
            }
            else if (_dataType == "kListTimeSpan" && _sortOrder == "升序") {
                _inputTimeSpanList.Sort();
            }
            else if (_dataType == "kListTimeSpan" && _sortOrder == "降序") {
                _inputTimeSpanList.Sort((a, b) => b.CompareTo(a));
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            if (_dataType == "kListInt") {
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "kListInt排序结果",
                    ActualParam = _inputIntList,
                    Description = "kListInt排序结果"
                });
            }
            else if (_dataType == "kListFloat") {
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "kListFloat排序结果",
                    ActualParam = _inputFloatList,
                    Description = "kListFloat排序结果"
                });
            }
            else if (_dataType == "kListDouble") {
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "kListDouble排序结果",
                    ActualParam = _inputDoubleList,
                    Description = "kListDouble排序结果"
                });
            }
            else if (_dataType == "kListString") {
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "kListString排序结果",
                    ActualParam = _inputStringList,
                    Description = "kListString排序结果"
                });
            }
            else if (_dataType == "kListTimeSpan") {
                List<string> strings = new List<string>();
                foreach (var item in _inputTimeSpanList) {
                    strings.Add(item.ToString());
                }
                outputParams.Add(new TaskOperationOutputParams() {
                    ParamName = "kListTimeSpan排序结果",
                    ActualParam = strings,
                    Description = "kListTimeSpan排序结果"
                });
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }
    }
}
