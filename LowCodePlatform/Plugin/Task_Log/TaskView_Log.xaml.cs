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

namespace LowCodePlatform.Plugin.Task_Log
{
    /// <summary>
    /// TaskView_Log.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Log : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_Log() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "日志等级",
                    IsBind = false,
                    UserParam = ComboBox_LogLevel.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "日志内容",
                    IsBind = LinkEdit_LogContent.IsBind,
                    UserParam = LinkEdit_LogContent.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "日志等级",
                    IsBind = false,
                    UserParam = ComboBox_LogLevel.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "日志内容",
                    IsBind = LinkEdit_LogContent.IsBind,
                    UserParam = LinkEdit_LogContent.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["ComboBox_LogLevel"] = ComboBox_LogLevel.SelectedIndex;
            json["LinkEdit_LogContent"] = LinkEdit_LogContent.ViewToJson();
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            ComboBox_LogLevel.SelectedIndex = ((int)json["ComboBox_LogLevel"]);
            LinkEdit_LogContent.JsonToView(json["LinkEdit_LogContent"].ToString());
        }

        public void ResetView() {
            ComboBox_LogLevel.SelectedIndex = 0;
            LinkEdit_LogContent.ResetView();
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
            LinkEdit_LogContent.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {

        }



        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "打印日志";
                case LangaugeType.kEnglish:
                    return "PrintLog";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
