# Punctuation Service (FastAPI)

HTTP-сервис для восстановления пунктуации и регистра в тексте. Предназначен для интеграции с локальной транскрипцией (Whisper).

Эндпоинты:
- GET /health — проверка статуса, возвращает { "status": "healthy" }
- POST /fix — вход: { "text": "..." } → ответ: { "text_fixed": "...", "fallback": false }

Быстрый запуск:

```powershell
cd sbert_punc_case_ru
pip install -r server/requirements.txt
python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
```

Примеры запросов:

curl (Windows PowerShell):

```powershell
$body = @{ text = 'privet kak dela' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:5050/fix' -Method Post -Body $body -ContentType 'application/json'
```

curl (Linux/macOS):

```bash
curl -X POST -H "Content-Type: application/json" -d '{"text":"privet kak dela"}' http://127.0.0.1:5050/fix
```

Конфигурация:
- `HOST`, `PORT`, `DEVICE`, `TIMEOUT_MS`, `MAX_TEXT_LENGTH` — см. `sbert_punc_case_ru/server/config.py`.

Логи и отладка:
- При старте сервис выводит информацию о выбранном устройстве и статусе инициализации модели.

Проблемы в продакшене:
- Сервис работает локально по умолчанию. Для внешнего доступа настройте HOST/PORT и безопасность.

Лицензия: Apache-2.0
