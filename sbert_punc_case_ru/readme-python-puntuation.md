# Punctuation service — SbertPuncCase (README)

Коротко: локальный HTTP‑сервис на FastAPI для восстановления пунктуации и регистра в тексте (русский). Предназначен для интеграции с локальным ASR (например, C#‑приложением). По умолчанию слушает на 127.0.0.1:5050.

Документы и код сервиса: каталог `sbert_punc_case_ru/server/`.

Что внутри этого README:
- Быстрая инструкция (Quick start)
- Установка и подготовка модели
- Конфигурация
- Запуск (dev / prod)
- API: примеры запросов и ответов
- Ошибки, стратегия «fail‑open» и рекомендации по интеграции
- Отладка и логирование

---

## Быстрый старт (Quick start)

1) Перейдите в папку проекта и (опционально) создайте виртуальное окружение:

```powershell
cd voice-to-text\sbert_punc_case_ru
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

2) Установите зависимости:

```powershell
pip install -r server/requirements.txt
```

3) Запустите сервис (development):

```powershell
python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
```

Проверка health:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/health' -Method Get
# -> {"status":"healthy"}
```

---

## Установка модели

- Если модель догружается через git‑lfs, убедитесь, что `git-lfs` установлен и файлы модели скачаны.
- В репозитории может быть `model.safetensors` или другой вес модели — проверьте `server/config.py` на `MODEL_DIR`/`MODEL_NAME`.

Если модель отсутствует, загрузите её согласно инструкциям в корне `sbert_punc_case_ru`.

---

## Конфигурация

Основные параметры в `sbert_punc_case_ru/server/config.py`:

- `HOST` — адрес (по умолчанию `127.0.0.1`)
- `PORT` — порт (по умолчанию `5050`)
- `DEVICE` — `auto` / `cpu` / `cuda`
- `BATCH_SIZE` — батч для инференса
- `TIMEOUT_MS` — таймаут обработки запроса
- `MAX_TEXT_LENGTH` — макс. длина запроса в байтах

Изменяйте порт и URL в C# (`AppSettings.PunctuationServiceUrl`) при необходимости.

---

## Запуск в production

Рекомендуемый вариант — gunicorn с uvicorn workers или systemd unit. Пример (gunicorn):

```powershell
cd sbert_punc_case_ru
gunicorn -k uvicorn.workers.UvicornWorker -c gunicorn_config.py server.app:app
```

При развёртывании обеспечьте: ограничение доступа (локальная сеть), мониторинг и ротацию логов.

---

## API

GET /health

- Возвращает статус сервиса.

Пример:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/health' -Method Get
# {"status":"healthy"}
```

POST /fix

- Вход: JSON { "text": "..." }
- Выход: JSON { "text_fixed": "...", "fallback": <bool> }

Ограничение: длина текста не должна превышать `MAX_TEXT_LENGTH` (байт). Для длинных текстов разбивайте на чанки на стороне клиента.

Пример (PowerShell):

```powershell
$body = @{ text = 'привет как дела' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/fix' -Method Post -Body $body -ContentType 'application/json'
```

Успешный ответ:

```json
{
  "text_fixed": "Привет, как дела?",
  "fallback": false
}
```

При ошибке / таймауте:

```json
{ "text_fixed": "привет как дела", "fallback": true }
```

(`fallback: true` означает, что сервис вернул исходный текст — вызывающему клиенту следует обработать это как «использовать оригинал»).

---

## Поведение при ошибках и политика отказоустойчивости

- Стратегия: fail‑open — если сервис недоступен, возвращается исходный текст (никаких блокировок в UX).
- На клиенте (C#) рекомендуется: таймаут ~1500–2000 ms, одна попытка повтора, затем использовать оригинальный текст.

---

## Производительность и рекомендации

- Latency: ~50–200 ms на CPU (зависит от модели и железа); на GPU — быстрее.
- Инициализация модели: несколько секунд — минут в зависимости от размера.
- Рекомендации:
  - Для низкой задержки используйте `DEVICE='cuda'` где доступно.
  - Включите `BATCH_SIZE` >1 только если ожидаете параллельные запросы.
  - Логируйте метрики: входной размер, время обработки, fallback events.

---

## Интеграция с C# (коротко)

- Endpoint: `POST http://127.0.0.1:5050/fix`
- Request: `{ "text": "..." }`
- Response: `{ "text_fixed": "...", "fallback": <bool> }`
- Клиентская логика: разбивать большие тексты по `MAX_TEXT_LENGTH`, ставить таймаут и fallback на оригинал.

Пример поведения в C#:

1. Если `UsePunctuation=false` — не вызывать сервис.
2. Иначе — отправить запрос, при `fallback==false` — использовать `text_fixed`, иначе — `originalText`.

---

## Отладка и логирование

- Логи при старте должны содержать:

```
INFO: Starting punctuation service...
INFO: Initializing SbertPuncCase model on device: cpu
INFO: Model initialized successfully
INFO: Service ready
```

- Типичные проблемы:
  - `Address already in use` — порт занят: `netstat -ano | findstr :5050`.
  - `Model not initialized` — проверьте наличие весов и доступность памяти.

---

## Частые вопросы

- Q: Можно ли делать публичный доступ?  
  A: По умолчанию служба локальная — для публичного доступа добавьте аутентификацию и безопасный прокси.

- Q: Что делать при нехватке памяти?  
  A: Используйте меньшую модель или `DEVICE='cpu'` и/или увеличьте swap/память GPU.

---

Если хотите, могу подправить формулировки, добавить примеры curl/Python-клиента или подготовить `systemd` unit (не Docker). 

---

## Коротко

Сервис принимает на вход сырой текст (обычно результат ASR — строчные буквы без пунктуации) и возвращает текст с восстановленным регистром и пунктуацией. Предназначен для локального запуска (по умолчанию 127.0.0.1:5050) и интеграции с C# клиентом через HTTP.

Поведение при ошибках: сервис следует стратегии fail-open — при сбое возвращает оригинальный текст и флаг `fallback: true`.

## Технические требования

- Python 3.8+
- Пакеты из `server/requirements.txt` (FastAPI, uvicorn, torch и т.д.)
- Дисковое пространство и память для модели (зависит от используемой модели: может потребоваться несколько гигабайт)
- (опционально) GPU + CUDA, если хотите ускорить inference

## Установка

1) Клонируйте репозиторий (или перейдите в директорию проекта уже у вас на машине):

```powershell
cd voice-to-text\sbert_punc_case_ru
```

2) Рекомендуется создать виртуальное окружение и активировать его:

```powershell
python -m venv .venv
.\.venv\Scripts\Activate.ps1
```

3) Установите зависимости:

```powershell
pip install -r server/requirements.txt
```

4) Если модель хранится через git-lfs, убедитесь, что git-lfs установлен и модель доступна (в репозитории может быть файл `model.safetensors` или похожие артефакты).

Если модель не присутствует, загрузите её согласно инструкциям в корне `sbert_punc_case_ru` (обычно через git-lfs или прямую ссылку).

## Конфигурация

Основные параметры в `sbert_punc_case_ru/server/config.py`:

- `HOST` — IP адрес (по умолчанию `127.0.0.1`)
- `PORT` — порт сервера (по умолчанию `5050`)
- `DEVICE` — `auto`, `cpu` или `cuda`
- `BATCH_SIZE` — размер батча для модели
- `TIMEOUT_MS` — таймаут ответа в миллисекундах
- `MAX_TEXT_LENGTH` — максимальная длина текста в байтах (по умолчанию ~20000)

Пример (в файле `server/config.py`):

```python
HOST = "127.0.0.1"
PORT = 5050
DEVICE = "auto"  # auto, cpu, cuda
TIMEOUT_MS = 2000
MAX_TEXT_LENGTH = 20000
```

Изменяйте порт и device в зависимости от окружения и интеграции с C# (при изменении порта — обновите `PunctuationServiceUrl` в `AppSettings` C#).

## Запуск

Dev (uvicorn):

```powershell
cd sbert_punc_case_ru
python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
```

Production (gunicorn + uvicorn workers):

```powershell
cd sbert_punc_case_ru
gunicorn -k uvicorn.workers.UvicornWorker -c gunicorn_config.py server.app:app
```

Если порт занят, остановите процессы или поменяйте порт в конфиге и в `AppSettings` C#.

## API

GET /health

- Проверка статуса сервиса.

Пример (PowerShell):

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/health' -Method Get
```

Ожидаемый ответ:

```json
{"status": "healthy"}
```

POST /fix

- Тело запроса JSON: `{ "text": "..." }` — строка для обработки.
- Ограничения: длина текста в байтах не должна превышать `MAX_TEXT_LENGTH`.

Пример запроса (PowerShell):

```powershell
$body = @{ text = 'привет как дела' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/fix' -Method Post -Body $body -ContentType 'application/json'
```

Пример cURL:

```bash
curl -X POST "http://127.0.0.1:5050/fix" -H "Content-Type: application/json" -d '{"text":"привет как дела"}'
```

Успешный ответ:

```json
{
  "text_fixed": "Привет, как дела?",
  "fallback": false
}
```

Если произошла ошибка или превышен таймаут — сервис вернёт:

```json
{
  "text_fixed": "привет как дела",
  "fallback": true
}
```

(То есть текст возвращается неизменным, `fallback: true` указывает, что восстановление не выполнено.)

## Ограничения и стратегия обработки больших текстов

- Максимальная длина запроса ограничена конфигурацией `MAX_TEXT_LENGTH`.
- Для длинных текстов сервис может поддерживать разбиение на сегменты — интегрируйте разбиение на стороне клиента (C#), отправляя чанки по `MAX_TEXT_LENGTH` байт.

## Логи и отладка

- При старте ожидается логи вида:

```
INFO: Starting punctuation service...
INFO: Initializing SbertPuncCase model on device: cpu
INFO: Model initialized successfully
INFO: Service ready
INFO: Uvicorn running on http://127.0.0.1:5050
```

- Частые проблемы:
  - `Address already in use` — порт занят; используйте `netstat -ano | findstr :5050` и завершите процесс или измените порт.
  - `Model not initialized` — проблема с загрузкой модели (проверьте наличие файлов модели и доступность GPU/памяти).

## Производительность

- Время инициализации модели: зависит от размера модели и устройства (CPU/GPU) — от секунд до минут.
- Latency на запрос: ~50–200 ms на CPU; на GPU — быстрее.
- Рекомендации: для низкой латентности используйте CUDA (если доступно) и увеличьте `BATCH_SIZE` аккуратно.

## Развёртывание

- Для Windows: запуск через `start.bat` (если он настроен) или сервис/планировщик задач.
- Для Linux: systemd service, контейнеризация (Docker) с предикативной настройкой памяти и GPU.

Пример systemd unit (на Linux):

```ini
[Unit]
Description=Punctuation Service
After=network.target

[Service]
User=voice2text
WorkingDirectory=/opt/voice-to-text/sbert_punc_case_ru
ExecStart=/usr/bin/python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
Restart=on-failure

[Install]
WantedBy=multi-user.target
```

## Интеграция с C# (коротко)

- Endpoint: `POST http://127.0.0.1:5050/fix`
- Request: `{ "text": "..." }`
- Response: `{ "text_fixed": "...", "fallback": <bool> }`
- Timeout: на стороне клиента используйте таймаут ~1500–2000 ms и стретегию retry=1, затем fallback на оригинальный текст.

## Тесты и примеры

- Проверка health:

```powershell
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/health' -Method Get
```

- Простой запрос:

```powershell
$body = @{ text = 'привет как дела' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://127.0.0.1:5050/fix' -Method Post -Body $body -ContentType 'application/json'
```

## Частые вопросы

- Q: Можно ли запускать сервис публично?  
  A: По умолчанию сервис локальный (127.0.0.1). Для публичного доступа настраивайте аутентификацию и ограничения сети — это не рекомендуется для приватных моделей.

- Q: Что делать, если память заканчивается при загрузке модели?  
  A: Используйте меньшую модель, загрузку в `cpu` или включите своп/увеличьте память GPU.

## Авторы и лицензия

- Авторы: Альмира Муртазина, Александр Абугалиев
- Лицензия модели/репозитория: проверьте `LICENSE` в корне репозитория (обычно MIT для проекта, модель может иметь собственную лицензию).

---

Если хотите, могу: добавить готовый `systemd` unit, Dockerfile или объединить этот README с `server/README.md` в единый файл `README_full.md`.
