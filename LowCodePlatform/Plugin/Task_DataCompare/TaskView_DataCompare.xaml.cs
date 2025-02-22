using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_BlobAnalysis;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

namespace LowCodePlatform.Plugin.Task_DataCompare
{
    public class CompareData : INotifyPropertyChanged
    {
        private string _dataType = string.Empty;
        public string DataType
        {
            get { return _dataType; }
            set {
                _dataType = value;
                OnPropertyChanged(nameof(DataType));
            }
        }

        private string _leftSurfaceValue = string.Empty;
        public string LeftSurfaceValue
        {
            get { return _leftSurfaceValue; }
            set {
                _leftSurfaceValue = value;
                OnPropertyChanged(nameof(LeftSurfaceValue));
            }
        }

        public string LeftSurfaceValueTip { get; set; } = string.Empty;

        private string _compare = string.Empty;
        public string Compare
        {
            get { return _compare; }
            set {
                _compare = value;
                OnPropertyChanged(nameof(Compare));
            }
        }

        private string _rightSurfaceValue = string.Empty;
        public string RightSurfaceValue
        {
            get { return _rightSurfaceValue; }
            set {
                _rightSurfaceValue = value;
                OnPropertyChanged(nameof(RightSurfaceValue));
            }
        }

        public string RightSurfaceValueTip { get; set; } = string.Empty;

        private string _leftActualValue = string.Empty;
        public string LeftActualValue
        {
            get { return _leftActualValue; }
            set {
                _leftActualValue = value;
                OnPropertyChanged(nameof(LeftActualValue));
            }
        }

        private string _rightActualValue = string.Empty;
        public string RightActualValue
        {
            get { return _rightActualValue; }
            set {
                _rightActualValue = value;
                OnPropertyChanged(nameof(RightActualValue));
            }
        }

        private string _result = string.Empty;
        public string Result
        {
            get { return _result; }
            set {
                _result = value;
                OnPropertyChanged(nameof(Result));
            }
        }

        public string Notes { get; set; } = string.Empty;

        public string DataToJson() {
            JObject json = new JObject();
            json["DataType"] = DataType;
            json["LeftSurfaceValue"] = LeftSurfaceValue;
            json["LeftSurfaceValueTip"] = LeftSurfaceValueTip;
            json["Compare"] = Compare;
            json["RightSurfaceValue"] = RightSurfaceValue;
            json["RightSurfaceValueTip"] = RightSurfaceValueTip;
            json["LeftActualValue"] = LeftActualValue;
            json["RightActualValue"] = RightActualValue;
            json["Result"] = Result;
            json["Notes"] = Notes;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            DataType = json["DataType"].ToString();
            LeftSurfaceValue = json["LeftSurfaceValue"].ToString();
            LeftSurfaceValueTip = json["LeftSurfaceValueTip"].ToString();
            Compare = (json["Compare"].ToString());
            RightSurfaceValue = (json["RightSurfaceValue"].ToString());
            RightSurfaceValueTip = json["RightSurfaceValueTip"].ToString();
            LeftActualValue = (json["LeftActualValue"].ToString());
            RightActualValue = (json["RightActualValue"].ToString());
            Result = (json["Result"].ToString());
            Notes = json["Notes"].ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// TaskView_DataCompare.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_DataCompare : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;

        private ExecuteClick _executeClick = null;

        private LinkClick _linkClick = null;

        public TaskView_DataCompare() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            MenuItem menuItem_Int = new MenuItem() { Header = "Int" };
            menuItem_Int.Click += Event_MenuItem_Click;
            ContextMenu_DataType.Items.Add(menuItem_Int);

            MenuItem menuItem_Float = new MenuItem() { Header = "Float" };
            menuItem_Float.Click += Event_MenuItem_Click;
            ContextMenu_DataType.Items.Add(menuItem_Float);

            MenuItem menuItem_Double = new MenuItem() { Header = "Double" };
            menuItem_Double.Click += Event_MenuItem_Click;
            ContextMenu_DataType.Items.Add(menuItem_Double);

        }

        private void InitEvent() {
            Button_AddItem.Click += Event_Button_AddItem;
            DataGrid_DataCompare.SelectionChanged += Event_DataGrid_SelectionItemChanged;
            Button_SubItem.Click += Event_Button_DeleteItem;
            Button_MoveUp.Click += Event_Button_MoveUpItem;
            Button_MoveDown.Click += Event_Button_MoveDownItem;

            LinkEdit_LeftValue.LinkContentTextChanged += Event_LinkEdit_LeftValue_TextChanged;
            ComboBox_CompareType.SelectionChanged += Event_ComboBox_CompareType_SelectChanged;
            LinkEdit_RightValue.LinkContentTextChanged += Event_LinkEdit_RightValue_TextChanged;
            TextBox_Notes.TextChanged += Event_TextBox_Note_TextChanged;

            Button_Execute.Click += (s, e) => {
                // 获取 ItemsSource 的数据源
                ObservableCollection<CompareData> sourceDatas = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
                if (sourceDatas == null) {
                    return;
                }

                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "比较数量",
                    IsBind = false,
                    UserParam = sourceDatas.Count,
                });
                foreach (var data in sourceDatas) {
                    LinkEdit leftLinkEdit = new LinkEdit();
                    LinkEdit rightLinkEdit = new LinkEdit();
                    if (data.DataType == "Int") {
                        leftLinkEdit.LinkContentType = LinkDataType.kInt;
                        rightLinkEdit.LinkContentType = LinkDataType.kInt;
                    }
                    else if (data.DataType == "Float") {
                        leftLinkEdit.LinkContentType = LinkDataType.kFloat;
                        rightLinkEdit.LinkContentType = LinkDataType.kFloat;
                    }
                    else if (data.DataType == "Double") {
                        leftLinkEdit.LinkContentType = LinkDataType.kDouble;
                        rightLinkEdit.LinkContentType = LinkDataType.kDouble;
                    }
                    else {
                        return;
                    }
                    leftLinkEdit.JsonToView(data.LeftSurfaceValueTip);
                    rightLinkEdit.JsonToView(data.RightSurfaceValueTip);

                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "数据类型",
                        IsBind = false,
                        UserParam = data.DataType,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "左值",
                        IsBind = leftLinkEdit.IsBind,
                        UserParam = leftLinkEdit.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "比较类型",
                        IsBind = false,
                        UserParam = data.Compare,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "右值",
                        IsBind = rightLinkEdit.IsBind,
                        UserParam = rightLinkEdit.UserParam,
                    });
                }
                _executeClick?.Invoke(inputParams);
            };
            Button_Confirm.Click += (s, e) => {
                // 获取 ItemsSource 的数据源
                ObservableCollection<CompareData> sourceDatas = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
                if (sourceDatas == null) {
                    return;
                }

                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "比较数量",
                    IsBind = false,
                    UserParam = sourceDatas.Count,
                });
                foreach (var data in sourceDatas) {
                    LinkEdit leftLinkEdit = new LinkEdit();
                    LinkEdit rightLinkEdit = new LinkEdit();
                    if (data.DataType == "Int") {
                        leftLinkEdit.LinkContentType = LinkDataType.kInt;
                        rightLinkEdit.LinkContentType = LinkDataType.kInt;
                    }
                    else if (data.DataType == "Float") {
                        leftLinkEdit.LinkContentType = LinkDataType.kFloat;
                        rightLinkEdit.LinkContentType = LinkDataType.kFloat;
                    }
                    else if (data.DataType == "Double") {
                        leftLinkEdit.LinkContentType = LinkDataType.kDouble;
                        rightLinkEdit.LinkContentType = LinkDataType.kDouble;
                    }
                    else {
                        return;
                    }
                    leftLinkEdit.JsonToView(data.LeftSurfaceValueTip);
                    rightLinkEdit.JsonToView(data.RightSurfaceValueTip);

                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "数据类型",
                        IsBind = false,
                        UserParam = leftLinkEdit.LinkContentType.ToString(),
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "左值",
                        IsBind = leftLinkEdit.IsBind,
                        UserParam = leftLinkEdit.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "比较类型",
                        IsBind = false,
                        UserParam = data.Compare,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = "右值",
                        IsBind = rightLinkEdit.IsBind,
                        UserParam = rightLinkEdit.UserParam,
                    });
                }
                _confirmClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(int)) {
                return;
            }
            int count = (int)outputParams[0].ActualParam;

            // 获取 ItemsSource 的数据源
            ObservableCollection<CompareData> sourceDatas = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (sourceDatas == null) {
                return;
            }
            if (sourceDatas.Count != count) {
                return;
            }
            for (int i = 0; i < count; i++) {
                CompareData data = sourceDatas[i];
                if (outputParams.Count < 2 + 3 * i + 0 || outputParams[1 + 3 * i + 0].ActualParam.GetType() != typeof(string)) {
                    return;
                }
                if (outputParams.Count < 2 + 3 * i + 1 || outputParams[1 + 3 * i + 1].ActualParam.GetType() != typeof(string)) {
                    return;
                }
                if (outputParams.Count < 2 + 3 * i + 2 || outputParams[1 + 3 * i + 2].ActualParam.GetType() != typeof(string)) {
                    return;
                }
                data.LeftActualValue = outputParams[1 + 3 * i + 0].ActualParam as string;
                data.RightActualValue = outputParams[1 + 3 * i + 1].ActualParam as string;
                data.Result = outputParams[1 + 3 * i + 2].ActualParam as string;
            }
        }

        public string ViewToJson() {
            JObject json = new JObject();
            ObservableCollection<CompareData> sourceDatas = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (sourceDatas == null) {
                return json.ToString();
            }
            json["DataGrid_DataCompare_Count"] = sourceDatas.Count;
            for (int i = 0; i < sourceDatas.Count; i++) {
                CompareData data = sourceDatas[i];
                json["DataGrid_DataCompare_Data" + i] = data.DataToJson();
            }
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            ObservableCollection<CompareData> sourceDatas = new ObservableCollection<CompareData>();
            if (sourceDatas == null) {
                return;
            }

            JObject json = JObject.Parse(str);
            int count = ((int)json["DataGrid_DataCompare_Count"]);

            for (int i = 0; i < count; i++) {
                CompareData data = new CompareData();
                data.JsonToData(json["DataGrid_DataCompare_Data" + i].ToString());
                sourceDatas.Add(data);
            }

            DataGrid_DataCompare.ItemsSource = sourceDatas;
        }

        public void ResetView() {
            DataGrid_DataCompare.ItemsSource = new  ObservableCollection<CompareData>();
            LinkEdit_LeftValue.ResetView();
            LinkEdit_RightValue.ResetView();
            ComboBox_CompareType.SelectedIndex = 0;
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
            LinkEdit_LeftValue.SetLinkClickCallback(linkClickCallback);
            LinkEdit_RightValue.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "数据比较";
                case LangaugeType.kEnglish:
                    return "DataCompare";
                default:
                    break;
            }
            return string.Empty;
        }

        private void Event_Button_AddItem(object sender, RoutedEventArgs e) {
            ContextMenu_DataType.IsOpen = true;
        }

        private void Event_MenuItem_Click(object sender, RoutedEventArgs e) {
            ContextMenu_DataType.IsOpen = false;
            var menuItem = sender as MenuItem;
            if (menuItem == null) {
                return;
            }
            string str_menuItem = menuItem.Header as string;
            if (str_menuItem == null) { 
                return ;
            }

            // 获取 ItemsSource 的数据源
            ObservableCollection<CompareData> sourceDatas = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (sourceDatas == null) {
                return;
            }

            CompareData data = new CompareData() {
                DataType = str_menuItem,
            };
            sourceDatas.Add(data);
            DataGrid_DataCompare.ItemsSource = sourceDatas;

            //设置当前选中项为新增的项，把数据显示一下，这个会触发Event_DataGrid_SelectionItemChanged
            DataGrid_DataCompare.SelectedItem = data;
        }

        /// <summary>
        /// 点击按钮删除当前选中的item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_DeleteItem(object sender, RoutedEventArgs e) {
            var selectedItem = DataGrid_DataCompare.SelectedItem;
            if (selectedItem == null) {
                return;
            }
            // 假设你的数据集合是 ObservableCollection<T>
            var itemList = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (itemList == null) {
                return;
            }
            itemList.Remove(selectedItem as CompareData);  // 删除选中的项
        }

        /// <summary>
        /// 点击按钮上移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveUpItem(object sender, RoutedEventArgs e) {
            CompareData selectedItem = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectedItem == null) {
                return;
            }
            var itemList = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (itemList == null) {
                return;
            }
            int index = itemList.IndexOf(selectedItem);
            // 如果选中的项不是第一个元素
            if (index <= 0) {
                return;
            }
            // 将选中的项和前一个项交换位置
            itemList.RemoveAt(index);
            itemList.Insert(index - 1, selectedItem);
            // 更新选中项
            DataGrid_DataCompare.SelectedItem = selectedItem;
        }

        /// <summary>
        /// 点击按钮下移当前item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_MoveDownItem(object sender, RoutedEventArgs e) {
            CompareData selectedItem = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectedItem == null) {
                return;
            }
            var itemList = DataGrid_DataCompare.ItemsSource as ObservableCollection<CompareData>;
            if (itemList == null) {
                return;
            }
            int index = itemList.IndexOf(selectedItem);
            // 如果选中的项不是最后一个元素
            if (index >= itemList.Count - 1) {
                return;
            }
            // 将选中的项和下一个项交换位置
            itemList.RemoveAt(index);
            itemList.Insert(index + 1, selectedItem);

            // 更新选中项
            DataGrid_DataCompare.SelectedItem = selectedItem;
        }


        private void Event_DataGrid_SelectionItemChanged(object sender, SelectionChangedEventArgs e) {
            CompareData selectData = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectData == null) {
                return;
            }
            LinkEdit_LeftValue.LinkContentTextChanged -= Event_LinkEdit_LeftValue_TextChanged;
            ComboBox_CompareType.SelectionChanged -= Event_ComboBox_CompareType_SelectChanged;
            LinkEdit_RightValue.LinkContentTextChanged -= Event_LinkEdit_RightValue_TextChanged;
            TextBox_Notes.TextChanged -= Event_TextBox_Note_TextChanged;


            //数据重置初始状态
            LinkEdit_LeftValue.ResetView();
            ComboBox_CompareType.SelectedIndex = 0;
            LinkEdit_RightValue.ResetView();
            TextBox_Notes.Text = string.Empty;

            //根据data还原状态
            if (selectData.DataType == "Int") {
                LinkEdit_LeftValue.LinkContentType = LinkDataType.kInt;
                LinkEdit_RightValue.LinkContentType = LinkDataType.kInt;
            }
            else if (selectData.DataType == "Float") {
                LinkEdit_LeftValue.LinkContentType = LinkDataType.kFloat;
                LinkEdit_RightValue.LinkContentType = LinkDataType.kFloat;
            }
            else if (selectData.DataType == "Double") {
                LinkEdit_LeftValue.LinkContentType = LinkDataType.kDouble;
                LinkEdit_RightValue.LinkContentType = LinkDataType.kDouble;
            }
            else { 
                return;
            }

            LinkEdit_LeftValue.JsonToView(selectData.LeftSurfaceValueTip);
            foreach (ComboBoxItem item in ComboBox_CompareType.Items) {
                if (item.Content.ToString() != selectData.Compare) {
                    continue;
                }
                ComboBox_CompareType.SelectedItem = item;
                break;
            }
            LinkEdit_RightValue.JsonToView(selectData.RightSurfaceValueTip);

            TextBox_Notes.Text = selectData.Notes;

            LinkEdit_LeftValue.LinkContentTextChanged += Event_LinkEdit_LeftValue_TextChanged;
            LinkEdit_RightValue.LinkContentTextChanged += Event_LinkEdit_RightValue_TextChanged;
            ComboBox_CompareType.SelectionChanged += Event_ComboBox_CompareType_SelectChanged;
            TextBox_Notes.TextChanged += Event_TextBox_Note_TextChanged;
        }

        private void Event_LinkEdit_LeftValue_TextChanged(object sender, TextChangedEventArgs e) {
            CompareData selectData = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectData == null) {
                return;
            }
            selectData.LeftSurfaceValue = LinkEdit_LeftValue.LinkContentText;
            selectData.LeftSurfaceValueTip = LinkEdit_LeftValue.ViewToJson();
            selectData.Compare = (ComboBox_CompareType.SelectedItem as ComboBoxItem).Content as string;
            selectData.RightSurfaceValue = LinkEdit_RightValue.LinkContentText;
            selectData.RightSurfaceValueTip = LinkEdit_RightValue.ViewToJson();
            selectData.Notes = TextBox_Notes.Text;
        }

        private void Event_LinkEdit_RightValue_TextChanged(object sender, TextChangedEventArgs e) {
            CompareData selectData = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectData == null) {
                return;
            }
            selectData.LeftSurfaceValue = LinkEdit_LeftValue.LinkContentText;
            selectData.LeftSurfaceValueTip = LinkEdit_LeftValue.ViewToJson();
            selectData.Compare = (ComboBox_CompareType.SelectedItem as ComboBoxItem).Content as string;
            selectData.RightSurfaceValue = LinkEdit_RightValue.LinkContentText;
            selectData.RightSurfaceValueTip = LinkEdit_RightValue.ViewToJson();
            selectData.Notes = TextBox_Notes.Text;
        }

        private void Event_ComboBox_CompareType_SelectChanged(object sender, SelectionChangedEventArgs e) {
            CompareData selectData = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectData == null) {
                return;
            }
            selectData.LeftSurfaceValue = LinkEdit_LeftValue.LinkContentText;
            selectData.LeftSurfaceValueTip = LinkEdit_LeftValue.ViewToJson();
            selectData.Compare = (ComboBox_CompareType.SelectedItem as ComboBoxItem).Content as string;
            selectData.RightSurfaceValue = LinkEdit_RightValue.LinkContentText;
            selectData.RightSurfaceValueTip = LinkEdit_RightValue.ViewToJson();
            selectData.Notes = TextBox_Notes.Text;
        }

        private void Event_TextBox_Note_TextChanged(object sender, TextChangedEventArgs e) {
            CompareData selectData = DataGrid_DataCompare.SelectedItem as CompareData;
            if (selectData == null) {
                return;
            }
            selectData.LeftSurfaceValue = LinkEdit_LeftValue.LinkContentText;
            selectData.LeftSurfaceValueTip = LinkEdit_LeftValue.ViewToJson();
            selectData.Compare = (ComboBox_CompareType.SelectedItem as ComboBoxItem).Content as string;
            selectData.RightSurfaceValue = LinkEdit_RightValue.LinkContentText;
            selectData.RightSurfaceValueTip = LinkEdit_RightValue.ViewToJson();
            selectData.Notes = TextBox_Notes.Text;
        }
    }
}
