using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_RegexMatch
{
    internal class TaskOperation_RegexMatch : TaskOperationPluginBase
    {
        private string _targetStr = string.Empty;
        private string _regexStr = string.Empty;
        private int _regexCount = 0;


        private bool _regexStatus = false;
        private List<string> _regexResult = new List<string>();

        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_RegexMatch();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "正则匹配";
                case LangaugeType.kEnglish:
                    return "RegexMatch";
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
            _targetStr = Convert.ToString(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _regexStr = Convert.ToString(inputParams[1].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _regexResult.Clear();

            Regex regex = new Regex(@_regexStr);
            MatchCollection matches = regex.Matches(_targetStr);  // 获取所有匹配结果

            _regexCount = matches.Count;
            if (matches.Count > 0) {
                _regexStatus = true;
                foreach (Match match in matches) {
                    // 输出完整的匹配结果
                    _regexResult.Add(match.Value);
                }
            }
            else {
                _regexStatus = false;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "匹配状态",
                ActualParam = _regexStatus,
                Description = "字符串正则式匹配状态"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "匹配结果",
                ActualParam = _regexResult,
                Description = "字符串正则式匹配结果"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "匹配个数",
                ActualParam = _regexCount,
                Description = "字符串正则式匹配个数"
            });

            return TaskNodeStatus.kSuccess;
        }
    }
}
