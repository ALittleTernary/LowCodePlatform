using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace LowCodePlatform.Plugin.Task_Ping
{
    /// <summary>
    /// TaskView_Ping.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Ping : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_Ping() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "IP地址",
                    IsBind = LinkEdit_IP.IsBind,
                    UserParam = LinkEdit_IP.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "超时",
                    IsBind = LinkEdit_TimeOut.IsBind,
                    UserParam = LinkEdit_TimeOut.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "IP地址",
                    IsBind = LinkEdit_IP.IsBind,
                    UserParam = LinkEdit_IP.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "超时",
                    IsBind = LinkEdit_TimeOut.IsBind,
                    UserParam = LinkEdit_TimeOut.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(List<string>)) {
                return;
            }
            List<string> messages = outputParams[1].ActualParam as List<string>;
            if (messages == null) { 
                return ;
            }
            ListView_Result.ItemsSource = new ObservableCollection<string>(messages);
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_IP.JsonToView(json["LinkEdit_IP"].ToString());
            LinkEdit_TimeOut.JsonToView(json["LinkEdit_TimeOut"].ToString());
        }

        public void ResetView() {
            LinkEdit_IP.ResetView();
            LinkEdit_TimeOut.ResetView();
            ListView_Result.ItemsSource = new ObservableCollection<string>();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_IP"] = LinkEdit_IP.ViewToJson();
            json["LinkEdit_TimeOut"] = LinkEdit_TimeOut.ViewToJson();
            return json.ToString();
        }

        public void SetConfirmClickCallback(ConfirmClick confirmClickCallback) {
            if (confirmClickCallback == null) { 
                return;
            }
            _confirmClick = confirmClickCallback;
        }

        public void SetExecuteClickCallback(ExecuteClick executeClickCallback) {
            if (executeClickCallback == null) { 
                return ;
            }
            _executeClick = executeClickCallback;
        }

        public void SetLinkClickCallback(LinkClick linkClickCallback) {
            if (linkClickCallback == null) { 
                return;
            }
            _linkClick = linkClickCallback;
            LinkEdit_IP.SetLinkClickCallback(linkClickCallback);
            LinkEdit_TimeOut.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "Ping";
                case LangaugeType.kEnglish:
                    return "Ping";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
