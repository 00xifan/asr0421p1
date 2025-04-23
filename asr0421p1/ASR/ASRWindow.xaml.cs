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
using Microsoft.UI.Windowing;

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

        public ASRWindow()
        {
            this.InitializeComponent();
            _asrWindowVm = new ASRWindowVm();

            
            this.ExtendsContentIntoTitleBar = true;
            this.SetTitleBar(null);


            InitializeRecorder();
            UpdateButtonStates();

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
