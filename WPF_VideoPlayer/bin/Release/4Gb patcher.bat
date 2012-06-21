@if "%VS90COMNTOOLS%"=="" goto check_vs2010
@call "%VS90COMNTOOLS%vsvars32.bat"
@goto editb

:check_vs2010
@if "%VS100COMNTOOLS%"=="" goto check_vs2011
@call "%VS100COMNTOOLS%vsvars32.bat"
@goto editb

:check_vs2011
@if "%VS110COMNTOOLS%"=="" goto error_no_vs
@call "%VS110COMNTOOLS%vsvars32.bat"

:editb
@call editbin /LARGEADDRESSAWARE WPF_VideoPlayer.exe
@if errorlevel 1 @pause

@goto end

:error_no_vs
@echo ERROR: VS90COMNTOOLS, VS100COMNTOOLS or VS110COMNTOOLS variables aren't set.
@echo ERROR: Can't determine where to find Visual Studio Tools directory!
@echo ERROR: Run "editbin.exe /LARGEADDRESSAWARE WPF_VideoPlayer.exe" by yourself..
@pause

:end
@exit

----------

Or you can add this text to "Project->Properties->Build Event->Post-build event command line"
and WPF_VideoPlayer.exe will be patched during the compilation:

call "$(DevEnvDir)..\tools\vsvars32.bat"
editbin /LARGEADDRESSAWARE "$(TargetPath)"