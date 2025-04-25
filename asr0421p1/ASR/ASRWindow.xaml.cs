using asr0421p1.Services;
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
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics;
using Windows.UI;
using Windows.UI.Text;
using WinRT.Interop;
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
        private const string speechKey = "9bip1SRtixiGbvLlIrD77AHlG02Z2NBCnUkpLvmF4OankckyqGt1JQQJ99BDACYeBjFXJ3w3AAAYACOGGLSi";
        private const string speechRegion = "eastus";

        // 翻译
        private const string translatorKey = "Dx7cnF12NpSHrf0eLYYRHcbhNEvPWTJPRLe8Ft0fZ6dF7fRHonX3JQQJ99BDACYeBjFXJ3w3AAAbACOGfMGj";
        private const string translatorEndpoint = "https://yanboasr.cognitiveservices.azure.com/";
        private const string translatorRegion = "eastus";

        private TextTranslationClient _textTranslationClient;
        private string _currentSourceLanguage = "zh-CN";
        private string _currentTargetLanguage = "en-US";
        private bool _translationEnabled = false;

        public ASRWindow()
        {
            this.InitializeComponent();
            
            this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            this.AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
            

            // 设置窗口初始大小
            this.AppWindow.Resize(new SizeInt32(820, 340));

            // 绑定拖动事件
            // 绑定拖动事件到 _GridFirst_
            //_GridFirst_.PointerPressed += GridFirst_PointerPressed;

            UpdateLanguageSettingsFromComboBox();

            InitializeSpeechRecognizer();
            InitializeTranslationClient();

            TranslationDirectionComboBox.SelectionChanged += TranslationDirectionComboBox_SelectionChanged;

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

        private void InitializeSpeechRecognizer()
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = _currentSourceLanguage;

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // 订阅识别事件
            //recognizer.SessionStarted += (s, e) =>
            //{
            //    DispatcherQueue.TryEnqueue(() => StatusTextBlock.Text = "正在识别...");
            //};

            //recognizer.Recognizing += (s, e) =>
            //{
            //    DispatcherQueue.TryEnqueue(() =>
            //    {
            //        if (StatusTextBlock.Text == "识别成功")
            //        {
            //            StatusTextBlock.Text = "正在识别...";
            //        }
            //    });
            //};

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        await ProcessRecognitionResult(e.Result.Text);
                        //StatusTextBlock.Text = "识别成功";
                    });
                }
            };
        }

        private async Task ProcessRecognitionResult(string text)
        {
            // 清除初始提示文本
            if (ResultsPanel.Children.Count == 1 &&
                ResultsPanel.Children[0] is RichTextBlock rtb &&
                rtb.Blocks.FirstOrDefault() is Paragraph p &&
                p.Inlines.FirstOrDefault() is Run run &&
                run.Text == "识别结果将显示在这里...")
            {
                ResultsPanel.Children.Clear();
            }

            // 添加原始识别结果
            AddResultText(text, Colors.White, 6,24);

            // 如果需要翻译
            if (_translationEnabled && !string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    var translationResult = await TranslateText(text);
                    if (!string.IsNullOrEmpty(translationResult))
                    {
                        AddResultText(translationResult, Color.FromArgb(255, 56, 164, 255), 30,24);
                    }
                }
                catch (Exception ex)
                {
                    AddResultText($"[{DateTime.Now:HH:mm:ss}] 翻译错误: {ex.Message}", Colors.OrangeRed);
                }
            }

            _recognitionResults.Add(text);
            ScrollToBottom();
        }
        private void AddResultText(string text, Color color, double marginBottom = 6, double fontSize = 16)
        {
            // 根据当前源语言选择字体
            string fontFamily = _currentSourceLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI";

            // 如果是翻译结果（蓝色文本），使用目标语言选择字体
            if (color == Colors.Blue)
            {
                fontFamily = _currentTargetLanguage.StartsWith("zh") ? "微软雅黑" : "Segoe UI";
            }

            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 0, marginBottom),
                TextWrapping = TextWrapping.Wrap,
                FontSize = fontSize,
                FontFamily = new FontFamily(fontFamily),
                HorizontalAlignment = HorizontalAlignment.Center,
                LineHeight = fontSize * 1.5 // 设置行高为字体大小的1.5倍
            };

            ResultsPanel.Children.Add(textBlock);
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
            var scrollViewer = (ScrollViewer)ResultsPanel.Parent;
            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
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
                    
                    ResultsPanel.Children.Clear();
                    // StatusTextBlock.Text = "开始识别";

                    await recognizer.StartContinuousRecognitionAsync();
                    isRecognizing = true;
                    StartButton.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    //StatusTextBlock.Text = $"识别失败: {ex.Message}";
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
                //StatusTextBlock.Text = "识别结束"; // 停止识别状态
            }
            catch (Exception ex)
            {
                //StatusTextBlock.Text = $"停止失败: {ex.Message}";
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

    }
}
