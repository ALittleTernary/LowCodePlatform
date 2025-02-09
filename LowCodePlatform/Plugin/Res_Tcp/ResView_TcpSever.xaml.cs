using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Tcp;
using Newtonsoft.Json.Linq;
using Serilog;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
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

namespace LowCodePlatform.Plugin.Res_Tcp
{
    public enum TcpAction
    {
        /// <summary>
        /// 空置
        /// </summary>
        kNone = 0,
        /// <summary>
        /// 服务端开启失败
        /// </summary>
        kServerOpenFail = 1,
        /// <summary>
        /// 服务端新增客户端连接
        /// </summary>
        kServerAddClientLink = 2,
        /// <summary>
        /// 服务端移除客户端连接
        /// </summary>
        kServerRemoveClientLink = 3,
        /// <summary>
        /// 服务端接收客户端信息
        /// </summary>
        kServerReceiveClientMsg = 4,
        /// <summary>
        /// 服务端广播给所有客户端信息
        /// </summary>
        kServerBroadClientMsg = 5,
    };

    public class TcpOptionData : INotifyPropertyChanged
    {
        /// <summary>
        /// 行为
        /// </summary>
        public TcpAction OptionAction { get; set; } = TcpAction.kNone;

        private int _serialNum = -1;
        /// <summary>
        /// 序号
        /// </summary>
        public int SerialNum
        {
            get { return _serialNum; }
            set {
                _serialNum = value;
                OnPropertyChanged(nameof(SerialNum));
            }
        }

        private string _actionTime = string.Empty;
        /// <summary>
        /// 行为时间
        /// </summary>
        public string ActionTime
        {
            get { return _actionTime; }
            set {
                _actionTime = value;
                OnPropertyChanged(nameof(ActionTime));
            }
        }

        private string _clientAddress = string.Empty;
        /// <summary>
        /// 客户端地址
        /// </summary>
        public string ClientAddress
        {
            get { return _clientAddress; }
            set {
                _clientAddress = value;
                OnPropertyChanged(nameof(ClientAddress));
            }
        }

        private string _clientToServerMsg = string.Empty;
        /// <summary>
        /// 客户端给服务端信息
        /// </summary>
        public string ClientToServerMsg
        {
            get { return _clientToServerMsg; }
            set {
                _clientToServerMsg = value;
                OnPropertyChanged(nameof(ClientToServerMsg));
            }
        }

        private string _serverToClientMsg = string.Empty;
        /// <summary>
        /// 服务端给客户端信息
        /// </summary>
        public string ServerToClientMsg
        {
            get { return _serverToClientMsg; }
            set {
                _serverToClientMsg = value;
                OnPropertyChanged(nameof(ServerToClientMsg));
            }
        }

        public string DataToJson() {
            JObject json = new JObject();
            json["SerialNum"] = SerialNum;
            json["ActionTime"] = ActionTime;
            json["ClientAddress"] = ClientAddress;
            json["ClientToServerMsg"] = ClientToServerMsg;
            json["ServerToClientMsg"] = ServerToClientMsg;
            return json.ToString();
        }

        public void JsonToData(string str) {
            if (str == null || str == string.Empty) {
                return;
            }
            JObject json = JObject.Parse(str);
            SerialNum = ((int)json["SerialNum"]);
            ActionTime = (json["ActionTime"].ToString());
            ClientAddress = (json["ClientAddress"].ToString());
            ClientToServerMsg = (json["ClientToServerMsg"].ToString());
            ServerToClientMsg = json["ServerToClientMsg"].ToString();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    /// <summary>
    /// ResView_TcpSever.xaml 的交互逻辑
    /// </summary>
    public partial class ResView_TcpSever : UserControl, ResViewPluginBase
    {
        private TurnOnResClick _turnOnResClick = null;
        private TurnOffResClick _turnOffResClick = null;
        private ResTemporaryEvent _resTemporaryEvent = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public ResView_TcpSever() {
            InitializeComponent();
            InitEvent();
        }

        private void InitEvent() {
            Button_TurnOnRes.Click += Event_Button_TurnOnRes;
            Button_TurnOffRes.Click += Event_Button_TurnOffRes;
            Button_SendMessage.Click += Event_Button_TemporaryEvent;
            Button_ClearMessage.Click += (s, e) => {
                TextBox_SendMessage.Text = string.Empty ;
            };
            TextBox_Port.PreviewTextInput += (s, e) => {
                // 只允许数字输入
                e.Handled = !Regex.IsMatch(e.Text, @"^\d+$");
            };
        }

        public void ViewOperationDataUpdate(in List<ResViewInputParams> inputParams, in List<ResOperationOutputParams> outputParams) {
            if (outputParams == null) { 
                return;
            }
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(bool)) {
                return;
            }
            bool resStatus = Convert.ToBoolean(outputParams[0].ActualParam);
            if (resStatus) {
                TextBox_IP.IsEnabled = false;
                TextBox_Port.IsEnabled = false;
            }
            else {
                TextBox_IP.IsEnabled = true;
                TextBox_Port.IsEnabled = true;
                DataGrid_Client.ItemsSource = null;
                DataGrid_Receive.ItemsSource = null;
                DataGrid_Send.ItemsSource = null;
            }
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(List<TcpOptionData>)) {
                return;
            }
            List<TcpOptionData> clientOperationDatas = outputParams[1].ActualParam as List<TcpOptionData>;
            List<TcpOptionData> clientViewDatas = new List<TcpOptionData>();
            for (int i = 0; i < clientOperationDatas.Count; i++) {
                clientViewDatas.Add(new TcpOptionData() { 
                    SerialNum = clientOperationDatas[i].SerialNum,
                    ActionTime = clientOperationDatas[i].ActionTime,
                    ClientAddress = clientOperationDatas[i].ClientAddress,
                });
            }
            DataGrid_Client.ItemsSource = clientViewDatas;
            if (outputParams.Count < 3 || outputParams[2].ActualParam.GetType() != typeof(List<TcpOptionData>)) {
                return;
            }
            List<TcpOptionData> receiveOperationDatas = outputParams[2].ActualParam as List<TcpOptionData>;
            List<TcpOptionData> receiveViewDatas = new List<TcpOptionData>();
            for (int i = 0; i < receiveOperationDatas.Count; i++) {
                receiveViewDatas.Add(new TcpOptionData() {
                    ActionTime = receiveOperationDatas[i].ActionTime,
                    ClientAddress = receiveOperationDatas[i].ClientAddress,
                    ClientToServerMsg = receiveOperationDatas[i].ClientToServerMsg,
                });
            }
            DataGrid_Receive.ItemsSource = receiveViewDatas;
            if (outputParams.Count < 4 || outputParams[3].ActualParam.GetType() != typeof(List<TcpOptionData>)) {
                return;
            }
            List<TcpOptionData> sendOperationDatas = outputParams[3].ActualParam as List<TcpOptionData>;
            List<TcpOptionData> sendViewDatas = new List<TcpOptionData>();
            for (int i = 0; i < sendOperationDatas.Count; i++) {
                sendViewDatas.Add(new TcpOptionData() {
                    ActionTime = sendOperationDatas[i].ActionTime,
                    ServerToClientMsg = sendOperationDatas[i].ServerToClientMsg,
                });
            }
            DataGrid_Send.ItemsSource = sendViewDatas;
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) { 
                return;
            }
            JObject json = JObject.Parse(str);
            TextBox_IP.Text = json["TextBox_IP_Text"].ToString();
            TextBox_IP.IsEnabled = ((bool)json["TextBox_IP_IsEnable"]);
            TextBox_Port.Text = json["TextBox_Port"].ToString();
            TextBox_Port.IsEnabled = ((bool)json["TextBox_Port_IsEnable"]);
        }

        public void ResetView() {
            TextBox_IP.Text = "0.0.0.0";
            TextBox_Port.Text = "9000";
            TextBox_SendMessage.Text = string.Empty;
            TextBox_IP.IsEnabled = true;
            TextBox_Port.IsEnabled = true;
            DataGrid_Client.ItemsSource = null;
            DataGrid_Receive.ItemsSource = null;
            DataGrid_Send.ItemsSource = null;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_IP_Text"] = TextBox_IP.Text;
            json["TextBox_IP_IsEnable"] = TextBox_IP.IsEnabled;
            json["TextBox_Port"] = TextBox_Port.Text;
            json["TextBox_Port_IsEnable"] = TextBox_Port.IsEnabled;
            return json.ToString();
        }

        /// <summary>
        /// 开启资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_TurnOnRes(object sender, RoutedEventArgs e) {
            if (!int.TryParse(TextBox_Port.Text, out int port)) {
                return;
            }
            if (port <= 1024 || port >= 49151) { 
                return;
            }
            List<ResViewInputParams> inputParams = new List<ResViewInputParams>();
            inputParams.Add(new ResViewInputParams() { 
                ParamName = "Tcp服务端IP",
                ActualParam = TextBox_IP.Text,
            });
            inputParams.Add(new ResViewInputParams() {
                ParamName = "Tcp服务端Port",
                ActualParam = port,
            });
            _turnOnResClick?.Invoke(inputParams);
        }

        /// <summary>
        /// 关闭资源
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_TurnOffRes(object sender, RoutedEventArgs e) {
            if (!int.TryParse(TextBox_Port.Text, out int port)) {
                return;
            }
            if (port <= 1024 || port >= 49151) {
                return;
            }
            List<ResViewInputParams> inputParams = new List<ResViewInputParams>();
            inputParams.Add(new ResViewInputParams() {
                ParamName = "Tcp服务端IP",
                ActualParam = TextBox_IP.Text,
            });
            inputParams.Add(new ResViewInputParams() {
                ParamName = "Tcp服务端Port",
                ActualParam = port,
            });
            _turnOffResClick?.Invoke(inputParams);
            DataGrid_Client.ItemsSource = null;
            DataGrid_Receive.ItemsSource = null;
            DataGrid_Send.ItemsSource = null;
        }

        private void Event_Button_TemporaryEvent(object sender, RoutedEventArgs e) {
            _resTemporaryEvent?.Invoke(TextBox_SendMessage.Text);
        }

        public void SetTurnOnResClickCallback(TurnOnResClick cb) {
            if (cb == null) {
                return;
            }
            _turnOnResClick = cb;
        }

        public void SetTurnOffResClickCallback(TurnOffResClick cb) {
            if (cb == null) {
                return;
            }
            _turnOffResClick = cb;
        }

        public void SetResTemporaryEventCallback(ResTemporaryEvent cb) {
            if (cb == null) {
                return;
            }
            _resTemporaryEvent = cb;
        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "Tcp服务端";
                case LangaugeType.kEnglish:
                    return "TcpServer";
                default:
                    break;
            }
            return string.Empty;
        }

        public void SwitchLanguage(LangaugeType type) {
             
        }

        public List<string> AllowTaskPluginLink() {
            List<string> datas = new List<string>();
            TaskOperation_TcpServer tcpServer = new TaskOperation_TcpServer();
            datas.Add(tcpServer.OperationUniqueName(LangaugeType.kChinese));
            datas.Add(tcpServer.OperationUniqueName(LangaugeType.kEnglish));
            return datas;
        }
    }
}
