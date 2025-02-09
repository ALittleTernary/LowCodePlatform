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

namespace LowCodePlatform.View
{
    /// <summary>
    /// InputDialog.xaml 的交互逻辑
    /// </summary>
    public partial class InputDialog : Window
    {

        public string DefaultText { set { TextBox_InputText.Text = value; } }
        public string InputText { get; private set; }

        public string Label {set { Label_InputText.Content = value; } }

        public string Tip { set { TextBox_InputText.ToolTip = value; } }

        public InputDialog() {
            InitializeComponent();
            Title = "输入对话框";
            Button_Comfirm.Click += (object sender, RoutedEventArgs e) => {
                InputText = TextBox_InputText.Text;
                DialogResult = true; // 设置为真表示用户点击了确定
                Close();
            };
            Button_Cancel.Click += (object sender, RoutedEventArgs e) => {
                DialogResult = false; // 设置为假表示用户点击了取消
                Close();
            };
            TextBox_InputText.KeyDown += (object sender, KeyEventArgs e) => {
                if (e.Key != Key.Enter) { 
                    return;
                }
                InputText = TextBox_InputText.Text;
                DialogResult = true; // 设置为真表示用户点击了确定
                Close();
            };
        }


    }
}
