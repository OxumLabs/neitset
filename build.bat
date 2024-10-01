@echo off
setlocal

REM Build for Windows
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=link

REM Build for Linux
dotnet publish -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=link

REM Define variables
set RELEASE_DIR=release
set WIN_PUBLISHED_FILE=bin\Release\net8.0\win-x64\publish\neitset.exe
set LINUX_PUBLISHED_FILE=bin\Release\net8.0\linux-x64\publish\neitset

REM Delete the release folder if it exists
if exist %RELEASE_DIR% (
    rmdir /s /q %RELEASE_DIR%
)

REM Create a new release folder
mkdir %RELEASE_DIR%

REM Copy the Windows build to the release folder with the desired name
if exist %WIN_PUBLISHED_FILE% (
    copy %WIN_PUBLISHED_FILE% %RELEASE_DIR%\neitset_win.exe
)

REM Copy the Linux build to the release folder with the desired name
if exist %LINUX_PUBLISHED_FILE% (
    copy %LINUX_PUBLISHED_FILE% %RELEASE_DIR%\neitset_lin
)

REM Confirmation message
echo Builds have been copied to the 'release' folder as neitset_win.exe and neitset_lin.

endlocal
pause
