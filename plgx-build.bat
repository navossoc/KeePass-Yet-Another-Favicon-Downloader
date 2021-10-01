@echo off
CHDIR /D %CD%

:: Configuration
SET SOURCE=%CD%\YAFD
SET TARGET=%CD%\publish
SET KEEPASS="%ProgramFiles%\KeePass Password Safe 2\KeePass.exe"

REM Windows x86 (32 bits)
if not exist %KEEPASS% (
	SET KEEPASS="%ProgramFiles(x86)%\KeePass Password Safe 2\KeePass.exe"
)

REM Windows x64 (64 bits)
if not exist %KEEPASS% (
	SET KEEPASS="%ProgramW6432%\KeePass Password Safe 2\KeePass.exe"
)

SET NAME=YetAnotherFaviconDownloader

:: Clean old files
if exist %TARGET% (
	echo Cleaning...
	rmdir /S /Q %TARGET%
)
echo.

:: Copy the files needed to build the plugin
echo Copying...
xcopy "%SOURCE%" "%TARGET%\" /s /e /exclude:plgx-exclude.txt
echo.

:: Let KeePass do its magic
echo Building...
%KEEPASS% --plgx-create "%TARGET%" --plgx-prereq-kp:2.34
::%KEEPASS% --plgx-create "%TARGET%" --plgx-prereq-kp:2.34 --plgx-prereq-net:2.0
echo.

:: Deploy PLGX file
echo Deploying...
move /Y publish.plgx %NAME%.plgx
del "%SOURCE%\bin\Debug\%NAME%.*"
copy "%CD%\%NAME%.plgx" "%SOURCE%\bin\Debug\%NAME%.plgx" /Y
echo.

if "%~1" == "" pause
