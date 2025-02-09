using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Base
{
    public class ResViewInputParams {
        /// <summary>
        /// 输入参数名字
        /// </summary>
        public string ParamName { set; get; } = string.Empty;

        /// <summary>
        /// 输入参数实际值
        /// </summary>
        public object ActualParam = null;

        /// <summary>
        /// 输入参数的注释描述，可以不实现，因为应该不会用到
        /// </summary>
        public string Description { set; get; } = string.Empty;

        /// <summary>
        /// 数据保存为json
        /// </summary>
        /// <returns></returns>
        public string DataToJson() {
            JObject json = new JObject();
            json["ParamName"] = ParamName;
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
                json["ActualParam_Type"] = "List<string>";
                json["ActualParam_Value"] = string.Join(",", Liststring_ActualParam);
            }
            else if (ActualParam is List<bool> Listbool_ActualParam) {
                json["ActualParam_Type"] = "List<bool>";
                json["ActualParam_Value"] = string.Join(",", Listbool_ActualParam);
            }
            else if (ActualParam is Mat Mat_ActualParam) {
                json["ActualParam_Type"] = "Mat";
                json["ActualParam_Value"] = string.Empty;
            }
            else if (ActualParam is Mat[] Region_ActualParam) {
                json["ActualParam_Type"] = "Mat[]";
                json["ActualParam_Value"] = string.Empty;
            }
            else {
                json["ActualParam_Type"] = "unknownParams";
                json["ActualParam_Value"] = "";
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
            else if (ActualParam_Type == "Mat") {
                ActualParam = new Mat();
            }
            else if (ActualParam_Type == "Mat[]") {
                List<Mat> mats = new List<Mat>();
                ActualParam = mats.ToArray();
            }
            else {

            }
            Description = json["Description"].ToString();
        }
    }

    /// <summary>
    /// 资源开启，传入参数，这些参数会被程序记录
    /// </summary>
    /// <param name="inputParams"></param>
    public delegate void TurnOnResClick(in List<ResViewInputParams> inputParams);

    /// <summary>
    /// 资源关闭，传入参数，这些参数会被程序记录
    /// </summary>
    /// <param name="inputParams"></param>
    public delegate void TurnOffResClick(in List<ResViewInputParams> inputParams);

    /// <summary>
    /// 资源开启后，会有其他交互事件，然后才会是资源关闭
    /// 其他交互事件的内容属于调试内容，不会被程序记录，如果要在资源开启后，然后记录交互内容，使用TurnOnResClick完成
    /// 例如在资源插件界面中，tcp服务/客户端暂时的发送数据事件、采集图像暂时的发送开始取图事件
    /// </summary>
    /// <param name="inputParams"></param>
    public delegate void ResTemporaryEvent(string temporaryParams);

    /// <summary>
    /// 资源管理器中的界面插件基类
    /// 使用同一个界面对于同一类的Operation端，作用于提醒开发者是否保存成功，并且所有的数据都应该在Operation端处理好，界面仅做显示
    /// 例如tcp服务端有多少个客户端连接了，这个必须由Operation端进行维护，而不应该推诿给view端
    /// </summary>
    public interface ResViewPluginBase
    {
        /// <summary>
        /// 获取当前插件的中/英/?名字
        /// 名字要求独特
        /// 界面区根据这个显示名字
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string ViewUniqueName(LangaugeType type);

        /// <summary>
        /// 输入参数可能被task算子改变
        /// 例如task算子中，改变了相机的曝光，那再次打开时，这里的相机曝光参数就会被修改
        /// </summary>
        /// <param name="config"></param>
        /// <param name="obj"></param>
        void ViewOperationDataUpdate(in List<ResViewInputParams> inputParams, in List<ResOperationOutputParams> outputParams);

        /// <summary>
        /// 硬编码
        /// 界面数据总结为str传出给到管理器存储，推荐使用json处理
        /// 触发节点在全局资源切换到对应选项时
        /// </summary>
        /// <returns></returns>
        string ViewToJson();

        /// <summary>
        /// 重置界面，同类型共用一个界面，为了方便开发者清楚自己是否保存成功了
        /// </summary>
        void ResetView();

        /// <summary>
        /// 硬编码
        /// 管理器存储的str传递给界面进行还原，推荐使用json
        /// 调用时机在全局资源切换走到其他选项时
        /// </summary>
        void JsonToView(string str);

        /// <summary>
        /// 获取这个资源插件对应的任务插件，task插件链接资源时需要根据这个进行筛选
        /// 不要new出window或者usercontrol使用其中的uniquename会导致资源释放失败
        /// </summary>
        /// <returns></returns>
        List<string> AllowTaskPluginLink();

        /// <summary>
        /// 管理器统一调用，把界面翻译成指定类型，具体实现由各自界面自己完成
        /// </summary>
        /// <param name="type"></param>
        void SwitchLanguage(LangaugeType type);

        /// <summary>
        /// 由关闭转开启时调用
        /// </summary>
        /// <param name="cb"></param>
        void SetTurnOnResClickCallback(TurnOnResClick cb);

        /// <summary>
        /// 由开启转关闭时调用
        /// </summary>
        /// <param name="cb"></param>
        void SetTurnOffResClickCallback(TurnOffResClick cb);

        /// <summary>
        /// 界面与运算数据交互
        /// </summary>
        /// <param name="cb"></param>
        void SetResTemporaryEventCallback(ResTemporaryEvent cb);
    }
}
