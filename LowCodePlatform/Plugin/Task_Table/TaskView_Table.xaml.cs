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
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LowCodePlatform.Plugin.Task_Table
{
    public class TableData : INotifyPropertyChanged
    {
        private int _num;
        public int Num
        {
            get { return _num; }
            set {
                _num = value;
                OnPropertyChanged(nameof(Num));
            }
        }

        private string _row;
        public string Row
        {
            get { return _row; }
            set {
                _row = value;
                OnPropertyChanged(nameof(Row));
            }
        }

        private string _column;
        public string Column
        {
            get { return _column; }
            set {
                _column = value;
                OnPropertyChanged(nameof(Column));
            }
        }

        private string _text;
        public string Text
        {
            get { return _text; }
            set {
                _text = value;
                OnPropertyChanged(nameof(Text));
            }
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["Num"] = Num;
            json["Row"] = Row;
            json["Column"] = Column;
            json["Text"] = Text;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            Num = ((int)json["Num"]);
            Row = (json["Row"].ToString());
            Column = (json["Column"].ToString());
            Text = (json["Text"].ToString());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// TaskView_Table.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_Table : Window, TaskViewPluginBase
    {
        private ConfirmClick _confirmClick = null;
        private ExecuteClick _executeClick = null;
        private LinkClick _linkClick = null;

        public TaskView_Table() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_AddItem.Click += (s, e) => {
                // 创建一个新的 RegionAnalysisOptionData 对象
                TableData data = new TableData() {
                    Num = DataGrid_TableData.Items.Count,
                    Row = string.Empty,
                    Column = string.Empty,
                    Text = string.Empty,
                };

                // 获取 ItemsSource 的数据源
                ObservableCollection<TableData> sourceDatas = DataGrid_TableData.ItemsSource as ObservableCollection<TableData>;
                sourceDatas.Add(data);
                DataGrid_TableData.ItemsSource = sourceDatas;

                //设置当前选中项为新增的项，把数据显示一下，这个会触发Event_DataGrid_SelectionItemChanged
                DataGrid_TableData.SelectedItem = data;
            };
            Button_SubItem.Click += (s, e) => {
                object selectedItem = DataGrid_TableData.SelectedItem;
                if (selectedItem == null) {
                    return;
                }
                // 假设你的数据集合是 ObservableCollection<T>
                ObservableCollection<TableData> itemList = DataGrid_TableData.ItemsSource as ObservableCollection<TableData>;
                if (itemList == null) {
                    return;
                }
                itemList.Remove(selectedItem as TableData);  // 删除选中的项

                //重新整理序号
                for (int i = 0; i < itemList.Count; i++) {
                    itemList[i].Num = i;
                }
            };
            //切换项触发
            DataGrid_TableData.SelectionChanged += (s, e) => {
                TableData selectData = DataGrid_TableData.SelectedItem as TableData;
                if (selectData == null) {
                    return;
                }
                if (selectData.Row == string.Empty) {
                    LinkEdit_Params01.ResetView();
                }
                if (selectData.Column == string.Empty) {
                    LinkEdit_Params02.ResetView();
                }
                if (selectData.Text == string.Empty) {
                    LinkEdit_Params03.ResetView();
                }
                LinkEdit_Params01.JsonToView(selectData.Row);
                LinkEdit_Params02.JsonToView(selectData.Column);
                LinkEdit_Params03.JsonToView(selectData.Text);
            };
            LinkEdit_Params01.LinkContentTextChanged += (s, e) => {
                TableData selectData = DataGrid_TableData.SelectedItem as TableData;
                if (selectData == null) {
                    return;
                }
                selectData.Row = LinkEdit_Params01.ViewToJson();
            };
            LinkEdit_Params02.LinkContentTextChanged += (s, e) => {
                TableData selectData = DataGrid_TableData.SelectedItem as TableData;
                if (selectData == null) {
                    return;
                }
                selectData.Column = LinkEdit_Params02.ViewToJson();
            };
            LinkEdit_Params03.LinkContentTextChanged += (s, e) => {
                TableData selectData = DataGrid_TableData.SelectedItem as TableData;
                if (selectData == null) {
                    return;
                }
                selectData.Text = LinkEdit_Params03.ViewToJson();
            };
            Button_Execute.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "选择表格界面",
                    IsBind = LinkEdit_View.IsBind,
                    UserParam = LinkEdit_View.UserParam,
                });
                ObservableCollection<TableData> datas = DataGrid_TableData.ItemsSource as ObservableCollection<TableData>;
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "单元格数量",
                    IsBind = false,
                    UserParam = datas.Count,
                });
                for (int i = 0; i < datas.Count; i++) {
                    TableData data = datas[i];
                    LinkEdit linkEdit_Row = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Row.JsonToView(data.Row);
                    LinkEdit linkEdit_Column = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Column.JsonToView(data.Column);
                    LinkEdit linkEdit_Text = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Text.JsonToView(data.Text);

                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Row",
                        IsBind = linkEdit_Row.IsBind,
                        UserParam = linkEdit_Row.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Column",
                        IsBind = linkEdit_Column.IsBind,
                        UserParam = linkEdit_Column.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Text",
                        IsBind = linkEdit_Text.IsBind,
                        UserParam = linkEdit_Text.UserParam,
                    });
                }
                _executeClick?.Invoke(inputParams);
            };
            Button_Confirm.Click += (s, e) => {
                List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "选择表格界面",
                    IsBind = LinkEdit_View.IsBind,
                    UserParam = LinkEdit_View.UserParam,
                });
                ObservableCollection<TableData> datas = DataGrid_TableData.ItemsSource as ObservableCollection<TableData>;
                inputParams.Add(new TaskViewInputParams() {
                    ParamName = "单元格数量",
                    IsBind = false,
                    UserParam = datas.Count,
                });
                for (int i = 0; i < datas.Count; i++) {
                    TableData data = datas[i];
                    LinkEdit linkEdit_Row = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Row.JsonToView(data.Row);
                    LinkEdit linkEdit_Column = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Column.JsonToView(data.Column);
                    LinkEdit linkEdit_Text = new LinkEdit() { LinkContentType = LinkDataType.kString };
                    linkEdit_Text.JsonToView(data.Text);

                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Row",
                        IsBind = linkEdit_Row.IsBind,
                        UserParam = linkEdit_Row.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Column",
                        IsBind = linkEdit_Column.IsBind,
                        UserParam = linkEdit_Column.UserParam,
                    });
                    inputParams.Add(new TaskViewInputParams() {
                        ParamName = i + "_Text",
                        IsBind = linkEdit_Text.IsBind,
                        UserParam = linkEdit_Text.UserParam,
                    });
                }
                _confirmClick?.Invoke(inputParams);
            };
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {

        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            LinkEdit_View.JsonToView(json["LinkEdit_View"].ToString());
            int count = ((int)json["datas_Count"]);
            ObservableCollection<TableData> datas = new ObservableCollection<TableData>();
            for (int i = 0; i < count; i++) {
                TableData data = new TableData();
                data.Row = json[i + "_data_Row"].ToString();
                data.Column = json[i + "_data_Column"].ToString();
                data.Text = json[i + "_data_Text"].ToString();
                datas.Add(data);
            }
            DataGrid_TableData.ItemsSource = datas;
        }

        public void ResetView() {
            DataGrid_TableData.ItemsSource = new ObservableCollection<TableData>();
            LinkEdit_View.ResetView();
            LinkEdit_Params01.ResetView();
            LinkEdit_Params02.ResetView();
            LinkEdit_Params03.ResetView();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["LinkEdit_View"] = LinkEdit_View.ViewToJson();
            ObservableCollection<TableData> datas = DataGrid_TableData.ItemsSource as ObservableCollection<TableData>;
            json["datas_Count"] = datas.Count;
            for (int i = 0; i < datas.Count; i++) {
                TableData data = datas[i];
                json[i + "_data_Row"] = data.Row;
                json[i + "_data_Column"] = data.Column;
                json[i + "_data_Text"] = data.Text;
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
                return ;
            }
            _linkClick = linkClickCallback;
            LinkEdit_View.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Params01.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Params02.SetLinkClickCallback(linkClickCallback);
            LinkEdit_Params03.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "表格";
                case LangaugeType.kEnglish:
                    return "Table";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
