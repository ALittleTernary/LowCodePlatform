using LiveCharts.Wpf.Charts.Base;
using LiveCharts.Wpf;
using LowCodePlatform.Plugin.Base;
using Newtonsoft.Json.Linq;
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
using LiveCharts;
using LiveCharts.Definitions.Charts;
using LowCodePlatform.View;
using LowCodePlatform.Plugin.Task_LineChart;


namespace LowCodePlatform.Plugin.Sub_LineChart
{
    /// <summary>
    /// SubView_LineChart.xaml 的交互逻辑
    /// </summary>
    public partial class SubView_LineChart : UserControl, SubViewPluginBase
    {
        public Dictionary<LangaugeType, string> UniqueName { get; set; } = new Dictionary<LangaugeType, string>() {
            {LangaugeType.kChinese, "折线图"},
            {LangaugeType.kEnglish, "LineChart" },
        };



        /// <summary>
        /// 总标题
        /// </summary>
        public string GeneralTitle { 
            get { 
                return Label_Title.Content.ToString();
            }
            set {
                Label_Title.Content = value;
            } 
        }

        /// <summary>
        /// X轴标题
        /// </summary>
        public string XAxisTitle
        {
            get {
                return Label_X.Content.ToString();
            }
            set {
                Label_X.Content = value;
            }
        }

        /// <summary>
        /// Y轴标题
        /// </summary>
        public string YAxisTitle
        {
            get {
                return TextBlock_Y.Text;
            }
            set {
                TextBlock_Y.Text = value;
            }
        }

        private bool _viewEditStatus = false;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SubView_LineChart() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            List<double> xs1 = new List<double> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
            List<double> ys1 = new List<double> { 8, 7, 5, 9, 3, 4, 8, 7, 6, 10 };
            LineSeries_GroupA.Values = new LiveCharts.ChartValues<LiveCharts.Defaults.ObservablePoint>();
            LineSeries_GroupA.Values.Clear();
            for (int i = 0; i < xs1.Count; i++) {
                LineSeries_GroupA.Values.Add(new LiveCharts.Defaults.ObservablePoint(xs1[i], ys1[i]));
            }
        }

        private void InitEvent() {
            Label_X.MouseDoubleClick += (s, e) => {
                if (_viewEditStatus == false) {
                    return;
                }

                InputDialog inputDialog = new InputDialog() { Label = "重命名", DefaultText = Label_X.Content.ToString() };
                inputDialog.ShowDialog();
                string userInput = inputDialog.InputText;
                if (userInput == null) {
                    return;
                }
                if (userInput == Label_X.Content.ToString()) {
                    return;
                }
                Label_X.Content = userInput;
            };
            TextBlock_Y.MouseDown += (s, e) => {
                if (_viewEditStatus == false) {
                    return;
                }

                InputDialog inputDialog = new InputDialog() { Label = "重命名", DefaultText = TextBlock_Y.Text };
                inputDialog.ShowDialog();
                string userInput = inputDialog.InputText;
                if (userInput == null) {
                    return;
                }
                if (userInput == TextBlock_Y.Text) {
                    return;
                }
                TextBlock_Y.Text = userInput;
            };
            Label_Title.MouseDoubleClick += (s, e) => {
                if (_viewEditStatus == false) {
                    return;
                }

                InputDialog inputDialog = new InputDialog() { Label = "重命名", DefaultText = Label_Title.Content.ToString() };
                inputDialog.ShowDialog();
                string userInput = inputDialog.InputText;
                if (userInput == null) {
                    return;
                }
                if (userInput == Label_Title.Content.ToString()) {
                    return;
                }
                Label_Title.Content = userInput;
            };
        }

        public List<string> AllowTaskPluginLink() {
            List<string> datas = new List<string>();
            TaskOperation_LineChart lineChart = new TaskOperation_LineChart();
            datas.Add(lineChart.OperationUniqueName(LangaugeType.kChinese));
            datas.Add(lineChart.OperationUniqueName(LangaugeType.kEnglish));
            return datas;
        }

        public void JsonToView(string str) {
            if (string.IsNullOrEmpty(str)) {
                return;
            }
            JObject json = JObject.Parse(str);
            Label_Title.Content = json["Label_Title"].ToString();
            TextBlock_Y.Text = json["TextBlock_Y"].ToString();
            Label_X.Content = json["Label_X"].ToString();
        }

        public string ViewToJson() {
            JObject json = new JObject();
            json["Label_Title"] = Label_Title.Content.ToString();
            json["TextBlock_Y"] = TextBlock_Y.Text;
            json["Label_X"] = Label_X.Content.ToString();
            return json.ToString();
        }

        public void SetViewEditStatus(bool status) {
            _viewEditStatus = status;
        }

        public void SwitchLanguage(LangaugeType type) {

        }

        public void SetAxisData(List<double> xs1, List<double> ys1) {
            if (xs1 == null || ys1 == null || xs1.Count != ys1.Count) {
                return;
            }
            Application.Current.Dispatcher.BeginInvoke(new Action(() => {
                LineSeries_GroupA.Values.Clear();
                for (int i = 0; i < xs1.Count; i++) {
                    LineSeries_GroupA.Values.Add(new LiveCharts.Defaults.ObservablePoint(xs1[i], ys1[i]));
                }
            }));
        }
    }
}
