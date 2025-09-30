---
language:
- ru
tags:
- PyTorch
- Transformers
license: apache-2.0
base_model: ai-forever/sbert_large_nlu_ru
inference: false
---

# SbertPuncCase

SbertPuncCase - модель восстановления пунктуации и регистра для русского языка. Модель способна расставлять точки, запятые и знаки вопроса; 
определять регистр - слово в нижнем регистре, слово с первой буквой в верхнем регистре, слово в верхнем регистре.
Модель разработана для восстановления текста после распознавания речи, поэтому работает со строками в нижнем регистре.
В основу модели легла [sbert_large_nlu_ru](https://huggingface.co/sberbank-ai/sbert_large_nlu_ru). 
В качестве обучающих данных использованы текстовые расшифровки интервью.

# Как это работает

1. Текст переводится в нижний регистр и разбивается на слова.
2. Слова разделяются на токены.
3. Модель (по аналогии с задачей NER) предсказывает класс для каждого токена. Классификация на 12 классов: 3+1 знака препинания * 3 варианта регистра.
4. Функция декодировки восстанавливает текст соответственно предсказанным классам.

# Как использовать

Код модели находится в файле `sbert-punc-case-ru/sbertpunccase.py`.

1. Убедитесь, что у вас установлен `git-lfs`.

2. Далее для быстрой установки можно воспользоваться командой:

```
pip install git+https://huggingface.co/kontur-ai/sbert_punc_case_ru
```

Использование модели:
```
from sbert_punc_case_ru import SbertPuncCase
model = SbertPuncCase()
model.punctuate("sbert punc case расставляет точки запятые и знаки вопроса вам нравится")
```

# sbert_punc_case_ru_src — исходный код пакета

Этот каталог содержит исходники Python-пакета, который обеспечивает API для работы с моделью SbertPuncCase.

Быстрое использование в Python:

```python
from sbertpunccase import SbertPuncCase
model = SbertPuncCase()
print(model.punctuate('privet kak dela'))
```

Установка (локальная разработка):

```powershell
cd sbert_punc_case_ru
pip install -e .
```

Требования и запуск сервера — см. `sbert_punc_case_ru/server/README.md`.

Если нужен перенос модели или упаковка — можно подготовить wheel/egg через обычные setuptools-инструкции (setup.py уже включён).

# Локальный HTTP-сервис постобработки

В пакете доступно FastAPI-приложение, которое можно запустить локально и вызывать по HTTP.

## Конфигурация

Поддерживаются переменные окружения:

| Имя | Назначение | Значение по умолчанию |
| --- | --- | --- |
| `HOST` | Адрес для прослушивания | `127.0.0.1` |
| `PORT` | TCP-порт | `8000` |
| `DEVICE` | `cpu`, `cuda` или `auto` | `auto` |
| `TIMEOUT_MS` | Таймаут обработки запроса | `2000` |
| `BATCH_SIZE` | Размер батча для модели | `4` |
| `SEGMENT_WORD_LENGTH` | Максимум слов в сегменте | `350` |
| `MAX_REQUEST_CHARS` | Максимальная длина строки | `20000` |
| `SPELL_LANGUAGE` | Язык орфографического словаря | `ru` |
| `MODEL_DIR` | Путь к весам модели | путь к локальной папке с моделью |

## Запуск

```
python -m uvicorn sbert_punc_case_ru.api:app --host 127.0.0.1 --port 8000 --workers 1
```

## Маршруты

- `GET /health` — проверка готовности (`{"status": "ok"}`).
- `POST /fix` — получает JSON `{"text": "..."}` и возвращает `{"text_fixed": "...", "fallback": false}`.

# Авторы

[Альмира Муртазина](https://github.com/almiradreamer)

[Александр Абугалиев](https://github.com/Squire-tomsk)