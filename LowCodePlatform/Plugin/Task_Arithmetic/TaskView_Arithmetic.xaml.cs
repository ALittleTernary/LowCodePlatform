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

namespace LowCodePlatform.Plugin.Task_Arithmetic
{
    /// <summary>
    /// TaskView_Arithmetic.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Arithmetic : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;
        
        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_Arithmetic() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Confirm.Click += Event_Button_Confirm_Click;
            Button_Execute.Click += Event_Button_Execute_Click;
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 3 || outputParams[2].ActualParam.GetType() != typeof(double)) { 
                return;
            }
            LinkEdit_Result.LinkContentText = Convert.ToDouble(outputParams[2].ActualParam).ToString();
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            ComboBox_ArithmeticType.SelectedIndex = ((int)json["ComboBox_ArithmeticType"]);
            LinkEdit_Param1.JsonToView(json["LinkEdit_Param1"].ToString());
            LinkEdit_Param2.JsonToView(json["LinkEdit_Param2"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["ComboBox_ArithmeticType"] = ComboBox_ArithmeticType.SelectedIndex;
            json["LinkEdit_Param1"] = LinkEdit_Param1.ViewToJson();
            json["LinkEdit_Param2"] = LinkEdit_Param2.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            ComboBox_ArithmeticType.SelectedIndex = 0;
            LinkEdit_Param1.ResetView();
            LinkEdit_Param2.ResetView();
            LinkEdit_Result.LinkContentText = "";
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "四则运算";
                case LangaugeType.kEnglish:
                    return "Arithmetic";
                default:
                    break;
            }
            return string.Empty;
        }

        public void SwitchLanguage(LangaugeType type) {
            return;
        }

        /// <summary>
        /// 点击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Confirm_Click(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();

            inputParams.Add(new TaskViewInputParams() {
                ParamName = "计算类型",
                IsBind = false,
                UserParam = ComboBox_ArithmeticType.Text,
            });

            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入参数一",
                IsBind = LinkEdit_Param1.IsBind,
                UserParam = LinkEdit_Param1.UserParam,
            });

            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入参数二",
                IsBind = LinkEdit_Param2.IsBind,
                UserParam = LinkEdit_Param2.UserParam,
            });
            _confirmClick?.Invoke(inputParams);
        }

        /// <summary>
        /// 点击执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Execute_Click(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "计算类型",
                IsBind = false,
                UserParam = ComboBox_ArithmeticType.Text,
            });

            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入参数一",
                IsBind = LinkEdit_Param1.IsBind,
                UserParam = LinkEdit_Param1.UserParam,
            });

            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入参数二",
                IsBind = LinkEdit_Param2.IsBind,
                UserParam = LinkEdit_Param2.UserParam,
            });
            _executeClick?.Invoke(inputParams);
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
            LinkEdit_Param1.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Param2.SetLinkClickCallback(linkClickCallback);
        }
    }
}
