using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_Arithmetic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_Ping
{
    internal class TaskOperation_Ping : TaskOperationPluginBase
    {
        private string _ip = string.Empty;

        private string _pingType = string.Empty;

        private bool _result = false;

        private List<string> _messages = new List<string>();

        private bool _isRunning = false;

        public bool EngineIsRunning {
            get { 
                return _isRunning;
            }
            set { 
                _isRunning = value;
            } 
        }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_Ping();
        }

        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "Ping";
                case LangaugeType.kEnglish:
                    return "Ping";
                default:
                    break;
            }
            return string.Empty;
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _ip = Convert.ToString(inputParams[0].ActualParam);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _pingType = Convert.ToString(inputParams[1].ActualParam);

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            Ping pingSender = new Ping();
            _result = false;
            _messages.Clear();


            if (_pingType == "单次ping") {
                try {
                    // 发送Ping请求
                    PingReply reply = pingSender.Send(_ip);

                    // 根据Ping的结果进行处理
                    if (reply.Status == IPStatus.Success) {
                        _result = true;
                        _messages.Add("来自 " + reply.Address.ToString() + " 的回复: 字节=" + reply.Buffer.Length
                        + " 时间=" + reply.RoundtripTime + "ms TTL=" + reply.Options.Ttl);
                    }
                    else {
                        _messages.Add("Ping失败，状态: " + reply.Status);
                    }
                }
                catch (PingException e) {
                    _messages.Add("Ping请求出现异常: " + e.Message);
                }
            }
            else if (_pingType == "直到ping通") {
                while (EngineIsRunning) {
                    bool test = _isRunning;
                    try {
                        // 发送Ping请求
                        PingReply reply = pingSender.Send(_ip);

                        // 根据Ping的结果进行处理
                        if (reply.Status == IPStatus.Success) {
                            _result = true;
                            _messages.Add("来自 " + reply.Address.ToString() + " 的回复: 字节=" + reply.Buffer.Length
                            + " 时间=" + reply.RoundtripTime + "ms TTL=" + reply.Options.Ttl);
                            break;
                        }
                        else {
                            _messages.Add("Ping失败，状态: " + reply.Status);
                        }
                    }
                    catch (PingException e) {
                        _messages.Add("Ping请求出现异常: " + e.Message);
                    }
                }
            }
            else { 
                return TaskNodeStatus.kFailure;
            }

            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "Ping结果",
                ActualParam = _result,
                Description = "Ping结果"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "详细信息",
                ActualParam = _messages,
                LinkVisual = false,
                Description = "ping过程中返回的详细信息"
            });

            return TaskNodeStatus.kSuccess;
        }
    }
}
