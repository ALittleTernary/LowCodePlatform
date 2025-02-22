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

namespace LowCodePlatform.Plugin.Task_LineChart
{
    /// <summary>
    /// TaskView_LineChart.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_LineChart : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_LineChart() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "折线图界面",
                    IsBind = LinkEdit_LineChart.IsBind,
                    UserParam = LinkEdit_LineChart.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "X轴数组",
                    IsBind = LinkEdit_XArray.IsBind,
                    UserParam = LinkEdit_XArray.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "Y轴数组",
                    IsBind = LinkEdit_YArray.IsBind,
                    UserParam = LinkEdit_YArray.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "折线图界面",
                    IsBind = LinkEdit_LineChart.IsBind,
                    UserParam = LinkEdit_LineChart.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "X轴数组",
                    IsBind = LinkEdit_XArray.IsBind,
                    UserParam = LinkEdit_XArray.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "Y轴数组",
                    IsBind = LinkEdit_YArray.IsBind,
                    UserParam = LinkEdit_YArray.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {

        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_LineChart"] = LinkEdit_LineChart.ViewToJson();
            json["LinkEdit_XArray"] = LinkEdit_XArray.ViewToJson();
            json["LinkEdit_YArray"] = LinkEdit_YArray.ViewToJson();
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_LineChart.JsonToView(json["LinkEdit_LineChart"].ToString());
            LinkEdit_XArray.JsonToView(json["LinkEdit_XArray"].ToString());
            LinkEdit_YArray.JsonToView(json["LinkEdit_YArray"].ToString());
        }

        public void ResetView() {
            LinkEdit_LineChart.ResetView();
            LinkEdit_XArray.ResetView();
            LinkEdit_YArray.ResetView();
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
            LinkEdit_LineChart.SetLinkClickCallback(linkClickCallback);
            LinkEdit_XArray.SetLinkClickCallback(linkClickCallback);
            LinkEdit_YArray.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "折线图";
                case LangaugeType.kEnglish:
                    return "LineChart";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
