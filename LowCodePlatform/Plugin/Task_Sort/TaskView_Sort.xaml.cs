using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LowCodePlatform.Plugin.Task_Sort
{
    /// <summary>
    /// TaskView_Sort.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Sort : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_Sort() {
            InitializeComponent();
            InitEvent();
        }


        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数据类型",
                    IsBind = false,
                    UserParam = ComboBox_DataType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "排序方式",
                    IsBind = false,
                    UserParam = ComboBox_SortOrder.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "输入数组",
                    IsBind = LinkEdit_InputArray.IsBind,
                    UserParam = LinkEdit_InputArray.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数据类型",
                    IsBind = false,
                    UserParam = ComboBox_DataType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "排序方式",
                    IsBind = false,
                    UserParam = ComboBox_SortOrder.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "输入数组",
                    IsBind = LinkEdit_InputArray.IsBind,
                    UserParam = LinkEdit_InputArray.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
            ComboBox_DataType.SelectionChanged += (s, e) => {
                string currentItem = (ComboBox_DataType.SelectedItem as ComboBoxItem).Content as string;
                if (currentItem == "kListInt") {
                    LinkEdit_InputArray.LinkContentType = LinkDataType.kListInt;
                }
                else if (currentItem == "kListFloat") {
                    LinkEdit_InputArray.LinkContentType = LinkDataType.kListFloat;
                }
                else if (currentItem == "kListDouble") {
                    LinkEdit_InputArray.LinkContentType = LinkDataType.kListDouble;
                }
                else if (currentItem == "kListString") {
                    LinkEdit_InputArray.LinkContentType = LinkDataType.kListString;
                }
                else if (currentItem == "kListTimeSpan") {
                    LinkEdit_InputArray.LinkContentType = LinkDataType.kListString;
                }
                else {
                    return;
                }
                LinkEdit_InputArray.ResetView();
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }   
            string dataType = ComboBox_DataType.Text;
            if (dataType == "kListInt" && outputParams.Count >= 1 && outputParams[0].ActualParam.GetType() == typeof(List<int>)) {
                List<int> datas = outputParams[0].ActualParam as List<int>;
                string str = string.Empty;
                foreach (var item in datas) {
                    str = str + "\n"+ item;
                }
                LinkEdit_Result.LinkContentText = str;
            }
            else if (dataType == "kListFloat" && outputParams.Count >= 1 && outputParams[0].ActualParam.GetType() == typeof(List<float>)) {
                List<float> datas = outputParams[0].ActualParam as List<float>;
                string str = string.Empty;
                foreach (var item in datas) {
                    str = str + "\n" + item;
                }
                LinkEdit_Result.LinkContentText = str;
            }
            else if (dataType == "kListDouble" && outputParams.Count >= 1 && outputParams[0].ActualParam.GetType() == typeof(List<double>)) {
                List<double> datas = outputParams[0].ActualParam as List<double>;
                string str = string.Empty;
                foreach (var item in datas) {
                    str = str + "\n" + item;
                }
                LinkEdit_Result.LinkContentText = str;
            }
            else if (dataType == "kListString" && outputParams.Count >= 1 && outputParams[0].ActualParam.GetType() == typeof(List<string>)) {
                List<string> datas = outputParams[0].ActualParam as List<string>;
                string str = string.Empty;
                foreach (var item in datas) {
                    str = str + "\n" + item;
                }
                LinkEdit_Result.LinkContentText = str;
            }
            else if (dataType == "kListTimeSpan" && outputParams.Count >= 1 && outputParams[0].ActualParam.GetType() == typeof(List<string>)) {
                List<string> datas = outputParams[0].ActualParam as List<string>;
                string str = string.Empty;
                foreach (var item in datas) {
                    str = str + "\n" + item;
                }
                LinkEdit_Result.LinkContentText = str;
            }
            else {
                return;
            }
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            ComboBox_DataType.SelectedIndex = ((int)json["ComboBox_DataType"]);
            ComboBox_SortOrder.SelectedIndex = ((int)json["ComboBox_SortOrder"]);
            LinkEdit_InputArray.JsonToView(json["LinkEdit_InputArray"].ToString());
            LinkEdit_Result.JsonToView(json["LinkEdit_Result"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["ComboBox_DataType"] = ComboBox_DataType.SelectedIndex;
            json["ComboBox_SortOrder"] = ComboBox_SortOrder.SelectedIndex;
            json["LinkEdit_InputArray"] = LinkEdit_InputArray.ViewToJson();
            json["LinkEdit_Result"] = LinkEdit_Result.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            ComboBox_DataType.SelectedIndex = 0;
            ComboBox_SortOrder.SelectedIndex = 0;
            LinkEdit_InputArray.ResetView();
            LinkEdit_Result.ResetView();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "排序";
                case LangaugeType.kEnglish:
                    return "Sort";
                default:
                    break;
            }
            return string.Empty;
        }

        public void SwitchLanguage(LangaugeType type) {
            return;
        }

        public void SetConfirmClickCallback(ConfirmClick confirmClickCallback) {
            if (confirmClickCallback == null) {
                return;
            }
            _confirmClick = confirmClickCallback;
        }

        public void SetExecuteClickCallback(ExecuteClick executeClickCallback) {
            if (executeClickCallback == null) {
                return;
            }
            _executeClick = executeClickCallback;
        }

        public void SetLinkClickCallback(LinkClick linkClickCallback) {
            if (linkClickCallback == null) {
                return;
            }
            _linkClick = linkClickCallback;
            LinkEdit_InputArray.SetLinkClickCallback(linkClickCallback);
        }
    }
}
