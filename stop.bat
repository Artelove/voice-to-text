@echo off
chcp 65001 > nul
echo ========================================
echo  Voice-to-Text - Остановка сервисов
echo ========================================
echo.

echo Остановка сервиса пунктуации...
taskkill /FI "WINDOWTITLE eq Punctuation Service*" /F >nul 2>&1
for /f "tokens=5" %%a in ('netstat -aon ^| findstr :5050 ^| findstr LISTENING') do (
    taskkill /F /PID %%a >nul 2>&1
)

echo Остановка Voice-to-Text...
taskkill /IM VoiceToText.App.exe /F >nul 2>&1

echo.
echo ✓ Все сервисы остановлены
echo.
pause
