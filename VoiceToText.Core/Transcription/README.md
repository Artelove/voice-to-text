# Transcription (VoiceToText.Core)

Описание подсистемы, отвечающей за приём аудио и преобразование его в текст.

Pipeline (кратко):
1. AudioRecorder — захват аудио с микрофона (PCM 16 kHz, mono).
2. WhisperService (Whisper.net) — локальная транскрибация в текст.
3. (опционально) PunctuationService — отправка текста в HTTP сервис для восстановления пунктуации и регистра.
4. ClipboardService — помещение результата в буфер обмена.
5. Авто-вставка — эмуляция Ctrl+V для быстрого ввода текста в активное окно.

Важные файлы:
- `Audio/AudioRecorder.cs` — логика записи/буферизации аудио.
- `Transcription/WhisperService.cs` — обёртка над Whisper.net.
- `Services/PunctuationService.cs` — HTTP-клиент для /fix (fail-open поведение).
- `VoiceToTextManager.cs` — оркестровка пайплайна.

Конфигурация — `VoiceToText.Core/Services/AppSettings.cs` (ModelPath, Language, UsePunctuation, PunctuationServiceUrl).

Примеры использования и тесты — см. основной проект `VoiceToText.App` (UI) или запустите модульные тесты при наличии тестовой среды.
