@echo off
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
    "Write-Host '   │' -F DarkYellow -N; Write-Host '                      B U I L D E R                        ' -F DarkGray -N; Write-Host '│' -F DarkYellow; " ^
    "Write-Host '   │                                                          │' -F DarkYellow; " ^
    "Write-Host '   ╰──────────────────────────────────────────────────────────╯' -F DarkYellow; " ^
    "Write-Host ''"

:: Check .NET SDK
powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host '..' -F Yellow -N; Write-Host ']' -N; Write-Host ' Checking .NET SDK...' -F White"
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host '!!' -F Red -N; Write-Host ']' -N; Write-Host ' .NET SDK not found!' -F Red"
    echo.
    echo    Download: https://dotnet.microsoft.com/download
    echo.
    pause
    exit /b 1
)
for /f %%v in ('dotnet --version') do set "VER=%%v"
powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host 'OK' -F Green -N; Write-Host ']' -N; Write-Host ' .NET SDK ' -F White -N; Write-Host '%VER%' -F Cyan"

:: Clean
powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host '..' -F Yellow -N; Write-Host ']' -N; Write-Host ' Cleaning...' -F White"
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1
if exist publish rmdir /s /q publish >nul 2>&1
powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host 'OK' -F Green -N; Write-Host ']' -N; Write-Host ' Cleaned' -F White"

:: Build
powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host '..' -F Yellow -N; Write-Host ']' -N; Write-Host ' Building...' -F White"
echo.
dotnet publish -c Release --nologo -v q
if %errorlevel% neq 0 (
    echo.
    powershell -NoProfile -Command "Write-Host '   [' -N; Write-Host '!!' -F Red -N; Write-Host ']' -N; Write-Host ' Build failed!' -F Red"
    echo.
    pause
    exit /b 1
)

:: Cleanup temp
if exist bin rmdir /s /q bin >nul 2>&1
if exist obj rmdir /s /q obj >nul 2>&1

:: Success
powershell -NoProfile -Command ^
    "Write-Host ''; " ^
    "Write-Host '   ╭──────────────────────────────────────────────────────────╮' -F DarkYellow; " ^
    "Write-Host '   │' -F DarkYellow -N; Write-Host '                       SUCCESS!                            ' -F Green -N; Write-Host '│' -F DarkYellow; " ^
    "Write-Host '   ╰──────────────────────────────────────────────────────────╯' -F DarkYellow; " ^
    "Write-Host ''"

:: Output info
powershell -NoProfile -Command "Write-Host '   Output: ' -F DarkGray -N; Write-Host 'publish\CustosAC.exe' -F Cyan"

:: File size
for %%f in (publish\CustosAC.exe) do (
    set /a "SIZE_MB=%%~zf/1048576"
)
powershell -NoProfile -Command "Write-Host '   Size:   ' -F DarkGray -N; Write-Host '%SIZE_MB% MB' -F Cyan"
echo.
pause
