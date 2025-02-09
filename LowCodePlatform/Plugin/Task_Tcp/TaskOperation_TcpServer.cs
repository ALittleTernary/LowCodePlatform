using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Res_Tcp;
using OpenCvSharp;
using Serilog.Context;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Tcp
{
    internal class TaskOperation_TcpServer : TaskOperationPluginBase
    {
        private ResOperation_TcpSever _tcpServer = null;
        private string _operation = string.Empty;
        private string _sendMessage = string.Empty;
        private double _receiveOvertime = 0;
        private string _receiveMessage = string.Empty;

        public bool EngineIsRunning { get; set; }
        public TaskOperationPluginBase Clone() {
            return new TaskOperation_TcpServer();
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

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(ResOperation_TcpSever)) {
                return TaskNodeStatus.kFailure;
            }
            _tcpServer = inputParams[0].ActualParam as ResOperation_TcpSever;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _operation = inputParams[1].ActualParam as string;
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _sendMessage = inputParams[2].ActualParam as string;
            if (inputParams.Count < 4 || inputParams[3].ActualParam.GetType() != typeof(double)) {
                return TaskNodeStatus.kFailure;
            }
            _receiveOvertime = Convert.ToDouble(inputParams[3].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            _receiveMessage = string.Empty;

            if (_operation == "发送数据") {
                _tcpServer.SendMessage(_sendMessage);
            }
            else if (_operation == "接收数据") {
                _receiveMessage = _tcpServer.ReceiveMessage();
            }
            else { 
                return TaskNodeStatus.kFailure;
            }
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "tcp服务端接收来自tcp客户端的数据",
                ActualParam = _receiveMessage,
                Description = "上一条数据，并不代表是最新的数据",
            });
            return TaskNodeStatus.kSuccess;
        }
    }
}
