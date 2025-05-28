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
        private string _saveType = string.Empty;

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
            else if (_dataType == "kInt" && inputParams[3].ActualParam.GetType() == typeof(int)) {
                _inputString = Convert.ToInt32(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kFloat" && inputParams[3].ActualParam.GetType() == typeof(float)) {
                _inputString = Convert.ToSingle(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kDouble" && inputParams[3].ActualParam.GetType() == typeof(double)) {
                _inputString = Convert.ToDouble(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kString" && inputParams[3].ActualParam.GetType() == typeof(string)) {
                _inputString = Convert.ToString(inputParams[3].ActualParam);
            }
            else if (_dataType == "kBool" && inputParams[3].ActualParam.GetType() == typeof(bool)) {
                _inputString = Convert.ToBoolean(inputParams[3].ActualParam).ToString();
            }
            else if (_dataType == "kListInt" && inputParams[3].ActualParam.GetType() == typeof(List<int>)) {
                List<int> datas = inputParams[3].ActualParam as List<int>;
                for (int i = 0; i < datas.Count; i++) {
                    if (i == 0) {
                        _inputString = datas[i].ToString();
                        continue;
                    }
                    _inputString = _inputString + "\n" + datas[i].ToString();
                }
            }
            else if (_dataType == "kListFloat" && inputParams[3].ActualParam.GetType() == typeof(List<float>)) {
                List<float> datas = inputParams[3].ActualParam as List<float>;
                for (int i = 0; i < datas.Count; i++) {
                    if (i == 0) {
                        _inputString = datas[i].ToString();
                        continue;
                    }
                    _inputString = _inputString + "\n" + datas[i].ToString();
                }
            }
            else if (_dataType == "kListDouble" && inputParams[3].ActualParam.GetType() == typeof(List<double>)) {
                List<double> datas = inputParams[3].ActualParam as List<double>;
                for (int i = 0; i < datas.Count; i++) {
                    if (i == 0) {
                        _inputString = datas[i].ToString();
                        continue;
                    }
                    _inputString = _inputString + "\n" + datas[i].ToString();
                }
            }
            else if (_dataType == "kListString" && inputParams[3].ActualParam.GetType() == typeof(List<string>)) {
                List<string> datas = inputParams[3].ActualParam as List<string>;
                for (int i = 0; i < datas.Count; i++) {
                    if (i == 0) {
                        _inputString = datas[i];
                        continue;
                    }
                    _inputString = _inputString + "\n" + datas[i];
                }
            }
            else if (_dataType == "kListBool" && inputParams[3].ActualParam.GetType() == typeof(List<bool>)) {
                List<bool> datas = inputParams[3].ActualParam as List<bool>;
                for (int i = 0; i < datas.Count; i++) {
                    if (i == 0) {
                        _inputString = datas[i].ToString();
                        continue;
                    }
                    _inputString = _inputString + "\n" + datas[i].ToString();
                }
            }
            else {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 5 || inputParams[4].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _saveType = Convert.ToString(inputParams[4].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            string filePath = _txtSavePath + "\\" + _txtFileName + ".txt";

            FileMode fileMode = FileMode.Append;
            if (_saveType == "追加到最后一行") {
                fileMode = FileMode.Append;
            }
            else if (_saveType == "覆盖原文件") {
                fileMode = FileMode.Create;
            }
            else {
                return TaskNodeStatus.kFailure;
            }

            using (FileStream fs = new FileStream(filePath, fileMode, FileAccess.Write, FileShare.None)) {
                using (BufferedStream bufferedStream = new BufferedStream(fs)) {
                    using (StreamWriter writer = new StreamWriter(bufferedStream, Encoding.Default)) {
                        // 将数据数组转换为 txt 格式字符串
                        string line = string.Join(",", _inputString);

                        // 使用 Write 而不是 WriteLine，避免多余换行符
                        writer.WriteLine(line);
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
