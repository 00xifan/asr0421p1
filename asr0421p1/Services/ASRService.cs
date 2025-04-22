using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace asr0421p1.Services;

public class ASRService : IDisposable
{
    private ClientWebSocket _ws;
    private readonly string _appKey = "7212290171";
    private readonly string _accessKey = "vG4MmOoYQncnMNY7QOd4MviduZAOColJ";
    private readonly CancellationTokenSource _cts = new();

    public event Action<string> TextRecognized;

    public async Task ConnectAsync()
    {
        _ws = new ClientWebSocket();
        _ws.Options.SetRequestHeader("X-Api-App-Key", _appKey);
        _ws.Options.SetRequestHeader("X-Api-Access-Key", _accessKey);
        _ws.Options.SetRequestHeader("X-Api-Resource-Id", "volc.bigasr.sauc.duration");
        _ws.Options.SetRequestHeader("X-Api-Connect-Id", "67ee89ba-7050-4c04-a3d7-ac61a63499b1");

        await _ws.ConnectAsync(
            new Uri("wss://openspeech.bytedance.com/api/v3/sauc/bigmodel"),
            _cts.Token);

        await SendInitialRequestAsync();
        _ = Task.Run(ReceiveMessagesAsync);
    }

    private async Task SendInitialRequestAsync()
    {
        var request = new
        {
            audio = new { format = "pcm", rate = 16000, channel = 1 },
            request = new { model_name = "bigmodel", enable_punc = true }
        };
        var json = JsonConvert.SerializeObject(request);
        await _ws.SendAsync(
            Encoding.UTF8.GetBytes(json),
            WebSocketMessageType.Text,
            true,
            _cts.Token);
    }

    public async Task SendAudioAsync(byte[] pcmData)
    {
        if (_ws?.State == WebSocketState.Open)
        {
            await _ws.SendAsync(
                new ArraySegment<byte>(pcmData),
                WebSocketMessageType.Binary,
                true,
                _cts.Token);
        }
    }

    private async Task ReceiveMessagesAsync()
    {
        var buffer = new byte[4096];
        while (_ws?.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
            if (result.MessageType == WebSocketMessageType.Text)
            {
                var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                var response = JsonConvert.DeserializeObject<dynamic>(json);
                TextRecognized?.Invoke(response?.result?.text?.ToString());
            }
        }
    }

    public void Dispose()
    {
        _ws?.Dispose();
        _cts.Cancel();
    }
}