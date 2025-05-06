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

        private SpeechRecognizer recognizer;
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
        private string _currentSourceLanguage = "zh-CN";
        private string _currentTargetLanguage = "en-US";
        private bool _translationEnabled = false;
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
            this.AppWindow.Resize(new SizeInt32(2220, 440));

            // 绑定拖动事件
            // 绑定拖动事件到 _GridFirst_
            //_GridFirst_.PointerPressed += GridFirst_PointerPressed;

            UpdateLanguageSettingsFromComboBox();

            InitializeSpeechRecognizer();
            InitializeTranslationClient();

            TranslationDirectionComboBox.SelectionChanged += TranslationDirectionComboBox_SelectionChanged;
            _sensorStatusManagerServices = ((App)Application.Current).GetService<ISensorStatusManagerServices>();
            _sensorStatusManagerServices.FormChangedAction += Sensor_FromChangedActioned;
            // var status = _sensorStatusManagerServices.CurrentFormStatus;
            _monitorStatusManagerServices = ((App)Application.Current).GetService<IMonitorStatusManagerServices>();
            _stylusGestureServices = ((App)Application.Current).GetService<IStylusGestureServices>();
            var monitor = _monitorStatusManagerServices.GetMonitor(CurrentScreenType);

            if (GlobalConstant.Instance.CurrentMachineType == GlobalConstant.MachineType.SingleDisplayDevice)
            {
                CurrentScreenType = ScreenNameEnum.SingleScreen;
                monitor = _monitorStatusManagerServices.GetMainMonitor();
                _stylusGestureServices.LongPressPenUpKey += _stylusGestureServices_LongPressPenUpKey;
            }

            // 初始窗口位置

            WindowMove(monitor);

            // 根据屏幕类型设置初始选项
            if (CurrentScreenType == ScreenNameEnum.ScreenC)
            {
                // 设置C屏默认选中"中文 > 英语"
                TranslationDirectionComboBox.SelectedIndex = 1;
            }
            // 如果是C屏且不是Tent模式，则隐藏窗口
            if (CurrentScreenType == ScreenNameEnum.ScreenC &&
                _sensorStatusManagerServices.CurrentFormStatus != SENSOR_FORM.FF_TENT)
            {
                this.AppWindow.Hide();
            }
            this.AppWindow.Hide();
            this.Activated += OnWindowActivated;


        }

        private void _stylusGestureServices_LongPressPenUpKey(object? sender, ushort e)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                this.Activate();
               var monitor = _monitorStatusManagerServices.GetMainMonitor();
                WindowMove(monitor);

            });

        }

        private void OnWindowActivated(object sender, WindowActivatedEventArgs args)
        {
            this.Activated -= OnWindowActivated;

            // 模拟点击开始按钮
            StartButton_Click(null, null);
        }

        private void UpdateLanguageSettingsFromComboBox()
        {
            if (TranslationDirectionComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                var direction = selectedItem.DataContext as string;
                if (!string.IsNullOrEmpty(direction))
                {
                    var languages = direction.Split('>');
                    if (languages.Length == 2)
                    {
                        _currentSourceLanguage = languages[0];
                        _currentTargetLanguage = languages[1];
                        _translationEnabled = _currentSourceLanguage != _currentTargetLanguage;
                    }
                }
            }
        }
        private void InitializeTranslationClient()
        {
            AzureKeyCredential credential = new(translatorKey);
            _textTranslationClient = new TextTranslationClient(credential, translatorRegion);
        }

        private void TranslationDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateLanguageSettingsFromComboBox();

            // 更新语音识别语言
            if (recognizer != null)
            {
                recognizer.Dispose();
                InitializeSpeechRecognizer();
            }
        }


        // 当前正在识别的句子和翻译的临时控件
        private TextBlock _currentRecognizingTextBlock = null;
        private TextBlock _currentTranslatingTextBlock = null;

        private void InitializeSpeechRecognizer()
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = _currentSourceLanguage;
            speechConfig.SetProperty(PropertyId.SpeechServiceResponse_PostProcessingOption, "TrueText");

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            recognizer.Recognizing += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizingSpeech)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        // 更新当前正在识别的句子
                        UpdateCurrentRecognizingText(e.Result.Text);

                        // 如果需要实时翻译
                        if (_translationEnabled)
                        {
                            _ = UpdateCurrentTranslation(e.Result.Text);
                        }
                    });
                }
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        //// 固定显示最终识别结果
                        //AddFinalRecognizedText($"[{DateTime.Now:HH:mm:ss}] 识别结果: {e.Result.Text}");

                        //// 固定显示最终翻译结果
                        //if (_translationEnabled)
                        //{
                        //    _ = AddFinalTranslation(e.Result.Text);
                        //}

                        // 清空当前识别和翻译的临时控件

                        _currentRecognizingTextBlock = null;
                        _currentTranslatingTextBlock = null;
                    });
                }
            };
        }

        // 更新当前正在识别的句子（会不断更新）
        private void UpdateCurrentRecognizingText(string text)
        {
            if (_currentRecognizingTextBlock == null)
            {
                _currentRecognizingTextBlock = new TextBlock
                {
                    Foreground = new SolidColorBrush(Colors.White),
                    Margin = new Thickness(0, 0, 0, 12),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 24,
                    FontFamily = new FontFamily(_currentSourceLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Center
                };
                ResultsPanel.Children.Add(_currentRecognizingTextBlock);
            }
            _currentRecognizingTextBlock.Text = text;
            ScrollToBottom();
        }

        // 更新当前正在翻译的句子（会不断更新）
        private async Task UpdateCurrentTranslation(string text)
        {
            try
            {
                var translationResult = await TranslateText(text);
                if (!string.IsNullOrEmpty(translationResult))
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        if (_currentTranslatingTextBlock == null)
                        {
                            _currentTranslatingTextBlock = new TextBlock
                            {
                                Foreground = new SolidColorBrush(Color.FromArgb(255, 56, 164, 255)),
                                Margin = new Thickness(0, 0, 0, 36),
                                TextWrapping = TextWrapping.Wrap,
                                FontSize = 24,
                                FontFamily = new FontFamily(_currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                                HorizontalAlignment = HorizontalAlignment.Center
                            };
                            ResultsPanel.Children.Add(_currentTranslatingTextBlock);
                        }
                        _currentTranslatingTextBlock.Text = translationResult;
                        ScrollToBottom();
                    });
                }
            }
            catch (Exception ex)
            {
                AddResultText($"[{DateTime.Now:HH:mm:ss}] 实时翻译错误: {ex.Message}", Colors.OrangeRed);
            }
        }


        private void AddRecognizingText(string text)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var textBlock = new TextBlock
                {
                    Text = text,
                    Foreground = new SolidColorBrush(Colors.LightGray),
                    Margin = new Thickness(0, 0, 0, 6),
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 14,
                    FontFamily = new FontFamily(_currentSourceLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI"),
                    HorizontalAlignment = HorizontalAlignment.Left
                };

                ResultsPanel.Children.Add(textBlock);
                ScrollToBottom();
            });
        }



      
        private async Task<string> TranslateText(string text)
        {
            try
            {
                // 将单个字符串包装为包含一个元素的列表
                var content = new List<string> { text };

                Response<IReadOnlyList<TranslatedTextItem>> response = await _textTranslationClient.TranslateAsync(
                    targetLanguage: _currentTargetLanguage,
                    content: content,  // 现在传递的是List<string>
                    sourceLanguage: _currentSourceLanguage);

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

        private void ScrollToBottom()
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                var scrollViewer = ResultsPanel.Parent as ScrollViewer;
                if (scrollViewer != null)
                {
                    // 使用 ChangeView 并等待布局更新完成
                    scrollViewer.UpdateLayout();
                    scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null, true);
                }
            });
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
                FontFamily = new FontFamily(color == Color.FromArgb(255, 56, 164, 255) ?
                                 (_currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI") :
                                 (_currentSourceLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI")),
                HorizontalAlignment = HorizontalAlignment.Left,
                LineHeight = fontSize * 1.5
            };

            ResultsPanel.Children.Add(textBlock);
            ScrollToBottom();
        }
        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecognizing)
            {
                try
                {
                    UpdateLanguageSettingsFromComboBox();

                    if (recognizer != null)
                    {
                        recognizer.Dispose();
                        InitializeSpeechRecognizer();
                    }

                    // 重置实时文本块
                    _currentRecognizingTextBlock = null;
                    _currentTranslatingTextBlock = null;
                    
              
                    await recognizer.StartContinuousRecognitionAsync();

                    isRecognizing = true;
                    ResultsPanel.Children.Clear();
                    StartButton.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    AddResultText($"[{DateTime.Now:HH:mm:ss}] 识别失败: {ex.Message}", Colors.OrangeRed);
                }
            }
        }

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecognizing)
            {
                await StopRecognition();
            }
        }

        private async Task StopRecognition()
        {
            try
            {
                await recognizer.StopContinuousRecognitionAsync();
                isRecognizing = false;
                StartButton.Visibility = Visibility.Visible;
                StopButton.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                AddResultText($"[{DateTime.Now:HH:mm:ss}] 停止失败: {ex.Message}", Colors.OrangeRed);
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecognizing)
            {
                await StopRecognition();
            }
            recognizer?.Dispose();
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

        #region 窗口缩放（右下角 Thumb）

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
                else if (CurrentScreenType == ScreenNameEnum.ScreenC)
                {
                    // 非Tent模式下隐藏C屏窗口
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

    }
}
