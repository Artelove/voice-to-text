namespace VoiceToText.Core.Services;

public sealed record AppSettings
{
    public string ModelPath { get; init; } = "ggml-large-v3-turbo.bin";
    public string Language { get; init; } = "auto";
    public int SampleRate { get; init; } = 16000;
    public int Channels { get; init; } = 1;
    
    // Punctuation service settings
    public bool UsePunctuation { get; init; } = true;
    public string PunctuationServiceUrl { get; init; } = "http://127.0.0.1:5050";
    public int PunctuationTimeoutSeconds { get; init; } = 1;
}

