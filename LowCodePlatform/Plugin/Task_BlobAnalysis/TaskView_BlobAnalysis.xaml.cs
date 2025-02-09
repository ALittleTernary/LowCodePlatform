using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using MenuItem = System.Windows.Controls.MenuItem;

namespace LowCodePlatform.Plugin.Task_BlobAnalysis
{
    /// <summary>
    /// 麻了，DataGrid必须绑定一个数据结构才好使用，简直日狗的设计，不知道哪个鬼才想出来的
    /// </summary>
    public class RegionAnalysisOptionData : INotifyPropertyChanged
    {
        private int _num;
        public int Num
        {
            get { return _num; }
            set {
                _num = value;
                OnPropertyChanged(nameof(Num));
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        private string _region;
        public string Region
        {
            get { return _region; }
            set {
                _region = value;
                OnPropertyChanged(nameof(Region));
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set {
                _description = value;
                OnPropertyChanged(nameof(Description));
            }
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["Num"] = Num;
            json["IsSelected"] = IsSelected;
            json["Region"] = Region;
            json["Name"] = Name;
            json["Description"] = Description;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            Num = ((int)json["Num"]);
            IsSelected = ((bool)json["IsSelected"]);
            Region = (json["Region"].ToString());
            Name = (json["Name"].ToString());
            Description = (json["Description"].ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class RegionAnalysisFeatureData : INotifyPropertyChanged
    {
        private int _num;
        public int Num
        {
            get { return _num; }
            set {
                _num = value;
                OnPropertyChanged(nameof(Num));
            }
        }

        private string _area;
        public string Area
        {
            get { return _area; }
            set {
                _area = value;
                OnPropertyChanged(nameof(Area));
            }
        }

        private string _centerX;
        public string CenterX
        {
            get { return _centerX; }
            set {
                _centerX = value;
                OnPropertyChanged(nameof(CenterX));
            }
        }

        private string _centerY;
        public string CenterY
        {
            get { return _centerY; }
            set {
                _centerY = value;
                OnPropertyChanged(nameof(CenterY));
            }
        }

        private string _circularity;
        public string Circularity
        {
            get { return _circularity; }
            set {
                _circularity = value;
                OnPropertyChanged(nameof(Circularity));
            }
        }

        private string _rectangularity;
        public string Rectangularity
        {
            get { return _rectangularity; }
            set {
                _rectangularity = value;
                OnPropertyChanged(nameof(Rectangularity));
            }
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["Num"] = Num;
            json["Area"] = Area;
            json["CenterX"] = CenterX;
            json["CenterY"] = CenterY;
            json["Circularity"] = Circularity;
            json["Rectangularity"] = Rectangularity;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            Num = ((int)json["Num"]);
            Area = json["Area"].ToString();
            CenterX = json["CenterX"].ToString();
            CenterY = json["CenterY"].ToString();
            Circularity = json["Circularity"].ToString();
            Rectangularity = json["Rectangularity"].ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// TaskView_BlobAnalysis.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_BlobAnalysis : System.Windows.Window, TaskViewPluginBase {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;



        /// <summary>
        /// 必须建立一个对应表处理区域处理具体操作界面中的ui变化
        /// </summary>
        private Dictionary<string, string> _regionAnalysisOptionToParamsDic = new Dictionary<string, string>();

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskView_BlobAnalysis() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            //初始化区域分析的按钮上的菜单
            //区域操作
            MenuItem menuItem_RegionOperator = new MenuItem() { Header = "区域操作" };
            MenuItem menuItem_RegionConnect = new MenuItem() { Header = "区域连通" };
            MenuItem menuItem_RegionMerge = new MenuItem() { Header = "区域合并" };
            menuItem_RegionConnect.Click += Event_MenuItem_Click;
            menuItem_RegionMerge.Click += Event_MenuItem_Click;
            menuItem_RegionOperator.Items.Add(menuItem_RegionConnect);
            menuItem_RegionOperator.Items.Add(menuItem_RegionMerge);
            ContextMenu_RegionAnalysisOptions.Items.Add(menuItem_RegionOperator);
            //形态处理
            MenuItem menuItem_MorphologyProcess = new MenuItem() { Header = "形态处理" };
            MenuItem menuItem_CircleExpansion = new MenuItem() { Header = "圆形膨胀" };
            MenuItem menuItem_CircleCorrosion = new MenuItem() { Header = "圆形腐蚀" };
            MenuItem menuItem_CircleOpenOperator = new MenuItem() { Header = "圆形开运算" };
            MenuItem menuItem_CircleCloseOperator = new MenuItem() { Header = "圆形闭运算" };
            MenuItem menuItem_RectExpansion = new MenuItem() { Header = "矩形膨胀" };
            MenuItem menuItem_RectCorrosion = new MenuItem() { Header = "矩形腐蚀" };
            MenuItem menuItem_RectOpenOperator = new MenuItem() { Header = "矩形开运算" };
            MenuItem menuItem_RectCloseOperator = new MenuItem() { Header = "矩形闭运算" };
            menuItem_CircleExpansion.Click += Event_MenuItem_Click;
            menuItem_CircleCorrosion.Click += Event_MenuItem_Click;
            menuItem_CircleOpenOperator.Click += Event_MenuItem_Click;
            menuItem_CircleCloseOperator.Click += Event_MenuItem_Click;
            menuItem_RectExpansion.Click += Event_MenuItem_Click;
            menuItem_RectCorrosion.Click += Event_MenuItem_Click;
            menuItem_RectOpenOperator.Click += Event_MenuItem_Click;
            menuItem_RectCloseOperator.Click += Event_MenuItem_Click;
            menuItem_MorphologyProcess.Items.Add(menuItem_CircleExpansion);
            menuItem_MorphologyProcess.Items.Add(menuItem_CircleCorrosion);
            menuItem_MorphologyProcess.Items.Add(menuItem_CircleOpenOperator);
            menuItem_MorphologyProcess.Items.Add(menuItem_CircleCloseOperator);
            menuItem_MorphologyProcess.Items.Add(menuItem_RectExpansion);
            menuItem_MorphologyProcess.Items.Add(menuItem_RectCorrosion);
            menuItem_MorphologyProcess.Items.Add(menuItem_RectOpenOperator);
            menuItem_MorphologyProcess.Items.Add(menuItem_RectCloseOperator);
            ContextMenu_RegionAnalysisOptions.Items.Add(menuItem_MorphologyProcess);
            //特殊运算
            MenuItem menuItem_SpecialCalculation = new MenuItem() { Header = "特征筛选" };
            MenuItem menuItem_SelectMaxRegion = new MenuItem() { Header = "获取最大区域" };
            MenuItem menuItem_SelectRegionArea = new MenuItem() { Header = "筛选面积" };
            MenuItem menuItem_SelectRegionCenterX= new MenuItem() { Header = "筛选中心X" };
            MenuItem menuItem_SelectRegionCenterY = new MenuItem() { Header = "筛选中心Y" };
            MenuItem menuItem_SelectRegionRoundness = new MenuItem() { Header = "筛选圆度" };
            MenuItem menuItem_SelectRegionRectangularity = new MenuItem() { Header = "筛选矩形度" };
            menuItem_SelectMaxRegion.Click += Event_MenuItem_Click;
            menuItem_SelectRegionArea.Click += Event_MenuItem_Click;
            menuItem_SelectRegionCenterX.Click += Event_MenuItem_Click;
            menuItem_SelectRegionCenterY.Click += Event_MenuItem_Click;
            menuItem_SelectRegionRoundness.Click += Event_MenuItem_Click;
            menuItem_SelectRegionRectangularity.Click += Event_MenuItem_Click;
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectMaxRegion );
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectRegionArea);
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectRegionCenterX);
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectRegionCenterY);
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectRegionRoundness);
            menuItem_SpecialCalculation.Items.Add(menuItem_SelectRegionRectangularity);
            ContextMenu_RegionAnalysisOptions.Items.Add(menuItem_SpecialCalculation);

            //初始化每一个区域分析的标签和数值，这个玩意应该单独开一个数据结构去处理的
            JObject json_RegionConnect = new JObject();
            json_RegionConnect["Label_ChooseRegion1"] = "处理区域";
            _regionAnalysisOptionToParamsDic["区域连通"] = json_RegionConnect.ToString();

            JObject json_RegionMerge = new JObject();
            json_RegionMerge["Label_ChooseRegion1"] = "处理区域";
            _regionAnalysisOptionToParamsDic["区域合并"] = json_RegionMerge.ToString();

            JObject json_CircleExpansion = new JObject();
            json_CircleExpansion["Label_ChooseRegion1"] = "处理区域";
            json_CircleExpansion["LinkEdit_Params01_LinkLabelText"] = "直径";
            json_CircleExpansion["LinkEdit_Params01_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["圆形膨胀"] = json_CircleExpansion.ToString();

            JObject json_CircleCorrosion = new JObject();
            json_CircleCorrosion["Label_ChooseRegion1"] = "处理区域";
            json_CircleCorrosion["LinkEdit_Params01_LinkLabelText"] = "直径";
            json_CircleCorrosion["LinkEdit_Params01_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["圆形腐蚀"] = json_CircleCorrosion.ToString();

            JObject json_CircleOpenOperator = new JObject();
            json_CircleOpenOperator["Label_ChooseRegion1"] = "处理区域";
            json_CircleOpenOperator["LinkEdit_Params01_LinkLabelText"] = "直径";
            json_CircleOpenOperator["LinkEdit_Params01_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["圆形开运算"] = json_CircleOpenOperator.ToString();

            JObject json_CircleCloseOperator = new JObject();
            json_CircleCloseOperator["Label_ChooseRegion1"] = "处理区域";
            json_CircleCloseOperator["LinkEdit_Params01_LinkLabelText"] = "直径";
            json_CircleCloseOperator["LinkEdit_Params01_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["圆形闭运算"] = json_CircleCloseOperator.ToString();

            JObject json_RectExpansion = new JObject();
            json_RectExpansion["Label_ChooseRegion1"] = "处理区域";
            json_RectExpansion["LinkEdit_Params01_LinkLabelText"] = "宽";
            json_RectExpansion["LinkEdit_Params01_LinkContentText"] = 5;
            json_RectExpansion["LinkEdit_Params02_LinkLabelText"] = "高";
            json_RectExpansion["LinkEdit_Params02_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["矩形膨胀"] = json_RectExpansion.ToString();

            JObject json_RectCorrosion = new JObject();
            json_RectCorrosion["Label_ChooseRegion1"] = "处理区域";
            json_RectCorrosion["LinkEdit_Params01_LinkLabelText"] = "宽";
            json_RectCorrosion["LinkEdit_Params01_LinkContentText"] = 5;
            json_RectCorrosion["LinkEdit_Params02_LinkLabelText"] = "高";
            json_RectCorrosion["LinkEdit_Params02_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["矩形腐蚀"] = json_RectCorrosion.ToString();

            JObject json_RectOpenOperator = new JObject();
            json_RectOpenOperator["Label_ChooseRegion1"] = "处理区域";
            json_RectOpenOperator["LinkEdit_Params01_LinkLabelText"] = "宽";
            json_RectOpenOperator["LinkEdit_Params01_LinkContentText"] = 5;
            json_RectOpenOperator["LinkEdit_Params02_LinkLabelText"] = "高";
            json_RectOpenOperator["LinkEdit_Params02_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["矩形开运算"] = json_RectOpenOperator.ToString();

            JObject json_RectCloseOperator = new JObject();
            json_RectCloseOperator["Label_ChooseRegion1"] = "处理区域";
            json_RectCloseOperator["LinkEdit_Params01_LinkLabelText"] = "宽";
            json_RectCloseOperator["LinkEdit_Params01_LinkContentText"] = 5;
            json_RectCloseOperator["LinkEdit_Params02_LinkLabelText"] = "高";
            json_RectCloseOperator["LinkEdit_Params02_LinkContentText"] = 5;
            _regionAnalysisOptionToParamsDic["矩形闭运算"] = json_RectCloseOperator.ToString();

            JObject json_FindMaxRegion = new JObject();
            json_FindMaxRegion["Label_ChooseRegion1"] = "处理区域";
            _regionAnalysisOptionToParamsDic["获取最大区域"] = json_FindMaxRegion.ToString();

            JObject json_SelectRegionArea = new JObject();
            json_SelectRegionArea["Label_ChooseRegion1"] = "处理区域";
            json_SelectRegionArea["LinkEdit_Params01_LinkLabelText"] = "最小值";
            json_SelectRegionArea["LinkEdit_Params01_LinkContentText"] = 0;
            json_SelectRegionArea["LinkEdit_Params02_LinkLabelText"] = "最大值";
            json_SelectRegionArea["LinkEdit_Params02_LinkContentText"] = 9999999;
            _regionAnalysisOptionToParamsDic["筛选面积"] = json_SelectRegionArea.ToString();

            JObject json_SelectRegionCenterX = new JObject();
            json_SelectRegionCenterX["Label_ChooseRegion1"] = "处理区域";
            json_SelectRegionCenterX["LinkEdit_Params01_LinkLabelText"] = "最小值";
            json_SelectRegionCenterX["LinkEdit_Params01_LinkContentText"] = 0;
            json_SelectRegionCenterX["LinkEdit_Params02_LinkLabelText"] = "最大值";
            json_SelectRegionCenterX["LinkEdit_Params02_LinkContentText"] = 9999999;
            _regionAnalysisOptionToParamsDic["筛选中心X"] = json_SelectRegionCenterX.ToString();

            JObject json_SelectRegionCenterY = new JObject();
            json_SelectRegionCenterY["Label_ChooseRegion1"] = "处理区域";
            json_SelectRegionCenterY["LinkEdit_Params01_LinkLabelText"] = "最小值";
            json_SelectRegionCenterY["LinkEdit_Params01_LinkContentText"] = 0;
            json_SelectRegionCenterY["LinkEdit_Params02_LinkLabelText"] = "最大值";
            json_SelectRegionCenterY["LinkEdit_Params02_LinkContentText"] = 9999999;
            _regionAnalysisOptionToParamsDic["筛选中心Y"] = json_SelectRegionCenterY.ToString();

            JObject json_SelectRegionRoundness = new JObject();
            json_SelectRegionRoundness["Label_ChooseRegion1"] = "处理区域";
            json_SelectRegionRoundness["LinkEdit_Params01_LinkLabelText"] = "最小值";
            json_SelectRegionRoundness["LinkEdit_Params01_LinkContentText"] = 0;
            json_SelectRegionRoundness["LinkEdit_Params02_LinkLabelText"] = "最大值";
            json_SelectRegionRoundness["LinkEdit_Params02_LinkContentText"] = 9999999;
            _regionAnalysisOptionToParamsDic["筛选圆度"] = json_SelectRegionRoundness.ToString();

            JObject json_SelectRegionRectangularity = new JObject();
            json_SelectRegionRectangularity["Label_ChooseRegion1"] = "处理区域";
            json_SelectRegionRectangularity["LinkEdit_Params01_LinkLabelText"] = "最小值";
            json_SelectRegionRectangularity["LinkEdit_Params01_LinkContentText"] = 0;
            json_SelectRegionRectangularity["LinkEdit_Params02_LinkLabelText"] = "最大值";
            json_SelectRegionRectangularity["LinkEdit_Params02_LinkContentText"] = 9999999;
            _regionAnalysisOptionToParamsDic["筛选矩形度"] = json_SelectRegionRectangularity.ToString();
        }

        private void InitEvent() {
            Button_Execute.Click += Event_Button_Execute;
            Button_Confirm.Click += Event_Button_Confirm;
            Button_AddItem.Click += Event_Button_AddItem;
            Button_SubItem.Click += Event_Button_DeleteItem;
            Button_MoveUp.Click += Event_Button_MoveUpItem;
            Button_MoveDown.Click += Event_Button_MoveDownItem;
            DataGrid_RegionAnalysisOptionsArea.SelectionChanged += Event_DataGrid_SelectionItemChanged;
            ComboBox_ChooseRegion1.SelectionChanged += Event_ComboBox_ChooseRegion1_SelectionChanged;
            ComboBox_ChooseRegion2.SelectionChanged += Event_ComboBox_ChooseRegion2_SelectionChanged;
            LinkEdit_Params01.LinkContentTextChanged = Event_LinkEdit_Params01_TextChanged;
            LinkEdit_Params02.LinkContentTextChanged = Event_LinkEdit_Params02_TextChanged;
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
            if (outputParams.Count < 3 || outputParams[2].ActualParam.GetType() != typeof(string)) {
                return;
            }
            string str_FeaturesData = (string)outputParams[2].ActualParam;
            JObject json_FeaturesData = JObject.Parse(str_FeaturesData);
            int masksLength = ((int)json_FeaturesData["MasksLength"]);
            ObservableCollection<RegionAnalysisFeatureData> regionAnalysisFeatureDatas = new ObservableCollection<RegionAnalysisFeatureData>();
            for (int i = 0; i < masksLength; i++) {
                RegionAnalysisFeatureData data = new RegionAnalysisFeatureData();
                data.Num = i;
                data.Area = json_FeaturesData[i + "_Area"].ToString();
                data.CenterX = json_FeaturesData[i + "_CenterX"].ToString();
                data.CenterY = json_FeaturesData[i + "_CenterY"].ToString();
                data.Circularity = json_FeaturesData[i + "_Circularity"].ToString();
                data.Rectangularity = json_FeaturesData[i + "_Rectangularity"].ToString();
                regionAnalysisFeatureDatas.Add(data);
            }
            DataGrid_RegionAttributes.ItemsSource = regionAnalysisFeatureDatas;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_InputImage"] = LinkEdit_InputImage.ViewToJson();
            json["LinkEdit_BinarizationLowerLimit"] = LinkEdit_BinarizationLowerLimit.ViewToJson();
            json["LinkEdit_BinarizationUpperLimit"] = LinkEdit_BinarizationUpperLimit.ViewToJson();
            json["LinkEdit_InputRegion"] = LinkEdit_InputRegion.ViewToJson();
            json["TabControl_RegionType"] = TabControl_RegionType.SelectedIndex;
            // 获取 ItemsSource 的数据源
            ObservableCollection<RegionAnalysisOptionData> sourceDatas = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            json["RegionAnalysisItem_Count"] = sourceDatas.Count;
            for (int i = 0; i < sourceDatas.Count; i++) {
                json["RegionAnalysisItem_" + i] = sourceDatas[i].DataToJson();
            }
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_InputImage.JsonToView(json["LinkEdit_InputImage"].ToString());
            LinkEdit_BinarizationLowerLimit.JsonToView(json["LinkEdit_BinarizationLowerLimit"].ToString());
            LinkEdit_BinarizationUpperLimit.JsonToView(json["LinkEdit_BinarizationUpperLimit"].ToString());
            LinkEdit_InputRegion.JsonToView(json["LinkEdit_InputRegion"].ToString());
            TabControl_RegionType.SelectedIndex = ((int)json["TabControl_RegionType"]);
            int RegionAnalysisItem_Count = ((int)json["RegionAnalysisItem_Count"]);
            ObservableCollection<RegionAnalysisOptionData> datas = new ObservableCollection<RegionAnalysisOptionData>();
            for (int i = 0; i < RegionAnalysisItem_Count; i++) {
                string RegionAnalysisItem_ = json["RegionAnalysisItem_" + i].ToString();
                RegionAnalysisOptionData data = new RegionAnalysisOptionData();
                data.JsonToData(RegionAnalysisItem_);
                datas.Add(data);
            }
            DataGrid_RegionAnalysisOptionsArea.ItemsSource = datas;

        }

        public void ResetView() {
            TabControl_BlobAnalysis.SelectedIndex = 0;
            LinkEdit_InputImage.ResetView();
            TabControl_RegionType.SelectedIndex = 0;
            LinkEdit_BinarizationLowerLimit.ResetView();
            LinkEdit_BinarizationLowerLimit.LinkContentText = "0";
            LinkEdit_BinarizationUpperLimit.ResetView();
            LinkEdit_BinarizationUpperLimit.LinkContentText = "200";
            DataGrid_RegionAnalysisOptionsArea.ItemsSource = new ObservableCollection<RegionAnalysisOptionData>();//重置区域分析选项区
            DataGrid_RegionAttributes.ItemsSource = new ObservableCollection<RegionAnalysisFeatureData>();
            ImageShow_Read.ResetView();
            LinkEdit_InputRegion.ResetView();
            ResetView_RegionAnalysisParams();//重置区域分析参数区
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
            LinkEdit_InputImage.SetLinkClickCallback(linkClickCallback);
            LinkEdit_BinarizationLowerLimit.SetLinkClickCallback(linkClickCallback);
            LinkEdit_BinarizationUpperLimit.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Params01.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Params02.SetLinkClickCallback(linkClickCallback);
            LinkEdit_InputRegion.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "斑点分析";
                case LangaugeType.kEnglish:
                    return "BlobAnalysis";
                default:
                    break;
            }
            return string.Empty;
        }

        /// <summary>
        /// 点击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Confirm(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入图像",
                IsBind = LinkEdit_InputImage.IsBind,
                UserParam = LinkEdit_InputImage.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入区域类型",
                IsBind = false,
                UserParam = (TabControl_RegionType.SelectedItem as TabItem).Header as string,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "二值化下限",
                IsBind = LinkEdit_BinarizationLowerLimit.IsBind,
                UserParam = LinkEdit_BinarizationLowerLimit.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "二值化上限",
                IsBind = LinkEdit_BinarizationUpperLimit.IsBind,
                UserParam = LinkEdit_BinarizationUpperLimit.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "链接区域",
                IsBind = LinkEdit_InputRegion.IsBind,
                UserParam = LinkEdit_InputRegion.UserParam,
            });
            ObservableCollection<RegionAnalysisOptionData> datas = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "区域分析数量",
                IsBind = false,
                UserParam = datas.Count,
            });
            for (int i = 0; i < datas.Count; i++) {
                RegionAnalysisOptionData data = datas[i];
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Num",
                    IsBind = false,
                    UserParam = data.Num,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_IsSelected",
                    IsBind = false,
                    UserParam = data.IsSelected,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Region",
                    IsBind = false,
                    UserParam = data.Region,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Name",
                    IsBind = false,
                    UserParam = data.Name,
                });
                string description = data.Description;
                if (description == string.Empty) {
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 0,
                    });
                    continue;
                }
                JObject json = JObject.Parse(description);
                if (json.ContainsKey("LinkEdit_Params01") && !json.ContainsKey("LinkEdit_Params02")) {
                    string LinkEdit_Params01 = json["LinkEdit_Params01"].ToString();
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 1,
                    });
                    LinkEdit linkEdit1 = new LinkEdit() { LinkContentType = LinkDataType.kDouble };
                    linkEdit1.JsonToView(LinkEdit_Params01);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = linkEdit1.IsBind,
                        UserParam = linkEdit1.UserParam,
                    });
                }
                else if (json.ContainsKey("LinkEdit_Params01") && json.ContainsKey("LinkEdit_Params02")) {
                    string LinkEdit_Params01 = json["LinkEdit_Params01"].ToString();
                    string LinkEdit_Params02 = json["LinkEdit_Params02"].ToString();
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 2,
                    });
                    LinkEdit linkEdit1 = new LinkEdit() { LinkContentType = LinkDataType.kDouble};
                    linkEdit1.JsonToView(LinkEdit_Params01);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Params01",
                        IsBind = linkEdit1.IsBind,
                        UserParam = linkEdit1.UserParam,
                    });
                    LinkEdit linkEdit2 = new LinkEdit() { LinkContentType = LinkDataType.kDouble };
                    linkEdit2.JsonToView(LinkEdit_Params02);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Params02",
                        IsBind = linkEdit2.IsBind,
                        UserParam = linkEdit2.UserParam,
                    });
                }
                else {

                }
            }
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
                ParamName = "输入图像",
                IsBind = LinkEdit_InputImage.IsBind,
                UserParam = LinkEdit_InputImage.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "输入区域类型",
                IsBind = false,
                UserParam = (TabControl_RegionType.SelectedItem as TabItem).Header as string,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "二值化下限",
                IsBind = LinkEdit_BinarizationLowerLimit.IsBind,
                UserParam = LinkEdit_BinarizationLowerLimit.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "二值化上限",
                IsBind = LinkEdit_BinarizationUpperLimit.IsBind,
                UserParam = LinkEdit_BinarizationUpperLimit.UserParam,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "链接区域",
                IsBind = LinkEdit_InputRegion.IsBind,
                UserParam = LinkEdit_InputRegion.UserParam,
            });
            ObservableCollection<RegionAnalysisOptionData> datas = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "区域分析数量",
                IsBind = false,
                UserParam = datas.Count,
            });
            for (int i = 0; i < datas.Count; i++) {
                RegionAnalysisOptionData data = datas[i];
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Num",
                    IsBind = false,
                    UserParam = data.Num,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_IsSelected",
                    IsBind = false,
                    UserParam = data.IsSelected,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Region",
                    IsBind = false,
                    UserParam = data.Region,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_Name",
                    IsBind = false,
                    UserParam = data.Name,
                });
                string description = data.Description;
                if (description == string.Empty) {
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 0,
                    });
                    continue;
                }
                JObject json = JObject.Parse(description);
                if (json.ContainsKey("LinkEdit_Params01") && !json.ContainsKey("LinkEdit_Params02")) {
                    string LinkEdit_Params01 = json["LinkEdit_Params01"].ToString();
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 1,
                    });
                    LinkEdit linkEdit1 = new LinkEdit() { LinkContentType = LinkDataType.kDouble };
                    linkEdit1.JsonToView(LinkEdit_Params01);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = linkEdit1.IsBind,
                        UserParam = linkEdit1.UserParam,
                    });
                }
                else if (json.ContainsKey("LinkEdit_Params01") && json.ContainsKey("LinkEdit_Params02")) {
                    string LinkEdit_Params01 = json["LinkEdit_Params01"].ToString();
                    string LinkEdit_Params02 = json["LinkEdit_Params02"].ToString();
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Count",
                        IsBind = false,
                        UserParam = 2,
                    });
                    LinkEdit linkEdit1 = new LinkEdit() { LinkContentType = LinkDataType.kDouble };
                    linkEdit1.JsonToView(LinkEdit_Params01);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Params01",
                        IsBind = linkEdit1.IsBind,
                        UserParam = linkEdit1.UserParam,
                    });
                    LinkEdit linkEdit2 = new LinkEdit() { LinkContentType = LinkDataType.kDouble };
                    linkEdit2.JsonToView(LinkEdit_Params02);
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Description_Params02",
                        IsBind = linkEdit2.IsBind,
                        UserParam = linkEdit2.UserParam,
                    });
                }
                else {

                }
            }
            _executeClick?.Invoke(inputParams);
        }

        private void Event_Button_AddItem(object sender, RoutedEventArgs e) {
            ContextMenu_RegionAnalysisOptions.IsOpen = true;
        }

        private void Event_MenuItem_Click(object sender, RoutedEventArgs e) {
            ContextMenu_RegionAnalysisOptions.IsOpen = false;
            var menuItem = sender as MenuItem;
            if (menuItem == null) {
                return;
            }

            // 创建一个新的 RegionAnalysisOptionData 对象
            RegionAnalysisOptionData data = new RegionAnalysisOptionData() {
                Num = DataGrid_RegionAnalysisOptionsArea.Items.Count,
                IsSelected = true,
                Region = "上一区域",
                Name = menuItem.Header.ToString(),
                Description = string.Empty,
            };

            // 获取 ItemsSource 的数据源
            ObservableCollection<RegionAnalysisOptionData> sourceDatas = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            if (sourceDatas != null) {
                // 如果是 ObservableCollection，直接添加数据
                sourceDatas.Add(data);
            }
            else {
                // 如果 ItemsSource 是 null 或不是 ObservableCollection，创建一个新的列表并赋值
                ObservableCollection<RegionAnalysisOptionData> datas = new ObservableCollection<RegionAnalysisOptionData>() { data };
                DataGrid_RegionAnalysisOptionsArea.ItemsSource = datas;
            }

            //设置当前选中项为新增的项，把数据显示一下，这个会触发Event_DataGrid_SelectionItemChanged
            DataGrid_RegionAnalysisOptionsArea.SelectedItem = data;
        }

        /// <summary>
        /// 点击按钮删除当前选中的item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_DeleteItem(object sender, RoutedEventArgs e) {
            var selectedItem = DataGrid_RegionAnalysisOptionsArea.SelectedItem;
            if (selectedItem == null) {
                return;
            }
            // 假设你的数据集合是 ObservableCollection<T>
            var itemList = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            if (itemList == null) {
                return;
            }
            itemList.Remove(selectedItem as RegionAnalysisOptionData);  // 删除选中的项

            //重新整理序号
            for (int i = 0; i < itemList.Count; i++) {
                itemList[i].Num = i;
            }
        }

        /// <summary>
        /// 点击按钮上移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveUpItem(object sender, RoutedEventArgs e) {
            RegionAnalysisOptionData selectedItem = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectedItem == null) {
                return;
            }
            var itemList = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            if (itemList == null) {
                return;
            }
            int index = itemList.IndexOf(selectedItem);
            // 如果选中的项不是第一个元素
            if (index <= 0) {
                return;
            }
            // 将选中的项和前一个项交换位置
            itemList.RemoveAt(index);
            itemList.Insert(index - 1, selectedItem);
            // 更新选中项
            DataGrid_RegionAnalysisOptionsArea.SelectedItem = selectedItem;
            //重新整理序号
            for (int i = 0; i < itemList.Count; i++) {
                itemList[i].Num = i;
            }
        }

        /// <summary>
        /// 点击按钮下移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveDownItem(object sender, RoutedEventArgs e) {
            RegionAnalysisOptionData selectedItem = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectedItem == null) {
                return;
            }
            var itemList = DataGrid_RegionAnalysisOptionsArea.ItemsSource as ObservableCollection<RegionAnalysisOptionData>;
            if (itemList == null) {
                return;
            }
            int index = itemList.IndexOf(selectedItem);
            // 如果选中的项不是最后一个元素
            if (index >= itemList.Count - 1)  {
                return;
            }
            // 将选中的项和下一个项交换位置
            itemList.RemoveAt(index);
            itemList.Insert(index + 1, selectedItem);

            // 更新选中项
            DataGrid_RegionAnalysisOptionsArea.SelectedItem = selectedItem;
            //重新整理序号
            for (int i = 0; i < itemList.Count; i++) {
                itemList[i].Num = i;
            }
        }

        /// <summary>
        /// 重置区域具体操作区，包括选项或者文本改变的委托均清空
        /// </summary>
        private void ResetView_RegionAnalysisParams() {
            Label_ChooseRegion1.Content = string.Empty;
            Label_ChooseRegion1.Visibility = Visibility.Collapsed;

            ComboBox_ChooseRegion1.Items.Clear();
            ComboBox_ChooseRegion1.Items.Add(new ComboBoxItem() { Content = "上一区域" });
            ComboBox_ChooseRegion1.SelectedIndex = 0;//初始为0
            ComboBox_ChooseRegion1.Visibility = Visibility.Collapsed;

            Label_ChooseRegion2.Content = string.Empty;
            Label_ChooseRegion2.Visibility = Visibility.Collapsed;

            ComboBox_ChooseRegion2.Items.Clear();
            ComboBox_ChooseRegion2.Items.Add(new ComboBoxItem() { Content = "上一区域" });
            ComboBox_ChooseRegion2.SelectedIndex = 0;//初始为0
            ComboBox_ChooseRegion2.Visibility = Visibility.Collapsed;

            LinkEdit_Params01.ResetView();
            LinkEdit_Params01.Visibility = Visibility.Collapsed;

            LinkEdit_Params02.ResetView();
            LinkEdit_Params02.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 当前选中item选中或者取消行时触发，切换每个item的编辑界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_DataGrid_SelectionItemChanged(object sender, SelectionChangedEventArgs e) {
            RegionAnalysisOptionData selectData = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectData == null) {
                return;
            }
            if (selectData.Name == "区域连通" 
                || selectData.Name == "区域合并" 
                || selectData.Name == "获取最大区域") {
                RegionAnalysisParams_Template01_1Combobox(selectData);
            }
            else if (selectData.Name == "圆形膨胀" 
                || selectData.Name == "圆形腐蚀" 
                || selectData.Name == "圆形开运算" 
                || selectData.Name == "圆形闭运算") {
                RegionAnalysisParams_Template02_1Combobox1Link(selectData);
            }
            else if (selectData.Name == "矩形膨胀" 
                || selectData.Name == "矩形腐蚀" 
                || selectData.Name == "矩形开运算" 
                || selectData.Name == "矩形闭运算"
                || selectData.Name == "筛选面积"
                || selectData.Name == "筛选中心X"
                || selectData.Name == "筛选中心Y"
                || selectData.Name == "筛选圆度"
                || selectData.Name == "筛选矩形度") {
                RegionAnalysisParams_Template03_1Combobox2Link(selectData);
            }
            else {

            }
        }

        /// <summary>
        /// 如果ComboBox_ChooseRegion1选中项改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_ComboBox_ChooseRegion1_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RegionAnalysisOptionData selectData = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectData == null) {
                return;
            }
            ComboBoxItem selectItem1 = ComboBox_ChooseRegion1.SelectedItem as ComboBoxItem;
            if (selectItem1 == null) {
                return;
            }
            string selectText1 = selectItem1.Content as string;

            ComboBoxItem selectItem2 = ComboBox_ChooseRegion2.SelectedItem as ComboBoxItem;
            if (selectItem2 == null) {
                return;
            }
            string selectText2 = selectItem2.Content as string;

            //先根据选中项获取当前格式
            string defaultData = _regionAnalysisOptionToParamsDic[selectData.Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            //只有一个label
            if (json_defaultData.ContainsKey("Label_ChooseRegion1") && !json_defaultData.ContainsKey("Label_ChooseRegion2")) {
                selectData.Region = selectText1;
            }
            //有两个label
            else if (json_defaultData.ContainsKey("Label_ChooseRegion1") && json_defaultData.ContainsKey("Label_ChooseRegion2")) {
                selectData.Region = selectText1 + "," + selectText2;
            }
            else { 
            
            }
        }

        /// <summary>
        /// 如果ComboBox_ChooseRegion2选中项改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_ComboBox_ChooseRegion2_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            RegionAnalysisOptionData selectData = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectData == null) {
                return;
            }
            ComboBoxItem selectItem1 = ComboBox_ChooseRegion1.SelectedItem as ComboBoxItem;
            if (selectItem1 == null) {
                return;
            }
            string selectText1 = selectItem1.Content as string;

            ComboBoxItem selectItem2 = ComboBox_ChooseRegion2.SelectedItem as ComboBoxItem;
            if (selectItem2 == null) {
                return;
            }
            string selectText2 = selectItem2.Content as string;

            //先根据选中项获取当前格式
            string defaultData = _regionAnalysisOptionToParamsDic[selectData.Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            //有两个label
            if (json_defaultData.ContainsKey("Label_ChooseRegion1") && json_defaultData.ContainsKey("Label_ChooseRegion2")) {
                selectData.Region = selectText1 + "," + selectText2;
            }
            else {

            }
        }

        /// <summary>
        /// 如果LinkEdit_Params01文本改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_LinkEdit_Params01_TextChanged(object sender, TextChangedEventArgs e) {
            RegionAnalysisOptionData selectData = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectData == null) {
                return;
            }
            //先根据选中项获取当前格式
            string defaultData = _regionAnalysisOptionToParamsDic[selectData.Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            //只有一个linkedit
            if (json_defaultData.ContainsKey("LinkEdit_Params01_LinkLabelText") && !json_defaultData.ContainsKey("LinkEdit_Params02_LinkLabelText")) {
                JObject json = new JObject();
                json["LinkEdit_Params01"] = LinkEdit_Params01.ViewToJson();
                selectData.Description = json.ToString();
            }
            else if (json_defaultData.ContainsKey("LinkEdit_Params01_LinkLabelText") && json_defaultData.ContainsKey("LinkEdit_Params02_LinkLabelText")) {
                JObject json = new JObject();
                json["LinkEdit_Params01"] = LinkEdit_Params01.ViewToJson();
                json["LinkEdit_Params02"] = LinkEdit_Params02.ViewToJson();
                selectData.Description = json.ToString();
            }
            else { 
            
            }
        }

        /// <summary>
        /// 如果LinkEdit_Params02文本改变
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_LinkEdit_Params02_TextChanged(object sender, TextChangedEventArgs e) {
            RegionAnalysisOptionData selectData = DataGrid_RegionAnalysisOptionsArea.SelectedItem as RegionAnalysisOptionData;
            if (selectData == null) {
                return;
            }
            //先根据选中项获取当前格式
            string defaultData = _regionAnalysisOptionToParamsDic[selectData.Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            if (json_defaultData.ContainsKey("LinkEdit_Params01_LinkLabelText") && json_defaultData.ContainsKey("LinkEdit_Params02_LinkLabelText")) {
                JObject json = new JObject();
                json["LinkEdit_Params01"] = LinkEdit_Params01.ViewToJson();
                json["LinkEdit_Params02"] = LinkEdit_Params02.ViewToJson();
                selectData.Description = json.ToString();
            }
            else {

            }
        }

        private void RegionAnalysisParams_Template01_1Combobox(RegionAnalysisOptionData selectData) {
            //把需要的参数先保存下来，因为selectData和界面绑定,拍个快照
            int snapshot_Num = selectData.Num;
            bool snapshot_IsSelected = selectData.IsSelected;
            string snapshot_Region = selectData.Region;
            string snapshot_Name = selectData.Name;
            string snapshot_Description = selectData.Description;

            //重置区域分析参数界面，也就是把数据清空并把所有控件隐藏
            ResetView_RegionAnalysisParams();

            //根据snapshot_Name把该有的界面显示
            Label_ChooseRegion1.Visibility = Visibility.Visible;
            ComboBox_ChooseRegion1.Visibility = Visibility.Visible;

            //根据snapshot_Name把该有的界面参数初始化
            string defaultData = _regionAnalysisOptionToParamsDic[snapshot_Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            Label_ChooseRegion1.Content = json_defaultData["Label_ChooseRegion1"].ToString();

            //根据snapshot_Num把"区域"还原
            for (int i = 0; i < snapshot_Num; i++) {
                ComboBox_ChooseRegion1.Items.Add(new ComboBoxItem() { Content = i.ToString() });//根据当前序号，重置区域可供选择项
            }
            for (int i = 0; i < ComboBox_ChooseRegion1.Items.Count; i++) {
                ComboBoxItem currentItem = ComboBox_ChooseRegion1.Items[i] as ComboBoxItem;
                if (currentItem == null) {
                    continue;
                }
                string currentIndex = currentItem.Content as string;
                if (currentIndex == null) {
                    continue;
                }
                if (currentIndex != snapshot_Region) {
                    continue;
                }
                ComboBox_ChooseRegion1.SelectedIndex = i;
            }
        }

        private void RegionAnalysisParams_Template02_1Combobox1Link(RegionAnalysisOptionData selectData) {
            //把需要的参数先保存下来，因为selectData和界面绑定,拍个快照
            int snapshot_Num = selectData.Num;
            bool snapshot_IsSelected = selectData.IsSelected;
            string snapshot_Region = selectData.Region;
            string snapshot_Name = selectData.Name;
            string snapshot_Description = selectData.Description;

            //重置区域分析参数界面，也就是把数据清空并把所有控件隐藏
            ResetView_RegionAnalysisParams();

            //根据snapshot_Name把该有的界面显示
            Label_ChooseRegion1.Visibility = Visibility.Visible;
            ComboBox_ChooseRegion1.Visibility = Visibility.Visible;
            LinkEdit_Params01.Visibility = Visibility.Visible;

            //根据snapshot_Name把该有的界面参数初始化
            string defaultData = _regionAnalysisOptionToParamsDic[snapshot_Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            Label_ChooseRegion1.Content = json_defaultData["Label_ChooseRegion1"].ToString();
            LinkEdit_Params01.LinkLabelText = json_defaultData["LinkEdit_Params01_LinkLabelText"].ToString();
            LinkEdit_Params01.LinkContentText = ((int)json_defaultData["LinkEdit_Params01_LinkContentText"]).ToString();

            //根据snapshot_Num把处理区域还原
            for (int i = 0; i < snapshot_Num; i++) {
                ComboBox_ChooseRegion1.Items.Add(new ComboBoxItem() { Content = i.ToString() });//根据当前序号，重置区域可供选择项
            }
            for (int i = 0; i < ComboBox_ChooseRegion1.Items.Count; i++) {
                ComboBoxItem currentItem = ComboBox_ChooseRegion1.Items[i] as ComboBoxItem;
                if (currentItem == null) { 
                    continue;
                }
                string currentIndex = currentItem.Content as string;
                if (currentIndex == null) {
                    continue;
                }
                if (currentIndex != snapshot_Region) {
                    continue;
                }
                ComboBox_ChooseRegion1.SelectedIndex = i;
            }

            //根据snapshot_Description把剩余的参数处理区域完成
            if (snapshot_Description == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(snapshot_Description);
            LinkEdit_Params01.JsonToView(json["LinkEdit_Params01"].ToString());
        }

        private void RegionAnalysisParams_Template03_1Combobox2Link(RegionAnalysisOptionData selectData) {
            //把需要的参数先保存下来，因为selectData和界面绑定,拍个快照
            int snapshot_Num = selectData.Num;
            bool snapshot_IsSelected = selectData.IsSelected;
            string snapshot_Region = selectData.Region;
            string snapshot_Name = selectData.Name;
            string snapshot_Description = selectData.Description;

            //重置区域分析参数界面，也就是把数据清空并把所有控件隐藏
            ResetView_RegionAnalysisParams();

            //根据snapshot_Name把该有的界面显示
            Label_ChooseRegion1.Visibility = Visibility.Visible;
            ComboBox_ChooseRegion1.Visibility = Visibility.Visible;
            LinkEdit_Params01.Visibility = Visibility.Visible;
            LinkEdit_Params02.Visibility = Visibility.Visible;

            //根据snapshot_Name把该有的界面参数初始化
            string defaultData = _regionAnalysisOptionToParamsDic[snapshot_Name];
            JObject json_defaultData = JObject.Parse(defaultData);
            Label_ChooseRegion1.Content = json_defaultData["Label_ChooseRegion1"].ToString();
            LinkEdit_Params01.LinkLabelText = json_defaultData["LinkEdit_Params01_LinkLabelText"].ToString();
            LinkEdit_Params01.LinkContentText = ((int)json_defaultData["LinkEdit_Params01_LinkContentText"]).ToString();
            LinkEdit_Params02.LinkLabelText = json_defaultData["LinkEdit_Params02_LinkLabelText"].ToString();
            LinkEdit_Params02.LinkContentText = ((int)json_defaultData["LinkEdit_Params02_LinkContentText"]).ToString();

            //根据snapshot_Num把处理区域还原
            for (int i = 0; i < snapshot_Num; i++) {
                ComboBox_ChooseRegion1.Items.Add(new ComboBoxItem() { Content = i.ToString() });//根据当前序号，重置区域可供选择项
            }
            for (int i = 0; i < ComboBox_ChooseRegion1.Items.Count; i++) {
                ComboBoxItem currentItem = ComboBox_ChooseRegion1.Items[i] as ComboBoxItem;
                if (currentItem == null) {
                    continue;
                }
                string currentIndex = currentItem.Content as string;
                if (currentIndex == null) {
                    continue;
                }
                if (currentIndex != snapshot_Region) {
                    continue;
                }
                ComboBox_ChooseRegion1.SelectedIndex = i;
            }

            //根据snapshot_Description把剩余的参数处理区域完成
            if (snapshot_Description == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(snapshot_Description);
            LinkEdit_Params01.JsonToView(json["LinkEdit_Params01"].ToString());
            LinkEdit_Params02.JsonToView(json["LinkEdit_Params02"].ToString());
        }

    }
}
