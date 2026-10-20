@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Tools\Android\Export-Apk.ps1"
set "cvdApkExit=%ERRORLEVEL%"
echo.
if not "%cvdApkExit%"=="0" echo Exportacao interrompida. Leia a mensagem acima.
pause
exit /b %cvdApkExit%
