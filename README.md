
# Voice-to-Text

![GitHub release](https://img.shields.io/github/v/release/your-org/voice-to-text?style=flat-square)
![GitHub Workflow Status](https://img.shields.io/github/workflow/status/your-org/voice-to-text/CI?style=flat-square)
![MIT License](https://img.shields.io/badge/license-MIT-green?style=flat-square)

Компактный набор для быстрой диктовки и автоматической вставки текста.

---

## О проекте

**Voice-to-Text** объединяет:
- C# WPF-приложение для записи аудио и локальной транскрипции ([Whisper.net](https://github.com/your-org/whisper.net)).
- Python FastAPI-сервис пунктуации на базе Sbert для восстановления регистра и знаков препинания.
- .bat-скрипты для локального старта/остановки и автозапуска в Windows.

## Документация
- [BAT_SCRIPTS_README.md](BAT_SCRIPTS_README.md) — подробная документация по .bat-скриптам
- [README_CSharp_Autostart.md](README_CSharp_Autostart.md) — автозапуск C# приложения
- [sbert_punc_case_ru/README.md](sbert_punc_case_ru/README.md) — документация по пунктуации
- [sbert_punc_case_ru/server/README.md](sbert_punc_case_ru/server/README.md) — API Python сервиса
- [CHANGELOG.md](CHANGELOG.md) — история изменений

## Быстрый старт

### 1. Запуск Python punctuation service

```powershell
cd sbert_punc_case_ru
pip install -r server/requirements.txt
python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
```

### 2. Сборка и запуск C# приложения

```powershell
cd VoiceToText.App
dotnet build
dotnet run
```

### 3. Использование .bat-скриптов
- `start.bat` — запуск всех сервисов
- `stop.bat` — остановка сервисов
- `add_to_startup.bat` / `remove_from_startup.bat` — автозапуск в Windows

## Структура репозитория
- `VoiceToText.App/` — WPF UI (C#)
- `VoiceToText.Core/` — бизнес-логика, интеграция Whisper, сервисы
- `sbert_punc_case_ru/` — модель и HTTP API для пунктуации

## Конфигурация
- C#: `VoiceToText.Core/Services/AppSettings.cs` (ModelPath, UsePunctuation, PunctuationServiceUrl)
- Python: `sbert_punc_case_ru/server/config.py`

## Проверка здоровья
- Punctuation service: `GET http://127.0.0.1:5050/health`

## Контакты и авторы

Смотрите подписи и контакты в отдельных README файлов.
