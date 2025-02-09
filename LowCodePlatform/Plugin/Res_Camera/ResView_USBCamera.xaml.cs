using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_ReadImage;
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

namespace LowCodePlatform.Plugin.Res_Camera
{


    /// <summary>
    /// ResView_Camera.xaml 的交互逻辑
    /// </summary>
    public partial class ResView_USBCamera : UserControl, ResViewPluginBase {
        private TurnOnResClick _turnOnResClick = null;
        private TurnOffResClick _turnOffResClick = null;
        private ResTemporaryEvent _resTemporaryEvent = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ResView_USBCamera() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_ShowImage.Click += (s, e) => {
                JObject json = new JObject();
                json["Action"] = "开始采图";
                _resTemporaryEvent?.Invoke(json.ToString());
            };
            Button_CloseImage.Click += (s, e) => {
                JObject json = new JObject();
                json["Action"] = "关闭采图";
                _resTemporaryEvent?.Invoke(json.ToString());
            };
            Button_CameraExposure.Click += Event_Button_TurnOnRes;
            Button_CameraGain.Click += Event_Button_TurnOnRes;
            Button_TurnOnRes.Click += Event_Button_TurnOnRes;
            Button_TurnOffRes.Click += Event_Button_TurnOffRes;
            ComboBox_CameraExposure.SelectionChanged += (s, e) => { 
                int index = ComboBox_CameraExposure.SelectedIndex;
                if (index == 0) {
                    TextBox_CameraExposure.IsEnabled = false;
                }
                else {
                    TextBox_CameraExposure.IsEnabled = true;
                }
            };
        }

        private void Event_Button_TurnOffRes(object sender, RoutedEventArgs e) {
            if (!int.TryParse(TextBox_CameraIndex.Text, out int index)) {
                return;
            }
            if (!double.TryParse(TextBox_CameraExposure.Text, out double exposure)) {
                exposure = -1;//转失败就自动曝光
            }
            if (!double.TryParse(TextBox_CameraGain.Text, out double gain)) {
                gain = -1;//转失败就自动增益
            }

            List<ResViewInputParams> datas = new List<ResViewInputParams>();
            datas.Add(new ResViewInputParams() {
                ParamName = "相机索引",
                ActualParam = TextBox_CameraIndex.Text,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "曝光类型",
                ActualParam = ComboBox_CameraExposure.SelectedIndex,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "曝光时间",
                ActualParam = exposure,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "相机增益",
                ActualParam = gain,
            });
            _turnOffResClick?.Invoke(datas);
        }

        private void Event_Button_TurnOnRes(object sender, RoutedEventArgs e) {
            if (!int.TryParse(TextBox_CameraIndex.Text, out int index)) {
                return;
            }
            if (!double.TryParse(TextBox_CameraExposure.Text, out double exposure)) {
                exposure = -1;//转失败就自动曝光
            }
            if (!double.TryParse(TextBox_CameraGain.Text, out double gain)) {
                gain = -1;//转失败就自动增益
            }

            List<ResViewInputParams> datas = new List<ResViewInputParams>();
            datas.Add(new ResViewInputParams() {
                ParamName = "相机索引",
                ActualParam = index,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "曝光类型",
                ActualParam = ComboBox_CameraExposure.SelectedIndex,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "曝光时间",
                ActualParam = exposure,
            });
            datas.Add(new ResViewInputParams() {
                ParamName = "相机增益",
                ActualParam = gain,
            });
            _turnOnResClick?.Invoke(datas);
        }

        public void ViewOperationDataUpdate(in List<ResViewInputParams> inputParams, in List<ResOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(string)) {
                return;
            }
            string cameraStatus = outputParams[0].ActualParam as string;
            TextBox_CameraStatus.Text = cameraStatus;
            if (cameraStatus != "相机正在显示取图") {
                ImageShow_CameraImage.Image = null;
            }
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            ImageShow_CameraImage.Image = outputParams[1].ActualParam as Mat;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_CameraStatus"] = TextBox_CameraStatus.Text;
            json["TextBox_CameraIndex"] = TextBox_CameraIndex.Text;
            json["ComboBox_CameraExposure"] = ComboBox_CameraExposure.SelectedIndex;
            json["TextBox_CameraExposure"] = TextBox_CameraExposure.Text;
            json["TextBox_CameraGain"] = TextBox_CameraGain.Text;
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            TextBox_CameraStatus.Text = json["TextBox_CameraStatus"].ToString();
            TextBox_CameraIndex.Text = json["TextBox_CameraIndex"].ToString();
            ComboBox_CameraExposure.SelectedIndex = ((int)json["ComboBox_CameraExposure"]);
            TextBox_CameraExposure.Text = json["TextBox_CameraExposure"].ToString();
            TextBox_CameraGain.Text = json["TextBox_CameraGain"].ToString();
        }

        public void ResetView() {
            TextBox_CameraStatus.Text = string.Empty;
            TextBox_CameraIndex.Text = "0";
            ComboBox_CameraExposure.SelectedIndex = 0;
            TextBox_CameraExposure.Text = "-1";
            TextBox_CameraGain.Text = "-1";
            ImageShow_CameraImage.ResetView();
        }

        public void SetResTemporaryEventCallback(ResTemporaryEvent cb) {
            if (cb == null) {
                return;
            }
            _resTemporaryEvent = cb;
        }

        public void SetTurnOffResClickCallback(TurnOffResClick cb) {
            if (cb == null) {
                return;
            }
            _turnOffResClick = cb;
        }

        public void SetTurnOnResClickCallback(TurnOnResClick cb) {
            if (cb == null) {
                return;
            }
            _turnOnResClick = cb;
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "USB相机";
                case LangaugeType.kEnglish:
                    return "USBCamera";
                default:
                    break;
            }
            return string.Empty;
        }

        public List<string> AllowTaskPluginLink() {
            List<string> strings = new List<string>();
            TaskOperation_ReadImage readImage = new TaskOperation_ReadImage();
            strings.Add(readImage.OperationUniqueName(LangaugeType.kChinese));
            strings.Add(readImage.OperationUniqueName(LangaugeType.kEnglish));
            return strings;
        }
    }
}
