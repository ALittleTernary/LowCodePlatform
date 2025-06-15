using Antlr.Runtime.Tree;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.View;
using LowCodePlatform.View.Base;
using NCalc;
using OpenCvSharp;
using OpenCvSharp.Flann;
using Serilog;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Windows;
using System.Windows.Markup;
using static OpenCvSharp.ML.DTrees;
using static OpenCvSharp.ML.SVM;

namespace LowCodePlatform.Engine
{

    /// <summary>
    /// 测试引擎运行状态
    /// </summary>
    public enum AlgoEngineStatus {
        kNone = 0,
        /// <summary>
        /// 工程单次运行中
        /// </summary>
        kProcessOnceRunning = 1,
        /// <summary>
        /// 工程循环运行中
        /// </summary>
        kProcessLoopRunning = 2,
        /// <summary>
        /// 流程单次运行中
        /// </summary>
        kFlowOnceRunning = 3,
        /// <summary>
        /// 流程在单次执行中进行重测
        /// </summary>
        kFlowOnceResurvey = 4,
        /// <summary>
        /// 流程循环运行中
        /// </summary>
        kFlowLoopRunning = 5,
        /// <summary>
        /// 流程在循环执行中进行重测
        /// </summary>
        kFlowLoopResurvey = 6,
        /// <summary>
        /// 当前是在单节点运行中
        /// </summary>
        kNodeOnceRunning = 7,
        /// <summary>
        /// 当前是在停止状态
        /// </summary>
        kStop = 6,

    }

    /// <summary>
    /// 流程节点
    /// </summary>
    public class FlowNode {
        /// <summary>
        /// 名字是整个工程里唯一的
        /// 流程名
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
        private AlgoEngineStatus _engineStatus = AlgoEngineStatus.kNone;
        public AlgoEngineStatus EngineStatus
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

        private bool _engineIsRunning = true;

        public bool EngineIsRunning { 
            set {
                foreach (var child in Children) { 
                    child.EngineIsRunning = value;
                }
                _engineIsRunning = value;
                if (TaskOperation == null) { 
                    return;
                }
                TaskOperation.EngineIsRunning = value;
            }
            get {
                return _engineIsRunning;
            }
        }
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

        /// <summary>
        /// 一个流程中的汇总字典，通过名字找到资源，用于停止、重测
        /// </summary>
        ConcurrentDictionary<string, FlowNode> _flowDataDictinary = new ConcurrentDictionary<string, FlowNode>();

        /// <summary>
        /// 线程名字，对应线程状态(并不需要把线程本身加入字典，拿到线程本身了也做不了什么事)
        /// </summary>
        private ConcurrentDictionary<string, bool> _threadDictinary = new ConcurrentDictionary<string, bool>();



        public AlgoEngine() { 
            
        }

        public void Dispose() {
            _linkDataDictinary.Clear();
            _subViewDictinary.Clear();
            _globalResDictinary.Clear();
            _flowDataDictinary.Clear();
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
            foreach (var flow in _flowDataDictinary) {
                foreach (var task in flow.Value.Children) {
                    if (task.TaskOperation == null) {
                        continue;
                    }
                    task.EngineIsRunning = false;
                    task.EngineStatus = AlgoEngineStatus.kStop;
                }
            } 
            return true;
        }

        /// <summary>
        /// 单个流程运行一次
        /// </summary>
        /// <returns></returns>
        public async Task FlowRunOnce(FlowNode flowData, int index = 0, AlgoEngineStatus engineStatus = AlgoEngineStatus.kFlowOnceRunning) {
            //单个流程运行前将所有节点状态置空,重测不置空
            foreach (var item in flowData.Children) {
                if (index == 0) {
                    InitializeTreeNodeStatus(item);
                }
                item.EngineStatus = engineStatus;
            }

            Task task = new Task(() => {
                try {
                    for (int i = index; i < flowData.Children.Count; i++) {
                        var child = flowData.Children[i];
                        //点击了暂停
                        bool state_NoFindValue = _threadDictinary.TryGetValue(flowData.Name, out bool state_Pause);
                        if (state_NoFindValue == false || state_Pause == false) {
                            break;
                        }
                        TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(child);

                        //选择到正确节点执行没成功，跳出整个循环，停止执行
                        if (runStatus == TaskNodeStatus.kNone) {
                            break;
                        }
                        else if (runStatus == TaskNodeStatus.kSuccess) {

                        }
                        else if (runStatus == TaskNodeStatus.kFailure) {
                            break;
                        }
                        else if (runStatus == TaskNodeStatus.kReturn) {
                            break;
                        }
                        else if (runStatus == TaskNodeStatus.kFlowStop) {
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
            _flowDataDictinary.AddOrUpdate(flowData.Name, flowData, (key, oldValue) => flowData);
        }

        /// <summary>
        /// 单个流程循环运行
        /// </summary>
        /// <returns></returns>
        public async Task FlowRunLoop(FlowNode flowData, int index = 0, AlgoEngineStatus engineStatus = AlgoEngineStatus.kFlowLoopRunning) {
            foreach (var item in flowData.Children) {
                //不是重测时，单个流程运行前将所有节点状态置空
                if (engineStatus != AlgoEngineStatus.kFlowLoopResurvey) {
                    InitializeTreeNodeStatus(item);
                }
                item.EngineStatus = engineStatus;
            }

            Task task = new Task(() => {
                try {
                    if (index >= flowData.Children.Count) {
                        return;
                    }

                    while (true) {
                        //单个流程运行前将所有节点状态置空
                        if (index == 0) {
                            foreach (var item in flowData.Children) {
                                InitializeTreeNodeStatus(item);
                                item.EngineStatus = AlgoEngineStatus.kProcessLoopRunning;
                            }
                        }

                        bool state_NoFindValue = _threadDictinary.TryGetValue(flowData.Name, out bool state_Pause);
                        if (state_NoFindValue == false || state_Pause == false) {
                            break;
                        }
                        TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(flowData.Children[index]);

                        //选择到正确节点执行没成功，跳出整个循环，停止执行
                        if (runStatus == TaskNodeStatus.kNone) {
                            break;
                        }
                        else if (runStatus == TaskNodeStatus.kSuccess) {

                        }
                        else if (runStatus == TaskNodeStatus.kFailure) {
                            break;
                        }
                        else if (runStatus == TaskNodeStatus.kReturn) {
                            index = 0;
                            Log.Verbose(flowData.Name + "执行完毕休眠20ms");
                            Thread.Sleep(20);
                            continue;
                        }
                        else if (runStatus == TaskNodeStatus.kFlowStop) {
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
            _flowDataDictinary.AddOrUpdate(flowData.Name, flowData, (key, oldValue) => flowData);
        }

        /// <summary>
        /// 单个流程停止运行
        /// </summary>
        /// <returns></returns>
        public bool FlowRunStop(string name) {
            _threadDictinary.TryUpdate(name, false, true);
            bool status = _flowDataDictinary.TryGetValue(name, out FlowNode flowNode);
            if (!status || flowNode == null) {
                return false;
            }
            foreach (var item in flowNode.Children) {
                if (item.TaskOperation == null) { 
                    continue;
                }
                item.EngineIsRunning = false;
                item.EngineStatus = AlgoEngineStatus.kStop;
            }
            return true;
        }

        /// <summary>
        /// 单个流程重测
        /// </summary>
        /// <returns></returns>
        public bool FlowRunResurvey(string name) {
            bool status = _flowDataDictinary.TryGetValue(name, out FlowNode flowNode);
            if (!status || flowNode == null) { 
                return false;
            }
            int resurveyIndex = 0;
            AlgoEngineStatus algoEngineStatus = AlgoEngineStatus.kNone;
            for (int i = 0; i < flowNode.Children.Count; i++) { 
                TaskNode node = flowNode.Children[i];
                //找到开始的重测点，初始为0
                if (node.OperationType == ItemOperationType.kOriginalResurvey || node.OperationType == ItemOperationType.kSwitchResurvey) {
                    resurveyIndex = i;
                }
                if (node.NodeStatus == TaskNodeStatus.kSuccess) {
                    continue;
                }

                algoEngineStatus = node.EngineStatus;
                //当找到不为成功的第一个时，就跳出循环
                break;
            }
            switch (algoEngineStatus) {
                case AlgoEngineStatus.kNone:
                    Log.Warning("算法引擎空置状态下暂时不支持重测机制");
                    break;
                case AlgoEngineStatus.kProcessOnceRunning:
                    Log.Warning("算法引擎工程单次运行状态下暂时不支持重测机制");
                    break;
                case AlgoEngineStatus.kProcessLoopRunning:
                    Log.Warning("算法引擎工程循环运行状态下暂时不支持重测机制");
                    break;
                case AlgoEngineStatus.kFlowOnceRunning:
                    Task taskFlowRunOnce = FlowRunOnce(flowNode, resurveyIndex, AlgoEngineStatus.kFlowOnceResurvey);
                    break;
                case AlgoEngineStatus.kFlowLoopRunning:
                    Task taskFlowRunLoop = FlowRunLoop(flowNode, resurveyIndex, AlgoEngineStatus.kFlowLoopResurvey);
                    break;
                case AlgoEngineStatus.kNodeOnceRunning:
                    Log.Warning("算法引擎单步执行运行状态下暂时不支持重测机制");
                    break;
                case AlgoEngineStatus.kStop:
                    Log.Warning("算法引擎已停止运行状态下暂时不支持重测机制");
                    break;
                default:
                    break;
            }
            return true;
        }

        /// <summary>
        /// 单个任务节点运行
        /// 需要单开一个线程，与界面主线程分开
        /// </summary>
        /// <returns></returns>
        public async Task NodeRunOnce(TaskNode data) {
            data.EngineStatus = AlgoEngineStatus.kNodeOnceRunning;

            //从这里往前是主线程执行的，直到task.run
            Task task = new Task(() => {
                try {
                    TaskNodeStatus status = SwitchToCorrectNodeOperation(data);
                    if (status == TaskNodeStatus.kSuccess || status == TaskNodeStatus.kReturn) {
                        //更新数据到界面存储，单步执行需要这个，其他流程、工程执行不需要
                        Application.Current.Dispatcher.BeginInvoke(new Action(() => { data.TaskView.ViewOperationDataUpdate(data.Data_InputParams, data.Data_OutputParams); }));
                    }
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
            data.EngineStatus = AlgoEngineStatus.kStop;
        }

        /// <summary>
        /// 切换到正确的节点操作，普通节点区别于if/while/for节点，切换失败则返回false基本意味着需要停止当前流程
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus SwitchToCorrectNodeOperation(TaskNode node) {
            //当前线程点击了暂停
            bool state_NoFindValue = _threadDictinary.TryGetValue(node.FlowName, out bool state_Pause);
            if (state_NoFindValue == false || state_Pause == false) {
                return TaskNodeStatus.kFlowStop;
            }

            //当前节点禁止运行
            if (!node.AllowRun) { 
                return TaskNodeStatus.kSuccess;
            }

            switch (node.OperationType) {
                case ItemOperationType.kNone:
                    return TaskNodeStatus.kFlowStop;
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicCommonNode.Data_OutputParams = node.Data_OutputParams;
                    dicCommonNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicCommonNode, (key, oldValue) => dicCommonNode);
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicIfNode.Data_OutputParams = node.Data_OutputParams;
                    dicIfNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicIfNode, (key, oldValue) => dicIfNode);
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseIfNode.Data_OutputParams = node.Data_OutputParams;
                    dicElseIfNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicElseIfNode, (key, oldValue) => dicElseIfNode);
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseNode.Data_OutputParams = node.Data_OutputParams;
                    dicElseNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicElseNode, (key, oldValue) => dicElseNode);
                    break;
                case ItemOperationType.kFor:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus forStatus = RunForNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicForNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicForNode.Data_OutputParams = node.Data_OutputParams;
                    dicForNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicForNode, (key, oldValue) => dicForNode);
                    break;
                case ItemOperationType.kWhile:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus whileStatus = RunWhileNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicWhileNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicWhileNode.Data_OutputParams = node.Data_OutputParams;
                    dicWhileNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicWhileNode, (key, oldValue) => dicWhileNode);
                    break;
                case ItemOperationType.kBreak:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus breakStatus = RunBreakNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicBreakNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicBreakNode.Data_OutputParams = node.Data_OutputParams;
                    dicBreakNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicBreakNode, (key, oldValue) => dicBreakNode);
                    break;
                case ItemOperationType.kContinue:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus continueStatus = RunContinueNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicContinueNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicContinueNode.Data_OutputParams = node.Data_OutputParams;
                    dicContinueNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicContinueNode, (key, oldValue) => dicContinueNode);
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicSerialNode.Data_OutputParams = node.Data_OutputParams;
                    dicSerialNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicSerialNode, (key, oldValue) => dicSerialNode);
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
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicParallelNode.Data_OutputParams = node.Data_OutputParams;
                    dicParallelNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicParallelNode, (key, oldValue) => dicParallelNode);
                    break;
                case ItemOperationType.kReturn:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus returnStatus = RunReturnNode(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicReturnNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicReturnNode.Data_OutputParams = node.Data_OutputParams;
                    dicReturnNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicReturnNode, (key, oldValue) => dicReturnNode);
                    break;
                case ItemOperationType.kStopFlow:
                    break;
                case ItemOperationType.kReRunFlow:
                    break;
                case ItemOperationType.kStopProcess:
                    break;
                case ItemOperationType.kReRunProcess:
                    break;
                case ItemOperationType.kOriginalResurvey:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus originalResurveyStatus = RunOriginalResurvey(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicOriginalResurveyNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicOriginalResurveyNode.Data_OutputParams = node.Data_OutputParams;
                    dicOriginalResurveyNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicOriginalResurveyNode, (key, oldValue) => dicOriginalResurveyNode);
                    break;
                case ItemOperationType.kSwitchResurvey:
                    //更新界面该节点为运行状态
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.NodeStatus = TaskNodeStatus.kRunning;
                    }));
                    TaskNodeStatus switchResurveyStatus = RunSwitchResurvey(node);
                    //更新该界面节点运行结束参数
                    Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                        node.ItemView.Time = node.Time;
                        node.ItemView.NodeStatus = node.NodeStatus;
                        node.ItemView.Data_InputParams = node.Data_InputParams;
                        node.ItemView.Data_OutputParams = node.Data_OutputParams;
                    }));

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + node.FlowName + "].[" + node.ItemName + "]", out TaskNode dicSwitchResurveyNode)) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicSwitchResurveyNode.Data_OutputParams = node.Data_OutputParams;
                    dicSwitchResurveyNode.Data_InputParams = node.Data_InputParams;
                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", dicSwitchResurveyNode, (key, oldValue) => dicSwitchResurveyNode);
                    break;
                default:
                    break;
            }

            return node.NodeStatus;
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
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为通用");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //输入参数并不进行深拷贝，传入输入参数，在算子中能进行修改输入参数，权且当作算子开发都是意识到这一点的人
            foreach (var item in data.Data_InputParams) {
                if (item.IsBind) {
                    string dataBindStr = item.UserParam as string;
                    if (dataBindStr == null) {
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    object linkData = RetrieveDataByName(data.FlowName, dataBindStr);
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

            //如果是需要编辑表达式的，就需要设置给回调函数
            LinkEditTaskOperationPluginBase linkEditOperation = data.TaskOperation as LinkEditTaskOperationPluginBase;
            if (linkEditOperation != null) {
                linkEditOperation.SetObtainResultByExpressionCallback((string expression) => { 
                    return ObtainResultByExpression(data.FlowName, expression); });
                linkEditOperation.SetUpdateLinkEditByExpressCallback((TaskViewInputParams input) => {
                    return UpdateLinkEditByExpress(data.FlowName, input); });
            }

            TaskNodeStatus startStatus = data.TaskOperation.Start(data.Data_InputParams);
            if (startStatus != TaskNodeStatus.kSuccess) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = startStatus;
                Log.Error(data.ItemName + "Start Fail");
                return startStatus;
            }
            //Log.Verbose(data.ItemName + "Start Success");
            try {
                TaskNodeStatus runStatus = data.TaskOperation.Run();
                if (runStatus != TaskNodeStatus.kSuccess) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = runStatus;
                    Log.Error(data.ItemName + "Run Fail");
                    return runStatus;
                }
            }
            catch (Exception ex) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运行崩溃:" + ex.ToString());
                return TaskNodeStatus.kFlowStop;
            }

            //Log.Verbose(data.ItemName + "Run Success");
            List<TaskOperationOutputParams> finishOutputParams = new List<TaskOperationOutputParams>();
            TaskNodeStatus finishStatus = data.TaskOperation.Finish(out finishOutputParams);
            if (finishStatus != TaskNodeStatus.kSuccess) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = finishStatus;
                Log.Error(data.ItemName + "Finish Fail");
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
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为串行");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                new TaskOperationOutputParams(){
                    ParamName = "break标志位",
                    ParamType = LinkDataType.kBool,
                    LinkVisual = false,
                    ActualParam = false,
                },
                new TaskOperationOutputParams(){
                    ParamName = "continue标志位",
                    ParamType = LinkDataType.kBool,
                    LinkVisual = false,
                    ActualParam = false,
                    }
            };

            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                if (runStatus == TaskNodeStatus.kNone) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kSuccess) { 
                
                }
                else if (runStatus == TaskNodeStatus.kFailure) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kFlowStop) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kReturn) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点请求返回");
                    return TaskNodeStatus.kReturn;
                }

                //获取全局变量中的该for节点状态，看看是否有外部对齐状态进行改变
                if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicSerialNode)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果elseif中break标志位置为了true，那么要跳出该循环
                if (dicSerialNode.Data_OutputParams.Count == 2 && dicSerialNode.Data_OutputParams[0] != null && dicSerialNode.Data_OutputParams[0].ActualParam.GetType() == typeof(bool)) {
                    bool breakStatus = Convert.ToBoolean(dicSerialNode.Data_OutputParams[0].ActualParam);
                    if (breakStatus) {
                        Log.Verbose(data.ItemName + "break信号触发使得串行执行提前结束");
                        break;
                    }
                }
                //如果elseif中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                if (dicSerialNode.Data_OutputParams.Count == 2 && dicSerialNode.Data_OutputParams[1] != null && dicSerialNode.Data_OutputParams[1].ActualParam.GetType() == typeof(bool)) {
                    bool continueStatus = Convert.ToBoolean(dicSerialNode.Data_OutputParams[1].ActualParam);
                    if (continueStatus) {
                        Log.Verbose(data.ItemName + "continue信号触发使得串行执行提前结束");
                        break;
                    }
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
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为并行");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，单个流程停止
            }


            // 创建并启动多个任务
            Task<TaskNodeStatus>[] tasks = new Task<TaskNodeStatus>[data.Children.Count];

            for (int i = 0; i < data.Children.Count; i++) {
                TaskNode childNode = data.Children[i];

                Func<TaskNodeStatus> func_task = () => {
                    bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                    if (state_NoFindValue == false || state_Pause == false) {
                        return TaskNodeStatus.kFlowStop;
                    }
                    TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                    return runStatus;
                };
                tasks[i] = Task.Run(func_task);
            }

            // 等待所有任务完成
            Task.WhenAll(tasks).Wait();

            for (int i = 0; i < tasks.Count(); i++) {
                if (tasks[i].Result == TaskNodeStatus.kNone) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "遇到意外的情况，有子流程执行结果为空");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (tasks[i].Result == TaskNodeStatus.kSuccess) {

                }
                else if (tasks[i].Result == TaskNodeStatus.kFailure) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "子节点执行失败");
                    return TaskNodeStatus.kFailure;
                }
                else if (tasks[i].Result == TaskNodeStatus.kReturn) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kReturn;
                    Log.Verbose(data.ItemName + "子节点请求返回");
                    return TaskNodeStatus.kReturn;
                }
                else if (tasks[i].Result == TaskNodeStatus.kFlowStop) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "遇到意外的情况，有子流程执行结果为空");
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

            string ifValue = ObtainResultByExpression(data.FlowName, ifExpression);



            // 计算结果
            try {
                // 使用 NCalc 解析并计算表达式
                var nCalcExpression = new NCalc.Expression(ifValue);
                object nCalcResult = nCalcExpression.Evaluate();
                if (nCalcResult.GetType() != typeof(bool)) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "if节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，运算结果不为bool",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，运算结果不为bool");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                bool ifResult = Convert.ToBoolean(nCalcResult);

                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "if节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = ifValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "if节点运行结果",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = ifResult,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "break标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "continue标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    }
                };

                if (!ifResult) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "该节点运算符为false，不执行if中运行块");
                    return TaskNodeStatus.kNone;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "if节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            Log.Verbose(data.ItemName + "Enter");

            //这里就是需要执行if中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                if (runStatus == TaskNodeStatus.kNone) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kSuccess) {

                }
                else if (runStatus == TaskNodeStatus.kFailure) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFailure;
                }
                else if (runStatus == TaskNodeStatus.kReturn) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kReturn;
                    Log.Verbose(data.ItemName + "子节点请求返回");
                    return TaskNodeStatus.kReturn;
                }
                else if (runStatus == TaskNodeStatus.kFlowStop) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }

                //获取全局变量中的该for节点状态，看看是否有外部对齐状态进行改变
                if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicElseIfNode)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果if中break标志位置为了true，那么要跳出该循环
                if (dicElseIfNode.Data_OutputParams.Count == 4 && dicElseIfNode.Data_OutputParams[2] != null && dicElseIfNode.Data_OutputParams[2].ActualParam.GetType() == typeof(bool)) {
                    bool breakStatus = Convert.ToBoolean(dicElseIfNode.Data_OutputParams[2].ActualParam);
                    if (breakStatus) {
                        Log.Verbose(data.ItemName + "break信号触发使得if提前结束");
                        break;
                    }
                }
                //如果if中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                if (dicElseIfNode.Data_OutputParams.Count == 4 && dicElseIfNode.Data_OutputParams[3] != null && dicElseIfNode.Data_OutputParams[3].ActualParam.GetType() == typeof(bool)) {
                    bool continueStatus = Convert.ToBoolean(dicElseIfNode.Data_OutputParams[3].ActualParam);
                    if (continueStatus) {
                        Log.Verbose(data.ItemName + "continue信号触发使得if提前结束");
                        break;
                    }
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
            bool elseifExecutionFlag = false;
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
                    if (tracingNode.Data_OutputParams.Count != 4) {
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
                    bool elseifResult = Convert.ToBoolean(tracingNode.Data_OutputParams[1].ActualParam);
                    //如果当前节点被执行了，那么执行标志位就置为true
                    if (elseifResult) {
                        elseifExecutionFlag = true;
                        break;
                    }
                }
                //如果当前节点是if那么就要结束了
                else if (tracingNode.OperationType == ItemOperationType.kIf) {
                    //如果当前else if的输出都不正常，那就需要停止当前流程运行
                    if (tracingNode.Data_OutputParams.Count != 4) {
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
                        elseifExecutionFlag = true;
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
            
            string ifValue = ObtainResultByExpression(data.FlowName, ifExpression);

            // 计算结果
            try {
                // 使用 NCalc 解析并计算表达式
                var nCalcExpression = new NCalc.Expression(ifValue);
                object nCalcResult = nCalcExpression.Evaluate();
                if (nCalcResult.GetType() != typeof(bool)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，运算结果不为bool");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                bool ifResult = Convert.ToBoolean(nCalcResult);

                //else if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = ifValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点运行结果",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = ifResult,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "break标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "continue标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    }
                };

                //判断是否需要串行执行，也就是之前是否有if或者elseif执行过了
                if (elseifExecutionFlag) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "None");
                    return TaskNodeStatus.kNone;
                }

                if (!ifResult) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "该节点运算符为false，不执行else if中运行块");
                    return TaskNodeStatus.kNone;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "else if节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            Log.Verbose(data.ItemName + "Enter");

            //这里就是需要执行if中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                if (runStatus == TaskNodeStatus.kNone) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kSuccess) {

                }
                else if (runStatus == TaskNodeStatus.kFailure) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFailure;
                }
                else if (runStatus == TaskNodeStatus.kReturn) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kReturn;
                    Log.Verbose(data.ItemName + "子节点请求返回");
                    return TaskNodeStatus.kReturn;
                }
                else if (runStatus == TaskNodeStatus.kFlowStop) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }

                //获取全局变量中的该for节点状态，看看是否有外部对齐状态进行改变
                if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicElseIfNode)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果elseif中break标志位置为了true，那么要跳出该循环
                if (dicElseIfNode.Data_OutputParams.Count == 4 && dicElseIfNode.Data_OutputParams[2] != null && dicElseIfNode.Data_OutputParams[2].ActualParam.GetType() == typeof(bool)) {
                    bool breakStatus = Convert.ToBoolean(dicElseIfNode.Data_OutputParams[2].ActualParam);
                    if (breakStatus) {
                        Log.Verbose(data.ItemName + "break信号触发使得else if提前结束");
                        break;
                    }
                }
                //如果elseif中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                if (dicElseIfNode.Data_OutputParams.Count == 4 && dicElseIfNode.Data_OutputParams[3] != null && dicElseIfNode.Data_OutputParams[3].ActualParam.GetType() == typeof(bool)) {
                    bool continueStatus = Convert.ToBoolean(dicElseIfNode.Data_OutputParams[3].ActualParam);
                    if (continueStatus) {
                        Log.Verbose(data.ItemName + "continue信号触发使得else if提前结束");
                        break;
                    }
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
                    if (tracingNode.Data_OutputParams.Count != 4) {
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
                    if (tracingNode.Data_OutputParams.Count != 4) {
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

            Log.Verbose(data.ItemName + "Enter");

            data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                new TaskOperationOutputParams(){
                    ParamName = "break标志位",
                    ParamType = LinkDataType.kBool,
                    LinkVisual = false,
                    ActualParam = false,
                },
                new TaskOperationOutputParams(){
                    ParamName = "continue标志位",
                    ParamType = LinkDataType.kBool,
                    LinkVisual = false,
                    ActualParam = false,
                },
            };

            //这里就是需要执行else中的内容的了
            //串行执行
            foreach (var childNode in data.Children) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }
                TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                if (runStatus == TaskNodeStatus.kNone) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (runStatus == TaskNodeStatus.kSuccess) {

                }
                else if (runStatus == TaskNodeStatus.kFailure) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFailure;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFailure;
                }
                else if (runStatus == TaskNodeStatus.kReturn) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kReturn;
                    Log.Verbose(data.ItemName + "子节点请求返回");
                    return TaskNodeStatus.kReturn;
                }
                else if (runStatus == TaskNodeStatus.kFlowStop) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                    return TaskNodeStatus.kFlowStop;
                }


                //获取全局变量中的该else节点状态，看看是否有外部对齐状态进行改变
                if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicElseNode)) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                    return TaskNodeStatus.kFlowStop;
                }
                //如果else中break标志位置为了true，那么要跳出该循环
                if (dicElseNode.Data_OutputParams.Count == 2 && dicElseNode.Data_OutputParams[0] != null && dicElseNode.Data_OutputParams[0].ActualParam.GetType() == typeof(bool)) {
                    bool breakStatus = Convert.ToBoolean(dicElseNode.Data_OutputParams[0].ActualParam);
                    if (breakStatus) {
                        Log.Verbose(data.ItemName + "break信号触发使得else提前结束");
                        break;
                    }
                }
                //如果else中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                if (dicElseNode.Data_OutputParams.Count == 2 && dicElseNode.Data_OutputParams[1] != null && dicElseNode.Data_OutputParams[1].ActualParam.GetType() == typeof(bool)) {
                    bool continueStatus = Convert.ToBoolean(dicElseNode.Data_OutputParams[1].ActualParam);
                    if (continueStatus) {
                        Log.Verbose(data.ItemName + "continue信号触发使得else提前结束");
                        break;
                    }
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
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kWhile) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为while");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            if (data.Data_InputParams.Count != 1) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，while节点的输入参数必须为1");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            data.Data_InputParams[0].ActualParam = data.Data_InputParams[0].UserParam;
            if (data.Data_InputParams[0].ActualParam == null || data.Data_InputParams[0].ActualParam.GetType() != typeof(string)) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，while节点的输入参数表达式必须为string类型且不为null");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //"KDouble(0.四则运算(1).2.double结果(2)) some other text KDouble(3.加法(2).4.double结果(5))"
            string whileExpression = data.Data_InputParams[0].ActualParam as string;

            string whileValue = ObtainResultByExpression(data.FlowName, whileExpression);

            // 计算结果
            try {
                // 使用 NCalc 解析并计算表达式
                var nCalcExpression = new NCalc.Expression(whileValue);
                object nCalcResult = nCalcExpression.Evaluate();
                if (nCalcResult.GetType() != typeof(bool)) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，运算结果不为bool",
                        },
                    };

                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，运算结果不为bool");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                bool whileResult = Convert.ToBoolean(nCalcResult);

                //while节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "while节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = whileValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "while节点运行结果",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = whileResult,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "break标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = "continue标志位",
                        ParamType = LinkDataType.kBool,
                        LinkVisual = false,
                        ActualParam = false,
                    }
                };

                if (!whileResult) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "该节点运算符为false，不执行while中运行块");
                    return TaskNodeStatus.kNone;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "while节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            Log.Verbose(data.ItemName + "Enter");

            while (true) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }

                // 计算结果
                try {
                    // 使用 NCalc 解析并计算表达式
                    var nCalcExpression = new NCalc.Expression(whileValue);
                    object nCalcResult = nCalcExpression.Evaluate();
                    if (nCalcResult.GetType() != typeof(bool)) {
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "while节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，运算结果不为bool",
                        },
                    };

                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，运算结果不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }

                    bool whileResult = Convert.ToBoolean(nCalcResult);

                    //while节点的输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "while节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = whileValue,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "while节点运行结果",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = whileResult,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "break标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "continue标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        }
                    };

                    if (!whileResult) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kNone;
                        Log.Verbose(data.ItemName + "该节点运算符为false，不再执行while中运行块");
                        return TaskNodeStatus.kNone;
                    }
                }
                catch (System.ArgumentException) {
                    //if节点的输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "while节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，符号非法",
                        },
                    };

                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，符号非法");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }


                //这里就是需要执行while中的内容的了
                //串行执行
                foreach (var childNode in data.Children) {
                    //点击了暂停
                    state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out state_Pause);
                    if (state_NoFindValue == false || state_Pause == false) {
                        break;
                    }

                    TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                    if (runStatus == TaskNodeStatus.kNone) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }
                    else if (runStatus == TaskNodeStatus.kSuccess) {

                    }
                    else if (runStatus == TaskNodeStatus.kFailure) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFailure;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFailure;
                    }
                    else if (runStatus == TaskNodeStatus.kReturn) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kReturn;
                        Log.Verbose(data.ItemName + "子节点请求返回");
                        return TaskNodeStatus.kReturn;
                    }
                    else if (runStatus == TaskNodeStatus.kFlowStop) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }

                    //获取全局变量中的该for节点状态，看看是否有外部对齐状态进行改变
                    if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicForNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    //如果for中break标志位置为了true，那么要跳出该循环
                    if (dicForNodeBreak.Data_OutputParams.Count == 8 && dicForNodeBreak.Data_OutputParams[6] != null && dicForNodeBreak.Data_OutputParams[6].ActualParam.GetType() == typeof(bool)) {
                        bool breakStatus = Convert.ToBoolean(dicForNodeBreak.Data_OutputParams[6].ActualParam);
                        if (breakStatus) {
                            stopwatch.Stop();
                            data.Time = stopwatch.ElapsedMilliseconds.ToString();
                            data.NodeStatus = TaskNodeStatus.kSuccess;
                            Log.Verbose(data.ItemName + "break信号触发使得while循环提前结束");
                            return TaskNodeStatus.kSuccess;
                        }
                    }
                    //如果for中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                    if (dicForNodeBreak.Data_OutputParams.Count == 8 && dicForNodeBreak.Data_OutputParams[7] != null && dicForNodeBreak.Data_OutputParams[7].ActualParam.GetType() == typeof(bool)) {
                        bool continueStatus = Convert.ToBoolean(dicForNodeBreak.Data_OutputParams[7].ActualParam);
                        if (continueStatus) {
                            Log.Verbose(data.ItemName + "continue信号触发使得while循环跳过一轮");
                            break;
                        }
                    }
                }

            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行for节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunForNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data.OperationType != ItemOperationType.kFor) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，该模块输入的执行类型不为for");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            if (data.Data_InputParams.Count != 1) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，for节点的输入参数必须为1");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            data.Data_InputParams[0].ActualParam = data.Data_InputParams[0].UserParam;
            if (data.Data_InputParams[0].ActualParam == null || data.Data_InputParams[0].ActualParam.GetType() != typeof(string)) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "软件框架出错，for节点的输入参数表达式必须为string类型且不为null");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //"KDouble(0.四则运算(1).2.double结果(2)) some other text KDouble(3.加法(2).4.double结果(5))"
            string forExpression = data.Data_InputParams[0].ActualParam as string;

            string forValue = ObtainResultByExpression(data.FlowName, forExpression);
            forValue = forValue.Replace("；", ";");
            string[] forValueArray = forValue.Split(';');
            if (forValueArray.Length != 3) {
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数表达式必须为三段，形如for(int i = 0; i < 10; i++)",
                    },
                };
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "for节点的输入参数表达式必须为三段，形如for(int i = 0; i < 10; i++)");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }

            //三段待解析的字符串
            string forValue01 = forValueArray[0].Trim();
            string forValue02 = forValueArray[1].Trim();
            string forValue03 = forValueArray[2].Trim();


            //************************解析第一段*****************************
            string forVariable01 = string.Empty;
            string forExpression01 = string.Empty;
            if (forValue01 == string.Empty) {
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第一段，形如int i = 0，暂时不支持其他形式",
                    },
                };
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，形如int i = 0，暂时不支持其他形式");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }


            //形如"int i = 0"，禁止稀奇古怪的
            string patternValue01 = @"(\w+)\s+(\w+)\s*=\s*(\w+)";
            // 使用正则表达式匹配字符串
            Match matchValue01 = Regex.Match(forValue01, patternValue01);
            if (!matchValue01.Success || matchValue01.Groups.Count != 4) {
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第一段，形如int i = 0，暂时不支持其他形式",
                    },
                };
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，形如int i = 0，暂时不支持其他形式");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }
            string forType01 = matchValue01.Groups[1].Value.Trim();   // "int"
            forVariable01 = matchValue01.Groups[2].Value.Trim(); // "i"
            forExpression01 = matchValue01.Groups[3].Value.Trim(); // "0 + 1"
            int forResult01 = 0;//初始i值

            if (forType01 != "int") {
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第一段，当前只支持定义int",
                    },
                };
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，当前只支持定义int");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }


            try {
                //解析value
                var nCalcExpression01 = new NCalc.Expression(forExpression01);
                object nCalcResult02 = nCalcExpression01.Evaluate();
                if (nCalcResult02.GetType() != typeof(int)) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第一段，运算结果不为int",
                        },
                    };
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，运算结果不为int");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                forResult01 = Convert.ToInt32(nCalcResult02);
                //for节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = forValue,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = forVariable01,
                        ParamType = LinkDataType.kInt,
                        ActualParam = forResult01,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = forVariable01,
                        ParamType = LinkDataType.kFloat,
                        ActualParam = (float)forResult01,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = forVariable01,
                        ParamType = LinkDataType.kDouble,
                        ActualParam = (double)forResult01,
                    },
                    new TaskOperationOutputParams(){
                        ParamName = forVariable01,
                        ParamType = LinkDataType.kString,
                        ActualParam = forResult01.ToString(),
                    },
                };
                //向全局变量中注册该变量

            }
            catch (ArgumentException) {
                //for节点的第一段输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第一段，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }


            //************************解析第二段*****************************
            string forValue02Expression = forValue02.Replace(forVariable01, forResult01.ToString());
            bool forResult02 = false;
            // 计算结果
            try {
                var nCalcExpression02 = new NCalc.Expression(forValue02Expression);
                object nCalcResult02 = nCalcExpression02.Evaluate();
                if (nCalcResult02.GetType() != typeof(bool)) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第二段，运算结果不为bool",
                        },
                    };
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第二段，运算结果不为bool");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                forResult02 = Convert.ToBoolean(nCalcResult02);

                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = forValue,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kInt,
                            ActualParam = forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kFloat,
                            ActualParam = (float)forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kDouble,
                            ActualParam = (double) forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kString,
                            ActualParam = forResult01.ToString(),
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "for节点运行结果",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = forResult02,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "break标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "continue标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                    };

                if (!forResult02) {
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kNone;
                    Log.Verbose(data.ItemName + "该节点运算结果为false，不执行for中运行块");
                    return TaskNodeStatus.kNone;
                }
            }
            catch (System.ArgumentException) {
                //if节点的输出
                data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第二段，符号非法",
                    },
                };

                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第二段，符号非法");
                return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
            }


            //************************解析第三段*****************************
            string forExpression03Type01 = forVariable01 + "++";
            string forExpression03Type02 = "++" + forVariable01;
            string forExpression03Type03 = @"^(\w+)\s*=\s*(.*)$";

            string forValue03copy = forValue03.Replace(" ", "");
            if (forValue03 == string.Empty) {

            }
            else if (forValue03copy == forExpression03Type01 || forValue03copy == forExpression03Type02) {

            }
            else {
                // 使用正则表达式匹配字符串
                Match matchValue03 = Regex.Match(forValue03, forExpression03Type03);
                if (!matchValue03.Success || matchValue03.Groups.Count != 3) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第三段，形如i = i + 1/i++/++i，暂时不支持其他形式",
                        },
                    };
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，形如i = i + 1/i++/++i，暂时不支持其他形式");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }
                string forVariable03 = matchValue03.Groups[1].Value.Trim();   // "i"
                string forExpression03 = matchValue03.Groups[2].Value.Trim(); // "i + 1"
                if (forVariable03 != forVariable01) {
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第三段，使用的变量" + forVariable03 + "与第一段定义的变量" + forVariable01 + "名称不一致",
                        },
                    };
                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，使用的变量" + forVariable03 + "与第一段定义的变量" + forVariable01 + "名称不一致");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                forExpression03 = forExpression03.Replace(forVariable01, forResult01.ToString());

                //解析value
                try {
                    var nCalcExpression03 = new NCalc.Expression(forExpression03);
                    object nCalcResult03 = nCalcExpression03.Evaluate();
                    if (nCalcResult03.GetType() != typeof(int)) {
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                            new TaskOperationOutputParams(){
                                ParamName = "for节点表达式",
                                ParamType = LinkDataType.kString,
                                LinkVisual = false,
                                ActualParam = "运算表达式出错，for节点的输入参数第三段，运算结果不为int",
                            },
                        };
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，运算结果不为int");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }

                    forResult01 = Convert.ToInt32(nCalcResult03);
                    //验证阶段不更新i

                }
                catch (ArgumentException) {
                    //for节点的第三段输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第三段，符号非法",
                        },
                    };

                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，符号非法");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

            }
            //************************根据解析运行子节点*****************************
            Log.Verbose(data.ItemName + "Enter");

            while (true) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }


                forValue02Expression = forValue02.Replace(forVariable01, forResult01.ToString());
                forResult02 = false;
                // 计算结果
                try {
                    var nCalcExpression02 = new NCalc.Expression(forValue02Expression);
                    object nCalcResult02 = nCalcExpression02.Evaluate();
                    if (nCalcResult02.GetType() != typeof(bool)) {
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                            new TaskOperationOutputParams(){
                                ParamName = "for节点表达式",
                                ParamType = LinkDataType.kString,
                                LinkVisual = false,
                                ActualParam = "运算表达式出错，for节点的输入参数第二段，运算结果不为bool",
                            },
                        };
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第二段，运算结果不为bool");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }

                    forResult02 = Convert.ToBoolean(nCalcResult02);

                    //for节点的输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = forValue,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kInt,
                            ActualParam = forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kFloat,
                            ActualParam = (float)forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kDouble,
                            ActualParam = (double)forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kString,
                            ActualParam = forResult01.ToString(),
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "for节点运行结果",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = forResult02,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "break标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "continue标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                    };

                    //这里要将字典里的数据更新一下，非常非常奇怪，如果不更新则字典中数据未更新，后续就链接不到，这玩意是c#的什么特性吗？理论上来说dicNode和node指针是一样的才对
                    //这里只手动更新输入输出，其他不重要的信息后续会自己同步更新？
                    if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicForNodeRefresh)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicForNodeRefresh.Data_OutputParams = data.Data_OutputParams;
                    dicForNodeRefresh.Data_InputParams = data.Data_InputParams;

                    if (!forResult02) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kSuccess;
                        Log.Verbose(data.ItemName + "Success，该节点当前运算结果为false，不再执行for中运行块");
                        return TaskNodeStatus.kSuccess;
                    }
                }
                catch (System.ArgumentException) {
                    //if节点的输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                    new TaskOperationOutputParams(){
                        ParamName = "for节点表达式",
                        ParamType = LinkDataType.kString,
                        LinkVisual = false,
                        ActualParam = "运算表达式出错，for节点的输入参数第二段，符号非法",
                    },
                };

                    stopwatch.Stop();
                    data.Time = stopwatch.ElapsedMilliseconds.ToString();
                    data.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第二段，符号非法");
                    return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                }

                //这里就是需要执行for中的内容的了
                //串行执行
                foreach (var childNode in data.Children) {
                    //点击了暂停
                    state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out state_Pause);
                    if (state_NoFindValue == false || state_Pause == false) {
                        break;
                    }

                    TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                    if (runStatus == TaskNodeStatus.kNone) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }
                    else if (runStatus == TaskNodeStatus.kSuccess) {

                    }
                    else if (runStatus == TaskNodeStatus.kFailure) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFailure;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFailure;
                    }
                    else if (runStatus == TaskNodeStatus.kReturn) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kReturn;
                        Log.Verbose(data.ItemName + "子节点请求返回");
                        return TaskNodeStatus.kReturn;
                    }
                    else if (runStatus == TaskNodeStatus.kFlowStop) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }

                    //获取全局变量中的该for节点状态，看看是否有外部对齐状态进行改变
                    if (!_linkDataDictinary.TryGetValue("[" + data.FlowName + "].[" + data.ItemName + "]", out TaskNode dicForNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    //如果for中break标志位置为了true，那么要跳出该循环
                    if (dicForNodeBreak.Data_OutputParams.Count == 8 && dicForNodeBreak.Data_OutputParams[6] != null && dicForNodeBreak.Data_OutputParams[6].ActualParam.GetType() == typeof(bool)) {
                        bool breakStatus = Convert.ToBoolean(dicForNodeBreak.Data_OutputParams[6].ActualParam);
                        if (breakStatus) {
                            stopwatch.Stop();
                            data.Time = stopwatch.ElapsedMilliseconds.ToString();
                            data.NodeStatus = TaskNodeStatus.kSuccess;
                            Log.Verbose(data.ItemName + "break信号触发使得for循环提前结束");
                            return TaskNodeStatus.kSuccess;
                        }
                    }
                    //如果for中continue标志位置为了true，那么要跳过这次循环运行，并且将continue标志位置为false
                    if (dicForNodeBreak.Data_OutputParams.Count == 8 && dicForNodeBreak.Data_OutputParams[7] != null && dicForNodeBreak.Data_OutputParams[7].ActualParam.GetType() == typeof(bool)) {
                        bool continueStatus = Convert.ToBoolean(dicForNodeBreak.Data_OutputParams[7].ActualParam);
                        if (continueStatus) {
                            Log.Verbose(data.ItemName + "continue信号触发使得for循环跳过一轮");
                            break;
                        }
                    }
                }

                //解析第三段
                if (forValue03 == string.Empty) {

                }
                else if (forValue03copy == forExpression03Type01 || forValue03copy == forExpression03Type02) {
                    forResult01 = forResult01 + 1;
                    //运算完更新输出
                    data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = forValue,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kInt,
                            ActualParam = forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kFloat,
                            ActualParam = (float)forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kDouble,
                            ActualParam = (double)forResult01,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = forVariable01,
                            ParamType = LinkDataType.kString,
                            ActualParam = forResult01.ToString(),
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "for节点运行结果",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = forResult02,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "break标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                        new TaskOperationOutputParams(){
                            ParamName = "continue标志位",
                            ParamType = LinkDataType.kBool,
                            LinkVisual = false,
                            ActualParam = false,
                        },
                    };
                }
                else {
                    // 使用正则表达式匹配字符串
                    Match matchValue03 = Regex.Match(forValue03, forExpression03Type03);
                    if (!matchValue03.Success || matchValue03.Groups.Count != 3) {
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                        new TaskOperationOutputParams(){
                            ParamName = "for节点表达式",
                            ParamType = LinkDataType.kString,
                            LinkVisual = false,
                            ActualParam = "运算表达式出错，for节点的输入参数第三段，形如i = i + 1/i++/++i，暂时不支持其他形式",
                        },
                    };
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第一段，形如i = i + 1/i++/++i，暂时不支持其他形式");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }
                    string forVariable03 = matchValue03.Groups[1].Value.Trim();   // "i"
                    string forExpression03 = matchValue03.Groups[2].Value.Trim(); // "i + 1"
                    if (forVariable03 != forVariable01) {
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                            new TaskOperationOutputParams(){
                                ParamName = "for节点表达式",
                                ParamType = LinkDataType.kString,
                                LinkVisual = false,
                                ActualParam = "运算表达式出错，for节点的输入参数第三段，使用的变量" + forVariable03 + "与第一段定义的变量" + forVariable01 + "名称不一致",
                            },
                        };
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，使用的变量" + forVariable03 + "与第一段定义的变量" + forVariable01 + "名称不一致");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }

                    forExpression03 = forExpression03.Replace(forVariable01, forResult01.ToString());

                    //解析value
                    var nCalcExpression03 = new NCalc.Expression(forExpression03);
                    try {
                        object nCalcResult03 = nCalcExpression03.Evaluate();
                        if (nCalcResult03.GetType() != typeof(int)) {
                            data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                                new TaskOperationOutputParams(){
                                    ParamName = "for节点表达式",
                                    ParamType = LinkDataType.kString,
                                    LinkVisual = false,
                                    ActualParam = "运算表达式出错，for节点的输入参数第三段，运算结果不为int",
                                },
                            };
                            stopwatch.Stop();
                            data.Time = stopwatch.ElapsedMilliseconds.ToString();
                            data.NodeStatus = TaskNodeStatus.kFlowStop;
                            Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，运算结果不为int");
                            return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                        }

                        forResult01 = Convert.ToInt32(nCalcResult03);

                        //运算完更新输出
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                            new TaskOperationOutputParams(){
                                ParamName = "for节点表达式",
                                ParamType = LinkDataType.kString,
                                LinkVisual = false,
                                ActualParam = forValue,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = forVariable01,
                                ParamType = LinkDataType.kInt,
                                ActualParam = forResult01,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = forVariable01,
                                ParamType = LinkDataType.kFloat,
                                ActualParam = (float)forResult01,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = forVariable01,
                                ParamType = LinkDataType.kDouble,
                                ActualParam = (double)forResult01,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = forVariable01,
                                ParamType = LinkDataType.kString,
                                ActualParam = forResult01.ToString(),
                            },
                            new TaskOperationOutputParams(){
                                ParamName = "for节点运行结果",
                                ParamType = LinkDataType.kBool,
                                LinkVisual = false,
                                ActualParam = forResult02,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = "break标志位",
                                ParamType = LinkDataType.kBool,
                                LinkVisual = false,
                                ActualParam = false,
                            },
                            new TaskOperationOutputParams(){
                                ParamName = "continue标志位",
                                ParamType = LinkDataType.kBool,
                                LinkVisual = false,
                                ActualParam = false,
                            },
                        };
                    }
                    catch (ArgumentException) {
                        //for节点的第三段输出
                        data.Data_OutputParams = new List<TaskOperationOutputParams>() {
                            new TaskOperationOutputParams(){
                                ParamName = "for节点表达式",
                                ParamType = LinkDataType.kString,
                                LinkVisual = false,
                                ActualParam = "运算表达式出错，for节点的输入参数第三段，符号非法",
                            },
                        };

                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "运算表达式出错，for节点的输入参数第三段，符号非法");
                        return TaskNodeStatus.kFlowStop;//算法引擎出错，整个工程停止
                    }

                }

            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行break节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunBreakNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //先判断前面的是不是if，不是就停止流程
            if (data == null || data.OperationType != ItemOperationType.kBreak) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，break输入节点为空或者执行类型不为break");
                return TaskNodeStatus.kFlowStop;
            }

            TaskNode taskNode = data;
            while (true) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }

                if (taskNode == null) {
                    stopwatch.Stop();
                    taskNode.Time = stopwatch.ElapsedMilliseconds.ToString();
                    taskNode.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(taskNode.ItemName + "该节点必须由while或者for包裹");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (taskNode.OperationType == ItemOperationType.kFor) {
                    //如果for运行标志位被外部break置为了false，那么也要停止运行
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicForNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicForNodeBreak.Data_OutputParams.Count != 8 || dicForNodeBreak.Data_OutputParams[6] == null || dicForNodeBreak.Data_OutputParams[6].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicForNodeBreak.Data_OutputParams[6].ActualParam = true;
                    break;
                }
                else if (taskNode.OperationType == ItemOperationType.kWhile) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicIfNodeBreak.Data_OutputParams.Count != 4 || dicIfNodeBreak.Data_OutputParams[2] == null || dicIfNodeBreak.Data_OutputParams[2].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicIfNodeBreak.Data_OutputParams[2].ActualParam = true;
                    break;
                }
                else if (taskNode.OperationType == ItemOperationType.kIf) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicIfNodeBreak.Data_OutputParams.Count != 4 || dicIfNodeBreak.Data_OutputParams[2] == null || dicIfNodeBreak.Data_OutputParams[2].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicIfNodeBreak.Data_OutputParams[2].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kElseIf) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicElseIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicElseIfNodeBreak.Data_OutputParams.Count != 4 || dicElseIfNodeBreak.Data_OutputParams[2] == null || dicElseIfNodeBreak.Data_OutputParams[2].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseIfNodeBreak.Data_OutputParams[2].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kElse) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicElseNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicElseNodeBreak.Data_OutputParams.Count != 2 || dicElseNodeBreak.Data_OutputParams[0] == null || dicElseNodeBreak.Data_OutputParams[0].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseNodeBreak.Data_OutputParams[0].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kParallel) {
                    Log.Error(data.ItemName + "不应该break出" + taskNode.ItemName + "范围");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (taskNode.OperationType == ItemOperationType.kSerial) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicSerialNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicSerialNodeBreak.Data_OutputParams.Count != 2 || dicSerialNodeBreak.Data_OutputParams[0] == null || dicSerialNodeBreak.Data_OutputParams[0].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicSerialNodeBreak.Data_OutputParams[0].ActualParam = true;
                }
                else {
                    
                }

                taskNode = taskNode.Parent;
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行continue节点
        /// </summary>
        /// <returns></returns>
        private TaskNodeStatus RunContinueNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //先判断前面的是不是if，不是就停止流程
            if (data == null || data.OperationType != ItemOperationType.kContinue) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，continue输入节点为空或者执行类型不为continue");
                return TaskNodeStatus.kFlowStop;
            }

            TaskNode taskNode = data;
            while (true) {
                //点击了暂停
                bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                if (state_NoFindValue == false || state_Pause == false) {
                    break;
                }

                if (taskNode == null) {
                    stopwatch.Stop();
                    taskNode.Time = stopwatch.ElapsedMilliseconds.ToString();
                    taskNode.NodeStatus = TaskNodeStatus.kFlowStop;
                    Log.Error(taskNode.ItemName + "该节点必须由while或者for包裹");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (taskNode.OperationType == ItemOperationType.kFor) {
                    //如果for运行标志位被外部break置为了false，那么也要停止运行
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicForNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicForNodeBreak.Data_OutputParams.Count != 8 || dicForNodeBreak.Data_OutputParams[7] == null || dicForNodeBreak.Data_OutputParams[7].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicForNodeBreak.Data_OutputParams[7].ActualParam = true;
                    break;
                }
                else if (taskNode.OperationType == ItemOperationType.kWhile) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicIfNodeBreak.Data_OutputParams.Count != 4 || dicIfNodeBreak.Data_OutputParams[3] == null || dicIfNodeBreak.Data_OutputParams[3].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicIfNodeBreak.Data_OutputParams[3].ActualParam = true;
                    break;
                }
                else if (taskNode.OperationType == ItemOperationType.kIf) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicIfNodeBreak.Data_OutputParams.Count != 4 || dicIfNodeBreak.Data_OutputParams[3] == null || dicIfNodeBreak.Data_OutputParams[3].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicIfNodeBreak.Data_OutputParams[3].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kElseIf) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicElseIfNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicElseIfNodeBreak.Data_OutputParams.Count != 4 || dicElseIfNodeBreak.Data_OutputParams[3] == null || dicElseIfNodeBreak.Data_OutputParams[3].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseIfNodeBreak.Data_OutputParams[3].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kElse) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicElseNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicElseNodeBreak.Data_OutputParams.Count != 2 || dicElseNodeBreak.Data_OutputParams[1] == null || dicElseNodeBreak.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicElseNodeBreak.Data_OutputParams[1].ActualParam = true;
                }
                else if (taskNode.OperationType == ItemOperationType.kParallel) {
                    Log.Error(data.ItemName + "不应该continue出" + taskNode.ItemName + "范围");
                    return TaskNodeStatus.kFlowStop;
                }
                else if (taskNode.OperationType == ItemOperationType.kSerial) {
                    if (!_linkDataDictinary.TryGetValue("[" + taskNode.FlowName + "].[" + taskNode.ItemName + "]", out TaskNode dicSerialNodeBreak)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "算法引擎出错，该节点中更新参数失败");
                        return TaskNodeStatus.kFlowStop;
                    }
                    if (dicSerialNodeBreak.Data_OutputParams.Count != 2 || dicSerialNodeBreak.Data_OutputParams[1] == null || dicSerialNodeBreak.Data_OutputParams[1].ActualParam.GetType() != typeof(bool)) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Error(data.ItemName + "检测到" + taskNode.ItemName + "运行状态异常");
                        return TaskNodeStatus.kFlowStop;
                    }
                    dicSerialNodeBreak.Data_OutputParams[1].ActualParam = true;
                }
                else { 
                
                }

                taskNode = taskNode.Parent;
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "Success");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行return节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus RunReturnNode(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            //先判断前面的是不是return，不是就停止流程
            if (data == null || data.OperationType != ItemOperationType.kReturn) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，return输入节点为空或者执行类型不为return");
                return TaskNodeStatus.kFlowStop;
            }


            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kReturn;
            Log.Verbose(data.ItemName + "Return");
            return TaskNodeStatus.kReturn;
        }

        /// <summary>
        /// 运行原路线恢复节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus RunOriginalResurvey(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data == null || data.OperationType != ItemOperationType.kOriginalResurvey) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，originalResurvey输入节点为空或者执行类型不为originalResurvey");
                return TaskNodeStatus.kFlowStop;
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "OriginalResurvey");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 运行切路线恢复节点
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private TaskNodeStatus RunSwitchResurvey(TaskNode data) {
            //计时开始
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            if (data == null || data.OperationType != ItemOperationType.kSwitchResurvey) {
                stopwatch.Stop();
                data.Time = stopwatch.ElapsedMilliseconds.ToString();
                data.NodeStatus = TaskNodeStatus.kFlowStop;
                Log.Error(data.ItemName + "算法引擎出错，switchResurvey输入节点为空或者执行类型不为switchResurvey");
                return TaskNodeStatus.kFlowStop;
            }

            if (data.EngineStatus == AlgoEngineStatus.kFlowOnceResurvey || data.EngineStatus == AlgoEngineStatus.kFlowLoopResurvey) {
                //这里就是需要执行切路线重测中的内容的了
                //串行执行
                foreach (var childNode in data.Children) {
                    //点击了暂停
                    bool state_NoFindValue = _threadDictinary.TryGetValue(data.FlowName, out bool state_Pause);
                    if (state_NoFindValue == false || state_Pause == false) {
                        break;
                    }
                    TaskNodeStatus runStatus = SwitchToCorrectNodeOperation(childNode);
                    if (runStatus == TaskNodeStatus.kNone) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }
                    else if (runStatus == TaskNodeStatus.kSuccess) {

                    }
                    else if (runStatus == TaskNodeStatus.kFailure) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFailure;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFailure;
                    }
                    else if (runStatus == TaskNodeStatus.kReturn) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kReturn;
                        Log.Verbose(data.ItemName + "子节点请求返回");
                        return TaskNodeStatus.kReturn;
                    }
                    else if (runStatus == TaskNodeStatus.kFlowStop) {
                        stopwatch.Stop();
                        data.Time = stopwatch.ElapsedMilliseconds.ToString();
                        data.NodeStatus = TaskNodeStatus.kFlowStop;
                        Log.Verbose(data.ItemName + "子节点出现问题导致停止执行");
                        return TaskNodeStatus.kFlowStop;
                    }
                }
            }

            stopwatch.Stop();
            data.Time = stopwatch.ElapsedMilliseconds.ToString();
            data.NodeStatus = TaskNodeStatus.kSuccess;
            Log.Verbose(data.ItemName + "SwitchResurvey");
            return TaskNodeStatus.kSuccess;
        }

        /// <summary>
        /// 总结全局输出数据到字典中
        /// </summary>
        private void SummarizeLinkDatas(List<FlowNode> processData) {
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
            _flowDataDictinary.Clear();
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
                _flowDataDictinary.TryAdd(flowNode.Name, flowNode);
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
        private object RetrieveDataByName(string flowName, string linkEdit) {
            string inputLinkStr = "[" + flowName + "].[" + linkEdit + "]";

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
        /// 根据表达式转化为可解析的字符串
        /// </summary>
        /// <returns></returns>
        private string ObtainResultByExpression(string flowName, string expression) {
            if (string.IsNullOrEmpty(expression)) { 
                return string.Empty;
            }

            // 正则表达式匹配形如 KDouble(数字.操作.数字.操作) 结构的模式
            string patternLink = @"([a-zA-Z]+)\((\d+)\.([^\.\(]+)\.(\d+)\.([^\)]+)\)";

            // 使用正则表达式匹配
            MatchCollection matches = Regex.Matches(expression, patternLink);

            string expressionValue = Regex.Replace(expression, patternLink, match => {
                object obj = RetrieveDataByName(flowName, match.Value);
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
            expressionValue = Regex.Replace(expressionValue, patternBool, match => match.Value.ToLower(), RegexOptions.IgnoreCase);

            return expressionValue;
        }

        private bool UpdateLinkEditByExpress(string flowName, TaskViewInputParams input) {
            if (!input.IsBind) {
                return false;
            }

            string inputLinkStr = "[" + flowName + "].[" + input.UserParam + "]";

            //[新建流程(0)].[KDouble(0.四则运算(1).2.double结果(2))]
            string pattern = @"\[(.*?)\]\.\[([^\(]+)\((.*?)\)\]";
            var match = Regex.Match(inputLinkStr, pattern);
            if (!match.Success) {
                return false;
            }

            // 提取第一部分 "新建流程(0)"
            string part1 = match.Groups[1].Value;
            // 提取第二部分 "KDouble" 
            string part2 = match.Groups[2].Value;
            // 提取括号内的内容 "0.四则运算(1).2.double结果(2)"
            string innerContent = match.Groups[3].Value;
            // 分割括号内的部分
            string[] parts = innerContent.Split(new[] { '.' }, StringSplitOptions.None);

            if (parts.Length == 4) {
                string part3 = parts[0]; // "0"
                string part4 = parts[1]; // "四则运算"
                string part5 = parts[2]; // "2"
                string part6 = parts[3]; // "double结果"


                if (!int.TryParse(part3, out int nodeId)) {
                    return false;
                }
                if (!int.TryParse(part5, out int paramId)) {
                    return false;
                }

                string taskLinkStr = "[" + part1 + "].[" + part3 + "." + part4 + "]";//[新建流程].[0.四则运算]

                if (!_linkDataDictinary.TryGetValue(taskLinkStr, out TaskNode node)) {
                    return false;
                }
                for (int i = 0; i < node.Data_OutputParams.Count; i++) {
                    if (i != paramId) {
                        continue;
                    }
                    if (part6 != node.Data_OutputParams[i].ParamName) {
                        continue;
                    }

                    node.Data_OutputParams[i].ActualParam = input.ActualParam;
                    var viewDatas = node.ItemView.Data_OutputParams;
                    viewDatas[i].ActualParam = input.ActualParam;
                    node.ItemView.Data_OutputParams = viewDatas;

                    _linkDataDictinary.AddOrUpdate("[" + node.FlowName + "].[" + node.ItemName + "]", node, (key, oldValue) => node);

                    return true;
                }
                return false;
            }

            return true;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return null;
            }
            else if (message.Function == "ProcessRunOnce" && message.Content is ValueTuple<List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_ProcessRunOnce) {
                ProcessRunStop();
                SummarizeLinkDatas(params_ProcessRunOnce.Item1);
                SummarizeSubView(params_ProcessRunOnce.Item2);
                SummarizeGlobalRes(params_ProcessRunOnce.Item3);
                ProcessRunOnce(params_ProcessRunOnce.Item1);
            }
            else if (message.Function == "ProcessRunLoop" && message.Content is ValueTuple<List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_ProcessRunLoop) {
                ProcessRunStop();
                SummarizeLinkDatas(params_ProcessRunLoop.Item1);
                SummarizeSubView(params_ProcessRunLoop.Item2);
                SummarizeGlobalRes(params_ProcessRunLoop.Item3);
                ProcessRunLoop(params_ProcessRunLoop.Item1);
            }
            else if (message.Function == "ProcessRunStop") {
                ProcessRunStop();
            }
            else if (message.Function == "FlowRunOnce" && message.Content is ValueTuple<FlowNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_FlowRunOnce) {
                ProcessRunStop();
                SummarizeLinkDatas(params_FlowRunOnce.Item2);
                SummarizeSubView(params_FlowRunOnce.Item3);
                SummarizeGlobalRes(params_FlowRunOnce.Item4);
                Task task = FlowRunOnce(params_FlowRunOnce.Item1);
            }
            else if (message.Function == "FlowRunLoop" && message.Content is ValueTuple<FlowNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_FlowRunLoop) {
                ProcessRunStop();
                SummarizeLinkDatas(params_FlowRunLoop.Item2);
                SummarizeSubView(params_FlowRunLoop.Item3);
                SummarizeGlobalRes(params_FlowRunLoop.Item4);
                Task task = FlowRunLoop(params_FlowRunLoop.Item1);
            }
            else if (message.Function == "FlowRunStop" && message.Content is string params_FlowRunStop) {
                FlowRunStop(params_FlowRunStop);
            }
            else if (message.Function == "FlowRunResurvey" && message.Content is string params_FlowRunResurvey) {
                FlowRunResurvey(params_FlowRunResurvey);
            }
            else if (message.Function == "NodeRunOnce" && message.Content is ValueTuple<TaskNode, List<FlowNode>, List<SubViewPluginBase>, List<ResourceOptionData>> params_NodeRunOnce) {
                ProcessRunStop();
                SummarizeLinkDatas(params_NodeRunOnce.Item2);
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
