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
        /// 界面独特名字
        /// </summary>
        Dictionary<LangaugeType, string> UniqueName { get; set; }

        /// <summary>
        /// 设置界面是否允许编辑
        /// </summary>
        /// <param name="status"></param>
        void SetViewEditStatus(bool status);

        /// <summary>
        /// 界面存储为json
        /// </summary>
        /// <returns></returns>
        string ViewToJson();

        /// <summary>
        /// json还原为界面
        /// </summary>
        /// <param name="str"></param>
        void JsonToView(string str);

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
