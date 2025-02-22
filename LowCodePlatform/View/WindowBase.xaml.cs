using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using LowCodePlatform.Engine;
using LowCodePlatform.Plugin;
using LowCodePlatform.Plugin.Base;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Xml;
using static LowCodePlatform.Engine.CommunicationCenter;

namespace LowCodePlatform.View
{
    /// <summary>
    /// WindowBase.xaml 的交互逻辑
    /// </summary>
    public partial class WindowBase : Window, CommunicationUser {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        /// <summary>
        /// 子界面，工程区
        /// </summary>
        private ProjectArea _projectArea = null;

        /// <summary>
        /// 子界面，选项区
        /// </summary>
        private OptionArea _optionArea = null;

        /// <summary>
        /// 子界面，组合区
        /// </summary>
        private CombinationArea _combinationArea = null;

        /// <summary>
        /// 子界面，日志区
        /// </summary>
        private LogArea _logArea = null;

        /// <summary>
        /// 子界面，交互区
        /// </summary>
        private InteractiveArea _interactiveArea = null;

        /// <summary>
        /// 插件管理器，双击组合区的item打开编辑界面
        /// </summary>
        private PluginManager _pluginManager = null;

        /// <summary>
        /// 全局资源
        /// </summary>
        private GlobalResource _globalResource = null;

        /// <summary>
        /// 界面插件加载完成后，把加载完成的布局设置为默认布局，暂时存储为一个类成员变量没有使用一个文件去处理
        /// </summary>
        private string _defaultLayout = string.Empty;

        /// <summary>
        /// 内部小群，提供给不同区域进行信息交互
        /// </summary>
        CommunicationCenter _communicationCenter = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public WindowBase() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            //添加子界面到主界面
            Action<UserControl> action_AddSubViewToMainWindow = (UserControl subViewUserControl) => {
                SubViewPluginBase subViewInterface = subViewUserControl as SubViewPluginBase;
                if (subViewUserControl == null || subViewInterface == null) {
                    return;
                }
                string viewName = subViewInterface.UniqueName[LangaugeType.kChinese];
                LayoutAnchorable anchorable = null;
                foreach (var item in LayoutAnchorablePane_DisplayArea.Children) {
                    if (item.ContentId != viewName || item.Title != viewName) {
                        continue;
                    }
                    anchorable = item;
                    item.Content = subViewUserControl;
                }
                //如果没有就添加
                if (anchorable == null) {
                    anchorable = new LayoutAnchorable() {
                        Title = viewName,
                        Content = subViewUserControl,
                        ContentId = viewName,
                    };
                    LayoutAnchorablePane_DisplayArea.Children.Add(anchorable);
                }

                MenuItem menuItem = new MenuItem() {
                    Header = viewName,
                };
                MenuItem_SubView.Items.Add(menuItem);
                anchorable.Hide();
                menuItem.Click += (s, e) => {
                    var pluginViews = DockManager.Layout.Descendents().OfType<LayoutAnchorable>();
                    LayoutAnchorable view = null;
                    foreach (var pluginView in pluginViews) {
                        if (pluginView.ContentId != viewName) {
                            continue;
                        }
                        view = pluginView;
                    }
                    if (view == null) {
                        return;
                    }
                    else if (view.IsHidden) {
                        view.Show();
                    }
                    else if (view.IsVisible) {
                        view.IsActive = true;
                    }
                    else {
                        view.AddToLayout(DockManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
                    }
                };
            };

            //记录默认布局
            Func<string> func_RecordDefaultLayout = () => {
                JObject json = new JObject();
                // 保存DockingManager布局
                using (MemoryStream stream = new MemoryStream()) {
                    XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockManager);
                    serializer.Serialize(stream);
                    stream.Position = 0;

                    // 从流中把数据转为字符串
                    using (StreamReader reader = new StreamReader(stream)) {
                        string xml = reader.ReadToEnd();
                        json["DockingManager"] = xml;
                    }
                }

                json["WindowWidth"] = Width;
                json["WindowHeight"] = Height;
                json["WindowState"] = WindowState.ToString();
                return json.ToString();
            }; 

            Title = "LowCodePlatform";

            string currentDirectory = Directory.GetCurrentDirectory();
            Image_Open.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\打开.png"));
            Image_Save.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\保存.png"));
            Image_RunOnce.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\单步执行.png"));
            Image_RunLoop.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\循环执行.png"));
            Image_RunStop.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\停止.png"));
            Image_GlobalVariable.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\全局变量.png"));
            Image_GlobalResource.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\全局资源.png"));

            _communicationCenter = new CommunicationCenter();

            _logArea = new LogArea();
            Grid_LogArea.Children.Add(_logArea);
            _communicationCenter.Register("LogArea", _logArea);

            //插件管理器中管理着task、view、res三种插件
            _pluginManager = new PluginRegister();
            _pluginManager.SetTaskSingleStepExecuteCallback(Event_Button_NodeRunOnce);
            _communicationCenter.Register("PluginManager", _pluginManager);

            _projectArea = new ProjectArea();
            Grid_ProjectArea.Children.Add(_projectArea);
            _communicationCenter.Register("ProjectArea", _projectArea);

            _optionArea = new OptionArea();
            _optionArea.InitOptionAreaOptions(_pluginManager.GetTaskNamesByLangauge(LangaugeType.kChinese));
            Grid_OptionArea.Children.Add(_optionArea);
            _communicationCenter.Register("OptionArea", _optionArea);

            //组合区管理task实例
            _combinationArea = new CombinationArea();
            _combinationArea.SetClickRunOnceCallback(Event_Button_FlowRunOnce);
            _combinationArea.SetClickRunLoopCallback(Event_Button_FlowRunLoop);
            _combinationArea.SetClickRunStopCallback(Event_Button_FlowRunStop);
            Grid_CombinationArea.Children.Add(_combinationArea);
            _communicationCenter.Register("CombinationArea", _combinationArea);

            _interactiveArea = new InteractiveArea();
            Grid_InteractiveArea.Children.Add(_interactiveArea);
            _communicationCenter.Register("InteractiveArea", _interactiveArea);

            //全局变量管理res实例
            _globalResource = new GlobalResource();
            _globalResource.InitGlobalResourceOptions(_pluginManager.GetResPluginNameList(LangaugeType.kChinese));
            _communicationCenter.Register("GlobalResource", _globalResource);

            //主界面管理subView实例
            foreach (var subView in _pluginManager.GetSubViewUserControlList()) {
                action_AddSubViewToMainWindow(subView);
            }
            foreach (var subIntf in _pluginManager.GetSubViewInterfaceList()) {
                subIntf.SetViewEditStatus(true);
            }
            //界面插件加载完毕后，把当前界面布局存储起来作为默认布局
            _defaultLayout = func_RecordDefaultLayout();
        }

        private void InitEvent() {
            //显示隐藏起来的dock界面
            Action<string> action_ShowSubDockView = (string ContentId) => {
                if (ContentId == null) {
                    return;
                }
                var view = DockManager.Layout.Descendents().OfType<LayoutAnchorable>().Single(a => a.ContentId == ContentId);
                if (view == null) {
                    return;
                }
                else if (view.IsHidden) {
                    view.Show();
                }
                else if (view.IsVisible) {
                    view.IsActive = true;
                }
                else {
                    view.AddToLayout(DockManager, AnchorableShowStrategy.Bottom | AnchorableShowStrategy.Most);
                }
            };

            MenuItem_Open.Click += Event_Button_OpenClick;
            Button_Open.Click += Event_Button_OpenClick;
            MenuItem_Open.Click += Event_Button_SaveClick;
            Button_Save.Click += Event_Button_SaveClick;
            MenuItem_SaveAs.Click += Event_Button_SaveAsClick;

            MenuItem_SubViewEdit.Click += (sender, e) => {
                MenuItem_SubViewEdit.IsChecked = !MenuItem_SubViewEdit.IsChecked;
                foreach (var interf in _pluginManager.GetSubViewInterfaceList()) {
                    interf.SetViewEditStatus(MenuItem_SubViewEdit.IsChecked);
                }
            };
            MenuItem_RestoreDefaultView.Click += Event_Button_RestoreDefaultViewClick;
            MenuItem_ProjectArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_ProjectArea.ContentId);
            };
            MenuItem_OptionArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_OptionArea.ContentId);
            };
            MenuItem_CombinationArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_CombinationArea.ContentId);
            };
            MenuItem_DisplayArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_DisplayArea.ContentId);
            };
            MenuItem_LogArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_LogArea.ContentId);
            };
            MenuItem_InteractiveArea.Click += (sender, e) => {
                action_ShowSubDockView(LayoutAnchorable_InteractiveArea.ContentId);
            };
            Button_RunOnce.Click += Event_Button_ProcessRunOnce;
            Button_RunLoop.Click += Event_Button_ProcessRunLoop;
            Button_RunStop.Click += Event_Button_ProcessRunStop;
            Button_GlobalResource.Click += Event_Button_GlobalResourceClick;

            //主界面关闭事件，释放所有资源，如果不释放仍然在运行的线程、隐藏的界面会导致程序并不会关闭
            Closing += Event_Button_Closeing;
        }

        /// <summary>
        /// 主界面关闭
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Closeing(object sender, CancelEventArgs e) {
            try {
                _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "Dispose"));
                _globalResource.Close();
                _pluginManager.Dispose();
            }
            catch (Exception ex) {
                string msg = ex.Message;
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_SaveClick(object sender, RoutedEventArgs e) {
            string fileSavePath = _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "GetSaveFilePath")) as string;

            if (fileSavePath == string.Empty) {
                // 创建 SaveFileDialog 实例
                SaveFileDialog saveFileDialog = new SaveFileDialog {
                    Filter = "Json files (*.json)|*.json|All files (*.*)|*.*", // 设置文件类型过滤器
                    Title = "Save a Json File" // 设置对话框标题
                };
                saveFileDialog.ShowDialog();
                fileSavePath = saveFileDialog.FileName;
            }

            //开始总结主界面数据
            JObject json = new JObject();
            // 保存DockingManager布局
            using (MemoryStream stream = new MemoryStream()) {
                XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockManager);
                serializer.Serialize(stream);
                stream.Position = 0;

                // 从流中把数据转为字符串
                using (StreamReader reader = new StreamReader(stream)) {
                    string xml = reader.ReadToEnd();
                    json["DockingManager"] = xml;
                }
            }

            json["WindowWidth"] = Width;
            json["WindowHeight"] = Height;
            json["WindowState"] = WindowState.ToString();
            json["MenuItem_SubViewEdit"] = MenuItem_SubViewEdit.IsChecked;

            //开始总结所有的子界面数据
            List<(string, object)> dataList = _communicationCenter.BroadcastMessage(new CommunicationCenterMessage("WindowBase", "", "DataToJson"));
            for (int i = 0; i < dataList.Count; i++) {
                string member = dataList[i].Item1;
                string content = dataList[i].Item2 as string;
                json[i.ToString() + "_SubMenberName"] = member;
                json[i.ToString() + "_SubMenberContent"] = content;
            }

            //需要存储的数据就发送给存储模块
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "SavePartData", json.ToString()));
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "SaveToJsonFile", fileSavePath));
        }

        /// <summary>
        /// 另存为
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_SaveAsClick(object sender, RoutedEventArgs e) {
            // 创建 SaveFileDialog 实例
            SaveFileDialog saveFileDialog = new SaveFileDialog {
                Filter = "Json files (*.json)|*.json|All files (*.*)|*.*", // 设置文件类型过滤器
                Title = "Save a Json File" // 设置对话框标题
            };
            saveFileDialog.ShowDialog();
            string fileSavePath = saveFileDialog.FileName;
            if (fileSavePath == string.Empty) {
                return;
            }

            JObject json = new JObject();
            // 保存DockingManager布局
            using (MemoryStream stream = new MemoryStream()) {
                XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockManager);
                serializer.Serialize(stream);
                stream.Position = 0;

                // 将序列化的布局数据作为Base64字符串保存
                byte[] layoutData = stream.ToArray();
                json["DockingManager"] = Convert.ToBase64String(layoutData);
            }

            json["WindowWidth"] = Width;
            json["WindowHeight"] = Height;
            json["WindowState"] = WindowState.ToString();
            json["MenuItem_SubViewEdit"] = MenuItem_SubViewEdit.IsChecked;

            //开始总结所有的子界面数据
            List<(string, object)> dataList = _communicationCenter.BroadcastMessage(new CommunicationCenterMessage("WindowBase", "", "DataToJson"));
            for (int i = 0; i < dataList.Count; i++) {
                string member = dataList[i].Item1;
                string content = dataList[i].Item2 as string;
                json[i.ToString() + "_SubMenberName"] = member;
                json[i.ToString() + "_SubMenberContent"] = content;
            }

            //需要存储的数据就发送给存储模块
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "SavePartData", json.ToString()));
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "SaveToJsonFile", fileSavePath));
        }

        /// <summary>
        /// 打开
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_OpenClick(object sender, RoutedEventArgs e) {
            // 创建 OpenFileDialog 实例
            OpenFileDialog openFileDialog = new OpenFileDialog {
                Filter = "Json files (*.json)|*.json|All files (*.*)|*.*", // 设置文件类型过滤器
                Title = "Select a Json File" // 设置对话框标题
            };
            openFileDialog.ShowDialog();

            //从存储模块读到数据
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "ParseFromJsonFile", openFileDialog.FileName));
            string reply = _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AppDataSerializer", "ParsePartData")) as string;
            if (reply == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(reply);

            // 还原DockingManager布局
            string str_DockingManager = json["DockingManager"].ToString();
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(str_DockingManager))) {
                XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockManager);
                serializer.Deserialize(stream);
            }

            Width = ((double)json["WindowWidth"]);
            Height = ((double)json["WindowHeight"]);
            if (Enum.TryParse(json["WindowState"].ToString(), out WindowState windowState)) {
                WindowState = windowState;
            }
            MenuItem_SubViewEdit.IsChecked = ((bool)json["MenuItem_SubViewEdit"]);
            foreach (var interf in _pluginManager.GetSubViewInterfaceList()) {
                interf.SetViewEditStatus(MenuItem_SubViewEdit.IsChecked);
            }

            //还原子界面数据
            int i = 0;
            while (json != null) {
                if (!json.ContainsKey(i.ToString() + "_SubMenberName")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_SubMenberContent")) {
                    break;
                }

                string menber = json[i.ToString() + "_SubMenberName"].ToString();
                string content = json[i.ToString() + "_SubMenberContent"].ToString();

                _communicationCenter.PublishMessage(new CommunicationCenterMessage("WindowBase", menber, "JsonToData", content));
                i++;
            }
        }

        /// <summary>
        /// 恢复默认视图
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_RestoreDefaultViewClick(object sender, RoutedEventArgs e) {
            JObject json = JObject.Parse(_defaultLayout);

            // 还原DockingManager布局
            string str_DockingManager = json["DockingManager"].ToString();
            using (MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(str_DockingManager))) {
                XmlLayoutSerializer serializer = new XmlLayoutSerializer(DockManager);
                serializer.Deserialize(stream);
            }

            Width = ((double)json["WindowWidth"]);
            Height = ((double)json["WindowHeight"]);
            if (Enum.TryParse(json["WindowState"].ToString(), out WindowState windowState)) {
                WindowState = windowState;
            }
        }

        /// <summary>
        /// 全局资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_GlobalResourceClick(object sender, RoutedEventArgs e) {
            _globalResource.ShowDialog();
        }

        /// <summary>
        /// 整个工程单次运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_ProcessRunOnce(object sender, RoutedEventArgs e) {
            List<FlowNode> processData = _combinationArea.SummarizeProcessNodes();
            if (processData == null) { 
                return;
            }
            List<SubViewPluginBase> subViewList = _pluginManager.GetSubViewInterfaceList();
            List<ResourceOptionData> resDataList = _globalResource.GetResDataList();
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "ProcessRunOnce", (processData, subViewList, resDataList)));
        }

        /// <summary>
        /// 整个工程循环运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_ProcessRunLoop(object sender, RoutedEventArgs e) {
            List<FlowNode> processData = _combinationArea.SummarizeProcessNodes();
            if (processData == null) {
                return;
            }
            List<SubViewPluginBase> subViewList = _pluginManager.GetSubViewInterfaceList();
            List<ResourceOptionData> resDataList = _globalResource.GetResDataList();
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "ProcessRunLoop", (processData, subViewList, resDataList)));
        }

        /// <summary>
        /// 整个工程停止运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_ProcessRunStop(object sender, RoutedEventArgs e) {
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "ProcessRunStop"));
        }

        /// <summary>
        /// 当前流程单次运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_FlowRunOnce(object sender, RoutedEventArgs e) {
            List<FlowNode> processData = _combinationArea.SummarizeProcessNodes();
            if (processData == null) {
                return;
            }
            FlowNode flowData = _combinationArea.SummarizeFlowNodes();
            if (flowData == null) { 
                return;
            }
            List<SubViewPluginBase> subViewList = _pluginManager.GetSubViewInterfaceList();
            List<ResourceOptionData> resDataList = _globalResource.GetResDataList();
            //流程执行却要把整个工程信息也发过去，是因为要建立链接字典，只有所有的信息才能建立链接字典
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "FlowRunOnce", (flowData, processData, subViewList, resDataList)));
        }

        /// <summary>
        /// 当前流程循环运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_FlowRunLoop(object sender, RoutedEventArgs e) {
            List<FlowNode> processData = _combinationArea.SummarizeProcessNodes();
            if (processData == null) {
                return;
            }
            FlowNode flowData = _combinationArea.SummarizeFlowNodes();
            if (flowData == null) {
                return;
            }
            List<SubViewPluginBase> subViewList = _pluginManager.GetSubViewInterfaceList();
            List<ResourceOptionData> resDataList = _globalResource.GetResDataList();
            //流程执行却要把整个工程信息也发过去，是因为要建立链接字典，只有所有的信息才能建立链接字典
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "FlowRunLoop", (flowData, processData, subViewList, resDataList)));
        }

        /// <summary>
        /// 当前流程停止运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_FlowRunStop(object sender, RoutedEventArgs e) {
            FlowNode flowData = _combinationArea.SummarizeFlowNodes();
            if (flowData == null) {
                return;
            }
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "FlowRunStop", flowData.Name));
        }

        /// <summary>
        /// 当前节点单次运行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_NodeRunOnce(object sender, RoutedEventArgs e) {
            List<FlowNode> processData = _combinationArea.SummarizeProcessNodes();
            if (processData == null) {
                return;
            }
            TaskNode taskData = _combinationArea.SummarizeSingleNode(); 
            if (taskData == null) {
                return;
            }
            List<SubViewPluginBase> subViewList = _pluginManager.GetSubViewInterfaceList();
            List<ResourceOptionData> resDataList = _globalResource.GetResDataList();
            //单步执行却要把整个工程信息也发过去，是因为要建立链接字典，只有所有的信息才能建立链接字典
            _sendMessage?.Invoke(new CommunicationCenterMessage("WindowBase", "AlgoEngine", "NodeRunOnce", (taskData, processData, subViewList, resDataList)));
        }

        public void SetSendMessageCallback(SendMessage cb) {
            if (cb == null) {
                return;
            }
            _sendMessage = cb;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else {

            }
            
            return string.Empty;
        }

        public string DataToJson() {
            return string.Empty;
        }

        public void JsonToData(string str) {

        }
    }
}
