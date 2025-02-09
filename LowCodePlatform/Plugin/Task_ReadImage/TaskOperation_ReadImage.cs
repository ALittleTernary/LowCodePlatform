using LowCodePlatform.Plugin.Base;
using LowCodePlatform.Plugin.Res_Camera;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LowCodePlatform.Plugin.Task_ReadImage
{
    public class TaskOperation_ReadImage : TaskOperationPluginBase
    {
        private ReadImageType _readImageType = ReadImageType.kNone;//
        private string _singleImagePath = string.Empty;
        private string _imageFolderPath = string.Empty;
        private List<string> _imageFolderImagePathList = new List<string>();
        private int _folderNextIndex = -1;//输入的序号
        private ResOperation_USBCamera _USBCamera;

        private Mat _image = null;
        public bool EngineIsRunning { get; set; }

        public TaskOperationPluginBase Clone() {
            return new TaskOperation_ReadImage();
        }
        public string OperationUniqueName(LangaugeType type) {
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
        public TaskNodeStatus Start(in List<TaskViewInputParams> inputParams) {
            if (inputParams == null) {
                return TaskNodeStatus.kFailure;
            }
            if (inputParams.Count < 1 || inputParams[0].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            Enum.TryParse(inputParams[0].ActualParam.ToString(), out _readImageType);
            if (inputParams.Count < 2 || inputParams[1].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _singleImagePath = (string)(inputParams[1].ActualParam);
            if (inputParams.Count < 3 || inputParams[2].ActualParam.GetType() != typeof(string)) {
                return TaskNodeStatus.kFailure;
            }
            _imageFolderPath = (string)(inputParams[2].ActualParam);
            if (inputParams.Count < 4 || inputParams[3].ActualParam.GetType() != typeof(List<string>)) {
                return TaskNodeStatus.kFailure;
            }
            _imageFolderImagePathList = (List<string>)(inputParams[3].ActualParam);
            if (inputParams.Count < 5 || inputParams[4].ActualParam.GetType() != typeof(int)) {
                return TaskNodeStatus.kFailure;
            }
            _folderNextIndex = (int)(inputParams[4].ActualParam);
            if (_folderNextIndex == -1) {
                _folderNextIndex = 0;
            }
            if (_readImageType == ReadImageType.kReadFolder && _imageFolderImagePathList.Count > 0) {
                _folderNextIndex = _folderNextIndex % _imageFolderImagePathList.Count;
                inputParams[4].UserParam = _folderNextIndex + 1;//直接修改输入参数，下次输入进来的参数就会改变，一般情况不要修改传入的参数
            }
            if (_readImageType == ReadImageType.kReadCamera && inputParams.Count > 5 && inputParams[5].ActualParam.GetType() == typeof(ResOperation_USBCamera)) {
                _USBCamera = inputParams[5].ActualParam as ResOperation_USBCamera;
            }
            
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Run() {
            Func<TaskNodeStatus> func_ReadSingleImage = () => {
                // 读取图像，使用 IMREAD_COLOR 读取（即使是灰度图也会按 RGB 读取）
                _image = Cv2.ImRead(_singleImagePath, ImreadModes.Color);

                // 检查图像是否加载成功
                if (_image.Empty()) {
                    _image.Dispose();
                    return TaskNodeStatus.kFailure;
                }

                return TaskNodeStatus.kSuccess;
            };

            Func<TaskNodeStatus> func_ReadFolderImage = () => {
                if (_imageFolderImagePathList.Count == 0 || _folderNextIndex < 0) { 
                    return TaskNodeStatus.kFailure;
                }
                _folderNextIndex = _folderNextIndex % _imageFolderImagePathList.Count;
                string path = _imageFolderImagePathList[_folderNextIndex];

                _image = Cv2.ImRead(path, ImreadModes.Color);
                
                // 检查图像是否加载成功
                if (_image.Empty()) {
                    _image.Dispose();
                    return TaskNodeStatus.kFailure;
                }
                return TaskNodeStatus.kSuccess;
            };

            Func<TaskNodeStatus> func_Camera = () => {
                if (_USBCamera == null) {
                    return TaskNodeStatus.kFailure;
                }
                _image = _USBCamera.LastestImage;
                if (_image == null || _image.Empty()) { 
                    return TaskNodeStatus.kFailure;
                }
                return TaskNodeStatus.kSuccess;
            };

            switch (_readImageType) {
                case ReadImageType.kNone:
                    break;
                case ReadImageType.kReadSingleImage:
                    return func_ReadSingleImage();
                    //break;
                case ReadImageType.kReadFolder:
                    return func_ReadFolderImage();
                    //break;
                case ReadImageType.kReadCamera:
                    return func_Camera();
                    //break;
                default:
                    break;
            }

            
            return TaskNodeStatus.kSuccess;
        }

        public TaskNodeStatus Finish(out List<TaskOperationOutputParams> outputParams) {
            outputParams = new List<TaskOperationOutputParams>();
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "图像",
                ActualParam = _image,
                Description = "当前模块获取到的图像",
            });
            outputParams.Add(new TaskOperationOutputParams() {
                ParamName = "图像",
                ActualParam = _folderNextIndex,
                Description = "文件夹读图当前索引",
            });
            return TaskNodeStatus.kSuccess;
        }
    }
}
