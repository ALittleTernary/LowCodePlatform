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

namespace LowCodePlatform.Plugin.Task_Tcp
{
    /// <summary>
    /// TaskView_TcpServer.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_TcpServer : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskView_TcpServer() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "Tcp服务端",
                    IsBind = LinkEdit_TcpServer.IsBind,
                    UserParam = LinkEdit_TcpServer.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "选择操作",
                    IsBind = false,
                    UserParam = (TabControl_Option.SelectedItem as TabItem).Header.ToString(),
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "发送数据",
                    IsBind = LinkEdit_SendData.IsBind,
                    UserParam = LinkEdit_SendData.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "接收超时",
                    IsBind = LinkEdit_ReceiveOvertime.IsBind,
                    UserParam = LinkEdit_ReceiveOvertime.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "Tcp服务端",
                    IsBind = LinkEdit_TcpServer.IsBind,
                    UserParam = LinkEdit_TcpServer.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "选择操作",
                    IsBind = false,
                    UserParam = (TabControl_Option.SelectedItem as TabItem).Header.ToString(),
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "发送数据",
                    IsBind = LinkEdit_SendData.IsBind,
                    UserParam = LinkEdit_SendData.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "接收超时",
                    IsBind = LinkEdit_ReceiveOvertime.IsBind,
                    UserParam = LinkEdit_ReceiveOvertime.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {

        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_TcpServer.JsonToView(json["LinkEdit_TcpServer"].ToString());
            TabControl_Option.SelectedIndex = ((int)json["TabControl_Option"]);
            LinkEdit_SendData.JsonToView(json["LinkEdit_SendData"].ToString());
            LinkEdit_ReceiveOvertime.JsonToView(json["LinkEdit_ReceiveOvertime"].ToString());
        }

        public void ResetView() {
            LinkEdit_TcpServer.ResetView();
            TabControl_Option.SelectedIndex = 0;
            LinkEdit_SendData.ResetView();
            LinkEdit_ReceiveOvertime.ResetView();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_TcpServer"] = LinkEdit_TcpServer.ViewToJson();
            json["TabControl_Option"] = TabControl_Option.SelectedIndex;
            json["LinkEdit_SendData"] = LinkEdit_SendData.ViewToJson();
            json["LinkEdit_ReceiveOvertime"] = LinkEdit_ReceiveOvertime.ViewToJson();
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
                return;
            }
            _executeClick = executeClickCallback;
        }

        public void SetLinkClickCallback(LinkClick linkClickCallback) {
            if (linkClickCallback == null) {
                return;
            }
            _linkClick = linkClickCallback;
            LinkEdit_TcpServer.SetLinkClickCallback(linkClickCallback);
            LinkEdit_SendData.SetLinkClickCallback(linkClickCallback);
            LinkEdit_ReceiveOvertime.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "Tcp服务端";
                case LangaugeType.kEnglish:
                    return "TcpServer";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
