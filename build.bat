@echo off
chcp 65001 >nul
echo ========================================
echo    CustosAC - Build Script
echo ========================================
echo.

REM Проверка наличия .NET SDK
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo [ОШИБКА] .NET SDK не установлен!
    echo Скачайте .NET 8 SDK с https://dotnet.microsoft.com/download
    pause
    exit /b 1
)

echo [1/3] Очистка...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist publish rmdir /s /q publish

echo [2/3] Сборка...
dotnet publish -c Release --nologo -v q

if %errorlevel% neq 0 (
    echo [ОШИБКА] Сборка не удалась!
    pause
    exit /b 1
)

echo [3/3] Очистка временных файлов...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj

echo.
echo ========================================
echo    ГОТОВО!
echo    Файлы в папке: publish\
echo ========================================
dir publish /b
echo ========================================
pause
