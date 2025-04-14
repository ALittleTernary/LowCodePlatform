using LowCodePlatform.Plugin.Base;
using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace LowCodePlatform.Plugin.Task_ReadImage
{
    public enum ReadImageType
    {
        kNone = 0,
        kReadSingleImage = 1,
        kReadFolder = 2,
        kReadCamera = 3,
    }
    /// <summary>
    /// TaskView_ReadImage.xaml 的交互逻辑
    /// </summary>
    public partial class TaskView_ReadImage : System.Windows.Window, TaskViewPluginBase {
        ConfirmClick _confirmClick = null;
        ExecuteClick _executeClick = null;
        LinkClick _linkClick = null;

        private Dictionary<string, ReadImageType> _readImageTypeDic = new Dictionary<string, ReadImageType>() {
            {"单张图片", ReadImageType.kReadSingleImage },
            {"文件夹图片", ReadImageType.kReadFolder },
            {"相机图片", ReadImageType.kReadCamera },
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        public TaskView_ReadImage() {
            InitializeComponent();
            InitEvent();
        }

        public void InitEvent() {
            Button_Execute.Click += Event_Button_Execute_Click;
            Button_Confirm.Click += Event_Button_Confirm_Click;
            Button_SingleImageOpen.Click += Event_Button_FindSingleImagePath;
            Button_ImageFolderOpen.Click += Event_Button_FindImageFolderPath;

        }

        /// <summary>
        /// 找到单张图片的路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_FindSingleImagePath(object sender, RoutedEventArgs e) {
            // 创建 OpenFileDialog 实例
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.Title = "找到单张图片的路径";
            // 设置文件类型过滤器，只显示图片文件
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*.gif;*.webp";

            // 显示文件选择对话框并判断是否点击了"打开"按钮
            if (openFileDialog.ShowDialog() == false) {
                return;
            }
            // 获取选中的文件路径
            TextBox_SingleImagePath.Text = openFileDialog.FileName;
            Event_Button_Execute_Click(null, null);
        }

        /// <summary>
        /// 找到图片文件夹的路径
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_FindImageFolderPath(object sender, RoutedEventArgs e) {
            // 创建 FolderBrowserDialog 实例
            System.Windows.Forms.FolderBrowserDialog folderDialog = new System.Windows.Forms.FolderBrowserDialog();
            // 可选：设置对话框的初始目录
            folderDialog.SelectedPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            // 显示对话框并等待用户选择文件夹
            if (folderDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) {
                return ;
            }
            ListView_ImageFloderShow.Items.Clear();

            // 获取选中的文件夹路径
            string folderPath = folderDialog.SelectedPath;
            TextBox_ImageFolderPath.Text = folderPath;

            // 获取所有图片文件类型的扩展名
            string[] imageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp" };

            // 获取文件夹下所有文件，并过滤出图片文件
            string[] imageFiles = Directory.GetFiles(folderPath)
                                      .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()))
                                      .ToArray();

            foreach (string imageFile in imageFiles) {
                ListView_ImageFloderShow.Items.Add(new ListViewItem() { Content = imageFile });
            }
        }

        /// <summary>
        /// 点击确定按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Confirm_Click(object sender, RoutedEventArgs e) {
            Func<List<string>> func_SummarizeFolderImagePaths = () => {
                List<string> pathList = new List<string>();
                foreach (var item in ListView_ImageFloderShow.Items) {
                    ListViewItem item_ListViewItem = item as ListViewItem;
                    if (item_ListViewItem == null) { 
                        continue;
                    }
                    string imageFile = item_ListViewItem.Content as string;
                    if (imageFile == null) { 
                        continue;
                    }
                    pathList.Add(imageFile);
                }
                return pathList;
            };

            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片获取类型",
                IsBind = false,
                UserParam = _readImageTypeDic[((TabItem)TabControl_ReadImageType.SelectedItem).Header.ToString()].ToString(),//把枚举作为字符串传递过去，直接传递枚举并不能保存数据
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片路径",
                IsBind = false,
                UserParam = TextBox_SingleImagePath.Text,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹路径",
                IsBind = false,
                UserParam = TextBox_ImageFolderPath.Text,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹中图片名称列表",
                IsBind = false,
                UserParam = func_SummarizeFolderImagePaths(),
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹中当前图片序号",
                IsBind = false,
                UserParam = ListView_ImageFloderShow.SelectedIndex,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "相机资源",
                IsBind = LinkEdit_Camera.IsBind,
                UserParam = LinkEdit_Camera.UserParam,
            });
            _confirmClick?.Invoke(inputParams);
        }

        /// <summary>
        /// 点击执行按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Event_Button_Execute_Click(object sender, RoutedEventArgs e) {
            Func<List<string>> func_SummarizeFolderImagePaths = () => {
                List<string> pathList = new List<string>();
                foreach (var item in ListView_ImageFloderShow.Items) {
                    ListViewItem item_ListViewItem = item as ListViewItem;
                    if (item_ListViewItem == null) {
                        continue;
                    }
                    string imageFile = item_ListViewItem.Content as string;
                    if (imageFile == null) {
                        continue;
                    }
                    pathList.Add(imageFile);
                }
                return pathList;
            };

            List<TaskViewInputParams> inputParams = new List<TaskViewInputParams>();
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片获取类型",
                IsBind = false,
                UserParam = _readImageTypeDic[((TabItem)TabControl_ReadImageType.SelectedItem).Header.ToString()].ToString(),//把枚举作为字符串传递过去，直接传递枚举并不能保存数据
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "图片路径",
                IsBind = false,
                UserParam = TextBox_SingleImagePath.Text,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹路径",
                IsBind = false,
                UserParam = TextBox_ImageFolderPath.Text,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹中图片名称列表",
                IsBind = false,
                UserParam = func_SummarizeFolderImagePaths(),
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "文件夹中当前图片序号",
                IsBind = false,
                UserParam = ListView_ImageFloderShow.SelectedIndex,
            });
            inputParams.Add(new TaskViewInputParams() {
                ParamName = "相机资源",
                IsBind = LinkEdit_Camera.IsBind,
                UserParam = LinkEdit_Camera.UserParam,
            });
            _executeClick?.Invoke(inputParams);
        }

        public void ViewOperationDataUpdate(in List<TaskViewInputParams> inputParams, in List<TaskOperationOutputParams> outputParams) {
            if (outputParams.Count < 1 || outputParams[0].ActualParam.GetType() != typeof(Mat)) {
                return;
            }
            Mat mat = (Mat)outputParams[0].ActualParam;
            ImageShow_Read.Image = mat;
            if (outputParams.Count < 2 || outputParams[1].ActualParam.GetType() != typeof(int)) {
                return;
            }
            ListView_ImageFloderShow.SelectedIndex = (int)outputParams[1].ActualParam;
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["TextBox_SingleImagePath"] = TextBox_SingleImagePath.Text;
            json["TextBox_FolderPath"] = TextBox_ImageFolderPath.Text;
            json["TabControl_ReadImageType"] = TabControl_ReadImageType.SelectedIndex;
            json["ListView_ImageFloderShow_Count"] = ListView_ImageFloderShow.Items.Count;
            for (int i = 0; i < ListView_ImageFloderShow.Items.Count; i++) {
                ListViewItem item_ListViewItem = ListView_ImageFloderShow.Items[i] as ListViewItem;
                if (item_ListViewItem == null) {
                    continue;
                }
                string imageFile = item_ListViewItem.Content as string;
                if (imageFile == null) {
                    continue;
                }
                json[i + "_ListView_ImageFloderShow"] = imageFile;
            }
            json["LinkEdit_Camera"] = LinkEdit_Camera.ViewToJson();
            return json.ToString();
        }

        public void JsonToView(string str) {
            if (str == null || str == string.Empty) { 
                return;
            }
            JObject json = JObject.Parse(str);
            TextBox_SingleImagePath.Text = json["TextBox_SingleImagePath"].ToString();
            TextBox_ImageFolderPath.Text = json["TextBox_FolderPath"].ToString();
            TabControl_ReadImageType.SelectedIndex = ((int)json["TabControl_ReadImageType"]);
            int count = ((int)json["ListView_ImageFloderShow_Count"]);
            for (int i = 0; i < count; i++) {
                if (!json.ContainsKey(i + "_ListView_ImageFloderShow")) { 
                    continue;
                }
                string imageFile = json[i + "_ListView_ImageFloderShow"].ToString();
                ListView_ImageFloderShow.Items.Add(new ListViewItem() { Content = imageFile});
            }
            LinkEdit_Camera.JsonToView(json["LinkEdit_Camera"].ToString());
        }

        public void ResetView() {
            TextBox_SingleImagePath.Clear();
            TextBox_ImageFolderPath.Clear();
            TabControl_ReadImageType.SelectedIndex = 0;
            ImageShow_Read.ResetView();
            ListView_ImageFloderShow.Items.Clear();
            LinkEdit_Camera.ResetView();
        }

        public void SetConfirmClickCallback(ConfirmClick confirmClickCallback) {
            if (confirmClickCallback == null) { 
                return ;
            }
            _confirmClick = confirmClickCallback;
        }

        public void SetExecuteClickCallback(ExecuteClick executeClickCallback) {
            if (executeClickCallback == null) {
                return;
            }
            _executeClick = executeClickCallback;
        }

        public void SetLinkClickCallback(LinkClick linkClickCallback) {
            if (linkClickCallback == null) {
                return;
            }
            _linkClick = linkClickCallback;
            LinkEdit_Camera.SetLinkClickCallback(linkClickCallback);
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public string ViewUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "读取图像";
                case LangaugeType.kEnglish:
                    return "ReadImage";
                default:
                    break;
            }
            return string.Empty;
        }
    }
}
