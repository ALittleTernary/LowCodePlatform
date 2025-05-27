using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_DataSave
{
    internal class TaskOperation_TxtSave : TaskOperationPluginBase
    {
        private string _txtSavePath = string.Empty;
        private string _txtFileName = string.Empty;
        private string _dataType = string.Empty;
        private string _inputString = string.Empty;

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_TxtSave();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "txt存储";
                case LangaugeType.kEnglish:
                    return "txtSave";
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
            _txtSavePath = Convert.ToString(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _txtFileName = Convert.ToString(inputParams[1].ActualParam);
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _dataType = Convert.ToString(inputParams[2].ActualParam);

            _inputString = string.Empty;

            if (inputParams.Count < 4) {
                return TaskNodeStatus.kFailure;
            }
            else if (_dataType == "kInt") {
                _inputString = Convert.ToInt32(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kFloat") {
                _inputString = Convert.ToSingle(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kDouble") {
                _inputString = Convert.ToDouble(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kString") {
                _inputString = Convert.ToString(inputParams[3].ActualParam);
            }
            else if (_dataType == "kBool") {
                _inputString = Convert.ToBoolean(inputParams[3].ActualParam).ToString();
            }
            else {
                return TaskNodeStatus.kFailure;
            }


            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            string filePath = _txtSavePath + "\\" + _txtFileName + ".txt";

            using (FileStream fs = new FileStream(filePath, FileMode.Append, FileAccess.Write, FileShare.None)) {
                using (BufferedStream bufferedStream = new BufferedStream(fs)) {
                    using (StreamWriter writer = new StreamWriter(bufferedStream, Encoding.Default)) {
                        // 将数据数组转换为 txt 格式字符串
                        string line = string.Join(",", _inputString);

                        // 使用 Write 而不是 WriteLine，避免多余换行符
                        writer.Write(line);
                    }
                }
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }
    }
}
