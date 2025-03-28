using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Control
{
    internal class TaskOperation_For : TaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_For();
        }


        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "for";
                case LangaugeType.kEnglish:
                    return "for";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
