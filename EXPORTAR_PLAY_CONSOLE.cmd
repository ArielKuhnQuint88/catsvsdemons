@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Android\Export-PlayConsole.ps1"
set "cvdExit=%ERRORLEVEL%"
echo.
if not "%cvdExit%"=="0" echo Exportacao interrompida. Leia a mensagem acima.
pause
exit /b %cvdExit%
