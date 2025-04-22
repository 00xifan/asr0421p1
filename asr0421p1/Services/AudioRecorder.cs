using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace asr0421p1.Services;
public class AudioRecorder : IDisposable
{
    private WaveInEvent _waveIn;
    private readonly ASRService _asrService;

    public AudioRecorder(ASRService asrService)
    {
        _asrService = asrService;
        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(16000, 16, 1), // 16kHz, 16-bit, 单声道
            BufferMilliseconds = 200 // 每200ms触发一次事件
        };
        _waveIn.DataAvailable += OnAudioDataAvailable;
    }

    private async void OnAudioDataAvailable(object sender, WaveInEventArgs e)
    {
        byte[] pcmChunk = new byte[e.BytesRecorded];
        Array.Copy(e.Buffer, pcmChunk, e.BytesRecorded);
        await _asrService.SendAudioAsync(pcmChunk);
    }

    public void StartRecording() => _waveIn.StartRecording();
    public void StopRecording() => _waveIn.StopRecording();
    public void Dispose() => _waveIn?.Dispose();
}