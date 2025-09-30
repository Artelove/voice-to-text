using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Sockets;

namespace VoiceToText.Core.Services;

/// <summary>
/// Service for punctuation and case restoration using Python backend
/// </summary>
public sealed class PunctuationService
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceUrl;
    private readonly bool _isEnabled;

    public PunctuationService(string serviceUrl, bool isEnabled = true, int timeoutSeconds = 10)
    {
        _serviceUrl = serviceUrl;
        _isEnabled = isEnabled;
        
        // Configure HttpClient for better performance
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
            UseProxy = false,
            MaxConnectionsPerServer = 10,
            EnableMultipleHttp2Connections = true
        };
        
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds),
            BaseAddress = new Uri(serviceUrl)
        };
        
        _httpClient.DefaultRequestHeaders.ExpectContinue = false;

        Logger.Info("PunctuationService initialized: url={0}, enabled={1}", serviceUrl, isEnabled);
    }

    /// <summary>
    /// Fix punctuation and case in the provided text
    /// </summary>
    /// <param name="text">Text to fix</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Fixed text, or original text if service is disabled or fails</returns>
    public async Task<string> FixTextAsync(string text, CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            Logger.Debug("Punctuation service is disabled, returning original text");
            return text;
        }

        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            Logger.Debug("Sending text to punctuation service: {0} characters", text.Length);

            var request = new PunctuationRequest { Text = text };
            var jsonContent = JsonSerializer.Serialize(request);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/fix", content, cancellationToken);
            stopwatch.Stop();

            if (!response.IsSuccessStatusCode)
            {
                Logger.Warn("Punctuation service returned error status: {0} (elapsed: {1}ms)", response.StatusCode, stopwatch.ElapsedMilliseconds);
                return text;
            }

            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<PunctuationResponse>(responseJson);

            if (result?.Fallback == true)
            {
                Logger.Warn("Punctuation service used fallback, returning original text (elapsed: {0}ms)", stopwatch.ElapsedMilliseconds);
                return text;
            }

            var fixedText = result?.TextFixed ?? text;
            Logger.Info("Text processed by punctuation service: {0} -> {1} characters (elapsed: {2}ms). Text: {3}", 
                text.Length, fixedText.Length, stopwatch.ElapsedMilliseconds, fixedText);

            return fixedText;
        }
        catch (TaskCanceledException)
        {
            stopwatch.Stop();
            Logger.Warn("Punctuation service request timed out after {0}ms, returning original text", stopwatch.ElapsedMilliseconds);
            return text;
        }
        catch (HttpRequestException ex)
        {
            stopwatch.Stop();
            Logger.Error("HTTP error when calling punctuation service (elapsed: {0}ms): {1}", stopwatch.ElapsedMilliseconds, ex.Message);
            return text;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Logger.Error("Unexpected error in punctuation service (elapsed: {0}ms): {1}", stopwatch.ElapsedMilliseconds, ex.Message);
            Logger.Debug("Full exception: {0}", ex.ToString());
            return text;
        }
    }

    /// <summary>
    /// Check if the punctuation service is healthy
    /// </summary>
    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (!_isEnabled)
        {
            return false;
        }

        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private record PunctuationRequest
    {
        [JsonPropertyName("text")]
        public string Text { get; init; } = string.Empty;
    }

    private record PunctuationResponse
    {
        [JsonPropertyName("text_fixed")]
        public string TextFixed { get; init; } = string.Empty;
        
        [JsonPropertyName("fallback")]
        public bool Fallback { get; init; }
    }
}
