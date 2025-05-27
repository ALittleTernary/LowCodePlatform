using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_BlobAnalysis;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace LowCodePlatform.Plugin.Task_TemplateMatch
{
    public class TemplateMatchData : INotifyPropertyChanged
    {
        private int _serialNumber;
        public int SerialNumber
        {
            get { return _serialNumber; }
            set {
                _serialNumber = value;
                OnPropertyChanged(nameof(SerialNumber));
            }
        }

        private double _initiationX;
        public double InitiationX
        {
            get { return _initiationX; }
            set {
                _initiationX = value;
                OnPropertyChanged(nameof(InitiationX));
            }
        }

        private double _initiationY;
        public double InitiationY
        {
            get { return _initiationY; }
            set {
                _initiationY = value;
                OnPropertyChanged(nameof(InitiationY));
            }
        }

        private double _matchValue;
        public double MatchValue
        {
            get { return _matchValue; }
            set {
                _matchValue = value;
                OnPropertyChanged(nameof(MatchValue));
            }
        }


        public string DataToJson() {
            JObject json = new JObject();
            json["SerialNumber"] = SerialNumber;
            json["InitiationX"] = InitiationX;
            json["InitiationY"] = InitiationY;
            json["MatchValue"] = MatchValue;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            SerialNumber = ((int)json["SerialNumber"]);
            InitiationX = ((double)json["InitiationX"]);
            InitiationY = ((double)json["InitiationY"]);
            MatchValue = ((double)json["MatchValue"]);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }


    /// <summary>
    /// TaskView_TemplateMatch.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_TemplateMatch : System.Windows.Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_TemplateMatch() {
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
                    ParamName = "模板图像",
                    IsBind = LinkEdit_TemplateImage.IsBind,
                    UserParam = LinkEdit_TemplateImage.UserParam,
                });

                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "匹配度阈值",
                    IsBind = LinkEdit_MatchThreshold.IsBind,
                    UserParam = LinkEdit_MatchThreshold.UserParam,
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
                    ParamName = "模板图像",
                    IsBind = LinkEdit_TemplateImage.IsBind,
                    UserParam = LinkEdit_TemplateImage.UserParam,
                });

                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "匹配度阈值",
                    IsBind = LinkEdit_MatchThreshold.IsBind,
                    UserParam = LinkEdit_MatchThreshold.UserParam,
                });
                _executeClick?.Invoke(inputParams);
            };;
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(Mat[])) {
                return;
            }
            Mat[] masks = (Mat[])outputParams[0].ActualParam;
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            Mat targetImage = (Mat)outputParams[1].ActualParam;
            if (outputParams.Count < 3 || outputParams[2].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            Mat templateImage = (Mat)outputParams[2].ActualParam;
            if (outputParams.Count < 4 || outputParams[3].ActualParam.GetType() != typeof(ObservableCollection<TemplateMatchData>)) {
                return;
            }
            ObservableCollection<TemplateMatchData> templateMatchDatas = outputParams[3].ActualParam as ObservableCollection<TemplateMatchData>;

            ImageShow_Template.Image = templateImage;
            ImageShow_Target.Image = targetImage;
            ImageShow_Target.Masks = masks;
            DataGrid_RegionAttributes.ItemsSource = templateMatchDatas;
        }

        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_TargetImage.JsonToView(json["LinkEdit_TargetImage"].ToString());
            LinkEdit_TemplateImage.JsonToView(json["LinkEdit_TemplateImage"].ToString());
            LinkEdit_MatchThreshold.JsonToView(json["LinkEdit_MatchThreshold"].ToString());

            int DataGrid_RegionAttributes_Count = ((int)json["DataGrid_RegionAttributes_Count"]);
            ObservableCollection<TemplateMatchData> datas = new ObservableCollection<TemplateMatchData>();
            for (int i = 0; i < DataGrid_RegionAttributes_Count; i++) {
                string DataGrid_RegionAttributes_ = json["DataGrid_RegionAttributes_" + i].ToString();
                TemplateMatchData data = new TemplateMatchData();
                data.JsonToData(DataGrid_RegionAttributes_);
                datas.Add(data);
            }
            DataGrid_RegionAttributes.ItemsSource = datas;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_TargetImage"] = LinkEdit_TargetImage.ViewToJson();
            json["LinkEdit_TemplateImage"] = LinkEdit_TemplateImage.ViewToJson();
            json["LinkEdit_MatchThreshold"] = LinkEdit_MatchThreshold.ViewToJson();

            ObservableCollection<TemplateMatchData> sourceDatas = DataGrid_RegionAttributes.ItemsSource as ObservableCollection<TemplateMatchData>;
            json["DataGrid_RegionAttributes_Count"] = sourceDatas.Count;
            for (int i = 0; i < sourceDatas.Count; i++) {
                json["DataGrid_RegionAttributes_" + i] = sourceDatas[i].DataToJson();
            }
            return json.ToString();
        }

        public void ResetView() {
            LinkEdit_TargetImage.ResetView();
            LinkEdit_TemplateImage.ResetView();
            LinkEdit_TemplateImage.ResetView();
            DataGrid_RegionAttributes.ItemsSource = new ObservableCollection<TemplateMatchData>();
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "模板匹配";
                case LangaugeType.kEnglish:
                    return "TemplateMatch";
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
            LinkEdit_TemplateImage.SetLinkClickCallback(linkClickCallback);
            LinkEdit_MatchThreshold.SetLinkClickCallback(linkClickCallback);
        }

    }
}
