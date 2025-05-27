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

namespace LowCodePlatform.Plugin.Task_ArraySplit
{
    /// <summary>
    /// TaskView_ArraySplit.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_ArraySplit : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_ArraySplit() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            ComboBox_ArrayType.SelectionChanged += (s, e) => { 
                string currentItem = (ComboBox_ArrayType.SelectedItem as ComboBoxItem).Content as string;
                if (currentItem == "kListString") {
                    LinkEdit_TargetArray.LinkContentType = LinkDataType.kListString;
                }
                else if (currentItem == "kRegion") {
                    LinkEdit_TargetArray.LinkContentType = LinkDataType.kRegion;
                }
                else {
                    return;
                }
                LinkEdit_TargetArray.ResetView();
            };
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();

                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数组类型",
                    IsBind = false,
                    UserParam = ComboBox_ArrayType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标数组",
                    IsBind = LinkEdit_TargetArray.IsBind,
                    UserParam = LinkEdit_TargetArray.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数组索引",
                    IsBind = LinkEdit_ArrayIndex.IsBind,
                    UserParam = LinkEdit_ArrayIndex.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数组类型",
                    IsBind = false,
                    UserParam = ComboBox_ArrayType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标数组",
                    IsBind = LinkEdit_TargetArray.IsBind,
                    UserParam = LinkEdit_TargetArray.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数组索引",
                    IsBind = LinkEdit_ArrayIndex.IsBind,
                    UserParam = LinkEdit_ArrayIndex.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            ComboBox_ArrayType.SelectedIndex = ((int)json["ComboBox_ArrayType"]);
            LinkEdit_ArrayIndex.JsonToView(json["LinkEdit_ArrayIndex"].ToString());
            LinkEdit_TargetArray.JsonToView(json["LinkEdit_TargetArray"].ToString());

        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["ComboBox_ArrayType"] = ComboBox_ArrayType.SelectedIndex;
            json["LinkEdit_ArrayIndex"] = LinkEdit_ArrayIndex.ViewToJson();
            json["LinkEdit_TargetArray"] = LinkEdit_TargetArray.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            ComboBox_ArrayType.SelectedIndex = 0;
            LinkEdit_ArrayIndex.ResetView();
            LinkEdit_TargetArray.ResetView();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "数组拆分";
                case LangaugeType.kEnglish:
                    return "ArraySplit";
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
            LinkEdit_ArrayIndex.SetLinkClickCallback(linkClickCallback);
            LinkEdit_TargetArray.SetLinkClickCallback(linkClickCallback);
        }


    }
}
