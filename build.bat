@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

for /F "delims=" %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"

set "R=%ESC%[0m"
set "RED=%ESC%[31m"
set "GRN=%ESC%[32m"
set "YLW=%ESC%[33m"
set "CYN=%ESC%[36m"
set "ORG=%ESC%[38;5;208m"
set "DIM=%ESC%[2m"

cls
echo.
echo   %ORG%┌─────────────────────────────────────────┐%R%
echo   %ORG%│%R%  %ORG%█▀▀ █ █ █▀▀ ▀█▀ █▀█ █▀▀%R%  %DIM%Build System%R%  %ORG%│%R%
echo   %ORG%│%R%  %ORG%█▄▄ █▄█ ▄▄█  █  █▄█ ▄▄█%R%  %DIM%v1.0%R%          %ORG%│%R%
echo   %ORG%└─────────────────────────────────────────┘%R%
echo.

REM Проверка .NET SDK
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo   %RED%✗%R% .NET SDK не найден
    echo   %DIM%  Скачайте: https://dotnet.microsoft.com/download%R%
    pause
    exit /b 1
)
for /f "tokens=*" %%v in ('dotnet --version') do set "V=%%v"
echo   %GRN%✓%R% .NET %DIM%%V%%R%
echo.

REM Очистка
echo   %CYN%›%R% Очистка...
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1
if exist publish rmdir /s /q publish >nul 2>&1
echo   %GRN%✓%R% Очищено
echo.

REM Сборка
echo   %CYN%›%R% Сборка...
dotnet publish -c Release --nologo -v q

if %errorlevel% neq 0 (
    echo.
    echo   %RED%✗ Ошибка сборки%R%
    pause
    exit /b 1
)
echo   %GRN%✓%R% Скомпилировано
echo.

REM Финальная очистка
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

REM Результат
echo   %DIM%─────────────────────────────────────────%R%
echo.
echo   %GRN%✓ Готово!%R%  %DIM%→%R%  publish\
echo.
for %%f in (publish\*) do echo     %DIM%•%R% %%~nxf
echo.
pause
