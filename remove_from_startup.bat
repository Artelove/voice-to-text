@echo off
chcp 65001 > nul
echo ========================================
echo  Удаление из автозагрузки Windows
echo ========================================
echo.

echo Удаление из реестра...
reg delete "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v "VoiceToText" /f 2>nul

if %errorlevel% == 0 (
    echo.
    echo ✓ Успешно удалено из автозагрузки!
) else (
    echo.
    echo ⚠ Voice-to-Text не найден в автозагрузке
    echo или уже был удален ранее.
)

REM Также удаляем старую запись, если была
reg delete "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v "VoiceToTextCSharp" /f 2>nul

echo.
pause
