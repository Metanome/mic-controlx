@echo off
echo Building MicControlX...
echo.

echo Cleaning previous builds...
if exist "publish" rmdir /s /q "publish"
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"

echo.
echo Building single-file executable...
dotnet publish src\MicControlX.csproj --configuration Release --runtime win-x64 --output publish --verbosity quiet /p:DebugType=none /p:DebugSymbols=false

if errorlevel 1 (
    echo.
    echo ERROR: Build failed!
    pause
    exit /b 1
)

echo.
echo Cleaning debug files...
if exist "publish\*.pdb" del /q "publish\*.pdb"
if exist "publish\*.xml" del /q "publish\*.xml"

echo.
echo SUCCESS: Executable created in publish folder
echo.
pause
