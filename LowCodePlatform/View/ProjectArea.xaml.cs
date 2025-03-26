using AvalonDock.Layout;
using LowCodePlatform.Engine;
using LowCodePlatform.View.Base;
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
using static System.Net.Mime.MediaTypeNames;

namespace LowCodePlatform.View
{
    /// <summary>
    /// ProjectArea.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectArea : UserControl, CommunicationUser
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        private const string _progressDefaultName = "新建流程"; 

        public ProjectArea() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            string currentDirectory = Directory.GetCurrentDirectory();
            Image_NewProcess.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\新建流程.png"));
            Image_DeleteProcess.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\删除流程.png"));
            Image_RenameProcess.Source = new BitmapImage(new Uri(currentDirectory + "\\Resource\\重命名.png"));
        }

        private void InitEvent() {
            Button_NewProcess.Click += Event_NewProcess_Click;
            Button_DeleteProcess.Click += Event_DeleteProcess_Click;
            Button_RenameProcess.Click += Event_RenameProcess_Click;
            ListBox_Project.SelectionChanged += Event_Project_SelectionChanged;
        }

        /// <summary>
        /// 新建流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_NewProcess_Click(object sender, RoutedEventArgs e) {
            Func<string, bool, Grid> func = (string name, bool check) => {
                Grid grid = new Grid();

                // 创建第一列
                ColumnDefinition col1 = new ColumnDefinition();
                ColumnDefinition col2 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(col1);
                grid.ColumnDefinitions.Add(col2);

                // 创建 CheckBox
                CheckBox checkBox = new CheckBox() { 
                    IsChecked = check
                };

                // 创建 Button
                Button button = new Button() {
                    Content = name,
                    Margin = new Thickness(5, 0, 0, 0), // 给文本一些边距
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0)
                };

                button.MouseDoubleClick += Event_RenameProcess_Click;

                // 将 CheckBox 和 TextBlock 添加到 Grid
                Grid.SetColumn(checkBox, 0); // 将 CheckBox 放在第一列
                Grid.SetColumn(button, 1); // 将 TextBlock 放在第二列

                grid.Children.Add(checkBox);
                grid.Children.Add(button);
                return grid;
            };

            string itemName = CheckUniqueItemName(_progressDefaultName);
            ListBox_Project.Items.Add(new ListBoxItem() { Content = func(itemName, true) });

            //发消息给组合区，组合区应该新增一个对应的tab页
            _sendMessage?.Invoke(new CommunicationCenterMessage("ProjectArea", "CombinationArea", "AddTab", itemName));
        }

        /// <summary>
        /// 删除流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_DeleteProcess_Click(object sender, RoutedEventArgs e) {
            if (ListBox_Project.SelectedItem == null) {
                return;
            }
            ListBoxItem currentListBoxItem = ListBox_Project.SelectedItem as ListBoxItem;
            if (currentListBoxItem == null) {
                return;
            }
            Grid currentGrid = currentListBoxItem.Content as Grid;
            if (currentGrid == null) {
                return;
            }
            string itemName = null;
            // 遍历 Grid 的 Children
            foreach (var child in currentGrid.Children) {
                if (child is Button b) {
                    itemName = b.Content.ToString();
                    break;
                }
            }
            if (itemName == null) {
                return;
            }
            ListBox_Project.Items.Remove(ListBox_Project.SelectedItem); // 删除选中项

            //发消息给组合区，组合区应该删除一个对应的tab页
            _sendMessage?.Invoke(new CommunicationCenterMessage("ProjectArea", "CombinationArea", "RemoveTab", itemName));
        }

        /// <summary>
        /// 重命名流程
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_RenameProcess_Click(object sender, RoutedEventArgs e) {
            if (ListBox_Project.SelectedItem == null) {
                return;
            }
            ListBoxItem currentListBoxItem = ListBox_Project.SelectedItem as ListBoxItem;
            if (currentListBoxItem == null) {
                return;
            }
            Grid currentGrid = currentListBoxItem.Content as Grid;
            if (currentGrid == null) {
                return;
            }
            CheckBox checkBox = null;
            Button button = null;
            // 遍历 Grid 的 Children
            foreach (var child in currentGrid.Children) {
                if (child is CheckBox cb) {
                    checkBox = cb;
                }
                else if (child is Button b) {
                    button = b;
                }
                else { 
                    continue;
                }
            }
            if (checkBox == null || button == null) { 
                return;
            }

            //先前的名字
            string previousName = button.Content.ToString();

            InputDialog inputDialog = new InputDialog() {Label = "重命名流程名", DefaultText = button.Content.ToString()};
            inputDialog.ShowDialog();
            string userInput = inputDialog.InputText;
            if (userInput == null) { 
                return ;
            }
            //没改名字就别整了，改了名字再整，没改名字也要check名字独特性就会拿到新的名字
            if (userInput == previousName) {
               return ;
            }
            button.Content = CheckUniqueItemName(userInput);

            //发消息给组合区，组合区应该重命名一个对应的tab页
            JObject json = new JObject();
            json["previousHeaderName"] = previousName;
            json["futureHeaderName"] = button.Content.ToString();
            _sendMessage?.Invoke(new CommunicationCenterMessage("ProjectArea", "CombinationArea", "RenameTab", json.ToString()));
        }

        /// <summary>
        /// 切换当前选中项
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Project_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (ListBox_Project.SelectedItem == null) {
                return;
            }
            ListBoxItem currentListBoxItem = ListBox_Project.SelectedItem as ListBoxItem;
            if (currentListBoxItem == null) {
                return;
            }
            Grid currentGrid = currentListBoxItem.Content as Grid;
            if (currentGrid == null) {
                return;
            }
            string itemName = null;
            // 遍历 Grid 的 Children
            foreach (var child in currentGrid.Children) {
                if (child is Button b) {
                    itemName = b.Content.ToString();
                    break;
                }
            }
            if (itemName == null) {
                return;
            }

            //发消息给组合区，组合区应该切换到一个对应的tab页
            _sendMessage?.Invoke(new CommunicationCenterMessage("ProjectArea", "CombinationArea", "SwitchToTab", itemName));
        }

        /// <summary>
        /// 检查名字是否独特，独特就返回原名，重复就返回默认处理好的名字
        /// </summary>
        /// <param name="checkName"></param>
        /// <returns></returns>
        private string CheckUniqueItemName(string checkName) {
            string preName = $"{_progressDefaultName} (0)";//预先给留的不重复名
            string resultName = checkName;//本身名字不重复，那最终就还是返回输入的名字
            int maxCount = 0;
            //获取预先留的不重复名
            foreach (ListBoxItem currentItem in ListBox_Project.Items) {
                Grid currentGrid = currentItem.Content as Grid;
                if (currentGrid == null) {
                    continue;
                }
                CheckBox checkBox = null;
                Button button = null;
                // 遍历 Grid 的 Children
                foreach (var child in currentGrid.Children) {
                    if (child is CheckBox cb) {
                        checkBox = cb;
                    }
                    else if (child is Button b) {
                        button = b;
                    }
                    else {
                        continue;
                    }
                }
                if (checkBox == null || button == null) {
                    continue;
                }

                string currentStr = button.Content.ToString();
                if (currentStr.StartsWith(_progressDefaultName)) {
                    int start = currentStr.IndexOf('(') + 1; // 找到 '(' 的位置
                    int end = currentStr.IndexOf(')'); // 找到 ')' 的位置

                    if (start <= 0 || end <= start) {
                        continue;
                    }

                    string numberStr = currentStr.Substring(start, end - start); // 提取数字字符串
                    if (!int.TryParse(numberStr, out int num)) {
                        continue; // 失败
                    }

                    maxCount = (num + 1 > maxCount) ? num + 1 : maxCount;
                    preName = $"{_progressDefaultName} ({maxCount})";
                }
            }

            //判断是否重名，重名则替换为预先留的名字
            foreach (ListBoxItem currentItem in ListBox_Project.Items) {
                Grid currentGrid = currentItem.Content as Grid;
                if (currentGrid == null) {
                    continue;
                }
                CheckBox checkBox = null;
                Button button = null;
                // 遍历 Grid 的 Children
                foreach (var child in currentGrid.Children) {
                    if (child is CheckBox cb) {
                        checkBox = cb;
                    }
                    else if (child is Button b) {
                        button = b;
                    }
                    else {
                        continue;
                    }
                }
                if (checkBox == null || button == null) {
                    continue;
                }

                if (button.Content.ToString() == checkName) {
                    resultName = preName;
                    break;
                }
            }

            return resultName;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else if (message.Function == "DataToJson") {
                return DataToJson();
            }
            else if (message.Function == "JsonToData" && message.Content is string params_JsonToData) {
                JsonToData(params_JsonToData);
            }
            else { 
            
            }
            return string.Empty;
        }

        public void SetSendMessageCallback(SendMessage cb) {
            _sendMessage = cb;
        }

        /// <summary>
        /// 重置情况当前所有数据
        /// </summary>
        public void ResetData() {
            ListBox_Project.Items.Clear();
        }

        public string DataToJson() {
            JObject json = new JObject();
            for (int i = 0; i < ListBox_Project.Items.Count; i = i + 1) {
                ListBoxItem item = ListBox_Project.Items[i] as ListBoxItem;
                if (item == null) { 
                    continue;
                }
                Grid currentGrid = item.Content as Grid;
                if (currentGrid == null) {
                    continue;
                }
                CheckBox checkBox = null;
                Button button = null;
                // 遍历 Grid 的 Children
                foreach (var child in currentGrid.Children) {
                    if (child is CheckBox cb) {
                        checkBox = cb;
                    }
                    else if (child is Button b) {
                        button = b;
                    }
                    else {
                        continue;
                    }
                }
                if (checkBox == null || button == null) {
                    continue;
                }

                //先前的名字
                bool? flowCheck = checkBox.IsChecked;
                string flowName = button.Content.ToString();

                json[i.ToString() + "_flowCheck"] = flowCheck;
                json[i.ToString() + "_flowName"] = flowName;
            }
            return json.ToString();
        }

        public void JsonToData(string str) {
            Func<string, bool?, Grid> func = (string name, bool? check) => {
                Grid grid = new Grid();

                // 创建第一列
                ColumnDefinition col1 = new ColumnDefinition();
                ColumnDefinition col2 = new ColumnDefinition();
                grid.ColumnDefinitions.Add(col1);
                grid.ColumnDefinitions.Add(col2);

                // 创建 CheckBox
                CheckBox checkBox = new CheckBox() {
                    IsChecked = check
                };

                // 创建 Button
                Button button = new Button() {
                    Content = name,
                    Margin = new Thickness(5, 0, 0, 0), // 给文本一些边距
                    Background = Brushes.Transparent,
                    BorderBrush = Brushes.Transparent,
                    BorderThickness = new Thickness(0)
                };

                button.MouseDoubleClick += Event_RenameProcess_Click;

                // 将 CheckBox 和 TextBlock 添加到 Grid
                Grid.SetColumn(checkBox, 0); // 将 CheckBox 放在第一列
                Grid.SetColumn(button, 1); // 将 TextBlock 放在第二列

                grid.Children.Add(checkBox);
                grid.Children.Add(button);
                return grid;
            };

            if (str == null || str == "") {
                return;
            }
            ResetData();

            JObject json = JObject.Parse(str);
            int i = 0;
            while (json != null) {
                if (!json.ContainsKey(i.ToString() + "_flowCheck")) {
                    break;
                }
                if (!json.ContainsKey(i.ToString() + "_flowName")) {
                    break;
                }

                bool? flowCheck = ((bool)json[i.ToString() + "_flowCheck"]);
                string flowName = json[i.ToString() + "_flowName"].ToString();

                string itemName = CheckUniqueItemName(_progressDefaultName);
                ListBoxItem listBoxItem = new ListBoxItem() { Content = func(flowName, flowCheck) };
                ListBox_Project.Items.Add(listBoxItem);

                i++;
            }
        }
    }
}
