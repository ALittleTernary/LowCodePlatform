using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_DataLoad
{
    internal class TaskOperation_TxtLoad : TaskOperationPluginBase
    {
        private string _fileName = string.Empty;

        private string _fileContent = string.Empty;
        private List<string> _fileLines = new List<string>();

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_TxtLoad();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "txt加载";
                case LangaugeType.kEnglish:
                    return "txtLoad";
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
            _fileName = Convert.ToString(inputParams[0].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _fileContent = File.ReadAllText(_fileName);
            _fileLines = File.ReadAllLines(_fileName).ToList();

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "整个文件内容",
                ActualParam = _fileContent,
                Description = "整个文本读成一个string"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "每行内容",
                ActualParam = _fileLines,
                Description = "整个文本读成List<string>"
            });

            return TaskNodeStatus.kSuccess;
        }
    }
}
