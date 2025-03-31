using LowCodePlatform.Engine;
using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace LowCodePlatform.Plugin.Task_Control
{
    /// <summary>
    /// TaskView_While.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_While : System.Windows.Window, TaskViewPluginBase
    {

        /// <summary>
        /// 借用插件管理器的发送接口，能与大群通讯
        /// </summary>
        private SummarizeBeforeNodes _summarizeBeforeNodes = null;

        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_While() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            //切换到指定界面的输出
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

            TextBox_Expression.TextChanged += (s, e) => {
                TextBox_Describe.Text = "while(" + TextBox_Expression.Text + "){\n\t运行块\n\t'英文单引号'：指代字符串\n\tAbs：返回指定数字的绝对值。\n\tAcos ：返回余弦为指定数字的角度。\n\tAsin ：返回正弦为指定数字的角度。\n\tAtan ：返回其切线为指定数字的角度。\n\tCeiling ：返回大于或等于指定数字的最小整数。\n\tCos ：返回指定角度的余弦值。\n\tExp ：返回 e 提高到指定幂。\n\tFloor ：返回小于或等于指定数字的最大整数。\n\tIEEERemainder ：返回将指定数字除以另一个指定数字所产生的余数。两个参数\n\tLog ：返回指定数字的对数。两个参数，第一个参数是指定的值，第二个参数是指定的对数\n\tLog10 ：返回指定数字的 10 进制对数。\n\tMax ：返回两个指定数字中的较大者。两个参数\n\tMin ：返回两个数字中的较小者。两个参数\n\tPow ：返回提高到指定幂的指定数字。两个参数，第一个参数是幂数，第二个参数是指定数字\n\tRound ：将值舍入到最接近的整数或指定的小数位数。\n\tSign ：返回一个值，该值指示数字的符号。\n\tSin ：返回指定角度的正弦波。\n\tSqrt ：返回指定数字的平方根。\n\tTan ：返回指定角度的切线。\n\tTruncate ：计算数字的整数部分。" + "\n}";
            };

            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "表达式",
                    IsBind = false,
                    UserParam = TextBox_Expression.Text,
                });
                _confirmClick?.Invoke(inputParams);
            };

            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "表达式",
                    IsBind = false,
                    UserParam = TextBox_Expression.Text,
                });
                _executeClick?.Invoke(inputParams);
            };

            TextBox_Expression.Clear();//触发TextBox_Expression的函数
        }


        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams == null) {
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(string)) {
                return;
            }
            string ifValue = outputParams[0].ActualParam as string;
            TextBox_Result.Text = "while(" + ifValue + "){\n\t运行块\n}";
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(bool)) {
                return;
            }
            bool ifResult = Convert.ToBoolean(outputParams[1].ActualParam);
            TextBox_Result.Text = "while(" + ifValue + "){\n\t运行块\n}" + "\n\n" + ifResult.ToString();
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            JObject json = JObject.Parse(str);
            TextBox_Expression.Text = json["TextBox_Expression"].ToString();
        }

        public void ResetView() {
            ResetNodeDataSource();
            TextBox_Expression.Clear();
            TextBox_Result.Clear();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_Expression"] = TextBox_Expression.Text;
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
                    return "while";
                case LangaugeType.kEnglish:
                    return "while";
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
    }
}
