using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_BlobAnalysis;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing.Drawing2D;
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

namespace LowCodePlatform.Plugin.Task_LocalVariable
{
    public class VariableData : INotifyPropertyChanged
    {
        private int _variableNum;
        public int VariableNum
        {
            get { return _variableNum; }
            set {
                _variableNum = value;
                OnPropertyChanged(nameof(VariableNum));
            }
        }

        private string _variableType;
        public string VariableType
        {
            get { return _variableType; }
            set {
                _variableType = value;
                OnPropertyChanged(nameof(VariableType));
            }
        }

        private string _variableName;
        public string VariableName
        {
            get { return _variableName; }
            set {
                _variableName = value;
                OnPropertyChanged(nameof(VariableName));
            }
        }

        private string _variableExpression;
        public string VariableExpression
        {
            get { return _variableExpression; }
            set {
                _variableExpression = value;
                OnPropertyChanged(nameof(VariableExpression));
            }
        }

        private string _variableTip;
        public string VariableTip
        {
            get { return _variableTip; }
            set {
                _variableTip = value;
                OnPropertyChanged(nameof(VariableTip));
            }
        }

        private string _variableStatus;
        public string VariableStatus
        {
            get { return _variableStatus; }
            set {
                _variableStatus = value;
                OnPropertyChanged(nameof(VariableStatus));
            }
        }

        private string _variableResult;
        public string VariableResult
        {
            get { return _variableResult; }
            set {
                _variableResult = value;
                OnPropertyChanged(nameof(VariableResult));
            }
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["VariableNum"] = VariableNum;
            json["VariableType"] = VariableType;
            json["VariableName"] = VariableName;
            json["VariableExpression"] = VariableExpression;
            json["VariableTip"] = VariableTip;
            json["VariableStatus"] = VariableStatus;
            json["VariableResult"] = VariableResult;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            VariableNum = ((int)json["VariableNum"]);
            VariableType = (json["VariableType"].ToString());
            VariableName = (json["VariableName"].ToString());
            VariableExpression = json["VariableExpression"].ToString();
            VariableTip = (json["VariableTip"].ToString());
            VariableStatus = json["VariableStatus"].ToString();
            VariableResult = json["VariableResult"].ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// TaskView_CreateLocalVariable.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_CreateLocalVariable : System.Windows.Window, LinkEditTaskViewPluginBase
    {
        /// <summary>
        /// 借用插件管理器的发送接口，能与大群通讯
        /// </summary>
        private SummarizeBeforeNodes _summarizeBeforeNodes = null;

        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_CreateLocalVariable() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            MenuItem menuItem_Int = new MenuItem() { Header = LinkDataType.kInt.ToString() };
            MenuItem menuItem_Float = new MenuItem() { Header = LinkDataType.kFloat.ToString() };
            MenuItem menuItem_Double = new MenuItem() { Header = LinkDataType.kDouble.ToString() };
            MenuItem menuItem_Bool = new MenuItem() { Header = LinkDataType.kBool.ToString() };
            MenuItem menuItem_String = new MenuItem() { Header = LinkDataType.kString.ToString() };
            menuItem_Int.Click += Event_MenuItem_Click;
            menuItem_Float.Click += Event_MenuItem_Click;
            menuItem_Double.Click += Event_MenuItem_Click;
            menuItem_Bool.Click += Event_MenuItem_Click;
            menuItem_String.Click += Event_MenuItem_Click;
            ContextMenu_Variable.Items.Add(menuItem_Int);
            ContextMenu_Variable.Items.Add(menuItem_Float);
            ContextMenu_Variable.Items.Add(menuItem_Double);
            ContextMenu_Variable.Items.Add(menuItem_Bool);
            ContextMenu_Variable.Items.Add(menuItem_String);
        }

        private void InitEvent() {
            Button_AddItem.Click += Event_Button_AddItem;
            Button_SubItem.Click += Event_Button_DeleteItem;
            Button_MoveUp.Click += Event_Button_MoveUpItem;
            Button_MoveDown.Click += Event_Button_MoveDownItem;
            DataGrid_Variable.SelectionChanged += Event_DataGrid_SelectionItemChanged;
            Button_Execute.Click += Event_Button_Execute_Click;
            Button_Confirm.Click += Event_Button_Confirm_Click;
            TreeView_Node.MouseLeftButtonUp += (s, e) => {
                TreeViewItem selectItem = TreeView_Node.SelectedItem as TreeViewItem;
                if (selectItem == null) {
                    return;
                }
                for (int i = 0; i < TabControl_NodeOutput.Items.Count; i++) {
                    TabItem item = TabControl_NodeOutput.Items[i] as TabItem;
                    if (item == null) {
                        continue;
                    }
                    if (item.Header != selectItem.Header) {
                        continue;
                    }
                    TabControl_NodeOutput.SelectedIndex = i;
                    break;
                }
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(int)) {
                return;
            }
            int count = Convert.ToInt32(outputParams[0].ActualParam);

            ObservableCollection<VariableData> sourceDatas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (count != sourceDatas.Count) {
                return;
            }
            for (int i = 0; i < count; i++) {
                VariableData data = sourceDatas[i];
                if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam == null) {
                    data.VariableStatus = false.ToString();
                }
                else if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam.GetType() == typeof(int) && data.VariableType == LinkDataType.kInt.ToString()) {
                    data.VariableStatus = true.ToString();
                    int result = Convert.ToInt32(outputParams[1 + i].ActualParam);
                    data.VariableResult = result.ToString();
                }
                else if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam.GetType() == typeof(float) && data.VariableType == LinkDataType.kFloat.ToString()) {
                    data.VariableStatus = true.ToString();
                    float result = Convert.ToSingle(outputParams[1 + i].ActualParam);
                    data.VariableResult = result.ToString();
                }
                else if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam.GetType() == typeof(double) && data.VariableType == LinkDataType.kDouble.ToString()) {
                    data.VariableStatus = true.ToString();
                    double result = Convert.ToDouble(outputParams[1 + i].ActualParam);
                    data.VariableResult = result.ToString();
                }
                else if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam.GetType() == typeof(string) && data.VariableType == LinkDataType.kString.ToString()) {
                    data.VariableStatus = true.ToString();
                    string result = Convert.ToString(outputParams[1 + i].ActualParam);
                    data.VariableResult = result;
                }
                else if (outputParams.Count >= 2 + i && outputParams[1 + i].ActualParam.GetType() == typeof(bool) && data.VariableType == LinkDataType.kBool.ToString()) {
                    data.VariableStatus = true.ToString();
                    bool result = Convert.ToBoolean(outputParams[1 + i].ActualParam);
                    data.VariableResult = result.ToString();
                }
                else { 
                    return;
                }
            }
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            JObject json = JObject.Parse(str);
            int sourceDatas_Count = ((int)json["DataGrid_Variable_Count"]);
            ObservableCollection<VariableData> datas = new ObservableCollection<VariableData>();
            for (int i = 0; i < sourceDatas_Count; i++) {
                string variableItem = json["DataGrid_Variable_" + i].ToString();
                VariableData data = new VariableData();
                data.JsonToData(variableItem);
                datas.Add(data);
            }
            DataGrid_Variable.ItemsSource = datas;
        }

        public void ResetView() {
            TextBox_Name.Clear();
            TextBox_Expression.Clear();
            TextBox_Tip.Clear();
            TextBox_Result.Clear();
            ResetNodeDataSource();
            DataGrid_Variable.ItemsSource = new ObservableCollection<VariableData>();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            // 获取 ItemsSource 的数据源
            ObservableCollection<VariableData> sourceDatas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            json["DataGrid_Variable_Count"] = sourceDatas.Count;
            for (int i = 0; i < sourceDatas.Count; i++) {
                json["DataGrid_Variable_" + i] = sourceDatas[i].DataToJson();
            }
            return json.ToString();
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
        }

        /// <summary>
        /// 获取之前所有节点信息
        /// </summary>
        /// <param name="cb"></param>
        public void SetSummarizeBeforeNodesCallback(SummarizeBeforeNodes cb) {
            if (cb == null) {
                return;
            };
            _summarizeBeforeNodes = cb;
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "创建局部变量";
                case LangaugeType.kEnglish:
                    return "CreateLocalVariable";
                default:
                    break;
            }
            return string.Empty;
        }

        /// <summary>
        /// 重置节点数据源
        /// </summary>
        private void ResetNodeDataSource() {
            //双击行数据更新到textbox中
            MouseButtonEventHandler event_DataGrid_DoubleClickRow = (sender, e) => {
                // 获取点击位置的源控件
                var hit = e.OriginalSource as DependencyObject;

                DataGridRow row = null;
                while (hit != null) {
                    if (hit is DataGridRow) {
                        row = (DataGridRow)hit;
                    }
                    hit = VisualTreeHelper.GetParent(hit);
                }
                // 判断点击源是否是 DataGridRow
                if (row == null) {
                    return;
                }
                DataGrid eventDataGrid = sender as DataGrid;

                // 获取被双击的行（通过 VisualTreeHelper 查找当前被选中的行）
                LinkData selectedData = eventDataGrid.SelectedItem as LinkData;

                if (selectedData == null) {
                    return;
                }

                TabItem contentItem = TabControl_NodeOutput.SelectedItem as TabItem;
                if (contentItem == null) {
                    return;
                }

                string linkDataText = selectedData.DataType + "(" + contentItem.Header.ToString() + "." + selectedData.DataName + ")";
                // 获取当前光标的位置
                int caretIndex = TextBox_Expression.CaretIndex;
                // 将光标前插入字符串
                TextBox_Expression.Text = TextBox_Expression.Text.Insert(caretIndex, linkDataText);
                // 更新光标位置，防止光标位置改变
                TextBox_Expression.CaretIndex = caretIndex + linkDataText.Length;
            };

            //根据链接类型和节点信息创建每个节点的链接界面
            Func<TaskNode, DataGrid> func_CreateDataGridItem = (TaskNode node) => {
                DataGrid dataGrid = new DataGrid() {
                    IsReadOnly = true,
                    RowHeight = 30,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    AutoGenerateColumns = false,
                };
                //第一行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding("DataType"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                //第二行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "内容", Binding = new Binding("DataName"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                //第三行需要定制，待办
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "其他", Binding = new Binding("DataOperation"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
                //第四行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "描述", Binding = new Binding("DataDescription"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });

                ObservableCollection<LinkData> datas = new ObservableCollection<LinkData>();
                //遍历所有输出
                for (int i = 0; i < node.Data_OutputParams.Count; i++) {
                    TaskOperationOutputParams outputParam = node.Data_OutputParams[i];
                    if (!outputParam.LinkVisual) {
                        continue;
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(int)) {
                        datas.Add(new LinkData() {
                            DataType = LinkDataType.kInt.ToString(),
                            DataName = i + "." + outputParam.ParamName,
                            DataOperation = string.Empty,
                            DataDescription = outputParam.Description,
                        });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<int>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(float)) {
                        datas.Add(new LinkData() {
                            DataType = LinkDataType.kFloat.ToString(),
                            DataName = i + "." + outputParam.ParamName,
                            DataOperation = string.Empty,
                            DataDescription = outputParam.Description,
                        });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<float>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(double)) {
                        datas.Add(new LinkData() {
                            DataType = LinkDataType.kDouble.ToString(),
                            DataName = i + "." + outputParam.ParamName,
                            DataOperation = string.Empty,
                            DataDescription = outputParam.Description,
                        });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<double>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(string)) {
                        datas.Add(new LinkData() {
                            DataType = LinkDataType.kString.ToString(),
                            DataName = i + "." + outputParam.ParamName,
                            DataOperation = string.Empty,
                            DataDescription = outputParam.Description,
                        });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<string>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(bool)) {
                        datas.Add(new LinkData() {
                            DataType = LinkDataType.kBool.ToString(),
                            DataName = i + "." + outputParam.ParamName,
                            DataOperation = string.Empty,
                            DataDescription = outputParam.Description,
                        });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<bool>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(Mat)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<Mat>)) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(Mat[])) {

                    }
                    //其他类型，也就是object
                    else {

                    }
                }
                dataGrid.ItemsSource = datas;
                //双击以后给输出的赋值，并且关闭链接选择界面
                dataGrid.MouseDoubleClick += event_DataGrid_DoubleClickRow;
                return dataGrid;
            };

            //递归创建树结构
            Func<TaskNode, TreeViewItem> func_CreateTreeViewItem = null;
            func_CreateTreeViewItem = (node) => {
                if (node == null) {
                    return null;
                }
                var treeViewItem = new TreeViewItem { Header = node.ItemName, IsExpanded = true, ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
                TabControl_NodeOutput.Items.Add(new TabItem() {
                    Header = node.ItemName,
                    Visibility = Visibility.Collapsed,
                    Content = func_CreateDataGridItem(node),
                });
                //完全迭代树结构
                foreach (var child in node.Children) {
                    var partItem = func_CreateTreeViewItem(child);
                    if (partItem == null) {
                        continue;
                    }
                    treeViewItem.Items.Add(partItem);
                }
                return treeViewItem;
            };

            //给流程中的每个元素在左边创建节点，右边创建其对应的输出
            TreeView_Node.Items.Clear();
            TabControl_NodeOutput.Items.Clear();
            //每次打开界面时刷新界面
            FlowNode flowData = _summarizeBeforeNodes?.Invoke();
            if (flowData == null) {
                return;
            }
            foreach (TaskNode node in flowData.Children) {
                var item = func_CreateTreeViewItem(node);
                if (item == null) {
                    continue;
                }
                TreeView_Node.Items.Add(item);
            }
        }

        private void Event_MenuItem_Click(object sender, RoutedEventArgs e) {
            ContextMenu_Variable.IsOpen = false;
            var menuItem = sender as MenuItem;
            if (menuItem == null) {
                return;
            }
            string menuHeader = menuItem.Header as string;
            if (menuHeader == null) { 
                return ;
            }
            if (!Enum.TryParse(menuHeader, out LinkDataType menuType)) {
                return;
            }

            string variableName = string.Empty;
            switch (menuType) {
                case LinkDataType.kInt:
                    variableName = "0";
                    break;
                case LinkDataType.kFloat:
                    variableName = "0.0";
                    break;
                case LinkDataType.kDouble:
                    variableName = "0.0";
                    break;
                case LinkDataType.kString:
                    variableName = "";
                    break;
                case LinkDataType.kBool:
                    variableName = "Flase";
                    break;
                default:
                    return;
            }

            // 创建一个新的 VariableData 对象
            VariableData data = new VariableData() {
                VariableNum = 0,
                VariableType = menuHeader,
                VariableName = "局部变量名",
                VariableExpression = variableName,
                VariableTip = string.Empty,
            };

            // 获取 ItemsSource 的数据源
            ObservableCollection<VariableData> sourceDatas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (sourceDatas == null) {
                return;
            }
            sourceDatas.Add(data);

            //设置当前选中项为新增的项，把数据显示一下，这个会触发Event_DataGrid_SelectionItemChanged
            DataGrid_Variable.SelectedItem = data;
        }

        private void Event_DataGrid_SelectionItemChanged(object sender, SelectionChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            //先把数据清空
            TextBox_Name.TextChanged -= Event_TextBox_Name_TextChanged;
            TextBox_Expression.TextChanged -= Event_TextBox_Expression_TextChanged;
            TextBox_Tip.TextChanged -= Event_TextBox_Tip_TextChanged;

            TextBox_Name.Clear();
            TextBox_Expression.Clear();
            TextBox_Tip.Clear();
            TextBox_Result.Clear();
            //再把数据更新
            TextBox_Name.Text = selectData.VariableName;
            TextBox_Expression.Text = selectData.VariableExpression;
            TextBox_Tip.Text = selectData.VariableTip;
            TextBox_Result.Text = selectData.VariableResult;

            TextBox_Name.TextChanged += Event_TextBox_Name_TextChanged;
            TextBox_Expression.TextChanged += Event_TextBox_Expression_TextChanged;
            TextBox_Tip.TextChanged += Event_TextBox_Tip_TextChanged;
        }

        /// <summary>
        /// 点击按钮添加一行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_AddItem(object sender, RoutedEventArgs e) {
            ContextMenu_Variable.IsOpen = true;
        }

        /// <summary>
        /// 点击按钮删除当前选中的item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_DeleteItem(object sender, RoutedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            // 假设你的数据集合是 ObservableCollection<T>
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (datas == null) {
                return;
            }
            datas.Remove(selectData);  // 删除选中的项

            //重新整理序号
            for (int i = 0; i < datas.Count; i++) {
                datas[i].VariableNum = i;
            }
        }

        /// <summary>
        /// 点击按钮上移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveUpItem(object sender, RoutedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (datas == null) {
                return;
            }
            int index = datas.IndexOf(selectData);
            // 如果选中的项不是第一个元素
            if (index <= 0) {
                return;
            }
            // 将选中的项和前一个项交换位置
            datas.RemoveAt(index);
            datas.Insert(index - 1, selectData);
            // 更新选中项
            DataGrid_Variable.SelectedItem = selectData;
            //重新整理序号
            for (int i = 0; i < datas.Count; i++) {
                datas[i].VariableNum = i;
            }
        }

        /// <summary>
        /// 点击按钮下移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveDownItem(object sender, RoutedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (datas == null) {
                return;
            }
            int index = datas.IndexOf(selectData);
            // 如果选中的项不是最后一个元素
            if (index >= datas.Count - 1) {
                return;
            }
            // 将选中的项和下一个项交换位置
            datas.RemoveAt(index);
            datas.Insert(index + 1, selectData);

            // 更新选中项
            DataGrid_Variable.SelectedItem = selectData;
            //重新整理序号
            for (int i = 0; i < datas.Count; i++) {
                datas[i].VariableNum = i;
            }
        }

        private void Event_TextBox_Name_TextChanged(object sender, TextChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            selectData.VariableName = TextBox_Name.Text;
        }

        private void Event_TextBox_Expression_TextChanged(object sender, TextChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            selectData.VariableExpression = TextBox_Expression.Text;
        }

        private void Event_TextBox_Tip_TextChanged(object sender, TextChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            selectData.VariableTip = TextBox_Tip.Text;
        }

        /// <summary>
        /// 点击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Confirm_Click(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "新建局部变量数量",
                IsBind = false,
                UserParam = datas.Count,
            });
            for (int i = 0; i < datas.Count; i++) {
                VariableData data = datas[i];
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableType",
                    IsBind = false,
                    UserParam = data.VariableType,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableName",
                    IsBind = false,
                    UserParam = data.VariableName,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableExpression",
                    IsBind = false,
                    UserParam = data.VariableExpression,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableTip",
                    IsBind = false,
                    UserParam = data.VariableTip,
                });
            }
            _confirmClick?.Invoke(inputParams);
        }

        /// <summary>
        /// 点击执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Execute_Click(object sender, RoutedEventArgs e) {
            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "新建局部变量数量",
                IsBind = false,
                UserParam = datas.Count,
            });
            for (int i = 0; i < datas.Count; i++) {
                VariableData data = datas[i];
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableType",
                    IsBind = false,
                    UserParam = data.VariableType,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableName",
                    IsBind = false,
                    UserParam = data.VariableName,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableExpression",
                    IsBind = false,
                    UserParam = data.VariableExpression,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableTip",
                    IsBind = false,
                    UserParam = data.VariableTip,
                });
            }
            _executeClick?.Invoke(inputParams);
        }
    }
}
