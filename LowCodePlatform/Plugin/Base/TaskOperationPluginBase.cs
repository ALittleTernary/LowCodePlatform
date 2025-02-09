using LowCodePlatform.Engine;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LowCodePlatform.Plugin.Base
{
    public enum TaskNodeStatus
    {
        /// <summary>
        /// 置空
        /// </summary>
        kNone = 0,
        /// <summary>
        /// 运行成功
        /// 继续往下执行
        /// </summary>
        kSuccess = 1,
        /// <summary>
        /// 运行失败
        /// 继续往下执行
        /// </summary>
        kFailure = 2,
        ///// <summary>
        ///// 当前流程停止运行，其他流程还是正常跑的
        ///// </summary>
        //kFlowStop = 3,
        ///// <summary>
        ///// 当前流程重新运行，不影响其他流程
        ///// </summary>
        //kFlowRerun = 4,
        /// <summary>
        /// 整个工程停止，除了资源管理器里的资源
        /// </summary>
        kProcessStop = 5,
        /// <summary>
        /// 整个工程重新运行，不影响资源管理器里的资源
        /// </summary>
        kProcessRerun = 6,
        /// <summary>
        /// 节点正在运行
        /// 插件中不应该使用，主要是由引擎通知界面item界面更新旋转图标使用，如果插件中使用，视为kFailure
        /// </summary>
        kRunning = 7,
    }

    public class TaskOperationOutputParams
    {
        /// <summary>
        /// 输出参数名字
        /// </summary>
        public string ParamName { set; get; } = string.Empty;

        /// <summary>
        /// 输出参数类型
        /// </summary>
        public LinkDataType ParamType { set; get; } = LinkDataType.kNone;

        /// 实际的输出参数值
        /// 支持int/double/string/List<int>/List<double>/List<string>/Mat,不支持Mat的序列化和反序列化
        /// </summary>
        public object ActualParam = null;

        /// <summary>
        /// 输出参数的注释描述，可以不实现，因为应该不会用到
        /// </summary>
        public string Description { set; get; } = string.Empty;

        public TaskOperationOutputParams Clone() {
            TaskOperationOutputParams clone = new TaskOperationOutputParams();
            clone.ParamName = ParamName;
            clone.ActualParam = ActualParam;
            clone.Description = Description;
            return clone;
        }

        /// <summary>
        /// 数据保存为json
        /// </summary>
        /// <returns></returns>
        public string DataToJson() {
            JObject json = new JObject();
            json["ParamName"] = ParamName;
            json["ParamType"] = ParamType.ToString();
            if (ActualParam is int int_ActualParam) {
                json["ActualParam_Type"] = "int";
                json["ActualParam_Value"] = int_ActualParam;
            }
            else if (ActualParam is float float_ActualParam) {
                json["ActualParam_Type"] = "float";
                json["ActualParam_Value"] = float_ActualParam;
            }
            else if (ActualParam is double double_ActualParam) {
                json["ActualParam_Type"] = "double";
                json["ActualParam_Value"] = double_ActualParam;
            }
            else if (ActualParam is string string_ActualParam) {
                json["ActualParam_Type"] = "string";
                json["ActualParam_Value"] = string_ActualParam;
            }
            else if (ActualParam is bool bool_ActualParam) {
                json["ActualParam_Type"] = "bool";
                json["ActualParam_Value"] = bool_ActualParam;
            }
            else if (ActualParam is List<int> Listint_ActualParam) {
                json["ActualParam_Type"] = "List<int>";
                json["ActualParam_Value"] = string.Join(",", Listint_ActualParam);
            }
            else if (ActualParam is List<float> Listfloat_ActualParam) {
                json["ActualParam_Type"] = "List<float>";
                json["ActualParam_Value"] = string.Join(",", Listfloat_ActualParam);
            }
            else if (ActualParam is List<double> Listdouble_ActualParam) {
                json["ActualParam_Type"] = "List<double>";
                json["ActualParam_Value"] = string.Join(",", Listdouble_ActualParam);
            }
            else if (ActualParam is List<string> Liststring_ActualParam) {
                json["ActualParam_Type"] = "List<double>";
                json["ActualParam_Value"] = string.Join(",", Liststring_ActualParam);
            }
            else if (ActualParam is List<bool> Listbool_ActualParam) {
                json["ActualParam_Type"] = "List<bool>";
                json["ActualParam_Value"] = string.Join(",", Listbool_ActualParam);
            }
            else {
                json["ActualParam_Type"] = "unknownParams";
                json["ActualParam_Value"] = "DontSaveTypeValues";
            }
            json["Description"] = Description;
            return json.ToString();
        }

        /// <summary>
        /// json还原为数据
        /// </summary>
        /// <param name="str"></param>
        public void JsonToData(string str) {
            if (str == null || str == "") {
                return;
            }
            JObject json = JObject.Parse(str);
            ParamName = json["ParamName"].ToString();
            if (Enum.TryParse(json["ParamType"].ToString(), out LinkDataType type)) {
                ParamType = type;
            }
            string ActualParam_Type = json["ActualParam_Type"].ToString();
            if (ActualParam_Type == "int") {
                ActualParam = ((int)json["ActualParam_Value"]);
            }
            else if (ActualParam_Type == "float") {
                ActualParam = ((float)json["ActualParam_Value"]);
            }
            else if (ActualParam_Type == "double") {
                ActualParam = ((double)json["ActualParam_Value"]);
            }
            else if (ActualParam_Type == "string") {
                ActualParam = json["ActualParam_Value"].ToString();
            }
            else if (ActualParam_Type == "bool") {
                ActualParam = ((bool)json["ActualParam_Value"]);
            }
            else if (ActualParam_Type == "List<int>") {
                ActualParam = json["ActualParam_Value"].ToString().Split(',').Select(int.Parse).ToList();
            }
            else if (ActualParam_Type == "List<float>") {
                ActualParam = json["ActualParam_Value"].ToString().Split(',').Select(float.Parse).ToList();
            }
            else if (ActualParam_Type == "List<double>") {
                ActualParam = json["ActualParam_Value"].ToString().Split(',').Select(double.Parse).ToList();
            }
            else if (ActualParam_Type == "List<string>") {
                ActualParam = json["ActualParam_Value"].ToString().Split(',').ToList();
            }
            else if (ActualParam_Type == "List<bool>") {
                ActualParam = json["ActualParam_Value"].ToString().Split(',').Select(bool.Parse).ToList();
            }
            else {
                ActualParam = json["ActualParam_Value"].ToString();
            }
            Description = json["Description"].ToString();
        }
    }

    /// <summary>
    /// 任务运算插件基类
    /// </summary>
    public interface TaskOperationPluginBase
    {
        /// <summary>
        /// clone接口
        /// 任务运算不再像任务界面一样取巧，任务运算则是每个任务均有一个单独的任务运算对象，而不是像界面一样一种任务才共享一个任务界面
        /// </summary>
        /// <returns></returns>
        TaskOperationPluginBase Clone();

        string OperationUniqueName(LangaugeType type);

        /// <summary>
        /// 任务运算获取参数
        /// </summary>
        /// <returns></returns>
        TaskNodeStatus Start(in List<TaskViewInputParams> inputParams);

        /// <summary>
        /// 任务运算
        /// </summary>
        /// <returns></returns>
        TaskNodeStatus Run();

        /// <summary>
        /// 任务运算输出参数
        /// </summary>
        /// <returns></returns>
        TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams);

        /// <summary>
        /// 如果要在插件内循环的话，需要注意引擎停止了，要停止循环
        /// true为引擎正在运行，false为外界点击了停止
        /// </summary>
        bool EngineIsRunning { get; set; }
    }
}
