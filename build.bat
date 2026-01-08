@echo off
setlocal enabledelayedexpansion
chcp 65001 >nul
cls

:: Header
powershell -NoProfile -Command ^
    "Write-Host ''; " ^
    "Write-Host '   ╭──────────────────────────────────────────────────────────╮' -F DarkYellow; " ^
    "Write-Host '   │                                                          │' -F DarkYellow; " ^
    "Write-Host '   │     ██████╗██╗   ██╗███████╗████████╗ ██████╗ ███████╗   │' -F DarkYellow; " ^
    "Write-Host '   │    ██╔════╝██║   ██║██╔════╝╚══██╔══╝██╔═══██╗██╔════╝   │' -F DarkYellow; " ^
    "Write-Host '   │    ██║     ██║   ██║███████╗   ██║   ██║   ██║███████╗   │' -F DarkYellow; " ^
    "Write-Host '   │    ██║     ██║   ██║╚════██║   ██║   ██║   ██║╚════██║   │' -F DarkYellow; " ^
    "Write-Host '   │    ╚██████╗╚██████╔╝███████║   ██║   ╚██████╔╝███████║   │' -F DarkYellow; " ^
    "Write-Host '   │     ╚═════╝ ╚═════╝ ╚══════╝   ╚═╝    ╚═════╝ ╚══════╝   │' -F DarkYellow; " ^
    "Write-Host '   │                                                          │' -F DarkYellow; " ^
    "Write-Host '   │' -F DarkYellow -N; Write-Host '            ✦  B U I L D   S Y S T E M  ✦              ' -F DarkGray -N; Write-Host '│' -F DarkYellow; " ^
    "Write-Host '   │                                                          │' -F DarkYellow; " ^
    "Write-Host '   ╰──────────────────────────────────────────────────────────╯' -F DarkYellow; " ^
    "Write-Host ''"

:: Save start time
set "START_TIME=%TIME%"

:: Step 1: Check .NET SDK
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host '1/4' -N -F Yellow; Write-Host '] ' -N -F DarkGray; Write-Host 'Проверка .NET SDK...' -F White"
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host ' ✗ ' -N -F Red; Write-Host '] ' -N -F DarkGray; Write-Host '.NET SDK не найден!' -F Red"
    echo.
    powershell -NoProfile -Command "Write-Host '   Скачать: ' -N -F DarkGray; Write-Host 'https://dotnet.microsoft.com/download' -F Cyan"
    echo.
    pause
    exit /b 1
)
for /f %%v in ('dotnet --version') do set "DOTNET_VER=%%v"
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host ' ✓ ' -N -F Green; Write-Host '] ' -N -F DarkGray; Write-Host '.NET SDK ' -N -F White; Write-Host '%DOTNET_VER%' -F Cyan"
echo.

:: Step 2: Clean
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host '2/4' -N -F Yellow; Write-Host '] ' -N -F DarkGray; Write-Host 'Очистка...' -F White"
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1
if exist publish rmdir /s /q publish >nul 2>&1
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host ' ✓ ' -N -F Green; Write-Host '] ' -N -F DarkGray; Write-Host 'Очищено' -F White"
echo.

:: Step 3: Restore packages
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host '3/4' -N -F Yellow; Write-Host '] ' -N -F DarkGray; Write-Host 'Восстановление пакетов...' -F White"
dotnet restore --nologo -v q >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host ' ✗ ' -N -F Red; Write-Host '] ' -N -F DarkGray; Write-Host 'Ошибка восстановления пакетов!' -F Red"
    echo.
    pause
    exit /b 1
)
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host ' ✓ ' -N -F Green; Write-Host '] ' -N -F DarkGray; Write-Host 'Пакеты восстановлены' -F White"
echo.

:: Step 4: Build
powershell -NoProfile -Command "Write-Host '   [' -N -F DarkGray; Write-Host '4/4' -N -F Yellow; Write-Host '] ' -N -F DarkGray; Write-Host 'Сборка...' -F White"
echo.
powershell -NoProfile -Command "Write-Host '   ────────────────────────────────────────────────────────' -F DarkGray"
dotnet publish -c Release --nologo -v q
set BUILD_RESULT=%errorlevel%
powershell -NoProfile -Command "Write-Host '   ────────────────────────────────────────────────────────' -F DarkGray"
echo.

if %BUILD_RESULT% neq 0 (
    powershell -NoProfile -Command ^
        "Write-Host '   ╭──────────────────────────────────────────────────────────╮' -F Red; " ^
        "Write-Host '   │                     СБОРКА ПРОВАЛЕНА                     │' -F Red; " ^
        "Write-Host '   ╰──────────────────────────────────────────────────────────╯' -F Red"
    echo.
    pause
    exit /b 1
)

:: Cleanup temp folders
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

:: Get file size
set "EXE_PATH=publish\CustosAC.exe"
for %%f in ("%EXE_PATH%") do (
    set /a "SIZE_KB=%%~zf/1024"
    set /a "SIZE_MB=%%~zf/1048576"
)

:: Success banner
powershell -NoProfile -Command ^
    "Write-Host '   ╭──────────────────────────────────────────────────────────╮' -F Green; " ^
    "Write-Host '   │                                                          │' -F Green; " ^
    "Write-Host '   │' -N -F Green; Write-Host '            ✓  СБОРКА УСПЕШНО ЗАВЕРШЕНА  ✓              ' -N -F White; Write-Host '│' -F Green; " ^
    "Write-Host '   │                                                          │' -F Green; " ^
    "Write-Host '   ╰──────────────────────────────────────────────────────────╯' -F Green"
echo.

:: Build info
powershell -NoProfile -Command ^
    "Write-Host '   ┌─────────────────────────────────────────────────────────┐' -F DarkGray; " ^
    "Write-Host '   │  ' -N -F DarkGray; Write-Host 'Файл:     ' -N -F White; Write-Host 'publish\CustosAC.exe                       ' -N -F Cyan; Write-Host '│' -F DarkGray; " ^
    "Write-Host '   │  ' -N -F DarkGray; Write-Host 'Размер:   ' -N -F White; Write-Host '%SIZE_MB% MB (%SIZE_KB% KB)                              ' -N -F Cyan; Write-Host '│' -F DarkGray; " ^
    "Write-Host '   │  ' -N -F DarkGray; Write-Host 'Платформа:' -N -F White; Write-Host ' Windows x64                              ' -N -F Cyan; Write-Host '│' -F DarkGray; " ^
    "Write-Host '   └─────────────────────────────────────────────────────────┘' -F DarkGray"
echo.

:: Hint
powershell -NoProfile -Command "Write-Host '   Запуск: ' -N -F DarkGray; Write-Host '.\publish\CustosAC.exe' -F Cyan"
echo.

pause
