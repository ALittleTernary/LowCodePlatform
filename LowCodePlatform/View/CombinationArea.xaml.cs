using AvalonDock.Controls;
using LowCodePlatform.Engine;
using LowCodePlatform.Plugin;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.View.Base;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OpenCvSharp.Dnn;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;
using static LowCodePlatform.View.Base.CombinationArea_TreeItem;


namespace LowCodePlatform.View
{
    /// <summary>
    /// CombinationArea.xaml 的交互逻辑
    /// </summary>
    public partial class CombinationArea : UserControl, CommunicationUser
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        /// <summary>
        /// 拖拽使用的装饰器
        /// </summary>
        private AdornerLayer _adornerLayer = null;

        /// <summary>
        /// 拖拽到某个item的悬停时间计时
        /// </summary>
        private DateTime _startHoverTime = DateTime.MinValue;

        /// <summary>
        /// 拖拽时，鼠标的悬停item，如果悬停在有子项未展开的item上，悬停超过1s则展开该项
        /// </summary>
        private TreeViewItem _hoveredItem = null;

        private RoutedEventHandler _clickRunOnce = null;
        private RoutedEventHandler _clickRunLoop = null;
        private RoutedEventHandler _clickRunStop = null;
        private RoutedEventHandler _clickRunResurvey = null;

        public CombinationArea() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {


        }

        /// <summary>
        /// 新增一个tab页
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private TabItem AddTab(string headerName) {
            //根据名字返回一个grid
            Func<string, Grid> func = (string name) => { 
                Grid grid = new Grid();

                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Auto) });
                grid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });

                Grid innerGrid = new Grid();
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 文字列
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // 伸展列
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 第一个按钮列
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 第二个按钮列
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 第三个按钮列
                innerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) }); // 第四个按钮列

                // 创建TextBlock
                TextBlock textBlock_flowName = new TextBlock {
                    Text = name,
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(textBlock_flowName, 0); // 设置为第一列
                innerGrid.Children.Add(textBlock_flowName);

                // 创建第一个按钮
                Button button_runOne = new Button {
                    Content = "执行",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5)
                };
                Grid.SetColumn(button_runOne, 2); // 设置为第三列
                innerGrid.Children.Add(button_runOne);
                button_runOne.Click += _clickRunOnce;

                // 创建第二个按钮
                Button button_runLoop = new Button {
                    Content = "循环",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5)
                };
                Grid.SetColumn(button_runLoop, 3); // 设置为第四列
                innerGrid.Children.Add(button_runLoop);
                button_runLoop.Click += _clickRunLoop;

                // 创建第三个按钮
                Button button_runStop = new Button {
                    Content = "停止",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5)
                };
                Grid.SetColumn(button_runStop, 4); // 设置为第五列
                innerGrid.Children.Add(button_runStop);
                button_runStop.Click += _clickRunStop;

                // 创建第四个按钮
                Button button_runResurvey = new Button {
                    Content = "重测",
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Margin = new Thickness(5)
                };
                Grid.SetColumn(button_runResurvey, 5); // 设置为第五列
                innerGrid.Children.Add(button_runResurvey);
                button_runResurvey.Click += _clickRunResurvey;

                // 将innerGrid添加到Grid的第一行
                Grid.SetRow(innerGrid, 0);
                grid.Children.Add(innerGrid);

                TreeView treeView = new TreeView() {
                    HorizontalContentAlignment = HorizontalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle"),
                    AllowDrop = true,
                };
                Grid.SetRow(treeView, 1);
                grid.Children.Add(treeView);

                treeView.MouseMove += Event_TreeView_MouseMove;
                //dragover仅在treeView内触发事件，使用OnQueryContinueDrag则可以在整个拖拽过程中触发事件，跨tree拖拽使用OnQueryContinueDrag而不能使用DragOver
                treeView.QueryContinueDrag += Event_TreeView_QueryContinueDrag;
                treeView.DragOver += Event_TreeView_DragOver;
                treeView.Drop += Event_TreeView_Drop;
                treeView.PreviewMouseDoubleClick += Event_TreeView_PreviewMouseDoubleClick;
                return grid;
            };

            TabItem newTab = new TabItem() { Visibility = Visibility.Collapsed };
            newTab.Header = headerName; // 设置 Tab 页标题
            newTab.Content = func(headerName);

            // 将新的 TabItem 添加到 TabControl 中
            TabControl_Combination.Items.Add(newTab);
            return newTab;
        }

        /// <summary>
        /// 删除一个tab页
        /// </summary>
        /// <param name="headerName"></param>
        private void RemoveTab(string headerName) {
            foreach (TabItem tab in TabControl_Combination.Items) {
                if (tab.Header.ToString() == headerName) {
                    TabControl_Combination.Items.Remove(tab);
                    break; 
                }
            }
        }

        /// <summary>
        /// 重命名一个tab页
        /// </summary>
        /// <param name="headerName"></param>
        private void RenameTab(JObject json) {
            if (json == null) { 
                return;
            }
            string previousHeaderName = json["previousHeaderName"].ToString();
            string futureHeaderName = json["futureHeaderName"].ToString();

            TabItem tab = null;
            foreach (TabItem currentTab in TabControl_Combination.Items) {
                //名字不相等就继续判断
                if (currentTab.Header.ToString() != previousHeaderName) {
                    continue;
                }
                tab = currentTab;
                break;
            }
            if (tab == null) { 
                return;
            }
            TextBlock textBlock_flowName = GetTextBlock_FlowNameFromTab(tab);
            //在这把名字改了
            textBlock_flowName.Text = futureHeaderName;
            tab.Header = futureHeaderName;
        }

        /// <summary>
        /// 切换到指定的tab页
        /// </summary>
        /// <param name="headerName"></param>
        private void SwitchToTab(string headerName) {
            for (int i = 0; i < TabControl_Combination.Items.Count; i++) { 
                var tab = TabControl_Combination.Items[i] as TabItem;
                if (tab == null) {
                    continue;
                }
                if (tab.Header.ToString() == headerName) {
                    TabControl_Combination.SelectedIndex = i;
                    break;
                }
            }
        }

        /// <summary>
        /// 获取tab页中TextBlock流程名
        /// </summary>
        private TextBlock GetTextBlock_FlowNameFromTab(TabItem tab) {
            if (tab == null) {
                return null;
            }

            Grid grid = tab.Content as Grid;
            if (grid == null) {
                return null;
            }
            Grid innerGrid = null;
            foreach (var child in grid.Children) {
                innerGrid = child as Grid;
                if (innerGrid == null) {
                    continue;
                }
                if (Grid.GetRow(innerGrid) != 0) {
                    continue;
                }
                break;
            }
            if (innerGrid == null) {
                return null;
            }
            TextBlock textBlock_flowName = null;
            foreach (var innerChild in innerGrid.Children) {
                var textBlock = innerChild as TextBlock;
                if (textBlock == null) {
                    continue;
                }
                if (Grid.GetColumn(textBlock) != 0) {
                    continue;
                }
                textBlock_flowName = textBlock;
                break;
            }
            return textBlock_flowName;
        }

        /// <summary>
        /// 获取tab页中button执行一次
        /// </summary>
        private Button GetButton_RunOneFromTab(TabItem tab) {
            if (tab == null) {
                return null;
            }

            Grid grid = tab.Content as Grid;
            if (grid == null) {
                return null;
            }
            Grid innerGrid = null;
            foreach (var child in grid.Children) {
                innerGrid = child as Grid;
                if (innerGrid == null) {
                    continue;
                }
                if (Grid.GetRow(innerGrid) != 0) {
                    continue;
                }
                break;
            }
            if (innerGrid == null) {
                return null;
            }
            Button button_runOne = null;
            foreach (var innerChild in innerGrid.Children) {
                var button = innerChild as Button;
                if (button == null) {
                    continue;
                }
                if (Grid.GetColumn(button) != 2) {
                    continue;
                }
                button_runOne = button;
                break;
            }
            return button_runOne;
        }

        /// <summary>
        /// 获取tab页中button循环执行
        /// </summary>
        private Button GetButton_RunLoopFromTab(TabItem tab) {
            if (tab == null) {
                return null;
            }

            Grid grid = tab.Content as Grid;
            if (grid == null) {
                return null;
            }
            Grid innerGrid = null;
            foreach (var child in grid.Children) {
                innerGrid = child as Grid;
                if (innerGrid == null) {
                    continue;
                }
                if (Grid.GetRow(innerGrid) != 0) {
                    continue;
                }
                break;
            }
            if (innerGrid == null) {
                return null;
            }
            Button button_runLoop = null;
            foreach (var innerChild in innerGrid.Children) {
                var button = innerChild as Button;
                if (button == null) {
                    continue;
                }
                if (Grid.GetColumn(button) != 3) {
                    continue;
                }
                button_runLoop = button;
                break;
            }
            return button_runLoop;
        }

        /// <summary>
        /// 获取tab页中button停止
        /// </summary>
        private Button GetButton_RunStopFromTab(TabItem tab) {
            if (tab == null) {
                return null;
            }

            Grid grid = tab.Content as Grid;
            if (grid == null) {
                return null;
            }
            Grid innerGrid = null;
            foreach (var child in grid.Children) {
                innerGrid = child as Grid;
                if (innerGrid == null) {
                    continue;
                }
                if (Grid.GetRow(innerGrid) != 0) {
                    continue;
                }
                break;
            }
            if (innerGrid == null) {
                return null;
            }
            Button button_runStop = null;
            foreach (var innerChild in innerGrid.Children) {
                var button = innerChild as Button;
                if (button == null) {
                    continue;
                }
                if (Grid.GetColumn(button) != 4) {
                    continue;
                }
                button_runStop = button;
                break;
            }
            return button_runStop;
        }

        /// <summary>
        /// 获取tab页中的运行树
        /// </summary>
        /// <param name="tab"></param>
        /// <returns></returns>
        private TreeView GetTreeViewFromTab(TabItem tab) {
            if (tab == null) {
                return null;
            }
            Grid grid = tab.Content as Grid;
            if (grid == null) {
                return null;
            }
            TreeView treeView = null;
            foreach (var child in grid.Children) {
                var tree = child as TreeView;
                if (tree == null) {
                    continue;
                }
                if (Grid.GetRow(tree) != 1) {
                    continue;
                }
                treeView = tree;
                break;
            }
            return treeView;
        }

        /// <summary>
        /// 获取当前选中的item
        /// </summary>
        /// <returns></returns>
        private TreeViewItem GetTreeViewSelectItem() {
            TreeView selectTree = GetTreeViewFromTab((TabItem)TabControl_Combination.SelectedItem);
            if (selectTree == null) {
                return null;
            }
            TreeViewItem selectItem = selectTree.SelectedItem as TreeViewItem;
            if (selectItem == null) {
                return null;
            }
            return selectItem;
        }

        /// <summary>
        /// 给当前tab添加一个item
        /// </summary>
        /// <param name="itemName"></param>
        private void AddItem(JObject json) {

            //遍历整个树，计算到当前插入新增的编号
            Func<TreeView, int> func_CalSerialNumber = (TreeView tree) => {
                //这个num要考虑已经被删除的
                int num = 0;
                List<int> list = new List<int>();
                //遍历拿到所有的序号
                Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
                // 将根项加入栈中
                foreach (var item in tree.Items) {
                    TreeViewItem treeViewItem = item as TreeViewItem;
                    if (treeViewItem != null) {
                        stack.Push(treeViewItem);
                    }
                }
                // 迭代栈中的项
                while (stack.Count > 0) {
                    // 弹出栈顶元素，获取当前 TreeViewItem
                    TreeViewItem currentItem = stack.Pop();

                    // 处理当前 TreeViewItem
                    CombinationArea_TreeItem customItem = currentItem.Header as CombinationArea_TreeItem;
                    if (customItem != null) {
                        list.Add(customItem.SerialNumber);
                    }
                    num = num + 1;

                    // 将该项的子项加入栈中
                    foreach (var child in currentItem.Items) {
                        TreeViewItem childItem = child as TreeViewItem;
                        if (childItem != null) {
                            stack.Push(childItem);
                        }
                    }
                }
                //序号从小到大排序
                list.Sort();
                //依次对比缺了哪个序号
                for (int i = 0; i < list.Count; i++) {
                    if (list[i] != i) {
                        num = i;
                        break;
                    }
                }
                return num;
            };

            TabItem selectTabItem = TabControl_Combination.SelectedItem as TabItem;
            if (selectTabItem == null) {
                return;
            }

            TreeView treeView = GetTreeViewFromTab(selectTabItem);
            if (treeView == null) { 
                return ;
            }

            int num_item = func_CalSerialNumber(treeView);
            string header = json["header"].ToString();
            if (!Enum.TryParse(json["tag"].ToString(), out ItemOperationType operationType)) { 
                return ;
            }

            CombinationArea_TreeItem custom = new CombinationArea_TreeItem() {SerialNumber = num_item, ItemName = header, OperationType = operationType };
            custom.SetDeleteItemCallback(Event_Button_RemoveItem);
            treeView.Items.Add(new TreeViewItem() { Header = custom , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") });
        }

        /// <summary>
        /// 删除当前tab页的当前选中的item
        /// </summary>
        private void Event_Button_RemoveItem(object sender, RoutedEventArgs e) {
            //获取目标item父类的所有子集,也就是拿到同层级所有子集
            Func<TreeViewItem, ItemCollection> func_FindParentItems = (TreeViewItem targetItem) => {
                if (targetItem == null) {
                    return null;
                }

                var parent_TreeViewItem = VisualTreeHelper.GetParent(targetItem);
                // 遍历父节点，直到找到 TreeViewItem 类型
                while (parent_TreeViewItem != null && !(parent_TreeViewItem is TreeViewItem)) {
                    parent_TreeViewItem = VisualTreeHelper.GetParent(parent_TreeViewItem);
                }
                if (parent_TreeViewItem is TreeViewItem parentItem) {
                    return parentItem.Items;
                }

                var parent_TreeView = VisualTreeHelper.GetParent(targetItem);
                while (parent_TreeView != null && !(parent_TreeView is TreeView)) {
                    parent_TreeView = VisualTreeHelper.GetParent(parent_TreeView);
                }
                if (parent_TreeView is TreeView treeView) {
                    return treeView.Items;
                }

                return null;
            };

            //拿到当前选中的，也就是你想删除的item
            TreeViewItem currentItem = GetTreeViewSelectItem();
            if (currentItem == null) { 
                return; 
            }
            ItemCollection parentItems = func_FindParentItems(currentItem);
            if (parentItems == null) { 
                return;
            }
            parentItems.Remove(currentItem);
        }

        /// <summary>
        /// 鼠标移动事件，启动拖拽事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_MouseMove(object sender, MouseEventArgs e) {

            if (Mouse.LeftButton != MouseButtonState.Pressed)
                return;

            //拿到当前你想拖动的item的父亲也就是tree
            TreeView dragTree = GetTreeViewFromTab((TabItem)TabControl_Combination.SelectedItem);
            if (dragTree == null) { 
                return;
            }
            Point pos = e.GetPosition(dragTree);
            HitTestResult result = VisualTreeHelper.HitTest(dragTree, pos);
            if (result == null)
                return;

            //拿到当前你想拖动的item
            TreeViewItem dragTreeItem = FindVisualParent<TreeViewItem>(result.VisualHit);
            if (dragTreeItem == null || dragTreeItem != dragTree.SelectedItem || !(dragTree.SelectedItem is TreeViewItem))
                return;

            //装饰器，装饰一下拖拽的项
            DragDropAdorner adorner = new DragDropAdorner((CombinationArea_TreeItem)dragTreeItem.Header);
            _adornerLayer = AdornerLayer.GetAdornerLayer(TabControl_Combination);
            _adornerLayer.Add(adorner);

            DataObject data = new DataObject();
            //数据来源给个名字，因为Option也会拖拽过来
            data.SetData("Combination", dragTreeItem);
            //发起拖动的控件/发送数据/移动方式
            DragDrop.DoDragDrop(dragTree, data, DragDropEffects.Move);
            _startHoverTime = DateTime.MinValue;
            //清空悬停项
            _hoveredItem = null;
            //装饰项清空
            _adornerLayer.Remove(adorner);
            //清空装饰器
            _adornerLayer = null;
        }

        /// <summary>
        /// 鼠标移动事件，拖动过程持续调用，范围不止于treeview中
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_QueryContinueDrag(object sender, QueryContinueDragEventArgs e) {
            //拿到当前你想拖动的item的父亲也就是tree
            TreeView dragTree = GetTreeViewFromTab((TabItem)TabControl_Combination.SelectedItem);
            if (dragTree == null) {
                return;
            }
            //装饰器更新
            _adornerLayer.Update();
            Win32.POINT point = new Win32.POINT();
            if (!Win32.GetCursorPos(ref point)) {
                return;
            }
            Point pos = new Point(point.X, point.Y);
            pos = dragTree.PointFromScreen(pos);
            HitTestResult result = VisualTreeHelper.HitTest(dragTree, pos);
            if (result == null) {
                return;
            }
            //当前悬停项
            TreeViewItem selectedItem = FindVisualParent<TreeViewItem>(result.VisualHit);
            if (selectedItem == null) {
                return;
            }
            //如果当前悬停项和上一个悬停项不一样，就更新当前悬停项，然后刷新悬停时间
            if (_hoveredItem != selectedItem) {
                _hoveredItem = selectedItem;
                _startHoverTime = DateTime.Now;
            }
            //如果悬停在折叠的树节点上超过1秒，则展开
            else if (_hoveredItem.Items.Count > 0 && !_hoveredItem.IsExpanded && DateTime.Now - _startHoverTime > TimeSpan.FromSeconds(1)) {
                _hoveredItem.IsExpanded = true;
            }
            else {
                //其他情况不处理
            }
        }

        /// <summary>
        /// 拖拽过程中，范围仅在treeview范围内触发，显示当前鼠标指向的TreeViewItem
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_DragOver(object sender, DragEventArgs e) {
            TreeView dropTree = GetTreeViewFromTab((TabItem)TabControl_Combination.SelectedItem);
            if (dropTree == null) {
                return;
            }

            //当拖动操作开始时，初始设置为 None 是为了确保在没有任何有效的拖放目标时，不会执行任何拖放操作。这是一种安全措施，表明当前鼠标位置没有有效的目标来接收拖放的数据。
            e.Effects = DragDropEffects.None;

            Point pos = e.GetPosition(dropTree);
            HitTestResult result = VisualTreeHelper.HitTest(dropTree, pos);
            if (result == null)
                return;

            TreeViewItem selectedItem = FindVisualParent<TreeViewItem>(result.VisualHit);
            if (selectedItem != null) {
                selectedItem.IsSelected = true;
            }


            //当鼠标移动到一个有效的 TreeViewItem上时，将e.Effects设置为 Move 是为了告诉系统，当前的位置可以接受拖放操作，并且可以将数据移动到这个位置。
            e.Effects = DragDropEffects.Move;
        }

        /// <summary>
        /// 拖拽完成
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_Drop(object sender, DragEventArgs e) {
            //获取目标item父类的所有子集,也就是拿到同层级所有子集
            Func<TreeViewItem, ItemCollection> func_FindParentItems = (TreeViewItem targetItem) => {
                if (targetItem == null) { 
                    return null;
                }

                var parent_TreeViewItem = VisualTreeHelper.GetParent(targetItem);
                // 遍历父节点，直到找到 TreeViewItem 类型
                while (parent_TreeViewItem != null && !(parent_TreeViewItem is TreeViewItem)) {
                    parent_TreeViewItem = VisualTreeHelper.GetParent(parent_TreeViewItem);
                }
                if (parent_TreeViewItem is TreeViewItem parentItem) {
                    return parentItem.Items;
                }

                var parent_TreeView = VisualTreeHelper.GetParent(targetItem);
                while (parent_TreeView != null && !(parent_TreeView is TreeView)) {
                    parent_TreeView = VisualTreeHelper.GetParent(parent_TreeView);
                }
                if (parent_TreeView is TreeView treeView) {
                    return treeView.Items;
                }

                return null;
            };

            //获取父类是否是树节点，也就是判断该节点是不是没有父节点
            Func<TreeViewItem, bool> func_IsParentTreeView = (TreeViewItem targetItem) => {
                if (targetItem == null) {
                    return false;
                }
                var parent_TreeViewItem = VisualTreeHelper.GetParent(targetItem);
                // 遍历父节点，直到找到 TreeViewItem 类型
                while (parent_TreeViewItem != null && !(parent_TreeViewItem is TreeViewItem)) {
                    parent_TreeViewItem = VisualTreeHelper.GetParent(parent_TreeViewItem);
                }
                if (parent_TreeViewItem is TreeViewItem parentItem) {
                    return false;
                }

                return true;
            };

            //遍历整个树，计算到当前插入新增的编号
            Func<TreeView, int> func_CalSerialNumber = (TreeView tree) => {
                //这个num要考虑已经被删除的
                int num = 0;
                List<int> list = new List<int>();
                //遍历拿到所有的序号
                Stack<TreeViewItem> stack = new Stack<TreeViewItem>();
                // 将根项加入栈中
                foreach (var item in tree.Items) {
                    TreeViewItem treeViewItem = item as TreeViewItem;
                    if (treeViewItem != null) {
                        stack.Push(treeViewItem);
                    }
                }
                // 迭代栈中的项
                while (stack.Count > 0) {
                    // 弹出栈顶元素，获取当前 TreeViewItem
                    TreeViewItem currentItem = stack.Pop();

                    // 处理当前 TreeViewItem
                    CombinationArea_TreeItem customItem = currentItem.Header as CombinationArea_TreeItem;
                    if (customItem != null) {
                        list.Add(customItem.SerialNumber);
                    }
                    num = num + 1;

                    // 将该项的子项加入栈中
                    foreach (var child in currentItem.Items) {
                        TreeViewItem childItem = child as TreeViewItem;
                        if (childItem != null) {
                            stack.Push(childItem);
                        }
                    }
                }
                //序号从小到大排序
                list.Sort();
                //依次对比缺了哪个序号
                for (int i = 0; i < list.Count; i++) {
                    if (list[i] != i) {
                        num = i;
                        break;
                    }
                }
                return num;
            };


            TreeView dropTree = GetTreeViewFromTab((TabItem)TabControl_Combination.SelectedItem);
            if (dropTree == null) {
                return;
            }

            Point pos = e.GetPosition(dropTree);
            HitTestResult result = VisualTreeHelper.HitTest(dropTree, pos);
            //在tree范围内
            if (result == null)
                return;

            //当前指向的item
            TreeViewItem selectedItem = FindVisualParent<TreeViewItem>(result.VisualHit);

            //启动拖拽时发送出的数据
            TreeViewItem dataItem_Option = e.Data.GetData("Option") as TreeViewItem;
            TreeViewItem dataItem_Combination = e.Data.GetData("Combination") as TreeViewItem;

            //此时一共8种情况，只有几种情况有效
            //目标指向为空，数据源于组合区，数据源放到tree的最后
            if (selectedItem == null && dataItem_Option == null && dataItem_Combination != null) {
                CombinationArea_TreeItem dataItem_Combination_Header = dataItem_Combination.Header as CombinationArea_TreeItem;
                if (dataItem_Combination_Header == null) {
                    return;
                }
                ItemCollection targetParentItems = func_FindParentItems(dataItem_Combination);
                if (targetParentItems != null) {
                    //需要与父组件断开连接,先移除再判断位置
                    targetParentItems.Remove(dataItem_Combination);
                }
                dropTree.Items.Add(dataItem_Combination);
            }
            //目标指向为空，数据源于选项区，数据源放到tree的最后
            else if (selectedItem == null && dataItem_Option != null && dataItem_Combination == null) {
                CombinationArea_TreeItem dataItem_Option_Header = dataItem_Option.Header as CombinationArea_TreeItem;
                if (dataItem_Option_Header == null) {
                    return;
                }

                int num_item = func_CalSerialNumber(dropTree);
                if (num_item < 0) {
                    return;
                }

                dataItem_Option_Header.SetDeleteItemCallback(Event_Button_RemoveItem);
                dataItem_Option_Header.SerialNumber = num_item;
                dropTree.Items.Add(dataItem_Option);
            }
            //目标指向某item，数据源于组合区，一般放在item同层级的item之后
            else if (selectedItem != null && dataItem_Option == null && dataItem_Combination != null) {
                //如果自己拖拽指向自己，闲的蛋疼，不处理
                if (selectedItem == dataItem_Combination) {
                    return;
                }

                //目标项
                CombinationArea_TreeItem selectedItem_Header = selectedItem.Header as CombinationArea_TreeItem;
                if (selectedItem_Header == null) {
                    return;
                }
                //目标项操作属性
                ItemOperationType operationType = selectedItem_Header.OperationType;

                //来源项
                CombinationArea_TreeItem dataItem_Header = dataItem_Combination.Header as CombinationArea_TreeItem;
                if (dataItem_Header == null) {
                    return;
                }
                //来源项操作属性
                ItemOperationType dataType = dataItem_Header.OperationType;

                if (operationType == ItemOperationType.kNone || dataType == ItemOperationType.kNone) {
                    return;
                }

                //来源项禁止拥有父项
                if (dataType == ItemOperationType.kOriginalResurvey || dataType == ItemOperationType.kSwitchResurvey) {
                    //获取到目标是否有父项
                    if (!func_IsParentTreeView(selectedItem)) {
                        //如果目标有父项，则该目标不符合要求，不能将来源挪动到目标处
                        return;
                    }
                }

                //目标项禁止拥有子项的情况，常规处理
                if (operationType == ItemOperationType.kCommon || operationType == ItemOperationType.kBreak || operationType == ItemOperationType.kContinue || operationType == ItemOperationType.kStopFlow || operationType == ItemOperationType.kReRunFlow || operationType == ItemOperationType.kStopProcess || operationType == ItemOperationType.kReRunProcess || operationType == ItemOperationType.kReturn || operationType == ItemOperationType.kOriginalResurvey) {
                    //获取到目标同层级所有的items
                    ItemCollection targetParentItems = func_FindParentItems(selectedItem);
                    if (targetParentItems == null) {
                        return;
                    }
                    //获取到来源同层级所有的items
                    ItemCollection sourceParentItems = func_FindParentItems(dataItem_Combination);
                    if (sourceParentItems == null) {
                        return;
                    }
                    sourceParentItems.Remove(dataItem_Combination);
                    int index = targetParentItems.IndexOf(selectedItem);
                    targetParentItems.Insert(index + 1, dataItem_Combination);
                }
                //目标项允许拥有子项的情况，特殊处理
                else if (operationType == ItemOperationType.kIf || operationType == ItemOperationType.kElseIf || operationType == ItemOperationType.kElse || operationType == ItemOperationType.kFor || operationType == ItemOperationType.kWhile || operationType == ItemOperationType.kSerial || operationType == ItemOperationType.kParallel || operationType == ItemOperationType.kSwitchResurvey) {
                    //获取到目标同层级所有的items
                    ItemCollection targetParentItems = func_FindParentItems(selectedItem);
                    if (targetParentItems == null) {
                        return;
                    }
                    //获取到来源同层级所有的items
                    ItemCollection sourceParentItems = func_FindParentItems(dataItem_Combination);
                    if (sourceParentItems == null) {
                        return;
                    }
                    sourceParentItems.Remove(dataItem_Combination);

                    //如果目标项允许拥有子项的情况下又拥有子项，就目标项后即可
                    if (selectedItem.Items.Count > 0 || dataType == ItemOperationType.kOriginalResurvey || dataType == ItemOperationType.kSwitchResurvey) {
                        int index = targetParentItems.IndexOf(selectedItem);
                        targetParentItems.Insert(index + 1, dataItem_Combination);
                    }
                    //如果目标项允许拥有子项的情况下但没有子项，就需要放在目标项的子项内
                    else {
                        selectedItem.Items.Add(dataItem_Combination);
                    }

                }
                else {

                }
            }
            //目标指向某item，数据源于选项区，一般放在item同层级的item之后
            else if (selectedItem != null && dataItem_Option != null && dataItem_Combination == null) {
                CombinationArea_TreeItem dataItem_Option_Header = dataItem_Option.Header as CombinationArea_TreeItem;
                if (dataItem_Option_Header == null) {
                    return;
                }
                int num_item = func_CalSerialNumber(dropTree);
                if (num_item < 0) {
                    return;
                }
                dataItem_Option_Header.SetDeleteItemCallback(Event_Button_RemoveItem);
                dataItem_Option_Header.SerialNumber = num_item;

                //目标项
                CombinationArea_TreeItem selectedItem_Header = selectedItem.Header as CombinationArea_TreeItem;
                if (selectedItem_Header == null) {
                    return;
                }
                //目标项操作属性
                ItemOperationType operationType = selectedItem_Header.OperationType;
                //来源项
                CombinationArea_TreeItem dataOption_Header = dataItem_Option.Header as CombinationArea_TreeItem;
                if (dataOption_Header == null) {
                    return;
                }
                //来源项操作属性
                ItemOperationType dataType = dataOption_Header.OperationType;

                if (operationType == ItemOperationType.kNone || dataType == ItemOperationType.kNone) {
                    return;
                }

                //来源项禁止拥有父项
                if (dataType == ItemOperationType.kOriginalResurvey || dataType == ItemOperationType.kSwitchResurvey) {
                    //获取到目标是否有父项
                    if (!func_IsParentTreeView(selectedItem)) {
                        //如果目标有父项，则该目标不符合要求，不能将来源挪动到目标处
                        return;
                    }
                }

                //目标项禁止拥有子项的情况，常规处理
                if (operationType == ItemOperationType.kCommon || operationType == ItemOperationType.kBreak || operationType == ItemOperationType.kContinue || operationType == ItemOperationType.kStopFlow || operationType == ItemOperationType.kReRunFlow || operationType == ItemOperationType.kStopProcess || operationType == ItemOperationType.kReRunProcess || operationType == ItemOperationType.kReturn || operationType == ItemOperationType.kOriginalResurvey) {
                    //获取到目标同层级所有的items
                    ItemCollection targetParentItems = func_FindParentItems(selectedItem);
                    if (targetParentItems == null) {
                        return;
                    }
                    int index = targetParentItems.IndexOf(selectedItem);
                    targetParentItems.Insert(index + 1, dataItem_Option);
                }
                //目标项允许拥有子项的情况，特殊处理
                else if (operationType == ItemOperationType.kIf || operationType == ItemOperationType.kElseIf || operationType == ItemOperationType.kElse || operationType == ItemOperationType.kFor || operationType == ItemOperationType.kWhile || operationType == ItemOperationType.kSerial || operationType == ItemOperationType.kParallel || operationType == ItemOperationType.kSwitchResurvey) {
                    //获取到目标同层级所有的items
                    ItemCollection targetParentItems = func_FindParentItems(selectedItem);
                    if (targetParentItems == null) {
                        return;
                    }

                    //如果目标项允许拥有子项的情况下又拥有子项，就目标项后即可
                    if (selectedItem.Items.Count > 0 || dataType == ItemOperationType.kOriginalResurvey || dataType == ItemOperationType.kSwitchResurvey) {
                        int index = targetParentItems.IndexOf(selectedItem);
                        targetParentItems.Insert(index + 1, dataItem_Option);
                    }
                    //如果目标项允许拥有子项的情况下但没有子项，就需要放在目标项的子项内
                    else {
                        selectedItem.Items.Add(dataItem_Option);
                    }
                }
                else {

                }
            }
            else { 
            
            }
        }

        /// <summary>
        /// 双击item打开编辑界面
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e) {
            // 阻止展开/折叠行为
            e.Handled = true;

            DependencyObject originalElement = e.OriginalSource as DependencyObject;
            ToggleButton toggleButton = originalElement.FindVisualAncestor<ToggleButton>();
            if (toggleButton != null && toggleButton.Name == "Expander") {
                return;
            }
            TreeViewItem treeViewItem = originalElement.FindVisualAncestor<TreeViewItem>();
            if (treeViewItem == null) {
                return;
            }

            TreeViewItem selectItem = GetTreeViewSelectItem();
            if (selectItem == null) { 
                return; 
            }
            CombinationArea_TreeItem selectItemHeader = selectItem.Header as CombinationArea_TreeItem;
            if (selectItemHeader == null) {
                return;
            }

            //把当前item传递过去，当前item中有需要的参数，并通知插件管理器把界面打开
            _sendMessage?.Invoke(new CommunicationCenterMessage("CombinationArea", "PluginManager", "TaskViewShowByData", selectItemHeader));
        }

        /// <summary>
        /// 拖拽过程中，获取鼠标位置指向的T
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obj"></param>
        /// <returns></returns>
        private T FindVisualParent<T>(DependencyObject obj) where T : class {
            while (obj != null) {
                if (obj is T)
                    return obj as T;

                obj = VisualTreeHelper.GetParent(obj);
            }
            return null;
        }

        /// <summary>
        /// 创建一个数据结构,仅用于tab中树的数据的保存
        /// </summary>
        private class TreeDataNode
        {
            public string Header { get; set; } = "";

            /// <summary>
            /// item的子项
            /// </summary>
            public List<TreeDataNode> Children { get; set; } = new List<TreeDataNode>();

            /// <summary>
            /// 构造函数
            /// </summary>
            /// <param name="header"></param>
            public TreeDataNode() {

            }
        }

        /// <summary>
        /// 将treeview变成string，保存数据
        /// </summary>
        /// <param name="treeView"></param>
        /// <returns></returns>
        private string GetTreeNodesDataFromTreeView(TreeView treeView) {
            Func<TreeViewItem, TreeDataNode> func_CreateTreeDataNode = null;
            func_CreateTreeDataNode = (treeViewItem) => {
                if (treeViewItem == null) { 
                    return null;
                }
                //这里假设了treeViewItem必定有值
                CombinationArea_TreeItem custom = treeViewItem.Header as CombinationArea_TreeItem;
                var node = new TreeDataNode() { Header = custom.ViewToJson() };

                foreach (var child in treeViewItem.Items) {
                    TreeViewItem childItem = child as TreeViewItem;
                    if (childItem == null) {
                        continue;
                    }
                    TreeDataNode treeDataNode = func_CreateTreeDataNode(childItem);
                    if (treeDataNode == null) {
                        continue;
                    }
                    node.Children.Add(treeDataNode);
                }
                return node;
            };
            List<TreeDataNode> nodes = new List<TreeDataNode>();
            foreach (var item in treeView.Items) {
                if (item is TreeViewItem treeViewItem) {
                    nodes.Add(func_CreateTreeDataNode(treeViewItem));
                }
            }
            string str = JsonConvert.SerializeObject(nodes, Formatting.Indented);
            return str;
        }

        /// <summary>
        /// 将str还原为TreeView
        /// </summary>
        /// <param name="tree"></param>
        /// <param name="str"></param>
        private void SetTreeNodesDataToTreeView(TreeView tree, string str) {
            Func<TreeDataNode, TreeViewItem> func_CreateTreeViewItem = null;
            func_CreateTreeViewItem = (node) => {
                CombinationArea_TreeItem custom = new CombinationArea_TreeItem();
                custom.SetDeleteItemCallback(Event_Button_RemoveItem);
                custom.JsonToView(node.Header);
                var treeViewItem = new TreeViewItem { Header = custom, ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
                foreach (var child in node.Children) {
                    treeViewItem.Items.Add(func_CreateTreeViewItem(child));
                }
                return treeViewItem;
            };

            var nodes = JsonConvert.DeserializeObject<List<TreeDataNode>>(str);
            foreach (var node in nodes) {
                tree.Items.Add(func_CreateTreeViewItem(node));
            }
        }

        /// <summary>
        /// 重置情况当前所有数据
        /// </summary>
        private void ResetData() {
            TabControl_Combination.Items.Clear();
        }

        /// <summary>
        /// 总结工程中所有的流程信息
        /// </summary>
        /// <returns></returns>
        public List<FlowNode> SummarizeProcessNodes() {
            List < FlowNode > flowNodes = new List< FlowNode >();
            for (int i = 0; i < TabControl_Combination.Items.Count; i = i + 1) {
                TabItem tab = TabControl_Combination.Items[i] as TabItem;
                if (tab == null) {
                    continue;
                }
                flowNodes.Add(SummarizeFlowNodes(tab));
            }

            return flowNodes;
        }

        /// <summary>
        /// 总结当前流程中所有节点信息
        /// </summary>
        /// <returns></returns>
        public FlowNode SummarizeFlowNodes() {
            return SummarizeFlowNodes(TabControl_Combination.SelectedItem as TabItem);
        }

        /// <summary>
        /// 总结当前选中项所有信息
        /// </summary>
        /// <returns></returns>
        public TaskNode SummarizeSingleNode() {
            return SummarizeSingleNode(GetTreeViewSelectItem(), ((TabItem)TabControl_Combination.SelectedItem).Header.ToString());
        }

        /// <summary>
        /// 总结某个流程里的所有信息
        /// </summary>
        /// <returns></returns>
        private FlowNode SummarizeFlowNodes(TabItem tab) {
            if (tab == null) {
                return null;
            }
            TreeView treeView = GetTreeViewFromTab(tab);
            if (treeView == null) {
                return null;
            }

            FlowNode currentFlow = new FlowNode();
            currentFlow.Name = tab.Header.ToString();//当前流程名

            TaskNode Previous = null;
            for (int i = 0; i < treeView.Items.Count; i++) {
                TreeViewItem treeViewItem = treeView.Items[i] as TreeViewItem;
                if (treeViewItem == null) {
                    continue;
                }
                TaskNode taskNode = SummarizeSingleNode(treeViewItem, currentFlow.Name);
                if (taskNode == null) {
                    continue;
                }
                currentFlow.Children.Add(taskNode);
                if (Previous == null) {
                    Previous = taskNode;
                    continue;
                }
                Previous.Next = taskNode;//当前节点就是先前节点的下一节点
                taskNode.Previous = Previous;//先前节点的下一节点就是当前节点
                Previous = taskNode;//此时先前节点就变为了当前节点
            }

            return currentFlow;
        }

        /// <summary>
        /// 总结单个item里的节点信息
        /// </summary>
        /// <param name="targetItem"></param>
        /// <param name="parentItem"></param>
        /// <returns></returns>
        private TaskNode SummarizeSingleNode(TreeViewItem targetViewItem, string flowName) {
            //获取目标item父类的所有子集,也就是拿到同层级所有子集,然后获取前一个TreeViewItem
            Func<TreeViewItem, TreeViewItem> func_FindPreviousItems = (TreeViewItem targetItem) => {
                if (targetItem == null) {
                    return null;
                }

                var parent_TreeViewItem = VisualTreeHelper.GetParent(targetItem);
                // 遍历父节点，直到找到 TreeViewItem 类型
                while (parent_TreeViewItem != null && !(parent_TreeViewItem is TreeViewItem)) {
                    parent_TreeViewItem = VisualTreeHelper.GetParent(parent_TreeViewItem);
                }
                if (parent_TreeViewItem is TreeViewItem parentItem) {
                    ItemCollection items = parentItem.Items;
                    // 找到当前项在父项中的索引
                    int currentIndex = items.IndexOf(targetItem);

                    if (currentIndex > 0) {
                        // 返回前一个节点
                        return (TreeViewItem)parentItem.ItemContainerGenerator.ContainerFromIndex(currentIndex - 1);
                    }
                }

                var parent_TreeView = VisualTreeHelper.GetParent(targetItem);
                while (parent_TreeView != null && !(parent_TreeView is TreeView)) {
                    parent_TreeView = VisualTreeHelper.GetParent(parent_TreeView);
                }
                if (parent_TreeView is TreeView treeView) {
                    ItemCollection items = treeView.Items;
                    // 找到当前项在父项中的索引
                    int currentIndex = items.IndexOf(targetItem);

                    if (currentIndex > 0) {
                        // 返回前一个节点
                        return (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromIndex(currentIndex - 1);
                    }
                }

                return null;
            };

            //递归获取当前item所有子项
            Func<TreeViewItem, TaskNode, TaskNode> func_CreateTreeDataNode = null;
            func_CreateTreeDataNode = (target, parent) => {
                //这里假设了target必定有值
                if (target == null) { 
                    return null ;
                }
                CombinationArea_TreeItem custom = target.Header as CombinationArea_TreeItem;

                //开始从custom中总结运行用的node所需参数，能深拷贝的均深拷贝
                var node = new TaskNode();
                node.FlowName = flowName;
                node.ItemName = custom.SerialNumber + "." + custom.ItemName;//在工程中的唯一名字,但是当前是流程中的唯一名字，名不副实
                node.AllowRun = custom.Enable;//该item是否允许运行
                node.OperationType = custom.OperationType;//该item的操作状态，是common，还是流程控制相关的while/if...
                node.Data_InputParams = custom.Data_InputParams;//获取是值拷贝
                node.Data_OutputParams = custom.Data_OutputParams;//获取是值拷贝
                node.ItemView = custom;//item指针拿到，用于给算法引擎更新item的时间和运行状态
                if (custom.TaskView == null) {
                    TaskViewPluginBase taskView = _sendMessage?.Invoke(new CommunicationCenterMessage("CombinationArea", "PluginManager", "GetTaskViewInterfaceByName", custom.ItemName)) as TaskViewPluginBase;//editView指针拿到，用于给算法引擎更新editView相关参数
                    custom.TaskView = taskView;
                }
                node.TaskView = custom.TaskView;

                if (custom.TaskOperation == null) {
                    TaskOperationPluginBase taskOperation = _sendMessage?.Invoke(new CommunicationCenterMessage("CombinationArea", "PluginManager", "GetTaskOperationInterfaceByName", custom.ItemName)) as TaskOperationPluginBase;//operator指针深拷贝拿到，用于运算
                    custom.TaskOperation = taskOperation;
                }
                node.TaskOperation = custom.TaskOperation;

                if (node.TaskOperation != null) {
                    node.TaskOperation.EngineIsRunning = true;//总结数据时，将该值置为运行运行
                }

                //父亲节点
                node.Parent = parent;

                TaskNode Previous = null;
                //孩子节点
                foreach (var child in target.Items) {
                    TreeViewItem childItem = child as TreeViewItem;
                    if (childItem == null) {
                        continue ;
                    }
                    TaskNode childNode = func_CreateTreeDataNode(childItem, node);
                    if (childNode == null) { 
                        continue;
                    }
                    node.Children.Add(childNode);
                    if (Previous == null) {
                        Previous = childNode;
                        continue;
                    }
                    Previous.Next = childNode;//当前节点就是先前节点的下一节点
                    childNode.Previous = Previous;//先前节点的下一节点就是当前节点
                    Previous = childNode;//此时先前节点就变为了当前节点
                }
                return node;
            };

            if (targetViewItem == null) {
                return null;
            }
            //从传入的item开始总结数据，因此不考虑传入item的父节点数据
            TaskNode currentNode = func_CreateTreeDataNode(targetViewItem, null);


            ////elseif/else单步执行时需要往前会总结,整个流程执行时，会把互相之间的指针都串联起来，这个previous会被替代掉

            TreeViewItem iterationItem = targetViewItem;
            TaskNode iterationNode = currentNode;
            while (true) {
                iterationItem = func_FindPreviousItems(iterationItem);
                if (iterationItem == null) {
                    break;
                }
                iterationNode.Previous = func_CreateTreeDataNode(iterationItem, null);
                iterationNode = iterationNode.Previous;
            }

            return currentNode;
        }

        /// <summary>
        /// 当前flow中当前item之前所有的items的信息
        /// </summary>
        /// <param name="targetViewItem"></param>
        /// <returns></returns>
        private FlowNode SummarizeBeforeNodes() {
            //查找指定名字的item在第几层
            Func<TaskNode, string, int, int> func_GetNodeLevel = null;
            func_GetNodeLevel = (TaskNode node, string targetName, int currentLevel) => {
                if (node == null) {
                    return -1; // 返回 -1 表示没有找到该节点
                }

                // 如果当前节点的值与目标值匹配，则返回当前层级
                if (node.ItemName == targetName) {
                    return currentLevel;
                }

                foreach (var item in node.Children) {
                    int level = func_GetNodeLevel(item, targetName, currentLevel + 1);
                    if (level != -1) {
                        return level;
                    }
                }

                return -1;
            };
            //删除第n层及以下的节点
            Action<TaskNode, int, int> action_RemoveNodesAtLevelOrBelow = null;
            action_RemoveNodesAtLevelOrBelow = (TaskNode node, int currentLevel, int n) => {
                if (node == null) return;

                // 当当前层级 >= n 时，删除节点（即将节点置为 null）
                if (currentLevel >= n) {
                    node.Children = new List<TaskNode>();
                    return;
                }
                foreach (var child in node.Children) {
                    action_RemoveNodesAtLevelOrBelow(child, currentLevel+1, n);
                }
            };
            //前序遍历树，遍历到目标节点时返回
            Func<TaskNode, string, (TaskNode, bool)> func_PreOrderSearch = null;
            func_PreOrderSearch = (TaskNode node, string targetName) => {
                if (node == null) {
                    return (null, false); // 如果节点为空，则返回 null
                }

                // 如果当前节点的值是目标值，返回当前节点
                if (node.ItemName == targetName) {
                    return (null, true);
                }

                TaskNode currentNode = new TaskNode() {
                    ItemName = node.ItemName,
                    Data_OutputParams = node.Data_OutputParams,
                };

                foreach (var item in node.Children) {
                    var (resultItem, resultState) = func_PreOrderSearch(item, targetName);
                    if (resultItem == null && resultState == false) {
                        continue;
                    }
                    else if (resultItem == null && resultState == true) {
                        return (currentNode, true);
                    }
                    else if (resultItem != null && resultState == false) {
                        currentNode.Children.Add(resultItem);
                    }
                    else if (resultItem != null && resultState == true) {
                        currentNode.Children.Add(resultItem);
                        return (currentNode, true);
                    }
                    else { 
                    
                    }
                }
                return (currentNode, false);
            };


            //获取当前流程所有信息
            FlowNode currentFlow = SummarizeFlowNodes();
            //获取当前节点所有信息
            TaskNode currentTask = SummarizeSingleNode();

            //获取当前item在第几层
            int currentItemLevel = -1;
            foreach (var item in currentFlow.Children) {
                currentItemLevel = func_GetNodeLevel(item, currentTask.ItemName, 0);
                if (currentItemLevel != -1) {
                    break;
                }
            }
            if (currentItemLevel == -1) {
                return null;
            }

            //去掉当前item层级（不包含）以下的信息
            foreach (var item in currentFlow.Children) {
                action_RemoveNodesAtLevelOrBelow(item, 0, currentItemLevel);
            }

            //获取查找路径所有遍历的集合
            FlowNode resultFlow = new FlowNode() { 
                Name = currentFlow.Name,
                AllowRun = currentFlow.AllowRun,
            };
            foreach (var item in currentFlow.Children) {
                var (resultItem, resultState) = func_PreOrderSearch(item, currentTask.ItemName);
                //如果没找到，而且遍历的item不为空
                if (!resultState) {
                    resultItem.Children = new List<TaskNode>();
                    resultFlow.Children.Add(resultItem);
                }
                else {
                    resultFlow.Children.Add(resultItem);
                    break;
                }
            }
            return resultFlow;
        }

        /// <summary>
        /// 界面转json保存
        /// </summary>
        /// <returns></returns>
        public string DataToJson() {
            JObject json = new JObject();
            //存储tab页
            for (int i = 0; i < TabControl_Combination.Items.Count; i = i + 1) {
                TabItem tab = TabControl_Combination.Items[i] as TabItem;
                if (tab == null) {
                    return string.Empty;
                }

                json[i.ToString() + "_Header"] = tab.Header.ToString();
                json[i.ToString() + "_Visibility"] = tab.Visibility.ToString();

                Button button_runOne = GetButton_RunOneFromTab(tab);
                if (button_runOne == null) {
                    return string.Empty;
                }
                json[i.ToString() + "_RunOne"] = button_runOne.Content.ToString();

                Button button_runLoop = GetButton_RunLoopFromTab(tab);
                if (button_runLoop == null) {
                    return string.Empty;
                }
                json[i.ToString() + "_RunLoop"] = button_runLoop.Content.ToString();

                Button button_runStop = GetButton_RunStopFromTab(tab);
                if (button_runStop == null) {
                    return string.Empty;
                }
                json[i.ToString() + "_RunStop"] = button_runStop.Content.ToString();

                TreeView treeView = GetTreeViewFromTab(tab);
                if (treeView == null) {
                    return string.Empty;
                }
                json[i.ToString() + "_TreeView"] = GetTreeNodesDataFromTreeView(treeView);
            }

            return json.ToString();
        }

        /// <summary>
        /// json转界面恢复
        /// </summary>
        public void JsonToData(string str) {
            if (str == null || str == "") {
                return;
            }
            ResetData();
            JObject json = JObject.Parse(str);

            int i = 0;
            while (json != null) {
                if (!json.ContainsKey(i.ToString() + "_Header")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_Visibility")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_RunOne")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_RunLoop")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_RunStop")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_TreeView")) {
                    break;
                }

                string header = json[i.ToString() + "_Header"].ToString();
                TabItem tab = AddTab(header);

                string visibility = json[i.ToString() + "_Visibility"].ToString();
                tab.Visibility = (Visibility)Enum.Parse(typeof(Visibility), visibility);

                Button button_runOne = GetButton_RunOneFromTab(tab);
                if (button_runOne == null) {
                    return;
                }
                button_runOne.Content = json[i.ToString() + "_RunOne"].ToString();

                Button button_runLoop = GetButton_RunLoopFromTab(tab);
                if (button_runLoop == null) {
                    return;
                }
                button_runLoop.Content = json[i.ToString() + "_RunLoop"].ToString();

                Button button_runStop = GetButton_RunStopFromTab(tab);
                if (button_runStop == null) {
                    return;
                }
                button_runStop.Content = json[i.ToString() + "_RunStop"].ToString();

                TreeView treeView = GetTreeViewFromTab(tab);
                if (treeView == null) {
                    return;
                }
                SetTreeNodesDataToTreeView(treeView, json[i.ToString() + "_TreeView"].ToString());

                i++;
            }


        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else if (message.Function == "AddTab" && message.Content is string params_AddTab) {
                AddTab(params_AddTab);
            }
            else if (message.Function == "RemoveTab" && message.Content is string params_RemoveTab) {
                RemoveTab(params_RemoveTab);
            }
            else if (message.Function == "RenameTab" && message.Content is string params_RenameTab) {
                RenameTab(JObject.Parse(params_RenameTab));
            }
            else if (message.Function == "SwitchToTab" && message.Content is string params_SwitchToTab) {
                SwitchToTab(params_SwitchToTab);
            }
            else if (message.Function == "AddItem" && message.Content is string params_AddItem) {
                AddItem(JObject.Parse(params_AddItem));
            }
            else if (message.Function == "SummarizeProcessNodes") {
                return SummarizeProcessNodes();
            }
            else if (message.Function == "SummarizeFlowNodes") {
                return SummarizeFlowNodes();
            }
            else if (message.Function == "SummarizeFlowNodes") {
                return SummarizeSingleNode();
            }
            else if (message.Function == "DataToJson") {
                return DataToJson();
            }
            else if (message.Function == "JsonToData" && message.Content is string params_JsonToData) {
                JsonToData(params_JsonToData);
            }
            else if (message.Function == "SummarizeBeforeNodes") {
                return SummarizeBeforeNodes();
            }
            else {

            }
            return string.Empty;
        }

        public void SetSendMessageCallback(SendMessage cb) {
            if (cb == null) {
                return;
            }
            _sendMessage = cb;
        }

        public void SetClickRunOnceCallback(RoutedEventHandler runOnce) {
            if (runOnce == null) { 
                return;
            }
            _clickRunOnce = runOnce;
        }

        public void SetClickRunLoopCallback(RoutedEventHandler runLoop) {
            if (runLoop == null) {
                return;
            }
            _clickRunLoop = runLoop;
        }

        public void SetClickRunStopCallback(RoutedEventHandler runStop) {
            if (runStop == null) {
                return;
            }
            _clickRunStop = runStop;
        }

        public void SetClickRunResurveyCallback(RoutedEventHandler runResurvey) {
            if (runResurvey == null) {
                return;
            }
            _clickRunResurvey = runResurvey;
        }
    }
}
