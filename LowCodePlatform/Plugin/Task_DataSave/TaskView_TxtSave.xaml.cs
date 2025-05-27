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

namespace LowCodePlatform.Plugin.Task_DataSave
{
    /// <summary>
    /// TaskView_TxtSave.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_TxtSave : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_TxtSave() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            ComboBox_DataType.SelectionChanged += (s, e) => {
                string currentItem = (ComboBox_DataType.SelectedItem as ComboBoxItem).Content as string;
                if (currentItem == "kInt") {
                    LinkEdit_TargetData.LinkContentType = LinkDataType.kInt;
                }
                else if (currentItem == "kFloat") {
                    LinkEdit_TargetData.LinkContentType = LinkDataType.kFloat;
                }
                else if (currentItem == "kDouble") {
                    LinkEdit_TargetData.LinkContentType = LinkDataType.kDouble;
                }
                else if (currentItem == "kString") {
                    LinkEdit_TargetData.LinkContentType = LinkDataType.kString;
                }
                else if (currentItem == "kBool") {
                    LinkEdit_TargetData.LinkContentType = LinkDataType.kBool;
                }
                else {
                    return;
                }
                LinkEdit_TargetData.ResetView();
            };
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "存储路径",
                    IsBind = false,
                    UserParam = TextBox_CsvSavePath.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "存储文件名",
                    IsBind = LinkEdit_CsvFileName.IsBind,
                    UserParam = LinkEdit_CsvFileName.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数据类型",
                    IsBind = false,
                    UserParam = ComboBox_DataType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标数据",
                    IsBind = LinkEdit_TargetData.IsBind,
                    UserParam = LinkEdit_TargetData.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "存储路径",
                    IsBind = false,
                    UserParam = TextBox_CsvSavePath.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "存储文件名",
                    IsBind = LinkEdit_CsvFileName.IsBind,
                    UserParam = LinkEdit_CsvFileName.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "数据类型",
                    IsBind = false,
                    UserParam = ComboBox_DataType.Text,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标数据",
                    IsBind = LinkEdit_TargetData.IsBind,
                    UserParam = LinkEdit_TargetData.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
            Button_CsvSaveOpen.Click += (s, e) => {
                // 创建 FolderBrowserDialog 实例
                System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
                // 可选：设置对话框的初始目录
                folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                // 显示对话框并等待用户选择文件夹
                if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
                    return;
                }

                // 获取选中的文件夹路径
                string folderPath = folderDialog.SelectedPath;
                TextBox_CsvSavePath.Text = folderPath;
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
            TextBox_CsvSavePath.Text = json["TextBox_CsvSavePath"].ToString();
            ComboBox_DataType.SelectedIndex = ((int)json["ComboBox_DataType"]);
            LinkEdit_TargetData.JsonToView(json["LinkEdit_TargetData"].ToString());
            LinkEdit_CsvFileName.JsonToView(json["LinkEdit_CsvFileName"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_CsvSavePath"] = TextBox_CsvSavePath.Text;
            json["ComboBox_DataType"] = ComboBox_DataType.SelectedIndex;
            json["LinkEdit_TargetData"] = LinkEdit_TargetData.ViewToJson();
            json["LinkEdit_CsvFileName"] = LinkEdit_CsvFileName.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            TextBox_CsvSavePath.Clear();
            ComboBox_DataType.SelectedIndex = 0;
            LinkEdit_TargetData.ResetView();
            LinkEdit_CsvFileName.ResetView();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "txt存储";
                case LangaugeType.kEnglish:
                    return "txtSave";
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
            LinkEdit_TargetData.SetLinkClickCallback(linkClickCallback);
            LinkEdit_CsvFileName.SetLinkClickCallback(linkClickCallback);
        }
    }
}
