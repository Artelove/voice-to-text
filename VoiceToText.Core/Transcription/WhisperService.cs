using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Whisper.net;
using Whisper.net.Ggml;
using VoiceToText.Core.Services;

namespace VoiceToText.Core.Transcription;

public sealed class WhisperService : IAsyncDisposable
{
    private readonly string _modelPath;
    private readonly string _language;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private WhisperFactory? _factory;

    public WhisperService(string modelPath, string language = "auto")
    {
        // Resolve relative path to absolute path based on executable location
        if (!Path.IsPathRooted(modelPath))
        {
            var exePath = AppDomain.CurrentDomain.BaseDirectory;
            _modelPath = Path.GetFullPath(Path.Combine(exePath, modelPath));
        }
        else
        {
            _modelPath = modelPath;
        }

        _language = language;
    }

    public async Task EnsureModelAsync(CancellationToken cancellationToken)
    {
        if (File.Exists(_modelPath))
        {
            Logger.Info("Model file already exists: {0}", _modelPath);
            return;
        }

        Logger.Info("Model file not found, downloading from Hugging Face: {0}", _modelPath);
        using var modelStream = await WhisperGgmlDownloader.Default
            .GetGgmlModelAsync(ModelFromFileName(_modelPath), cancellationToken: cancellationToken);
        using var fileStream = File.OpenWrite(_modelPath);
        await modelStream.CopyToAsync(fileStream, cancellationToken);
        Logger.Info("Model downloaded and saved successfully");
    }

    private static GgmlType ModelFromFileName(string fileName)
    {
        var normalized = Path.GetFileName(fileName)?.ToLowerInvariant();
        if (normalized is null)
        {
            return GgmlType.Base;
        }

        if (normalized.Contains("tiny"))
        {
            return normalized.Contains("en") ? GgmlType.TinyEn : GgmlType.Tiny;
        }

        if (normalized.Contains("base"))
        {
            return normalized.Contains("en") ? GgmlType.BaseEn : GgmlType.Base;
        }

        if (normalized.Contains("large-v3-turbo"))
        {
            return GgmlType.LargeV3Turbo;
        }

        if (normalized.Contains("small"))
        {
            return normalized.Contains("en") ? GgmlType.SmallEn : GgmlType.Small;
        }

        if (normalized.Contains("medium"))
        {
            return normalized.Contains("en") ? GgmlType.MediumEn : GgmlType.Medium;
        }

        if (normalized.Contains("large-v3"))
        {
            return GgmlType.LargeV3;
        }

        if (normalized.Contains("large-v2"))
        {
            return GgmlType.LargeV2;
        }

        return GgmlType.Base;
    }

    private WhisperFactory EnsureFactory()
    {
        if (_factory != null)
        {
            return _factory;
        }

        try
        {
            Logger.Info("🎮 Attempting to initialize Whisper with GPU (CUDA) support...");
            _factory = WhisperFactory.FromPath(_modelPath, new WhisperFactoryOptions
            {
                UseGpu = true // Try GPU first for better performance

            });
            Logger.Info("✅ GPU (CUDA) successfully initialized for Whisper transcription!");
        }
        catch (Exception ex)
        {
            Logger.Warn("⚠️ GPU initialization failed: {0}", ex.Message);
            Logger.Debug("GPU error details: {0}", ex.ToString());
            Logger.Info("🔄 Falling back to CPU mode for Whisper...");
            _factory = WhisperFactory.FromPath(_modelPath, new WhisperFactoryOptions
            {
                UseGpu = false
            });
            Logger.Info("✅ CPU mode initialized successfully");
        }

        return _factory;
    }

    public async Task<string> TranscribeAsync(Stream audioStream, CancellationToken cancellationToken)
    {
        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            var factory = EnsureFactory();
            await using var processor = factory.CreateBuilder()
                .WithLanguage(_language == "auto" ? "ru" : _language)
                .WithThreads(16)
                .WithLanguageDetection()
                .WithTemperature(0.2f)
                .Build();

            var segments = new List<string>();
            var segmentCount = 0;

            await foreach (var result in processor.ProcessAsync(audioStream, cancellationToken))
            {
                segments.Add(result.Text);
                segmentCount++;
            }

            var finalText = string.Concat(segments);
            Logger.Info("Transcription completed: {0} characters. Text : {1}", finalText.Length, finalText);

            return finalText;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _semaphore.WaitAsync();
        try
        {
            _factory?.Dispose();
        }
        finally
        {
            _semaphore.Release();
            _semaphore.Dispose();
        }
    }
}
