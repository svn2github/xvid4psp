@echo off

if defined VS160COMNTOOLS call :patch_exe %VS160COMNTOOLS%
if defined VS150COMNTOOLS call :patch_exe %VS150COMNTOOLS%
if defined VS140COMNTOOLS call :patch_exe %VS140COMNTOOLS%
if defined VS130COMNTOOLS call :patch_exe %VS130COMNTOOLS%
if defined VS120COMNTOOLS call :patch_exe %VS120COMNTOOLS%
if defined VS110COMNTOOLS call :patch_exe %VS110COMNTOOLS%
if defined VS100COMNTOOLS call :patch_exe %VS100COMNTOOLS%
if defined VS90COMNTOOLS call :patch_exe %VS90COMNTOOLS%
goto :print_error

:patch_exe
call "%*vsvars32.bat"
call editbin /LARGEADDRESSAWARE XviD4PSP.exe
if %errorlevel% neq 0 (pause) else (echo patched - OK!)
exit

:print_error
echo ERROR: VS90COMNTOOLS - VS160COMNTOOLS variables aren't set.
echo ERROR: Can't determine where to find Visual Studio Tools directory!
echo ERROR: Run "editbin.exe /LARGEADDRESSAWARE XviD4PSP.exe" by yourself..
echo.
echo Or you can add this text to "Project->Properties->Build Events->Post-build event command line"
echo and XviD4PSP.exe will be patched during the compilation:
echo.
echo call "$(DevEnvDir)..\tools\vsvars32.bat"
echo editbin /LARGEADDRESSAWARE "$(TargetPath)"
echo.
pause
