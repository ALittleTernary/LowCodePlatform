using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Arithmetic
{
    public class TaskOperation_Arithmetic : TaskOperationPluginBase
    {
        private string _calType = string.Empty;
        private double _inputParams01 = 0;
        private double _inputParams02 = 0;
        private double _outputParams01 = 0;

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Arithmetic();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "四则运算";
                case LangaugeType.kEnglish:
                    return "Arithmetic";
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
            _calType = Convert.ToString(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(double)) { 
                return TaskNodeStatus.kFailure;
            }
            _inputParams01 = Convert.ToDouble(inputParams[1].ActualParam);

            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(double)) {
                return TaskNodeStatus.kFailure;
            }
            _inputParams02 = Convert.ToDouble(inputParams[2].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            if (_calType == "加") {
                _outputParams01 = _inputParams01 + _inputParams02;
            }
            else if (_calType == "减") {
                _outputParams01 = _inputParams01 - _inputParams02;
            }
            else if (_calType == "乘") {
                _outputParams01 = _inputParams01 * _inputParams02;
            }
            else if (_calType == "除") {
                if (_inputParams02 == 0) {
                    _outputParams01 = 0;
                }
                else {
                    _outputParams01 = _inputParams01 / _inputParams02;
                }
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "int结果",
                ActualParam = (int)_outputParams01,
                Description = "四则运算输出结果"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "float结果",
                ActualParam = (float)_outputParams01,
                Description = "四则运算输出结果"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "double结果",
                ActualParam = _outputParams01,
                Description = "四则运算输出结果"
            });

            return TaskNodeStatus.kSuccess;
        }
    }
}
