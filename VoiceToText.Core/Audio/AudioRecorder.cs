using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NAudio.Wave;
using VoiceToText.Core.Services;

namespace VoiceToText.Core.Audio;

public sealed class AudioRecorder : IDisposable
{
    private readonly int _sampleRate;
    private readonly int _channels;
    private readonly WaveInEvent _waveIn;
    private readonly ConcurrentQueue<float> _buffer = new();
    private bool _isRecording;

    public AudioRecorder(int sampleRate, int channels)
    {
        _sampleRate = sampleRate;
        _channels = channels;

        _waveIn = new WaveInEvent
        {
            WaveFormat = new WaveFormat(sampleRate, channels),
            BufferMilliseconds = 50
        };
        _waveIn.DataAvailable += OnDataAvailable;
    }

    private void OnDataAvailable(object? sender, WaveInEventArgs e)
    {
        if (!_isRecording)
        {
            return;
        }

        var samplesBefore = _buffer.Count;
        for (var index = 0; index < e.BytesRecorded; index += sizeof(short))
        {
            var sample = BitConverter.ToInt16(e.Buffer, index);
            _buffer.Enqueue(sample / 32768f);
        }
        var samplesAdded = _buffer.Count - samplesBefore;

    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_isRecording)
        {
            Logger.Warn("Recording is already active, ignoring start request");
            return Task.CompletedTask;
        }

        ClearBuffer();
        _waveIn.StartRecording();
        _isRecording = true;
        return Task.CompletedTask;
    }

    private void ClearBuffer()
    {
        while (_buffer.TryDequeue(out _))
        {
        }
    }

    public async Task<float[]> StopAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRecording)
        {
            Logger.Warn("Recording is not active, nothing to stop");
            return Array.Empty<float>();
        }

        await Task.Run(() => _waveIn.StopRecording(), cancellationToken).ConfigureAwait(false);
        _isRecording = false;

        var samples = _buffer.ToArray();
        ClearBuffer();

        return samples;
    }

    public void Dispose()
    {
        _waveIn.DataAvailable -= OnDataAvailable;
        _waveIn.Dispose();
    }
}
