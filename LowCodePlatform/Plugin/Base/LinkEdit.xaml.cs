using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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

namespace LowCodePlatform.Plugin.Base
{
    public enum LinkEditColumnLayout
    { 
        /// <summary>
        /// 默认布局
        /// </summary>
        kDefault = 0,   
        /// <summary>
        /// label:textbox=1:1
        /// </summary>
        kLabel1TextBox1 = 1,
        /// <summary>
        /// label:textbox=1:2
        /// </summary>
        kLabel1TextBox2 = 2,
        /// <summary>
        /// label:textbox=1:3
        /// </summary>
        kLabel1TextBox3 = 3,
        kDefaultNoLink = 4,
        kLabel1TextBox1NoLink = 5,
        kLabel1TextBox2NoLink = 6,
        kLabel1TextBox3NoLink = 7,
    }

    /// <summary>
    /// LinkEdit.xaml 的交互逻辑
    /// </summary>
    public partial class LinkEdit : UserControl
    {
        private LinkEditColumnLayout _columnLayout = LinkEditColumnLayout.kDefault;
        /// <summary>
        /// linkedit布局
        /// </summary>
        public LinkEditColumnLayout ColumnLayout { 
            get { 
                return _columnLayout;
            }
            set {
                Button_Link.Visibility = Visibility.Visible;
                Button_Clear.Visibility = Visibility.Visible;
                Grid_Total.ColumnDefinitions.Clear();
                switch (value) {
                    case LinkEditColumnLayout.kDefault:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kDefault;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox1:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox1;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox2:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox2;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox3:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(3, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox3;
                        break;
                    case LinkEditColumnLayout.kDefaultNoLink:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kDefault;
                        Button_Link.Visibility = Visibility.Collapsed;
                        Button_Clear.Visibility = Visibility.Collapsed;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox1NoLink:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox1;
                        Button_Link.Visibility = Visibility.Collapsed;
                        Button_Clear.Visibility = Visibility.Collapsed;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox2NoLink:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(2, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox2;
                        Button_Link.Visibility = Visibility.Collapsed;
                        Button_Clear.Visibility = Visibility.Collapsed;
                        break;
                    case LinkEditColumnLayout.kLabel1TextBox3NoLink:
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid_Total.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                        Grid.SetColumn(Grid_Label, 0);
                        Grid.SetColumn(Grid_Link, 1);
                        _columnLayout = LinkEditColumnLayout.kLabel1TextBox1;
                        Button_Link.Visibility = Visibility.Collapsed;
                        Button_Clear.Visibility = Visibility.Collapsed;
                        break;
                    default:
                        break;
                }
            } 
        }

        /// <summary>
        /// link标签的文本
        /// </summary>
        public string LinkLabelText {
            get { 
                return Label_Link.Content.ToString();
            }
            set {
                Label_Link.Content = value;
            }
        }

        /// <summary>
        /// link内容的文本
        /// </summary>
        public string LinkContentText{
            get {
                return TextBox_Link.Text;
            }
            set {
                TextBox_Link.Text = value;
            }
        }

        /// <summary>
        /// link内容的使能
        /// </summary>
        public bool LinkContentEnable{
            get {
                return TextBox_Link.IsEnabled;
            }
            set {
                TextBox_Link.IsEnabled = value;
            }
        }

        /// <summary>
        /// 是否是绑定状态
        /// </summary>
        public bool IsBind {
            get {
                return !TextBox_Link.IsEnabled;
            }
        }

        /// <summary>
        /// 参数
        /// </summary>
        public object UserParam{
            get {
                if (_linkContentType == LinkDataType.kInt && !IsBind) {
                    int.TryParse(LinkContentText, out int num);
                    return num;
                }
                else if (_linkContentType == LinkDataType.kFloat && !IsBind) {
                    float.TryParse(LinkContentText, out float num);
                    return num;
                }
                else if (_linkContentType == LinkDataType.kDouble && !IsBind) {
                    double.TryParse(LinkContentText, out double num);
                    return num;
                }
                else if (_linkContentType == LinkDataType.kMat && !IsBind) {
                    return new Mat();
                }
                else if (_linkContentType == LinkDataType.kRegion && !IsBind) {
                    List<Mat> mats = new List<Mat>();
                    return mats.ToArray();
                }
                else if (_linkContentType == LinkDataType.kListDouble && !IsBind) {
                    string input = LinkContentText;
                    input = input.Replace("【", "[").Replace("】", "]");
                    input = input.Trim('[', ']');
                    input = input.Replace('，', ',');
                    return input.Split(',')
                                .Select(x => Convert.ToDouble(x.Trim()))
                                .ToList();
                }
                else {

                }
                return LinkContentText;
            }
        }

        private LinkDataType _linkContentType = LinkDataType.kNone;
        /// <summary>
        /// link内容的类型
        /// </summary>
        public LinkDataType LinkContentType{
            get {
                return _linkContentType;
            }
            set {
                _linkContentType = value;
                BindingOperations.ClearAllBindings(TextBox_Link);//清除所有绑定事件
                InputMethod.SetIsInputMethodEnabled(TextBox_Link, true);//启用输入法
                if (_linkContentType == LinkDataType.kInt || _linkContentType == LinkDataType.kFloat || _linkContentType == LinkDataType.kDouble) {
                    TextBox_Link.PreviewTextInput += Event_LinkEditDouble_PreviewTextInput;
                    TextBox_Link.KeyDown += Event_LinkEditDouble_KeyDown;
                    InputMethod.SetIsInputMethodEnabled(TextBox_Link, false);
                }
            }
        }

        private LinkClick _linkClick = null;

        /// <summary>
        /// 文本改变触发
        /// </summary>
        public TextChangedEventHandler LinkContentTextChanged = null;

        /// <summary>
        /// 双击标签事件，界面插件用于改变标签
        /// </summary>
        public MouseButtonEventHandler LabelMouseDoubleClick = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LinkEdit() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {

        }


        private void InitEvent() {
            Button_Link.Click += (s, e) => {
                string str = _linkClick?.Invoke(LinkContentType);
                if (LinkContentType == LinkDataType.kNone || LinkContentType == LinkDataType.kObject || str == string.Empty || str == null) { 
                    return ;
                }
                LinkContentEnable = false;
                LinkContentText = str;//数据最后改
            };
            Button_Clear.Click += (s, e) => {
                LinkContentEnable = true;
                LinkContentText = string.Empty;
            };
            TextBox_Link.TextChanged += Event_TextBox_TextChanged;
            Label_Link.MouseDoubleClick += Event_Label_MouseDoubleClick;
        }


        private void Event_LinkEditDouble_PreviewTextInput(object sender, TextCompositionEventArgs e) {
            // 定义一个正则表达式，匹配整数或浮点数，支持负号
            string pattern = @"^[-]?\d*(\.{0,1}\d{0,1})?$";  // 可选负号，整数或浮动最多一个小数位

            // 检查输入的字符是否符合规则
            if (!Regex.IsMatch(e.Text, pattern)) {
                e.Handled = true;  // 如果不匹配，则忽略此输入
            }
        }

        private void Event_LinkEditDouble_KeyDown(object sender, KeyEventArgs e) {
            // 允许的键：退格键、方向键、删除键、数字键、负号和小数点
            if (!(e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Left || e.Key == Key.Right ||
                (e.Key >= Key.D0 && e.Key <= Key.D9) ||
                (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) ||
                e.Key == Key.OemMinus || e.Key == Key.OemPeriod)) {
                e.Handled = true;  // 如果按下不允许的键，忽略它
            }
        }

        public void SetLinkClickCallback(LinkClick linkClickCallback) {
            if (linkClickCallback == null) { 
                return;
            }
            _linkClick = linkClickCallback;
        }

        public void JsonToView(string str) {
            if (str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            LinkContentEnable = ((bool)json["LinkContentEnable"]);
            LinkContentText = json["LinkContentText"].ToString();
        }

        public string ViewToJson() { 
            JObject json = new JObject();
            json["LinkContentEnable"] = LinkContentEnable;
            json["LinkContentText"] = LinkContentText;
            return json.ToString();
        }

        public void ResetView() {
            LinkContentEnable = true;
            LinkContentText = string.Empty;
        }

        private void Event_TextBox_TextChanged(object sender, TextChangedEventArgs e) {
            LinkContentTextChanged?.Invoke(sender, e);
        }

        private void Event_Label_MouseDoubleClick(object sender, MouseButtonEventArgs e) {
            LabelMouseDoubleClick?.Invoke(sender, e);
        }
    }
}
