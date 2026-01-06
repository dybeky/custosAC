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

echo [1/2] Очистка...
if exist bin rmdir /s /q bin
if exist obj rmdir /s /q obj
if exist publish rmdir /s /q publish

echo [2/2] Сборка...
dotnet publish CustosAC.csproj -c Release -o publish

if %errorlevel% neq 0 (
    echo [ОШИБКА] Сборка не удалась!
    pause
    exit /b 1
)

echo.
echo ========================================
echo    ГОТОВО! publish\CustosAC.exe
echo ========================================
pause
