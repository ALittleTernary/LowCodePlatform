using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace LowCodePlatform.Plugin.Task_Delay
{

    /// <summary>
    /// TaskView_Delay.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Delay : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_Delay() {
            InitializeComponent();
            InitView();
            InitEvent();
        }
        private void InitView() {

        }

        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "延时类型",
                    IsBind = false,
                    UserParam = ComboBox_DelayType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "延时时间",
                    IsBind = LinkEdit_DelayTime.IsBind,
                    UserParam = LinkEdit_DelayTime.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "延时类型",
                    IsBind = false,
                    UserParam = ComboBox_DelayType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "延时时间",
                    IsBind = LinkEdit_DelayTime.IsBind,
                    UserParam = LinkEdit_DelayTime.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void JsonToView(string str) {
            if (str == null || str == "") { 
                return;
            }
            JObject json = JObject.Parse(str);
            ComboBox_DelayType.SelectedIndex = ((int)json["ComboBox_DelayType"]);
            LinkEdit_DelayTime.JsonToView(json["LinkEdit_DelayTime"].ToString());
        }

        public void ResetView() {
            ComboBox_DelayType.SelectedIndex = 3;
            LinkEdit_DelayTime.ResetView();
        }

        public void SetConfirmClickCallback(ConfirmClick confirmClickCallback) {
            if (confirmClickCallback == null) { 
                return ;
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
            LinkEdit_DelayTime.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {
            return;
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            return;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["ComboBox_DelayType"] = ComboBox_DelayType.SelectedIndex;
            json["LinkEdit_DelayTime"] = LinkEdit_DelayTime.ViewToJson();
            return json.ToString();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "延时";
                case LangaugeType.kEnglish:
                    return "Delay";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
