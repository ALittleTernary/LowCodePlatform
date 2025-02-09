using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Task_ReadImage;
using LowCodePlatform.Plugin.Sub_ShowImage;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_ShowImage
{
    internal class TaskOperation_ShowImage : TaskOperationPluginBase
    {
        private SubView_ShowImage _showView = null;
        private Mat _image = null;
        private Mat[] _masks = null;

        public bool EngineIsRunning { get; set; }
        public TaskOperationPluginBase Clone() {
            return new TaskOperation_ShowImage();
        }
        public string OperationUniqueName(LangaugeType type) {
            switch (type) {
                case LangaugeType.kChinese:
                    return "显示图像";
                case LangaugeType.kEnglish:
                    return "ShowImage";
                default:
                    break;
            }
            return string.Empty;
        }

        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(SubView_ShowImage)) {
                return TaskNodeStatus.kFailure;
            }
            _showView = inputParams[0].ActualParam as SubView_ShowImage;
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(Mat)) {
                return TaskNodeStatus.kFailure;
            }
            _image = inputParams[1].ActualParam as Mat;
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(Mat[])) {
                return TaskNodeStatus.kFailure;
            }
            _masks = inputParams[2].ActualParam as Mat[];
            return TaskNodeStatus.kSuccess;
        }
        public TaskNodeStatus Run() {
            _showView.Image = _image;
            _showView.Masks = _masks;
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "显示图像",
                ActualParam = _image,
                Description = "显示图像最终的显示效果图"
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "显示区域",
                ActualParam = _masks,
                Description = "显示区域最终的显示效果图"
            });
            return TaskNodeStatus.kSuccess;
        }
    }
}
