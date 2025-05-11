using LowCodePlatform.Plugin.Base;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_LocalVariable
{
    internal class TaskOperation_ModifyLocalVariable : LinkEditTaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        private ObtainResultByExpression _obtainResultByExpression = null;

        private UpdateLinkEditByExpression _updateLinkEditByExpression = null;

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_ModifyLocalVariable();
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            int count = Convert.ToInt32(inputParams[0].ActualParam);

            for (int i = 0; i < count; i++) {
                VariableData data = new VariableData();
                if (inputParams.Count < 2 + i * 3 || inputParams[1 + i * 3].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableType = inputParams[1 + i * 3].ActualParam as string;

                if (inputParams.Count < 3 + i * 3 || inputParams[2 + i * 3].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.VariableExpression = inputParams[2 + i * 3].ActualParam as string;

                if (inputParams.Count < 4 + i * 3) {
                    return TaskNodeStatus.kFailure;
                }

                //先计算出修改的数值
                data.VariableExpression = _obtainResultByExpression?.Invoke(data.VariableExpression);
                object nCalcResult = null;
                // 计算结果
                try {
                    // 使用 NCalc 解析并计算表达式
                    var nCalcExpression = new NCalc.Expression(data.VariableExpression);
                    nCalcResult = nCalcExpression.Evaluate();
                }
                catch (ArgumentException) {
                    Log.Error("解析表达式失败:" + data.VariableExpression);
                    nCalcResult = null;
                }
                if (nCalcResult == null) {
                    return TaskNodeStatus.kFailure;
                }
                else if (data.VariableType == "kInt") {
                    inputParams[3 + i * 3].ActualParam = Convert.ToInt32(nCalcResult);
                    _updateLinkEditByExpression?.Invoke(inputParams[3 + i * 3]);
                }
                else if (data.VariableType == "kFloat") {
                    inputParams[3 + i * 3].ActualParam = Convert.ToSingle(nCalcResult);
                    _updateLinkEditByExpression?.Invoke(inputParams[3 + i * 3]);
                }
                else if (data.VariableType == "kDouble") {
                    inputParams[3 + i * 3].ActualParam = Convert.ToDouble(nCalcResult);
                    _updateLinkEditByExpression?.Invoke(inputParams[3 + i * 3]);
                }
                else if (data.VariableType == "kString") {
                    inputParams[3 + i * 3].ActualParam = Convert.ToString(nCalcResult);
                    _updateLinkEditByExpression?.Invoke(inputParams[3 + i * 3]);
                }
                else if (data.VariableType == "kBool") {
                    inputParams[3 + i * 3].ActualParam = Convert.ToBoolean(nCalcResult);
                    _updateLinkEditByExpression?.Invoke(inputParams[3 + i * 3]);
                }
                else { 
                
                }
                



            }
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
                    return "修改局部变量";
                case LangaugeType.kEnglish:
                    return "ModifyLocalVariable";
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
                return;
            }
            _updateLinkEditByExpression = cb;
        }
    }
}
