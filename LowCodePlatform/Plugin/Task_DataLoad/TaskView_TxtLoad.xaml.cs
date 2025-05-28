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

namespace LowCodePlatform.Plugin.Task_DataLoad
{
    /// <summary>
    /// TaskView_TxtLoad.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_TxtLoad : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_TxtLoad() {
            InitializeComponent();
            InitEvent();
        }


        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "txt文件路径",
                    IsBind = false,
                    UserParam = TextBox_TxtLoadPath.Text,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "txt文件路径",
                    IsBind = false,
                    UserParam = TextBox_TxtLoadPath.Text,
                });
                _executeClick?.Invoke(inputParams);
            };
            Button_TxtLoadOpen.Click += (s, e) => {
                // 创建 OpenFileDialog 实例
                Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
                openFileDialog.Title = "找到文本文件的路径";
                // 设置文件类型过滤器，只显示图片文件
                openFileDialog.Filter = "Txt Files|*.txt";

                // 显示文件选择对话框并判断是否点击了"打开"按钮
                if (openFileDialog.ShowDialog() == false) {
                    return;
                }
                // 获取选中的文件路径
                TextBox_TxtLoadPath.Text = openFileDialog.FileName;
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
            TextBox_TxtLoadPath.Text = json["TextBox_TxtLoadPath"].ToString();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_TxtLoadPath"] = TextBox_TxtLoadPath.Text;
            return json.ToString();
        }

        public void ResetView() {
            TextBox_TxtLoadPath.Clear();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "txt加载";
                case LangaugeType.kEnglish:
                    return "txtLoad";
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
        }
    }
}
