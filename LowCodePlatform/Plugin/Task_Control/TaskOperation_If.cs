using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Control
{
    internal class TaskOperation_If : TaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_If();
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
                    return "if";
                case LangaugeType.kEnglish:
                    return "if";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
