using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
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

namespace LowCodePlatform.Plugin.Task_CharRecognize
{
    /// <summary>
    /// TaskView_CharRecognize.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_CharRecognize : System.Windows.Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_CharRecognize() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标图像",
                    IsBind = LinkEdit_TargetImage.IsBind,
                    UserParam = LinkEdit_TargetImage.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域类型",
                    IsBind = false,
                    UserParam = (TabControl_RegionType.SelectedItem as TabItem).Header as string,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域左上角X",
                    IsBind = LinkEdit_RegionLeftTopX.IsBind,
                    UserParam = LinkEdit_RegionLeftTopX.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域左上角Y",
                    IsBind = LinkEdit_RegionLeftTopY.IsBind,
                    UserParam = LinkEdit_RegionLeftTopY.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域横宽",
                    IsBind = LinkEdit_RegionLength.IsBind,
                    UserParam = LinkEdit_RegionLength.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域竖高",
                    IsBind = LinkEdit_RegionWidth.IsBind,
                    UserParam = LinkEdit_RegionWidth.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "链接区域",
                    IsBind = LinkEdit_InputRegion.IsBind,
                    UserParam = LinkEdit_InputRegion.UserParam,
                });
                _confirmClick?.Invoke(inputParams);
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "目标图像",
                    IsBind = LinkEdit_TargetImage.IsBind,
                    UserParam = LinkEdit_TargetImage.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域类型",
                    IsBind = false,
                    UserParam = (TabControl_RegionType.SelectedItem as TabItem).Header as string,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域左上角X",
                    IsBind = LinkEdit_RegionLeftTopX.IsBind,
                    UserParam = LinkEdit_RegionLeftTopX.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域左上角Y",
                    IsBind = LinkEdit_RegionLeftTopY.IsBind,
                    UserParam = LinkEdit_RegionLeftTopY.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域横宽",
                    IsBind = LinkEdit_RegionLength.IsBind,
                    UserParam = LinkEdit_RegionLength.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "区域竖高",
                    IsBind = LinkEdit_RegionWidth.IsBind,
                    UserParam = LinkEdit_RegionWidth.UserParam,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "链接区域",
                    IsBind = LinkEdit_InputRegion.IsBind,
                    UserParam = LinkEdit_InputRegion.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            Mat mat = (Mat)outputParams[0].ActualParam;
            ImageShow_Target.Image = mat;
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(string)) {
                return;
            }
            TextBlock_Result.Text = Convert.ToString(outputParams[1].ActualParam);
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            TabControl_RegionType.SelectedIndex = ((int)json["TabControl_RegionType"]);
            LinkEdit_TargetImage.JsonToView(json["LinkEdit_TargetImage"].ToString());
            LinkEdit_RegionLeftTopX.JsonToView(json["LinkEdit_RegionLeftTopX"].ToString());
            LinkEdit_RegionLeftTopY.JsonToView(json["LinkEdit_RegionLeftTopY"].ToString());
            LinkEdit_RegionLength.JsonToView(json["LinkEdit_RegionLength"].ToString());
            LinkEdit_RegionWidth.JsonToView(json["LinkEdit_RegionWidth"].ToString());
            LinkEdit_InputRegion.JsonToView(json["LinkEdit_InputRegion"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TabControl_RegionType"] = TabControl_RegionType.SelectedIndex;
            json["LinkEdit_TargetImage"] = LinkEdit_TargetImage.ViewToJson();
            json["LinkEdit_RegionLeftTopX"] = LinkEdit_RegionLeftTopX.ViewToJson();
            json["LinkEdit_RegionLeftTopY"] = LinkEdit_RegionLeftTopY.ViewToJson();
            json["LinkEdit_RegionLength"] = LinkEdit_RegionLength.ViewToJson();
            json["LinkEdit_RegionWidth"] = LinkEdit_RegionWidth.ViewToJson();
            json["LinkEdit_InputRegion"] = LinkEdit_InputRegion.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            TabControl_RegionType.SelectedIndex = 0;
            LinkEdit_TargetImage.ResetView();
            LinkEdit_RegionLeftTopX.ResetView();
            LinkEdit_RegionLeftTopY.ResetView();
            LinkEdit_RegionLength.ResetView();
            LinkEdit_RegionWidth.ResetView();
            LinkEdit_InputRegion.ResetView();
            TextBlock_Result.Clear();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "字符识别";
                case LangaugeType.kEnglish:
                    return "CharRecognize";
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
            LinkEdit_TargetImage.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RegionLeftTopX.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RegionLeftTopY.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RegionLength.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RegionWidth.SetLinkClickCallback(linkClickCallback);
            LinkEdit_InputRegion.SetLinkClickCallback(linkClickCallback);
        }
    }
}
