# C# Client Integration Contract

> ✅ **IMPLEMENTED**: Full integration available in `VoiceToText.Core.Services.PunctuationService`

## HTTP Service Endpoint
- **URL**: `http://127.0.0.1:5050/fix`
- **Method**: POST
- **Content-Type**: `application/json`

## Request Format
```json
{
  "text": "<string>"
}
```

### Request Fields
- `text` (string, required): Text to process with punctuation and case restoration

### Request Constraints
- Maximum text length: 20 KB (20000 bytes)
- Text must not be empty or whitespace-only

## Response Format
```json
{
  "text_fixed": "<string>",
  "fallback": <boolean>
}
```

### Response Fields
- `text_fixed` (string): Processed text with punctuation and case fixes, or original text if fallback occurred
- `fallback` (boolean): `false` for successful processing, `true` if error occurred and original text returned

## Integration Guidelines

### HttpClient Configuration
```csharp
// Singleton HttpClient instance
private static readonly HttpClient _httpClient = new HttpClient
{
    BaseAddress = new Uri("http://127.0.0.1:5050/"),
    Timeout = TimeSpan.FromMilliseconds(1500) // or 2000ms
};
```

### Request/Response Handling
```csharp
public async Task<string> FixTextAsync(string originalText)
{
    try
    {
        var request = new { text = originalText };
        var response = await _httpClient.PostAsJsonAsync("fix", request);

        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadFromJsonAsync<FixResponse>();
            if (result?.fallback == false)
            {
                return result.text_fixed;
            }
            else
            {
                // Log fallback but continue with original text
                _logger.LogWarning("Punctuation service returned fallback, using original text");
                return originalText;
            }
        }
        else
        {
            _logger.LogWarning($"Punctuation service returned {response.StatusCode}");
            return originalText;
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error calling punctuation service");
        return originalText;
    }
}
```

### Pipeline Integration
1. **After transcription**: Receive raw ASR text
2. **Before clipboard**: Call `FixTextAsync(rawText)`
3. **On success**: Use `text_fixed` for clipboard
4. **On failure/fallback**: Use original `rawText` (fail-open behavior)

### Text Size Handling
```csharp
public async Task<string> ProcessLargeTextAsync(string text)
{
    const int maxBytes = 20000;

    if (Encoding.UTF8.GetByteCount(text) <= maxBytes)
    {
        return await FixTextAsync(text);
    }

    // Split into chunks and process separately
    var chunks = SplitTextIntoChunks(text, maxBytes);
    var processedChunks = new List<string>();

    foreach (var chunk in chunks)
    {
        var processed = await FixTextAsync(chunk);
        processedChunks.Add(processed);
    }

    return string.Join(" ", processedChunks);
}
```

### Error Handling Strategy
- **Network timeouts**: Retry once, then use original text
- **Service unavailable**: Use original text (fail-open)
- **Invalid responses**: Use original text
- **Large texts**: Split and process in chunks
- **Always preserve UX**: Never block user workflow

### Health Check
- **Endpoint**: `GET http://127.0.0.1:5050/health`
- **Expected**: `200 OK` with `{"status": "healthy"}`
- **Usage**: Check service availability before processing

### Performance Expectations
- **Typical latency**: 100-700ms depending on device (CPU/CUDA)
- **Batch processing**: Supported via `BATCH_SIZE` configuration
- **Concurrent requests**: Single worker, serialize requests

### Logging
Log the following events:
- Service unavailable
- Fallback responses
- Processing failures
- Large text splitting
- Performance metrics (optional)

---

## Actual Implementation

### Files
- **Service**: `VoiceToText.Core/Services/PunctuationService.cs`
- **Settings**: `VoiceToText.Core/Services/AppSettings.cs`
- **Integration**: `VoiceToText.Core/VoiceToTextManager.cs`
- **Documentation**: `VoiceToText.Core/Services/README_Punctuation.md`

### Implementation Details

```csharp
// PunctuationService.cs - HTTP client for Python service
public sealed class PunctuationService : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _serviceUrl;
    private readonly bool _isEnabled;

    public async Task<string> FixTextAsync(string text, CancellationToken cancellationToken)
    {
        // POST request to Python service
        // Returns fixed text or original on error (fail-open)
    }

    public async Task<bool> CheckHealthAsync(CancellationToken cancellationToken)
    {
        // GET /health endpoint check
    }
}
```

### Configuration

```csharp
// AppSettings.cs
public sealed record AppSettings
{
    public bool UsePunctuation { get; init; } = true;
    public string PunctuationServiceUrl { get; init; } = "http://localhost:5050/fix";
    public int PunctuationTimeoutSeconds { get; init; } = 10;
}
```

### Pipeline Flow

```csharp
// VoiceToTextManager.cs - StopAndTranscribeAsync()
public async Task<string> StopAndTranscribeAsync(CancellationToken cancellationToken)
{
    // 1. Record audio
    var samples = await _recorder.StopAsync(cancellationToken);
    
    // 2. Transcribe with Whisper
    var result = await _whisperService.TranscribeAsync(memoryStream, cancellationToken);
    
    // 3. Fix punctuation (NEW)
    if (_punctuationService != null && !string.IsNullOrEmpty(result))
    {
        result = await _punctuationService.FixTextAsync(result, cancellationToken);
    }
    
    // 4. Copy to clipboard and paste
    await ClipboardService.CopyAndPasteTextAsync(result);
    
    return result;
}
```

### Usage

```csharp
// Initialize with punctuation enabled
var settings = new AppSettings
{
    ModelPath = "ggml-large-v3-turbo.bin",
    Language = "ru",
    UsePunctuation = true,
    PunctuationServiceUrl = "http://localhost:5050/fix"
};

var manager = new VoiceToTextManager(settings);

// Start Python server first:
// cd sbert_punc_case_ru
// python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1

// Record and transcribe
await manager.StartRecordingAsync();
// ... speak ...
var transcribedText = await manager.StopAndTranscribeAsync();
// Text is automatically fixed, copied to clipboard, and pasted
```

### Error Handling

The implementation follows fail-open strategy:
- ✅ Service unavailable → returns original text
- ✅ Timeout exceeded → returns original text  
- ✅ HTTP error → returns original text
- ✅ Service disabled → skips punctuation entirely
- ✅ All errors are logged via Logger

### Dependencies

- `System.Net.Http` - HttpClient
- `System.Text.Json` - JSON serialization
- No additional NuGet packages required
