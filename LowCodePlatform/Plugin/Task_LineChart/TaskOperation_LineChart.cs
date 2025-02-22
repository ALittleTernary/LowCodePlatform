using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Sub_LineChart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_LineChart
{
    internal class TaskOperation_LineChart : TaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        private SubView_LineChart _LineChart = null;
        private List<double> _XArray = new List<double>();
        private List<double> _YArray = new List<double>();

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_LineChart();
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(SubView_LineChart)) {
                return TaskNodeStatus.kFailure;
            }
            _LineChart = inputParams[0].ActualParam as SubView_LineChart;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(List<double>)) {
                return TaskNodeStatus.kFailure;
            }
            _XArray = inputParams[1].ActualParam as List<double>;
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(List<double>)) {
                return TaskNodeStatus.kFailure;
            }
            _YArray = inputParams[2].ActualParam as List<double>;

            if (_XArray.Count != _YArray.Count) {
                return TaskNodeStatus.kFailure;
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _LineChart.SetAxisData(_XArray, _YArray);
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "折线图";
                case LangaugeType.kEnglish:
                    return "LineChart";
                default:
                    break;
            }
            return string.Empty;
        }


    }
}
