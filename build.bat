@echo off
chcp 65001 >nul
echo ========================================
echo    CustosAC - Build Script (C#)
echo ========================================
echo.

REM Проверка наличия .NET SDK
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не установлен!
    echo.
    echo Скачайте .NET 8 SDK с https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)

echo [1/3] Очистка предыдущей сборки...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo [2/3] Сборка проекта...
dotnet build -c Release

if %errorlevel% neq 0 (
    echo.
    echo [ОШИБКА] Сборка не удалась!
    pause
    exit /b 1
)

echo [3/3] Публикация (single file)...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish

if %errorlevel% neq 0 (
    echo.
    echo [ОШИБКА] Публикация не удалась!
    pause
    exit /b 1
)

echo.
echo ========================================
echo    СБОРКА УСПЕШНА!
echo ========================================
echo.
echo Исполняемый файл: publish\CustosAC.exe
echo.

pause
