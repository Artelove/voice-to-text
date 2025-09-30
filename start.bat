@echo off
chcp 65001 > nul
echo ========================================
echo  Voice-to-Text - Запуск приложения
echo ========================================
echo.

cd /d "%~dp0"

echo [1/2] Запуск сервиса пунктуации...
cd sbert_punc_case_ru/server
start /min "Punctuation Service" cmd /c "python -m uvicorn server.app:app --host 127.0.0.1 --port 5050 --workers 1"
cd ../..
echo Ожидание инициализации (5 сек)...
timeout /t 5 /nobreak >nul

echo [2/2] Запуск Voice-to-Text...
cd /d "%~dp0"
if exist "VoiceToText.App\bin\Debug\net9.0-windows\VoiceToText.App.exe" (
    start "" "VoiceToText.App\bin\Debug\net9.0-windows\VoiceToText.App.exe"
    echo.
    echo ✓ Приложение запущено!
    echo.
    echo Для остановки всех сервисов запустите: stop.bat
) else (
    echo ОШИБКА: Приложение не найдено!
    echo Пожалуйста, сначала соберите проект:
    echo   cd VoiceToText.App
    echo   dotnet build
    pause
    exit /b 1
)
