using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Sub_Table;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Markup;

namespace LowCodePlatform.Plugin.Task_Table
{

    internal class TaskOperation_Table : TaskOperationPluginBase
    {
        public bool EngineIsRunning { get; set; }

        private SubView_Table _table = null;

        private List<TableData> _datas = new List<TableData>();

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Table();
        }


        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "表格";
                case LangaugeType.kEnglish:
                    return "Table";
                default:
                    break;
            }
            return string.Empty;
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(SubView_Table)) {
                return TaskNodeStatus.kFailure;
            }
            _table = inputParams[0].ActualParam as SubView_Table;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            int count = Convert.ToInt32(inputParams[1].ActualParam);

            _datas.Clear();
            for (int i = 2; i < 2 + count * 3; i = i + 3) { 
                TableData data = new TableData();
                if (inputParams.Count < i + 1 || inputParams[i].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Row = inputParams[i].ActualParam as string;

                if (inputParams.Count < i + 2 || inputParams[i + 1].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Column = inputParams[i + 1].ActualParam as string;

                if (inputParams.Count < i + 3 || inputParams[i + 2].ActualParam.GetType() != typeof(string)) {
                    return TaskNodeStatus.kFailure;
                }
                data.Text = inputParams[i + 2].ActualParam as string;
                _datas.Add(data);
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            foreach (var data in _datas) {
                if (!int.TryParse(data.Row, out int row)) { 
                    return TaskNodeStatus.kFailure;
                }
                if (!int.TryParse(data.Column, out int column)) {
                    return TaskNodeStatus.kFailure;
                }
                _table.SetCellContent(row, column, data.Text);
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            return TaskNodeStatus.kSuccess;
        }
    }
}
