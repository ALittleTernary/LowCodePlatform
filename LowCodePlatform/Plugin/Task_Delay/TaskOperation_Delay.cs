using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Delay
{
    internal class TaskOperation_Delay : TaskOperationPluginBase
    {
        private string _timeType = string.Empty;
        private int _delayTime = 0;

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Delay();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "延时";
                case LangaugeType.kEnglish:
                    return "Delay";
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
            _timeType = (string)(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _delayTime = (int)(inputParams[1].ActualParam);
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            if (_timeType == "秒") {
                Thread.Sleep(_delayTime * 1000);
            }
            else if (_timeType == "毫秒") {
                Thread.Sleep(_delayTime);
            }
            else { 
            
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }
    }
}
