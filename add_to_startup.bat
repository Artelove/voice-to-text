@echo off
chcp 65001 > nul
echo ========================================
echo  Добавление в автозагрузку Windows
echo ========================================
echo.

set SCRIPT_PATH=%~dp0start.bat

echo Добавление в реестр...
reg add "HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Run" /v "VoiceToText" /t REG_SZ /d "\"%SCRIPT_PATH%\"" /f

if %errorlevel% == 0 (
    echo.
    echo ✓ Успешно добавлено в автозагрузку!
    echo.
    echo Voice-to-Text будет запускаться автоматически
    echo при входе в Windows.
    echo.
    echo Для удаления из автозагрузки запустите:
    echo   remove_from_startup.bat
) else (
    echo.
    echo ✗ Ошибка при добавлении в автозагрузку.
    echo Попробуйте запустить от имени администратора.
)

echo.
pause
