@echo off
REM WotlkBot Startup Script with Ollama
REM This script starts Ollama (if not running) and launches WotlkBot

echo ============================================
echo    WotlkBot with Phi-3 AI
echo ============================================
echo.

REM Check if Ollama is installed
where ollama >nul 2>&1
if %errorlevel% neq 0 (
    echo [ERROR] Ollama is not installed!
    echo Please install Ollama from: https://ollama.ai/download
    echo.
    pause
    exit /b 1
)

REM Check if Ollama is already running
curl -s http://localhost:11434/api/tags >nul 2>&1
if %errorlevel% neq 0 (
    echo [INFO] Starting Ollama...
    start /B ollama serve >nul 2>&1
    timeout /t 3 >nul
)

REM Check if phi3:mini model is available
echo [INFO] Checking Phi-3 model...
ollama list | findstr "phi3:mini" >nul 2>&1
if %errorlevel% neq 0 (
    echo [INFO] Downloading Phi-3 Mini model (this may take a few minutes)...
    ollama pull phi3:mini
    if %errorlevel% neq 0 (
        echo [ERROR] Failed to download Phi-3 model!
        pause
        exit /b 1
    )
)

echo [OK] Ollama is ready with Phi-3!
echo.

REM Get bot arguments
if "%~1"=="" (
    echo Usage: StartBot.bat ^<host^> ^<account^> ^<password^> ^<character^>
    echo Example: StartBot.bat 127.0.0.1 myaccount mypassword MyCharacter
    echo.
    set /p HOST="Server host: "
    set /p ACCOUNT="Account name: "
    set /p PASSWORD="Password: "
    set /p CHARACTER="Character name: "
) else (
    set HOST=%1
    set ACCOUNT=%2
    set PASSWORD=%3
    set CHARACTER=%4
)

echo.
echo [INFO] Starting WotlkBot...
echo.

cd /d "%~dp0"
WotlkBot.exe %HOST% %ACCOUNT% %PASSWORD% %CHARACTER%

pause
