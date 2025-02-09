using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Base
{
    /// <summary>
    /// dockmanager界面添加插件基类
    /// </summary>
    public interface SubViewPluginBase
    {
        /// <summary>
        /// 获取当前插件的中/英/?名字
        /// 名字要求独特
        /// 界面区根据这个显示名字
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string GetUniqueName(LangaugeType type);

        /// <summary>
        /// 修改插件名字，一般在界面插件注册时使用，界面改个名字就能复用
        /// </summary>
        /// <param name="dic"></param>
        void SetUniqueName(Dictionary<LangaugeType, string> dic);

        /// <summary>
        /// 获取这个界面插件允许哪些任务插件列表链接
        /// 不要new出window或者usercontrol使用其中的uniquename会导致资源释放失败
        /// </summary>
        /// <returns></returns>
        List<string> AllowTaskPluginLink();

        /// <summary>
        /// 管理器统一调用，把界面翻译成指定类型，具体实现由各自界面自己完成
        /// </summary>
        /// <param name="type"></param>
        void SwitchLanguage(LangaugeType type);
    }
}
