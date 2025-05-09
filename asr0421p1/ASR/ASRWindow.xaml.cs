using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.AI.Translation.Text;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using UniversalLib.Common;
using UniversalLib.Core.Monitors;
using UniversalLib.Core.Sensors;
using UniversalLib.Services;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using AppWindow = Microsoft.UI.Windowing.AppWindow;
using SpeechRecognizer = Microsoft.CognitiveServices.Speech.SpeechRecognizer;
using Window = Microsoft.UI.Xaml.Window;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace asr0421p1.ASR
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class ASRWindow : Window
    {
        private ASRWindowVm _asrWindowVm;
        private bool _isDragging = false;

        private bool isRecognizing = false;
        private readonly List<string> _recognitionResults = new();

        //  Azure 语音服务密钥和区域
        //private const string speechKey = "9bip1SRtixiGbvLlIrD77AHlG02Z2NBCnUkpLvmF4OankckyqGt1JQQJ99BDACYeBjFXJ3w3AAAYACOGGLSi";
        //private const string speechRegion = "eastus";

        private const string speechKey = "89TPNkg1y6WFDpkH2Ee80aJPEyJ9eNlm14S9SjvGaPYDtexlHKh0JQQJ99BDAEHpCsCfT1gyAAAYACOG5uvI";
        private const string speechRegion = "chinanorth3";


        // 翻译
        private const string translatorKey = "Dx7cnF12NpSHrf0eLYYRHcbhNEvPWTJPRLe8Ft0fZ6dF7fRHonX3JQQJ99BDACYeBjFXJ3w3AAAbACOGfMGj";
        private const string translatorEndpoint = "https://yanboasr.cognitiveservices.azure.com/";
        private const string translatorRegion = "eastus";

        private TextTranslationClient _textTranslationClient;
        private SpeechRecognizer _recognizer;
        private AudioConfig _audioConfig;
        private bool _isMicActive = false;

        private string _currentTargetLanguage = "en-US";
        private bool _translationEnabled = true;
        ISensorStatusManagerServices _sensorStatusManagerServices;
        IMonitorStatusManagerServices _monitorStatusManagerServices;
        IStylusGestureServices _stylusGestureServices;
        private readonly ScreenNameEnum CurrentScreenType;
        public ASRWindow(ScreenNameEnum screenName)
        {
            CurrentScreenType = screenName;
            GlobalConstant.Instance.SystemMachineType();

            this.InitializeComponent();

            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;


            // 设置窗口初始大小
            this.AppWindow.Resize(new SizeInt32(2220, 640));

            //InitializeSpeechRecognizer();

            InitializeSpeechResources();
            InitializeTranslationClient();


            _sensorStatusManagerServices = ((App)Application.Current).GetService<ISensorStatusManagerServices>();
            _sensorStatusManagerServices.FormChangedAction += Sensor_FromChangedActioned;

            _monitorStatusManagerServices = ((App)Application.Current).GetService<IMonitorStatusManagerServices>();
            _stylusGestureServices = ((App)Application.Current).GetService<IStylusGestureServices>();


            if (GlobalConstant.Instance.CurrentMachineType == GlobalConstant.MachineType.SingleDisplayDevice)
            {
                CurrentScreenType = ScreenNameEnum.SingleScreen;
                _stylusGestureServices.LongPressPenUpKey += _stylusGestureServices_LongPressPenUpKey;
            }

            // 初始窗口位置
            var monitor = _monitorStatusManagerServices.GetMonitor(CurrentScreenType);
            WindowMove(monitor);

            // 根据屏幕类型设置初始选项
            if (CurrentScreenType == ScreenNameEnum.ScreenC)
            {
                // 设置C屏默认选中"中文 > 英语"
                //TranslationDirectionComboBox.SelectedIndex = 1;
            }
            // 如果是B屏且不是Tent模式，则隐藏窗口
            if (CurrentScreenType == ScreenNameEnum.ScreenB &&
                _sensorStatusManagerServices.CurrentFormStatus != SENSOR_FORM.FF_TENT)
            {
                this.AppWindow.Hide();
            }
            currentASRresultTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
                ? $"请按住说话"
                : $"Please Holding";
            UpdateStatusText("Ready", Colors.White);
            //this.Activated += OnWindowActivated;
            // 识别完成，停止识别

            TranslationHelper.TranslationAction += OnTranslation;
            TranslationHelper.TranslationEndAction += OnTranslationEnd;
            //_sensorStatusManagerServices.FormChangedAction?.Invoke(SENSOR_FORM.FF_TENT);
        }
        // 在类级别添加一个变量来跟踪最后一次完整翻译
        private string _lastFinalTranslation = string.Empty;
        // 判断是否是最终确认的句子（而非中间结果）
        //bool isFinal = content1.EndsWith("。") || content1.EndsWith(".") || content1.EndsWith("?") || content1.EndsWith("？");

        //        if (isFinal)
        //        {
        //            // 如果是最终句子，更新最后翻译并显示
        //            _lastFinalTranslation = translationResult;
        //            _currentRecognizingTextBlock.Text = _lastFinalTranslation;
        //        }
        //        else
        //        {
        //            // 如果是中间结果，只显示在临时文本块中
        //            _currentRecognizingTextBlock.Text = translationResult;
        //        }

        private async void OnTranslationEnd(ScreenNameEnum senderScerrn, string content1)
        {
            //  TranslationHelper.TranslationEndAction
            if (senderScerrn == CurrentScreenType)
            {
                var _currentTranslatingTextBlock = new TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 20,
                    FontFamily = new FontFamily(_currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Text = CurrentScreenType == ScreenNameEnum.ScreenC
             ? $"我: {content1}"
             : $"I: {content1}"
                };

                //ResultsPanel.ScrollIntoView(_currentTranslatingTextBlock);

                ResultsPanel.Items.Add(_currentTranslatingTextBlock);
                ResultsPanel.ScrollIntoView(_currentTranslatingTextBlock);
                return;
            }
            //如果按下的人是C屏，中到英
            if (senderScerrn == ScreenNameEnum.ScreenC)
            {
                _currentTargetLanguage = "en-US";
            }
            else
            {
                _currentTargetLanguage = "zh-CN";
            }
            //修改源语言和目标语音
            var translationResult = await TranslateText(content1);






            //

            TranslationResultUpdate(translationResult);


            if (senderScerrn == ScreenNameEnum.ScreenC)
            {
                if (App.m_TentwindowB != null) App.m_TentwindowB.currentASRresultTextBlock.Text = "Please Holding";
            }
            else
            {
                if (App.m_TentwindowC != null) App.m_TentwindowC.currentASRresultTextBlock.Text = "请按住说话";
            }



        }
        /// ScreenNameEnum 当前窗口是什么。
        /// ，按照DEMO istarget true 是英文。
        private async void OnTranslation(ScreenNameEnum senderScerrn, string content1)
        {
            //thisScerrn 是ASRwindow 界面对应的窗口，B屏是B ScreenNameEnum ,.C屏是C ScreenNMameEnum
            if (senderScerrn == CurrentScreenType)
            {
                return;
            }
            //如果按下的人是C屏，中到英
            if (senderScerrn == ScreenNameEnum.ScreenC)
            {
                _currentTargetLanguage = "en-US";
            }
            else
            {
                _currentTargetLanguage = "zh-CN";
            }
            //修改源语言和目标语音
            var translationResult = await TranslateText(content1);

            //true 是目标语言，false 是源语言
            //true是要把这个语言翻译到另一台屏幕上




            //
            currentASRresultTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
      ? $"⏳ 翻译中: {translationResult}"
      : $"⏳ Translating: {translationResult}";




        }

        private void TranslationResultUpdate(string translationResult)
        {
            var _currentTranslatingTextBlock = new TextBlock
            {
                Foreground = new SolidColorBrush(Colors.Wheat),
                Margin = new Thickness(0, 0, 0, 12),
                TextWrapping = TextWrapping.Wrap,
                FontSize = 20,
                FontFamily = new FontFamily(_currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                HorizontalAlignment = CurrentScreenType == ScreenNameEnum.ScreenC
                ?HorizontalAlignment.Left
                :HorizontalAlignment.Right,
                Text = CurrentScreenType == ScreenNameEnum.ScreenC
              ? $"{translationResult} :他"
              : $"{translationResult} :He"
            };

            ResultsPanel.Items.Add(_currentTranslatingTextBlock);
            ResultsPanel.ScrollIntoView(_currentTranslatingTextBlock);
        }
        private void _stylusGestureServices_LongPressPenUpKey(object? sender, ushort e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.Activate();
                var monitor = _monitorStatusManagerServices.GetMonitor(CurrentScreenType);
                WindowMove(monitor);

            });

        }

        //private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        //{
        //    this.Activated -= OnWindowActivated;

        //}

        private void InitializeTranslationClient()
        {
            AzureKeyCredential credential = new(translatorKey);
            _textTranslationClient = new TextTranslationClient(credential, translatorRegion);
        }



        private SpeechConfig _speechConfig;
        private AutoDetectSourceLanguageConfig _autoDetectConfig;
        private bool _isInitialized = false;


        private void InitializeSpeechResources()
        {
            if (_isInitialized) return;

            // 1. 创建配置对象
            _speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            _speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");
            _speechConfig.SetProperty(PropertyId.SpeechServiceConnection_LanguageIdMode, "Continuous");

            // 2. 语言配置
            _autoDetectConfig = AutoDetectSourceLanguageConfig.FromLanguages(new string[] { "en-US", "zh-CN" });

            // 3. 延迟创建音频配置（首次使用时才初始化硬件）
            //_audioConfig = null;


            _audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            _recognizer = new SpeechRecognizer(_speechConfig, _autoDetectConfig, _audioConfig);

            // 4. 创建识别器（不带音频配置）
            //_recognizer = new SpeechRecognizer(_speechConfig, _autoDetectConfig);
            _recognizer.Recognizing -= Recognizer_Recognizing;
            _recognizer.Recognized -= Recognizer_Recognized;
            _recognizer.Recognizing += Recognizer_Recognizing;
            _recognizer.Recognized += Recognizer_Recognized;
            _isInitialized = true;
        }


        // 更新当前正在识别的句子（会不断更新）
        private void UpdateCurrentRecognizingText(string text)
        {
            //UpdateCurrentRecognizingText 是ASR 的语音识别返回文本，还没翻译。 ASR识别，支持源语言是中英文。
            //输出是按照语言的，你说的是中文。返回中文。你说的是英文返回英文。
            //B是需要翻译成英文。C屏需要翻译成中文
            //谁ASR识别的，这里就会获取一个。 B C客户端一次只有一个。需要同步。

            currentASRresultTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
              ? $"请按住说话: {text}"
              : $"Please Holding: {text}";

            TranslationHelper.TranslationAction.Invoke(CurrentScreenType, text);

            //  if (_currentRecognizingTextBlock == null)
            //  {
            //      _currentRecognizingTextBlock = new TextBlock
            //      {
            //          Foreground = new SolidColorBrush(Colors.White),
            //          Margin = new Thickness(0, 0, 0, 12),
            //          TextWrapping = TextWrapping.Wrap,
            //          FontSize = 20,
            //          FontFamily = new FontFamily("微软雅黑"),
            //          HorizontalAlignment = HorizontalAlignment.Left
            //      };
            //      ResultsPanel.Children.Add(_currentRecognizingTextBlock);
            //  }
            //  _currentRecognizingTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
            //      ? $"请按住说话: {text}"
            //      : $"Please hold your speech: {text}";
        }

        // 更新当前正在翻译的句子（会不断更新）
        //private async void UpdateCurrentTranslation(string text)
        //{
        //    //text 语音识别结果

        //    //这是我的目标语言。但是对于另一个程序来说，这是源语言。
        //    // true 是目标语言，false 是源语言
        //    TranslationHelper.TranslationAction.Invoke(CurrentScreenType, false, text);

        //}


        private async Task<string> TranslateText(string text)
        {
            try
            {
                // 将单个字符串包装为包含一个元素的列表
                var content = new List<string> { text };

                //todo：源语言可为空 ，目标语言需要指定
                Response<IReadOnlyList<TranslatedTextItem>> response = await _textTranslationClient.TranslateAsync(
                    targetLanguage: _currentTargetLanguage,
                    content: content);  // 现在传递的是List<string>

                if (response.Value != null && response.Value.Count > 0)
                {
                    return response.Value[0].Translations[0].Text;
                }
            }
            catch (Exception ex)
            {
                AddResultText($"[{DateTime.Now:HH:mm:ss}] 翻译错误: {ex.Message}", Colors.OrangeRed);
                throw;
            }

            return string.Empty;
        }


        private void AddResultText(string text, Color color, double marginBottom = 6, double fontSize = 16)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 0, marginBottom),
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                FontFamily = new FontFamily(_currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Left,
                LineHeight = fontSize * 1.5
            };

            ResultsPanel.Items.Add(textBlock);
            ResultsPanel.ScrollIntoView(textBlock);
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            // 停止识别
            if (_isMicActive)
            {
                await _recognizer.StopContinuousRecognitionAsync();
            }

            // 释放资源
            _recognizer?.Dispose();
            _audioConfig?.Dispose();

            // 重置状态
            _isMicActive = false;
            _isInitialized = false;

            // 关闭窗口
            this.Close();
        }

        #region DargWindow
        private OverlappedPresenter GetPresenter()
        {
            return AppWindow.Presenter as OverlappedPresenter;
        }

        private PointInt32 _dragStartPoint;
        private double _dpiScale = 2;
        private double _lastNormalWindowWidth = 0;
        private Point _initialScreenPosition;
        private double _targetRatio;
        private bool _isMaximizedBeforeDrag;

        public void DragArea_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            var presenter = GetPresenter();
            if (presenter == null) return;

            TitleBar.CapturePointer(e.Pointer);
            var point = e.GetCurrentPoint(TitleBar);

            _isDragging = true;
            _isMaximizedBeforeDrag = (presenter.State == OverlappedPresenterState.Maximized);

            if (_isMaximizedBeforeDrag)
            {
                var screenPoint = e.GetCurrentPoint(null);
                _initialScreenPosition = new Point(screenPoint.Position.X, screenPoint.Position.Y);
                _targetRatio = point.Position.X / TitleBar.ActualWidth;
            }

            _dragStartPoint = new PointInt32((int)point.Position.X, (int)point.Position.Y);
        }

        public void DragArea_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (!_isDragging) return;

            var presenter = GetPresenter();
            if (presenter == null) return;

            var currentScreenPoint = e.GetCurrentPoint(null);
            var currentPosition = new Point(currentScreenPoint.Position.X, currentScreenPoint.Position.Y);

            if (_isMaximizedBeforeDrag)
            {
                var delta = new Point(
                    (currentPosition.X - _initialScreenPosition.X) * _dpiScale,
                    (currentPosition.Y - _initialScreenPosition.Y) * _dpiScale
                );

                if (Math.Abs(delta.X) < 5 && Math.Abs(delta.Y) < 5) return;

                presenter.Restore();

                var screenX = _initialScreenPosition.X * _dpiScale;
                var screenY = _initialScreenPosition.Y * _dpiScale;
                var windowWidth = _lastNormalWindowWidth > 0 ? _lastNormalWindowWidth : AppWindow.Size.Width;

                AppWindow.Move(new PointInt32(
                    (int)(screenX - windowWidth * _targetRatio),
                    (int)(screenY - _dragStartPoint.Y)
                ));

                var newLocalPoint = e.GetCurrentPoint(TitleBar).Position;
                _dragStartPoint = new PointInt32((int)newLocalPoint.X, (int)newLocalPoint.Y);
                _isMaximizedBeforeDrag = false;
            }

            var localPoint = e.GetCurrentPoint(TitleBar);
            var offsetX = localPoint.Position.X - _dragStartPoint.X;
            var offsetY = localPoint.Position.Y - _dragStartPoint.Y;

            AppWindow.Move(new PointInt32(
                AppWindow.Position.X + (int)offsetX,
                AppWindow.Position.Y + (int)offsetY
            ));
        }

        public void DragArea_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            _isDragging = false;
            _isMaximizedBeforeDrag = false;
            TitleBar.ReleasePointerCapture(e.Pointer);
        }
        #endregion

        #region 窗口缩放

        //private void GridFirst_PointerPressed(object sender, PointerRoutedEventArgs e)
        //{
        //    if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
        //    {
        //        // 获取窗口句柄
        //        var hWnd = WindowNative.GetWindowHandle(this);

        //        // 发送 WM_NCLBUTTONDOWN 消息（HTCAPTION = 2）
        //        Win32Interop.SendMessage(hWnd, 0x00A1 /* WM_NCLBUTTONDOWN */, (IntPtr)2 /* HTCAPTION */, IntPtr.Zero);
        //    }
        //}


        // 窗口缩放（右下角 Thumb）
        private void ResizeThumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            // 调整窗口大小
            var newWidth = (int)(this.AppWindow.Size.Width + e.HorizontalChange);
            var newHeight = (int)(this.AppWindow.Size.Height + e.VerticalChange);

            // 限制最小大小
            newWidth = Math.Max(newWidth, 200);
            newHeight = Math.Max(newHeight, 100);

            this.AppWindow.Resize(new SizeInt32(newWidth, newHeight));
        }

        #endregion

        #region Tent模式
        private void Sensor_FromChangedActioned(SENSOR_FORM fORM)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                var monitor = _monitorStatusManagerServices.GetMonitor(CurrentScreenType);

                if (fORM == SENSOR_FORM.FF_TENT)
                {
                    // Tent模式下显示窗口并定位
                    this.AppWindow.Show();
                    WindowMove(monitor);
                }
                else if (CurrentScreenType == ScreenNameEnum.ScreenB)
                {
                    // 非Tent模式下隐藏B屏窗口
                    this.AppWindow.Hide();
                }
            });


        }

        private void WindowMove(Monitor monitor)
        {
            // 计算窗口应该放置的顶部位置（显示器底部减去窗口高度和任务栏高度）
            //var targetTop = monitor.WindowRect.Bottom - this.AppWindow.Size.Height - 80;

            // 计算窗口应该放置的顶部位置（中间）
            var targetTop = monitor.WindowRect.Top + (monitor.WindowRect.Height - this.AppWindow.Size.Height) / 2;

            // 计算窗口的水平居中位置
            var targetLeft = monitor.WindowRect.Left + (monitor.WindowRect.Width - this.AppWindow.Size.Width) / 2;

            // 确保窗口不会超出屏幕顶部
            targetTop = Math.Max(monitor.WindowRect.Top, targetTop);

            // 移动窗口到目标位置
            this.AppWindow.Move(new PointInt32((int)targetLeft, (int)targetTop));

            // 确保窗口保持置顶
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsAlwaysOnTop = true;
            }

            // 激活窗口
            this.Activate();
        }


        #endregion

        #region 新加按钮
        private bool _isRecording = false;
        private bool _isRecognizing = false;

        // 修改按钮事件处理
        private async void ChineseToEnglishButton_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (!_isMicActive)
            {
                await _recognizer.StartContinuousRecognitionAsync();
                _isMicActive = true;

                // 更新UI
                currentASRresultTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
                ? $"按住说话中："
                : $"Please Holding：";

                ChineseToEnglishButton.Background = new SolidColorBrush(Colors.LightBlue);
                ChineseToEnglishButton.Content = "Speaking...";
                UpdateStatusText("Recording...", Colors.Lime);
            }
        }

        private async void Button_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isMicActive)
            {
                await _recognizer.StopContinuousRecognitionAsync();
                _isMicActive = false;
           


                // 更新UI

                currentASRresultTextBlock.Text = CurrentScreenType == ScreenNameEnum.ScreenC
            ? $"请按住说话"
            : $"Please Holding";
              
                ChineseToEnglishButton.Content = "Start";
                ChineseToEnglishButton.Background = new SolidColorBrush(Color.FromArgb(255, 51, 51, 51));
                UpdateStatusText("Ready", Colors.White);



            }
        }

        private void Recognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizingSpeech)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
               
                    UpdateCurrentRecognizingText(e.Result.Text);   
                });
            }
        }

        private void Recognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (e.Result.Reason == ResultReason.RecognizedSpeech)
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (e.Result.Text != "")
                        TranslationHelper.TranslationEndAction?.Invoke(CurrentScreenType, e.Result.Text);
                });
            }
        }

        private void UpdateStatusText(string text, Color color)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                StatusTextBlock.Text = text;
                StatusTextBlock.Foreground = new SolidColorBrush(color);
            });
        }

        #endregion

    }
}
