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
        private readonly ASRService _asrService = new();
        private AudioRecorder _recorder;
        private bool _isRecording = false;

        public ASRWindow()
        {
            this.InitializeComponent();
            _asrWindowVm = new ASRWindowVm();

            // 设置窗口大小
            this.SetWindowSize(_MainGrid_.Width, _MainGrid_.Height);

            // 禁用窗口边框
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);

            // 禁用窗口调整大小
            this.SetIsResizable(false);

            // 隐藏最小化、最大化和关闭按钮
            SetWindowButtonsVisibility(false, false, false);

            InitializeRecorder();
            UpdateButtonStates();

        }
        
        private void SetWindowButtonsVisibility(bool minimize, bool maximize, bool close)
        {
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
            if (appWindow != null)
            {
                var titleBar = appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                titleBar.PreferredHeightOption = TitleBarHeightOption.Collapsed;
                titleBar.ButtonBackgroundColor = Colors.Transparent;
            }
        }

        private void InitializeRecorder()
        {
            _recorder = new AudioRecorder(_asrService);
            _asrService.TextRecognized += text =>
                DispatcherQueue.TryEnqueue(() => txtResult.Text += text + " ");
        }

        private async void StartRecording(object sender, RoutedEventArgs e)
        {
            try
            {
                _isRecording = true;
                UpdateButtonStates();

                await _asrService.ConnectAsync();
                _recorder.StartRecording();
            }
            catch (Exception ex)
            {
                _isRecording = false;
                UpdateButtonStates();
                txtResult.Text += $"\n[错误] {ex.Message}";
            }
        }

        private void StopRecording(object sender, RoutedEventArgs e)
        {
            _isRecording = false;
            UpdateButtonStates();

            _recorder.StopRecording();
            _asrService.Dispose();
        }

        private void UpdateButtonStates()
        {
            btnStart.Visibility = _isRecording ? Visibility.Collapsed : Visibility.Visible;
            btnStop.Visibility = _isRecording ? Visibility.Visible : Visibility.Collapsed;
            btnStart.IsEnabled = !_isRecording;
            btnStop.IsEnabled = _isRecording;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CleanupResources();
            this.Close();
        }

        private void CleanupResources()
        {
            if (_isRecording)
            {
                _recorder?.StopRecording();
            }
            _recorder?.Dispose();
            _asrService?.Dispose();
        }


    }
}
