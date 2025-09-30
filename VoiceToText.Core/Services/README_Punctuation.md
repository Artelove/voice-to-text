# PunctuationService (интеграция для C#)

Клиентская часть в проекте отвечает за вызов внешнего HTTP-сервиса пунктуации (`/fix`) и безопасное поведение (fail-open).

Как это работает:
- При включённом `UsePunctuation=true` приложение посылает POST на `PunctuationServiceUrl` (по умолчанию http://localhost:5050/fix).
- В случае ошибки/таймаута возвращается исходный текст (fail-open).

Пример использования (псевдокод на C#):

```csharp
var service = new PunctuationService("http://localhost:5050/fix");
if (await service.CheckHealthAsync()) {
    var result = await service.FixTextAsync("privet kak dela");
    Console.WriteLine(result.TextFixed);
}
```

Таймаут и URL настраиваются через `AppSettings`:
- `UsePunctuation` — включить/выключить отправку на Python-сервис.
- `PunctuationServiceUrl` — URL POST /fix.
- `PunctuationTimeoutSeconds` — таймаут ожидания ответа.

Тестирование вручную (PowerShell):

```powershell
$body = @{ text = 'privet kak dela' } | ConvertTo-Json
Invoke-RestMethod -Uri 'http://localhost:5050/fix' -Method Post -Body $body -ContentType 'application/json'
```

Если нужна помощь с обработкой ошибок или изменением политики (например, блокировать вставку при недоступности сервиса) — могу предложить изменения.
