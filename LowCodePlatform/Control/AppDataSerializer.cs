using AvalonDock.Layout.Serialization;
using AvalonDock;
using LowCodePlatform.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace LowCodePlatform.Control
{
    /// <summary>
    /// 应用程序数据的序列化/反序列化，即保存和恢复数据
    /// </summary>
    public class AppDataSerializer : CommunicationUser
    {
        private JObject _data = new JObject();
        private SendMessage _sendMessage = null;
        private string _saveFilePath = string.Empty;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="name"></param>
        public AppDataSerializer(){

        }

        /// <summary>
        /// 所有Json数据结构，保存为路径文件
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool SaveToJsonFile(string filePath) {
            if (filePath == string.Empty) { 
                return false;
            }
            _saveFilePath = filePath;
            File.WriteAllText(filePath, _data.ToString());
            return true;
        }

        /// <summary>
        /// 当前保存路径是否有效，有效了就返回true，用于界面判断是否需要打开保存界面
        /// </summary>
        /// <returns></returns>
        private string GetSaveFilePath() {
            return _saveFilePath;
        }

        /// <summary>
        /// 解析路径文件,还原为Json数据结构
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool ParseFromJsonFile(string filePath) {
            if (filePath == string.Empty) { 
                return false; 
            }
            string str = File.ReadAllText(filePath);
            _data = JObject.Parse(str);
            return true;
        }

        /// <summary>
        /// 根据Node名字，添加部分数据进入总数据集合中
        /// </summary>
        /// <returns></returns>
        private bool SavePartData(string nodeName, JObject json) {
            if (nodeName == "" || json == null) {
                return false;
            }
            _data[nodeName] = json;
            return true;
        }

        /// <summary>
        /// 根据Node名字，获取总数据集合中的部分数据
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        private string ParsePartData(string nodeName) {
            if (!_data.ContainsKey(nodeName)) { 
                return string.Empty;
            }
            return ((JObject)_data[nodeName]).ToString();
        }

        /// <summary>
        /// 根据Node名字，删除总数据集合中的部分数据
        /// </summary>
        /// <param name="nodeName"></param>
        /// <returns></returns>
        private bool DeletePartData(string nodeName) {
            _data.Remove(nodeName);
            return true;
        }

        /// <summary>
        /// 保存默认布局
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool SaveDefalutLayoutToJsonFile(string data) {
            File.WriteAllText(Directory.GetCurrentDirectory() + "\\Resource\\DefaultLayout.json", data);
            return true;
        }

        /// <summary>
        /// 读取默认布局
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string ParseDefalutLayoutFromJsonFile() {
            string str = File.ReadAllText(Directory.GetCurrentDirectory() + "\\Resource\\DefaultLayout.json");
            return str;
        }


        public void SetSendMessageCallback(SendMessage cb) {
            _sendMessage = cb;
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            if (message == null) {
                return string.Empty;
            }
            else if (message.Function == "SavePartData" && message.Content is string params_SavePartData) {
                SavePartData(message.Source, JObject.Parse(params_SavePartData));
            }
            else if (message.Function == "SaveToJsonFile" && message.Content is string params_SaveToJsonFile) {
                SaveToJsonFile(params_SaveToJsonFile);
            }
            else if (message.Function == "ParseFromJsonFile" && message.Content is string params_ParseFromJsonFile) {
                ParseFromJsonFile(params_ParseFromJsonFile);
            }
            else if (message.Function == "ParsePartData") {
                return ParsePartData(message.Source).ToString();
            }
            else if (message.Function == "GetSaveFilePath") {
                return GetSaveFilePath();
            }
            else if (message.Function == "SaveDefalutLayoutToJsonFile" && message.Content is string params_SaveDefalutLayoutToJsonFile) {
                SaveDefalutLayoutToJsonFile(params_SaveDefalutLayoutToJsonFile);
            }
            else if (message.Function == "ParseDefalutLayoutFromJsonFile") {
                return ParseDefalutLayoutFromJsonFile();
            }
            else {

            }

            return string.Empty;
        }

        public string DataToJson() {
            return string.Empty;
        }

        public void JsonToData(string str) {

        }
    }
}
