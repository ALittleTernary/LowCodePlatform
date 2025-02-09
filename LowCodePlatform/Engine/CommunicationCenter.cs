using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Engine
{
    /// <summary>
    /// 进行通信的简单数据结构
    /// </summary>
    public class CommunicationCenterMessage
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">消息来源处</param>
        /// <param name="target">消息目的地</param>
        /// <param name="func">消息目的处调用的函数</param>
        /// <param name="content">消息目的处调用函数，所需要的输入数据</param>
        public CommunicationCenterMessage(string source, string target, string func, object content) {
            Source = source;
            Target = target;
            Function = func;
            Content = content;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="source">消息来源处</param>
        /// <param name="target">消息目的地</param>
        /// <param name="func">消息目的处调用的函数</param>
        public CommunicationCenterMessage(string source, string target, string func) {
            Source = source;
            Target = target;
            Function = func;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">消息目的地</param>
        /// <param name="func">消息目的处调用的函数</param>
        /// <param name="content">消息目的处调用函数，所需要的输入数据</param>
        public CommunicationCenterMessage(string target, string func, object content) {
            Target = target;
            Function = func;
            Content = content;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target">消息目的处调用的函数</param>
        /// <param name="func">消息目的处调用的函数</param>
        public CommunicationCenterMessage(string target, string func) {
            Target = target;
            Function = func;
        }


        /// <summary>
        /// 消息来源
        /// </summary>
        public string Source { get; }

        /// <summary>
        /// 发送对象
        /// </summary>
        public string Target { get; }

        /// <summary>
        /// 希望调用的函数
        /// </summary>
        public string Function { get; }

        /// <summary>
        /// 消息具体内容
        /// </summary>
        public object Content { get; }
    }

    /// <summary>
    /// 通讯中枢，所有注册的用户均能相互进行单对单的通讯
    /// 每次只能支持一对单对单的通讯，仅在单线程中使用，不支持同时多个单对单通讯
    /// 其实就是中介者模式，但是中介者模式是广播形式，而且要持有所有指针才能通讯，这里用字符串标识符代替了指针，因此子类内部就能通讯
    /// </summary>
    public class CommunicationCenter
    {
        /// <summary>
        /// 注册到通讯中枢用户/接受函数的管理集合
        /// </summary>
        private Dictionary<string, Func<CommunicationCenterMessage, object>> _users = new Dictionary<string, Func<CommunicationCenterMessage, object>>();

        public CommunicationCenter() { 
        
        }

        /// <summary>
        /// 注册用户
        /// </summary>
        /// <param name="userName">唯一用户名</param>
        /// <param name="handleFunc">接收消息函数</param>
        public void Register(string userName, CommunicationUser user) {
            if (user == null) {
                return;
            }
            _users[userName] = user.AcceptMessage;
            user.SetSendMessageCallback(PublishMessage);
        }

        /// <summary>
        /// 注销用户
        /// </summary>
        /// <param name="userName"></param>
        public void UnRegister(string userName) {
            if (!_users.ContainsKey(userName)) {
                return;
            }
            _users.Remove(userName);
        }

        /// <summary>
        /// 发布给某个对象单独的消息
        /// </summary>
        /// <param name="message"></param>
        public object PublishMessage(CommunicationCenterMessage message) {
            foreach (var targetUser in _users) {
                if (targetUser.Key == message.Target) {
                    //发送信息到接收函数中，接收函数返回信息
                    object reply = targetUser.Value(message);
                    return reply;
                }
            }
            return null;
        }

        /// <summary>
        /// 广播给所有注册在群聊里的用户消息
        /// </summary>
        /// <returns></returns>
        public List<(string, object)> BroadcastMessage(CommunicationCenterMessage message) {
            List<(string, object)> messages = new List<(string, object)>();
            foreach (var targetUser in _users) {
                //发送信息到接收函数中，接收函数返回信息
                object reply = targetUser.Value(message);
                messages.Add((targetUser.Key, reply));
            }
            return messages;
        }
    }
}
