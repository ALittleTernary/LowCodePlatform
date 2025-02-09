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

namespace LowCodePlatform.Plugin.Task_ShowImage
{
    /// <summary>
    /// TaskView_ShowImage.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_ShowImage : System.Windows.Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;
        private ExecuteClick _executeClick = null;
        private LinkClick _linkClick = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskView_ShowImage() {
            InitializeComponent();
            Button_Confirm.Click += Event_Button_Confirm;
            Button_Execute.Click += Event_Button_Execute;
        }

        /// <summary>
        /// 点击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Confirm(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "界面链接",
                IsBind = LinkEdit_ShowView.IsBind,
                UserParam = LinkEdit_ShowView.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片链接",
                IsBind = LinkEdit_Image.IsBind,
                UserParam = LinkEdit_Image.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "区域链接",
                IsBind = LinkEdit_Region.IsBind,
                UserParam = LinkEdit_Region.UserParam,
            });
            _confirmClick?.Invoke(inputParams);
        }

        /// <summary>
        /// 点击执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Execute(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "界面链接",
                IsBind = LinkEdit_ShowView.IsBind,
                UserParam = LinkEdit_ShowView.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片链接",
                IsBind = LinkEdit_Image.IsBind,
                UserParam = LinkEdit_Image.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "区域链接",
                IsBind = LinkEdit_Region.IsBind,
                UserParam = LinkEdit_Region.UserParam,
            });
            _executeClick?.Invoke(inputParams);
        }

        public void JsonToView(string str) {
            if (str == null || str == string.Empty) { 
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_ShowView.JsonToView(json["LinkEdit_ShowView"].ToString());
            LinkEdit_Image.JsonToView(json["LinkEdit_Image"].ToString());
            LinkEdit_Region.JsonToView(json["LinkEdit_Region"].ToString());
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_ShowView"] = LinkEdit_ShowView.ViewToJson();
            json["LinkEdit_Image"] = LinkEdit_Image.ViewToJson();
            json["LinkEdit_Region"] = LinkEdit_Region.ViewToJson();
            return json.ToString();
        }

        public void ResetView() {
            LinkEdit_ShowView.ResetView();
            LinkEdit_Image.ResetView();
            LinkEdit_Region.ResetView();
            ImageShow_Read.ResetView();
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
                return ;
            }
            _linkClick = linkClickCallback;
            LinkEdit_ShowView.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Image.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Region.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            Mat mat = (Mat)outputParams[0].ActualParam;
            ImageShow_Read.Image = mat;
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(Mat[])) {
                return;
            }
            Mat[] masks = (Mat[])outputParams[1].ActualParam;
            ImageShow_Read.Masks = masks;
        }



        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "显示图像";
                case LangaugeType.kEnglish:
                    return "ShowImage";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
