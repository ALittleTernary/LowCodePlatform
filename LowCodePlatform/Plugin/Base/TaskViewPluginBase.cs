using LowCodePlatform.Engine;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Base
{
    /// <summary>
    /// 链接数据的类型
    /// </summary>
    public enum LinkDataType{ 
        /// <summary>
        /// 空置类型
        /// </summary>
        kNone = 0,
        kInt = 1,
        kListInt = 2,
        kFloat = 3,
        kListFloat = 4,
        kDouble = 5,
        kListDouble = 6,
        kString = 7,
        kListString = 8,
        kBool = 9,
        kListBool = 10,
        kRegion = 11,
        kListRegion = 12,
        kMat = 13,
        kListMat = 14,
        kView = 15,
        kResource = 16,
        /// <summary>
        /// 变量类型
        /// </summary>
        kVariable = 17,
        /// <summary>
        /// 对象类型
        /// </summary>
        kObject = 100,
    }


    public class TaskViewInputParams
    {
        /// <summary>
        /// 输入参数名字
        /// </summary>
        public string ParamName { set; get; } = string.Empty;

        /// <summary>
        /// 该输入参数是否绑定之前模块的输出
        /// 不绑定为false，绑定为true
        /// </summary>
        public bool IsBind { set; get; } = false;

        /// <summary>
        /// 不绑定时需要赋值，用户界面的输入参数
        /// 支持int/double/string
        /// 绑定时赋值绑定回调返回的链接
        /// </summary>
        public object UserParam = null;

        /// <summary>
        /// 由算法引擎赋值，插件开发者不需要处理
        /// 如果未绑定则赋值为UserParam，如果绑定则将BindParam对应的数据赋值给此
        /// 该值不进行序列化
        /// </summary>
        public object ActualParam = null;

        /// <summary>
        /// 输入参数的注释描述，可以不实现，因为应该不会用到
        /// </summary>
        public string Description { set; get; } = string.Empty;

        public TaskViewInputParams Clone() {
            var clone = new TaskViewInputParams();
            clone.ParamName = ParamName;
            clone.IsBind = IsBind;
            clone.UserParam = UserParam;
            clone.ActualParam = ActualParam;//引用指针
            return clone;
        }

        /// <summary>
        /// 数据保存为json
        /// </summary>
        /// <returns></returns>
        public string DataToJson() {
            JObject json = new JObject();
            json["ParamName"] = ParamName;
            json["IsBind"] = IsBind;
            if (UserParam is int int_UserParam) {
                json["UserParam_Type"] = "int";
                json["UserParam_Value"] = int_UserParam;
            }
            else if (UserParam is float float_UserParam) {
                json["UserParam_Type"] = "float";
                json["UserParam_Value"] = float_UserParam;
            }
            else if (UserParam is double double_UserParam) {
                json["UserParam_Type"] = "double";
                json["UserParam_Value"] = double_UserParam;
            }
            else if (UserParam is string string_UserParam) {
                json["UserParam_Type"] = "string";
                json["UserParam_Value"] = string_UserParam;
            }
            else if (UserParam is bool bool_UserParam) {
                json["UserParam_Type"] = "bool";
                json["UserParam_Value"] = bool_UserParam;
            }
            else if (UserParam is List<int> Listint_UserParam) {
                json["UserParam_Type"] = "List<int>";
                json["UserParam_Value"] = string.Join(",", Listint_UserParam);
            }
            else if (UserParam is List<float> Listfloat_UserParam) {
                json["UserParam_Type"] = "List<float>";
                json["UserParam_Value"] = string.Join(",", Listfloat_UserParam);
            }
            else if (UserParam is List<double> Listdouble_UserParam) {
                json["UserParam_Type"] = "List<double>";
                json["UserParam_Value"] = string.Join(",", Listdouble_UserParam);
            }
            else if (UserParam is List<string> Liststring_UserParam) {
                json["UserParam_Type"] = "List<string>";
                json["UserParam_Value"] = string.Join(",", Liststring_UserParam);
            }
            else if (UserParam is List<bool> Listbool_UserParam) {
                json["UserParam_Type"] = "List<bool>";
                json["UserParam_Value"] = string.Join(",", Listbool_UserParam);
            }
            else if (UserParam is Mat Mat_UserParam) {
                json["UserParam_Type"] = "Mat";
                json["UserParam_Value"] = string.Empty;
            }
            else if (UserParam is Mat[] Region_UserParam) {
                json["UserParam_Type"] = "Mat[]";
                json["UserParam_Value"] = string.Empty;
            }
            else {
                json["UserParam_Type"] = "unknownParams";
                json["UserParam_Value"] = "";
            }
            json["Description"] = Description;
            string str = json.ToString();
            return str;
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
            IsBind = ((bool)json["IsBind"]);
            string UserParam_Type = json["UserParam_Type"].ToString();
            if (UserParam_Type == "int") {
                UserParam = ((int)json["UserParam_Value"]);
            }
            else if (UserParam_Type == "float") {
                UserParam = ((float)json["UserParam_Value"]);
            }
            else if (UserParam_Type == "double") {
                UserParam = ((double)json["UserParam_Value"]);
            }
            else if (UserParam_Type == "string") {
                UserParam = json["UserParam_Value"].ToString();
            }
            else if (UserParam_Type == "bool") {
                UserParam = ((bool)json["UserParam_Value"]);
            }
            else if (UserParam_Type == "List<int>") {
                UserParam = json["UserParam_Value"].ToString().Split(',').Select(int.Parse).ToList();
            }
            else if (UserParam_Type == "List<float>") {
                UserParam = json["UserParam_Value"].ToString().Split(',').Select(float.Parse).ToList();
            }
            else if (UserParam_Type == "List<double>") {
                UserParam = json["UserParam_Value"].ToString().Split(',').Select(double.Parse).ToList();
            }
            else if (UserParam_Type == "List<string>") {
                UserParam = json["UserParam_Value"].ToString().Split(',').ToList();
            }
            else if (UserParam_Type == "List<bool>") {
                UserParam = json["UserParam_Value"].ToString().Split(',').Select(bool.Parse).ToList();
            }
            else if (UserParam_Type == "Mat") {
                UserParam = new Mat();
            }
            else if (UserParam_Type == "Mat[]") {
                List<Mat> mats = new List<Mat>();
                UserParam = mats.ToArray();
            }
            else {

            }
            Description = json["Description"].ToString();
        }
    }

    public delegate void ConfirmClick(List<TaskViewInputParams> bindingInputParams);

    public delegate void ExecuteClick(List<TaskViewInputParams> bindingInputParams);

    public delegate string LinkClick(LinkDataType type);

    /// <summary>
    /// 任务界面插件接口
    /// 同一种类的任务用同一个界面，一方面是用于提醒开发者是否正确存储数据，另一方面是节省一点内存
    /// </summary>
    public interface TaskViewPluginBase
    {
        /// <summary>
        /// 硬编码
        /// 界面数据总结为str传出给到管理器存储，推荐使用json
        /// 调用时机在点击确定或者执行按钮后
        /// </summary>
        /// <returns></returns>
        string ViewToJson();

        /// <summary>
        /// 硬编码
        /// 管理器存储的str传递给界面进行还原，推荐使用json
        /// 调用时机在双击任务打开编辑界面
        /// </summary>
        void JsonToView(string str);

        /// <summary>
        /// 重置界面所有数据
        /// </summary>
        void ResetView();

        /// <summary>
        /// 动态编码
        /// 获取该任务所有的输入输出数据
        /// 调用位置在JsonToView之后，意味着其重要性高过JsonToView
        /// 调用时机在每次双击任务打开编辑界面，或者点击执行按钮后
        /// 使用场景1，“blob分析”打开界面后需要单步执行获取每个区域的数据（获取该任务本身执行一次后，刷新后的输出数据）
        /// 使用场景2，“柱状图显示”打开界面后需要更新通过链接输入的数据
        /// </summary>
        void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams);

        /// <summary>
        /// 传入界面点击确定回调
        /// 界面会有一个确定按钮，保存数据后关闭界面，关闭界面后管理器需要进行一些处理，由此回调进行通知管理器
        /// </summary>
        void SetConfirmClickCallback(ConfirmClick confirmClickCallback);

        /// <summary>
        /// 传入界面点击执行回调
        /// 界面会有一个执行按钮，保存数据后单步执行该算子，由此回调进行通知管理器
        /// </summary>
        void SetExecuteClickCallback(ExecuteClick executeClickCallback);

        /// <summary>
        /// 传入界面点击链接回调
        /// 界面会有很多链接按钮，点击链接后会打开链接界面，关闭链接界面后会返回选中的链接字符串
        /// </summary>
        /// <param name="linkClickCallback"></param>
        void SetLinkClickCallback(LinkClick linkClickCallback);

        /// <summary>
        /// 获取当前插件的中/英/?名字
        /// 名字要求独特
        /// 选项区根据这个显示名字，点击组合区后，根据名字找到插件管理器中对应的界面
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string ViewUniqueName(LangaugeType type);

        /// <summary>
        /// 管理器统一调用，把界面翻译成指定类型，具体实现由各自界面自己完成
        /// </summary>
        /// <param name="type"></param>
        void SwitchLanguage(LangaugeType type);
    }

    /// <summary>
    /// 链接编辑任务界面插件基础的基类
    /// </summary>
    public interface LinkEditTaskViewPluginBase : TaskViewPluginBase {
        /// <summary>
        /// 总结当前节点之前数据
        /// </summary>
        /// <param name="cb"></param>
        void SetSummarizeBeforeNodesCallback(SummarizeBeforeNodes cb);
    }
}
