@echo off
chcp 65001 >nul
setlocal EnableDelayedExpansion

REM Получаем ESC символ
for /F "delims=" %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"

REM Цвета ANSI
set "R=%ESC%[0m"
set "RED=%ESC%[31m"
set "GRN=%ESC%[32m"
set "YLW=%ESC%[33m"
set "CYN=%ESC%[36m"
set "ORG=%ESC%[38;5;208m"
set "BLD=%ESC%[1m"
set "DIM=%ESC%[2m"

cls
echo.
echo   %ORG%╭──────────────────────────────────────────────────────────╮%R%
echo   %ORG%│                                                          │%R%
echo   %ORG%│     ██████╗██╗   ██╗███████╗████████╗ ██████╗ ███████╗   │%R%
echo   %ORG%│    ██╔════╝██║   ██║██╔════╝╚══██╔══╝██╔═══██╗██╔════╝   │%R%
echo   %ORG%│    ██║     ██║   ██║███████╗   ██║   ██║   ██║███████╗   │%R%
echo   %ORG%│    ██║     ██║   ██║╚════██║   ██║   ██║   ██║╚════██║   │%R%
echo   %ORG%│    ╚██████╗╚██████╔╝███████║   ██║   ╚██████╔╝███████║   │%R%
echo   %ORG%│     ╚═════╝ ╚═════╝ ╚══════╝   ╚═╝    ╚═════╝ ╚══════╝   │%R%
echo   %ORG%│                                                          │%R%
echo   %ORG%│                    BUILD SYSTEM                          │%R%
echo   %ORG%│                                                          │%R%
echo   %ORG%╰──────────────────────────────────────────────────────────╯%R%
echo.
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo.

REM Проверка .NET SDK
echo   %CYN%[i]%R% Проверка .NET SDK...
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo.
    echo   %RED%[-] .NET SDK не установлен!%R%
    echo   %CYN%    Скачайте .NET 8 SDK с https://dotnet.microsoft.com/download%R%
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%v in ('dotnet --version') do set DOTNET_VERSION=%%v
echo   %GRN%[+]%R% .NET SDK найден: %GRN%v%DOTNET_VERSION%%R%
echo.

REM Шаг 1: Очистка
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo   %CYN%[~]%R% %BLD%%YLW%Шаг 1/3:%R% Очистка предыдущих сборок...
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo.

if exist bin (
    echo   %CYN%[i]%R% Удаление bin/...
    rmdir /s /q bin
    echo   %GRN%[+]%R% bin/ удалена
)
if exist obj (
    echo   %CYN%[i]%R% Удаление obj/...
    rmdir /s /q obj
    echo   %GRN%[+]%R% obj/ удалена
)
if exist publish (
    echo   %CYN%[i]%R% Удаление publish/...
    rmdir /s /q publish
    echo   %GRN%[+]%R% publish/ удалена
)
echo.

REM Шаг 2: Сборка
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo   %CYN%[~]%R% %BLD%%YLW%Шаг 2/3:%R% Компиляция проекта...
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo.

echo   %CYN%[i]%R% Запуск dotnet publish...
echo.
dotnet publish -c Release --nologo -v q

if %errorlevel% neq 0 (
    echo.
    echo   %RED%[-] %BLD%Сборка не удалась!%R%
    echo   %YLW%[!]%R% Проверьте ошибки выше
    echo.
    pause
    exit /b 1
)

echo.
echo   %GRN%[+] Компиляция завершена успешно%R%
echo.

REM Шаг 3: Финальная очистка
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo   %CYN%[~]%R% %BLD%%YLW%Шаг 3/3:%R% Финальная очистка...
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo.

if exist bin (
    echo   %CYN%[i]%R% Очистка bin/...
    rmdir /s /q bin
    echo   %GRN%[+]%R% bin/ очищена
)
if exist obj (
    echo   %CYN%[i]%R% Очистка obj/...
    rmdir /s /q obj
    echo   %GRN%[+]%R% obj/ очищена
)
echo.

REM Результат
echo   %DIM%════════════════════════════════════════════════════════════%R%
echo.
echo   %GRN%%BLD%╔══════════════════════════════════════════════════════════╗%R%
echo   %GRN%%BLD%║                                                          ║%R%
echo   %GRN%%BLD%║                    СБОРКА ЗАВЕРШЕНА!                     ║%R%
echo   %GRN%%BLD%║                                                          ║%R%
echo   %GRN%%BLD%╚══════════════════════════════════════════════════════════╝%R%
echo.
echo   %GRN%[+]%R% Файлы готовы в папке: %CYN%publish\%R%
echo.
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo   %CYN%[i]%R% Содержимое publish/:
echo   %DIM%────────────────────────────────────────────────────────────%R%
echo.

for %%f in (publish\*) do (
    echo   %CYN%[~]%R% %%~nxf
)

echo.
echo   %DIM%════════════════════════════════════════════════════════════%R%
echo.
pause
