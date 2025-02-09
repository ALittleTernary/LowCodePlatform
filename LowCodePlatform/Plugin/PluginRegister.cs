using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Res_Tcp;
using LowCodePlatform.Plugin.Task_Arithmetic;
using LowCodePlatform.Plugin.Task_BlobAnalysis;
using LowCodePlatform.Plugin.Task_Delay;
using LowCodePlatform.Plugin.Task_Log;
using LowCodePlatform.Plugin.Task_ReadImage;
using LowCodePlatform.Plugin.Task_ShowImage;
using LowCodePlatform.Plugin.Sub_ShowImage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LowCodePlatform.Plugin.Task_Tcp;
using LowCodePlatform.Plugin.Res_Camera;

namespace LowCodePlatform.Plugin
{
    /// <summary>
    /// 插件注册处
    /// </summary>
    public class PluginRegister: PluginManager
    {
        /// <summary>
        /// 注册任务类型的插件——通用算子
        /// </summary>
        protected override void RegisterTaskPlugin() { 
            AddTaskPlugin(TaskPluginType.kDataProcess, new TaskView_Arithmetic(), new TaskOperation_Arithmetic());//四则运算
            AddTaskPlugin(TaskPluginType.kControlStatement, new TaskView_Delay(), new TaskOperation_Delay());//延时
            AddTaskPlugin(TaskPluginType.kResourceObtain, new TaskView_ReadImage(), new TaskOperation_ReadImage());//读取图像
            AddTaskPlugin(TaskPluginType.kDataDisplay, new TaskView_ShowImage(), new TaskOperation_ShowImage());//显示图像
            AddTaskPlugin(TaskPluginType.kDataProcess, new TaskView_BlobAnalysis(), new TaskOperation_BlobAnalysis());//斑点分析
            AddTaskPlugin(TaskPluginType.kResourcePublic, new TaskView_Log(), new TaskOperation_Log());//打印日志
            AddTaskPlugin(TaskPluginType.kResourceObtain, new TaskView_TcpServer(), new TaskOperation_TcpServer());//tcp服务端
        }

        /// <summary>
        /// 注册资源类型的插件——通用资源
        /// </summary>
        protected override void RegisterResourcePlugin() {
            AddResPlugin(new ResView_TcpSever(), new ResOperation_TcpSever());//tcp服务端
            AddResPlugin(new ResView_USBCamera(), new ResOperation_USBCamera());//usb相机
        }

        /// <summary>
        /// 注册界面类型的插件——子界面
        /// </summary>
        protected override void RegisterSubDockPlugin() {
            AddSubViewPlugin(new SubView_ShowImage());//显示图像
            AddSubViewPlugin(new SubView_ShowImage(), new Dictionary<LangaugeType, string>() {
                {LangaugeType.kChinese, "显示图像(2)"},
                {LangaugeType.kEnglish, "ShowImage(2)"}
            });//显示图像，注册一个同样的界面插件，需要修改名字
        }
    }
}
