@echo off
chcp 65001 >NUL 2>&1

REM Переходим в директорию где находится bat-файл
cd /d "%~dp0"

echo ═══════════════════════════════════════════════════════════════════════
echo                    КОМПИЛЯЦИЯ CUSTOSAC - GO VERSION
echo ═══════════════════════════════════════════════════════════════════════
echo.
echo Рабочая директория: %CD%
echo.

REM Проверка наличия Go
where go >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Go не установлен!
    echo.
    echo Скачайте Go с официального сайта:
    echo https://go.dev/dl/
    echo.
    echo После установки перезапустите командную строку.
    pause
    exit /b 1
)

echo [1/4] Проверка версии Go...
go version
echo.

REM Проверка наличия rsrc для встраивания манифеста
where rsrc >nul 2>&1
if %errorlevel% neq 0 (
    echo [2/4] Установка rsrc для встраивания манифеста...
    go install github.com/akavel/rsrc@latest
    if %errorlevel% neq 0 (
        echo.
        echo [ERROR] Ошибка установки rsrc!
        pause
        exit /b 1
    )
    echo.
    echo rsrc успешно установлен.
    echo.
) else (
    echo [2/4] rsrc уже установлен
    echo.
)

REM Проверка наличия config.json
if not exist "config.json" (
    echo.
    echo [ERROR] config.json не найден!
    echo Создайте config.json с вашими Discord данными.
    pause
    exit /b 1
)

echo [3/5] Шифрование и встраивание config.json...
go run tools\encrypt_config.go
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Ошибка шифрования конфигурации!
    pause
    exit /b 1
)
echo.

echo [4/5] Создание файла ресурсов с манифестом...
REM Удаляем старый файл ресурсов, если существует
if exist rsrc.syso (
    del rsrc.syso
    echo Старый rsrc.syso удален.
)
rsrc -manifest custosAC.manifest -o rsrc.syso
if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Ошибка создания файла ресурсов!
    pause
    exit /b 1
)
echo Файл ресурсов создан успешно.
echo.

echo [5/5] Компиляция custosAC...
echo.

REM Компиляция с флагами для уменьшения размера
REM -ldflags "-s -w" убирает отладочную информацию
REM rsrc.syso автоматически встроится с манифестом

REM Удаляем старый исполняемый файл, если существует
if exist custosAC.exe (
    del custosAC.exe
    echo Старый custosAC.exe удален.
)

go build -ldflags "-s -w" -o custosAC.exe ./cmd/custos

if %errorlevel% neq 0 (
    echo.
    echo [ERROR] Ошибка компиляции!
    echo Очистка промежуточных файлов...
    if exist rsrc.syso del rsrc.syso
    pause
    exit /b 1
)

echo.
echo ═══════════════════════════════════════════════════════════════════════
echo                         ГОТОВО!
echo ═══════════════════════════════════════════════════════════════════════
echo.
echo Создан файл:
echo   • custosAC.exe - готовая программа с правами администратора
echo.
echo Размер:
for %%A in (custosAC.exe) do echo   %%~zA байт
echo.
echo Для запуска:
echo   1. Дважды кликните на custosAC.exe
echo   2. Подтвердите запрос на права администратора (появится автоматически)
echo.
echo ВАЖНО - БЕЗОПАСНОСТЬ:
echo   • config.json встроен и зашифрован внутри .exe файла
echo   • Программа работает автономно без внешних файлов
echo   • НЕ загружайте internal\config\embedded.go на GitHub!
echo   • config.json уже в .gitignore
echo   • Для большей безопасности измените XOR ключ в tools\encrypt_config.go
echo.
echo Промежуточный файл rsrc.syso сохранен для повторной сборки.
echo Для очистки используйте: del rsrc.syso
echo.
pause
