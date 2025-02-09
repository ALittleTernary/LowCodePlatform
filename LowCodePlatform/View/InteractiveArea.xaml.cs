using LowCodePlatform.Engine;
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
    /// InteractiveArea.xaml 的交互逻辑
    /// </summary>
    public partial class InteractiveArea : UserControl, CommunicationUser
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;
        public InteractiveArea() {
            InitializeComponent();
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
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
