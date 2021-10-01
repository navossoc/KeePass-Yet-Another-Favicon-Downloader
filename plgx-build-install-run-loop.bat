:: Automatic UAC elevation script from https://stackoverflow.com/a/37669661/7024666

NET SESSION
IF %ERRORLEVEL% NEQ 0 GOTO ELEVATE
GOTO ADMINTASKS

:ELEVATE
CD /d %~dp0
MSHTA "javascript: var shell = new ActiveXObject('shell.application'); shell.ShellExecute('%~nx0', '', '', 'runas', 1);close();"
EXIT

:ADMINTASKS
:: Change to script directory
CD /d %~dp0

:: Call build script
call plgx-build.bat 1

echo Installing...
copy "%CD%\%NAME%.plgx" %KEEPASS%"\..\Plugins\%NAME%.plgx" /Y
echo.

echo Running KeePass...
echo !NB! build-install-run will be executed again once KeePass is closed
echo  - Change your code and compile before closing KeePass
echo  - If this is not desired, close this window before closing KeePass
%KEEPASS%

:: After KeePass is closed, do build-install-run again 
:: (useful only if the code has been changed and built before KeePass was closed)
GOTO ADMINTASKS
