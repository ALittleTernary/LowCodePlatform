using LowCodePlatform.Plugin.Base;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Serilog.Core;
using Serilog;
using Newtonsoft.Json.Linq;
using System.Windows.Documents;
using System.Windows;

namespace LowCodePlatform.Plugin.Res_Tcp
{
    public delegate void TcpServerCoreNotify(TcpOptionData data);

    /// <summary>
    /// tcp服务端核心
    /// </summary>
    public class TcpServerCore {
        /// <summary>
        /// 系统的tcp服务端
        /// </summary>
        private TcpListener _tcpListener = null;
        /// <summary>
        /// 构造函数决定的tcp服务端地址
        /// </summary>
        private string _tcpServerAddress = string.Empty;
        /// <summary>
        /// 构造函数决定的tcp服务端一次性监听客户端发送多少字节的数据，超出了就下次读取才能接收了
        /// </summary>
        private int _bufferLength = -1;
        /// <summary>
        /// tcp服务端的状态，开始还是关闭
        /// </summary>
        private bool _resStatus = false;
        /// <summary>
        /// 线程安全的客户端集合
        /// </summary>
        private ConcurrentDictionary<string, TcpClient> _clientsDic = new ConcurrentDictionary<string, TcpClient>();
        /// <summary>
        /// 对外通知的回调函数
        /// </summary>
        private TcpServerCoreNotify _notify = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="bufferLength"></param>
        public TcpServerCore(string ipAddress, int port, int bufferLength) {
            _tcpListener = new TcpListener(IPAddress.Parse(ipAddress), port);
            _bufferLength = bufferLength;
            _tcpServerAddress = ((IPEndPoint)_tcpListener.LocalEndpoint).ToString();
        }

        /// <summary>
        /// 资源核心类析构时需要释放资源，别占着就傻逼了
        /// </summary>
        ~TcpServerCore() {
            Stop();
        }

        /// <summary>
        /// tcp服务端核心状态，也就是tcp服务端状态
        /// </summary>
        /// <returns></returns>
        public bool ResStatus() {
            return _resStatus;
        }

        public void Start() {
            // 启动监听
            try {
                _tcpListener.Start();
                // 启动一个线程来接收客户端连接
                Thread listenerThread = new Thread(AcceptClients);
                listenerThread.Start();
                _resStatus = true;
                Log.Verbose("tcp服务端" + _tcpServerAddress + "开启服务");
            }
            catch (Exception ex) {
                _resStatus = false;
                _notify?.Invoke(new TcpOptionData() {
                    OptionAction = TcpAction.kServerOpenFail,
                });
                Log.Error("tcp服务端" + _tcpServerAddress + "开启服务失败" + ex.Message);
            }
        }

        // 接受客户端连接并为每个客户端创建处理线程
        private void AcceptClients() {
            Log.Verbose("tcp服务端" + _tcpServerAddress + "开启“监听客户端连接请求”线程");
            while (true) {
                try {
                    // 接受客户端连接，没有新的连接就会一直阻塞
                    TcpClient tcpClient = _tcpListener.AcceptTcpClient();
                    string tcpClientAddress = (tcpClient.Client.RemoteEndPoint as IPEndPoint).ToString();
                    _clientsDic.TryAdd(tcpClientAddress, tcpClient);

                    // 启动新线程来处理客户端通信
                    Thread clientThread = new Thread(HandleClient);
                    clientThread.Start(tcpClient);
                }
                catch (Exception ex) {
                    Log.Verbose("tcp服务端" + _tcpServerAddress + "关闭服务" + ex);
                    break;
                }
            }
            Log.Verbose("tcp服务端" + _tcpServerAddress + "关闭“监听客户端连接请求”线程");
        }

        // 处理客户端请求和广播消息
        private void HandleClient(object obj) {
            TcpClient tcpClient = obj as TcpClient;
            if (tcpClient == null) {
                return;
            }
            NetworkStream stream = tcpClient.GetStream();
            byte[] buffer = new byte[_bufferLength];

            string tcpClientAddress = (tcpClient.Client.RemoteEndPoint as IPEndPoint).ToString();

            //通知界面
            _notify?.Invoke(new TcpOptionData() {
                OptionAction = TcpAction.kServerAddClientLink,
                ActionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ClientAddress = tcpClientAddress,
            });
            Log.Verbose("tcp服务端" + _tcpServerAddress + "与tcp客户端" + tcpClientAddress + "建立连接");

            try {
                int bytesRead;
                while (true) {
                    bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) {
                        Log.Verbose("tcp服务端" + _tcpServerAddress + "与tcp客户端" + tcpClientAddress + "断开连接");
                        break;
                    }
                    // 将接收到的数据转为字符串
                    string request = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    _notify?.Invoke(new TcpOptionData() {
                        OptionAction = TcpAction.kServerReceiveClientMsg,
                        ActionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                        ClientAddress = tcpClientAddress,
                        ClientToServerMsg = request,
                    });
                }
            }
            catch (Exception ex) {
                Log.Verbose("tcp服务端" + _tcpServerAddress + "与tcp客户端" + tcpClientAddress + "断开连接" + ex.Message);
            }
            finally {
                // 客户端断开连接，移除客户端
                _clientsDic.TryRemove(tcpClientAddress, out _);
                stream.Close();
                tcpClient.Close();
                _notify?.Invoke(new TcpOptionData() {
                    OptionAction = TcpAction.kServerRemoveClientLink,
                    ClientToServerMsg = tcpClientAddress,
                });
            }
        }

        /// <summary>
        /// 广播消息给所有连接的客户端
        /// 可外部调用，需要加锁
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(string message) {
            byte[] messageBytes = Encoding.UTF8.GetBytes(message);

            foreach (var keyValue in _clientsDic) {
                TcpClient client = null;
                _clientsDic.TryGetValue(keyValue.Key, out client);
                string tcpClientAddress = (client.Client.RemoteEndPoint as IPEndPoint)?.ToString();
                try {
                    NetworkStream stream = client.GetStream();
                    stream.Write(messageBytes, 0, messageBytes.Length);
                }
                catch (Exception ex) {
                    Log.Warning("tcp服务端" + _tcpServerAddress + "发送消息给客户端" + tcpClientAddress + "出错" + ex.Message);
                }
            }
            _notify?.Invoke(new TcpOptionData() {
                OptionAction = TcpAction.kServerBroadClientMsg,
                ActionTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                ServerToClientMsg = message,
            });
        }

        public void SetNotifyCallback(TcpServerCoreNotify cb) {
            if (cb == null) {
                return;
            }
            _notify = cb;
        }

        public void Stop() {
            // 先停止监听新的客户端连接,会导致AcceptClients跳出循环停止监听
            _tcpListener.Stop();
            // 关闭所有已连接的客户端
            foreach (var keyValue in _clientsDic) {
                try {
                    TcpClient client = null;
                    _clientsDic.TryGetValue(keyValue.Key, out client);
                    NetworkStream stream = client.GetStream();
                    stream.Close();
                    client.Close();
                }
                catch (SocketException ex) {
                    Log.Warning("tcp服务端" + _tcpServerAddress + "关闭tcp客户端时出错" + ex.Message);
                }
            }
            _resStatus = false;
        }
    }

    /// <summary>
    /// 整一个tcp服务端会被多线程操控，每一个对外开放的接口均需加锁
    /// </summary>
    internal class ResOperation_TcpSever : ResOperationPluginBase
    {
        /// <summary>
        /// tcp服务端核心类
        /// </summary>
        private TcpServerCore _tcpServer = null;

        /// <summary>
        /// 对外发布消息的回调函数，用于与资源界面进行联动
        /// </summary>
        private ResMessageLaunch _resMessageLaunch = null;

        private static readonly object _lockObject = new object();
        /// <summary>
        /// 对应界面的DataGrid_Client
        /// </summary>
        private List<TcpOptionData> _dataGridClient = new List<TcpOptionData>();
        /// <summary>
        /// 对应界面的DataGrid_Receive
        /// </summary>
        private List<TcpOptionData> _dataGridReceive = new List<TcpOptionData>();
        /// <summary>
        /// 对应界面的DataGrid_Send
        /// </summary>
        private List<TcpOptionData> _dataGridSend = new List<TcpOptionData>();

        public void Dispose() {
            _tcpServer?.Stop();
            ResStatus = false;
            _resMessageLaunch = null;
        }

        public ResOperationPluginBase Clone() {
            return new ResOperation_TcpSever();
        }

        public string OperationUniqueName(LangaugeType type) {
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

        /// <summary>
        /// 打开需要内部加锁，避免多线程处理顺序出现问题
        /// </summary>
        /// <param name="inputParams"></param>
        public void TurnOffRes(List<ResViewInputParams> inputParams) {
            lock (_lockObject) {
                //无论怎么样都调用这个函数，使得tcp停止
                _tcpServer?.Stop();
                _tcpServer = null;
                //更新tcp服务端的状态，必须关闭成功
                ResStatus = false;
            }
        }

        /// <summary>
        /// 关闭需要内部加锁，避免多线程处理顺序出现问题
        /// </summary>
        /// <param name="inputParams"></param>
        public void TurnOnRes(List<ResViewInputParams> inputParams) {
            lock (_lockObject) {
                //设备已经打开了的话，就别瞎搞了
                if (inputParams == null || _tcpServer != null) {
                    return;
                }
                if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(string)) {
                    return;
                }
                string ip = Convert.ToString(inputParams[0].ActualParam);
                if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(int)) {
                    return;
                }
                int port = Convert.ToInt32(inputParams[1].ActualParam);

                _tcpServer = new TcpServerCore(ip, port, 1024);
                _tcpServer.SetNotifyCallback(TcpServerCoreNotify);
                _tcpServer.Start();
                //更新tcp的状态，不一定开启成功
                ResStatus = _tcpServer.ResStatus();
                //开启失败就清空
                if (!ResStatus) {
                    _tcpServer.Stop();
                    _tcpServer = null;
                }
            }
        }

        private void TcpServerCoreNotify(TcpOptionData data) {
            Action action_kServerOpenFail = () => {
                _dataGridClient.Clear();
                _dataGridReceive.Clear();
                _dataGridSend.Clear();
            };
            Action action_kServerAddClientLink = () => {
                int num = _dataGridClient.Count;
                _dataGridClient.Add(new TcpOptionData() {
                    SerialNum = num,
                    ActionTime = data.ActionTime,
                    ClientAddress = data.ClientAddress,
                });
            };
            Action action_kServerRemoveClientLink = () => {
                for (int i = 0; i < _dataGridClient.Count; i++) {
                    TcpOptionData tcpOptionData = _dataGridClient[i];
                    if (tcpOptionData.ClientAddress == data.ClientAddress) {
                        continue;
                    }
                    _dataGridClient.Remove(tcpOptionData);
                    break;
                }
            };
            Action action_kServerReceiveClientMsg = () => {
                //超出100则剔除队首
                if (_dataGridReceive.Count >= 100) {
                    _dataGridReceive.RemoveAt(0);
                }
                _dataGridReceive.Add(new TcpOptionData() {
                    ActionTime = data.ActionTime,
                    ClientAddress = data.ClientAddress,
                    ClientToServerMsg = data.ClientToServerMsg,
                });
            };
            Action action_kServerBroadClientMsg = () => {
                //超出100则剔除队首
                if (_dataGridSend.Count >= 100) {
                    _dataGridSend.RemoveAt(0);
                }
                _dataGridSend.Add(new TcpOptionData() {
                    ActionTime = data.ActionTime,
                    ServerToClientMsg = data.ServerToClientMsg,
                });
            };
            lock (_lockObject) {
                switch (data.OptionAction) {
                    case TcpAction.kNone:
                        break;
                    case TcpAction.kServerOpenFail:
                        action_kServerOpenFail();
                        break;
                    case TcpAction.kServerAddClientLink:
                        action_kServerAddClientLink();
                        break;
                    case TcpAction.kServerRemoveClientLink:
                        action_kServerRemoveClientLink();
                        break;
                    case TcpAction.kServerReceiveClientMsg:
                        action_kServerReceiveClientMsg();
                        break;
                    case TcpAction.kServerBroadClientMsg:
                        action_kServerBroadClientMsg();
                        break;
                    default:
                        break;
                }
                List<ResOperationOutputParams> outputParams = new List<ResOperationOutputParams>();
                outputParams.Add(new ResOperationOutputParams() {
                    ParamName = "tcp服务端状态",
                    ActualParam = ResStatus,
                });
                outputParams.Add(new ResOperationOutputParams() {
                    ParamName = "tcp服务端连接tcp客户端列表",
                    ActualParam = _dataGridClient,
                });
                outputParams.Add(new ResOperationOutputParams() {
                    ParamName = "tcp服务端接收tcp客户端信息列表",
                    ActualParam = _dataGridReceive,
                });
                outputParams.Add(new ResOperationOutputParams() {
                    ParamName = "tcp服务端广播tcp客户端信息列表",
                    ActualParam = _dataGridSend,
                });
                _resMessageLaunch?.Invoke(outputParams);
            }
        }

        public void SetResMessageLaunchCallback(ResMessageLaunch cb) {
            if (cb == null) { 
                return;
            }
            _resMessageLaunch = cb;
        }

        public bool ResStatus{ get; set; }

        public void ResTemporaryEvent(string temporaryParams) {
            if (string.IsNullOrEmpty(temporaryParams) || _tcpServer == null) {
                return;
            }
            _tcpServer.SendMessage(temporaryParams);
        }

        /// <summary>
        /// 获取在链接中的tcp客户端
        /// </summary>
        public List<TcpOptionData> TcpClientsData() {
            return _dataGridClient;
        }

        /// <summary>
        /// tcp服务端接收所有连接的客户端发送信息
        /// </summary>
        /// <param name="ouputParams"></param>
        public string ReceiveMessage() {
            lock (_lockObject) {
                TcpOptionData data = _dataGridReceive.LastOrDefault();
                if (data == null) {
                    return string.Empty;
                }
                return data.ClientToServerMsg;
            }
        }

        /// <summary>
        /// 广播信息给所有连接tcp服务端的tcp客户端
        /// </summary>
        public void SendMessage(string message) {
            lock (_lockObject) {
                _tcpServer?.SendMessage(message);
            }
        }
    }


}
