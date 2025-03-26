using LowCodePlatform.Engine;
using Newtonsoft.Json.Linq;
using OpenCvSharp;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace LowCodePlatform.View
{
    /// <summary>
    /// Serilog日志库绑定的控件
    /// </summary>
    public class DataGridSink : ILogEventSink
    {
        /// <summary>
        /// log日志显示用的数据结构
        /// </summary>
        private class LogData : INotifyPropertyChanged
        {
            private string _time;
            public string Time
            {
                get { return _time; }
                set {
                    _time = value;
                    OnPropertyChanged(nameof(Time));
                }
            }

            private string _level;
            public string Level
            {
                get { return _level; }
                set {
                    _level = value;
                    OnPropertyChanged(nameof(Level));
                }
            }

            private string _content;
            public string Content
            {
                get { return _content; }
                set {
                    _content = value;
                    OnPropertyChanged(nameof(Content));
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;
            private void OnPropertyChanged(string propertyName) {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private ObservableCollection<LogData> _logs { get; set; } = new ObservableCollection<LogData>();

        private System.Windows.Controls.DataGrid _dataGrid = null;

        public LogEventLevel LevelFilter { get; set; } = LogEventLevel.Verbose;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataGrid"></param>
        public DataGridSink(System.Windows.Controls.DataGrid dataGrid) {
            _dataGrid = dataGrid;
            _dataGrid.ItemsSource = _logs;
            CollectionView view = CollectionViewSource.GetDefaultView(_logs) as CollectionView;
            view.Filter = (object obj) => {
                LogData log = obj as LogData;
                if (log == null) { 
                    return false;
                }
                else if (LevelFilter == LogEventLevel.Verbose) { 
                    return true;
                }
                else if (LevelFilter == LogEventLevel.Debug && (log.Level == LogEventLevel.Debug.ToString() || log.Level == LogEventLevel.Information.ToString() || log.Level == LogEventLevel.Warning.ToString() || log.Level == LogEventLevel.Error.ToString() || log.Level == LogEventLevel.Fatal.ToString())) { 
                    return true;
                }
                else if (LevelFilter == LogEventLevel.Information && (log.Level == LogEventLevel.Information.ToString() || log.Level == LogEventLevel.Warning.ToString() || log.Level == LogEventLevel.Error.ToString() || log.Level == LogEventLevel.Fatal.ToString())) {
                    return true;
                }
                else if (LevelFilter == LogEventLevel.Warning && (log.Level == LogEventLevel.Warning.ToString() || log.Level == LogEventLevel.Error.ToString() || log.Level == LogEventLevel.Fatal.ToString())) {
                    return true;
                }
                else if (LevelFilter == LogEventLevel.Error && (log.Level == LogEventLevel.Error.ToString() || log.Level == LogEventLevel.Fatal.ToString())) {
                    return true;
                }
                else if (LevelFilter == LogEventLevel.Fatal && log.Level == LogEventLevel.Fatal.ToString()) {
                    return true;
                }
                return false;
            };
        }

        public void Emit(LogEvent logEvent) {
            LogData data = new LogData() { 
                Time = logEvent.Timestamp.ToString("yyyy-MM-dd HH:mm:ss"),
                Level = logEvent.Level.ToString(),
                Content = logEvent.RenderMessage(),
            };
            //使用界面线程单独更新，别卡着运算线程
            Application.Current?.Dispatcher?.BeginInvoke(new Action(() => {
                _logs.Add(data);
                while (_logs.Count > 100) {
                    _logs.RemoveAt(0);
                }
                if (_dataGrid.Items.Count > 0) {
                    var lastItem = _dataGrid.Items[_dataGrid.Items.Count - 1];
                    _dataGrid.SelectedItem = lastItem;
                    _dataGrid.ScrollIntoView(lastItem);
                }
            }));
        }

        public void ClearShowLog() {
            _logs.Clear();
        }
    }

    /// <summary>
    /// LogArea.xaml 的交互逻辑
    /// </summary>
    public partial class LogArea : System.Windows.Controls.UserControl, CommunicationUser
    {
        /// <summary>
        /// 把数据发送给大群成员
        /// </summary>
        private SendMessage _sendMessage = null;

        /// <summary>
        /// 与Serilog日志库绑定的控件
        /// </summary>
        DataGridSink _dataGridSink = null;

        /// <summary>
        /// 构造函数
        /// </summary>
        public LogArea() {
            InitializeComponent();
            InitView();
            InitEvent();
        }

        private void InitView() {
            _dataGridSink = new DataGridSink(DataGrid_Logger);
            Log.Logger = new LoggerConfiguration()
                        .MinimumLevel.Verbose()
                        .WriteTo.Sink(_dataGridSink)
                        .CreateLogger();
        } 

        private void InitEvent() {
            RoutedEventHandler Event_RadioButton_Checked = (sender, e) => {
                if (RadioButton_Verbose.IsChecked == true) {
                    _dataGridSink.LevelFilter = LogEventLevel.Verbose;
                }
                else if (RadioButton_Warning.IsChecked == true) {
                    _dataGridSink.LevelFilter = LogEventLevel.Warning;
                }
                else if (RadioButton_Error.IsChecked == true) {
                    _dataGridSink.LevelFilter = LogEventLevel.Error;
                }
                else { 
                
                }
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(DataGrid_Logger.ItemsSource);
                view.Refresh();
            };
            RadioButton_Verbose.Checked += Event_RadioButton_Checked;
            RadioButton_Warning.Checked += Event_RadioButton_Checked;
            RadioButton_Error.Checked += Event_RadioButton_Checked;

            RadioButton_Verbose.IsChecked = true;

            Button_ClearLogShow.Click += (s, e) => {
                _dataGridSink.ClearShowLog();
            };
        }

        public object AcceptMessage(CommunicationCenterMessage message) {
            return string.Empty;
        }

        public void SetSendMessageCallback(SendMessage cb) {
            _sendMessage = cb;
        }

        public string DataToJson() {
            return string.Empty;
        }

        public void JsonToData(string str) {

        }
    }
}
