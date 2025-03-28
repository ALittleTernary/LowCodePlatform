using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Res_Tcp;
using LowCodePlatform.Plugin.Task_Control;
using LowCodePlatform.View;
using LowCodePlatform.View.Base;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;

namespace LowCodePlatform.Plugin.Base
{
    /// <summary>
    /// 任务插件种类
    /// </summary>
    public enum TaskPluginType { 
        /// <summary>
        /// 资源获取
        /// </summary>
        kResourceObtain = 0,
        /// <summary>
        /// 资源发布
        /// </summary>
        kResourcePublic = 1,
        /// <summary>
        /// 变量处理
        /// </summary>
        kVariableHandle = 2,
        /// <summary>
        /// 控制语句
        /// </summary>
        kControlStatement = 3,
        /// <summary>
        /// 数据处理
        /// </summary>
        kDataProcess = 4,
        /// <summary>
        /// 数据显示
        /// </summary>
        kDataDisplay = 5,
    
    }

    /// <summary>
    /// 语言枚举，用于翻译
    /// </summary>
    public enum LangaugeType
    {
        kChinese = 0,
        kEnglish = 1,
    }

    /// <summary>
    /// 总结当前节点之前的所有节点信息
    /// 实际上只用于调用CombinationArea里的SummarizeBeforeNodes
    /// </summary>
    /// <returns></returns>
    public delegate FlowNode SummarizeBeforeNodes();

    /// <summary>
    /// 插件管理器
    /// 负责插件的注册、界面数据收集
    /// </summary>
    public abstract class PluginManager : CommunicationUser, IDisposable
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        /// <summary>
        /// 总界面调用销毁时会将这个改为true,这样就能把task界面close释放
        /// </summary>
        private bool _isClosing = false;

        /// <summary>
        /// 任务类型的插件集合
        /// </summary>
        private List<(TaskPluginType, TaskViewPluginBase, TaskOperationPluginBase)> _taskPluginList = new List<(TaskPluginType, TaskViewPluginBase, TaskOperationPluginBase)>();

        /// <summary>
        /// 界面类型的插件集合,界面应当允许重复使用
        /// </summary>
        private List<SubViewPluginBase> _subViewPluginList = new List<SubViewPluginBase>();

        /// <summary>
        /// 资源类型的插件集合，资源应当允许重复使用
        /// </summary>
        private List<(ResViewPluginBase, ResOperationPluginBase)> _resPluginList = new List<(ResViewPluginBase, ResOperationPluginBase)>();

        /// <summary>
        /// 当前打开正在编辑的任务类型的插件的item
        /// 这个item存储着数据
        /// </summary>
        private CombinationArea_TreeItem _editingItem = null;

        /// <summary>
        /// 任务插件单步执行，调用算法引擎单步执行
        /// </summary>
        private RoutedEventHandler _taskSingleStepExecute = null;

        public PluginManager() {
            InitControlPlugin();
            RegisterTaskPlugin();
            RegisterResourcePlugin();
            RegisterSubDockPlugin();
        }

        private void InitControlPlugin() {
            //if
            TaskView_If taskViewIf = new TaskView_If();
            taskViewIf.SetSummarizeBeforeNodesCallback(SummarizeBeforeNodes);
            AddTaskPlugin(TaskPluginType.kControlStatement, taskViewIf, new TaskOperation_If());
            //else if
            TaskView_ElseIf taskViewElseIf = new TaskView_ElseIf();
            taskViewElseIf.SetSummarizeBeforeNodesCallback(SummarizeBeforeNodes);
            AddTaskPlugin(TaskPluginType.kControlStatement, taskViewElseIf, new TaskOperation_ElseIf());
            //for
            TaskView_For taskViewFor = new TaskView_For();
            taskViewFor.SetSummarizeBeforeNodesCallback(SummarizeBeforeNodes);
            AddTaskPlugin(TaskPluginType.kControlStatement, taskViewFor, new TaskOperation_For());
        }

        public void Dispose() {
            _isClosing = true;
            foreach (var item in _taskPluginList) {
                Window window = item.Item2 as Window;
                if (window == null) {
                    continue;
                }
                window.Close();
            }
            foreach (var item in _resPluginList) {
                ResOperationPluginBase res = item.Item2;
                if (res == null) {
                    continue;
                }
                res.Dispose();
            }
            _taskPluginList.Clear();
            _resPluginList.Clear();
        }
         
        /// <summary>
        /// 添加task插件
        /// </summary>
        /// <param name="view"></param>
        /// <param name="operation"></param>
        protected void AddTaskPlugin(TaskPluginType taskType, TaskViewPluginBase viewInterface, TaskOperationPluginBase operationInterface) {
            Window viewWindow = viewInterface as Window;
            if (viewWindow == null) { 
                return;
            }

            Array enumValues = Enum.GetValues(typeof(LangaugeType));
            foreach (var value in enumValues) {
                //判断一下名字是否一样对应
                LangaugeType langaugeType = (LangaugeType)value;
                string viewName = viewInterface.ViewUniqueName(langaugeType);
                string operationName = operationInterface.OperationUniqueName(langaugeType);
                if (viewName != operationName) {
                    return;
                }
                //判断一下名字是否重复
                foreach (var item in _taskPluginList) {
                    string currentViewTaskName = item.Item2.ViewUniqueName(langaugeType);
                    if (currentViewTaskName == viewName) {
                        //名字重复了
                        return; 
                    }
                }
            }

            //把关闭改为隐藏
            viewWindow.Closing += (s, e) => {
                if (_isClosing) {
                    return;
                }
                e.Cancel = true;
                viewWindow.Hide();
            };
            viewInterface.SetConfirmClickCallback(TaskViewClickConfirm);
            viewInterface.SetExecuteClickCallback(TaskViewClickExecute);
            viewInterface.SetLinkClickCallback(TaskViewClickLink);

            //界面点开则是中心显示
            viewWindow.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            _taskPluginList.Add((taskType, viewInterface, operationInterface));
        }

        /// <summary>
        /// 从插件管理器获取所有task插件名，选项区使用，提供给用户哪些task可以使用
        /// </summary>
        /// <returns></returns>
        public List<(TaskPluginType, string)> GetTaskNamesByLangauge(LangaugeType langauge) {
            List <(TaskPluginType, string)> result = new List<(TaskPluginType, string)>();
            foreach (var item in _taskPluginList) {
                result.Add((item.Item1, item.Item2.ViewUniqueName(langauge)));
            }
            return result;
        }

        /// <summary>
        /// 根据名字获取到task插件view的Interface类型
        /// </summary>
        /// <param name="langauge"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private TaskViewPluginBase GetTaskViewInterfaceByName(string name) {
            foreach (var item in _taskPluginList) {
                if (item.Item2.ViewUniqueName(LangaugeType.kChinese) == name || item.Item2.ViewUniqueName(LangaugeType.kEnglish) == name) {
                    return item.Item2;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据名字获取到task插件view的Window类型
        /// </summary>
        /// <param name="langauge"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private Window GetTaskViewWindowByName(string name) {
            foreach (var item in _taskPluginList) {
                if (item.Item2.ViewUniqueName(LangaugeType.kChinese) == name || item.Item2.ViewUniqueName(LangaugeType.kEnglish) == name) {
                    return item.Item2 as Window;
                }
            }
            return null;
        }

        /// <summary>
        /// 根据名字获取到task插件operation的接口类型
        /// </summary>
        /// <param name="langauge"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private TaskOperationPluginBase GetTaskOperationInterfaceByName(string name) {
            foreach (var item in _taskPluginList) {
                if (item.Item3.OperationUniqueName(LangaugeType.kChinese) == name || item.Item3.OperationUniqueName(LangaugeType.kEnglish) == name) {
                    return item.Item3.Clone();
                }
            }
            return null;
        }

        /// <summary>
        /// 根据数据显示对应的界面
        /// </summary>
        /// <param name="langauge"></param>
        /// <param name="name"></param>
        private void TaskViewShowByData(CombinationArea_TreeItem data) {
            if (data == null) {
                return;
            }
            Window taskViewWindow = GetTaskViewWindowByName(data.ItemName);
            TaskViewPluginBase taskViewInterface = GetTaskViewInterfaceByName(data.ItemName);
            TaskOperationPluginBase taskOperationInterface = GetTaskOperationInterfaceByName(data.ItemName);
            if (taskViewWindow == null || taskViewInterface == null) {
                return;
            }
            _editingItem = data;

            taskViewInterface.ResetView();
            taskViewInterface.JsonToView(data.Data_JsonView);//还原界面硬编码
            taskViewInterface.ViewOperationDataUpdate(data.Data_InputParams, data.Data_OutputParams);
            taskViewWindow.Title = data.ItemName;
            taskOperationInterface.EngineIsRunning = true;//打开界面后允许执行
            taskViewWindow.ShowDialog();//阻塞打开

            //关闭界面后不再执行
            taskOperationInterface.EngineIsRunning = false;
            _editingItem = null;
        }

        /// <summary>
        /// 打开的编辑界面点击了确定按钮，仅保存参数，不单步执行
        /// </summary>
        private void TaskViewClickConfirm(List<TaskViewInputParams> inputParams) {
            if (_editingItem == null) {
                return;
            }
            Window taskViewWindow = GetTaskViewWindowByName(_editingItem.ItemName);
            TaskViewPluginBase taskViewInterface = GetTaskViewInterfaceByName(_editingItem.ItemName);
            if (taskViewWindow == null || taskViewInterface == null) {
                return;
            }
            _editingItem.Data_InputParams = inputParams;
            _editingItem.Data_JsonView = taskViewInterface.ViewToJson();
            taskViewWindow.Hide();//hide必须放最后，不然hide后控制器立马转到ShowDialog处执行
        }

        /// <summary>
        /// 打开的编辑界面点击了执行按钮，保存参数，单步执行
        /// </summary>
        private void TaskViewClickExecute(List<TaskViewInputParams> inputParams) {
            if (_editingItem == null) {
                return;
            }
            Window taskViewWindow = GetTaskViewWindowByName(_editingItem.ItemName);
            TaskViewPluginBase taskViewInterface = GetTaskViewInterfaceByName(_editingItem.ItemName);
            if (taskViewWindow == null || taskViewInterface == null) {
                return;
            }

            _editingItem.Data_InputParams = inputParams;
            _editingItem.Data_JsonView = taskViewInterface.ViewToJson();

            //RoutedEventHandler需要两个输入参数，但是其实这里是调用的任务引擎，其实就不需要输入什么东西，输入了也不会使用
            _taskSingleStepExecute?.Invoke(null, null);
        }

        /// <summary>
        /// 打开的编辑界面点击了链接按钮，打开链接选择界面
        /// </summary>
        /// <param name="linkType"></param>
        /// <returns></returns>
        private string TaskViewClickLink(LinkDataType linkType) {
            FlowNode flowData = _sendMessage?.Invoke(new CommunicationCenterMessage("PluginManager", "CombinationArea", "SummarizeBeforeNodes")) as FlowNode;
            if (flowData == null) {
                return string.Empty;
            }
            LinkSelectView selectView = new LinkSelectView() {
                LinkDataType = linkType,
            };
            if (linkType == LinkDataType.kView && _editingItem != null) {
                //获取当前任务插件的类型
                string taskName = _editingItem.ItemName;
                selectView.InitSubOption(taskName, _subViewPluginList);
            }
            else if (linkType == LinkDataType.kResource) {
                //获取当前任务插件的类型
                string taskName = _editingItem.ItemName;
                //发个信号给全局资源判断当前有实例化了多少个资源，才能允许链接
                List<ResourceOptionData> datas = _sendMessage?.Invoke(new CommunicationCenterMessage("PluginManager", "GlobalResource", "GetResDataList")) as List<ResourceOptionData>;
                selectView.InitResOption(taskName, datas);
            }
            else {
                selectView.InitTaskOption(flowData);
            }

            //需要把当前流程之前的所有内容获取
            selectView.ShowDialog();
            return selectView.LinkDataText;
        }

        /// <summary>
        /// 单任务打开界面时候的执行按钮
        /// </summary>
        /// <param name="handler"></param>
        public void SetTaskSingleStepExecuteCallback(RoutedEventHandler handler) {
            if (handler == null) {
                return;
            }
            _taskSingleStepExecute = handler;
        }

        public FlowNode SummarizeBeforeNodes() {
            FlowNode flowData = _sendMessage?.Invoke(new CommunicationCenterMessage("PluginManager", "CombinationArea", "SummarizeBeforeNodes")) as FlowNode;
            return flowData;
        }

        /// <summary>
        /// 注册界面插件
        /// </summary>
        /// <param name="viewInterface"></param>
        protected void AddSubViewPlugin(SubViewPluginBase viewInterface) {
            if (viewInterface == null) { 
                return;
            }
            Array enumValues = Enum.GetValues(typeof(LangaugeType));
            foreach (var value in enumValues) {
                //判断一下名字是否一样对应
                LangaugeType langaugeType = (LangaugeType)value;
                string viewName = viewInterface.UniqueName[langaugeType];
                if (viewName == null || viewName == string.Empty) {
                    return;
                }
                //判断一下名字是否重复
                foreach (var item in _subViewPluginList) {
                    string currentViewTaskName = item.UniqueName[langaugeType];
                    if (currentViewTaskName == viewName) {
                        //名字重复了
                        return;
                    }
                }
            }
            _subViewPluginList.Add(viewInterface);
        }

        public List<UserControl> GetSubViewUserControlList() {
            List<UserControl> userControls = new List<UserControl>();
            foreach (var item in _subViewPluginList) {
                UserControl userControl = item as UserControl;
                if (userControl == null) { 
                    continue;
                }
                userControls.Add(userControl);
            }
            return userControls;
        }

        /// <summary>
        /// 获取所有注册的界面插件
        /// </summary>
        /// <returns></returns>
        public List<SubViewPluginBase> GetSubViewInterfaceList() { 
            return _subViewPluginList;
        }

        /// <summary>
        /// 注册资源插件
        /// </summary>
        /// <param name="viewInterface"></param>
        /// <param name="operationInterface"></param>
        protected void AddResPlugin(ResViewPluginBase viewInterface, ResOperationPluginBase operationInterface) {
            UserControl viewUserControl = viewInterface as UserControl;
            if (viewUserControl == null) {
                return;
            }

            Array enumValues = Enum.GetValues(typeof(LangaugeType));
            foreach (var value in enumValues) {
                //判断一下名字是否一样对应
                LangaugeType langaugeType = (LangaugeType)value;
                string viewName = viewInterface.ViewUniqueName(langaugeType);
                string operationName = operationInterface.OperationUniqueName(langaugeType);
                if (viewName != operationName) {
                    return;
                }
                //判断一下名字是否重复
                foreach (var item in _resPluginList) {
                    string currentViewTaskName = item.Item1.ViewUniqueName(langaugeType);
                    if (currentViewTaskName == viewName) {
                        //名字重复了
                        return;
                    }
                }
            }

            _resPluginList.Add((viewInterface, operationInterface));
        }

        /// <summary>
        /// 根据名字获取res插件view的UserControl类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private UserControl GetResViewUserControlByName(string name) {
            UserControl resViewUserControl = null;
            foreach (var item in _resPluginList) {
                if (item.Item1.ViewUniqueName(LangaugeType.kChinese) == name || item.Item1.ViewUniqueName(LangaugeType.kEnglish) == name) {
                    resViewUserControl = item.Item1 as UserControl;
                    break;
                }
            }
            return resViewUserControl;
        }

        /// <summary>
        /// 根据名字获取res插件view的Interface类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ResViewPluginBase GetResViewInterfaceByName(string name) {
            ResViewPluginBase resViewInterface = null;
            foreach (var item in _resPluginList) {
                if (item.Item1.ViewUniqueName(LangaugeType.kChinese) == name || item.Item1.ViewUniqueName(LangaugeType.kEnglish) == name) {
                    resViewInterface = item.Item1;
                    break;
                }
            }
            return resViewInterface;
        }

        /// <summary>
        /// 根据名字获取res插件operation的Interface类型
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private ResOperationPluginBase GetResOperationInterfaceByName(string name) {
            ResOperationPluginBase resOperationInterface = null;
            foreach (var item in _resPluginList) {
                if (item.Item1.ViewUniqueName(LangaugeType.kChinese) == name || item.Item1.ViewUniqueName(LangaugeType.kEnglish) == name) {
                    resOperationInterface = item.Item2;
                    break;
                }
            }
            return resOperationInterface.Clone();
        }

        /// <summary>
        /// 从插件管理器获取所有res插件名，全局资源使用，提供给用户哪些res可以创建
        /// </summary>
        /// <param name="langauge"></param>
        /// <returns></returns>
        public List<string> GetResPluginNameList(LangaugeType langauge) {
            List<string> result = new List<string>();
            foreach (var item in _resPluginList) {
                result.Add(item.Item1.ViewUniqueName(langauge));
            }
            return result;
        }

        /// <summary>
        /// 注册任务类型的插件
        /// </summary>
        protected abstract void RegisterTaskPlugin();

        /// <summary>
        /// 注册资源类型的插件
        /// </summary>
        protected abstract void RegisterResourcePlugin();

        /// <summary>
        /// 注册子dock窗口的插件
        /// </summary>
        protected abstract void RegisterSubDockPlugin();

        public void SetSendMessageCallback(SendMessage cb) {
            if (cb == null) { 
                return ;
            };
            _sendMessage = cb;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return null;
            }
            else if (message.Function == "TaskViewShowByData" && message.Content is CombinationArea_TreeItem params_ShowItemWindow) {
                TaskViewShowByData(params_ShowItemWindow);
            }
            else if (message.Function == "GetTaskOperationInterfaceByName" && message.Content is string params_GetTaskOperationInterface) {
                return GetTaskOperationInterfaceByName(params_GetTaskOperationInterface);
            }
            else if (message.Function == "GetTaskViewInterfaceByName" && message.Content is string params_GetTaskViewInterface) {
                return GetTaskViewWindowByName(params_GetTaskViewInterface);
            }
            else if (message.Function == "GetResViewInterfaceByName" && message.Content is string params_GetResPluginViewByName) {
                return GetResViewInterfaceByName(params_GetResPluginViewByName);
            }
            else if (message.Function == "GetResOperationInterfaceByName" && message.Content is string params_GetResOperationInterfaceByName) {
                return GetResOperationInterfaceByName(params_GetResOperationInterfaceByName);
            }
            else if (message.Function == "DataToJson") {
                return DataToJson();
            }
            else if (message.Function == "JsonToData" && message.Content is string params_JsonToData) {
                JsonToData(params_JsonToData);
            }
            else {

            }
            return string.Empty;
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["SubViewCount"] = _subViewPluginList.Count;
            for (int i = 0; i < _subViewPluginList.Count; i++) {
                json[i + "_Name"] = _subViewPluginList[i].UniqueName[LangaugeType.kChinese];
                json[i + "_SubView"] = _subViewPluginList[i].ViewToJson();
            }
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return; 
            }
            JObject json = JObject.Parse(str);
            int count = (int)json["SubViewCount"];
            if (count != _subViewPluginList.Count) {
                return;
            }
            for (int i = 0; i < _subViewPluginList.Count; i++) {
                string name = json[i + "_Name"].ToString();
                if (name != _subViewPluginList[i].UniqueName[LangaugeType.kChinese]) { 
                    return;
                }
                 _subViewPluginList[i].JsonToView(json[i + "_SubView"].ToString());
            }
        }


    }
}
