using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using Serilog;
using Serilog.Core;
using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LowCodePlatform.Plugin.Task_LocalVariable
{
    internal class TaskOperation_CreateLocalVariable : LinkEditTaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        private ObtainResultByExpression _obtainResultByExpression = null;

        private UpdateLinkEditByExpression _updateLinkEditByExpression = null;

        private List<VariableData> _variableDatas = new List<VariableData>();

        private List<TaskOperationOutputParams> _outputParams = new List<TaskOperationOutputParams>();

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_CreateLocalVariable();
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            int count = Convert.ToInt32(inputParams[0].ActualParam);
            _variableDatas.Clear();
            for (int i = 0; i < count; i++) {
                VariableData data = new VariableData();
                if (inputParams.Count < 2 + i * 4 || inputParams[1 + i * 4].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableType = inputParams[1 + i * 4].ActualParam as string;
                if (inputParams.Count < 3 + i * 4 || inputParams[2 + i * 4].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableName = inputParams[2 + i * 4].ActualParam as string;
                if (inputParams.Count < 4 + i * 4 || inputParams[3 + i * 4].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableExpression = inputParams[3 + i * 4].ActualParam as string;
                if (inputParams.Count < 5 + i * 4 || inputParams[4 + i * 4].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableTip = inputParams[4 + i * 4].ActualParam as string;
                _variableDatas.Add(data);
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _outputParams.Clear();
            _outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "局部变量数量",
                ActualParam = _variableDatas.Count,
                LinkVisual = false,
            });

            foreach (var data in _variableDatas) {
                data.VariableExpression = _obtainResultByExpression?.Invoke(data.VariableExpression);
                if (!Enum.TryParse(data.VariableType, out LinkDataType menuType)) {
                    return TaskNodeStatus.kFailure;
                }

                object nCalcResult = null;
                // 计算结果
                try {
                    // 使用 NCalc 解析并计算表达式
                    var nCalcExpression = new NCalc.Expression(data.VariableExpression);
                    nCalcResult = nCalcExpression.Evaluate();
                }
                catch (ArgumentException ex) {
                    Log.Error("解析表达式失败:" + data.VariableExpression);
                    nCalcResult = null;
                }

                TaskOperationOutputParams output = new TaskOperationOutputParams();
                if (nCalcResult == null) {
                    _outputParams.Add(new TaskOperationOutputParams() {
                        ParamName = data.VariableName,
                        ActualParam = null,
                        Description = data.VariableTip,
                    });
                }
                else if (menuType == LinkDataType.kInt && (nCalcResult.GetType() == typeof(float) || nCalcResult.GetType() == typeof(int) || nCalcResult.GetType() == typeof(double))) {
                    int result = Convert.ToInt32(nCalcResult);
                    _outputParams.Add(new TaskOperationOutputParams() { 
                        ParamName = data.VariableName,
                        ActualParam = result,
                        Description = data.VariableTip,
                    });
                }
                else if (menuType == LinkDataType.kFloat && (nCalcResult.GetType() == typeof(float) || nCalcResult.GetType() == typeof(int) || nCalcResult.GetType() == typeof(double))) {
                    float result = Convert.ToSingle(nCalcResult);
                    _outputParams.Add(new TaskOperationOutputParams() {
                        ParamName = data.VariableName,
                        ActualParam = result,
                        Description = data.VariableTip,
                    });
                }
                else if (menuType == LinkDataType.kDouble && (nCalcResult.GetType() == typeof(float) || nCalcResult.GetType() == typeof(int) || nCalcResult.GetType() == typeof(double))) {
                    double result = Convert.ToDouble(nCalcResult);
                    _outputParams.Add(new TaskOperationOutputParams() {
                        ParamName = data.VariableName,
                        ActualParam = result,
                        Description = data.VariableTip,
                    });
                }
                else if (menuType == LinkDataType.kString && (nCalcResult.GetType() == typeof(float) || nCalcResult.GetType() == typeof(int) || nCalcResult.GetType() == typeof(double) || nCalcResult.GetType() == typeof(string))) {
                    string result = Convert.ToString(nCalcResult);
                    _outputParams.Add(new TaskOperationOutputParams() {
                        ParamName = data.VariableName,
                        ActualParam = result,
                        Description = data.VariableTip,
                    });
                }
                else if (menuType == LinkDataType.kBool && nCalcResult.GetType() == typeof(bool)) {
                    bool result = Convert.ToBoolean(nCalcResult);
                    _outputParams.Add(new TaskOperationOutputParams() {
                        ParamName = data.VariableName,
                        ActualParam = result,
                        Description = data.VariableTip,
                    });
                }
                else {
                    return TaskNodeStatus.kFailure;
                }
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = _outputParams;
            return TaskNodeStatus.kSuccess;
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "创建局部变量";
                case LangaugeType.kEnglish:
                    return "CreateLocalVariable";
                default:
                    break;
            }
            return string.Empty;
        }

        public void SetObtainResultByExpressionCallback(ObtainResultByExpression cb) {
            if (cb == null) {
                return;
            }
            _obtainResultByExpression = cb;
        }

        public void SetUpdateLinkEditByExpressCallback(UpdateLinkEditByExpression cb) {
            if (cb == null) { 
                return ;
            }
            _updateLinkEditByExpression = cb;
        }
    }
}
