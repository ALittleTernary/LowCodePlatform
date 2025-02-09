using LowCodePlatform.Engine;
using LowCodePlatform.View;
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
using static OpenCvSharp.ML.DTrees;

namespace LowCodePlatform.Plugin.Base
{
    // 数据模型类
    public class DataItem
    {
        public string Column1 { get; set; }
        public string Column2 { get; set; }
        public string Column4 { get; set; }
    }

    /// <summary>
    /// LinkSelectView.xaml 的交互逻辑
    /// </summary>
    public partial class LinkSelectView : System.Windows.Window
    {
        private const string _title = "类型数据的链接器";

        /// <summary>
        /// 链接得到的信息
        /// </summary>
        public string LinkDataText { get; set; } = string.Empty;


        /// <summary>
        /// 链接的类型
        /// </summary>
        private LinkDataType _linkDataType = LinkDataType.kNone;
        public LinkDataType LinkDataType { 
            get { 
                return _linkDataType;
            }
            set {
                _linkDataType = value;
                Title = _linkDataType.ToString() + _title;
            } }

        /// <summary>
        /// 构造函数
        /// </summary>
        public LinkSelectView() {
            InitializeComponent();
            Title = LinkDataType.ToString() + _title;
            InitEvent();
        }

        private void InitEvent() {
            TreeView_Catalogue.MouseLeftButtonUp += Event_TreeView_SwitchToTab;


        }

        /// <summary>
        /// 初始化界面类型插件的选项
        /// </summary>
        /// <param name="subViews"></param>
        public void InitSubOption(string taskName, List<SubViewPluginBase> subViews) {
            if (LinkDataType != LinkDataType.kView) { 
                return;
            }
            DataGrid dataGrid = new DataGrid() {
                IsReadOnly = true,
                RowHeight = 30,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
            };
            //第一行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding("String1"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第二行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "界面名称", Binding = new Binding("String2"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第三行需要定制，待办
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "对应任务插件", Binding = new Binding("String3"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第四行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "描述", Binding = new Binding("String4"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //界面
            foreach (SubViewPluginBase subView in subViews) {
                List<string> allowLinkTasks = subView.AllowTaskPluginLink();
                List<string> taskNames = new List<string>();
                //判断当前界面允许列表中是否有这个task插件
                foreach (var allowTask in allowLinkTasks) {
                    if (allowTask == taskName) {
                        taskNames.Add(allowTask);
                    }
                    else { 
                        continue;
                    }
                }
                if (taskNames.Count == 0) {
                    continue;
                }
                dataGrid.Items.Add(new { 
                    String1 = LinkDataType.kView.ToString(), 
                    String2 = subView.GetUniqueName(LangaugeType.kChinese), 
                    String3 = string.Join(",", taskNames), 
                    String4 = "特定任务插件才能使用" 
                });
            }
            //双击以后给输出的赋值，并且关闭链接选择界面
            dataGrid.MouseDoubleClick += Event_DataGrid_DoubleClickRow;
            TreeView_Catalogue.Items.Add(new TreeViewItem {
                Header = "SubView",
                IsExpanded = true,
                ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle"),
            });
            TabControl_Content.Items.Add(new TabItem() {
                Header = "SubView",
                Visibility = Visibility.Collapsed,
                Content = dataGrid,
            });
        }

        /// <summary>
        /// 初始化任务类型插件的选项
        /// </summary>
        /// <param name="flowData"></param>
        public void InitTaskOption(FlowNode flowData) {
            //根据链接类型和节点信息创建每个节点的链接界面
            Func<TaskNode, DataGrid> func_CreateDataGrid = (TaskNode node) => {
                DataGrid dataGrid = new DataGrid() { 
                    IsReadOnly = true,
                    RowHeight = 30,
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                };
                //第一行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding("String1"), Width = new DataGridLength(1, DataGridLengthUnitType.Star)});
                //第二行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "内容", Binding = new Binding("String2"), Width = new DataGridLength(1, DataGridLengthUnitType.Star)});
                //第三行需要定制，待办
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "其他", Binding = new Binding("String3"), Width = new DataGridLength(1, DataGridLengthUnitType.Star)});
                //第四行
                dataGrid.Columns.Add(new DataGridTextColumn { Header = "描述", Binding = new Binding("String4"), Width = new DataGridLength(1, DataGridLengthUnitType.Star)});

                //遍历所有输出
                for (int i = 0; i < node.Data_OutputParams.Count; i++) {
                    TaskOperationOutputParams outputParam = node.Data_OutputParams[i];
                    if (outputParam.ActualParam.GetType() == typeof(int) && LinkDataType == LinkDataType.kInt) {
                        dataGrid.Items.Add(new {String1 = LinkDataType.kInt.ToString(),String2 = i + "." +outputParam.ParamName,String3 = "" , String4 = outputParam.Description  });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<int>) && LinkDataType == LinkDataType.kListInt) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(float) && LinkDataType == LinkDataType.kFloat) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kFloat.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<float>) && LinkDataType == LinkDataType.kListFloat) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(double) && LinkDataType == LinkDataType.kDouble) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kDouble.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<double>) && LinkDataType == LinkDataType.kListDouble) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(string) && LinkDataType == LinkDataType.kString) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kString.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<string>) && LinkDataType == LinkDataType.kListString) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(bool) && LinkDataType == LinkDataType.kBool) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kBool.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<bool>) && LinkDataType == LinkDataType.kListBool) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(Mat) && LinkDataType == LinkDataType.kMat) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kMat.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    else if (outputParam.ActualParam.GetType() == typeof(List<Mat>) && LinkDataType == LinkDataType.kListMat) {

                    }
                    else if (outputParam.ActualParam.GetType() == typeof(Mat[]) && LinkDataType == LinkDataType.kRegion) {
                        dataGrid.Items.Add(new { String1 = LinkDataType.kRegion.ToString(), String2 = i + "." + outputParam.ParamName, String3 = "", String4 = outputParam.Description });
                    }
                    //其他类型，也就是object
                    else {

                    }
                }
                //双击以后给输出的赋值，并且关闭链接选择界面
                dataGrid.MouseDoubleClick += Event_DataGrid_DoubleClickRow;
                return dataGrid;
            };

            //递归创建树结构
            Func<TaskNode, TreeViewItem> func_CreateTreeViewItem = null;
            func_CreateTreeViewItem = (node) => {
                if (node == null) { 
                    return null;
                }
                var treeViewItem = new TreeViewItem { Header = node.ItemName, IsExpanded = true, ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
                TabControl_Content.Items.Add(new TabItem() { 
                    Header = node.ItemName,
                    Visibility = Visibility.Collapsed,
                    Content = func_CreateDataGrid(node),
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

            foreach (TaskNode node in flowData.Children) {
                var item = func_CreateTreeViewItem(node);
                if (item == null) {
                    continue;
                }
                TreeView_Catalogue.Items.Add(item);
            }
        }

        /// <summary>
        /// 初始化资源类型插件的选项
        /// </summary>
        public void InitResOption(string taskName, List<ResourceOptionData> resDatas) {
            if (LinkDataType != LinkDataType.kResource || resDatas == null) {
                return;
            }
            DataGrid dataGrid = new DataGrid() {
                IsReadOnly = true,
                RowHeight = 30,
                Background = Brushes.Transparent,
                BorderBrush = Brushes.Transparent,
                BorderThickness = new Thickness(0),
            };
            //第一行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "类型", Binding = new Binding("String1"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第二行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "资源名称", Binding = new Binding("String2"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第三行需要定制，待办
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "对应任务插件", Binding = new Binding("String3"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //第四行
            dataGrid.Columns.Add(new DataGridTextColumn { Header = "描述", Binding = new Binding("String4"), Width = new DataGridLength(1, DataGridLengthUnitType.Star) });
            //界面
            foreach (ResourceOptionData resData in resDatas) {
                List<string> allowLinkTasks = resData.ResViewInterface.AllowTaskPluginLink();
                List<string> taskNames = new List<string>();
                //判断当前界面允许列表中是否有这个task插件
                foreach (var allowTask in allowLinkTasks) {
                    if (allowTask == taskName) {
                        taskNames.Add(allowTask);
                    }
                    else {
                        continue;
                    }
                }
                if (taskNames.Count == 0) {
                    continue;
                }
                dataGrid.Items.Add(new {
                    String1 = LinkDataType.kResource.ToString(),
                    String2 = resData.ResName,
                    String3 = string.Join(",", taskNames),
                    String4 = "特定任务插件才能使用"
                });
            }
            //双击以后给输出的赋值，并且关闭链接选择界面
            dataGrid.MouseDoubleClick += Event_DataGrid_DoubleClickRow;
            TreeView_Catalogue.Items.Add(new TreeViewItem {
                Header = "Res",
                IsExpanded = true,
                ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle"),
            });
            TabControl_Content.Items.Add(new TabItem() {
                Header = "Res",
                Visibility = Visibility.Collapsed,
                Content = dataGrid,
            });
        }

        /// <summary>
        /// 切换到指定名字的tab页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_SwitchToTab(object sender, MouseButtonEventArgs e) {
            var selectItem = TreeView_Catalogue.SelectedItem as TreeViewItem;
            if (selectItem == null) {
                return;
            }
            for (int i = 0; i < TabControl_Content.Items.Count; i++) {
                var item = TabControl_Content.Items[i] as TabItem;
                if (item == null) { 
                    continue ;
                }
                if (item.Header != selectItem.Header) { 
                    continue;
                }
                TabControl_Content.SelectedIndex = i;
                break;
            }
        }

        /// <summary>
        /// 双击点击行以后拿到数据后就关闭界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_DataGrid_DoubleClickRow(object sender, MouseButtonEventArgs e) {
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
            var selectedItem = eventDataGrid.SelectedItem;

            if (selectedItem == null) {
                return;
            }
            var type = selectedItem.GetType();
            string String1 = type.GetProperty("String1")?.GetValue(selectedItem).ToString();
            string String2 = type.GetProperty("String2")?.GetValue(selectedItem).ToString();
            string String3 = type.GetProperty("String3")?.GetValue(selectedItem).ToString();
            string String4 = type.GetProperty("String4")?.GetValue(selectedItem).ToString();

            TabItem contentItem = TabControl_Content.SelectedItem as TabItem;
            if (contentItem == null) {
                return;
            }

            switch (LinkDataType) {
                case LinkDataType.kNone:
                    break;
                case LinkDataType.kInt:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListInt:
                    break;
                case LinkDataType.kFloat:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListFloat:
                    break;
                case LinkDataType.kDouble:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListDouble:
                    break;
                case LinkDataType.kString:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListString:
                    break;
                case LinkDataType.kBool:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListBool:
                    break;
                case LinkDataType.kRegion:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListRegion:
                    break;
                case LinkDataType.kMat:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kListMat:
                    break;
                case LinkDataType.kView:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kResource:
                    LinkDataText = LinkDataType + "(" + contentItem.Header.ToString() + "." + String2 + ")";
                    break;
                case LinkDataType.kObject:
                    break;
                default:
                    break;
            }
            Close();
        }
    }
}
