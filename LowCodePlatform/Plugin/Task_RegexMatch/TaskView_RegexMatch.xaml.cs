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

namespace LowCodePlatform.Plugin.Task_RegexMatch
{
    /// <summary>
    /// TaskView_RegexMatch.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_RegexMatch : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_RegexMatch() {
            InitializeComponent();
            InitEvent();
        }


        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标字符串",
                    IsBind = LinkEdit_TargetStr.IsBind,
                    UserParam = LinkEdit_TargetStr.UserParam,
                });

                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "正则化规则",
                    IsBind = LinkEdit_RegexRule.IsBind,
                    UserParam = LinkEdit_RegexRule.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标字符串",
                    IsBind = LinkEdit_TargetStr.IsBind,
                    UserParam = LinkEdit_TargetStr.UserParam,
                });

                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "正则化规则",
                    IsBind = LinkEdit_RegexRule.IsBind,
                    UserParam = LinkEdit_RegexRule.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(bool)) {
                return;
            }
            bool status = Convert.ToBoolean(outputParams[0].ActualParam);
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(List<string>)) {
                return;
            }
            List<string> resultList = outputParams[1].ActualParam as List<string>;
            string resultStr = string.Empty;
            foreach (string str in resultList) {
                resultStr = resultStr + "\n" + str;
            }
            LinkEdit_Result.LinkContentText = status + resultStr;
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_TargetStr.JsonToView(json["LinkEdit_TargetStr"].ToString());
            LinkEdit_RegexRule.JsonToView(json["LinkEdit_RegexRule"].ToString());
            LinkEdit_RegexRule.JsonToView(json["LinkEdit_RegexRule"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_TargetStr"] = LinkEdit_TargetStr.ViewToJson();
            json["LinkEdit_RegexRule"] = LinkEdit_RegexRule.ViewToJson();
            json["LinkEdit_Result"] = LinkEdit_Result.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            LinkEdit_TargetStr.ResetView();
            LinkEdit_RegexRule.ResetView();
            LinkEdit_Result.ResetView();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "正则匹配";
                case LangaugeType.kEnglish:
                    return "RegexMatch";
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
            LinkEdit_TargetStr.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RegexRule.SetLinkClickCallback(linkClickCallback);
        }
    }
}
