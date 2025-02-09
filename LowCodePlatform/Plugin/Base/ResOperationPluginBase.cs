using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Base
{
    public class ResOperationOutputParams
    {
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
                ActualParam = string.Empty;
            }
            Description = json["Description"].ToString();
        }
    }

    /// <summary>
    /// 资源插件发布数据
    /// </summary>
    /// <param name="objs"></param>
    public delegate void ResMessageLaunch(List<ResOperationOutputParams> ouputParams);

    /// <summary>
    /// 资源管理器运算插件基类
    /// 必须有析构函数确保资源释放
    /// </summary>
    public interface ResOperationPluginBase: IDisposable
    {
        /// <summary>
        /// 提供clone接口
        /// </summary>
        /// <returns></returns>
        ResOperationPluginBase Clone();

        /// <summary>
        /// 资源插件中需要唯一名字，对应界面名字
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        string OperationUniqueName(LangaugeType type);

        /// <summary>
        /// 资源状态，是打开还是关闭状态
        /// </summary>
        /// <returns></returns>
        bool ResStatus { get; set; }

        /// <summary>
        /// 开启资源
        /// 开启资源要看看能不能开启成功
        /// </summary>
        void TurnOnRes(List<ResViewInputParams> inputParams);

        /// <summary>
        /// 关闭资源
        /// 关闭资源必须成功
        /// </summary>
        void TurnOffRes(List<ResViewInputParams> inputParams);

        /// <summary>
        /// 开启资源后
        /// 界面与资源进行通讯
        /// 该数据temporaryParams不会被存储
        /// </summary>
        /// <param name="temporaryParams"></param>
        void ResTemporaryEvent(string temporaryParams);

        /// <summary>
        /// 资源信息发布
        /// </summary>
        void SetResMessageLaunchCallback(ResMessageLaunch cb);
    }
}
