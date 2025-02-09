using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace LowCodePlatform.Control
{
    internal class LogControl
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public LogControl() {
            InitLog();
        }

        private void InitLog() {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Console()
                .CreateLogger();

            try {
                //正常来说应该是日志启动以后，再启动软件
                Log.Verbose("Log.Verbose——详细的日志信息，记录所有信息。");
                Log.Debug("Log.Debug——调试信息，适用于开发阶段。");
                Log.Information("Log.Information——一般信息，记录应用程序的正常操作。");
                Log.Warning("Log.Warning——警告信息，表示潜在的问题。");
                Log.Error("Log.Error——错误信息，记录错误事件。");
                Log.Fatal("Log.Fatal——致命错误，通常会导致应用程序崩溃。");
            }
            catch (Exception ex) {
                Log.Fatal(ex, "日志启动失败");
                throw;
            }
            finally {
                Log.CloseAndFlush();
            }
        }

    }


}
