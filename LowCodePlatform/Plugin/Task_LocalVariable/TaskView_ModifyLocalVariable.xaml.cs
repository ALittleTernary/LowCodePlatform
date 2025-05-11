using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace LowCodePlatform.Plugin.Task_LocalVariable
{
    /// <summary>
    /// TaskView_ModifyLocalVariable.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_ModifyLocalVariable : System.Windows.Window, LinkEditTaskViewPluginBase
    {
        /// <summary>
        /// 借用插件管理器的发送接口，能与大群通讯
        /// </summary>
        private SummarizeBeforeNodes _summarizeBeforeNodes = null;

        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_ModifyLocalVariable() {
            InitializeComponent();
            InitEvent();
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
            if (inputParams == null) {
                return;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam == null || inputParams[0].ActualParam.GetType() != typeof(int)) {
                return;
            }
            int count = Convert.ToInt32(inputParams[0].ActualParam);
            ObservableCollection<VariableData> sourceDatas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            for (int i = 0; i < count; i++) {
                if (inputParams.Count < 2 + i * 3 || inputParams[1 + i * 3].ActualParam.GetType() != typeof(string)) {
                    return;
                }
                string variableType = inputParams[1 + i * 3].ActualParam as string;
                if (variableType != sourceDatas[i].VariableType) {
                    return;
                }
                if (inputParams.Count < 4 + i * 3) {
                    return;
                }
                if (variableType == "kInt") {
                    sourceDatas[i].VariableResult = Convert.ToInt32(inputParams[3 + i * 3].ActualParam).ToString();
                    sourceDatas[i].VariableStatus = true.ToString();
                }
                else if (variableType == "kFloat") {
                    sourceDatas[i].VariableResult = Convert.ToSingle(inputParams[3 + i * 3].ActualParam).ToString();
                    sourceDatas[i].VariableStatus = true.ToString();
                }
                else if (variableType == "kDouble") {
                    sourceDatas[i].VariableResult = Convert.ToDouble(inputParams[3 + i * 3].ActualParam).ToString();
                    sourceDatas[i].VariableStatus = true.ToString();
                }
                else if (variableType == "kString") {
                    sourceDatas[i].VariableResult = Convert.ToString(inputParams[3 + i * 3].ActualParam);
                    sourceDatas[i].VariableStatus = true.ToString();
                }
                else if (variableType == "kBool") {
                    sourceDatas[i].VariableResult = Convert.ToBoolean(inputParams[3 + i * 3].ActualParam).ToString();
                    sourceDatas[i].VariableStatus = true.ToString();
                }
                else {
                    sourceDatas[i].VariableStatus = false.ToString();
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
            TextBox_Expression.Clear();
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
                    return "修改局部变量";
                case LangaugeType.kEnglish:
                    return "ModifyLocalVariable";
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

        private void Event_DataGrid_SelectionItemChanged(object sender, SelectionChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            //先把数据清空
            TextBox_Expression.TextChanged -= Event_TextBox_Expression_TextChanged;

            TextBox_Expression.Clear();
            TextBox_Result.Clear();
            //再把数据更新
            TextBox_Expression.Text = selectData.VariableExpression;
            TextBox_Result.Text = selectData.VariableResult;

            TextBox_Expression.TextChanged += Event_TextBox_Expression_TextChanged;
        }

        /// <summary>
        /// 点击按钮添加一行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_AddItem(object sender, RoutedEventArgs e) {
            string linkDataText = _linkClick?.Invoke(LinkDataType.kVariable);
            ObservableCollection<VariableData> datas = DataGrid_Variable.ItemsSource as ObservableCollection<VariableData>;
            if (datas == null) {
                return;
            }

            // 使用正则表达式来匹配并拆分
            Regex regex = new Regex(@"^([^\(]+)\((.*)\)$");
            Match match = regex.Match(linkDataText);
            if (!match.Success || match.Groups.Count != 3) {
                return;
            }
            string part1 = match.Groups[1].Value; // KDouble
            string part2 = match.Groups[2].Value; // 0.四则运算(1).2.double结果(2)

            VariableData data = new VariableData() {
                VariableType = part1,
                VariableName = linkDataText,
            };
            datas.Add(data);
            DataGrid_Variable.SelectedItem = data;
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

        private void Event_TextBox_Expression_TextChanged(object sender, TextChangedEventArgs e) {
            VariableData selectData = DataGrid_Variable.SelectedItem as VariableData;
            if (selectData == null) {
                return;
            }
            selectData.VariableExpression = TextBox_Expression.Text;
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
                ParamName = "修改局部变量数量",
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
                    ParamName = i + "_VariableExpression",
                    IsBind = false,
                    UserParam = data.VariableExpression,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableName",
                    IsBind = true,
                    UserParam = data.VariableName,
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
                ParamName = "修改局部变量数量",
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
                    ParamName = i + "_VariableExpression",
                    IsBind = false,
                    UserParam = data.VariableExpression,
                });
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = i + "_VariableName",
                    IsBind = true,
                    UserParam = data.VariableName,
                });
            }
            _executeClick?.Invoke(inputParams);
        }
    }
}
