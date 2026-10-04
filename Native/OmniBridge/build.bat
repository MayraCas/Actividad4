@echo off
REM Compila OmniBridge.dll (plugin nativo de Unity) y test_omni.exe.
REM Requiere Visual Studio Build Tools con el toolset de C++ x64.

setlocal
set VCVARS="C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
if not exist %VCVARS% (
    echo No se encontro vcvars64.bat en %VCVARS%
    exit /b 1
)
call %VCVARS% >nul

cd /d "%~dp0"
if not exist build mkdir build
cd build

echo --- Compilando OmniBridge.dll ---
cl /nologo /std:c++17 /O2 /EHsc /W3 /MD /LD ^
   ..\OmniBridge.cpp ..\hd_dyn.cpp ^
   /Fe:OmniBridge.dll /link kernel32.lib user32.lib
if errorlevel 1 exit /b 1

echo --- Compilando test_omni.exe ---
cl /nologo /std:c++17 /O2 /EHsc /W3 /MD ^
   ..\test_omni.cpp ^
   /Fe:test_omni.exe /link OmniBridge.lib
if errorlevel 1 exit /b 1

echo --- Copiando a Assets\Plugins\x86_64 ---
set DEST=..\..\..\Assets\Plugins\x86_64
if not exist "%DEST%" mkdir "%DEST%"
copy /y OmniBridge.dll "%DEST%\OmniBridge.dll" >nul

REM hd.dll viene con los drivers del Touch; se copia junto al plugin.
set HDDLL="C:\Program Files\3D Systems\Touch Device Drivers\OpenHaptics_Demos\HapticDemo\HapticDemo_Data\Plugins\x86_64\hd.dll"
if exist %HDDLL% (
    copy /y %HDDLL% "%DEST%\hd.dll" >nul
    copy /y %HDDLL% hd.dll >nul
)

echo.
echo Listo. DLL en %DEST%
endlocal
