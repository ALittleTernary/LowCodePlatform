using LowCodePlatform.Engine;
using LowCodePlatform.Plugin;
using LowCodePlatform.Plugin.Base;
using LowCodePlatform.View.Base;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
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

namespace LowCodePlatform.View
{
    /// <summary>
    /// OptionArea.xaml 的交互逻辑
    /// </summary>
    public partial class OptionArea : UserControl, CommunicationUser {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        /// <summary>
        /// 拖拽使用的装饰器
        /// </summary>
        private AdornerLayer _adornerLayer = null;

        private TreeViewItem _treeViewItem_ResourceObtain = null;
        private TreeViewItem _treeViewItem_ResourcePublic = null;
        private TreeViewItem _treeViewItem_DataProcess = null;
        private TreeViewItem _treeViewItem_VariableHandle = null;
        private TreeViewItem _treeViewItem_ControlStatement = null;
        private TreeViewItem _treeViewItem_DataDisplay = null;

        public OptionArea() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            //资源获取
            _treeViewItem_ResourceObtain = new TreeViewItem() { Header = "资源获取" , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            TreeView_Option.Items.Add(_treeViewItem_ResourceObtain);

            //资源发布
            _treeViewItem_ResourcePublic = new TreeViewItem() { Header = "资源发布", ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            TreeView_Option.Items.Add(_treeViewItem_ResourcePublic);

            //数据处理
            _treeViewItem_DataProcess = new TreeViewItem() { Header = "数据处理" , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            TreeView_Option.Items.Add(_treeViewItem_DataProcess);

            //变量处理
            _treeViewItem_VariableHandle = new TreeViewItem() { Header = "变量处理" , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            //_treeViewItem_VariableHandle.Items.Add(new TreeViewItem() { Header = "新建局部变量", Tag = ItemOperationType.kCommon });
            //_treeViewItem_VariableHandle.Items.Add(new TreeViewItem() { Header = "修改变量数据", Tag = ItemOperationType.kCommon });
            TreeView_Option.Items.Add(_treeViewItem_VariableHandle);

            //控制语句
            _treeViewItem_ControlStatement = new TreeViewItem() { Header = "控制语句" , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "if", Tag = ItemOperationType.kIf });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "else if", Tag = ItemOperationType.kElseIf });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "else", Tag = ItemOperationType.kElse });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "for", Tag = ItemOperationType.kFor });
            //_treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "while", Tag = ItemOperationType.kWhile });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "break", Tag = ItemOperationType.kBreak });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "continue", Tag = ItemOperationType.kContinue });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "串行执行", Tag = ItemOperationType.kSerial });
            _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "并行执行", Tag = ItemOperationType.kParallel });
            //_treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "停止运行当前流程", Tag = ItemOperationType.kStopFlow });
            //_treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "重新运行当前流程", Tag = ItemOperationType.kReRunFlow });
            //_treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "停止运行所有流程", Tag = ItemOperationType.kStopProcess });
            //_treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = "重新运行所有流程", Tag = ItemOperationType.kReRunProcess });
            TreeView_Option.Items.Add(_treeViewItem_ControlStatement);

            //数据显示
            _treeViewItem_DataDisplay = new TreeViewItem() { Header = "数据显示" , ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle") };
            TreeView_Option.Items.Add(_treeViewItem_DataDisplay);
        }

        private void InitEvent() {
            TreeView_Option.MouseDoubleClick += Event_TreeView_Option_MouseDoubleClick;
            TreeView_Option.MouseMove += Event_TreeView_MouseMove;
            TreeView_Option.QueryContinueDrag += Event_TreeView_QueryContinueDrag;
        }

        private void Event_TreeView_Option_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            DependencyObject dependencyObject = e.OriginalSource as DependencyObject;
            if (dependencyObject == null) { 
                return;
            }
            TreeViewItem clickItem = null;
            while (dependencyObject != null) {
                if (dependencyObject is TreeViewItem ancestor) {
                    clickItem = ancestor;
                    break;
                }
                dependencyObject = VisualTreeHelper.GetParent(dependencyObject);
            }

            if (clickItem == null) {
                return;
            }
            if (clickItem.Items.Count > 0  || clickItem.Tag == null) { 
                return;
            }
            // 获取 Header
            string header = clickItem.Header.ToString();
            ItemOperationType tag = (ItemOperationType)clickItem.Tag;
            JObject json = new JObject();
            json["header"] = header;
            json["tag"] = tag.ToString();
            _sendMessage?.Invoke(new CommunicationCenterMessage("ProjectArea", "CombinationArea", "AddItem", json.ToString()));
        }

        /// <summary>
        /// 鼠标移动事件，启动拖拽事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_TreeView_MouseMove(object sender, MouseEventArgs e) {
            if (Mouse.LeftButton != MouseButtonState.Pressed)
                return;

            Point pos = e.GetPosition(TreeView_Option);
            HitTestResult result = VisualTreeHelper.HitTest(TreeView_Option, pos);
            if (result == null)
                return;

            //拿到当前你想拖动的item
            TreeViewItem dragTreeItem = FindVisualParent<TreeViewItem>(result.VisualHit);

            //没有子项就是文件夹，别传文件夹名字
            if (dragTreeItem == null || dragTreeItem.Items.Count > 0) { 
                return;
            }

            if (dragTreeItem?.Tag == null) { 
                return;
            }
            //把tag的拿一下
            ItemOperationType operationType = (ItemOperationType)dragTreeItem.Tag;
            if (operationType == ItemOperationType.kNone) { 
                return;
            }

            //判断想拖动的item在不在禁止名单里
            var header = dragTreeItem.Header.ToString();

            if (dragTreeItem == null || dragTreeItem != TreeView_Option.SelectedItem || !(TreeView_Option.SelectedItem is TreeViewItem))
                return;

            //装饰器，装饰一下拖拽的项
            DragDropAdorner adorner = new DragDropAdorner(dragTreeItem);
            _adornerLayer = AdornerLayer.GetAdornerLayer(TreeView_Option);
            _adornerLayer.Add(adorner);

            DataObject data = new DataObject();

            //数据来源给个名字，因为Combination也会拖拽过来
            CombinationArea_TreeItem sourceItem = new CombinationArea_TreeItem() {
                ItemName = dragTreeItem.Header.ToString() ,
                OperationType = operationType
            };

            //直接在这里构造CombinationArea_TreeItem，然后把CombinationArea_TreeItem发送过去
            data.SetData("Option", new TreeViewItem() { Header = sourceItem, ItemContainerStyle = (Style)FindResource("StretchTreeViewItemStyle")});
            //发起拖动的控件/发送数据/移动方式
            DragDrop.DoDragDrop(TreeView_Option, data, DragDropEffects.Move);
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
            //装饰器更新
            _adornerLayer.Update();
        }

        public void InitOptionAreaOptions(List<(TaskPluginType, string)> data) {
            foreach (var item in data) {
                if (item.Item1 == TaskPluginType.kControlStatement && (item.Item2 == "if" || item.Item2 == "else if" || item.Item2 == "for" || item.Item2 == "while")) {
                    continue;
                }
                AddOptionAreaItem(item.Item1, item.Item2);
            }
        }

        /// <summary>
        /// 选项区添加item，这玩意是给外部添加的，因此所有添加的项，都是禁止有子类但是允许双击的，如果有额外需求，那就在引擎中实现，不开放给插件
        /// </summary>
        /// <param name="taskType"></param>
        /// <param name="name"></param>
        public void AddOptionAreaItem(TaskPluginType taskType, string name) {
            switch (taskType) {
                case TaskPluginType.kResourceObtain:
                    _treeViewItem_ResourceObtain.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                case TaskPluginType.kResourcePublic:
                    _treeViewItem_ResourcePublic.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                case TaskPluginType.kVariableHandle:
                    _treeViewItem_VariableHandle.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                case TaskPluginType.kControlStatement:
                    _treeViewItem_ControlStatement.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                case TaskPluginType.kDataProcess:
                    _treeViewItem_DataProcess.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                case TaskPluginType.kDataDisplay:
                    _treeViewItem_DataDisplay.Items.Add(new TreeViewItem() { Header = name, Tag = ItemOperationType.kCommon });
                    break;
                default:
                    break;
            }
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

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else { 
            
            }

            return string.Empty;
        }

        public void SetSendMessageCallback(SendMessage cb) {
            _sendMessage = cb;
        }

        public string DataToJson() {
            return string.Empty;
        }

        public void JsonToData(string str) {

        }
    }
}
