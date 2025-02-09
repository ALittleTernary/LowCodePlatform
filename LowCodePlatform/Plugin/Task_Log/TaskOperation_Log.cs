using LowCodePlatform.Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Log
{
    internal class TaskOperation_Log : TaskOperationPluginBase
    {
        private string _logLevel = string.Empty;
        private string _logContent = string.Empty;

        public bool EngineIsRunning { get; set; }
        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Log();
        }
        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "打印日志";
                case LangaugeType.kEnglish:
                    return "PrintLog";
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
            _logLevel = inputParams[0].ActualParam as string;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _logContent = inputParams[1].ActualParam as string;
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            if (_logLevel == "所有") {
                Log.Verbose(_logContent);
            }
            else if (_logLevel == "调试") {
                Log.Debug(_logContent);
            }
            else if (_logLevel == "一般") {
                Log.Information(_logContent);
            }
            else if (_logLevel == "警告") {
                Log.Warning(_logContent);
            }
            else if (_logLevel == "错误") {
                Log.Error(_logContent);
            }
            else if (_logLevel == "致命") {
                Log.Fatal(_logContent);
            }
            else {
                //return TaskNodeStatus.kFailure;
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }
    }
}
