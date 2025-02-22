using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_ShowImage;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace LowCodePlatform.Plugin.Sub_ShowImage
{
    /// <summary>
    /// SubView_ShowImage.xaml 的交互逻辑
    /// </summary>
    public partial class SubView_ShowImage : UserControl, SubViewPluginBase
    {
        /// <summary>
        /// 显示的图片
        /// </summary>
        public Mat Image
        {
            get {
                return ImageShow_Read.Image;
            }
            set {
                ImageShow_Read.Image = value;
            }
        }

        /// <summary>
        /// 显示的区域
        /// </summary>
        public Mat[] Masks
        {
            set {
                ImageShow_Read.Masks = value;
            }
        }

        public Dictionary<LangaugeType, string> UniqueName { get; set; } = new Dictionary<LangaugeType, string>() {
            {LangaugeType.kChinese, "显示图像"},
            {LangaugeType.kEnglish, "ShowImage" },
        };

        /// <summary>
        /// 构造函数
        /// </summary>
        public SubView_ShowImage() {
            InitializeComponent();
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public List<string> AllowTaskPluginLink() {
            List<string> datas = new List <string>();
            TaskOperation_ShowImage showImage = new TaskOperation_ShowImage();
            datas.Add(showImage.OperationUniqueName(LangaugeType.kChinese));
            datas.Add(showImage.OperationUniqueName(LangaugeType.kEnglish));
            return datas;
        }

        public string ViewToJson() {
            return string.Empty ;
        }

        public void JsonToView(string str) {
            
        }

        public void SetViewEditStatus(bool status) {
            
        }
    }
}
