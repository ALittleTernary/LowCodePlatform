using LowCodePlatform.Control;
using LowCodePlatform.Engine;
using LowCodePlatform.Plugin;
using LowCodePlatform.View;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace LowCodePlatform
{
    class Progress
    {
        /// <summary>
        /// 程序起点
        /// Main函数
        /// 必须使用[STAThread]标记方法
        /// </summary>
        [STAThread]
        static void Main() {
            Application app = new Application();

            //日志类提前启动，不需要加入群聊，日志类单例全局调用
            LogControl logControl = new LogControl();

            //消息中心，用于类之间的通讯，相当于创建了一个qq群，加入群聊能进行单对单通讯
            CommunicationCenter communicationCenter = new CommunicationCenter();

            //主界面，界面交互，用户点击运行按钮，会把数据一股脑整合一次发送给引擎，插件管理器也在界面中进行控制
            WindowBase windowBase = new WindowBase();
            communicationCenter.Register("WindowBase", windowBase);

            //流程引擎，负责把界面传递进来的信息进行运行，由于是一个节点一个节点的运行的，因此会把节点运行状态再通知给主界面
            //当前引擎需要资源发布和资源获取时，会与资源管理器交互
            AlgoEngine engine = new AlgoEngine();
            communicationCenter.Register("AlgoEngine", engine);

            //数据存储，主界面点击保存后，会通知大群中所有成员把数据发给存储模块，然后进行保存。打开工程恢复同理。
            AppDataSerializer appDataSerializer = new AppDataSerializer();//方案存储有必要提升到最高层级
            communicationCenter.Register("AppDataSerializer", appDataSerializer);

            //运行界面线程
            app.Run(windowBase);
        }
    }
}
