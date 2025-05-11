

using LowCodePlatform.Engine;
using LowCodePlatform.Plugin;
using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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
using static LowCodePlatform.View.Base.CombinationArea_TreeItem;

namespace LowCodePlatform.View.Base
{
    /// <summary>
    /// item操作类别
    /// </summary>
    public enum ItemOperationType
    {
        /// <summary>
        /// 无
        /// </summary>
        kNone = 0,
        /// <summary>
        /// 常规使用
        /// 禁止拥有子项，但是允许打开编辑界面
        /// </summary>
        kCommon = 1,
        /// <summary>
        /// if
        /// 允许拥有子项，并且允许打开编辑界面
        /// </summary>
        kIf = 2,
        /// <summary>
        /// else if
        /// 允许拥有子项，并且允许打开编辑界面
        /// </summary>
        kElseIf = 3,
        /// <summary>
        /// else
        /// 允许拥有子项，但是禁止打开编辑界面
        /// </summary>
        kElse = 4,
        /// <summary>
        /// for
        /// 允许拥有子项，
        /// </summary>
        kFor = 5,
        /// <summary>
        /// while
        /// 允许拥有子项，并且允许打开编辑界面
        /// </summary>
        kWhile = 6,
        /// <summary>
        /// break
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kBreak = 7,
        /// <summary>
        /// continue
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kContinue = 8,
        /// <summary>
        /// 串行执行
        /// 允许拥有子项，并且禁止打开编辑界面
        /// </summary>
        kSerial = 9,
        /// <summary>
        /// 并行执行
        /// 允许拥有子项，并且禁止打开编辑界面
        /// </summary>
        kParallel = 10,
        /// <summary>
        /// 返回
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kReturn = 11,
        /// <summary>
        /// 停止运行当前流程
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kStopFlow = 12,
        /// <summary>
        /// 重新运行当前流程
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kReRunFlow = 13,
        /// <summary>
        /// 停止运行当前所有流程/整个工程
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kStopProcess = 14,
        /// <summary>
        /// 重新运行当前所有流程/整个工程
        /// 禁止拥有子项，并且禁止打开编辑界面
        /// </summary>
        kReRunProcess = 15,
    }

    /// <summary>
    /// CombinationArea_TreeItem.xaml 的交互逻辑
    /// 非运行状态，所有的数据存储在这里，因为如果单独建立一个数据结构去跟随treeview的变化，那么代价非常大，较为容易出错，还不如直接把数据放在设立，运行时再把数据收集成一棵树
    /// </summary>
    public partial class CombinationArea_TreeItem : UserControl {

        /// <summary>
        /// 当前item的序号
        /// </summary>
        public int SerialNumber
        {
            set {
                TextBlock_Num.Text = value.ToString();
            }
            get {
                return int.Parse(TextBlock_Num.Text);
            }
        }

        /// <summary>
        /// 当前item的内容
        /// </summary>
        public string ItemName
        {
            set {
                TextBlock_Name.Text = value;
            }
            get {
                return TextBlock_Name.Text;
            }
        }

        /// <summary>
        /// 当前item的开启状态
        /// </summary>
        public bool Enable{ set; get; } = true;

        private Dictionary<TaskNodeStatus, string> _nodeStatusText = new Dictionary<TaskNodeStatus, string>() {
            {TaskNodeStatus.kNone, "空置" },
            {TaskNodeStatus.kSuccess, "成功" },
            {TaskNodeStatus.kFailure, "失败" },
            {TaskNodeStatus.kReturn, "返回" },
            //{TaskNodeStatus.kProcessStop, "停止" },
            //{TaskNodeStatus.kProcessRerun, "重跑" },
            {TaskNodeStatus.kFlowStop, "停止" },
            {TaskNodeStatus.kRunning, "运行中" },
        };

        /// <summary>
        /// 当前item的运行状态，无/成功/失败/运行中
        /// </summary>
        public TaskNodeStatus NodeStatus
        {
            set {
                TextBlock_State.Text = _nodeStatusText[value];
            }
            get {
                foreach (var item in _nodeStatusText) {
                    if (item.Value == TextBlock_State.Text) { 
                        return item.Key;
                    }
                }
                return TaskNodeStatus.kNone;
            }
        }

        /// <summary>
        /// 当前item运行时间
        /// </summary>
        public string Time
        {
            set {
                if (int.TryParse(value, out int milliseconds) == false) {
                    TextBlock_Time.Text = value;
                    return;
                }
                else {
                    TextBlock_Time.Text = milliseconds + "ms";
                }
            }
            get {
                return TextBlock_Time.Text;
            }
        }



        /// <summary>
        /// 当前item的操作类型
        /// </summary>
        public ItemOperationType OperationType { set; get; } = ItemOperationType.kCommon;

        /// <summary>
        /// 当前item的注释
        /// </summary>
        public string ExplanatoryNote { 
            set {
                TextBlock_Note.Text = value;
            }
            get {
                return TextBlock_Note.Text;
            } 
        }

        /// <summary>
        /// ViewToJson存储的数据，以后要给到JsonToView，用于保存还原界面
        /// </summary>
        public string Data_JsonView { set; get; } = null;

        /// <summary>
        /// 输入参数集合,获取是深拷贝
        /// </summary>
        private List<TaskViewInputParams> _data_InputParams = new List<TaskViewInputParams>();
        public List<TaskViewInputParams> Data_InputParams { 
            set { 
                _data_InputParams = value;
            }
            get {
                List < TaskViewInputParams > result = new List < TaskViewInputParams > ();
                foreach (var item in _data_InputParams) {
                    result.Add (item.Clone());
                }
                return result;
            } }

        /// <summary>
        /// 输出参数集合，获取是深拷贝
        /// </summary>
        private List<TaskOperationOutputParams> _data_OutputParams = new List<TaskOperationOutputParams>();
        public List<TaskOperationOutputParams> Data_OutputParams {
            set {
                _data_OutputParams = value;
            }
            get {
                List<TaskOperationOutputParams> result = new List < TaskOperationOutputParams > ();
                foreach (var item in _data_OutputParams) {
                    result.Add(item.Clone());
                }
                return result;
            } }

        /// <summary>
        /// 当前item的语言类型
        /// </summary>
        public LangaugeType ItemLangaugeType { set; get; } = LangaugeType.kChinese;

        public delegate void DeleteItem(object sender, RoutedEventArgs e);
        private DeleteItem _deleteCallback = null;

        /// <summary>
        /// 初始化时候，需要把listbox删除一行的回调函数传递进来
        /// </summary>
        /// <param name="cb"></param>
        public CombinationArea_TreeItem() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
        }

        private void InitEvent() {
            RoutedEventHandler Event_MenuItem_Click = (s, e) => {
                ContextMenu_RightClickTip.IsOpen = false;
                var menuItem = s as MenuItem;
                if (menuItem == null) {
                    return;
                }
                string header = menuItem.Header as string;
                if (header == null) {
                    return;
                }
                else if (header == "备注") {
                    InputDialog inputDialog = new InputDialog() { Label = "备注", DefaultText = TextBlock_Note.Text };
                    inputDialog.ShowDialog();
                    string userInput = inputDialog.InputText;
                    if (userInput == null) {
                        return;
                    }
                    if (userInput == TextBlock_Note.Text) {
                        return;
                    }
                    TextBlock_Note.Text = userInput;
                }
                else if (header == "启用") {
                    Enable = true;
                    Grid_TreeItem.Background = Brushes.Transparent;
                }
                else if (header == "禁用") {
                    Enable = false;
                    Grid_TreeItem.Background = new SolidColorBrush(Colors.Gray);
                }
                else if (header == "删除") {
                    _deleteCallback?.Invoke(s, e);
                }
                else { 
                
                }
            };

            Grid_TreeItem.MouseRightButtonDown += (s, e) => {
                ContextMenu_RightClickTip.IsOpen = true;
            };

            MenuItem_Tip.Click += Event_MenuItem_Click;
            MenuItem_Enable.Click += Event_MenuItem_Click;
            MenuItem_Disable.Click += Event_MenuItem_Click;
            MenuItem_Delete.Click += Event_MenuItem_Click;
        }

        public void SetDeleteItemCallback(DeleteItem cb) {
            if (cb == null) { 
                return;
            }
            _deleteCallback = cb;
        }

        /// <summary>
        /// 收集界面信息用于保存
        /// </summary>
        /// <returns></returns>
        public string ViewToJson() {
            JObject json = new JObject();
            json["SerialNumber"] = SerialNumber;
            json["ItemName"] = ItemName;
            json["Enable"] = Enable;
            //json["WorkCondition"] = WorkCondition.ToString();
            //json["Time"] = Time;
            json["OperationType"] = OperationType.ToString();
            json["ExplanatoryNote"] = ExplanatoryNote;
            json["Data_JsonView"] = Data_JsonView;

            for (int i = 0; i < Data_InputParams.Count; i++) {
                json[i.ToString() + "_Data_InputParams"] = Data_InputParams[i].DataToJson();
            }
            json["count_InputParams"] = Data_InputParams.Count;


            for (int i = 0; i < Data_OutputParams.Count; i++) {
                json[i.ToString() + "_Data_OutputParams"] = Data_OutputParams[i].DataToJson();
            }
            json["count_OutputParams"] = Data_OutputParams.Count;

            return json.ToString();
        }

        /// <summary>
        /// 外部传递进来的json，来还原界面
        /// </summary>
        public void JsonToView(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            SerialNumber = ((int)json["SerialNumber"]);
            ItemName = json["ItemName"].ToString();
            Enable = ((bool)json["Enable"]);
            //WorkCondition = (ItemWorkCondition)Enum.Parse(typeof(ItemWorkCondition), json["WorkCondition"].ToString());
            //Time = json["Time"].ToString();
            OperationType = (ItemOperationType)Enum.Parse(typeof(ItemOperationType), json["OperationType"].ToString());
            ExplanatoryNote = json["ExplanatoryNote"].ToString();
            Data_JsonView = json["Data_JsonView"].ToString();

            Data_InputParams.Clear();
            int count_InputParams = ((int)json["count_InputParams"]);
            for (int i = 0; i < count_InputParams; i++) {
                TaskViewInputParams inputParams = new TaskViewInputParams();
                inputParams.JsonToData(json[i.ToString() + "_Data_InputParams"].ToString());
                _data_InputParams.Add(inputParams);
            }

            Data_OutputParams.Clear();
            int count_OutputParams = ((int)json["count_OutputParams"]);
            for (int i = 0; i < count_OutputParams; i++) {
                TaskOperationOutputParams outputParams = new TaskOperationOutputParams();
                outputParams.JsonToData(json[i.ToString() + "_Data_OutputParams"].ToString());
                _data_OutputParams.Add(outputParams);
            }
        }
    }
}
