using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using asr0421p1.Services;
using WinUIEx;
using Microsoft.UI.Windowing;
using Microsoft.UI;
using WinRT.Interop;
using Windows.Media.SpeechRecognition;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using SpeechRecognizer = Microsoft.CognitiveServices.Speech.SpeechRecognizer;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Documents;
using Azure.AI.Translation.Text;
using Azure;
using Windows.UI;
using Windows.UI.Text;

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
        private AppWindow appWindow;
        private readonly ASRService _asrService = new();
        private AudioRecorder _recorder;

        private bool _isRecording = false;

        private Microsoft.CognitiveServices.Speech.SpeechRecognizer recognizer;
        private bool isRecognizing = false;
        private readonly List<string> _recognitionResults = new();
        private int _currentResultIndex = 0;

        //  Azure 语音服务密钥和区域
        private const string speechKey = "9bip1SRtixiGbvLlIrD77AHlG02Z2NBCnUkpLvmF4OankckyqGt1JQQJ99BDACYeBjFXJ3w3AAAYACOGGLSi";
        private const string speechRegion = "eastus";

        // 翻译
        private const string translatorKey = "Dx7cnF12NpSHrf0eLYYRHcbhNEvPWTJPRLe8Ft0fZ6dF7fRHonX3JQQJ99BDACYeBjFXJ3w3AAAbACOGfMGj";
        private const string translatorEndpoint = "https://yanboasr.cognitiveservices.azure.com/";
        private const string translatorRegion = "eastus"; // 你的服务区域
        
        private TextTranslationClient _textTranslationClient;
        private string _currentSourceLanguage = "zh-CN";
        private string _currentTargetLanguage = "en-US";
        private bool _translationEnabled = false;

        public ASRWindow()
        {
            this.InitializeComponent();
            _asrWindowVm = new ASRWindowVm();

            InitializeSpeechRecognizer();

            InitializeTranslationClient();
            TranslationDirectionComboBox.SelectionChanged += TranslationDirectionComboBox_SelectionChanged;

            // 设置窗口大小
            //this.SetWindowSize(_MainGrid_.Width, _MainGrid_.Height);

            //// 禁用窗口边框
            //this.ExtendsContentIntoTitleBar = true;
            //this.SetTitleBar(null);

            //// 禁用窗口调整大小
            //this.SetIsResizable(true);

            //// 隐藏最小化、最大化和关闭按钮
            //SetWindowButtonsVisibility(false, false, false);

            //InitializeRecorder();
            //UpdateButtonStates();

        }
        private void InitializeTranslationClient()
        {
            AzureKeyCredential credential = new(translatorKey);
            _textTranslationClient = new TextTranslationClient(credential, translatorRegion);
        }

        private void TranslationDirectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
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

                        // 更新语音识别语言
                        if (recognizer != null)
                        {
                            recognizer.Dispose();
                            InitializeSpeechRecognizer();
                        }
                    }
                }
            }
        }

        private void InitializeSpeechRecognizer()
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = _currentSourceLanguage;

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // 订阅识别事件
            recognizer.SessionStarted += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() => StatusTextBlock.Text = "正在识别...");
            };

            recognizer.Recognizing += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    if (StatusTextBlock.Text == "识别成功")
                    {
                        StatusTextBlock.Text = "正在识别...";
                    }
                });
            };

            recognizer.Recognized += (s, e) =>
            {
                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                {
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        await ProcessRecognitionResult(e.Result.Text);
                        StatusTextBlock.Text = "识别成功";
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
            AddResultText($"[{DateTime.Now:HH:mm:ss}] {text}", Colors.White);

            // 如果需要翻译
            if (_translationEnabled && !string.IsNullOrWhiteSpace(text))
            {
                try
                {
                    var translationResult = await TranslateText(text);
                    if (!string.IsNullOrEmpty(translationResult))
                    {
                        AddResultText($"[{DateTime.Now:HH:mm:ss}] [翻译] {translationResult}", Colors.LightGray, true);
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
        private void AddResultText(string text, Color color, bool isItalic = false)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                Foreground = new SolidColorBrush(color),
                Margin = new Thickness(0, 0, 0, 5),
                TextWrapping = TextWrapping.Wrap,
                FontStyle = isItalic ? FontStyle.Italic : FontStyle.Normal
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
                Console.WriteLine($"翻译错误: {ex.Message}");
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
                    ResultsPanel.Children.Clear();
                    StatusTextBlock.Text = "开始识别";

                    await recognizer.StartContinuousRecognitionAsync();
                    isRecognizing = true;
                    StartButton.Visibility = Visibility.Collapsed;
                    StopButton.Visibility = Visibility.Visible;
                }
                catch (Exception ex)
                {
                    StatusTextBlock.Text = $"识别失败: {ex.Message}";
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
                StatusTextBlock.Text = "识别结束"; // 停止识别状态
            }
            catch (Exception ex)
            {
                StatusTextBlock.Text = $"停止失败: {ex.Message}";
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


    }
}
