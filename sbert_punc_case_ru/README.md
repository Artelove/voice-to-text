# SbertPuncCase — пунктуация и регистрозамена для русского текста

Небольшой ML-пакет и HTTP-сервис для восстановления регистра и пунктуации в тексте, подходящий для интеграции с локальной транскрипцией (Whisper).

Что внутри:
- Модель и вспомогательные файлы (включая `model.safetensors`).
- HTTP-сервер на FastAPI в `server/` с API `/health` и `/fix`.

Быстрый запуск (dev):

```powershell
cd sbert_punc_case_ru
pip install -r server/requirements.txt
python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1
```

После запуска:
- Проверка: GET http://127.0.0.1:5050/health
- Пример запроса: POST http://127.0.0.1:5050/fix { "text": "..." }

Файлы конфигурации сервера — `sbert_punc_case_ru/server/config.py`.

Ссылки:
- `sbert_punc_case_ru/server/README.md` — подробная документация API и примеры.
- `sbert_punc_case_ru/adapters/csharp_contract.md` — контракт интеграции с C# клиентом.

Лицензия: Apache-2.0 (см. метаданные пакета).

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

# Авторы

[Альмира Муртазина](https://github.com/almiradreamer)

[Александр Абугалиев](https://github.com/Squire-tomsk)