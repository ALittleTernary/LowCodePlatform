using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Table;
using LowCodePlatform.View;
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

namespace LowCodePlatform.Plugin.Sub_Table
{
    /// <summary>
    /// SubView_Table.xaml 的交互逻辑
    /// </summary>
    public partial class SubView_Table : UserControl, SubViewPluginBase
    {
        public Dictionary<LangaugeType, string> UniqueName { get; set; } = new Dictionary<LangaugeType, string>() {
            {LangaugeType.kChinese, "表格"},
            {LangaugeType.kEnglish, "Table" },
        };

        private bool _viewEditStatus = false;

        public SubView_Table() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Label_First.MouseDoubleClick += Event_Label_DoubleClick;

            //编辑状态下允许改变标题名字
            Label_Title.MouseDoubleClick += (s, e) => {
                if (_viewEditStatus == false) {
                    return;
                }

                InputDialog inputDialog = new InputDialog() { Label = "重命名", DefaultText = Label_Title.Content.ToString() };
                inputDialog.ShowDialog();
                string userInput = inputDialog.InputText;
                if (userInput == null) {
                    return;
                }
                if (userInput == Label_Title.Content.ToString()) {
                    return;
                }
                Label_Title.Content = userInput;
            };

            Button_AddRow.Click += (s, e) => {
                int rowCount = Grid_Table.RowDefinitions.Count;
                RowDefinition rowDefinition = new RowDefinition() { Height = new GridLength(3, GridUnitType.Auto) };
                Grid_Table.RowDefinitions.Add(rowDefinition);

                // 根据列数添加Label
                for (int i = 0; i < Grid_Table.ColumnDefinitions.Count; i++) {
                    Label newLabel = new Label { Content = "Label-" + rowCount + "-" + i};
                    Grid.SetRow(newLabel, rowCount);
                    Grid.SetColumn(newLabel, i);
                    Grid_Table.Children.Add(newLabel);

                    newLabel.MouseDoubleClick += Event_Label_DoubleClick;
                }
            };

            Button_SubRow.Click += (s, e) => {
                int rowCount = Grid_Table.RowDefinitions.Count;
                if (rowCount > 1) {
                    // 创建一个列表存储待删除的控件
                    List<UIElement> toRemove = new List<UIElement>();

                    foreach (UIElement child in Grid_Table.Children) {
                        if (Grid.GetRow(child) == rowCount - 1) {
                            toRemove.Add(child);
                        }
                    }

                    // 删除所有待删除的控件
                    foreach (UIElement element in toRemove) {
                        Grid_Table.Children.Remove(element);
                    }

                    Grid_Table.RowDefinitions.RemoveAt(rowCount - 1);
                }
            };

            Button_AddColumn.Click += (s, e) => {
                int columnCount = Grid_Table.ColumnDefinitions.Count;
                ColumnDefinition columnDefinition = new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) };
                Grid_Table.ColumnDefinitions.Add(columnDefinition);

                // 根据行数添加Label
                for (int i = 0; i < Grid_Table.RowDefinitions.Count; i++) {
                    Label newLabel = new Label { Content = "Label-" + i + "-" + columnCount };
                    Grid.SetRow(newLabel, i);
                    Grid.SetColumn(newLabel, columnCount);
                    Grid_Table.Children.Add(newLabel);
                    newLabel.MouseDoubleClick += Event_Label_DoubleClick;
                }
            };

            Button_SubColumn.Click += (s, e) => {
                int columnCount = Grid_Table.ColumnDefinitions.Count;
                if (columnCount > 1) {
                    // 创建一个列表存储待删除的控件
                    List<UIElement> toRemove = new List<UIElement>();

                    // 遍历控件，找出在最后一列的控件
                    foreach (UIElement child in Grid_Table.Children) {
                        if (Grid.GetColumn(child) == columnCount - 1) {
                            toRemove.Add(child);  // 将控件加入待删除列表
                        }
                    }

                    // 删除所有待删除的控件
                    foreach (UIElement element in toRemove) {
                        Grid_Table.Children.Remove(element);
                    }

                    // 移除最后一列
                    Grid_Table.ColumnDefinitions.RemoveAt(columnCount - 1);
                }
            };
        }

        public List<string> AllowTaskPluginLink() {
            List<string> datas = new List<string>();
            TaskOperation_Table taskOperation_Table = new TaskOperation_Table();
            datas.Add(taskOperation_Table.OperationUniqueName(LangaugeType.kChinese));
            datas.Add(taskOperation_Table.OperationUniqueName(LangaugeType.kEnglish));
            return datas;
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            Label_Title.Content = json["Label_Title"].ToString();
            int rowCount = ((int)json["Grid_Table_RowCount"]);
            int columnCount = ((int)json["Grid_Table_ColumnCount"]);

            for (int i = 0; i < rowCount - 1; i++) {
                Grid_Table.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(3, GridUnitType.Auto) });
            }
            for (int i = 0; i < columnCount - 1; i++) {
                Grid_Table.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
            }
            
            for (int i = 0; i < rowCount; i++){
                for (int j = 0; j < columnCount; j++) {
                    if (i == 0 && j == 0) {
                        Label_First.Content = json["Child_" + i + "_" + j].ToString();
                        continue;
                    }

                    string labelText = json["Child_" + i + "_" + j].ToString();
                    Label newLabel = new Label() { Content = labelText };
                    newLabel.MouseDoubleClick += Event_Label_DoubleClick;
                    Grid.SetRow(newLabel, i);
                    Grid.SetColumn(newLabel, j);
                    Grid_Table.Children.Add(newLabel);
                }
            }
        }

        public void SetViewEditStatus(bool status) {
            _viewEditStatus = status;
            if (status) {
                Grid_Button.Visibility = Visibility.Visible;
            }
            else {
                Grid_Button.Visibility = Visibility.Collapsed;
            }
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["Label_Title"] = Label_Title.Content.ToString();
            json["Grid_Table_RowCount"] = Grid_Table.RowDefinitions.Count;
            json["Grid_Table_ColumnCount"] = Grid_Table.ColumnDefinitions.Count;
            foreach (UIElement element in Grid_Table.Children) {
                Label label = element as Label;
                if (label == null) { 
                    continue;
                }

                // 获取当前元素所在的行和列
                int childRow = Grid.GetRow(element);
                int childColumn = Grid.GetColumn(element);
                json["Child_" + childRow + "_" + childColumn] = label.Content.ToString();
            }

            return json.ToString();
        }

        private void Event_Label_DoubleClick(object sender, MouseButtonEventArgs e) {
            Label newLabel = sender as Label;
            if (_viewEditStatus == false || newLabel == null) {
                return;
            }

            InputDialog inputDialog = new InputDialog() { Label = "修改文本", DefaultText = newLabel.Content.ToString() };
            inputDialog.ShowDialog();
            string userInput = inputDialog.InputText;
            if (userInput == null) {
                return;
            }
            if (userInput == newLabel.Content.ToString()) {
                return;
            }
            newLabel.Content = userInput;
        }

        public void SetCellContent(int row, int column, string text) {
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                foreach (UIElement element in Grid_Table.Children) {
                    int childRow = Grid.GetRow(element);
                    int childColumn = Grid.GetColumn(element);
                    if (childRow != row || childColumn != column) {
                        continue;
                    }

                    Label label = element as Label;
                    if (label == null) {
                        continue;
                    }

                    label.Content = text;
                }
            }));
        }
    }
}
