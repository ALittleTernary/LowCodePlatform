using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_BlobAnalysis;
using LowCodePlatform.View.Base;
using Newtonsoft.Json.Linq;
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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static OpenCvSharp.ML.DTrees;

namespace LowCodePlatform.View
{
    /// <summary>
    /// 资源列表的数据结构
    /// </summary>
    public class ResourceOptionData : INotifyPropertyChanged
    {
        private int _serialNum = -1;
        /// <summary>
        /// 资源序号
        /// </summary>
        public int SerialNum
        {
            get { return _serialNum; }
            set {
                _serialNum = value;
                OnPropertyChanged(nameof(SerialNum));
            }
        }

        private bool _resStatus = false;
        /// <summary>
        /// 资源状态
        /// </summary>
        public bool ResStatus
        {
            get { return _resStatus; }
            set {
                _resStatus = value;
                OnPropertyChanged(nameof(ResStatus));
            }
        }

        private string _resType = string.Empty;
        /// <summary>
        /// 资源类型
        /// </summary>
        public string ResType
        {
            get { return _resType; }
            set {
                _resType = value;
                OnPropertyChanged(nameof(ResType));
            }
        }

        private string _resName = string.Empty;
        /// <summary>
        /// 资源名字
        /// 名字唯一
        /// </summary>
        public string ResName
        {
            get { return _resName; }
            set {
                _resName = value;
                OnPropertyChanged(nameof(ResName));
            }
        }

        /// <summary>
        /// 输入参数
        /// </summary>
        public List<ResViewInputParams> InputParams { get; set; } = new List<ResViewInputParams>();

        /// <summary>
        /// 输出参数
        /// </summary>
        public List<ResOperationOutputParams> OutputParams { get; set; } = new List<ResOperationOutputParams>();

        /// <summary>
        /// ViewToJson存储的数据，以后要给到JsonToView，用于保存还原界面
        /// </summary>
        public string Data_JsonView { set; get; } = string.Empty;

        /// <summary>
        /// 界面插件接口
        /// </summary>
        public ResViewPluginBase ResViewInterface { set; get; } = null;

        /// <summary>
        /// 界面插件运算
        /// </summary>
        public ResOperationPluginBase ResOperationInterface { set; get; } = null;

        public string DataToJson() {
            JObject json = new JObject();
            json["SerialNum"] = SerialNum;
            json["ResStatus"] = ResStatus;
            json["ResType"] = ResType;
            json["ResName"] = ResName;
            json["Data_JsonView"] = Data_JsonView;
            json["InputParamsCount"] = InputParams.Count;
            for (int i = 0; i < InputParams.Count; i++) {
                json[i + "_InputParam"] = InputParams[i].DataToJson();
            }
            json["OutputParamsCount"] = OutputParams.Count;
            for (int i = 0; i < OutputParams.Count; i++) {
                json[i + "_OutputParam"] = OutputParams[i].DataToJson();
            }
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            JObject json = JObject.Parse(str);
            SerialNum = ((int)json["SerialNum"]);
            ResStatus = ((bool)json["ResStatus"]);
            ResType = (json["ResType"].ToString());
            ResName = (json["ResName"].ToString());
            Data_JsonView = json["Data_JsonView"].ToString();
            int inputParamsCount = ((int)json["InputParamsCount"]);
            InputParams.Clear();
            for (int i = 0; i < inputParamsCount; i++) {
                ResViewInputParams param = new ResViewInputParams();
                param.JsonToData(json[i + "_InputParam"].ToString());
                InputParams.Add(param);
            }
            int outputParamsCount = ((int)json["OutputParamsCount"]);
            OutputParams.Clear();
            for (int i = 0; i < outputParamsCount; i++) {
                ResOperationOutputParams param = new ResOperationOutputParams();
                param.JsonToData(json[i + "_OutputParam"].ToString());
                OutputParams.Add(param);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
    /// <summary>
    /// GlobalResource.xaml 的交互逻辑
    /// </summary>
    public partial class GlobalResource : Window, CommunicationUser
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        private static readonly object _lockObject = new object();

        /// <summary>
        /// 构造函数
        /// </summary>
        public GlobalResource() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            Title = "全局资源";
            DataGrid_Resource.ItemsSource = new ObservableCollection<ResourceOptionData>();
        }

        private void InitEvent() {
            Button_Add.Click += Event_Button_AddItem;
            Button_Sub.Click += Event_Button_SubItem;
            Button_Rename.Click += Event_Button_RenameItem;
            DataGrid_Resource.SelectionChanged += Event_DataGrid_SelectionItemChanged;
            Button_MinWindow.Click += Event_Window_MinimizeButtonClick;
            Button_MaxWindow.Click += Event_Window_MaximizeButtonClick;
            Button_HideWindow.Click += Event_Window_CloseButtonClick;
            MouseLeftButtonDown += Event_Window_MouseLeftButtonDown;
            Closing += Event_Button_Closing;
        }

        public void InitGlobalResourceOptions(List<string> resNames) {
            foreach (string name in resNames) {
                MenuItem menuItem = new MenuItem() { Header = name };
                menuItem.Click += Event_MenuItem_Click;
                ContextMenu_Add.Items.Add(menuItem);
            }
        }

        /// <summary>
        /// 外部关闭界面时，需要释放资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Closing(object sender, CancelEventArgs e) {
            ObservableCollection<ResourceOptionData> sourceDatas = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            foreach (var data in sourceDatas) {
                data.ResOperationInterface.Dispose();
            }
        }

        private void Event_Button_AddItem(object sender, RoutedEventArgs e) {
            ContextMenu_Add.IsOpen = true;
        }

        private void Event_MenuItem_Click(object sender, RoutedEventArgs e) {
            ContextMenu_Add.IsOpen = false;
            var menuItem = sender as MenuItem;
            if (menuItem == null) {
                return;
            }

            // 创建一个新的 RegionAnalysisOptionData 对象
            ResourceOptionData data = new ResourceOptionData() {
                SerialNum = DataGrid_Resource.Items.Count,
                ResStatus = false,
                ResType = menuItem.Header.ToString(),
                ResName = menuItem.Header.ToString(),
            };
            data.ResName = CheckUniqueItemName(data.ResType, data.ResName);
            data.ResViewInterface = _sendMessage?.Invoke(new CommunicationCenterMessage("GlobalResource", "PluginManager", "GetResViewInterfaceByName", data.ResType)) as ResViewPluginBase;
            data.ResOperationInterface = _sendMessage?.Invoke(new CommunicationCenterMessage("GlobalResource", "PluginManager", "GetResOperationInterfaceByName", data.ResType)) as ResOperationPluginBase;
            data.ResViewInterface.SetTurnOffResClickCallback(ViewClickTurnOffRes);
            data.ResViewInterface.SetTurnOnResClickCallback(ViewClickTurnOnRes);
            data.ResViewInterface.SetResTemporaryEventCallback(ViewResTemporaryEvent);

            TabItem tabItem = new TabItem() {
                Header = data.ResName,
                Visibility = Visibility.Collapsed,
                Content = data.ResViewInterface,
            };
            TabControl_Resource.Items.Add(tabItem);
            TabControl_Resource.SelectedItem = tabItem;//切换到指定选项

            // 获取 ItemsSource 的数据源
            ObservableCollection<ResourceOptionData> sourceDatas = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            sourceDatas.Add(data);
            DataGrid_Resource.SelectedItem = data;//切换到指定选项，会触发Event_DataGrid_SelectionItemChanged
        }

        private void Event_Button_SubItem(object sender, RoutedEventArgs e) {
            ResourceOptionData selectedData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectedData == null) {
                return;
            }
            //释放资源
            selectedData.ResOperationInterface.Dispose();
            //删除显示区
            foreach (TabItem item in TabControl_Resource.Items) {
                if (item.Header.ToString() != selectedData.ResName) {
                    continue;
                }
                TabControl_Resource.Items.Remove(item);
                break;
            }
            //删除选项区
            ObservableCollection<ResourceOptionData> resPluginItems = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            if (resPluginItems == null) {
                return;
            }
            resPluginItems.Remove(selectedData);// 删除选中的项              
            //最后再重新整理序号
            for (int i = 0; i < resPluginItems.Count; i++) {
                resPluginItems[i].SerialNum = i;
            }
        }

        private void Event_Button_RenameItem(object sender, RoutedEventArgs e) {
            ResourceOptionData selectedData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectedData == null) {
                return;
            }
            TabItem selectedTab = null;
            foreach (TabItem item in TabControl_Resource.Items) {
                if (item.Header.ToString() != selectedData.ResName) { 
                    continue;
                }
                selectedTab = item;
                break;
            }
            if (selectedTab == null) { 
                return;
            }

            //先前的名字
            string previousName = selectedData.ResName;
            InputDialog inputDialog = new InputDialog() { Label = "重命名资源名", DefaultText = previousName };
            inputDialog.ShowDialog();
            string userInput = inputDialog.InputText;
            if (userInput == null) {
                return;
            }
            //没改名字就别整了，改了名字再整，没改名字也要check名字独特性就会拿到新的名字
            if (userInput == previousName) {
                return;
            }
            selectedData.ResName = CheckUniqueItemName(selectedData.ResType, userInput);
            selectedTab.Header = selectedData.ResName;
        }

        /// <summary>
        /// 当前选中项切换
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_DataGrid_SelectionItemChanged(object sender, SelectionChangedEventArgs e) {
            //选项界面
            ResourceOptionData selectData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectData == null) {
                return;
            }
            //资源界面
            TabItem selectedTab = null;
            foreach (TabItem item in TabControl_Resource.Items) {
                if (item.Header.ToString() != selectData.ResName) {
                    continue;
                }
                selectedTab = item;
                break;
            }
            if (selectedTab == null) {
                return;
            }
            //切换到指定资源界面
            TabControl_Resource.SelectedItem = selectedTab;
            //把其他资源界面operation和view断开
            ObservableCollection<ResourceOptionData> resPluginItems = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;

            //以下会切换operation的数据输出函数，可能会因为operation多线程输出而产生错误，最好在输出和替换回调两个加个锁
            lock (_lockObject) {
                foreach (ResourceOptionData item in resPluginItems) {
                    item.ResOperationInterface.SetResMessageLaunchCallback((List<ResOperationOutputParams> ouputParams) => {
                        //不在当前界面，仍然想触发该函数就先把数据存储起来
                        item.OutputParams = ouputParams;
                    });
                }
                selectData.ResViewInterface.ResetView();
                selectData.ResViewInterface.JsonToView(selectData.Data_JsonView);
                selectData.ResViewInterface.ViewOperationDataUpdate(selectData.InputParams, selectData.OutputParams);
                selectData.ResOperationInterface.SetResMessageLaunchCallback(OperationResMessageLaunch);
            }
        }

        // 处理窗口拖动
        private void Event_Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            if (e.ButtonState != MouseButtonState.Pressed) { 
                return;
            }
            DragMove();
        }

        // 最小化按钮点击事件
        private void Event_Window_MinimizeButtonClick(object sender, RoutedEventArgs e) {
            WindowState = WindowState.Minimized;
        }

        // 最大化按钮点击事件
        private void Event_Window_MaximizeButtonClick(object sender, RoutedEventArgs e) {
            if (WindowState == WindowState.Normal) {
                WindowState = WindowState.Maximized;
            }
            else {
                WindowState = WindowState.Normal;
            }
        }

        // 关闭按钮点击事件
        private void Event_Window_CloseButtonClick(object sender, RoutedEventArgs e) {
            Hide();
        }

        /// <summary>
        /// 关闭资源
        /// 输入参数存储到data中，其他参数存储到data中，清空输出参数
        /// </summary>
        /// <param name="inputParams"></param>
        private void ViewClickTurnOffRes(in List<ResViewInputParams> inputParams) {
            ResourceOptionData selectData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectData == null) {
                return;
            }
            selectData.InputParams = inputParams;
            selectData.OutputParams.Clear();
            selectData.Data_JsonView = selectData.ResViewInterface.ViewToJson();
            selectData.ResOperationInterface.TurnOffRes(selectData.InputParams);
            selectData.ResStatus = false;
        }

        /// <summary>
        /// 开启资源
        /// 输入参数存储到data中，其他参数存储到data中，清空输出参数
        /// </summary>
        /// <param name="inputParams"></param>
        private void ViewClickTurnOnRes(in List<ResViewInputParams> inputParams) {
            ResourceOptionData selectData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectData == null) {
                return;
            }
            selectData.InputParams = inputParams;
            selectData.OutputParams.Clear();
            selectData.Data_JsonView = selectData.ResViewInterface.ViewToJson();
            selectData.ResOperationInterface.TurnOnRes(selectData.InputParams);
            selectData.ResStatus = selectData.ResOperationInterface.ResStatus;
        }

        private void ViewResTemporaryEvent(string config) {
            ResourceOptionData selectData = DataGrid_Resource.SelectedItem as ResourceOptionData;
            if (selectData == null) {
                return;
            }
            selectData.ResOperationInterface.ResTemporaryEvent(config);
        }

        /// <summary>
        /// operation输出数据到界面时
        /// 输出数据存入data中，再触发更新函数
        /// </summary>
        /// <param name="outputParams"></param>
        private void OperationResMessageLaunch(List<ResOperationOutputParams> outputParams) {
            lock (_lockObject) {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                    ResourceOptionData selectData = DataGrid_Resource.SelectedItem as ResourceOptionData;
                    if (selectData == null) {
                        return;
                    }
                    selectData.OutputParams = outputParams;
                    selectData.ResViewInterface.ViewOperationDataUpdate(selectData.InputParams, outputParams);
                }));
            }
        }

        public List<ResourceOptionData> GetResDataList() {
            List<ResourceOptionData> datas = new List<ResourceOptionData>();
            ObservableCollection<ResourceOptionData> resPluginItems = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            if (resPluginItems == null) {
                return datas;
            }
            foreach (var item in resPluginItems) {
                datas.Add(item);
            }
            return datas;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else if (message.Function == "DataToJson") {
                return DataToJson();
            }
            else if (message.Function == "JsonToData" && message.Content is string params_JsonToData) {
                JsonToData(params_JsonToData);
            }
            else if (message.Function == "GetResDataList") {
                return GetResDataList();
            }
            return string.Empty;
        }

        public string DataToJson() {
            JObject json = new JObject();
            ObservableCollection<ResourceOptionData> resPluginItems = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            json["ResCount"] = resPluginItems.Count;
            for (int i = 0; i < resPluginItems.Count; i++) {
                ResourceOptionData data = resPluginItems[i];
                json[i + "_ResData"] = data.DataToJson();
            }
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            ObservableCollection<ResourceOptionData> resPluginItems = DataGrid_Resource.ItemsSource as ObservableCollection<ResourceOptionData>;
            //释放之前的资源
            for (int i = 0; i < resPluginItems.Count; i++) {
                ResourceOptionData data = resPluginItems[i];
                data.ResOperationInterface.Dispose();
            }
            resPluginItems.Clear();
            TabControl_Resource.Items.Clear();
            //新增加载的资源
            JObject json = JObject.Parse(str);
            int resCount = ((int)json["ResCount"]);
            for (int i = 0; i < resCount; i++) {
                ResourceOptionData data = new ResourceOptionData();
                data.JsonToData(json[i + "_ResData"].ToString());
                //data不完整，缺少view和operation，得补上
                data.ResViewInterface = _sendMessage?.Invoke(new CommunicationCenterMessage("GlobalResource", "PluginManager", "GetResViewInterfaceByName", data.ResType)) as ResViewPluginBase;
                data.ResOperationInterface = _sendMessage?.Invoke(new CommunicationCenterMessage("GlobalResource", "PluginManager", "GetResOperationInterfaceByName", data.ResType)) as ResOperationPluginBase;

                data.ResViewInterface.SetTurnOffResClickCallback(ViewClickTurnOffRes);
                data.ResViewInterface.SetTurnOnResClickCallback(ViewClickTurnOnRes);
                data.ResViewInterface.SetResTemporaryEventCallback(ViewResTemporaryEvent);
                //资源界面
                TabItem tabItem = new TabItem() {
                    Header = data.ResName,
                    Visibility = Visibility.Collapsed,
                    Content = data.ResViewInterface,
                };
                TabControl_Resource.Items.Add(tabItem);
                TabControl_Resource.SelectedItem = tabItem;//切换到指定选项
                //选项界面
                resPluginItems.Add(data);
                //切换到指定选项，会触发Event_DataGrid_SelectionItemChanged，给operation赋值回调函数，让界面能够有处理
                DataGrid_Resource.SelectedItem = data;
                //最后看看是否开启资源
                if (data.ResStatus) {
                    data.ResOperationInterface.TurnOnRes(data.InputParams);
                }
            }
        }

        public void SetSendMessageCallback(SendMessage cb) {
            if (cb == null) { 
                return;
            }
            _sendMessage = cb;
        }

        private string CheckUniqueItemName(string defaultName, string checkName) {
            string preName = $"{defaultName} (0)";//预先给留的不重复名
            string resultName = checkName;//本身名字不重复，那最终就还是返回输入的名字
            int maxCount = 0;
            //获取预先留的不重复名
            foreach (ResourceOptionData data in DataGrid_Resource.Items) {
                string currentStr = data.ResName;
                if (currentStr.StartsWith(defaultName)) {
                    int start = currentStr.IndexOf('(') + 1; // 找到 '(' 的位置
                    int end = currentStr.IndexOf(')'); // 找到 ')' 的位置

                    if (start <= 0 || end <= start) {
                        continue;
                    }

                    string numberStr = currentStr.Substring(start, end - start); // 提取数字字符串
                    if (!int.TryParse(numberStr, out int num)) {
                        continue; // 失败
                    }

                    maxCount = (num + 1 > maxCount) ? num + 1 : maxCount;
                    preName = $"{defaultName} ({maxCount})";
                }
            }

            //判断是否重名，重名则替换为预先留的名字
            foreach (ResourceOptionData data in DataGrid_Resource.Items) {
                if (data.ResName == checkName) {
                    resultName = preName;
                    break;
                }
            }

            return resultName;
        }


    }
}
