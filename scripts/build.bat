@echo off
setlocal
echo ========================================================
echo        MicGuardian - Compilador Automatizado
echo ========================================================

set SCRIPT_DIR=%~dp0
set ROOT_DIR=%SCRIPT_DIR%..
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    set CSC=C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe
)

if not exist "%CSC%" (
    echo [ERROR] No se encontro el compilador csc.exe en .NET Framework v4.0.
    echo Asegurate de estar ejecutando Windows con .NET Framework 4.x instalado.
    pause
    exit /b 1
)

echo Compilando MicGuardian.exe desde archivos fuente modulares...
"%CSC%" /target:winexe /optimize+ /out:"%ROOT_DIR%\MicGuardian.exe" /recurse:"%ROOT_DIR%\src\MicGuardian\*.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [OK] Compilacion exitosa.
    echo Binario generado: %ROOT_DIR%\MicGuardian.exe
) else (
    echo.
    echo [ERROR] Fallo la compilacion. Revisa los errores superiores.
)

pause
