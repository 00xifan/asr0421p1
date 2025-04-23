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

        public ASRWindow()
        {
            this.InitializeComponent();
            _asrWindowVm = new ASRWindowVm();

            InitializeSpeechRecognizer();

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
        private void InitializeSpeechRecognizer()
        {
            var speechConfig = SpeechConfig.FromSubscription(speechKey, speechRegion);
            speechConfig.SpeechRecognitionLanguage = "zh-CN";

            var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            recognizer = new SpeechRecognizer(speechConfig, audioConfig);

            // 订阅识别事件
            recognizer.SessionStarted += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    StatusTextBlock.Text = "正在识别..."; // 录音中状态
                });
            };

            recognizer.Recognizing += (s, e) =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    // 当有新的语音输入时，重置为"正在识别..."状态
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
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        AddCompleteRecognitionResult(e.Result.Text);
                        StatusTextBlock.Text = "识别成功"; // 识别成功状态
                    });
                }
            };
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isRecognizing)
            {
                try
                {
                    ResultsPanel.Children.Clear();
                    StatusTextBlock.Text = "开始识别"; // 开始识别状态

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
        private void AddCompleteRecognitionResult(string text)
        {
            // 清除初始提示文本（如果存在）
            if (ResultsPanel.Children.Count == 1 &&
                ResultsPanel.Children[0] is RichTextBlock rtb &&
                rtb.Blocks.FirstOrDefault() is Paragraph p &&
                p.Inlines.FirstOrDefault() is Run run &&
                run.Text == "识别结果将显示在这里...")
            {
                ResultsPanel.Children.Clear();
            }

            // 创建新的结果条目
            var resultEntry = new TextBlock
            {
                Text = $"[{DateTime.Now:HH:mm:ss}] {text}",
                Foreground = new SolidColorBrush(Colors.White),
                Margin = new Thickness(0, 0, 0, 10),
                TextWrapping = TextWrapping.Wrap
            };

            // 添加到结果面板
            ResultsPanel.Children.Add(resultEntry);
            _recognitionResults.Add(text);
            _currentResultIndex = _recognitionResults.Count;

            // 自动滚动到底部
            var scrollViewer = (ScrollViewer)ResultsPanel.Parent;
            scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
        }

      

        private async void StopButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecognizing)
            {
                await StopRecognition();
            }
        }

        private async void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (isRecognizing)
            {
                await StopRecognition();
            }
            recognizer.Dispose();

            this.Close();
        }


    }
}
