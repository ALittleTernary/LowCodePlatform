using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LowCodePlatform.Engine.CommunicationCenter;

namespace LowCodePlatform.Engine
{
    /// <summary>
    /// 回调函数由消息中心设置
    /// </summary>
    /// <param name="message"></param>
    public delegate object SendMessage(CommunicationCenterMessage message);
    /// <summary>
    /// 单个通讯用户
    /// </summary>
    public interface CommunicationUser
    {
        /// <summary>
        /// 设置发送回调函数
        /// </summary>
        /// <param name="cb"></param>
        void SetSendMessageCallback(SendMessage cb);


        /// <summary>
        /// 接收数据，数据处理完可能需要返回一些值
        /// </summary>
        /// <param name="message"></param>
        object AcceptMessage(CommunicationCenterMessage message);

        /// <summary>
        /// 数据保存为json，根据需要实现
        /// </summary>
        /// <returns></returns>
        string DataToJson();

        /// <summary>
        /// json还原为数据，根据需要实现
        /// </summary>
        /// <param name="str"></param>
        void JsonToData(string str);
    }
}
