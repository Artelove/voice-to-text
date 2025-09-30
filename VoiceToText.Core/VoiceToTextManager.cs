using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using VoiceToText.Core.Audio;
using VoiceToText.Core.Services;
using VoiceToText.Core.Transcription;

namespace VoiceToText.Core;

public sealed class VoiceToTextManager : IAsyncDisposable
{
    private readonly AppSettings _settings;
    private readonly AudioRecorder _recorder;
    private readonly WhisperService _whisperService;
    private readonly PunctuationService? _punctuationService;

    public VoiceToTextManager(AppSettings settings)
    {
        _settings = settings;
        Logger.Info("Initializing VoiceToTextManager with model: {0}, language: {1}",
            settings.ModelPath, settings.Language);

        _recorder = new AudioRecorder(settings.SampleRate, settings.Channels);
        _whisperService = new WhisperService(settings.ModelPath, settings.Language);
        
        // Initialize punctuation service if enabled
        if (settings.UsePunctuation)
        {
            _punctuationService = new PunctuationService(
                settings.PunctuationServiceUrl, 
                settings.UsePunctuation,
                settings.PunctuationTimeoutSeconds);
        }
    }

    public Task InitializeAsync(CancellationToken cancellationToken)
    {
        return _whisperService.EnsureModelAsync(cancellationToken);
    }

    public async Task StartRecordingAsync(CancellationToken cancellationToken = default)
    {
        Logger.Info("Starting recording process...");
        await InitializeAsync(cancellationToken).ConfigureAwait(false);
        await _recorder.StartAsync(cancellationToken).ConfigureAwait(false);
        Logger.Info("Recording started successfully");
    }

    public async Task<string> StopAndTranscribeAsync(CancellationToken cancellationToken = default)
    {
        MemoryStream? memoryStream = null;
        try
        {
            Logger.Info("Processing audio...");

            var samples = await _recorder.StopAsync(cancellationToken).ConfigureAwait(false);

            if (samples.Length == 0)
            {
                Logger.Warn("No audio samples captured during recording");
                return string.Empty;
            }

            memoryStream = new MemoryStream();
            WaveExportService.WriteToStream(samples, _settings.SampleRate, _settings.Channels, memoryStream);
            memoryStream.Position = 0;

            var result = await _whisperService.TranscribeAsync(memoryStream, cancellationToken).ConfigureAwait(false);

            // Apply punctuation fix if service is available
            if (_punctuationService != null && !string.IsNullOrEmpty(result))
            {
                result = await _punctuationService.FixTextAsync(result, cancellationToken).ConfigureAwait(false);
                
                // Remove duplicate punctuation marks
                result = Regex.Replace(result, @"(\.)\1+", "$1");
                result = Regex.Replace(result, @"(,)\1+", "$1");
                result = Regex.Replace(result, @"(!)\1+", "$1");
                result = Regex.Replace(result, @"(\?)\1+", "$1");
                result = Regex.Replace(result, @"(:)\1+", "$1");
                result = Regex.Replace(result, @"("")\1+", "$1");
            }

            // Copy transcribed text to clipboard and paste it
            if (!string.IsNullOrEmpty(result))
            {
                await ClipboardService.CopyAndPasteTextAsync(result);
            }

            memoryStream.Dispose();
            return result;
        }
        catch (Exception e)
        {
            Logger.Error("Error during transcription: {0}", e.Message);
            Logger.Debug("Full exception details: {0}", e.ToString());
            memoryStream?.Dispose();
            return string.Empty;
        }
    }

    public async ValueTask DisposeAsync()
    {
        _recorder.Dispose();
        await _whisperService.DisposeAsync();
    }
}
