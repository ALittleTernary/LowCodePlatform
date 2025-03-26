using Antlr.Runtime.Tree;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.View;
using LowCodePlatform.View.Base;
using OpenCvSharp;
using Serilog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using static OpenCvSharp.ML.SVM;

namespace LowCodePlatform.Engine
{

    /// <summary>
    /// 测试引擎运行状态
    /// </summary>
    public enum TaskEngineStatus {
        kNone = 0,
        /// <summary>
        /// 当前是在工程运行中
        /// </summary>
        kProcessRunning = 1,
        /// <summary>
        /// 当前是在流程运行中
        /// </summary>
        kFlowRunning = 2,
        /// <summary>
        /// 当前是在单节点运行中
        /// </summary>
        kNodeRunning = 3,
        /// <summary>
        /// 当前是在停止状态
        /// </summary>
        kStop = 4,

    }

    /// <summary>
    /// 流程节点
    /// </summary>
    public class FlowNode {
        /// <summary>
        /// 名字是整个工程里唯一的
        /// 工程名+item名
        /// </summary>
        public string Name { set; get; } = "";

        /// <summary>
        /// 流程节点集合
        /// </summary>
        public List<TaskNode> Children { set; get; } = new List<TaskNode>();

        /// <summary>
        /// 当前流程是否允许运行
        /// 由流程的开启/禁止按钮决定
        /// </summary>
        public bool AllowRun { set; get; } = true;
    }

    /// <summary>
    /// 运行节点
    /// </summary>
    public class TaskNode
    {
        /// <summary>
        /// 流程名
        /// </summary>
        public string FlowName { set; get; } = "";

        /// <summary>
        /// 名字是整个流程里唯一的
        /// item名
        /// </summary>
        public string ItemName { set; get; } = "";

        /// <summary>
        /// 父节点
        /// </summary>
        public TaskNode Parent { set; get; } = null;

        /// <summary>
        /// 前一节点
        /// </summary>
        public TaskNode Previous { set; get; } = null;

        /// <summary>
        /// 后一节点
        /// </summary>
        public TaskNode Next { set; get; } = null ;

        /// <summary>
        /// 子节点集合
        /// </summary>
        public List<TaskNode> Children { set; get; } = new List<TaskNode>();

        /// <summary>
        /// 当前节点运行状态，对应于界面中item显示的状态切换
        /// </summary>
        public TaskNodeStatus NodeStatus { set; get; } = TaskNodeStatus.kNone;

        /// <summary>
        /// 当前节点运行时间
        /// </summary>
        public string Time { set; get; } = string.Empty;

        /// <summary>
        /// 引擎状态
        /// </summary>
        private TaskEngineStatus _engineStatus = TaskEngineStatus.kNone;
        public TaskEngineStatus EngineStatus
        {
            set {
                _engineStatus = value;
                if (Children == null) {
                    return;
                }
                //子节点均更新引擎状态
                foreach (var child in Children) {
                    child.EngineStatus = value;
                }
            }
            get {
                return _engineStatus;
            }
        }

        /// <summary>
        /// 当前节点是否允许运行
        /// 由界面item的开启/禁止按钮决定
        /// </summary>
        private bool _allowRun = true;
        public bool AllowRun
        {
            set {
                _allowRun = value;
                //如果value是真的就不用管
                if (value) {
                    return;
                }
                //否则把所有子项置为false禁止运行
                foreach (var child in Children) {
                    child.AllowRun = false;
                }
            }
            get {
                return _allowRun;
            }
        }

        /// <summary>
        /// 当前节点的操作类型，common/if/while...
        /// </summary>
        public ItemOperationType OperationType { set; get; } = ItemOperationType.kNone;

        /// <summary>
        /// 输入参数集合
        /// </summary>
        public List<TaskViewInputParams> Data_InputParams { set; get; } = new List<TaskViewInputParams>();

        /// <summary>
        /// 输出参数集合
        /// </summary>
        public List<TaskOperationOutputParams> Data_OutputParams { set; get; } = new List<TaskOperationOutputParams>();

        /// <summary>
        /// 这个节点实际上的运行类,这个运行类界面上每有一个item就有一个运行类，深拷贝来的
        /// 任务引擎运行会往里送参数
        /// </summary>
        public TaskOperationPluginBase TaskOperation { set; get; } = null;

        /// <summary>
        /// 这个界面对应的编辑界面，同名的item对应一个编辑界面类
        /// 任务引擎运行会对其更新参数
        /// </summary>
        public TaskViewPluginBase TaskView { set; get; } = null;

        /// <summary>
        /// 每个节点都实际对应界面上的item
        /// 任务引擎会对其更新参数
        /// </summary>
        public CombinationArea_TreeItem ItemView { set; get; } = null ;
    }

    public class ThreadData
    {
        public ThreadData() {

        }

        /// <summary>
        /// 线程名字
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 线程运行状态
        /// </summary>
        public bool Status { get; set; } = true;

        /// <summary>
        /// 线程本身
        /// </summary>
        public Task Task { get; set; } = null;

    }

    public class AlgoEngine : CommunicationUser, IDisposable
    {
         private SendMessage _sendMessage = null;

        /// <summary>
        /// 任务树根节点
        /// </summary>
        TaskNode _taskTreeRoot = new TaskNode();

        /// <summary>
        /// 输出数据汇总字典，通过名字找到链接节点，再找到输出数据
        /// </summary>
        ConcurrentDictionary<string, TaskNode> _linkDataDictinary = new ConcurrentDictionary<string, TaskNode>();

        /// <summary>
        /// 子界面汇总字典，通过名字找到子界面
        /// </summary>
        ConcurrentDictionary<string, SubViewPluginBase> _subViewDictinary = new ConcurrentDictionary<string, SubViewPluginBase>();

        /// <summary>
        /// 全局资源汇总字典，通过名字找到全局资源
        /// </summary>
        ConcurrentDictionary<string, ResOperationPluginBase> _globalResDictinary = new ConcurrentDictionary<string, ResOperationPluginBase>();

        // 线程名字，对应线程状态(并不需要把线程本身加入字典，拿到线程本身了也做不了什么事)
        private ConcurrentDictionary<string, bool> _threadDictinary = new ConcurrentDictionary<string, bool>();

        public AlgoEngine() { 

        }

        public void Dispose() {
            _linkDataDictinary.Clear();
            _subViewDictinary.Clear();
            _globalResDictinary.Clear();
        }

        /// <summary>
        /// 整个工程运行一次
        /// </summary>
        /// <returns></returns>
        public void ProcessRunOnce(List<FlowNode> processData) {
            //如果字典不为空，就说明有流程或者单个任务没执行完毕，别开启新工程
            if (!_threadDictinary.IsEmpty) { 
                return;
            }

            foreach (var item in processData) {
                Task task = FlowRunOnce(item);
            }
        }

        /// <summary>
        /// 整个工程循环运行
        /// </summary>
        /// <returns></returns>
        public void ProcessRunLoop(List<FlowNode> processData) {
            //如果字典不为空，就说明有流程或者单个任务没执行完毕，别开启新工程
            if (!_threadDictinary.IsEmpty) {
                return;
            }

            foreach (var item in processData) {
                Task task = FlowRunLoop(item);
            }
        }

        /// <summary>
        /// 整个工程停止运行
        /// 不区分工程、流程、一次、循环运行，除了资源管理器外所有行动停止，实际上算法引擎也管不了资源管理器
        /// </summary>
        /// <returns></returns>
        public bool ProcessRunStop() {
            foreach (var item in _threadDictinary) {
                _threadDictinary.TryUpdate(item.Key, false, true);
            }
            return true;
        }

        /// <summary>
        /// 单个流程运行一次
        /// </summary>
        /// <returns></returns>
        public async Task FlowRunOnce(FlowNode flowData) {
            //单个流程运行前将所有节点状态置空
            foreach (var item in flowData.Children) {
                InitializeTreeNodeStatus(item);
            }

            Task task = new Task(() => {
                try {
                    foreach (var child in flowData.Children) {
                        //点击了暂停
                        bool state_NoFindValue = _threadDictinary.TryGetValue(flowData.Name, out bool state_Pause);
                        if (state_NoFindValue == false || state_Pause == false) { 
                            break;
                        }
                        bool runStatus = SwitchToCorrectNodeOperation(child);

                        //选择到正确节点执行没成功，跳出整个循环，停止执行
                        if (!runStatus) {
                            break;
                        }
                    }
                }
                finally {
                    //线程字典移除当前线程，因为当前线程结束了
                    _threadDictinary.TryRemove(flowData.Name, out _);
                }
            });
            //如果字典中有总工程在运行，不允许执行单个流程
            if (_threadDictinary.ContainsKey("process")) {
                return;
            }
            bool state = _threadDictinary.TryAdd(flowData.Name, true);
            if (!state) {
                return;
            }
            task.Start();
            await task;
        }

        /// <summary>
        /// 单个流程循环运行
        /// </summary>
        /// <returns></returns>
        public async Task FlowRunLoop(FlowNode flowData) {
            Task task = new Task(() => {
                try {
                    int index = 0; // 初始化索引
                    while (true) {
                        //单个流程运行前将所有节点状态置空
                        if (index == 0) {
                            foreach (var item in flowData.Children) {
                                InitializeTreeNodeStatus(item);
                            }
                        }

                        bool state_NoFindValue = _threadDictinary.TryGetValue(flowData.Name, out bool state_Pause);
                        if (state_NoFindValue == false || state_Pause == false) {
                            break;
                        }
                        bool runStatus = SwitchToCorrectNodeOperation(flowData.Children[index]);

                        //选择到正确节点执行没成功，跳出整个循环，停止执行
                        if (!runStatus){
                            break;
                        }

                        if (index < flowData.Children.Count - 1) {
                            index++;
                        }
                        else {
                            index = 0;
                            Log.Verbose(flowData.Name + "执行完毕休眠20ms");
                            Thread.Sleep(20);
                        }
                    };
                }
                finally {
                    _threadDictinary.TryRemove(flowData.Name, out _);
                }
            });
            //如果字典中有总工程在运行，不允许执行单个流程
            if (_threadDictinary.ContainsKey("process")) {
                return;
            }
            bool state = _threadDictinary.TryAdd(flowData.Name, true);
            if (!state) {
                return;
            }
            task.Start();
            await task;
        }

        /// <summary>
        /// 单个流程停止运行
        /// </summary>
        /// <returns></returns>
        public bool FlowRunStop(string name) {
            _threadDictinary.TryUpdate(name, false, true);
            return true;
        }

        /// <summary>
        /// 单个任务节点运行
        /// 需要单开一个线程，与界面主线程分开
        /// </summary>
        /// <returns></returns>
        public async Task NodeRunOnce(TaskNode data) {
            //从这里往前是主线程执行的，直到task.run
            Task task = new Task(() => {
                try {
                    bool status = SwitchToCorrectNodeOperation(data);
                    //更新数据到界面存储，单步执行需要这个，其他流程、工程执行不需要
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { data.TaskView.ViewOperationDataUpdate(data.Data_InputParams, data.Data_OutputParams); }));
                }
                finally {
                    //线程字典移除当前线程，因为当前线程结束了
                    _threadDictinary.TryRemove(data.FlowName, out _);
                }
            });
            //如果字典不为空，就说明有流程或者单个任务没执行完毕，禁止单步执行
            if (!_threadDictinary.IsEmpty) {
                return;
            }
            //从这里往后是子线程上下文捕获，还是子线程执行
            bool state = _threadDictinary.TryAdd(data.FlowName, true);
            if (!state) {
                return;
            }
            task.Start();
            await task;
        }

        /// <summary>
        /// 切换到正确的节点操作，普通节点区别于if/while/for节点，切换失败则返回false基本意味着需要停止当前流程
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool SwitchToCorrectNodeOperation(TaskNode node) {
            //当前线程点击了暂停
            bool state_NoFindValue = _threadDictinary.TryGetValue(node.FlowName, out bool state_Pause);
            if (state_NoFindValue == false || state_Pause == false) {
                return false;
            }

            //当前节点禁止运行
            if (!node.AllowRun) { 
                return true;
            }

            switch (node.OperationType) {
                case ItemOperationType.kNone:
                    return false;
                //break;
                case ItemOperationType.kCommon:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => { 
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus commonStatus = RunCommonNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicCommonNode)) {
                        return false;
                    }
                    dicCommonNode.Data_OutputParams = node.Data_OutputParams;
                    dicCommonNode.Data_InputParams = node.Data_InputParams;

                    break;
                case ItemOperationType.kIf:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus ifStatus = RunIfNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicIfNode)) {
                        return false;
                    }
                    dicIfNode.Data_OutputParams = node.Data_OutputParams;
                    dicIfNode.Data_InputParams = node.Data_InputParams;

                    break;
                case ItemOperationType.kElseIf:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus elseIfStatus = RunElseIfNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicElseIfNode)) {
                        return false;
                    }
                    dicElseIfNode.Data_OutputParams = node.Data_OutputParams;
                    dicElseIfNode.Data_InputParams = node.Data_InputParams;
                    break;
                case ItemOperationType.kElse:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus elseStatus = RunElseNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicElseNode)) {
                        return false;
                    }
                    dicElseNode.Data_OutputParams = node.Data_OutputParams;
                    dicElseNode.Data_InputParams = node.Data_InputParams;
                    break;
                case ItemOperationType.kFor:
                    break;
                case ItemOperationType.kWhile:
                    break;
                case ItemOperationType.kBreak:
                    break;
                case ItemOperationType.kContinue:
                    break;
                case ItemOperationType.kSerial:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus serialStatus = RunSerialNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicSerialNode)) {
                        return false;
                    }
                    dicSerialNode.Data_OutputParams = node.Data_OutputParams;
                    dicSerialNode.Data_InputParams = node.Data_InputParams;

                    break;
                case ItemOperationType.kParallel:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus parallelStatus = RunParallelNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicParallelNode)) {
                        return false;
                    }
                    dicParallelNode.Data_OutputParams = node.Data_OutputParams;
                    dicParallelNode.Data_InputParams = node.Data_InputParams;
                    break;
                case ItemOperationType.kStopFlow:
                    break;
                case ItemOperationType.kReRunFlow:
                    break;
                case ItemOperationType.kStopProcess:
                    break;
                case ItemOperationType.kReRunProcess:
                    break;
                default:
                    break;
            }
            //停止流程
            if (node.NodeStatus == TaskNodeStatus.kFlowStop) {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 重置树节点状态为空
        /// </summary>
        /// <param name="node"></param>
        private void InitializeTreeNodeStatus(TaskNode node) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                node.ItemView.NodeStatus = TaskNodeStatus.kNone;
            }));
            node.NodeStatus = TaskNodeStatus.kNone;
            foreach (var item in node.Children) {
                InitializeTreeNodeStatus(item);
            }
        }

        /// <summary>
        /// 运行普通节点
        /// 这个普通的意思是相较于if/while/for...
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunCommonNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kCommon) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Warning(data.ItemName + "算法引擎出错，该模块输入的执行类型不为通用");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //输入参数并不进行深拷贝，传入输入参数，在算子中能进行修改输入参数，权且当作算子开发都是意识到这一点的人
            foreach (var item in data.Data_InputParams) {
                if (item.IsBind) {
                    string dataBindStr = item.UserParam as string;
                    if (dataBindStr == null) {
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    string linkLabel = "[" + data.FlowName + "].[" + dataBindStr + "]";
                    object linkData = RetrieveDataByName(linkLabel);
                    if (linkData == null) {
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    item.ActualParam = linkData;//同时改变item中的ActualParam，因为是object拷贝引用的指针
                }
                //非绑定，直接把UserParam给到ActualParam
                else {
                    item.ActualParam = item.UserParam;
                }
            }

            TaskNodeStatus startStatus = data.TaskOperation.Start(data.Data_InputParams);
            if (startStatus != TaskNodeStatus.kSuccess) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = startStatus;
                Log.Warning(data.ItemName + "Start Fail");
                return startStatus;
            }
            //Log.Verbose(data.ItemName + "Start Success");

            TaskNodeStatus runStatus = data.TaskOperation.Run();
            if (runStatus != TaskNodeStatus.kSuccess) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = runStatus;
                Log.Warning(data.ItemName + "Run Fail");
                return runStatus;
            }
            //Log.Verbose(data.ItemName + "Run Success");
            List<TaskOperationOutputParams> finishOutputParams = new List<TaskOperationOutputParams>();
            TaskNodeStatus finishStatus = data.TaskOperation.Finish(out finishOutputParams);
            if (finishStatus != TaskNodeStatus.kSuccess) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = finishStatus;
                Log.Warning(data.ItemName + "Finish Fail");
                return finishStatus;
            }
            //Log.Verbose(data.ItemName + "Finish Success");
            Log.Verbose(data.ItemName + "Success");
            //由于是引用传递进来的，所以把赋值外部会有改变
            data.Data_OutputParams = finishOutputParams;

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行串行节点
        /// 执行到该节点会暂停继续往下执行，先执行该节点以下的系列子节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus RunSerialNode(TaskNode data) {
            //计时开始
            //Log.Verbose(data.ItemName + "Start Success");
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kSerial) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Warning(data.ItemName + "算法引擎出错，该模块输入的执行类型不为串行");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                bool runStatus = SwitchToCorrectNodeOperation(childNode);
                if (!runStatus) { 
                    break;
                }
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行并行节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus RunParallelNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kParallel) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Warning(data.ItemName + "算法引擎出错，该模块输入的执行类型不为并行");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，单个流程停止
            }


            // 创建并启动多个任务
            Task[] tasks = new Task[data.Children.Count];
            for (int i = 0; i < data.Children.Count; i++) {
                TaskNode childNode = data.Children[i];
                tasks[i] = Task.Run(() => {
                    //点击了暂停
                    bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                    if (state_NoFindValue == false || state_Pause == false) {
                        return;
                    }
                    SwitchToCorrectNodeOperation(childNode);
                    ;
                });
            }

            // 等待所有任务完成
            Task.WhenAll(tasks).Wait();

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行if节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunIfNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kIf) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为if");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            if (data.Data_InputParams.Count != 1) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，if节点的输入参数必须为1");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            data.Data_InputParams[0].ActualParam = data.Data_InputParams[0].UserParam;
            if (data.Data_InputParams[0].ActualParam == null || data.Data_InputParams[0].ActualParam.GetType() != typeof(string)) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，if节点的输入参数表达式必须为string类型且不为null");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //"KDouble(0.四则运算(1).2.double结果(2)) some other text KDouble(3.加法(2).4.double结果(5))"
            string ifExpression = data.Data_InputParams[0].ActualParam as string;

            // 正则表达式匹配形如 KDouble(数字.操作.数字.操作) 结构的模式
            string patternLink = @"([a-zA-Z]+)\((\d+)\.([^\.\(]+)\.(\d+)\.([^\)]+)\)";

            // 使用正则表达式匹配
            MatchCollection matches = Regex.Matches(ifExpression, patternLink);

            string ifValue = Regex.Replace(ifExpression, patternLink, match => {
                string globalStr = "[" + data.FlowName + "].[" + match.Value + "]";
                object obj = RetrieveDataByName(globalStr);
                if (obj == null) {
                    return "链接为空，非法链接";
                }
                else if (obj.GetType() == typeof(int)) {
                    int actualInt = Convert.ToInt32(obj);
                    return actualInt.ToString();
                }
                else if (obj.GetType() == typeof(float)) {
                    float actualFloat = (float)obj;
                    return actualFloat.ToString();
                }
                else if (obj.GetType() == typeof(double)) {
                    double actualDouble = Convert.ToDouble(obj);
                    return actualDouble.ToString();
                }
                else if (obj.GetType() == typeof(string)) {
                    string actualString = obj as string;
                    return actualString;
                }
                else if (obj.GetType() == typeof(bool)) {
                    bool actualBool = Convert.ToBoolean(obj);
                    return actualBool.ToString();
                }
                return "if节点的输入参数表达式暂时只支持int/float/double/string/bool";
            });

            // 使用正则表达式匹配 "true" 或 "false"，忽略大小写
            string patternBool = @"\b(true|false)\b";
            ifValue = Regex.Replace(ifValue, patternBool, match => match.Value.ToLower());

            // 使用 NCalc 解析并计算表达式
            var nCalcExpression = new NCalc.Expression(ifValue);

            // 计算结果
            try {
                object nCalcResult = nCalcExpression.Evaluate();
                if (nCalcResult.GetType() != typeof(bool)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Warning(data.ItemName + "运算表达式出错，运算结果不为bool");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                bool ifResult = Convert.ToBoolean(nCalcResult);

                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "if节点表达式",
                        ParamType = LinkDataType.kString,
                        ActualParam = ifValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "if节点运行结果",
                        ParamType = LinkDataType.kBool,
                        ActualParam = ifResult,
                    }
                };

                if (!ifResult) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "该节点运算符为false，不执行if中运行块");
                    return TaskNodeStatus.kFailure;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "if节点表达式",
                        ParamType = LinkDataType.kString,
                        ActualParam = "运算表达式出错，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Warning(data.ItemName + "运算表达式出错，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }



            //这里就是需要执行if中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                bool runStatus = SwitchToCorrectNodeOperation(childNode);
                if (!runStatus) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
            }


            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行elseif节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunElseIfNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //先判断前面的是不是if，不是就停止流程
            if (data == null) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，elseif输入节点为空");
                return TaskNodeStatus.kFlowStop;
            }
            if (data.Previous == null || (data.Previous.OperationType != ItemOperationType.kIf && data.Previous.OperationType != ItemOperationType.kElseIf)) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "该节点前只应该是if或者else if");
                return TaskNodeStatus.kFlowStop;
            }

            if (data.OperationType != ItemOperationType.kElseIf) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为elseif");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            if (data.Data_InputParams.Count != 1) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，else if节点的输入参数必须为1");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //else if还需要判断之前节点是否有执行结果
            TaskNode tracingNode = data.Previous;
            //if、else if这一些控制语句往前遍历是否有执行过了的，执行过了标志位置为true
            bool ifExecutionFlag = false;
            while (true) {
                if (tracingNode == null) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该节点往前遍历执行状态时遇到空节点");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果当前节点是else if那么正常迭代
                if (tracingNode.OperationType == ItemOperationType.kElseIf) {
                    //如果当前else if的输出都不正常，那就需要停止当前流程运行
                    if (tracingNode.Data_OutputParams.Count != 2) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "该往前遍历状态遇到输出异常点,输出参数不为2");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    if (tracingNode.Data_OutputParams.Count < 2 || tracingNode.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "软件框架出错，该往前遍历状态遇到输出异常点,输出参数不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    bool ifResult = Convert.ToBoolean(tracingNode.Data_OutputParams[1].ActualParam);
                    //如果当前节点被执行了，那么执行标志位就置为true
                    if (ifResult) {
                        ifExecutionFlag = true;
                        break;
                    }
                }
                //如果当前节点是if那么就要结束了
                else if (tracingNode.OperationType == ItemOperationType.kIf) {
                    //如果当前else if的输出都不正常，那就需要停止当前流程运行
                    if (tracingNode.Data_OutputParams.Count != 2) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "该往前遍历状态遇到输出异常点,输出参数不为2");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    if (tracingNode.Data_OutputParams.Count < 2 || tracingNode.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "软件框架出错，往前遍历状态遇到输出异常点,输出参数不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    bool ifResult = Convert.ToBoolean(tracingNode.Data_OutputParams[1].ActualParam);
                    //如果当前节点被执行了，那么执行标志位就置为true
                    if (ifResult) {
                        ifExecutionFlag = true;
                    }
                    break;
                }
                else { 
                
                }
                //还要一些其他奇怪的情况就统一报错
                if (tracingNode.Previous == null) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该往前遍历状态遇到空节点");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                if (tracingNode.OperationType == ItemOperationType.kElseIf && tracingNode.Previous.OperationType != ItemOperationType.kIf && tracingNode.Previous.OperationType != ItemOperationType.kElseIf) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该节点之前必须为if或者elseif");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                tracingNode = tracingNode.Previous;
            }


            data.Data_InputParams[0].ActualParam = data.Data_InputParams[0].UserParam;
            if (data.Data_InputParams[0].ActualParam == null || data.Data_InputParams[0].ActualParam.GetType() != typeof(string)) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，else if节点的输入参数表达式必须为string类型且不为null");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //"KDouble(0.四则运算(1).2.double结果(2)) some other text KDouble(3.加法(2).4.double结果(5))"
            string ifExpression = data.Data_InputParams[0].ActualParam as string;

            // 正则表达式匹配形如 KDouble(数字.操作.数字.操作) 结构的模式
            string patternLink = @"([a-zA-Z]+)\((\d+)\.([^\.\(]+)\.(\d+)\.([^\)]+)\)";

            // 使用正则表达式匹配
            MatchCollection matches = Regex.Matches(ifExpression, patternLink);

            string ifValue = Regex.Replace(ifExpression, patternLink, match => {
                string globalStr = "[" + data.FlowName + "].[" + match.Value + "]";
                object obj = RetrieveDataByName(globalStr);
                if (obj == null) {
                    return "链接为空，非法链接";
                }
                else if (obj.GetType() == typeof(int)) {
                    int actualInt = Convert.ToInt32(obj);
                    return actualInt.ToString();
                }
                else if (obj.GetType() == typeof(float)) {
                    float actualFloat = (float)obj;
                    return actualFloat.ToString();
                }
                else if (obj.GetType() == typeof(double)) {
                    double actualDouble = Convert.ToDouble(obj);
                    return actualDouble.ToString();
                }
                else if (obj.GetType() == typeof(string)) {
                    string actualString = obj as string;
                    return actualString;
                }
                else if (obj.GetType() == typeof(bool)) {
                    bool actualBool = Convert.ToBoolean(obj);
                    return actualBool.ToString();
                }
                return "else if节点的输入参数表达式暂时只支持int/float/double/string/bool";
            });

            // 使用正则表达式匹配 "true" 或 "false"，忽略大小写
            string patternBool = @"\b(true|false)\b";
            ifValue = Regex.Replace(ifValue, patternBool, match => match.Value.ToLower());

            // 使用 NCalc 解析并计算表达式
            var nCalcExpression = new NCalc.Expression(ifValue);

            // 计算结果
            try {
                object nCalcResult = nCalcExpression.Evaluate();
                if (nCalcResult.GetType() != typeof(bool)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Warning(data.ItemName + "运算表达式出错，运算结果不为bool");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                bool ifResult = Convert.ToBoolean(nCalcResult);

                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点表达式",
                        ParamType = LinkDataType.kString,
                        ActualParam = ifValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点运行结果",
                        ParamType = LinkDataType.kBool,
                        ActualParam = ifResult,
                    }
                };

                //判断是否需要串行执行，也就是之前是否有if或者elseif执行过了
                if (ifExecutionFlag) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "None");
                    return TaskNodeStatus.kNone;
                }

                if (!ifResult) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "该节点运算符为false，不执行else if中运行块");
                    return TaskNodeStatus.kFailure;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点表达式",
                        ParamType = LinkDataType.kString,
                        ActualParam = "运算表达式出错，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Warning(data.ItemName + "运算表达式出错，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //这里就是需要执行if中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                bool runStatus = SwitchToCorrectNodeOperation(childNode);
                if (!runStatus) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
            }


            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行else节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunElseNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kElse) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为else");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //else if还需要判断之前节点是否有执行结果
            TaskNode tracingNode = data.Previous;
            //if、else if这一些控制语句往前遍历是否有执行过了的，执行过了标志位置为true
            bool ifExecutionFlag = false;
            while (true) {
                if (tracingNode == null) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该节点往前遍历执行状态时遇到空节点");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果当前节点是else if那么正常迭代
                if (tracingNode.OperationType == ItemOperationType.kElseIf) {
                    //如果当前else if的输出都不正常，那就需要停止当前流程运行
                    if (tracingNode.Data_OutputParams.Count != 2) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "该往前遍历状态遇到输出异常点,输出参数不为2");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    if (tracingNode.Data_OutputParams.Count < 2 || tracingNode.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "软件框架出错，该往前遍历状态遇到输出异常点,输出参数不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    bool ifResult = Convert.ToBoolean(tracingNode.Data_OutputParams[1].ActualParam);
                    //如果当前节点被执行了，那么执行标志位就置为true
                    if (ifResult) {
                        ifExecutionFlag = true;
                        break;
                    }
                }
                //如果当前节点是if那么就要结束了
                else if (tracingNode.OperationType == ItemOperationType.kIf) {
                    //如果当前else if的输出都不正常，那就需要停止当前流程运行
                    if (tracingNode.Data_OutputParams.Count != 2) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "该往前遍历状态遇到输出异常点,输出参数不为2");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    if (tracingNode.Data_OutputParams.Count < 2 || tracingNode.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "软件框架出错，往前遍历状态遇到输出异常点,输出参数不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    bool ifResult = Convert.ToBoolean(tracingNode.Data_OutputParams[1].ActualParam);
                    //如果当前节点被执行了，那么执行标志位就置为true
                    if (ifResult) {
                        ifExecutionFlag = true;
                    }
                    break;
                }
                else if (tracingNode.OperationType == ItemOperationType.kElse) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该节点之前必须为if或者elseif");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                else {

                }
                //还要一些其他奇怪的情况就统一报错
                if (tracingNode.Previous == null) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该往前遍历状态遇到空节点");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                if (tracingNode.OperationType == ItemOperationType.kElseIf && tracingNode.Previous.OperationType != ItemOperationType.kIf && tracingNode.Previous.OperationType != ItemOperationType.kElseIf) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "该节点之前必须为if或者elseif");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                tracingNode = tracingNode.Previous;
            }

            //判断是否需要串行执行，也就是之前是否有if或者elseif执行过了
            if (ifExecutionFlag) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kNone;
                Log.Verbose(data.ItemName + "None");
                return TaskNodeStatus.kNone;
            }

            //这里就是需要执行else中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                bool runStatus = SwitchToCorrectNodeOperation(childNode);
                if (!runStatus) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行while节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunWhileNode(TaskNode data) {
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行for节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunForNode(TaskNode data) {
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行break节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunBreakNode(TaskNode data) {
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行continue节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunContinueNode(TaskNode data) {
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 总结全局输出数据到字典中
        /// </summary>
        private void SummarizeLinkData(List<FlowNode> processData) {
            //前序遍历树，遍历到目标节点时返回
            Action<TaskNode> action_PreorderTraversal = null;
            action_PreorderTraversal = (TaskNode node) => {
                if (node == null) {
                    return; // 如果节点为空，则返回 null
                }
                _linkDataDictinary.TryAdd("[" + node.FlowName + "].[" + node.ItemName + "]", node);
                foreach (var item in node.Children) {
                    action_PreorderTraversal(item);
                }
            };



            _linkDataDictinary.Clear();
            foreach (FlowNode flowNode in processData) {
                if (flowNode == null) { 
                    continue;
                }
                foreach (var item in flowNode.Children) {
                    if (item == null) {
                        continue;
                    }
                    action_PreorderTraversal(item);
                }
            }
        }

        /// <summary>
        /// 总结全局子界面数据到字典中
        /// </summary>
        /// <param name="subViewData"></param>
        private void SummarizeSubView(List<SubViewPluginBase> subViewDatas) {
            _subViewDictinary.Clear();
            foreach (var subView in subViewDatas) {
                _subViewDictinary.TryAdd(subView.UniqueName[LangaugeType.kChinese], subView);
                _subViewDictinary.TryAdd(subView.UniqueName[LangaugeType.kEnglish], subView);
            }
        }

        /// <summary>
        /// 总结全局资源数据到字典中
        /// </summary>
        /// <param name="resData"></param>
        private void SummarizeGlobalRes(List<ResourceOptionData> resDatas) {
            _globalResDictinary.Clear();
            foreach (var res in resDatas) {
                _globalResDictinary.TryAdd(res.ResName, res.ResOperationInterface);
            }
        }

        /// <summary>
        /// 根据名字检索输出，全局检索
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private object RetrieveDataByName(string inputLinkStr) {
            //[新建流程(0)].[KDouble(0.四则运算(1).2.double结果(2))]
            string pattern = @"\[(.*?)\]\.\[([^\(]+)\((.*?)\)\]";
            var match = Regex.Match(inputLinkStr, pattern);
            if (!match.Success) { 
                return null;
            }

            // 提取第一部分 "新建流程(0)"
            string part1 = match.Groups[1].Value;
            // 提取第二部分 "KDouble" 
            string part2 = match.Groups[2].Value;
            // 提取括号内的内容 "0.四则运算(1).2.double结果(2)"
            string innerContent = match.Groups[3].Value;
            // 分割括号内的部分
            string[] parts = innerContent.Split(new[] { '.' }, StringSplitOptions.None);

            if (parts.Length == 2 && part2 == LinkDataType.kView.ToString()) {
                string part3 = parts[0]; 
                string part4 = parts[1]; 
                string viewLinkStr = part4;
                if (!_subViewDictinary.ContainsKey(viewLinkStr)) {
                    return null;
                }
                if (!_subViewDictinary.TryGetValue(viewLinkStr, out SubViewPluginBase subView)) {
                    return null;
                }
                return subView;
            }
            else if (parts.Length == 2 && part2 == LinkDataType.kResource.ToString()) {
                string part3 = parts[0]; 
                string part4 = parts[1];
                string resLinkStr = part4;
                if (!_globalResDictinary.ContainsKey(resLinkStr)) {
                    return null;
                }
                if (!_globalResDictinary.TryGetValue(resLinkStr, out ResOperationPluginBase globalRes)) {
                    return null;
                }
                return globalRes;
            }
            else if (parts.Length == 4) {
                string part3 = parts[0]; // "0"
                string part4 = parts[1]; // "四则运算"
                string part5 = parts[2]; // "2"
                string part6 = parts[3]; // "double结果"


                if (!int.TryParse(part3, out int nodeId)) {
                    return null;
                }
                if (!int.TryParse(part5, out int paramId)) {
                    return null;
                }

                string taskLinkStr = "[" + part1 + "].[" + part3 + "." + part4 + "]";//[新建流程].[0.四则运算]

                if (!_linkDataDictinary.TryGetValue(taskLinkStr, out TaskNode node)) {
                    return null;
                }
                for (int i = 0; i < node.Data_OutputParams.Count; i++) {
                    TaskOperationOutputParams output = node.Data_OutputParams[i];
                    if (i != paramId) {
                        continue;
                    }
                    if (part6 != output.ParamName) {
                        continue;
                    }
                    return output.ActualParam;
                }
                return null;
            }

            return null;
        }

        /// <summary>
        /// 根据表达式解析出结果，全局检索
        /// </summary>
        /// <returns></returns>
        private object ObtainResultByExpression(string expression) {
            if (string.IsNullOrEmpty(expression)) { 
                return null;
            }



            return null;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return null;
            }
            else if (message.Function == "ProcessRunOnce" && message.Content is ValueTuple<List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_ProcessRunOnce) {
                SummarizeLinkData(params_ProcessRunOnce.Item1);
                SummarizeSubView(params_ProcessRunOnce.Item2);
                SummarizeGlobalRes(params_ProcessRunOnce.Item3);
                ProcessRunOnce(params_ProcessRunOnce.Item1);
            }
            else if (message.Function == "ProcessRunLoop" && message.Content is ValueTuple<List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_ProcessRunLoop) {
                SummarizeLinkData(params_ProcessRunLoop.Item1);
                SummarizeSubView(params_ProcessRunLoop.Item2);
                SummarizeGlobalRes(params_ProcessRunLoop.Item3);
                ProcessRunLoop(params_ProcessRunLoop.Item1);
            }
            else if (message.Function == "ProcessRunStop") {
                ProcessRunStop();
            }
            else if (message.Function == "FlowRunOnce" && message.Content is ValueTuple<FlowNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_FlowRunOnce) {
                SummarizeLinkData(params_FlowRunOnce.Item2);
                SummarizeSubView(params_FlowRunOnce.Item3);
                SummarizeGlobalRes(params_FlowRunOnce.Item4);
                Task task = FlowRunOnce(params_FlowRunOnce.Item1);
            }
            else if (message.Function == "FlowRunLoop" && message.Content is ValueTuple<FlowNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_FlowRunLoop) {
                SummarizeLinkData(params_FlowRunLoop.Item2);
                SummarizeSubView(params_FlowRunLoop.Item3);
                SummarizeGlobalRes(params_FlowRunLoop.Item4);
                Task task = FlowRunLoop(params_FlowRunLoop.Item1);
            }
            else if (message.Function == "FlowRunStop" && message.Content is string params_FlowRunStop) {
                FlowRunStop(params_FlowRunStop);
            }
            else if (message.Function == "NodeRunOnce" && message.Content is ValueTuple<TaskNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_NodeRunOnce) {
                SummarizeLinkData(params_NodeRunOnce.Item2);
                SummarizeSubView(params_NodeRunOnce.Item3);
                SummarizeGlobalRes(params_NodeRunOnce.Item4);
                Task task = NodeRunOnce(params_NodeRunOnce.Item1);
            }
            else if (message.Function == "Dispose") {
                Dispose();
            }
            else {

            }
            return null;
        }

        public string DataToJson() {
            return string.Empty;
        }

        public void JsonToData(string str) {

        }

        public void SetSendMessageCallback(SendMessage cb) {
            if (cb == null) { 
                return;
            }
            _sendMessage = cb;
        }


    }
}
